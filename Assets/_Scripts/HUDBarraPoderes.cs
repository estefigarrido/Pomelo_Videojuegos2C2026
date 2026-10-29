using UnityEngine;
using UnityEngine.UI;

// HUD de arriba a la derecha (barra de poderes del Figma).
// Los 3 lugares de la izquierda guardan las piezas de la estatua que sueltan los troncos al morir
// (brazo, pecho y peineta): empiezan vacios y cada pieza aparece con un latido cuando llega volando.
public class HUDBarraPoderes : MonoBehaviour
{
    [Tooltip("La pieza de la estatua de cada lugar (brazo, pecho y peineta).")]
    [SerializeField] private Image[] piezas;
    [Tooltip("Cuanto se agranda la pieza al llegar (1.35 = 35% mas grande) y cuanto tarda en volver.")]
    [SerializeField] private float latido = 1.35f;
    [SerializeField] private float duracionLatido = 0.3f;

    private float[] inicioLatido;

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
    }
}
