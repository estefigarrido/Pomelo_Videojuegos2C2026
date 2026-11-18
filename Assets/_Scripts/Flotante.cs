using UnityEngine;

// Algo que flota en el agua (las columnas grises caidas y la vasija de la pantalla de inicio): sube y baja apenas
// y se mece un poquito. Este objeto se pone en la base, a la altura del agua, y queda QUIETO (con el agua de
// adelante de hijo); lo que flota es 'queFlota' (el dibujo), que se mece alrededor de este punto.
// Asi, si se mueve el objeto a mano, el dibujo y su agua lo siguen juntos.
// Usa el tiempo del juego: en pausa se queda quieto.
// Lo pone el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
// [SelectionBase]: al hacer clic sobre el dibujo en la escena se selecciona este objeto (la base), no el dibujo;
// asi, al moverlo, se mueve todo junto (el dibujo, su brillo, el agua de adelante, el Lore de la vasija...).
[SelectionBase]
public class Flotante : MonoBehaviour
{
    [Tooltip("Lo que sube, baja y se mece (el dibujo). Vacio = este mismo objeto.")]
    [SerializeField] private Transform queFlota;
    [Tooltip("Cuanto sube y baja (unidades).")]
    [SerializeField] private float subeYBaja = 0.06f;
    [Tooltip("Cuanto se mece (grados).")]
    [SerializeField] private float seMece = 0.7f;
    [Tooltip("Segundos de una subida y bajada.")]
    [SerializeField] private float periodo = 3.6f;
    [Tooltip("Para que cada cosa flote a su ritmo (en segundos).")]
    [SerializeField] private float desfase;

    private Transform objetivo;
    private Vector3 lugar;
    private Quaternion giro;
    private float tiempo;

    private void Awake()
    {
        objetivo = queFlota != null ? queFlota : transform;
        lugar = objetivo.localPosition;
        giro = objetivo.localRotation;
        tiempo = desfase;
    }

    private void Update()
    {
        tiempo += Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        float a = periodo > 0f ? tiempo * Mathf.PI * 2f / periodo : 0f;
        // se mece un poco desfasado de la subida, como algo que flota de verdad
        var meceo = Quaternion.Euler(0f, 0f, Mathf.Sin(a * 0.8f + 1.3f) * seMece);
        var sube = Vector3.up * (Mathf.Sin(a) * subeYBaja);
        if (objetivo == transform)
        {
            objetivo.localPosition = lugar + sube;
            objetivo.localRotation = giro * meceo;
        }
        else
        {
            // el dibujo gira alrededor de este punto (la base, en el agua), no de su esquina
            objetivo.localPosition = meceo * lugar + sube;
            objetivo.localRotation = meceo * giro;
        }
    }
}
