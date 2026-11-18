using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// El cuadro de dialogo (uno solo para todo el juego, en el Canvas del HUD). Lo arma el menu
// Pomelo -> Dialogos -> Armar cuadro de dialogo  (Assets/Editor/ArmarCuadroDialogo.cs).
// Lo abre un componente Dialogo (en un objeto o personaje) con su lista de lineas:
//  - el nombre de quien habla (Alegreya Bold) y el texto (Alegreya Regular), que aparece letra por letra;
//  - si la linea tiene ilustracion, se ve detras del cuadro (pantalla completa); si no, se ve la escena;
//  - flecha derecha (o D, o la cruceta) avanza: si el texto se esta escribiendo lo completa, si no pasa a la
//    siguiente linea; flecha izquierda (o A) vuelve a la anterior;
//  - arriba a la derecha: » si quedan mas lineas, el tilde si es la ultima. En la ultima, avanzar cierra.
// Mientras esta abierto la chica no se mueve ni ataca (Abierto); el resto del juego sigue.
[RequireComponent(typeof(CanvasGroup))]
public class CuadroDialogo : MonoBehaviour
{
    public static CuadroDialogo Instancia { get; private set; }
    // la chica se queda quieta mientras hay un dialogo abierto
    public static bool Abierto => Instancia != null && Instancia.dialogo != null;

    [Header("Piezas (las pone el menu que arma el cuadro)")]
    [SerializeField] private RectTransform cuadro;
    [SerializeField] private TMP_Text nombre;
    [SerializeField] private TMP_Text texto;
    [SerializeField] private Image iconoSeguir;
    [SerializeField] private Image iconoFin;
    [SerializeField] private Image ilustracion;
    [SerializeField] private CanvasGroup grupoIlustracion;

    [Header("Ritmo")]
    [Tooltip("Letras por segundo que aparecen.")]
    [SerializeField] private float letrasPorSegundo = 40f;
    [Tooltip("Segundos que tarda en aparecer y desaparecer el cuadro.")]
    [SerializeField] private float segundosFundido = 0.2f;
    [Tooltip("Segundos que tarda en cambiar la ilustracion de fondo.")]
    [SerializeField] private float segundosIlustracion = 0.35f;

    [Header("Teclas")]
    [SerializeField] private Key teclaSiguiente = Key.RightArrow;
    [SerializeField] private Key teclaSiguiente2 = Key.D;
    [SerializeField] private Key teclaAnterior = Key.LeftArrow;
    [SerializeField] private Key teclaAnterior2 = Key.A;

    private CanvasGroup grupo;
    private Dialogo dialogo;
    private int linea;
    private float letras;          // letras visibles (con decimales: va sumando de a poco)
    private int totalLetras;
    private int cuadroAbierto = -1; // cuadro (frame) en que se abrio: ese cuadro no cuenta la tecla
    private Coroutine fundido, fundidoIlustracion;
    private float? bordeArribaTexto;  // donde empieza el texto cuando hay nombre (como quedo acomodado en la escena)

    private void Awake()
    {
        Instancia = this;
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (grupoIlustracion != null) grupoIlustracion.alpha = 0f;
        // sin kerning: los pares importados de Alegreya vienen exagerados y separan / pegan letras al azar
        foreach (var t in GetComponentsInChildren<TMP_Text>(true)) t.fontFeatures.Clear();
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // Lo llama un Dialogo cuando la chica interactua.
    public bool Abrir(Dialogo d)
    {
        if (d == null || d.Lineas == null || d.Lineas.Length == 0 || Abierto) return false;
        dialogo = d;
        cuadroAbierto = Time.frameCount;
        MostrarLinea(0);
        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(Fundir(grupo, 1f, segundosFundido));
        return true;
    }

    public void Cerrar()
    {
        if (dialogo == null) return;
        var terminado = dialogo;
        dialogo = null;
        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(Fundir(grupo, 0f, segundosFundido));
        CambiarIlustracion(null);
        terminado.AlTerminar();
    }

    private void Update()
    {
        if (dialogo == null || MenuPausa.BloqueaControles) return;

        // el texto aparece letra por letra
        if (letras < totalLetras)
        {
            letras = Mathf.Min(totalLetras, letras + letrasPorSegundo * Time.deltaTime);
            texto.maxVisibleCharacters = Mathf.FloorToInt(letras);
        }
        bool escrito = letras >= totalLetras;
        bool ultima = linea >= dialogo.Lineas.Length - 1;
        if (iconoSeguir != null) iconoSeguir.enabled = escrito && !ultima;
        if (iconoFin != null) iconoFin.enabled = escrito && ultima;

        if (Time.frameCount == cuadroAbierto) return;
        var teclado = Keyboard.current;
        bool siguiente = (teclado != null && (teclado[teclaSiguiente].wasPressedThisFrame || teclado[teclaSiguiente2].wasPressedThisFrame))
                         || Mando.DialogoSiguiente;
        bool anterior = (teclado != null && (teclado[teclaAnterior].wasPressedThisFrame || teclado[teclaAnterior2].wasPressedThisFrame))
                        || Mando.DialogoAnterior;

        if (siguiente)
        {
            if (!escrito) CompletarLinea();
            else if (ultima) Cerrar();
            else MostrarLinea(linea + 1);
        }
        else if (anterior && linea > 0)
        {
            MostrarLinea(linea - 1);
            CompletarLinea();   // lo que ya se leyo aparece entero
        }
    }

    private void MostrarLinea(int i)
    {
        linea = i;
        var l = dialogo.Lineas[i];
        bool conNombre = !l.sinNombre && !string.IsNullOrEmpty(l.personaje);
        if (nombre != null)
        {
            nombre.text = conNombre ? l.personaje : "";
            nombre.enabled = conNombre;
        }
        // sin nombre, el borde de arriba del texto sube hasta donde empieza el nombre (el resto queda igual:
        // respeta como se haya acomodado el texto y su alineacion)
        var rt = texto.rectTransform;
        if (bordeArribaTexto == null) bordeArribaTexto = rt.offsetMax.y;
        rt.offsetMax = new Vector2(rt.offsetMax.x, bordeArribaTexto.Value);
        if (!conNombre && nombre != null)
        {
            var rn = nombre.rectTransform;
            float arribaNombre = rn.localPosition.y + rn.rect.yMax;
            float arribaTexto = rt.localPosition.y + rt.rect.yMax;
            rt.offsetMax += new Vector2(0f, arribaNombre - arribaTexto);
        }
        texto.text = l.texto;
        texto.ForceMeshUpdate();
        totalLetras = texto.textInfo.characterCount;
        letras = 0f;
        texto.maxVisibleCharacters = 0;
        CambiarIlustracion(l.ilustracion);
    }

    private void CompletarLinea()
    {
        letras = totalLetras;
        texto.maxVisibleCharacters = totalLetras;
    }

    private void CambiarIlustracion(Sprite s)
    {
        if (ilustracion == null || grupoIlustracion == null) return;
        if (s != null && ilustracion.sprite == s && grupoIlustracion.alpha > 0.99f) return;
        if (fundidoIlustracion != null) StopCoroutine(fundidoIlustracion);
        fundidoIlustracion = StartCoroutine(Ilustrar(s));
    }

    // apaga la ilustracion que habia y enciende la nueva (o se queda apagada si la linea no tiene)
    private IEnumerator Ilustrar(Sprite s)
    {
        bool habia = grupoIlustracion.alpha > 0.01f;
        if (habia && ilustracion.sprite != s) yield return Fundir(grupoIlustracion, 0f, segundosIlustracion * 0.5f);
        if (s == null) { yield return Fundir(grupoIlustracion, 0f, segundosIlustracion * 0.5f); yield break; }
        ilustracion.sprite = s;
        yield return Fundir(grupoIlustracion, 1f, habia ? segundosIlustracion * 0.5f : segundosIlustracion);
    }

    private static IEnumerator Fundir(CanvasGroup g, float hasta, float tiempo)
    {
        float desde = g.alpha;
        for (float t = 0f; t < tiempo; t += Time.unscaledDeltaTime)
        {
            g.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / tiempo));
            yield return null;
        }
        g.alpha = hasta;
    }
}
