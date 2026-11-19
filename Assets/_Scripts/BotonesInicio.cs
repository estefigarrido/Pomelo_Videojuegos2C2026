using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Botones de la pantalla de inicio (debajo del titulo). Por ahora hay uno solo: "Comenzar juego".
// Diseno de Caro (Figma, pagina "intro juego"): una franja oscura que se desvanece a los costados y el texto en
// Alegreya versalitas. Con el mouse encima (hover) la franja se oscurece y el texto brilla.
// - Click sobre un boton: lo activa.
// - Teclado / joystick: flechas, W/S o la cruceta arriba/abajo eligen; espacio, Enter o la X (Cruz) lo activan
//   (si no se eligio ninguno, activan el primero: Comenzar juego).
// No usa el EventSystem: mira la posicion del mouse sobre cada franja.
//
// Para AGREGAR un boton: sumarlo a la lista del menu en Assets/Editor/BotonesInicioEditor.cs (con sus imagenes de
// texto y brillo) y volver a correr  Pomelo -> Inicio -> Poner botones de inicio. Se ubica solo debajo del anterior.
// Con accion "Evento", lo que hace se conecta en el Inspector (alTocar).
public class BotonesInicio : MonoBehaviour
{
    public enum Accion { Comenzar, Evento }

    [System.Serializable]
    public class Boton
    {
        public Accion accion;
        [Tooltip("La franja entera (zona donde se puede hacer click).")]
        public RectTransform zona;
        public Image fondo;
        public Image brillo;
        [Tooltip("Solo con accion 'Evento': lo que pasa al tocarlo.")]
        public UnityEvent alTocar;
    }

    [SerializeField] private PantallaInicio pantalla;
    [SerializeField] private Boton[] botones;

    [Header("Aspecto")]
    [Tooltip("Opacidad de la franja oscura sin el mouse encima (Figma: negro al 20 %).")]
    [Range(0f, 1f)] [SerializeField] private float alfaFondoNormal = 0.2f;
    [Tooltip("Opacidad de la franja oscura con el mouse encima (Figma: negro al 60 %).")]
    [Range(0f, 1f)] [SerializeField] private float alfaFondoHover = 0.6f;
    [Tooltip("Opacidad del brillo del texto con el mouse encima.")]
    [Range(0f, 1f)] [SerializeField] private float alfaBrillo = 1f;
    [Tooltip("Segundos que tarda en pasar de un estado al otro.")]
    [SerializeField] private float duracionCambio = 0.12f;

    private float[] estado;      // 0 = normal, 1 = hover (con decimales mientras cambia)
    private int elegido = -1;    // el que esta con hover (-1 = ninguno)
    private Vector2 ultimoMouse;
    private bool usado;

    private void Awake()
    {
        estado = new float[botones != null ? botones.Length : 0];
        for (int i = 0; i < estado.Length; i++) Pintar(i);
        if (Mouse.current != null) ultimoMouse = Mouse.current.position.ReadValue();
    }

    private void Update()
    {
        if (usado || botones == null || botones.Length == 0) return;

        // el mouse elige el boton que tiene abajo (solo si se movio, asi no le gana al teclado)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 p = mouse.position.ReadValue();
            int bajoMouse = -1;
            for (int i = 0; i < botones.Length; i++)
                if (botones[i].zona != null && RectTransformUtility.RectangleContainsScreenPoint(botones[i].zona, p, null))
                    bajoMouse = i;
            if ((p - ultimoMouse).sqrMagnitude > 0.5f) elegido = bajoMouse;
            ultimoMouse = p;
            if (mouse.leftButton.wasPressedThisFrame && bajoMouse >= 0) { Hacer(bajoMouse); return; }
        }

        // teclado y joystick
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        bool arriba = (teclado != null && (teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame))
                      || (mando != null && mando.dpad.up.wasPressedThisFrame);
        bool abajo = (teclado != null && (teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame))
                     || (mando != null && mando.dpad.down.wasPressedThisFrame);
        if (arriba) elegido = elegido <= 0 ? botones.Length - 1 : elegido - 1;
        if (abajo) elegido = elegido < 0 || elegido >= botones.Length - 1 ? 0 : elegido + 1;
        bool aceptar = (teclado != null && (teclado.spaceKey.wasPressedThisFrame || teclado.enterKey.wasPressedThisFrame))
                       || Mando.Aceptar;
        if (aceptar) { Hacer(elegido >= 0 ? elegido : 0); return; }

        // animacion suave entre normal y hover
        float paso = duracionCambio > 0f ? Time.unscaledDeltaTime / duracionCambio : 1f;
        for (int i = 0; i < botones.Length; i++)
        {
            estado[i] = Mathf.MoveTowards(estado[i], i == elegido ? 1f : 0f, paso);
            Pintar(i);
        }
    }

    private void Pintar(int i)
    {
        var b = botones[i];
        float k = Mathf.SmoothStep(0f, 1f, estado[i]);
        if (b.fondo != null) b.fondo.color = new Color(0f, 0f, 0f, Mathf.Lerp(alfaFondoNormal, alfaFondoHover, k));
        if (b.brillo != null) b.brillo.color = new Color(1f, 1f, 1f, alfaBrillo * k);
    }

    private void Hacer(int i)
    {
        var b = botones[i];
        if (b.accion == Accion.Comenzar)
        {
            usado = true;   // los botones se desvanecen junto con el titulo
            if (pantalla != null) pantalla.Comenzar();
        }
        else b.alTocar?.Invoke();
    }
}
