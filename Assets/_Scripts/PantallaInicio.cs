using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla de inicio (escena Inicio_introducción): la cueva con el titulo TÉCHNI y "Comenzar  [espacio] o [X]".
// - La camara encuadra la cueva de inicio de punta a punta (los 3612 px del dibujo de Caro), en cualquier pantalla.
// - Con la barra espaciadora o la X (Cruz) del joystick, funde a negro y carga el nivel.
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class PantallaInicio : MonoBehaviour
{
    [Tooltip("Escena que se carga al comenzar (tiene que estar en la lista del build).")]
    [SerializeField] private string escenaJuego = "Nivel1_Templo";

    [Header("Encuadre")]
    [SerializeField] private Camera camara;
    [Tooltip("Ancho de la cueva de inicio en unidades (3612 px / 100). La camara la muestra entera.")]
    [SerializeField] private float anchoCueva = 36.12f;
    [Tooltip("Alto de todo el dibujo en unidades (2160 px / 100). Si la pantalla es muy angosta, se ve entero el alto.")]
    [SerializeField] private float altoDibujo = 21.6f;

    [Header("Al comenzar")]
    [Tooltip("Imagen negra de pantalla completa que se usa para el fundido.")]
    [SerializeField] private Image fundido;
    [SerializeField] private float duracionFundido = 0.8f;

    private bool comenzando;
    private float inicioFundido;

    private void Awake()
    {
        Time.timeScale = 1f;   // por si se vuelve aca desde la pausa
        if (fundido != null)
        {
            fundido.color = new Color(0f, 0f, 0f, 0f);
            fundido.raycastTarget = false;
        }
        Encuadrar();
    }

    private void Update()
    {
        Encuadrar();   // por si cambia el tamano de la ventana

        if (!comenzando)
        {
            var teclado = Keyboard.current;
            bool apreto = (teclado != null && teclado.spaceKey.wasPressedThisFrame) || Mando.Aceptar;
            if (apreto)
            {
                comenzando = true;
                inicioFundido = Time.unscaledTime;
            }
            return;
        }

        float k = duracionFundido > 0f ? Mathf.Clamp01((Time.unscaledTime - inicioFundido) / duracionFundido) : 1f;
        if (fundido != null) fundido.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, k));
        if (k >= 1f)
        {
            enabled = false;
            SceneManager.LoadScene(escenaJuego);
        }
    }

    // muestra la cueva entera de ancho, centrada (en 16:9 se recorta apenas arriba y abajo, parejo)
    private void Encuadrar()
    {
        if (camara == null) return;
        float aspecto = camara.aspect > 0f ? camara.aspect : 16f / 9f;
        float alto = Mathf.Min(anchoCueva / aspecto, altoDibujo);
        camara.orthographicSize = alto / 2f;
        var p = camara.transform.position;
        camara.transform.position = new Vector3(anchoCueva / 2f, altoDibujo / 2f, p.z);
    }
}
