using UnityEngine;
using UnityEngine.UI;

// Loader de tres puntos (estilo "DOTS"): los puntos laten uno detras del otro (crecen y se aclaran y vuelven).
// Va en la pantalla "Ingresaste al Templo de botanica" (EntradaTemplo), debajo del ultimo texto: aparece y se va con
// la pantalla. Usa el tiempo real, asi sigue andando aunque el juego este en pausa o cargando el nivel.
// Los puntos son Images hijas; el circulo se dibuja solo al arrancar (no hace falta una imagen).
public class PuntosCargando : MonoBehaviour
{
    [SerializeField] private Image[] puntos;
    [Tooltip("Segundos de una vuelta completa (los tres puntos).")]
    [SerializeField] private float periodo = 1.2f;
    [Tooltip("Tamano de cada punto cuando esta 'apagado' (1 = su tamano entero).")]
    [Range(0f, 1f)] [SerializeField] private float escalaMinima = 0.55f;
    [Range(0f, 1f)] [SerializeField] private float opacidadMinima = 0.35f;

    private void Awake()
    {
        var circulo = Circulo();
        if (puntos != null) foreach (var p in puntos) if (p != null) p.sprite = circulo;
    }

    private void Update()
    {
        if (puntos == null || puntos.Length == 0) return;
        float t = Time.unscaledTime / Mathf.Max(0.05f, periodo);
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == null) continue;
            // cada punto late en su tercio de la vuelta (una campana suave), uno detras del otro
            float fase = Mathf.Repeat(t - i / (float)(puntos.Length + 1), 1f);
            float k = fase < 0.6f ? 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * fase / 0.6f) : 0f;
            puntos[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(escalaMinima, 1f, k);
            var c = puntos[i].color; c.a = Mathf.Lerp(opacidadMinima, 1f, k); puntos[i].color = c;
        }
    }

    private static Sprite circulo;
    private static Sprite Circulo()
    {
        if (circulo != null) return circulo;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(n / 2f - d));   // borde suave de 1 px
            }
        tex.SetPixels(px);
        tex.Apply();
        circulo = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return circulo;
    }
}
