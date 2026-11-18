using UnityEngine;

// Los puntitos de luz de la cueva (pantalla de inicio). Cada uno se dibuja como en el SVG de Caro
// (inicio_17_particulas): un halo verdoso (#dfe6cf al 50 %, desenfocado) con un centro claro (#f4f7ec) de la mitad
// del tamano. Se dibujan uno por uno al arrancar (antes se recortaban de la capa y, como hay puntitos muy juntos,
// cada recorte se llevaba un pedazo del vecino y se veian cortados).
// Cada uno flota en su lugar y titila como una estrellita (se apaga y se prende suave y, cada tanto, destella).
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class Destellos : MonoBehaviour
{
    [Tooltip("x, y = lugar (relativo a este objeto); z = radio del halo en px de la escena (el r del SVG).")]
    [SerializeField] private Vector3[] puntos;
    [Tooltip("Desenfoque del halo, en px de la escena (en el SVG: 1.6).")]
    [SerializeField] private float desenfoque = 4.7f;
    [SerializeField] private float pixelesPorUnidad = 100f;
    [SerializeField] private int ordenDibujo = -16;

    [Header("Movimiento")]
    [Tooltip("Cuanto se alejan de su lugar (unidades).")]
    [SerializeField] private float vaiven = 0.3f;            // (Caro: que el movimiento se note mas; antes 0.12)
    [SerializeField] private float velocidadVaiven = 0.5f;

    [Header("Titilar")]
    [Tooltip("Lo mas apagado que se ponen (1 = no se apagan nunca).")]
    [Range(0f, 1f)]
    [SerializeField] private float minimo = 0.12f;
    [SerializeField] private float velocidadTitilar = 1.8f;
    [Tooltip("Cada cuantos segundos destella cada uno, mas o menos (un brillo mas fuerte y un poquito mas grande).")]
    [SerializeField] private float cadaCuantoDestella = 2.5f;
    [SerializeField] private float duracionDestello = 0.6f;
    [SerializeField] private float agrandaAlDestellar = 0.7f;

    private static readonly Color Halo = new Color32(0xdf, 0xe6, 0xcf, 255);
    private static readonly Color Centro = new Color32(0xf4, 0xf7, 0xec, 255);

    private Transform[] puntitos;
    private SpriteRenderer[] dibujos;
    private float[] fase1, fase2, fase3, velocidad, proximo, inicioDestello;

    private void Start()
    {
        int n = puntos != null ? puntos.Length : 0;
        puntitos = new Transform[n];
        dibujos = new SpriteRenderer[n];
        fase1 = new float[n]; fase2 = new float[n]; fase3 = new float[n]; velocidad = new float[n];
        proximo = new float[n]; inicioDestello = new float[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Destello " + i);
            go.transform.SetParent(transform, false);
            // relativos a este objeto: si se lo mueve, se mueven todos los puntitos
            go.transform.localPosition = new Vector3(puntos[i].x, puntos[i].y, 0f);
            puntitos[i] = go.transform;
            dibujos[i] = go.AddComponent<SpriteRenderer>();
            dibujos[i].sprite = Dibujar(puntos[i].z);
            dibujos[i].sortingOrder = ordenDibujo;

            fase1[i] = Random.value * 100f;
            fase2[i] = Random.value * 100f;
            fase3[i] = Random.value * Mathf.PI * 2f;
            velocidad[i] = velocidadTitilar * Random.Range(0.6f, 1.4f);
            proximo[i] = Time.time + Random.Range(0.3f, cadaCuantoDestella * 1.5f);
            inicioDestello[i] = -10f;
        }
    }

    // un puntito como el del SVG: halo de radio r desenfocado + centro de radio r/2, con todo su borde transparente
    private Sprite Dibujar(float r)
    {
        float s = Mathf.Max(0.5f, desenfoque);
        int lado = Mathf.CeilToInt(2f * (r + 4f * s)) + 2;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[lado * lado];
        float c = lado / 2f;
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                // circulo desenfocado (aproximacion del desenfoque gaussiano del borde)
                float halo = 0.5f / (1f + Mathf.Exp(1.702f * (d - r) / s));
                // centro nitido, con el borde suavizado un pixel
                float centro = Mathf.Clamp01(r / 2f + 0.5f - d);
                float a = centro + halo * (1f - centro);
                Color col = a > 0f ? (Centro * centro + Halo * halo * (1f - centro)) / a : Halo;
                col.a = a;
                px[y * lado + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f), pixelesPorUnidad, 0, SpriteMeshType.FullRect);
    }

    private void Update()
    {
        if (puntitos == null) return;
        float t = Time.time;
        for (int i = 0; i < puntitos.Length; i++)
        {
            // flota en su lugar (ruido suave, distinto para cada uno)
            float dx = (Mathf.PerlinNoise(fase1[i], t * velocidadVaiven) - 0.5f) * 2f * vaiven;
            float dy = (Mathf.PerlinNoise(fase2[i], t * velocidadVaiven) - 0.5f) * 2f * vaiven;
            puntitos[i].localPosition = new Vector3(puntos[i].x + dx, puntos[i].y + dy, 0f);

            // titila: se apaga y se prende suave, cada uno a su ritmo
            float titilar = 0.5f + 0.5f * Mathf.Sin(t * velocidad[i] + fase3[i]);

            // cada tanto destella: brilla del todo y crece un poquito
            if (t >= proximo[i])
            {
                inicioDestello[i] = t;
                proximo[i] = t + cadaCuantoDestella * Random.Range(0.6f, 1.6f);
            }
            float k = (t - inicioDestello[i]) / duracionDestello;
            float destello = k >= 0f && k <= 1f ? Mathf.Sin(k * Mathf.PI) : 0f;

            float alfa = Mathf.Lerp(minimo, 1f, Mathf.Max(titilar * 0.8f, destello));
            dibujos[i].color = new Color(1f, 1f, 1f, alfa);
            puntitos[i].localScale = Vector3.one * (1f + agrandaAlDestellar * destello);
        }
    }

    private void OnDestroy()
    {
        if (dibujos == null) return;
        foreach (var d in dibujos)
        {
            if (d == null || d.sprite == null) continue;
            Destroy(d.sprite.texture);
            Destroy(d.sprite);
        }
    }
}
