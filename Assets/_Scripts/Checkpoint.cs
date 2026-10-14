using UnityEngine;

// Checkpoint consumible: flota, brilla, y al tocarlo la chica guarda este lugar
// como punto de reaparicion y el icono desaparece (como una moneda de Mario).
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Flotacion")]
    [Tooltip("Cuanto sube y baja, en unidades.")]
    [SerializeField] private float alturaFlotacion = 0.12f;
    [Tooltip("Segundos que tarda en subir y bajar una vez.")]
    [SerializeField] private float periodoFlotacion = 1.6f;

    [Header("Brillo")]
    [SerializeField] private Color colorBrillo = new Color(1f, 0.93f, 0.75f, 1f);
    [Tooltip("Tamano del halo respecto del icono.")]
    [SerializeField] private float tamanoBrillo = 2.4f;
    [SerializeField] private float brilloMinimo = 0.25f;
    [SerializeField] private float brilloMaximo = 0.6f;

    [Header("Al tomarlo")]
    [SerializeField] private float duracionDesaparecer = 0.25f;

    private static Sprite spriteBrillo;

    private Vector3 posicionBase;
    private Vector3 escalaBase;
    private float fase;
    private SpriteRenderer dibujo;
    private SpriteRenderer brillo;
    private bool tomado;
    private float inicioDesaparecer;

    private void Awake()
    {
        posicionBase = transform.position;
        escalaBase = transform.localScale;
        fase = Random.value * Mathf.PI * 2f;
        dibujo = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        CrearBrillo();
    }

    private void Update()
    {
        if (tomado)
        {
            float t = Mathf.Clamp01((Time.time - inicioDesaparecer) / duracionDesaparecer);
            transform.localScale = escalaBase * (1f + 0.6f * t);
            SetAlfa(1f - t);
            if (t >= 1f) gameObject.SetActive(false);
            return;
        }

        float onda = Mathf.Sin(Time.time * Mathf.PI * 2f / periodoFlotacion + fase);
        transform.position = posicionBase + Vector3.up * onda * alturaFlotacion;
        if (brillo != null)
        {
            var c = colorBrillo;
            c.a = Mathf.Lerp(brilloMinimo, brilloMaximo, (onda + 1f) * 0.5f);
            brillo.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (tomado) return;
        var salud = otro.GetComponentInParent<SaludPersonaje>();
        if (salud == null) return;

        tomado = true;
        inicioDesaparecer = Time.time;
        GetComponent<Collider2D>().enabled = false;
        salud.DefinirPuntoDeReaparicion(posicionBase);
    }

    private void SetAlfa(float a)
    {
        if (dibujo != null) { var c = dibujo.color; c.a = a; dibujo.color = c; }
        if (brillo != null) { var c = brillo.color; c.a = Mathf.Min(c.a, a * brilloMaximo); brillo.color = c; }
    }

    private void CrearBrillo()
    {
        if (dibujo == null || dibujo.sprite == null) return;
        if (spriteBrillo == null) spriteBrillo = GenerarHalo(128);

        var go = new GameObject("Brillo");
        go.transform.SetParent(transform, false);
        brillo = go.AddComponent<SpriteRenderer>();
        brillo.sprite = spriteBrillo;
        brillo.sharedMaterial = dibujo.sharedMaterial;
        brillo.sortingLayerID = dibujo.sortingLayerID;
        brillo.sortingOrder = dibujo.sortingOrder - 1;

        // el halo mide tamanoBrillo veces el icono (en espacio local del icono)
        float lado = Mathf.Max(dibujo.sprite.bounds.size.x, dibujo.sprite.bounds.size.y) * tamanoBrillo;
        float ladoHalo = spriteBrillo.bounds.size.x;
        go.transform.localScale = Vector3.one * (lado / ladoHalo);
        go.transform.localPosition = dibujo.sprite.bounds.center;
    }

    private static Sprite GenerarHalo(int tam)
    {
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[tam * tam];
        float medio = (tam - 1) / 2f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float d = Mathf.Sqrt((x - medio) * (x - medio) + (y - medio) * (y - medio)) / medio;
                float a = Mathf.Clamp01(1f - d);
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }
}
