using UnityEngine;

// Cuando la chica entra al rango de agro, deja de patrullar, se da vuelta hacia ella,
// se acerca (sin salir de su recorrido) y muerde en loop. Pega si las hitboxes se tocan.
[RequireComponent(typeof(ComelibrosPatrulla))]
public class AtaqueComelibros : MonoBehaviour
{
    [Header("Animacion (comelibros-ataque)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 8f;

    [Header("Rango de agro (capsula horizontal, relativa al comelibros)")]
    [SerializeField] private Vector2 centroAgro = new Vector2(0f, 0.72f);
    [SerializeField] private Vector2 tamanoAgro = new Vector2(6.9f, 1.7f);

    [Header("Ataque")]
    [Tooltip("Vida que le saca a la chica por mordida (su vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Segundos entre mordida y mordida mientras siguen tocandose.")]
    [SerializeField] private float esperaEntreGolpes = 1f;
    [Tooltip("Velocidad con la que se acerca a la chica (unidades por segundo).")]
    [SerializeField] private float velocidadAtaque = 2.5f;
    [Tooltip("El dibujo original mira hacia la izquierda.")]
    [SerializeField] private bool dibujoMiraIzquierda = true;

    private ComelibrosPatrulla patrulla;
    private SpriteRenderer dibujo;
    private Collider2D hitbox;
    private SaludPersonaje victima;
    private Collider2D hitboxVictima;
    private bool atacando;
    private float inicioAtaque;
    private float proximoGolpe;

    public bool Atacando => atacando;

    private void Awake()
    {
        patrulla = GetComponent<ComelibrosPatrulla>();
        dibujo = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
    }

    private void Start()
    {
        victima = FindFirstObjectByType<SaludPersonaje>();
        if (victima != null) hitboxVictima = victima.GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (hitboxVictima == null) return;

        if (!VictimaEnAgro())
        {
            if (atacando) { atacando = false; patrulla.enabled = true; }
            return;
        }

        if (!atacando)
        {
            atacando = true;
            inicioAtaque = Time.time;
            patrulla.enabled = false;
        }

        float objetivoX = hitboxVictima.bounds.center.x;
        bool tocando = hitbox != null && hitbox.Distance(hitboxVictima).isOverlapped;

        Vector3 p = transform.position;
        if (!tocando)
        {
            p.x = Mathf.MoveTowards(p.x, objetivoX, velocidadAtaque * Time.deltaTime);
            p.x = Mathf.Clamp(p.x, patrulla.XMinimo, patrulla.XMaximo);
            transform.position = p;
        }
        if (dibujo != null) dibujo.flipX = dibujoMiraIzquierda ? objetivoX > p.x : objetivoX < p.x;

        if (tocando && Time.time >= proximoGolpe)
        {
            victima.RecibirDanio(danio);
            proximoGolpe = Time.time + esperaEntreGolpes;
        }
    }

    private void LateUpdate()
    {
        if (atacando && frames != null && frames.Length > 0)
            dibujo.sprite = frames[Mathf.FloorToInt((Time.time - inicioAtaque) * fps) % frames.Length];
    }

    private bool VictimaEnAgro()
    {
        Vector2 centro = (Vector2)transform.position + centroAgro;
        foreach (var c in Physics2D.OverlapCapsuleAll(centro, tamanoAgro, CapsuleDirection2D.Horizontal, 0f))
            if (c == hitboxVictima) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.55f, 0.5f, 1f, 0.9f);
        Vector2 c = (Vector2)transform.position + centroAgro;
        float r = tamanoAgro.y / 2f, medio = Mathf.Max(0f, tamanoAgro.x / 2f - r);
        Gizmos.DrawWireSphere(c + Vector2.left * medio, r);
        Gizmos.DrawWireSphere(c + Vector2.right * medio, r);
        Gizmos.DrawLine(c + new Vector2(-medio, r), c + new Vector2(medio, r));
        Gizmos.DrawLine(c + new Vector2(-medio, -r), c + new Vector2(medio, -r));
    }
}
