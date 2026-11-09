using UnityEngine;

// Barra de vida de Umbrae (Figma, pagina UI: "barra de vida umbrae" con sus ornamentos y la gema verde).
// Esta en el mundo, arriba de la arena: cuando la camara se aleja para la pelea aparece como si siempre
// hubiera estado ahi. Arranca vacia (lila) y:
// - Al empezar la pelea (la camara fija el encuadre) se llena como un liquido oscuro y viscoso que despues
//   se asienta en violeta. Igual cuando Umbrae se cura mientras sube a tirar los rayos.
// - Cada golpe la vacia de derecha a izquierda: el pedazo perdido queda un momento en lila claro con un
//   destello blanco y se achica; la barra tiembla un poquito.
// - Mientras Umbrae esta protegido (subiendo, no se le puede pegar) los ornamentos titilan en violeta.
// - La gema arranca desaturada y se satura en proporcion a la vida que perdio (300 de vida = 100 %).
// - Al morir se vacia del todo y la gema queda con todo su color.
public class BarraVidaUmbrae : MonoBehaviour
{
    [SerializeField] private VidaEnemigo vida;
    [SerializeField] private UmbraeBoss umbrae;
    [SerializeField] private CamaraSeguidora camara;

    [Header("Piezas")]
    [SerializeField] private SpriteRenderer pista;
    [SerializeField] private SpriteRenderer gema;
    [Tooltip("Marco y ornamentos: titilan cuando Umbrae esta protegido.")]
    [SerializeField] private SpriteRenderer[] ornamentos;

    [Header("Tiempos")]
    [Tooltip("Velocidad del liquido al llenarse (parte de la barra por segundo).")]
    [SerializeField] private float velocidadLlenado = 0.35f;
    [Tooltip("Velocidad con la que el liquido oscuro se asienta en violeta.")]
    [SerializeField] private float velocidadAsentado = 0.22f;
    [Tooltip("Segundos que queda el pedazo perdido antes de achicarse.")]
    [SerializeField] private float esperaFantasma = 0.35f;
    [SerializeField] private float velocidadFantasma = 0.9f;
    [SerializeField] private float segundosDestello = 0.25f;
    [SerializeField] private float temblor = 0.08f;
    [SerializeField] private float segundosTemblor = 0.2f;
    [SerializeField] private Color colorProtegido = new Color(0.78f, 0.6f, 1f, 1f);
    [Tooltip("Material con el shader Pomelo/Sprite Aditivo: copias de los ornamentos que suman luz violeta.")]
    [SerializeField] private Material materialAditivo;

    private float relleno, asentado, fantasma;
    private float ultimoGolpe = -99f;
    private Vector3 posicion;
    private MaterialPropertyBlock mpbPista, mpbGema;
    private SpriteRenderer[] brillos;
    private float alfaBrillo;
    private MaterialPropertyBlock mpbBrillo;
    private static readonly int IdColor = Shader.PropertyToID("_Color");

    private static readonly int IdRelleno = Shader.PropertyToID("_Relleno");
    private static readonly int IdAsentado = Shader.PropertyToID("_Asentado");
    private static readonly int IdFantasma = Shader.PropertyToID("_Fantasma");
    private static readonly int IdDestello = Shader.PropertyToID("_Destello");
    private static readonly int IdSaturacion = Shader.PropertyToID("_Saturacion");

    private void Start()
    {
        posicion = transform.localPosition;
        mpbPista = new MaterialPropertyBlock();
        mpbGema = new MaterialPropertyBlock();
        mpbBrillo = new MaterialPropertyBlock();
        // copias aditivas de los ornamentos (apagadas) para el titileo de "protegido"
        brillos = new SpriteRenderer[ornamentos.Length];
        for (int i = 0; i < ornamentos.Length; i++)
        {
            if (ornamentos[i] == null || materialAditivo == null) continue;
            var go = new GameObject(ornamentos[i].name + " (brillo)");
            go.transform.SetParent(ornamentos[i].transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ornamentos[i].sprite; sr.sharedMaterial = materialAditivo;
            sr.sortingLayerID = ornamentos[i].sortingLayerID; sr.sortingOrder = ornamentos[i].sortingOrder + 1;
            sr.color = Color.clear;
            brillos[i] = sr;
        }
        if (camara == null) camara = FindFirstObjectByType<CamaraSeguidora>();
        Aplicar(0f);
    }

    private void Update()
    {
        float max = vida != null ? Mathf.Max(1f, vida.VidaMaxima) : 1f;
        float actual = vida != null ? Mathf.Clamp01(vida.Vida / max) : 0f;
        // vacia hasta que la camara se aleja para la pelea
        bool enPelea = camara != null && camara.Fijo;
        float objetivo = enPelea ? actual : 0f;
        float dt = Time.deltaTime;

        if (objetivo < relleno - 0.0001f)
        {
            // golpe: baja de una; lo perdido queda como fantasma
            fantasma = Mathf.Max(fantasma, relleno);
            relleno = objetivo;
            asentado = Mathf.Min(asentado, relleno);
            if (enPelea) ultimoGolpe = Time.time;
        }
        else if (objetivo > relleno)
        {
            // se llena como un liquido viscoso
            relleno = Mathf.MoveTowards(relleno, objetivo, velocidadLlenado * dt);
        }
        asentado = Mathf.MoveTowards(asentado, relleno, velocidadAsentado * dt);
        if (Time.time - ultimoGolpe > esperaFantasma) fantasma = Mathf.MoveTowards(fantasma, relleno, velocidadFantasma * dt);
        fantasma = Mathf.Max(fantasma, relleno);

        float destello = Mathf.Clamp01(1f - (Time.time - ultimoGolpe) / Mathf.Max(0.01f, segundosDestello));
        Aplicar(destello);

        // la gema se satura con la vida perdida (sin pelea, desaturada)
        if (gema != null)
        {
            gema.GetPropertyBlock(mpbGema);
            mpbGema.SetFloat(IdSaturacion, enPelea || (vida != null && !vida.Vivo) ? 1f - actual : 0f);
            gema.SetPropertyBlock(mpbGema);
        }

        // temblorcito al recibir un golpe
        float tiembla = Mathf.Clamp01(1f - (Time.time - ultimoGolpe) / Mathf.Max(0.01f, segundosTemblor));
        transform.localPosition = posicion + (Vector3)(Random.insideUnitCircle * temblor * tiembla);

        // protegido: los ornamentos titilan en violeta claro (suman luz)
        bool protegido = umbrae != null && umbrae.Protegido && enPelea;
        float k = protegido ? 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(Time.time * 9f)) : 0f;
        alfaBrillo = Mathf.MoveTowards(alfaBrillo, k * 0.45f, dt * 6f);
        var cb = colorProtegido; cb.a = alfaBrillo;
        foreach (var b in brillos)
            if (b != null) { b.GetPropertyBlock(mpbBrillo); mpbBrillo.SetColor(IdColor, cb); b.SetPropertyBlock(mpbBrillo); }
    }

    private void Aplicar(float destello)
    {
        if (pista == null) return;
        pista.GetPropertyBlock(mpbPista);
        mpbPista.SetFloat(IdRelleno, relleno);
        mpbPista.SetFloat(IdAsentado, asentado);
        mpbPista.SetFloat(IdFantasma, fantasma);
        mpbPista.SetFloat(IdDestello, destello);
        pista.SetPropertyBlock(mpbPista);
    }
}

