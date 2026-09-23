using UnityEngine;

// Cuando la chica entra al rango de agro, queda enganchado durante 'duracionAgro' segundos
// y encadena ataques completos: anticipacion -> picada hacia donde estaba la chica -> remonte.
// Al terminar la ventana, completa el ataque en curso y vuelve volando a su zona.
[RequireComponent(typeof(PajaroRevoloteo))]
public class AtaquePajaro : MonoBehaviour
{
    private enum Fase { Revoloteando, Anticipacion, Picada, Remonte, Regreso }

    [Header("Animaciones")]
    [SerializeField] private Sprite[] framesAnticipacion;
    [SerializeField] private float fpsAnticipacion = 6f;
    [SerializeField] private Sprite[] framesPicada;
    [SerializeField] private float fpsPicada = 12f;

    [Header("Rango de agro (circulo centrado en el pajaro)")]
    [SerializeField] private float radioAgro = 1.85f;
    [Tooltip("Segundos que sigue atacando desde que la chica entra al agro, aunque se aleje.")]
    [SerializeField] private float duracionAgro = 10f;

    [Header("Ataque")]
    [Tooltip("Vida que le saca a la chica por picada (su vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Velocidad de la picada (unidades por segundo).")]
    [SerializeField] private float velocidadPicada = 9f;
    [Tooltip("Distancia maxima de cada picada: si la chica esta mas lejos, se tira hacia ella pero sin irse de su zona.")]
    [SerializeField] private float alcancePicada = 4f;
    [Tooltip("Segundos que remonta (hacia su zona) despues de cada picada, antes de la siguiente.")]
    [SerializeField] private float tiempoRemonte = 0.8f;
    [Tooltip("Velocidad al remontar y al volver a su zona.")]
    [SerializeField] private float velocidadRegreso = 4f;
    [Tooltip("Segundos de espera, ya de vuelta en su zona, antes de poder engancharse otra vez.")]
    [SerializeField] private float espera = 1.5f;
    [Tooltip("El dibujo original mira hacia la derecha.")]
    [SerializeField] private bool dibujoMiraDerecha = true;

    private PajaroRevoloteo revoloteo;
    private SpriteRenderer dibujo;
    private Collider2D hitbox;
    private SaludPersonaje victima;
    private Collider2D hitboxVictima;

    private Fase fase = Fase.Revoloteando;
    private float inicioFase;
    private float finAgro;
    private float libreDesde;
    private Vector2 objetivo;
    private bool pego;

    public bool Atacando => fase != Fase.Revoloteando;

    private void Awake()
    {
        revoloteo = GetComponent<PajaroRevoloteo>();
        dibujo = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
    }

    private void Start()
    {
        victima = FindFirstObjectByType<SaludPersonaje>();
        if (victima != null) hitboxVictima = victima.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (hitboxVictima == null) return;

        switch (fase)
        {
            case Fase.Revoloteando:
                if (Time.time >= libreDesde && VictimaEnAgro())
                {
                    finAgro = Time.time + duracionAgro;
                    revoloteo.enabled = false;
                    CambiarFase(Fase.Anticipacion);
                }
                break;

            case Fase.Anticipacion:
                MirarHacia(hitboxVictima.bounds.center.x);
                if (Time.time - inicioFase >= Duracion(framesAnticipacion, fpsAnticipacion))
                {
                    Vector2 desde = transform.position;
                    objetivo = desde + Vector2.ClampMagnitude((Vector2)hitboxVictima.bounds.center - desde, alcancePicada);
                    pego = false;
                    CambiarFase(Fase.Picada);
                }
                break;

            case Fase.Picada:
                MirarHacia(objetivo.x);
                Mover(objetivo, velocidadPicada);
                if (!pego && hitbox != null && hitbox.Distance(hitboxVictima).isOverlapped)
                {
                    pego = true;
                    victima.RecibirDanio(danio);
                }
                // La picada sigue de largo hasta el objetivo aunque ya haya pegado, y no se corta
                // antes de mostrar la animacion completa.
                float enPicada = Time.time - inicioFase;
                bool llego = Vector2.Distance(transform.position, objetivo) < 0.05f;
                if ((llego && enPicada >= Duracion(framesPicada, fpsPicada)) || enPicada > 2f) CambiarFase(Fase.Remonte);
                break;

            case Fase.Remonte:
                MirarHacia(revoloteo.CentroZona.x);
                Mover(revoloteo.CentroZona, velocidadRegreso);
                if (Time.time - inicioFase >= tiempoRemonte)
                    CambiarFase(Time.time < finAgro ? Fase.Anticipacion : Fase.Regreso);
                break;

            case Fase.Regreso:
                Vector2 casa = revoloteo.CentroZona;
                MirarHacia(casa.x);
                Mover(casa, velocidadRegreso);
                if (Vector2.Distance(transform.position, casa) < 0.05f)
                {
                    revoloteo.enabled = true;
                    libreDesde = Time.time + espera;
                    CambiarFase(Fase.Revoloteando);
                }
                break;
        }
    }

    private void LateUpdate()
    {
        if (fase == Fase.Anticipacion) MostrarFrame(framesAnticipacion, fpsAnticipacion, false);
        else if (fase == Fase.Picada) MostrarFrame(framesPicada, fpsPicada, true);
    }

    private void CambiarFase(Fase nueva)
    {
        fase = nueva;
        inicioFase = Time.time;
    }

    private void Mover(Vector2 destino, float velocidad)
    {
        Vector2 p = Vector2.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    private void MirarHacia(float x)
    {
        if (dibujo == null || Mathf.Abs(x - transform.position.x) < 0.01f) return;
        dibujo.flipX = dibujoMiraDerecha ? x < transform.position.x : x > transform.position.x;
    }

    private void MostrarFrame(Sprite[] lista, float fps, bool enLoop)
    {
        if (lista == null || lista.Length == 0) return;
        int i = Mathf.FloorToInt((Time.time - inicioFase) * fps);
        dibujo.sprite = lista[enLoop ? i % lista.Length : Mathf.Min(i, lista.Length - 1)];
    }

    private static float Duracion(Sprite[] lista, float fps) => lista == null ? 0f : lista.Length / Mathf.Max(0.01f, fps);

    private bool VictimaEnAgro()
    {
        foreach (var c in Physics2D.OverlapCircleAll(transform.position, radioAgro))
            if (c == hitboxVictima) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.5f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radioAgro);
    }
}
