using UnityEngine;

// Levitacion del boss: flota un poquito sobre el piso y sube y baja suave (idle).
// Una sombra en el piso se achica y aclara cuando sube, para que se lea como flotar a proposito.
public class LevitacionBoss : MonoBehaviour
{
    [Tooltip("Altura a la que flota sobre el piso (unidades; 1 = 100 px).")]
    [SerializeField] private float altura = 0.22f;
    [Tooltip("Cuanto sube y baja en el idle.")]
    [SerializeField] private float amplitud = 0.08f;
    [Tooltip("Segundos que tarda en subir y bajar una vez.")]
    [SerializeField] private float periodo = 2.6f;

    [Header("Sombra en el piso")]
    [SerializeField] private float anchoSombra = 1.3f;
    [SerializeField] private float opacidadSombra = 0.35f;

    private Vector3 posicionBase;
    private Transform sombra;
    private SpriteRenderer dibujoSombra;

    // Punto del piso sobre el que flota. Otro script (UmbraeBoss) lo mueve al escapar o teletransportarse.
    public Vector3 PosicionBase
    {
        get => posicionBase;
        set => posicionBase = value;
    }

    // 1 = sombra normal, 0 = sin sombra (por ejemplo mientras desaparece en el teleport)
    public float VisibilidadSombra { get; set; } = 1f;

    private void Awake()
    {
        posicionBase = transform.position;
    }

    private void Start()
    {
        CrearSombra();
    }

    private void Update()
    {
        float onda = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, periodo));
        float y = altura + onda * amplitud;
        transform.position = posicionBase + new Vector3(0f, y, 0f);

        if (sombra == null) return;
        // la sombra queda pegada al piso y reacciona a la altura
        sombra.position = posicionBase;
        float k = Mathf.InverseLerp(altura + amplitud, altura - amplitud, y); // 1 = mas cerca del piso
        sombra.localScale = new Vector3(anchoSombra * Mathf.Lerp(0.85f, 1f, k), anchoSombra * 0.18f * Mathf.Lerp(0.85f, 1f, k), 1f);
        dibujoSombra.color = new Color(0f, 0f, 0f, opacidadSombra * Mathf.Lerp(0.7f, 1f, k) * Mathf.Clamp01(VisibilidadSombra));
    }

    private void CrearSombra()
    {
        const int tam = 64;
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float dx = (x + 0.5f) / tam * 2f - 1f, dy = (y + 0.5f) / tam * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();

        var go = new GameObject("Sombra");
        sombra = go.transform;
        sombra.SetParent(transform, false);
        dibujoSombra = go.AddComponent<SpriteRenderer>();
        dibujoSombra.sprite = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam);
        var propio = GetComponent<SpriteRenderer>();
        if (propio != null)
        {
            dibujoSombra.sortingLayerID = propio.sortingLayerID;
            dibujoSombra.sortingOrder = propio.sortingOrder - 1;
            dibujoSombra.sharedMaterial = propio.sharedMaterial;
        }
    }
}
