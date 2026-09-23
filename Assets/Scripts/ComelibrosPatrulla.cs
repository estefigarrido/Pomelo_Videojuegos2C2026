using UnityEngine;

// Camina de ida y vuelta entre dos puntos sobre la plataforma.
public class ComelibrosPatrulla : MonoBehaviour
{
    [Header("Recorrido")]
    [Tooltip("Hasta donde camina hacia la izquierda (X del mundo, posicion del centro).")]
    [SerializeField] private float xMinimo;
    [Tooltip("Hasta donde camina hacia la derecha (X del mundo, posicion del centro).")]
    [SerializeField] private float xMaximo;

    [Header("Caminata")]
    [Tooltip("Unidades por segundo (1 unidad = 100 px).")]
    [SerializeField] private float velocidad = 1.5f;
    [Tooltip("El dibujo original mira hacia la izquierda.")]
    [SerializeField] private bool dibujoMiraIzquierda = true;

    private SpriteRenderer dibujo;
    private float sentido = 1f;

    public float XMinimo => xMinimo;
    public float XMaximo => xMaximo;

    public void ConfigurarRecorrido(float minimo, float maximo)
    {
        xMinimo = Mathf.Min(minimo, maximo);
        xMaximo = Mathf.Max(minimo, maximo);
    }

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        sentido = Random.value < 0.5f ? -1f : 1f;

        var animator = GetComponent<Animator>();
        if (animator != null) animator.Play(0, -1, Random.value);
    }

    private void Update()
    {
        Vector3 p = transform.position;
        p.x += sentido * velocidad * Time.deltaTime;

        if (p.x >= xMaximo) { p.x = xMaximo; sentido = -1f; }
        else if (p.x <= xMinimo) { p.x = xMinimo; sentido = 1f; }

        transform.position = p;
        if (dibujo != null) dibujo.flipX = dibujoMiraIzquierda ? sentido > 0f : sentido < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.9f);
        float y = transform.position.y;
        Gizmos.DrawLine(new Vector3(xMinimo, y), new Vector3(xMaximo, y));
        Gizmos.DrawLine(new Vector3(xMinimo, y - 0.3f), new Vector3(xMinimo, y + 0.3f));
        Gizmos.DrawLine(new Vector3(xMaximo, y - 0.3f), new Vector3(xMaximo, y + 0.3f));
    }
}
