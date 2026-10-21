using System.Collections;
using UnityEngine;

// Hoja de loto del agua venenosa. Cuando la chica se para arriba:
//  1) se hunde de a poco (3 s) con ella encima;
//  2) se inclina (la punta izquierda arriba, como en el Figma) y deja de sostenerla;
//  3) se cae al fondo del lago meciendose de costado, como si bajara por el agua.
// Despues de un rato en el fondo se desvanece y vuelve a aparecer en su lugar.
// Las medidas salen del Figma (px / 100): se hunde 48 px y termina 421 px mas abajo, inclinada -4.42 grados.
[RequireComponent(typeof(Collider2D))]
public class HojaLotoQueSeHunde : MonoBehaviour
{
    [Header("Hundirse con la chica arriba")]
    [Tooltip("Segundos que tarda en hundirse con la chica arriba antes de caerse.")]
    [SerializeField] private float segundosHundiendose = 3f;
    [Tooltip("Cuanto baja mientras se hunde (unidades).")]
    [SerializeField] private float bajaAlHundirse = 0.48f;

    [Header("Inclinarse")]
    [Tooltip("Grados que se inclina antes de caerse (negativo = la punta izquierda queda arriba).")]
    [SerializeField] private float inclinacion = -4.42f;
    [SerializeField] private float segundosInclinandose = 0.5f;

    [Header("Caer al fondo del agua")]
    [Tooltip("Cuanto baja desde que se inclina hasta el fondo del lago (unidades).")]
    [SerializeField] private float caida = 3.73f;
    [SerializeField] private float segundosCayendo = 3f;
    [Tooltip("Vaiven mientras cae: cuanto se corre de costado (unidades) y cuanto se balancea (grados).")]
    [SerializeField] private float vaivenCostado = 0.18f;
    [SerializeField] private float vaivenGiro = 4f;
    [Tooltip("Vaivenes por segundo.")]
    [SerializeField] private float frecuenciaVaiven = 0.7f;
    [Tooltip("Orden de dibujo bajo el agua: entre las espinas (3) y el agua violeta (4), para que se vea sumergida.")]
    [SerializeField] private int ordenBajoElAgua = 3;

    [Header("Disolverse en el fondo")]
    [Tooltip("Segundos que tarda en disolverse al tocar el fondo (se tine de violeta, se desarma y se desvanece).")]
    [SerializeField] private float segundosDisolviendose = 0.9f;
    [SerializeField] private Color colorDisuelta = new Color(0.75f, 0.45f, 0.85f, 1f);

    [Header("Crecer una nueva")]
    [Tooltip("Segundos despues de disolverse hasta que crece una hoja nueva en su lugar (0 = no crece).")]
    [SerializeField] private float segundosParaRebrotar = 4f;
    [Tooltip("Segundos que tarda en crecer desde el tallo.")]
    [SerializeField] private float segundosCreciendo = 0.8f;

    private Collider2D col;
    private Rigidbody2D rb;
    private SpriteRenderer dibujo;
    private Vector2 posicionInicial;
    private float rotacionInicial;
    private int ordenInicial;
    private float zInicial;
    private bool cayendo;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        dibujo = GetComponent<SpriteRenderer>();
        // cinematico: se mueve por codigo y lleva a la chica encima sin sacudirla
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        posicionInicial = transform.position;
        rotacionInicial = transform.eulerAngles.z;
        zInicial = transform.position.z;
        if (dibujo != null) ordenInicial = dibujo.sortingOrder;
    }

    private void OnCollisionEnter2D(Collision2D choque) => Revisar(choque.collider);
    private void OnCollisionStay2D(Collision2D choque) => Revisar(choque.collider);

    // solo cuenta si la chica esta parada arriba (no si la toca de costado o de abajo)
    private void Revisar(Collider2D otro)
    {
        if (cayendo || otro.GetComponentInParent<SaludPersonaje>() == null) return;
        if (otro.bounds.min.y < col.bounds.center.y) return;
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        cayendo = true;
        var espera = new WaitForFixedUpdate();

        // 1) se hunde de a poco con ella encima
        for (float t = 0f; t < segundosHundiendose; t += Time.fixedDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / segundosHundiendose);
            rb.MovePosition(posicionInicial + Vector2.down * bajaAlHundirse * k);
            yield return espera;
        }
        Vector2 hundida = posicionInicial + Vector2.down * bajaAlHundirse;

        // 2) se inclina y deja de sostenerla; desde aca ya esta bajo el agua
        col.enabled = false;
        if (dibujo != null) dibujo.sortingOrder = ordenBajoElAgua;
        SetZ(zInicial - 0.01f);   // por delante de las espinas, que estan en el mismo orden
        for (float t = 0f; t < segundosInclinandose; t += Time.fixedDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / segundosInclinandose);
            rb.MoveRotation(rotacionInicial + inclinacion * k);
            rb.MovePosition(hundida + Vector2.down * 0.08f * k);
            yield return espera;
        }
        Vector2 inicioCaida = hundida + Vector2.down * 0.08f;

        // 3) cae al fondo como en el agua: arranca despacio, va frenando al llegar, y se mece de costado
        for (float t = 0f; t < segundosCayendo; t += Time.fixedDeltaTime)
        {
            float u = t / segundosCayendo;
            float bajada = (caida - 0.08f) * (1f - Mathf.Pow(1f - u, 2f));    // frena al llegar al fondo
            float amortiguado = 1f - u;                                         // el vaiven se apaga al llegar
            float onda = Mathf.Sin(u * segundosCayendo * frecuenciaVaiven * Mathf.PI * 2f);
            rb.MovePosition(inicioCaida + new Vector2(onda * vaivenCostado * amortiguado, -bajada));
            rb.MoveRotation(rotacionInicial + inclinacion + onda * vaivenGiro * amortiguado);
            yield return espera;
        }
        rb.MovePosition(inicioCaida + Vector2.down * (caida - 0.08f));
        rb.MoveRotation(rotacionInicial + inclinacion);

        // 4) se disuelve en el fondo: se tine de violeta, se desarma (se aplasta y se estira) y se desvanece
        Vector3 escala = transform.localScale;
        Color colorOriginal = dibujo != null ? dibujo.color : Color.white;
        Vector3 fondo = transform.position;
        for (float t = 0f; t < segundosDisolviendose; t += Time.deltaTime)
        {
            float u = t / segundosDisolviendose;
            transform.localScale = new Vector3(escala.x * (1f + 0.25f * u), escala.y * (1f - 0.7f * u), escala.z);
            Poner(fondo + Vector3.up * 0.15f * u);
            if (dibujo != null)
            {
                var c = Color.Lerp(colorOriginal, colorDisuelta, Mathf.Min(1f, u * 2f));
                c.a = colorOriginal.a * (1f - u * u);
                dibujo.color = c;
            }
            yield return null;
        }
        if (dibujo != null) dibujo.enabled = false;

        if (segundosParaRebrotar <= 0f) yield break;

        // 5) despues de un rato crece una hoja nueva desde el tallo (abajo al medio de la hoja), en su lugar
        yield return new WaitForSeconds(segundosParaRebrotar);
        rb.position = posicionInicial;
        rb.rotation = rotacionInicial;
        transform.rotation = Quaternion.Euler(0f, 0f, rotacionInicial);
        if (dibujo != null)
        {
            dibujo.color = colorOriginal;
            dibujo.sortingOrder = ordenInicial;
            dibujo.enabled = true;
        }
        // la hoja nace de la punta del tallo: abajo, del lado de la muesca (a la izquierda si no esta espejada),
        // y se abre como una flor: arranca inclinada y angosta, se despliega a lo ancho y se endereza
        transform.localScale = escala;
        Vector2 medio = dibujo != null ? (Vector2)dibujo.sprite.bounds.extents : new Vector2(2f, 0.4f);
        bool espejada = dibujo != null && dibujo.flipX;
        var deTallo = new Vector2((espejada ? 1f : -1f) * 0.45f * medio.x, -medio.y);   // en el espacio de la hoja, sin escala
        var centro = new Vector3(posicionInicial.x, posicionInicial.y, zInicial);
        Vector2 tallo = (Vector2)centro + new Vector2(deTallo.x * escala.x, deTallo.y * escala.y);
        float inclinacionInicial = espejada ? -28f : 28f;
        for (float t = 0f; t < segundosCreciendo; t += Time.deltaTime)
        {
            float u = t / segundosCreciendo;
            float sy = Mathf.Max(0.02f, RebotarAlFinal(u));
            float sx = Mathf.Max(0.02f, RebotarAlFinal(Mathf.Clamp01((u - 0.12f) / 0.88f)));   // se abre a lo ancho un poco despues
            float giro = rotacionInicial + inclinacionInicial * (1f - Mathf.SmoothStep(0f, 1f, u));
            var q = Quaternion.Euler(0f, 0f, giro);
            transform.localScale = new Vector3(escala.x * sx, escala.y * sy, escala.z);
            // el punto del tallo queda quieto: el centro se acomoda alrededor de el
            Vector2 desde = q * new Vector3(deTallo.x * escala.x * sx, deTallo.y * escala.y * sy, 0f);
            rb.rotation = giro;
            transform.rotation = q;
            Poner(new Vector3(tallo.x - desde.x, tallo.y - desde.y, zInicial));
            yield return null;
        }
        transform.localScale = escala;
        rb.rotation = rotacionInicial;
        transform.rotation = Quaternion.Euler(0f, 0f, rotacionInicial);
        Poner(centro);
        col.enabled = true;
        cayendo = false;
    }

    // crece rapido y se pasa un poquito antes de quedar en su tamano
    private static float RebotarAlFinal(float u)
    {
        u = Mathf.Clamp01(u);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(u - 1f, 3f) + c1 * Mathf.Pow(u - 1f, 2f);
    }

    // mueve la hoja y su Rigidbody2D juntos (con interpolacion, el Rigidbody2D manda sobre el transform)
    private void Poner(Vector3 p)
    {
        rb.position = p;
        transform.position = p;
    }

    private void SetZ(float z)
    {
        var p = transform.position; p.z = z; transform.position = p;
    }
}
