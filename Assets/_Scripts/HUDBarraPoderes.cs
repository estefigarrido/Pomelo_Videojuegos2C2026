using UnityEngine;
using UnityEngine.UI;

// HUD de arriba a la derecha (barra de poderes del Figma).
// Los 3 lugares de la izquierda guardan las piezas de la estatua que sueltan los troncos al morir
// (brazo, pecho y peineta): empiezan vacios y cada pieza aparece con un latido cuando llega volando.
// El circulo principal muestra el golpe elegido (latigo o sabiduria, se cambia con B): al cambiar,
// los iconos se cruzan rapido y el circulo se ilumina un poco.
// Los puntitos verdes de arriba del circulo de la hoja (selector_poder_A / B) marcan cual de los dos
// golpes esta elegido: al cambiar, un dibujo se funde con el otro y el nuevo destella un instante.
public class HUDBarraPoderes : MonoBehaviour
{
    [Tooltip("La pieza de la estatua de cada lugar (brazo, pecho y peineta).")]
    [SerializeField] private Image[] piezas;
    [Tooltip("Cuanto se agranda la pieza al llegar (1.35 = 35% mas grande) y cuanto tarda en volver.")]
    [SerializeField] private float latido = 1.35f;
    [SerializeField] private float duracionLatido = 0.3f;
    [Tooltip("Sombra de cada pieza (su silueta desenfocada en #252525, estatua_*_HUD_sombra): va detras, centrada (X 0, Y 0).")]
    [SerializeField] private Sprite[] sombrasPiezas;
    [Range(0f, 1f)] [SerializeField] private float opacidadSombra = 0.3f;
    [Tooltip("Margen que tiene la imagen de la sombra a cada lado respecto de la pieza (px de la imagen).")]
    [SerializeField] private float margenSombraPx = 14f;
    private Image[] sombras;

    [Header("Golpe elegido (circulo principal)")]
    [Tooltip("Icono de cada golpe, en el orden de SelectorAtaque.Ataque (latigo, sabiduria).")]
    [SerializeField] private CanvasGroup[] iconosGolpe;
    [Tooltip("Brillo suave sobre el circulo que aparece y se apaga al cambiar de golpe.")]
    [SerializeField] private Image brilloCirculo;
    [SerializeField] private Color colorBrillo = new Color(1f, 0.93f, 0.62f, 0.5f);
    [Tooltip("Segundos que tardan los iconos en cruzarse.")]
    [SerializeField] private float segundosCambio = 0.12f;
    [SerializeField] private float segundosBrillo = 0.35f;

    [Header("Selector de poder (los puntitos verdes)")]
    [Tooltip("Lo pone el menu Pomelo -> Poner el selector de poder.")]
    [SerializeField] private Image selectorPoder;
    [Tooltip("selector_poder_A y selector_poder_B, en el orden de SelectorAtaque.Ataque (latigo, sabiduria).")]
    [SerializeField] private Sprite[] spritesSelector;
    [Tooltip("Color del destello al cambiar (se mezcla con el verde del puntito).")]
    [SerializeField] private Color colorBrilloSelector = new Color(1f, 1f, 0.8f, 0.9f);
    [SerializeField] private float segundosBrilloSelector = 0.4f;
    [Tooltip("Segundos que tarda un dibujo en fundirse con el otro.")]
    [SerializeField] private float segundosFundidoSelector = 0.25f;

    private Image selectorAnterior;
    private Image brilloSelector;
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
        CrearSombras();
    }

    // la sombra es un hermano de la pieza, justo antes en la jerarquia (asi se dibuja detras), del mismo lugar y
    // un poco mas grande por el margen del desenfoque
    private void CrearSombras()
    {
        sombras = new Image[piezas.Length];
        if (sombrasPiezas == null) return;
        for (int i = 0; i < piezas.Length && i < sombrasPiezas.Length; i++)
        {
            if (sombrasPiezas[i] == null || piezas[i] == null || piezas[i].sprite == null) continue;
            var pieza = piezas[i].rectTransform;
            var go = new GameObject("sombra " + piezas[i].name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(pieza.parent, false);
            rt.SetSiblingIndex(pieza.GetSiblingIndex());
            var tam = piezas[i].sprite.rect.size;
            float fx = (tam.x + margenSombraPx * 2f) / tam.x, fy = (tam.y + margenSombraPx * 2f) / tam.y;
            // centrada en la pieza (X 0, Y 0) y mas grande por el margen del desenfoque
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = pieza.localRotation;
            rt.sizeDelta = new Vector2(pieza.rect.width * fx, pieza.rect.height * fy);
            rt.position = pieza.TransformPoint(pieza.rect.center);
            var img = go.GetComponent<Image>();
            img.sprite = sombrasPiezas[i];
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, opacidadSombra);
            img.enabled = false;
            sombras[i] = img;
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
        if (selectorPoder != null)
        {
            selectorAnterior = CopiaDelSelector("Selector anterior");
            brilloSelector = CopiaDelSelector("Brillo selector");
        }
        MostrarSelector();
    }

    private void CambiarGolpe(SelectorAtaque.Ataque elegido)
    {
        if (selectorAnterior != null && selectorPoder != null) selectorAnterior.sprite = selectorPoder.sprite;
        golpe = (int)elegido;
        inicioCambio = Time.time;
        MostrarSelector();
    }

    private void MostrarSelector()
    {
        if (selectorPoder == null || spritesSelector == null || golpe >= spritesSelector.Length) return;
        selectorPoder.sprite = spritesSelector[golpe];
        if (brilloSelector != null) brilloSelector.sprite = selectorPoder.sprite;
    }

    // copia del puntito encima de el: una muestra el dibujo que se va y la otra el destello del nuevo
    private Image CopiaDelSelector(string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(selectorPoder.rectTransform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var imagen = go.GetComponent<Image>();
        imagen.raycastTarget = false;
        imagen.color = Color.clear;
        return imagen;
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
        if (sombras != null && sombras[lugar] != null) sombras[lugar].enabled = true;
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
            if (sombras != null && sombras[i] != null) sombras[i].rectTransform.localScale = new Vector3(escala, escala, 1f);
        }
        ActualizarGolpe();
        ActualizarSelector();
    }

    private void ActualizarSelector()
    {
        if (selectorPoder == null) return;
        float transcurrido = Time.time - inicioCambio;

        // el dibujo nuevo aparece mientras el anterior se apaga
        float f = Mathf.Clamp01(transcurrido / Mathf.Max(0.01f, segundosFundidoSelector));
        f = f * f * (3f - 2f * f);
        selectorPoder.color = new Color(1f, 1f, 1f, f);
        if (selectorAnterior != null) selectorAnterior.color = new Color(1f, 1f, 1f, 1f - f);

        // el destello del nuevo se prende rapido y se apaga despacio, sin cambiar de tamano
        if (brilloSelector == null) return;
        float t = transcurrido / Mathf.Max(0.01f, segundosBrilloSelector);
        float k = t < 1f ? Mathf.Sin(Mathf.Sqrt(t) * Mathf.PI) : 0f;
        var c = colorBrilloSelector;
        c.a *= k;
        brilloSelector.color = c;
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
