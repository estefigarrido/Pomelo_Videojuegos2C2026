using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla de inicio (escena Inicio_introducción): la cueva con el titulo TÉCHNI y "Comenzar  [espacio] o [X]".
// - Al principio la camara encuadra la cueva de inicio de punta a punta (los 3612 px del dibujo de Caro).
// - Con la barra espaciadora o la X (Cruz) del joystick: el titulo y "Comenzar" se desvanecen y la camara se
//   desplaza hacia la derecha hasta encuadrar la otra parte del dibujo (la catarata y el templo).
//   Despues funde a negro y carga el nivel.
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class PantallaInicio : MonoBehaviour
{
    [Tooltip("Escena que se carga al terminar el recorrido (tiene que estar en la lista del build).")]
    [SerializeField] private string escenaJuego = "Nivel1_Templo";

    [Header("Encuadre (en unidades: 100 px del dibujo = 1)")]
    [SerializeField] private Camera camara;
    [Tooltip("La cueva de inicio: desde x 0 hasta aca (3612 px).")]
    [SerializeField] private float anchoCueva = 36.12f;
    [Tooltip("La otra parte del dibujo (catarata y templo): empieza en x 36.11 y termina en 72.02.")]
    [SerializeField] private float inicioOtraParte = 36.11f;
    [SerializeField] private float finOtraParte = 72.02f;
    [Tooltip("Alto de todo el dibujo (2160 px).")]
    [SerializeField] private float altoDibujo = 21.6f;

    [Header("Al comenzar")]
    [Tooltip("El titulo y 'Comenzar' (se desvanecen).")]
    [SerializeField] private CanvasGroup textos;
    [SerializeField] private float duracionDesvanecer = 0.35f;
    [Tooltip("Segundos que tarda la camara en ir de la cueva a la otra parte.")]
    [SerializeField] private float duracionRecorrido = 1.8f;
    [Tooltip("Segundos quieta en la otra parte antes de fundir a negro.")]
    [SerializeField] private float esperaAlLlegar = 0.8f;
    [Tooltip("Si esta apagado, la camara se queda en la otra parte y no carga el nivel. " +
             "Por ahora apagado (Caro: que al llegar se quede ahi, sin fundir a negro).")]
    [SerializeField] private bool cargarNivelAlTerminar = false;
    [Tooltip("Imagen negra de pantalla completa que se usa para el fundido.")]
    [SerializeField] private Image fundido;
    [SerializeField] private float duracionFundido = 0.8f;

    [Header("La chica de la parte 2")]
    [Tooltip("Arranca sin controles y los recibe cuando la camara llega a la parte 2.")]
    [SerializeField] private GameObject chica;
    [Tooltip("Dialogo que se abre al llegar a la parte 2 (Jaya sale a explorar). La chica recibe los controles " +
             "recien cuando se cierra. Vacio = los recibe al llegar.")]
    [SerializeField] private Dialogo dialogoAlLlegar;
    [Tooltip("Segundos que pasan desde que la camara llega a la parte 2 hasta que aparece el dialogo.")]
    [SerializeField] private float segundosAntesDelDialogo = 3f;

    private Behaviour[] controlesChica;
    private bool chicaActiva;
    private bool esperandoDialogo;
    private bool comenzando;
    private float inicio;
    private float avance;   // 0 = la cueva, 1 = la otra parte

    private void Awake()
    {
        Time.timeScale = 1f;   // por si se vuelve aca desde la pausa
        if (fundido != null)
        {
            fundido.color = new Color(0f, 0f, 0f, 0f);
            fundido.raycastTarget = false;
        }
        Encuadrar();

        // la chica no se mueve ni ataca mientras se ve la cueva (la barra espaciadora es tambien "Comenzar")
        if (chica != null)
        {
            controlesChica = chica.GetComponents<MonoBehaviour>();
            foreach (var c in controlesChica) c.enabled = false;
            ApoyarEnElPiso(chica);
        }
    }

    // Deja a la chica parada sobre lo que tenga abajo (la piedra): la baja (o la sube un poquito) hasta que su
    // colisionador toca el piso. Lo usa tambien el menu que arma la escena.
    public static void ApoyarEnElPiso(GameObject chica, float hastaUnidades = 6f)
    {
        if (chica == null || !chica.TryGetComponent<Collider2D>(out var propio)) return;
        Physics2D.SyncTransforms();
        var b = propio.bounds;
        // desde un poquito por encima de los pies, para abajo
        var desde = new Vector2(b.center.x, b.min.y + 0.3f);
        RaycastHit2D? mejor = null;
        foreach (var h in Physics2D.RaycastAll(desde, Vector2.down, hastaUnidades + 0.3f))
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.transform.IsChildOf(chica.transform)) continue;
            if (mejor == null || h.distance < mejor.Value.distance) mejor = h;
        }
        if (mejor == null) return;
        float mover = mejor.Value.point.y - b.min.y + 0.005f;
        chica.transform.position += new Vector3(0f, mover, 0f);
        Physics2D.SyncTransforms();
    }

    // reloj del agua (shader Pomelo/AguaOndulante): avanza con el tiempo del juego, asi en pausa el agua se frena
    private static readonly int TiempoAgua = Shader.PropertyToID("_TiempoAgua");
    private float tiempoAgua;

    private void Update()
    {
        tiempoAgua += Time.deltaTime;
        Shader.SetGlobalFloat(TiempoAgua, tiempoAgua);

        if (!comenzando)
        {
            var teclado = Keyboard.current;
            if ((teclado != null && teclado.spaceKey.wasPressedThisFrame) || Mando.Aceptar)
            {
                comenzando = true;
                inicio = Time.unscaledTime;
            }
            Encuadrar();   // por si cambia el tamano de la ventana
            return;
        }

        float t = Time.unscaledTime - inicio;

        // el titulo y "Comenzar" se van primero
        if (textos != null)
            textos.alpha = 1f - Mathf.SmoothStep(0f, 1f, duracionDesvanecer > 0f ? t / duracionDesvanecer : 1f);

        // la camara arranca enseguida, junto con el desvanecido. Curva seno: acelera y frena parejo, sin tirones
        float tCamara = t;
        float x = duracionRecorrido > 0f ? Mathf.Clamp01(tCamara / duracionRecorrido) : 1f;
        avance = 0.5f - 0.5f * Mathf.Cos(x * Mathf.PI);
        Encuadrar();

        // al llegar a la parte 2: primero el dialogo (Jaya sale a explorar) y, cuando se cierra, la chica recibe
        // los controles (moverse, correr, saltar, como en el nivel)
        if (x >= 1f && tCamara >= duracionRecorrido + segundosAntesDelDialogo && !chicaActiva && !esperandoDialogo)
        {
            if (dialogoAlLlegar != null && CuadroDialogo.Instancia != null && CuadroDialogo.Instancia.Abrir(dialogoAlLlegar))
                esperandoDialogo = true;
            else ActivarChica();
        }
        if (esperandoDialogo && !CuadroDialogo.Abierto)
        {
            esperandoDialogo = false;
            ActivarChica();
        }

        if (!cargarNivelAlTerminar) return;
        float tFundido = tCamara - duracionRecorrido - esperaAlLlegar;
        if (tFundido < 0f) return;
        float k = duracionFundido > 0f ? Mathf.Clamp01(tFundido / duracionFundido) : 1f;
        if (fundido != null) fundido.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, k));
        if (k >= 1f)
        {
            enabled = false;
            SceneManager.LoadScene(escenaJuego);
        }
    }

    private void ActivarChica()
    {
        chicaActiva = true;
        if (controlesChica != null) foreach (var c in controlesChica) if (c != null) c.enabled = true;
    }

    // muestra de ancho la parte que toca (la cueva, la otra parte o en el camino entre las dos), centrada en alto
    private void Encuadrar()
    {
        if (camara == null) return;
        float desde = Mathf.Lerp(0f, inicioOtraParte, avance);
        float hasta = Mathf.Lerp(anchoCueva, finOtraParte, avance);
        float ancho = hasta - desde;
        float aspecto = camara.aspect > 0f ? camara.aspect : 16f / 9f;
        float alto = Mathf.Min(ancho / aspecto, altoDibujo);
        camara.orthographicSize = alto / 2f;
        var p = camara.transform.position;
        camara.transform.position = new Vector3((desde + hasta) / 2f, altoDibujo / 2f, p.z);
    }
}
