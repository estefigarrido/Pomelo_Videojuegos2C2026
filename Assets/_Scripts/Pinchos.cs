using UnityEngine;

// Los pinchos son solidos (BoxCollider2D): la chica no los atraviesa. Cuando cae encima rebota
// un poquito (20 px) y mientras los toca recibe dano cada 'intervalo' segundos.
// Sirve tanto con collider solido como con trigger.
[RequireComponent(typeof(Collider2D))]
public class Pinchos : MonoBehaviour
{
    [Tooltip("Vida que saca cada golpe (la vida maxima es 100). Antes era 20; ahora el doble.")]
    [SerializeField] private float danio = 40f;
    [Tooltip("Segundos entre golpe y golpe mientras sigue tocando los pinchos.")]
    [SerializeField] private float intervalo = 2f;

    [Header("Rebote")]
    [Tooltip("Cuanto sube la chica al caer sobre los pinchos, en pixeles del dibujo.")]
    [SerializeField] private float alturaRebotePx = 20f;
    [Tooltip("Cuantos pixeles del dibujo entran en 1 unidad de Unity (igual que en MovimientoPersonaje).")]
    [SerializeField] private float pixelesPorUnidad = 100f;

    private Collider2D propio;
    private SaludPersonaje victima;
    private float proximoGolpe;

    private void Awake() => propio = GetComponent<Collider2D>();

    private void OnCollisionEnter2D(Collision2D choque) => Entrar(choque.collider);
    private void OnCollisionExit2D(Collision2D choque) => Salir(choque.collider);
    private void OnTriggerEnter2D(Collider2D otro) => Entrar(otro);
    private void OnTriggerExit2D(Collider2D otro) => Salir(otro);

    private void Entrar(Collider2D otro)
    {
        var salud = otro.GetComponentInParent<SaludPersonaje>();
        if (salud == null) return;

        Rebotar(otro, salud);

        bool nuevo = victima != salud;
        victima = salud;
        if (nuevo && Time.time >= proximoGolpe) Golpear();
    }

    private void Salir(Collider2D otro)
    {
        if (victima != null && otro.GetComponentInParent<SaludPersonaje>() == victima) victima = null;
    }

    // Se controla aca y no con OnCollisionStay2D: si la chica se queda quieta, la fisica
    // la "duerme" y deja de avisar el contacto, pero sigue parada sobre los pinchos.
    private void Update()
    {
        if (victima != null && Time.time >= proximoGolpe) Golpear();
    }

    private void Golpear()
    {
        victima.RecibirDanio(danio);
        proximoGolpe = Time.time + intervalo;
    }

    // Solo si cae desde arriba (de costado no rebota). Le da la velocidad justa para subir
    // 'alturaRebotePx'; al volver a caer toca otra vez los pinchos y vuelve a rebotar.
    private void Rebotar(Collider2D otro, SaludPersonaje salud)
    {
        if (alturaRebotePx <= 0f || propio == null) return;
        if (otro.bounds.min.y < propio.bounds.max.y - 0.1f) return;

        var rb = salud.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        float gravedad = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (gravedad <= 0f) return;

        float altura = alturaRebotePx / Mathf.Max(1f, pixelesPorUnidad);
        // misma cuenta que el salto: compensa el medio paso de fisica que se pierde al subir
        float medioPaso = gravedad * Time.fixedDeltaTime * 0.5f;
        float impulso = medioPaso + Mathf.Sqrt(medioPaso * medioPaso + 2f * gravedad * altura);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, impulso));
    }
}
