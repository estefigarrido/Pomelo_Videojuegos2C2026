using UnityEngine;

// Los puntitos de luz de la cueva (pantalla de inicio). Son los ORIGINALES del dibujo de Caro: cada uno se recorta
// de la capa inicio_17_particulas y queda suelto, para que flote apenas en su lugar y titile como una estrellita
// (se apaga y se prende suave y, cada tanto, destella un poco mas fuerte).
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class Destellos : MonoBehaviour
{
    [Tooltip("La capa de los puntitos (inicio_17_particulas).")]
    [SerializeField] private Texture2D textura;
    [Tooltip("Donde esta cada puntito dentro de la capa (px, desde abajo a la izquierda).")]
    [SerializeField] private Rect[] recortes;
    [Tooltip("El centro de cada puntito dentro de su recorte (0 a 1).")]
    [SerializeField] private Vector2[] centros;
    [Tooltip("El lugar de cada puntito en el mundo.")]
    [SerializeField] private Vector2[] lugares;
    [SerializeField] private float pixelesPorUnidad = 100f;
    [SerializeField] private int ordenDibujo = -16;

    [Header("Movimiento")]
    [Tooltip("Cuanto se alejan de su lugar (unidades).")]
    [SerializeField] private float vaiven = 0.12f;
    [SerializeField] private float velocidadVaiven = 0.35f;

    [Header("Titilar")]
    [Tooltip("Lo mas apagado que se ponen (1 = no se apagan nunca).")]
    [Range(0f, 1f)]
    [SerializeField] private float minimo = 0.25f;
    [SerializeField] private float velocidadTitilar = 1.4f;
    [Tooltip("Cada cuantos segundos destella cada uno, mas o menos (un brillo mas fuerte y un poquito mas grande).")]
    [SerializeField] private float cadaCuantoDestella = 4f;
    [SerializeField] private float duracionDestello = 0.5f;
    [SerializeField] private float agrandaAlDestellar = 0.35f;

    private Transform[] puntitos;
    private SpriteRenderer[] dibujos;
    private float[] fase1, fase2, fase3, velocidad, proximo, inicioDestello;

    private void Start()
    {
        int n = textura != null && recortes != null ? Mathf.Min(recortes.Length, Mathf.Min(centros.Length, lugares.Length)) : 0;
        puntitos = new Transform[n];
        dibujos = new SpriteRenderer[n];
        fase1 = new float[n]; fase2 = new float[n]; fase3 = new float[n]; velocidad = new float[n];
        proximo = new float[n]; inicioDestello = new float[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Destello " + i);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(lugares[i].x, lugares[i].y, 0f);
            puntitos[i] = go.transform;
            dibujos[i] = go.AddComponent<SpriteRenderer>();
            dibujos[i].sprite = Sprite.Create(textura, recortes[i], centros[i], pixelesPorUnidad, 0, SpriteMeshType.FullRect);
            dibujos[i].sortingOrder = ordenDibujo;

            fase1[i] = Random.value * 100f;
            fase2[i] = Random.value * 100f;
            fase3[i] = Random.value * Mathf.PI * 2f;
            velocidad[i] = velocidadTitilar * Random.Range(0.6f, 1.4f);
            proximo[i] = Time.time + Random.Range(0.3f, cadaCuantoDestella * 1.5f);
            inicioDestello[i] = -10f;
        }
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
            puntitos[i].position = new Vector3(lugares[i].x + dx, lugares[i].y + dy, 0f);

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
        foreach (var d in dibujos) if (d != null && d.sprite != null) Destroy(d.sprite);
    }
}
