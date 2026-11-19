using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Pantalla "Instruccion ataque" (Figma, pagina PANTALLAS LORE): explica los dos golpes. La abre la runa del
// conocimiento al terminar su dialogo (Dialogo -> Al Terminar -> InstruccionAtaque.Mostrar).
// Es interactiva:
//  - el golpe elegido se ve blanco y con brillo; el otro, al 60 %;
//  - click izquierdo (o ○): el brillo del golpe elegido se agranda bien visible y vuelve;
//  - B (o R1 / R2): cambia de golpe (tambien en el juego: SelectorAtaque);
//  - flecha derecha / D (o el joystick derecho hacia la derecha, o la cruceta): continuar.
// Mientras esta abierta la chica queda quieta (QuietaPorEscena).
// La arma el menu  Pomelo -> Pantallas -> Armar instruccion ataque  (Assets/Editor/ArmarInstruccionAtaque.cs).
[RequireComponent(typeof(CanvasGroup))]
public class InstruccionAtaque : MonoBehaviour
{
    public static bool Abierta { get; private set; }

    [System.Serializable]
    public class Golpe
    {
        [Tooltip("Todo lo del golpe (dibujo, brillo y nombre): baja al 60 % cuando no esta elegido.")]
        public CanvasGroup grupo;
        public RectTransform dibujo;
        [Tooltip("El brillo detras del dibujo.")]
        public UnityEngine.UI.Image brillo;
    }

    [Header("Piezas (las pone el menu que arma la pantalla)")]
    [SerializeField] private Golpe latigo;
    [SerializeField] private Golpe sabiduria;

    [Header("Fondo")]
    [Tooltip("La imagen de fondo de pantalla completa.")]
    [SerializeField] private UnityEngine.UI.Image fondo;
    [Tooltip("Color del fondo (Figma: #020804). Se ve al cambiarlo, tambien sin darle Play.")]
    [SerializeField] private Color colorFondo = new Color(0.0082f, 0.0312f, 0.0157f, 1f);

    [Header("Aspecto")]
    [Tooltip("Opacidad del golpe que no esta elegido (Figma: 60 %).")]
    [Range(0f, 1f)] [SerializeField] private float alfaNoElegido = 0.6f;
    [Tooltip("Opacidad del brillo (drop shadow blanco) del golpe elegido, quieto.")]
    [Range(0f, 1f)] [SerializeField] private float alfaBrillo = 0.45f;
    [Tooltip("Segundos que tarda en pasar de un golpe al otro (B).")]
    [SerializeField] private float segundosCambio = 0.1f;
    [SerializeField] private float segundosFundido = 0.35f;

    [Header("Click: el brillo se intensifica")]
    [Tooltip("Opacidad del brillo en lo mas fuerte del click (no crece: solo se intensifica).")]
    [Range(0f, 1f)] [SerializeField] private float alfaBrilloClick = 1f;
    [Tooltip("Segundos que dura el destello del click (sube rapido y baja suave).")]
    [SerializeField] private float segundosClick = 0.3f;

    [Header("Continuar")]
    [Tooltip("Segundos al abrir en que no se puede continuar (asi la tecla que cerro el dialogo no la saltea).")]
    [SerializeField] private float esperaParaContinuar = 0.6f;

    private CanvasGroup grupo;
    private float elegido;          // 0 = latigo, 1 = sabiduria (con decimales mientras cambia)
    private float pulso = -1f;      // tiempo desde el ultimo click (-1 = sin pulso)
    private float abiertaDesde;
    private Coroutine fundido;
    private bool cerrando;
    private bool estaAbierta;   // esta copia (si hubiera otra pantalla en la escena, no responde a las teclas)

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true)) t.fontFeatures.Clear();   // sin kerning (Alegreya)
        PintarFondo();
        Abierta = false;
    }

    private void OnValidate() => PintarFondo();

    private void PintarFondo()
    {
        if (fondo != null) fondo.color = colorFondo;
    }

    private void OnDisable()
    {
        if (estaAbierta) { Abierta = false; estaAbierta = false; QuietaPorEscena.Soltar(); }
    }

    // Lo llama el dialogo de la runa al terminar.
    [ContextMenu("Mostrar")]
    public void Mostrar()
    {
        if (Abierta) return;
        Abierta = true;
        estaAbierta = true;
        cerrando = false;
        abiertaDesde = Time.unscaledTime;
        QuietaPorEscena.Frenar();
        elegido = SelectorAtaque.Elegido == SelectorAtaque.Ataque.Latigo ? 0f : 1f;
        pulso = -1f;
        Pintar();
        grupo.blocksRaycasts = true;
        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(Fundir(1f));
    }

    public void Cerrar()
    {
        if (!Abierta || cerrando) return;
        cerrando = true;
        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(CerrarDeAPoco());
    }

    private IEnumerator CerrarDeAPoco()
    {
        yield return Fundir(0f);
        grupo.blocksRaycasts = false;
        Abierta = false;
        estaAbierta = false;
        QuietaPorEscena.Soltar();
    }

    private void Update()
    {
        if (!estaAbierta) return;
        // (Estefi, 19/11) mientras se ve, la chica queda quieta aunque otra cosa la suelte (el Lore de la tablilla la
        // soltaba al cerrarse el dialogo): si no, la B tambien cambiaba el golpe en el juego (se anulaban) y el click
        // la hacia atacar detras de la pantalla
        if (!cerrando && !QuietaPorEscena.Activa) QuietaPorEscena.Frenar();
        if (!cerrando && !MenuPausa.BloqueaControles)
        {
            var teclado = Keyboard.current;
            var mouse = Mouse.current;
            var mando = Gamepad.current;

            if ((teclado != null && teclado.bKey.wasPressedThisFrame) || Mando.CambiarAtaque)
            {
                var nuevo = SelectorAtaque.Elegido == SelectorAtaque.Ataque.Latigo ? SelectorAtaque.Ataque.Sabiduria : SelectorAtaque.Ataque.Latigo;
                SelectorAtaque.Elegir(nuevo);
                pulso = -1f;
            }
            if ((mouse != null && mouse.leftButton.wasPressedThisFrame) || Mando.Atacar) pulso = 0f;

            bool continuar = (teclado != null && (teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame))
                             || Mando.DialogoSiguiente
                             || (mando != null && mando.rightStick.right.wasPressedThisFrame);
            if (continuar && Time.unscaledTime - abiertaDesde >= esperaParaContinuar) Cerrar();
        }

        float objetivo = SelectorAtaque.Elegido == SelectorAtaque.Ataque.Latigo ? 0f : 1f;
        float paso = segundosCambio > 0f ? Time.unscaledDeltaTime / segundosCambio : 1f;
        elegido = Mathf.MoveTowards(elegido, objetivo, paso);
        if (pulso >= 0f) { pulso += Time.unscaledDeltaTime; if (pulso >= segundosClick) pulso = -1f; }
        Pintar();
    }

    private void Pintar()
    {
        float k = Mathf.SmoothStep(0f, 1f, elegido);
        // el pulso del click: sube rapido (el primer 25 %) y baja suave (0 -> 1 -> 0)
        float p = 0f;
        if (pulso >= 0f)
        {
            float u = Mathf.Clamp01(pulso / segundosClick);
            p = u < 0.25f ? Mathf.SmoothStep(0f, 1f, u / 0.25f) : 1f - Mathf.SmoothStep(0f, 1f, (u - 0.25f) / 0.75f);
        }
        PintarGolpe(latigo, 1f - k, SelectorAtaque.Elegido == SelectorAtaque.Ataque.Latigo ? p : 0f);
        PintarGolpe(sabiduria, k, SelectorAtaque.Elegido == SelectorAtaque.Ataque.Sabiduria ? p : 0f);
    }

    // cuanto: 1 = elegido, 0 = no elegido
    private void PintarGolpe(Golpe g, float cuanto, float p)
    {
        if (g == null) return;
        if (g.grupo != null) g.grupo.alpha = Mathf.Lerp(alfaNoElegido, 1f, cuanto);
        // el brillo no crece: solo se intensifica (mismo tamano, mas opaco)
        if (g.brillo != null) g.brillo.color = new Color(1f, 1f, 1f, Mathf.Lerp(alfaBrillo * cuanto, alfaBrilloClick, p));
    }

    private IEnumerator Fundir(float hasta)
    {
        float desde = grupo.alpha;
        for (float t = 0f; t < segundosFundido; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / segundosFundido));
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
