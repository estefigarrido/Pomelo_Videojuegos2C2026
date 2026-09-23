using UnityEngine;

// Cuando la chica entra al rango de agro: anticipacion en el lugar, picada hacia donde
// estaba la chica y vuelta volando a su zona. Pega si las hitboxes se tocan durante la picada.
[RequireComponent(typeof(PajaroRevoloteo))]
public class AtaquePajaro : MonoBehaviour
{
    private enum Fase { Revoloteando, Anticipacion, Picada, Regreso }

    [Header("Animaciones")]
    [SerializeField] private Sprite[] framesAnticipacion;
    [SerializeField] private float fpsAnticipacion = 6f;
    [SerializeField] private Sprite[] framesPicada;
    [SerializeField] private float fpsPicada = 12f;

    [Header("Rango de agro (circulo centrado en el pajaro)")]
    [SerializeField] private float radioAgro = 1.85f;

    [Header("Ataque")]
    [Tooltip("Vida que le saca a la chica por picada (su vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Velocidad de la picada (unidades por segundo).")]
    [SerializeField] private float velocidadPicada = 9f;
    [Tooltip("Velocidad con la que vuelve a su zona despues de atacar.")]
    [SerializeField] private float velocidadRegreso = 4f;
    [Tooltip("Segundos de espera, ya de vuelta en su zona, antes de poder atacar otra vez.")]
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
                    revoloteo.enabled = false;
                    MirarHacia(hitboxVictima.bounds.center.x);
                    CambiarFase(Fase.Anticipacion);
                }
                break;

            case Fase.Anticipacion:
                MirarHacia(hitboxVictima.bounds.center.x);
                if (Time.time - inicioFase >= Duracion(framesAnticipacion, fpsAnticipacion))
                {
                    objetivo = hitboxVictima.bounds.center;
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
                if (pego || Vector2.Distance(transform.position, objetivo) < 0.05f) CambiarFase(Fase.Regreso);
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
