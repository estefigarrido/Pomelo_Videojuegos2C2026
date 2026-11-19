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
    [Tooltip("Alto de todo el dibujo (unidades).")]
    [SerializeField] private float altoDibujo = 21.6f;
    [Tooltip("Donde va el centro de la camara, medido desde arriba del dibujo (unidades). Si con eso la camara se " +
             "saliera del dibujo, se corre lo justo para no salirse.")]
    [SerializeField] private float centroDesdeArriba = 10.8f;

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
    [Tooltip("Cartel de tutorial (Caminar) que aparece cuando la chica recibe los controles.")]
    [SerializeField] private CartelTutorial cartelAlLlegar;

    [Header("Camara que sigue a la chica")]
    [Tooltip("Despues de Comenzar, la camara va hasta la chica y la sigue (como en el nivel) por la cueva y la " +
             "parte 2, sin salirse del dibujo. Apagado = encuadra la parte 2 entera, como antes.")]
    [SerializeField] private bool seguirALaChica = true;
    [Tooltip("Mitad del alto que muestra la camara al seguirla (en el nivel es 5.4; aca un poco mas, para que el doble " +
             "salto entre en el cuadro).")]
    [SerializeField] private float tamanoSeguir = 6.4f;
    [Tooltip("Cuanto por encima de la chica va el centro de la camara (unidades).")]
    [SerializeField] private float offsetVertical = 1.6f;
    [Tooltip("Cuanto por encima del borde de abajo del dibujo queda siempre el borde de abajo de la camara (unidades): " +
             "asi nunca se ve debajo del agua (las columnas y el fondo vacio).")]
    [SerializeField] private float margenAbajo = 0.3f;
    [Tooltip("Segundos que tarda la camara en alcanzarla (suave).")]
    [SerializeField] private float suavizadoSeguir = 0.25f;
    [Tooltip("Apagado: la camara la sigue solo de costado y se queda a la altura del piso donde arranca (no sube " +
             "cuando salta). Prendido: tambien la sigue para arriba y para abajo.")]
    [SerializeField] private bool seguirAltura = false;
    private Vector3 velCamara;
    private float alturaPiso = float.NaN;   // y de la chica parada donde arranca

    [Header("Golpes")]
    [Tooltip("Apagado: en la intro la chica no puede atacar (ni con click izquierdo ni con el mando).")]
    [SerializeField] private bool chicaPuedeAtacar = false;

    [Header("Alejar la camara despues del jarron")]
    [Tooltip("Apenas la chica llega a la piedra que sigue al jarron ('alejarDesdeX'), pisandola o pasandole por arriba " +
             "(aunque la salte), y despues de 'esperaAlejar' segundos, la camara se aleja de a poco hasta " +
             "'tamanoAlejado' y queda asi, para ver el alrededor (los murales, el techo y el agua). Lo pone el menu " +
             "que arma la escena.")]
    [SerializeField] private float alejarDesdeX = float.NaN;
    [SerializeField] private float alejarHastaX = float.NaN;
    [SerializeField] private float esperaAlejar = 0f;
    [Tooltip("Mitad del alto que muestra la camara alejada (antes 5.4: muestra unas 28 unidades de alto).")]
    [SerializeField] private float tamanoAlejado = 14f;
    [Tooltip("Segundos que tarda en alejarse (gradual).")]
    [SerializeField] private float duracionAlejar = 2f;
    private float tiempoSobrePiedra;
    private float alejado = -1f;   // -1 = todavia no; de 0 a 1 mientras se aleja; 1 = alejada
    // la camara ya termino de alejarse (lo usa EscenaMaestra para que brille la hojita del mural)
    public bool TerminoDeAlejarse => alejado >= 1f;

    private BotonesInicio botones;
    private Behaviour[] controlesChica;
    private bool chicaActiva;
    private bool esperandoDialogo;
    private bool comenzando;
    // ya se apreto "Comenzar juego" (lo usa EntradaTemplo para empezar a precargar el Nivel 1)
    public bool Comenzo => comenzando;
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
        botones = GetComponentInChildren<BotonesInicio>(true);

        // la chica no se mueve ni ataca mientras se ve la cueva (la barra espaciadora es tambien "Comenzar")
        if (chica != null)
        {
            controlesChica = chica.GetComponents<MonoBehaviour>();
            foreach (var c in controlesChica) c.enabled = false;
            ApoyarEnElPiso(chica);
            alturaPiso = chica.transform.position.y;
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
            // con los botones de inicio, comienza el boton "Comenzar juego" (BotonesInicio llama a Comenzar).
            // Sin botones (escena vieja): la barra espaciadora o la X, como antes.
            if (botones == null)
            {
                var teclado = Keyboard.current;
                if ((teclado != null && teclado.spaceKey.wasPressedThisFrame) || Mando.Aceptar) Comenzar();
            }
            Encuadrar();   // por si cambia el tamano de la ventana
            return;
        }

        float t = Time.unscaledTime - inicio;
        RevisarAlejar();

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
        // (sin dialogo al llegar, la chica recibe los controles apenas llega la camara; hoy el dialogo de Jaya lo
        // abre el Lore de la vasija)
        if (x >= 1f && !chicaActiva && !esperandoDialogo)
        {
            if (dialogoAlLlegar == null) ActivarChica();
            else if (tCamara >= duracionRecorrido + segundosAntesDelDialogo)
            {
                if (CuadroDialogo.Instancia != null && CuadroDialogo.Instancia.Abrir(dialogoAlLlegar)) esperandoDialogo = true;
                else ActivarChica();
            }
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

    // Lo llama el boton "Comenzar juego" (BotonesInicio): el titulo y los botones se desvanecen y la camara arranca
    public void Comenzar()
    {
        if (comenzando) return;
        comenzando = true;
        inicio = Time.unscaledTime;
        // la musica del titulo se va y entra la de la intro
        if (MusicaIntro.Instancia != null) MusicaIntro.Instancia.Comenzar();
    }

    // apenas llega a la piedra que sigue al jarron (pisandola o saltandola) -> la camara se aleja de a poco (2 s) y queda
    // alejada
    private void RevisarAlejar()
    {
        if (alejado >= 0f)
        {
            alejado = Mathf.MoveTowards(alejado, 1f, Time.deltaTime / Mathf.Max(0.01f, duracionAlejar));
            return;
        }
        if (!chicaActiva || chica == null || float.IsNaN(alejarDesdeX)) return;
        // cuenta desde que llega al borde izquierdo de la piedra, este en el piso o en el aire (y aunque ya la haya
        // pasado de un salto)
        bool llego = chica.transform.position.x >= alejarDesdeX;
        tiempoSobrePiedra = llego ? tiempoSobrePiedra + Time.deltaTime : 0f;
        if (llego && tiempoSobrePiedra >= esperaAlejar) alejado = 0f;
    }

    private void ActivarChica()
    {
        chicaActiva = true;
        // (Estefi, 19/11) en la intro del templo no ataca: el selector de golpes (click izquierdo / boton de atacar / V)
        // queda apagado salvo que se prenda 'chicaPuedeAtacar'
        if (controlesChica != null)
            foreach (var c in controlesChica)
                if (c != null && (chicaPuedeAtacar || !(c is SelectorAtaque))) c.enabled = true;
        if (cartelAlLlegar != null) cartelAlLlegar.Mostrar();   // el cartel de Caminar aparece suave
    }

    // muestra de ancho la parte que toca (la cueva, la otra parte o en el camino entre las dos), centrada en alto
    private void Encuadrar()
    {
        if (camara == null) return;
        float aspecto = camara.aspect > 0f ? camara.aspect : 16f / 9f;
        var p = camara.transform.position;

        // la cueva entera (el principio)
        float altoCueva = Mathf.Min(anchoCueva / aspecto, altoDibujo);
        var centroCueva = new Vector2(anchoCueva / 2f, Mathf.Clamp(altoDibujo - centroDesdeArriba, altoCueva / 2f, altoDibujo - altoCueva / 2f));

        Vector2 centroDestino; float altoDestino;
        if (seguirALaChica && chica != null)
        {
            // pegada a la chica, con el zoom del nivel, sin salirse del dibujo (de 0 a finOtraParte)
            float tamano = alejado > 0f ? Mathf.Lerp(tamanoSeguir, tamanoAlejado, Mathf.SmoothStep(0f, 1f, alejado)) : tamanoSeguir;
            altoDestino = Mathf.Min(tamano * 2f, altoDibujo);
            float medioAncho = altoDestino * aspecto / 2f;
            var c = chica.transform.position;
            float y = seguirAltura || float.IsNaN(alturaPiso) ? c.y : alturaPiso;
            centroDestino = new Vector2(Mathf.Clamp(c.x, medioAncho, Mathf.Max(medioAncho, finOtraParte - medioAncho)),
                                        Mathf.Clamp(y + offsetVertical, margenAbajo + altoDestino / 2f,
                                                    Mathf.Max(margenAbajo + altoDestino / 2f, altoDibujo - altoDestino / 2f)));
        }
        else
        {
            // la parte 2 entera (como antes)
            float ancho2 = finOtraParte - inicioOtraParte;
            altoDestino = Mathf.Min(ancho2 / aspecto, altoDibujo);
            centroDestino = new Vector2((inicioOtraParte + finOtraParte) / 2f,
                                        Mathf.Clamp(altoDibujo - centroDesdeArriba, altoDestino / 2f, altoDibujo - altoDestino / 2f));
        }

        float alto = Mathf.Lerp(altoCueva, altoDestino, avance);
        var centro = Vector2.Lerp(centroCueva, centroDestino, avance);
        camara.orthographicSize = alto / 2f;
        var objetivo = new Vector3(centro.x, centro.y, p.z);
        // ya llego: la sigue suave
        if (seguirALaChica && avance >= 1f && Application.isPlaying)
        {
            objetivo = Vector3.SmoothDamp(p, objetivo, ref velCamara, suavizadoSeguir);
            // el tamano cambia enseguida (al alejarse) pero la posicion llega con retraso: sin este limite, mientras
            // se aleja, el borde de abajo bajaba del dibujo y se veia debajo del agua. Se limita despues del suavizado
            float medioAlto = alto / 2f, medioAncho = medioAlto * aspecto;
            float yMin = margenAbajo + medioAlto, yMax = Mathf.Max(yMin, altoDibujo - medioAlto);
            float xMin = medioAncho, xMax = Mathf.Max(xMin, finOtraParte - medioAncho);
            var limitado = new Vector3(Mathf.Clamp(objetivo.x, xMin, xMax), Mathf.Clamp(objetivo.y, yMin, yMax), objetivo.z);
            if (limitado.x != objetivo.x) velCamara.x = 0f;
            if (limitado.y != objetivo.y) velCamara.y = 0f;
            objetivo = limitado;
        }
        camara.transform.position = objetivo;
    }
}
