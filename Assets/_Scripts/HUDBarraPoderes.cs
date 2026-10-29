using UnityEngine;
using UnityEngine.UI;

// HUD de arriba a la derecha (barra de poderes del Figma).
// Los 3 lugares de la izquierda guardan las piezas de la estatua que sueltan los troncos al morir
// (brazo, pecho y peineta): empiezan vacios y cada pieza aparece con un latido cuando llega volando.
// El circulo principal muestra el golpe elegido (latigo o sabiduria, se cambia con V): al cambiar,
// los iconos se cruzan rapido y el circulo se ilumina un poco.
public class HUDBarraPoderes : MonoBehaviour
{
    [Tooltip("La pieza de la estatua de cada lugar (brazo, pecho y peineta).")]
    [SerializeField] private Image[] piezas;
    [Tooltip("Cuanto se agranda la pieza al llegar (1.35 = 35% mas grande) y cuanto tarda en volver.")]
    [SerializeField] private float latido = 1.35f;
    [SerializeField] private float duracionLatido = 0.3f;

    [Header("Golpe elegido (circulo principal)")]
    [Tooltip("Icono de cada golpe, en el orden de SelectorAtaque.Ataque (latigo, sabiduria).")]
    [SerializeField] private CanvasGroup[] iconosGolpe;
    [Tooltip("Brillo suave sobre el circulo que aparece y se apaga al cambiar de golpe.")]
    [SerializeField] private Image brilloCirculo;
    [SerializeField] private Color colorBrillo = new Color(1f, 0.93f, 0.62f, 0.5f);
    [Tooltip("Segundos que tardan los iconos en cruzarse.")]
    [SerializeField] private float segundosCambio = 0.12f;
    [SerializeField] private float segundosBrillo = 0.35f;

    private float[] inicioLatido;
    private int golpe;
    private float inicioCambio = -99f;
    private Vector3[] escalaIconos;

    public static HUDBarraPoderes Instancia { get; private set; }

    private void Awake()
    {
        Instancia = this;
        inicioLatido = new float[piezas.Length];
        for (int i = 0; i < piezas.Length; i++)
        {
            inicioLatido[i] = -99f;
            piezas[i].enabled = false;
        }
    }

    private void OnEnable() => SelectorAtaque.Cambio += CambiarGolpe;
    private void OnDisable() => SelectorAtaque.Cambio -= CambiarGolpe;

    private void Start()
    {
        // arranca mostrando el golpe elegido, sin animacion
        golpe = (int)SelectorAtaque.Elegido;
        escalaIconos = new Vector3[iconosGolpe.Length];
        for (int i = 0; i < iconosGolpe.Length; i++)
        {
            escalaIconos[i] = iconosGolpe[i].transform.localScale;
            iconosGolpe[i].alpha = i == golpe ? 1f : 0f;
        }
        if (brilloCirculo != null) brilloCirculo.color = Color.clear;
    }

    private void CambiarGolpe(SelectorAtaque.Ataque elegido)
    {
        golpe = (int)elegido;
        inicioCambio = Time.time;
    }

    // Centro del lugar en pantalla (ahi vuela la pieza que suelta el tronco).
    public Vector2 PuntoEnPantalla(int lugar)
    {
        var rt = piezas[lugar].rectTransform;
        return RectTransformUtility.WorldToScreenPoint(CamaraDelCanvas(), rt.TransformPoint(rt.rect.center));
    }

    // Tamano del icono de la pieza en pantalla (px).
    public Vector2 TamanoEnPantalla(int lugar)
    {
        var esquinas = new Vector3[4];
        piezas[lugar].rectTransform.GetWorldCorners(esquinas);
        Camera cam = CamaraDelCanvas();
        Vector2 a = RectTransformUtility.WorldToScreenPoint(cam, esquinas[0]);
        Vector2 b = RectTransformUtility.WorldToScreenPoint(cam, esquinas[2]);
        return new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
    }

    public void Coleccionar(int lugar)
    {
        if (lugar < 0 || lugar >= piezas.Length) return;
        piezas[lugar].enabled = true;
        inicioLatido[lugar] = Time.time;
    }

    private Camera CamaraDelCanvas()
    {
        var canvas = GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private void Update()
    {
        for (int i = 0; i < piezas.Length; i++)
        {
            float t = (Time.time - inicioLatido[i]) / duracionLatido;
            float escala = t < 1f ? Mathf.Lerp(latido, 1f, t * t * (3f - 2f * t)) : 1f;
            piezas[i].rectTransform.localScale = new Vector3(escala, escala, 1f);
        }
        ActualizarGolpe();
    }

    private void ActualizarGolpe()
    {
        // el que sale se achica y se apaga; el que entra crece hasta su tamano
        for (int i = 0; i < iconosGolpe.Length; i++)
        {
            var icono = iconosGolpe[i];
            icono.alpha = Mathf.MoveTowards(icono.alpha, i == golpe ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, segundosCambio));
            float a = icono.alpha;
            float escala = Mathf.Lerp(0.75f, 1f, a * a * (3f - 2f * a));
            icono.transform.localScale = escalaIconos[i] * escala;
        }

        if (brilloCirculo == null) return;
        float t = (Time.time - inicioCambio) / Mathf.Max(0.01f, segundosBrillo);
        float k = t < 1f ? Mathf.Sin(t * Mathf.PI) : 0f;
        brilloCirculo.color = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, colorBrillo.a * k);
    }
}
