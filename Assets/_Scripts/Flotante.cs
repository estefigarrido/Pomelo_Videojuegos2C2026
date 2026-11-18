using UnityEngine;

// Algo que flota en el agua (las columnas grises caidas y la vasija de la pantalla de inicio): sube y baja apenas
// y se mece un poquito, sobre su propio punto (el objeto se pone en la base, a la altura del agua, y el dibujo va
// de hijo). Usa el tiempo del juego: en pausa se queda quieto.
// Lo pone el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class Flotante : MonoBehaviour
{
    [Tooltip("Cuanto sube y baja (unidades).")]
    [SerializeField] private float subeYBaja = 0.06f;
    [Tooltip("Cuanto se mece (grados).")]
    [SerializeField] private float seMece = 0.7f;
    [Tooltip("Segundos de una subida y bajada.")]
    [SerializeField] private float periodo = 3.6f;
    [Tooltip("Para que cada cosa flote a su ritmo (en segundos).")]
    [SerializeField] private float desfase;

    private Vector3 lugar;
    private float tiempo;

    private void Awake()
    {
        lugar = transform.localPosition;
        tiempo = desfase;
    }

    private void Update()
    {
        tiempo += Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        float a = periodo > 0f ? tiempo * Mathf.PI * 2f / periodo : 0f;
        transform.localPosition = lugar + Vector3.up * (Mathf.Sin(a) * subeYBaja);
        // se mece un poco desfasado de la subida, como algo que flota de verdad
        transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(a * 0.8f + 1.3f) * seMece);
    }
}
