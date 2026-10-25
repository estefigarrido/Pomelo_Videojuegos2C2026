using UnityEngine;
using UnityEngine.UI;

// HUD de arriba a la izquierda (componente "contenedor_vida_sabiduria" del Figma).
// Vidas: 5 iconos que valen 20% de la vida cada uno; los que faltan quedan apagados (al 20% de opacidad).
// Sabiduria: el liquido del circulo. Es una ola blanca que sube y baja adentro de una mascara circular,
// con un brillo dorado alrededor que sale de la forma del liquido (hay una imagen cada 10%: se mezclan las dos mas cercanas).
public class HUDVidaSabiduria : MonoBehaviour
{
    // la imagen de la ola tiene 30 px (del Figma) de bruma arriba de la cresta
    private const float BrumaSobreLaOla = 30f;

    [Header("Vidas")]
    [SerializeField] private Image[] vidas;
    [SerializeField] private Sprite vidaPrendida;
    [SerializeField] private Sprite vidaApagada;
    [Tooltip("Cuanto se agranda un icono cuando cambia (1.3 = 30% mas grande) y cuanto tarda en volver.")]
    [SerializeField] private float latido = 1.3f;
    [SerializeField] private float duracionLatido = 0.25f;

    [Header("Liquido de sabiduria")]
    [Tooltip("La ola, hija de la mascara circular.")]
    [SerializeField] private RectTransform liquido;
    [Tooltip("Donde queda la cresta de la ola con el circulo vacio y lleno (px del Figma desde arriba del circulo).")]
    [SerializeField] private float crestaVacio = 124f;
    [SerializeField] private float crestaLleno = -15f;
    [Tooltip("Segundos (aprox.) que tarda el liquido en llegar a su nivel nuevo.")]
    [SerializeField] private float suavizado = 0.35f;
    [Tooltip("Vaiven de la ola: px que se mueve de costado y para arriba y abajo.")]
    [SerializeField] private float vaivenCostado = 5f;
    [SerializeField] private float vaivenAlto = 1.5f;
    [SerializeField] private float velocidadVaiven = 1.2f;

    [Header("Brillo del liquido")]
    [SerializeField] private Image brilloA;
    [SerializeField] private Image brilloB;
    [Tooltip("Brillo para 0%, 10%, ..., 100% de liquido (11 imagenes).")]
    [SerializeField] private Sprite[] brillos;

    private SaludPersonaje salud;
    private Image imagenLiquido;
    private float xLiquido;
    private float nivel;              // 0..1, lo que se ve (sigue a la sabiduria de a poco)
    private float velocidadNivel;
    private int vidasPrendidas = -1;
    private float[] inicioLatido;

    public static HUDVidaSabiduria Instancia { get; private set; }

    private void Awake() => Instancia = this;

    // Centro del circulo de sabiduria en pantalla (ahi vuelan los pedacitos de los mobs que mueren).
    public Vector2 PuntoDelCirculoEnPantalla()
    {
        var circulo = liquido.parent as RectTransform;
        var canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.WorldToScreenPoint(cam, circulo.TransformPoint(circulo.rect.center));
    }

    private void Start()
    {
        salud = FindFirstObjectByType<SaludPersonaje>();
        imagenLiquido = liquido.GetComponent<Image>();
        xLiquido = liquido.anchoredPosition.x;
        inicioLatido = new float[vidas.Length];
        for (int i = 0; i < inicioLatido.Length; i++) inicioLatido[i] = -99f;
        if (salud != null) nivel = salud.Sabiduria / salud.SabiduriaMaxima;
    }

    private void Update()
    {
        if (salud == null) return;

        ActualizarVidas();
        ActualizarLiquido();
    }

    private void ActualizarVidas()
    {
        int prendidas = Mathf.CeilToInt(salud.Vida / salud.VidaMaxima * vidas.Length - 0.001f);
        if (prendidas != vidasPrendidas)
        {
            for (int i = 0; i < vidas.Length; i++)
            {
                bool prendida = i < prendidas;
                // late el icono que cambio (no al arrancar)
                if (vidasPrendidas >= 0 && prendida != (i < vidasPrendidas)) inicioLatido[i] = Time.time;
                vidas[i].sprite = prendida ? vidaPrendida : vidaApagada;
            }
            vidasPrendidas = prendidas;
        }

        for (int i = 0; i < vidas.Length; i++)
        {
            float t = (Time.time - inicioLatido[i]) / duracionLatido;
            float escala = t < 1f ? Mathf.Lerp(latido, 1f, t * t * (3f - 2f * t)) : 1f;
            vidas[i].rectTransform.localScale = new Vector3(escala, escala, 1f);
        }
    }

    private void ActualizarLiquido()
    {
        float objetivo = Mathf.Clamp01(salud.Sabiduria / salud.SabiduriaMaxima);
        nivel = Mathf.SmoothDamp(nivel, objetivo, ref velocidadNivel, suavizado);
        if (Mathf.Abs(nivel - objetivo) < 0.0005f) nivel = objetivo;

        bool hayLiquido = nivel > 0.002f;
        if (imagenLiquido.enabled != hayLiquido) imagenLiquido.enabled = hayLiquido;

        float t = Time.time * velocidadVaiven;
        float cresta = Mathf.Lerp(crestaVacio, crestaLleno, nivel);
        liquido.anchoredPosition = new Vector2(xLiquido + Mathf.Sin(t) * vaivenCostado,
                                               -(cresta - BrumaSobreLaOla) + Mathf.Sin(t * 1.7f) * vaivenAlto);

        // brillo: mezcla de las dos imagenes mas cercanas al nivel
        float b = nivel * (brillos.Length - 1);
        int i0 = Mathf.Min(Mathf.FloorToInt(b), brillos.Length - 2);
        float k = b - i0;
        if (brilloA.sprite != brillos[i0]) brilloA.sprite = brillos[i0];
        if (brilloB.sprite != brillos[i0 + 1]) brilloB.sprite = brillos[i0 + 1];
        brilloA.color = new Color(1f, 1f, 1f, 1f - k);
        brilloB.color = new Color(1f, 1f, 1f, k);
    }
}
