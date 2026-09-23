using UnityEngine;

// Cuando la chica entra al rango de agro, queda enganchado durante 'duracionAgro' segundos
// (aunque ella salga del rango): la persigue dentro de su recorrido y muerde cada vez que la
// alcanza. Cada mordida es la animacion completa; el dano entra en el frame de la mordida
// si las hitboxes se tocan. Al terminar la ventana, completa la mordida en curso y vuelve a patrullar.
[RequireComponent(typeof(ComelibrosPatrulla))]
public class AtaqueComelibros : MonoBehaviour
{
    private enum Estado { Patrullando, Persiguiendo, Mordiendo }

    [Header("Animacion (comelibros-ataque)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 8f;
    [Tooltip("Frame de la animacion en el que muerde (saca las pinzas). Empieza en 1.")]
    [SerializeField] private int frameMordida = 3;

    [Header("Rango de agro (capsula horizontal, relativa al comelibros)")]
    [SerializeField] private Vector2 centroAgro = new Vector2(0f, 0.72f);
    [SerializeField] private Vector2 tamanoAgro = new Vector2(6.9f, 1.7f);
    [Tooltip("Segundos que sigue atacando desde que la chica entra al agro, aunque se aleje.")]
    [SerializeField] private float duracionAgro = 10f;

    [Header("Ataque")]
    [Tooltip("Vida que le saca a la chica por mordida (su vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Pausa entre una mordida y la siguiente.")]
    [SerializeField] private float esperaEntreMordidas = 0.6f;
    [Tooltip("Velocidad con la que la persigue (unidades por segundo).")]
    [SerializeField] private float velocidadAtaque = 2.5f;
    [Tooltip("El dibujo original mira hacia la izquierda.")]
    [SerializeField] private bool dibujoMiraIzquierda = true;

    private ComelibrosPatrulla patrulla;
    private SpriteRenderer dibujo;
    private Collider2D hitbox;
    private SaludPersonaje victima;
    private Collider2D hitboxVictima;

    private Estado estado = Estado.Patrullando;
    private float finAgro;
    private float inicioMordida;
    private float proximaMordida;
    private bool mordidaAplicada;

    public bool Atacando => estado != Estado.Patrullando;

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
        if (hitboxVictima == null || hitbox == null) return;

        switch (estado)
        {
            case Estado.Patrullando:
                if (VictimaEnAgro())
                {
                    finAgro = Time.time + duracionAgro;
                    proximaMordida = Time.time;
                    patrulla.enabled = false;
                    estado = Estado.Persiguiendo;
                }
                break;

            case Estado.Persiguiendo:
                if (Time.time >= finAgro)
                {
                    patrulla.enabled = true;
                    estado = Estado.Patrullando;
                    break;
                }
                MirarHaciaVictima();
                if (Tocando())
                {
                    if (Time.time >= proximaMordida)
                    {
                        inicioMordida = Time.time;
                        mordidaAplicada = false;
                        estado = Estado.Mordiendo;
                    }
                }
                else
                {
                    Vector3 p = transform.position;
                    p.x = Mathf.MoveTowards(p.x, hitboxVictima.bounds.center.x, velocidadAtaque * Time.deltaTime);
                    p.x = Mathf.Clamp(p.x, patrulla.XMinimo, patrulla.XMaximo);
                    transform.position = p;
                }
                break;

            case Estado.Mordiendo:
                int frame = FrameMordida();
                if (!mordidaAplicada && frame >= frameMordida)
                {
                    mordidaAplicada = true;
                    if (Tocando()) victima.RecibirDanio(danio);
                }
                if (frame > frames.Length)
                {
                    proximaMordida = Time.time + esperaEntreMordidas;
                    estado = Estado.Persiguiendo;
                }
                break;
        }
    }

    private void LateUpdate()
    {
        if (estado == Estado.Mordiendo && frames != null && frames.Length > 0)
            dibujo.sprite = frames[Mathf.Clamp(FrameMordida() - 1, 0, frames.Length - 1)];
    }

    private int FrameMordida() => Mathf.FloorToInt((Time.time - inicioMordida) * fps) + 1;

    private bool Tocando() => hitbox.Distance(hitboxVictima).isOverlapped;

    private void MirarHaciaVictima()
    {
        if (dibujo == null) return;
        float x = hitboxVictima.bounds.center.x;
        dibujo.flipX = dibujoMiraIzquierda ? x > transform.position.x : x < transform.position.x;
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
