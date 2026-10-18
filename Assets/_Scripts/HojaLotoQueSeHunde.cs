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

    [Header("Volver a su lugar")]
    [Tooltip("Segundos que se queda en el fondo antes de volver (0 = no vuelve).")]
    [SerializeField] private float segundosEnElFondo = 4f;
    [SerializeField] private float segundosFundido = 0.6f;

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

        if (segundosEnElFondo <= 0f) yield break;

        // 4) despues de un rato vuelve a su lugar (se desvanece y reaparece)
        yield return new WaitForSeconds(segundosEnElFondo);
        yield return Fundir(1f, 0f);
        rb.position = posicionInicial;
        rb.rotation = rotacionInicial;
        transform.SetPositionAndRotation(new Vector3(posicionInicial.x, posicionInicial.y, zInicial), Quaternion.Euler(0f, 0f, rotacionInicial));
        if (dibujo != null) dibujo.sortingOrder = ordenInicial;
        col.enabled = true;
        yield return Fundir(0f, 1f);
        cayendo = false;
    }

    private IEnumerator Fundir(float desde, float hasta)
    {
        if (dibujo == null) yield break;
        for (float t = 0f; t < segundosFundido; t += Time.deltaTime)
        {
            var c = dibujo.color; c.a = Mathf.Lerp(desde, hasta, t / segundosFundido); dibujo.color = c;
            yield return null;
        }
        var f = dibujo.color; f.a = hasta; dibujo.color = f;
    }

    private void SetZ(float z)
    {
        var p = transform.position; p.z = z; transform.position = p;
    }
}
