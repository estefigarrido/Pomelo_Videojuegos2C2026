using UnityEngine;

// Neblina que se mueve un poquito (pantalla de inicio): va y viene de costado muy despacio, sube y baja apenas y
// se pone un poco mas y menos densa, cada capa a su ritmo. Usa el tiempo del juego: en pausa se queda quieta.
// Lo pone el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción  en las capas de niebla.
[RequireComponent(typeof(SpriteRenderer))]
public class Neblina : MonoBehaviour
{
    [Tooltip("Cuanto va y viene de costado (unidades).")]
    [SerializeField] private float deCostado = 0.35f;
    [Tooltip("Cuanto sube y baja (unidades).")]
    [SerializeField] private float arribaAbajo = 0.06f;
    [Tooltip("Segundos de una ida y vuelta.")]
    [SerializeField] private float periodo = 18f;
    [Tooltip("Lo menos densa que se pone (1 = no cambia).")]
    [Range(0f, 1f)] [SerializeField] private float densidadMinima = 0.8f;
    [Tooltip("Para que cada capa vaya a su ritmo (segundos).")]
    [SerializeField] private float desfase;

    private SpriteRenderer dibujo;
    private Vector3 lugar;
    private float alfa;
    private float tiempo;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        lugar = transform.localPosition;
        alfa = dibujo.color.a;
        tiempo = desfase;
    }

    private void Update()
    {
        tiempo += Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        float w = periodo > 0f ? Mathf.PI * 2f / periodo : 0f;
        float a = tiempo * w;
        transform.localPosition = lugar + new Vector3(Mathf.Sin(a) * deCostado, Mathf.Sin(a * 1.7f + 0.8f) * arribaAbajo, 0f);
        var c = dibujo.color;
        c.a = alfa * Mathf.Lerp(densidadMinima, 1f, 0.5f + 0.5f * Mathf.Sin(a * 1.3f + 2f));
        dibujo.color = c;
    }
}
