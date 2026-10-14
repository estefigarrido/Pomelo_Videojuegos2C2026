using UnityEngine;

// La gema verde que deja Umbrae al morir. Queda quieta un segundo despues de caer, despues flota
// de arriba a abajo (como los spawnpoints) con un brillo fuerte (destello_gema), y al tocarla la chica
// la agarra: hace un "pop" y desaparece. Avisa con el evento Recolectada (la camara de la pelea lo usa).
[RequireComponent(typeof(SpriteRenderer))]
public class GemaUmbrae : MonoBehaviour
{
    public static event System.Action Recolectada;

    [SerializeField] private float esperaAntesDeFlotar = 1f;
    [Header("Flotacion (igual que los spawnpoints)")]
    [SerializeField] private float alturaFlotacion = 0.12f;
    [SerializeField] private float periodoFlotacion = 1.6f;
    [Header("Brillo")]
    [SerializeField] private float brilloMinimo = 0.75f;
    [SerializeField] private float brilloMaximo = 1f;
    [Tooltip("Segundos que tarda en encenderse el brillo (termina justo cuando empieza a flotar).")]
    [SerializeField] private float duracionAparecerBrillo = 0.6f;
    [Header("Al agarrarla")]
    [SerializeField] private float radio = 0.45f;
    [SerializeField] private float duracionRecolectar = 0.25f;

    private SpriteRenderer dibujo;
    private SpriteRenderer brillo;
    private Vector3 posicionBase;
    private Vector3 escalaBase;
    private float inicio;
    private bool recolectada;
    private float inicioRecolectar;

    public void Iniciar(Sprite spriteBrillo, Material materialBrillo)
    {
        dibujo = GetComponent<SpriteRenderer>();
        posicionBase = transform.position;
        escalaBase = transform.localScale;
        inicio = Time.time;

        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radio;

        if (spriteBrillo != null)
        {
            var go = new GameObject("Destello");
            go.transform.SetParent(transform, false);
            brillo = go.AddComponent<SpriteRenderer>();
            brillo.sprite = spriteBrillo;
            if (materialBrillo != null) brillo.sharedMaterial = materialBrillo;
            brillo.sortingLayerID = dibujo.sortingLayerID;
            brillo.sortingOrder = dibujo.sortingOrder - 1;
            brillo.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private void Update()
    {
        if (recolectada)
        {
            float r = Mathf.Clamp01((Time.time - inicioRecolectar) / duracionRecolectar);
            transform.localScale = escalaBase * (1f + 0.6f * r);
            SetAlfa(1f - r);
            if (r >= 1f) gameObject.SetActive(false);
            return;
        }

        float t = Time.time - inicio;
        float onda = 0f;
        if (t >= esperaAntesDeFlotar)
        {
            onda = Mathf.Sin((t - esperaAntesDeFlotar) * Mathf.PI * 2f / periodoFlotacion);
            transform.position = posicionBase + Vector3.up * onda * alturaFlotacion;
        }
        if (brillo != null)
        {
            float encendido = Mathf.Clamp01((t - (esperaAntesDeFlotar - duracionAparecerBrillo)) / Mathf.Max(0.01f, duracionAparecerBrillo));
            var c = brillo.color;
            c.a = encendido * Mathf.Lerp(brilloMinimo, brilloMaximo, (onda + 1f) * 0.5f);
            brillo.color = c;
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (recolectada || otro.GetComponentInParent<SaludPersonaje>() == null) return;
        recolectada = true;
        inicioRecolectar = Time.time;
        GetComponent<Collider2D>().enabled = false;
        Recolectada?.Invoke();
    }

    private void SetAlfa(float a)
    {
        var c = dibujo.color; c.a = a; dibujo.color = c;
        if (brillo != null) { var b = brillo.color; b.a = Mathf.Min(b.a, a); brillo.color = b; }
    }
}
