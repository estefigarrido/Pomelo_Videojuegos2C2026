using UnityEngine;

// Va en el rombo del ojo de Umbrae: mira a la chica moviendose dentro del iris, de izquierda a derecha y de
// arriba a abajo, sin salirse (se mueve dentro de una elipse) y con un poco de suavidad, como un ojo que sigue.
public class OjoSigueChica : MonoBehaviour
{
    [Tooltip("Cuanto se puede correr el rombo para los costados (unidades).")]
    [SerializeField] private float maximoX = 0.9f;
    [Tooltip("Cuanto se puede correr para arriba y abajo (unidades).")]
    [SerializeField] private float maximoY = 0.4f;
    [Tooltip("A esta distancia de la chica ya mira del todo para ese lado.")]
    [SerializeField] private float distanciaCompleta = 14f;
    [Tooltip("Segundos que tarda en acomodarse la mirada.")]
    [SerializeField] private float suavidad = 0.18f;

    private Transform chica;
    private Vector3 inicio;
    private Vector2 velocidad;
    private Vector2 actual;

    private void Start()
    {
        inicio = transform.localPosition;
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
    }

    private void LateUpdate()
    {
        if (chica == null) return;
        Vector3 centro = transform.parent != null ? transform.parent.position : transform.position;
        // mira al medio del cuerpo de la chica
        Vector2 d = (Vector2)(chica.position + Vector3.up * 1.2f - centro);
        Vector2 dir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.zero;
        float cuanto = Mathf.Clamp01(d.magnitude / distanciaCompleta);
        Vector2 objetivo = new Vector2(dir.x * maximoX, dir.y * maximoY) * cuanto;
        actual = Vector2.SmoothDamp(actual, objetivo, ref velocidad, suavidad);
        transform.localPosition = inicio + new Vector3(actual.x, actual.y, 0f);
    }
}
