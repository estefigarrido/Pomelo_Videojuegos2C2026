using UnityEngine;

// Va en cada botellita colgante (junto con BotellitaRompible). La soga dibujada se reemplaza por una soga
// de verdad (CadenaVerlet + LineRenderer) atada al techo, y la botellita cuelga de la punta:
// - Se acomoda a la altura del salto de la chica: el fondo de la botellita queda 'sobreLaChica' arriba de
//   su cabeza, asi caminando no la toca y saltando si. La soga se alarga o acorta hasta el techo.
// - Se hamaca con la gravedad cuando la chica la toca, y el aire la mece apenas.
// - Al romperse (BotellitaRompible) la botellita cae y la soga queda colgando suelta.
// Los puntos de enganche los mide el menu de configuracion sobre los PNG.
[RequireComponent(typeof(SpriteRenderer))]
public class SogaBotellita : MonoBehaviour
{
    [Tooltip("El cuerpo de la botellita sin la soga (mismo lienzo que el dibujo completo).")]
    [SerializeField] private Sprite cuerpo;
    [Tooltip("Arriba de todo de la soga dibujada, en el lienzo (0 a 1).")]
    [SerializeField] private Vector2 puntoArriba = new Vector2(0.5f, 1f);
    [Tooltip("Donde la soga se ata a la botellita, en el lienzo (0 a 1).")]
    [SerializeField] private Vector2 puntoEnganche = new Vector2(0.5f, 0.5f);
    [Tooltip("Lo que ocupa el cuerpo en el lienzo (0 a 1): de aca sale el collider.")]
    [SerializeField] private Rect rectCuerpo = new Rect(0f, 0f, 1f, 0.5f);

    [Header("Altura")]
    [Tooltip("Cuanto queda el fondo de la botellita arriba de la cabeza de la chica parada (unidades).")]
    [SerializeField] private float sobreLaChica = 0.25f;
    [SerializeField] private float altoChica = 2.34f;

    [Header("Soga")]
    [SerializeField] private Material materialSoga;
    [SerializeField] private Color colorSoga = new Color(0.05f, 0.23f, 0.17f, 1f);
    [SerializeField] private float anchoSoga = 0.035f;
    [SerializeField] private int puntos = 12;
    [SerializeField] private float gravedad = 12f;
    [Tooltip("Cuanto pesa la botellita respecto de un pedazo de soga.")]
    [SerializeField] private float masaBotellita = 4f;
    [SerializeField] private float empuje = 1.1f;
    [SerializeField] private float viento = 0.15f;

    private CadenaVerlet cadena;
    private LineRenderer linea;
    private SpriteRenderer dibujo;
    private Collider2D cuerpoChica;
    private Rigidbody2D rbChica;
    private bool suelta;
    private float fase;

    private Vector3 Local(Vector2 enLienzo)
    {
        Sprite sp = dibujo.sprite;
        return new Vector3((enLienzo.x * sp.rect.width - sp.pivot.x) / sp.pixelsPerUnit,
                           (enLienzo.y * sp.rect.height - sp.pivot.y) / sp.pixelsPerUnit, 0f);
    }

    private void Start()
    {
        dibujo = GetComponent<SpriteRenderer>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) { cuerpoChica = salud.GetComponent<Collider2D>(); rbChica = salud.GetComponent<Rigidbody2D>(); }
        fase = Random.value * 10f;

        Vector2 arriba = transform.TransformPoint(Local(puntoArriba));
        float techo = Rayo(arriba + Vector2.down * 0.05f, Vector2.up, 25f);
        if (!float.IsNaN(techo)) arriba.y = techo;

        // la botellita a la altura del salto: el fondo del cuerpo, 'sobreLaChica' arriba de la cabeza
        float fondo = transform.TransformPoint(Local(new Vector2(rectCuerpo.center.x, rectCuerpo.yMin))).y;
        float piso = Rayo(new Vector2(transform.position.x, fondo + 0.05f), Vector2.down, 30f);
        if (!float.IsNaN(piso)) transform.position += Vector3.up * (piso + altoChica + sobreLaChica - fondo);

        if (cuerpo != null) dibujo.sprite = cuerpo;
        var caja = GetComponent<BoxCollider2D>();
        if (caja != null)
        {
            Vector3 a = Local(rectCuerpo.min), b = Local(rectCuerpo.max);
            caja.offset = (a + b) * 0.5f;
            caja.size = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
        }

        Vector2 enganche = transform.TransformPoint(Local(puntoEnganche));
        if (arriba.y < enganche.y + 0.2f) arriba.y = enganche.y + 0.2f;
        arriba.x = enganche.x;
        int n = Mathf.Max(3, puntos);
        var pts = new Vector2[n];
        for (int i = 0; i < n; i++) pts[i] = Vector2.Lerp(arriba, enganche, i / (n - 1f));
        cadena = new CadenaVerlet(pts, 0f, masaBotellita);

        var go = new GameObject("soga");
        go.transform.SetParent(transform.parent, false);
        linea = go.AddComponent<LineRenderer>();
        linea.positionCount = n;
        linea.useWorldSpace = true;
        linea.widthMultiplier = anchoSoga;
        linea.numCapVertices = 2;
        linea.startColor = linea.endColor = colorSoga;
        if (materialSoga != null) linea.sharedMaterial = materialSoga;
        linea.sortingLayerID = dibujo.sortingLayerID;
        linea.sortingOrder = dibujo.sortingOrder - 1;
        Acomodar();
    }

    // la botellita se rompio: el cuerpo cae (lo maneja BotellitaRompible) y la soga queda suelta
    public void Soltar()
    {
        if (cadena == null) return;
        suelta = true;
        cadena.SoltarPunta();
    }

    private void Update()
    {
        if (cadena == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        float t = Time.time;
        var aire = new Vector2(viento * Mathf.Sin(t * 0.9f + fase), 0f);
        if (cuerpoChica != null && rbChica != null)
        {
            Bounds b = cuerpoChica.bounds; b.Expand(0.25f);
            cadena.Empujar(b, rbChica.linearVelocity, dt, empuje);
        }
        cadena.Paso(dt, gravedad, aire);
        Acomodar();
    }

    private void Acomodar()
    {
        for (int i = 0; i < cadena.Cantidad; i++)
            linea.SetPosition(i, new Vector3(cadena.p[i].x, cadena.p[i].y, transform.position.z));
        if (suelta) return;

        // la botellita sigue a la punta, girada como el ultimo tramo
        int u = cadena.Cantidad - 1;
        transform.rotation = Quaternion.Euler(0f, 0f, cadena.Angulo(u) * 0.85f);
        Vector3 enganche = transform.TransformPoint(Local(puntoEnganche));
        transform.position += (Vector3)cadena.Punta - new Vector3(enganche.x, enganche.y, 0f) + Vector3.forward * 0f;
    }

    // primer collider solido en esa direccion (ignora triggers, la chica y los mobs)
    private static float Rayo(Vector2 desde, Vector2 dir, float distancia)
    {
        foreach (var h in Physics2D.RaycastAll(desde, dir, distancia))
        {
            if (h.collider.isTrigger || h.collider.GetComponentInParent<SaludPersonaje>() != null
                || h.collider.GetComponentInParent<VidaEnemigo>() != null || h.collider.GetComponentInParent<BotellitaRompible>() != null) continue;
            return h.point.y;
        }
        return float.NaN;
    }
}
