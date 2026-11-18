using UnityEngine;

// Las dos columnas grises caidas de la parte 2 de la pantalla de inicio (capa cascada_08_columnas_sueltas):
// cada una se recorta de la capa y queda suelta, flotando en el agua: sube y baja apenas y se mece un poquito,
// cada una a su ritmo. La base queda metida en el agua (el agua de adelante la arma el menu de la escena).
// Usa el tiempo del juego: en pausa se quedan quietas.
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class ColumnasFlotantes : MonoBehaviour
{
    [SerializeField] private Texture2D textura;
    [Tooltip("Donde esta cada columna dentro de la capa (px, desde abajo a la izquierda).")]
    [SerializeField] private Rect[] recortes;
    [Tooltip("El punto de cada recorte sobre el que se mece (la base, a la altura del agua), de 0 a 1.")]
    [SerializeField] private Vector2[] pivotes;
    [Tooltip("Donde queda ese punto en el mundo.")]
    [SerializeField] private Vector2[] lugares;
    [SerializeField] private float pixelesPorUnidad = 100f;
    [SerializeField] private int ordenDibujo;

    [Header("Flotar")]
    [Tooltip("Cuanto suben y bajan (unidades).")]
    [SerializeField] private float subeYBaja = 0.06f;
    [Tooltip("Cuanto se mecen (grados).")]
    [SerializeField] private float seMece = 0.7f;
    [Tooltip("Segundos de una subida y bajada.")]
    [SerializeField] private float periodo = 3.6f;

    private Transform[] columnas;
    private float[] fase;
    private float tiempo;

    private void Start()
    {
        int n = textura != null && recortes != null ? Mathf.Min(recortes.Length, Mathf.Min(pivotes.Length, lugares.Length)) : 0;
        columnas = new Transform[n];
        fase = new float[n];
        for (int i = 0; i < n; i++)
        {
            var go = new GameObject("Columna " + (i + 1));
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(lugares[i].x, lugares[i].y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(textura, recortes[i], pivotes[i], pixelesPorUnidad, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = ordenDibujo;
            columnas[i] = go.transform;
            fase[i] = i * 2.1f + Random.value;   // que no floten las dos al mismo tiempo
        }
    }

    private void Update()
    {
        if (columnas == null) return;
        tiempo += Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        float w = periodo > 0f ? Mathf.PI * 2f / periodo : 0f;
        for (int i = 0; i < columnas.Length; i++)
        {
            float a = tiempo * w + fase[i];
            columnas[i].position = new Vector3(lugares[i].x, lugares[i].y + Mathf.Sin(a) * subeYBaja, 0f);
            // se mece un poco desfasado de la subida, como un objeto que flota de verdad
            columnas[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(a * 0.8f + 1.3f) * seMece);
        }
    }

    private void OnDestroy()
    {
        if (columnas == null) return;
        foreach (var c in columnas)
            if (c != null && c.TryGetComponent<SpriteRenderer>(out var sr) && sr.sprite != null) Destroy(sr.sprite);
    }
}
