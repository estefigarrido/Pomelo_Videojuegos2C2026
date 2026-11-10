using UnityEngine;

// La plantita del jarron que sube a la chica y la plantita horizontal que sale de la pared.
// - Cuando la chica se para arriba de las hojas, la planta se desliza hacia arriba (es mas larga de lo que se
//   ve: el resto queda dentro del jarron) hasta la altura del puente.
// - Al llegar arriba, la plantita horizontal sale de la pared deslizandose (no se estira) y arma un puente.
// - Las dos vuelven a su lugar si la chica se cae, si se baja de las dos y llega al otro nivel (del lado de la
//   pared), o si nadie las usa durante 'esperaBajar'.
// Los dos pisos son de un solo sentido (PlatformEffector2D): desde abajo se atraviesan, asi nunca la aprietan.
public class PlantaElevadora : MonoBehaviour
{
    [Header("Planta del jarron")]
    [Tooltip("Cuanto sube la planta (unidades).")]
    [SerializeField] private float subida = 7.61f;
    [SerializeField] private float segundosSubir = 3f;
    [SerializeField] private float segundosBajar = 2f;
    [Tooltip("Segundos sin la chica arriba (ni en el puente) antes de volver a bajar.")]
    [SerializeField] private float esperaBajar = 2.5f;
    [Tooltip("Donde se para la chica, relativo a la planta: centro y tamano (solo a la derecha del tallo, asi al subir no roza la plataforma de piedra de la izquierda).")]
    [SerializeField] private Vector2 centroPisable = new Vector2(0.51f, 6.34f);
    [SerializeField] private Vector2 tamanoPisable = new Vector2(1.25f, 0.3f);

    [Header("Planta horizontal (puente)")]
    [Tooltip("La planta horizontal ya afuera (su base en la cara de la pared, su eje Y apuntando hacia afuera). Al empezar se mete en la pared y sale deslizandose.")]
    [SerializeField] private Transform puente;
    [Tooltip("Largo del puente afuera de la pared (unidades) y alto de su piso.")]
    [SerializeField] private float largoPuente = 13.2f;
    [SerializeField] private float altoPisoPuente = 0.3f;
    [SerializeField] private float segundosPuente = 1.2f;

    private Rigidbody2D rb, rbPuente;
    private BoxCollider2D pisable, pisoPuente;
    private Collider2D chica;
    private SpriteRenderer[] dibujosPuente;
    private Vector2 inicio, inicioPuente;
    private float progreso, progresoPuente, ultimaVezArriba = -99f;
    private bool reiniciando;

    private void Awake()
    {
        inicio = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        pisable = CrearPiso("piso planta", transform, out _);
        Ajustar(pisable, centroPisable, tamanoPisable);

        if (puente != null)
        {
            inicioPuente = puente.position;
            dibujosPuente = puente.GetComponentsInChildren<SpriteRenderer>(true);
            var go = new GameObject("piso puente");
            go.transform.position = new Vector3(inicioPuente.x, inicioPuente.y, 0f);
            rbPuente = go.AddComponent<Rigidbody2D>();
            rbPuente.bodyType = RigidbodyType2D.Kinematic;
            pisoPuente = CrearPiso("piso", go.transform, out _);
            AplicarPuente(0f);
        }
    }

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.GetComponent<Collider2D>();
    }

    private static BoxCollider2D CrearPiso(string nombre, Transform padre, out PlatformEffector2D ef)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var c = go.AddComponent<BoxCollider2D>();
        c.usedByEffector = true;
        ef = go.AddComponent<PlatformEffector2D>();
        ef.useOneWay = true;
        ef.surfaceArc = 160f;   // solo desde arriba: de costado y desde abajo se atraviesa
        return c;
    }

    private void Ajustar(BoxCollider2D c, Vector2 centroMundo, Vector2 tamanoMundo)
    {
        // el collider vive en la escala de la planta: se pasa a local
        Vector3 s = transform.lossyScale;
        c.transform.localPosition = Vector3.zero;
        c.offset = new Vector2(centroMundo.x / s.x, centroMundo.y / s.y);
        c.size = new Vector2(tamanoMundo.x / Mathf.Abs(s.x), tamanoMundo.y / Mathf.Abs(s.y));
    }

    private bool ChicaSobre(Collider2D piso)
    {
        if (chica == null || piso == null || !piso.enabled) return false;
        Bounds b = piso.bounds, c = chica.bounds;
        bool encima = c.min.y >= b.max.y - 0.15f && c.min.y <= b.max.y + 0.2f;
        bool adentroX = c.max.x > b.min.x + 0.05f && c.min.x < b.max.x - 0.05f;
        return encima && adentroX;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool enPlanta = ChicaSobre(pisable);
        bool enPuente = progresoPuente > 0.01f && ChicaSobre(pisoPuente);
        if (enPlanta || enPuente) ultimaVezArriba = Time.time;

        // se reinician si la chica se cae o si llega al otro nivel (del lado de la pared, a la altura del puente o mas)
        if (!reiniciando && (progreso > 0f || progresoPuente > 0f) && !enPlanta && !enPuente && chica != null)
        {
            Bounds c = chica.bounds;
            float pisoPlanta = pisable.bounds.max.y;
            bool cayo = c.min.y < pisoPlanta - 0.6f && c.min.y < inicioPuente.y - 0.6f;
            bool delOtroLado = puente != null && Vector2.Dot((Vector2)c.center - inicioPuente, puente.up) < -0.3f;
            bool llego = delOtroLado && c.min.y > inicioPuente.y - 0.2f;
            if (cayo || llego) reiniciando = true;
        }
        if (reiniciando && progreso <= 0f && progresoPuente <= 0f) reiniciando = false;
        bool bajar = reiniciando || Time.time - ultimaVezArriba > esperaBajar;

        // planta: sube mientras la chica esta arriba; baja cuando se reinicia (y el puente ya se guardo)
        if (enPlanta && !reiniciando) progreso = Mathf.MoveTowards(progreso, 1f, dt / segundosSubir);
        else if (bajar && progresoPuente <= 0f) progreso = Mathf.MoveTowards(progreso, 0f, dt / segundosBajar);
        float e = progreso * progreso * (3f - 2f * progreso);
        rb.MovePosition(inicio + Vector2.up * subida * e);

        // puente: sale cuando la planta llego arriba; se guarda si nadie lo usa
        if (puente != null)
        {
            if (progreso >= 1f && !reiniciando) progresoPuente = Mathf.MoveTowards(progresoPuente, 1f, dt / segundosPuente);
            else if (bajar) progresoPuente = Mathf.MoveTowards(progresoPuente, 0f, dt / segundosPuente);
            AplicarPuente(progresoPuente);
        }
    }

    private void AplicarPuente(float p)
    {
        float e = 1f - (1f - p) * (1f - p);   // sale rapido y frena al final
        // el dibujo se desliza (no se estira): metido en la pared hasta la punta, o afuera entero
        float largo = largoPuente * e;
        puente.position = new Vector3(inicioPuente.x, inicioPuente.y, puente.position.z) - puente.up * (largoPuente - largo);
        bool afuera = largo > 0.2f;
        if (dibujosPuente != null) foreach (var d in dibujosPuente) d.enabled = largo > 0.01f;
        pisoPuente.enabled = afuera;
        // piso: desde la pared hasta la punta, con el borde de arriba a la altura del puente
        Vector2 dir = puente.up;   // hacia afuera de la pared
        pisoPuente.size = new Vector2(Mathf.Max(0.05f, largo), altoPisoPuente);
        pisoPuente.offset = new Vector2(dir.x * largo * 0.5f, dir.y * largo * 0.5f - altoPisoPuente * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.8f);
        Vector3 c = transform.position + (Vector3)centroPisable;
        Gizmos.DrawWireCube(c, tamanoPisable);
        Gizmos.DrawWireCube(c + Vector3.up * subida, tamanoPisable);
        if (puente != null)
        {
            Vector3 a = puente.position, b = a + puente.up * largoPuente;
            Gizmos.DrawLine(a, b);
        }
    }
}
