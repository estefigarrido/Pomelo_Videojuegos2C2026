using UnityEngine;

// Un brillo suave que late despacio (se hace un poco mas fuerte y mas debil). Va en el SpriteRenderer del brillo
// de la vasija flotante de la pantalla de inicio, para que se note que es "especial" sin exagerar.
// Usa el tiempo del juego: en pausa se queda quieto.
[RequireComponent(typeof(SpriteRenderer))]
public class Resplandor : MonoBehaviour
{
    [Tooltip("Lo mas debil que se ve el brillo (0 a 1).")]
    [Range(0f, 1f)] [SerializeField] private float minimo = 0.3f;
    [Tooltip("Lo mas fuerte que se ve el brillo (0 a 1).")]
    [Range(0f, 1f)] [SerializeField] private float maximo = 0.55f;
    [Tooltip("Segundos de un latido completo.")]
    [SerializeField] private float periodo = 3.2f;

    private SpriteRenderer dibujo;
    private float tiempo;

    private void Awake() => dibujo = GetComponent<SpriteRenderer>();

    private void Update()
    {
        tiempo += Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        float k = periodo > 0f ? 0.5f - 0.5f * Mathf.Cos(tiempo * Mathf.PI * 2f / periodo) : 1f;
        var c = dibujo.color;
        c.a = Mathf.Lerp(minimo, maximo, k);
        dibujo.color = c;
    }
}
