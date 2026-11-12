using UnityEngine;

// Drop shadow violeta y suave de la barra de vida de Umbrae (en el Figma: violeta 7340A2, sin corrimiento).
// Pone detras de cada pieza una copia con el shader "Pomelo/Sprite Sombra Exterior" (desenfoque por mipmaps:
// los PNG de la barra tienen mipmaps y malla FullRect). Todo se ajusta desde aca, tambien jugando.
public class SombraBarraUmbrae : MonoBehaviour
{
    [Tooltip("Material con el shader 'Pomelo/Sprite Sombra Exterior' (SombraBarraUmbrae.mat).")]
    [SerializeField] private Material material;
    [Tooltip("Piezas que llevan sombra.")]
    [SerializeField] private SpriteRenderer[] piezas;

    [Header("Sombra")]
    [SerializeField] private Color color = new Color(0.4485f, 0.2506f, 0.6337f, 1f);
    [Range(0f, 1f)] [SerializeField] private float opacidad = 0.35f;
    [Tooltip("Desenfoque en px del Figma (en el Figma: 60).")]
    [SerializeField] private float desenfoque = 60f;
    [Tooltip("Corrimiento vertical en px del Figma (positivo = la sombra baja).")]
    [SerializeField] private float corrimientoY = 0f;
    [Tooltip("Orden de dibujo de las sombras (debajo de la pista, que esta en 3).")]
    [SerializeField] private int orden = 1;

    [Header("Escala de los PNG")]
    [SerializeField] private float pixelesPorUnidad = 75f;
    [Tooltip("Px de los PNG por px del Figma (se exportaron a 1456/852.62).")]
    [SerializeField] private float pxPorPxFigma = 1.7077f;
    [Tooltip("Cuanto se agranda el dibujo de la sombra para que no se corte (texeles).")]
    [SerializeField] private float margen = 80f;

    private SpriteRenderer[] sombras;
    private MaterialPropertyBlock mpb;

    private static readonly int IdA = Shader.PropertyToID("_SombraA");
    private static readonly int IdAOpacidad = Shader.PropertyToID("_SombraAOpacidad");
    private static readonly int IdABlur = Shader.PropertyToID("_SombraABlur");
    private static readonly int IdAOffset = Shader.PropertyToID("_SombraAOffsetY");
    private static readonly int IdBOpacidad = Shader.PropertyToID("_SombraBOpacidad");
    private static readonly int IdPPU = Shader.PropertyToID("_PPU");
    private static readonly int IdMargen = Shader.PropertyToID("_Margen");
    private static readonly int IdEscala = Shader.PropertyToID("_EscalaFigma");
    private static readonly int IdAlfa = Shader.PropertyToID("_Alfa");

    private void Start()
    {
        mpb = new MaterialPropertyBlock();
        if (material == null || piezas == null) return;
        sombras = new SpriteRenderer[piezas.Length];
        for (int i = 0; i < piezas.Length; i++)
        {
            var p = piezas[i];
            if (p == null) continue;
            var go = new GameObject(p.name + " (sombra)");
            go.transform.SetParent(p.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sortingLayerID = p.sortingLayerID;
            sombras[i] = sr;
        }
        LateUpdate();
    }

    // cada cuadro: sigue el dibujo de su pieza (y si esta prendida) y toma los valores de arriba
    private void LateUpdate()
    {
        if (sombras == null) return;
        for (int i = 0; i < sombras.Length; i++)
        {
            var s = sombras[i];
            var p = piezas[i];
            if (s == null || p == null) continue;
            s.enabled = p.enabled && p.gameObject.activeInHierarchy && p.sprite != null && opacidad > 0f;
            if (!s.enabled) continue;
            s.sprite = p.sprite;
            s.flipX = p.flipX;
            s.sortingOrder = orden;
            s.GetPropertyBlock(mpb);
            mpb.SetColor(IdA, new Color(color.r, color.g, color.b, 1f));
            mpb.SetFloat(IdAOpacidad, opacidad * color.a);
            mpb.SetFloat(IdABlur, desenfoque);
            mpb.SetFloat(IdAOffset, corrimientoY);
            mpb.SetFloat(IdBOpacidad, 0f);
            mpb.SetFloat(IdPPU, pixelesPorUnidad);
            mpb.SetFloat(IdMargen, margen);
            mpb.SetFloat(IdEscala, pxPorPxFigma);
            mpb.SetFloat(IdAlfa, p.color.a);
            s.SetPropertyBlock(mpb);
        }
    }
}
