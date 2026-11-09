using UnityEngine;

// Pedacito suelto (por ejemplo, de la bola de ignorancia rota por el latigo): sale volando con gravedad,
// gira, se achica y se apaga. Se destruye solo.
[RequireComponent(typeof(SpriteRenderer))]
public class PedazoQueVuela : MonoBehaviour
{
    private Vector2 velocidad;
    private float duracion, vida, giro;
    private SpriteRenderer sr;
    private Vector3 escala;

    public void Lanzar(Vector2 velocidadInicial, float segundos)
    {
        velocidad = velocidadInicial;
        duracion = Mathf.Max(0.05f, segundos);
        giro = Random.Range(-540f, 540f);
        sr = GetComponent<SpriteRenderer>();
        escala = transform.localScale;
    }

    private void Update()
    {
        vida += Time.deltaTime;
        float u = vida / Mathf.Max(0.01f, duracion);
        if (u >= 1f) { Destroy(gameObject); return; }
        velocidad += Vector2.down * 9f * Time.deltaTime;
        transform.position += (Vector3)(velocidad * Time.deltaTime);
        transform.Rotate(0f, 0f, giro * Time.deltaTime);
        transform.localScale = escala * (1f - u * 0.6f);
        if (sr != null) { var c = sr.color; c.a = 1f - u * u; sr.color = c; }
    }
}
