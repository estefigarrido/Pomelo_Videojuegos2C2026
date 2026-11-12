using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menu de pausa (Assets/_UI/Pausa). Se abre y se cierra con Esc (teclado) o Options / Start (joystick).
// Mientras esta abierto el juego se frena (Time.timeScale = 0) y los controles del juego no responden.
// Al abrirse aparece con un fundido mientras se acomoda desde apenas mas grande; al cerrarse se desvanece.
//  - Siempre arranca con "Continuar" elegido. El boton elegido se funde a Boton_hover, crece un poquito,
//    tiene una sombra detras y muestra el icono de como aceptar (mouse / X cada 2.5 s, AlternarTeclas).
//    Texto + icono quedan centrados, con el mismo espacio entre los dos en todos los botones.
//    Al pasar de un boton a otro, todo se anima en "duracionCambio" segundos.
//  - Cambiar de boton: rueda del mouse arriba / abajo, stick derecho arriba / abajo (tambien el izquierdo
//    y la cruceta), flechas o W / S, o pasando el mouse por encima.
//  - Aceptar: clic izquierdo, X (Cruz), Enter o Espacio.  Volver al juego: Esc u Options (el Circulo no hace nada).
//  - Botones: Continuar, Nuevo juego (reinicia el nivel), Configuracion (todavia no tiene pantalla),
//    Salir del juego.
// Lo arma el menu  Pomelo -> Pausa -> Armar menu de pausa  (Assets/Editor/ArmarMenuPausa.cs).
public class MenuPausa : MonoBehaviour
{
    public enum Accion { Continuar, NuevoJuego, Configuracion, Salir }

    [System.Serializable]
    public class Boton
    {
        public Accion accion;
        [Tooltip("Boton_normal (siempre se ve).")]
        public Image fondo;
        [Tooltip("Boton_hover, encima del normal: se funde cuando el boton esta elegido.")]
        public Image hover;
        [Tooltip("Sombra suave detras del boton: aparece cuando esta elegido.")]
        public Image sombra;
        public TMP_Text texto;
        [Tooltip("Iconos de como aceptar (mouse / X). Solo se ven en el boton elegido.")]
        public GameObject icono;

        [System.NonSerialized] public float peso;          // 0 = normal, 1 = elegido (animado)
        [System.NonSerialized] public float xTexto, xIcono; // donde van texto e icono cuando esta elegido
        [System.NonSerialized] public CanvasGroup grupoIcono;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private Boton[] botones;

    [Header("Boton elegido")]
    [Tooltip("Espacio entre el texto y el icono (px), como en CONTINUAR del Figma.")]
    [SerializeField] private float espacioTextoIcono = 23f;
    [Tooltip("Ancho de lo que se ve del icono (px).")]
    [SerializeField] private float anchoIcono = 28f;
    [Tooltip("Cuanto crece el boton elegido (1.04 = 4 % mas grande).")]
    [SerializeField] private float escalaElegido = 1.04f;
    [Tooltip("Que tan oscura es la sombra del boton elegido (0 a 1).")]
    [SerializeField] private float alfaSombra = 0.75f;
    [Tooltip("Segundos que tarda en pasar de un boton al otro (fundido, crecer, correr el texto).")]
    [SerializeField] private float duracionCambio = 0.18f;

    [Header("Sonido")]
    [Tooltip("Suena cada vez que se pasa a otro boton (Assets/_MUSICA/Pausa_cambio_boton.wav).")]
    [SerializeField] private AudioClip sonidoCambio;
    [Range(0f, 1f)]
    [SerializeField] private float volumenCambio = 0.6f;

    [Header("Abrir y cerrar")]
    [Tooltip("Segundos que tarda en aparecer el menu (fundido + se acomoda desde un poquito mas grande).")]
    [SerializeField] private float duracionAbrir = 0.3f;
    [Tooltip("Segundos que tarda en irse el menu.")]
    [SerializeField] private float duracionCerrar = 0.2f;
    [Tooltip("Tamano desde el que arranca al abrirse (1.03 = 3 % mas grande) y se achica hasta su lugar.")]
    [SerializeField] private float escalaInicial = 1.03f;

    [Header("Controles")]
    [Tooltip("Con el stick apretado para un lado, cada cuanto pasa al boton siguiente (segundos).")]
    [SerializeField] private float repeticionStick = 0.28f;

    // los controles del juego (caminar, saltar, atacar...) no responden mientras esto es true
    public static bool BloqueaControles => Abierto || Time.frameCount == frameCierre;
    public static bool Abierto { get; private set; }
    private static int frameCierre = -1;

    private int elegido;
    private float proximoStick;
    private Vector2 ultimoMouse;
    private float escalaDeTiempoAntes = 1f;

    private AudioSource fuente;
    private CanvasGroup grupoPanel;
    private float visible;   // 0 = escondido, 1 = del todo a la vista (animado)

    private void Awake()
    {
        Abierto = false;
        if (panel != null)
        {
            grupoPanel = panel.GetComponent<CanvasGroup>();
            if (grupoPanel == null) grupoPanel = panel.AddComponent<CanvasGroup>();
            grupoPanel.interactable = false;
            grupoPanel.blocksRaycasts = false;
            grupoPanel.alpha = 0f;
            panel.SetActive(false);
        }

        // fuente propia para los sonidos del menu (suena aunque el juego este frenado)
        fuente = GetComponent<AudioSource>();
        if (fuente == null) fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.ignoreListenerPause = true;
    }

    private void OnDestroy()
    {
        // si se cambia de escena con el menu abierto, que el juego no quede frenado
        if (Abierto) { Abierto = false; Time.timeScale = escalaDeTiempoAntes; }
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        var mouse = Mouse.current;

        AnimarPanel(Time.unscaledDeltaTime);

        bool alternar = (teclado != null && teclado.escapeKey.wasPressedThisFrame) || Mando.Pausa;
        if (!Abierto)
        {
            if (alternar) Abrir();
            return;
        }
        if (alternar) { Cerrar(); return; }

        // ---- elegir ----
        int mover = 0;
        if (mouse != null)
        {
            float rueda = mouse.scroll.ReadValue().y;
            if (rueda > 0.01f) mover = -1;
            else if (rueda < -0.01f) mover = 1;
        }
        if (teclado != null)
        {
            if (teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame) mover = -1;
            if (teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame) mover = 1;
        }
        float stick = Mando.Vertical();
        if (stick == 0f) proximoStick = 0f;
        else if (Time.unscaledTime >= proximoStick)
        {
            mover = (int)stick;
            proximoStick = Time.unscaledTime + repeticionStick;
        }
        if (mover != 0) Elegir(Mathf.Clamp(elegido + mover, 0, botones.Length - 1));

        // el mouse elige el boton que tiene abajo (solo si se movio, asi no le gana a la rueda o al stick)
        int bajoElMouse = -1;
        if (mouse != null)
        {
            Vector2 pos = mouse.position.ReadValue();
            bajoElMouse = BotonEn(pos);
            if ((pos - ultimoMouse).sqrMagnitude > 1f && bajoElMouse >= 0 && bajoElMouse != elegido) Elegir(bajoElMouse);
            ultimoMouse = pos;
        }

        // ---- aceptar ----
        bool clic = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool aceptar = Mando.Aceptar ||
                       (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame ||
                                            teclado.spaceKey.wasPressedThisFrame));
        if (clic && bajoElMouse >= 0) { Elegir(bajoElMouse); aceptar = true; }
        if (aceptar) { Hacer(botones[elegido].accion); if (!Abierto) return; }

        Animar(Time.unscaledDeltaTime);
    }

    // cada boton va de a poco hacia normal o elegido: fundido al hover, sombra, tamano, texto e icono
    private void Animar(float dt)
    {
        float paso = duracionCambio > 0f ? dt / duracionCambio : 1f;
        for (int k = 0; k < botones.Length; k++)
        {
            var b = botones[k];
            b.peso = Mathf.MoveTowards(b.peso, k == elegido ? 1f : 0f, paso);
            Pintar(b);
        }
    }

    private void Pintar(Boton b)
    {
        float e = Mathf.SmoothStep(0f, 1f, b.peso);
        float escala = Mathf.Lerp(1f, escalaElegido, e);
        if (b.fondo != null) b.fondo.rectTransform.localScale = new Vector3(escala, escala, 1f);
        if (b.hover != null) b.hover.color = new Color(1f, 1f, 1f, e);
        if (b.sombra != null)
        {
            b.sombra.color = new Color(0f, 0f, 0f, alfaSombra * e);
            b.sombra.rectTransform.localScale = new Vector3(escala, escala, 1f);
        }
        if (b.texto != null)
        {
            var p = b.texto.rectTransform.anchoredPosition;
            b.texto.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(0f, b.xTexto, e), p.y);
        }
        if (b.icono != null)
        {
            bool ver = e > 0.01f;
            if (b.icono.activeSelf != ver) b.icono.SetActive(ver);   // al prenderse arranca con el mouse
            if (b.grupoIcono != null) b.grupoIcono.alpha = e;
            var rt = (RectTransform)b.icono.transform;
            rt.anchoredPosition = new Vector2(b.xIcono, rt.anchoredPosition.y);
        }
    }

    // texto + espacio + icono quedan centrados en el boton, con el mismo espacio para todos
    private void MedirBotones()
    {
        foreach (var b in botones)
        {
            float anchoTexto = b.texto != null ? b.texto.GetPreferredValues(b.texto.text).x : 0f;
            float total = anchoTexto + espacioTextoIcono + anchoIcono;
            b.xTexto = -total / 2f + anchoTexto / 2f;
            b.xIcono = total / 2f - anchoIcono / 2f;
            if (b.icono != null) b.grupoIcono = b.icono.GetComponent<CanvasGroup>();
        }
    }

    public void Abrir()
    {
        if (Abierto) return;
        Abierto = true;
        escalaDeTiempoAntes = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
        if (Mouse.current != null) ultimoMouse = Mouse.current.position.ReadValue();
        MedirBotones();
        // siempre arranca en Continuar, ya elegido (sin animacion ni sonido al abrir)
        elegido = 0;
        for (int k = 0; k < botones.Length; k++) { botones[k].peso = k == 0 ? 1f : 0f; Pintar(botones[k]); }
    }

    public void Cerrar()
    {
        if (!Abierto) return;
        Abierto = false;
        frameCierre = Time.frameCount;   // el mismo toque que cierra no salta ni ataca
        Time.timeScale = escalaDeTiempoAntes;
        // el panel se desvanece en AnimarPanel y recien ahi se apaga
    }

    // aparece con un fundido mientras se achica apenas hasta su lugar; al cerrarse, se desvanece
    private void AnimarPanel(float dt)
    {
        if (panel == null || grupoPanel == null) return;
        float objetivo = Abierto ? 1f : 0f;
        float duracion = Abierto ? duracionAbrir : duracionCerrar;
        visible = Mathf.MoveTowards(visible, objetivo, duracion > 0f ? dt / duracion : 1f);

        float e = Mathf.SmoothStep(0f, 1f, visible);
        grupoPanel.alpha = e;
        float escala = Mathf.Lerp(escalaInicial, 1f, e);
        panel.transform.localScale = new Vector3(escala, escala, 1f);

        if (!Abierto && visible <= 0f && panel.activeSelf) panel.SetActive(false);
    }

    // el cambio visual lo hace Animar, de a poco; si cambio de boton, suena el "tic"
    private void Elegir(int i, bool conSonido = true)
    {
        if (i == elegido) return;
        elegido = i;
        if (conSonido && sonidoCambio != null && fuente != null)
        {
            fuente.pitch = 1f;   // siempre el mismo tono
            fuente.PlayOneShot(sonidoCambio, volumenCambio);
        }
    }

    private int BotonEn(Vector2 pantalla)
    {
        for (int k = 0; k < botones.Length; k++)
        {
            var f = botones[k].fondo;
            if (f != null && RectTransformUtility.RectangleContainsScreenPoint(f.rectTransform, pantalla, null)) return k;
        }
        return -1;
    }

    private void Hacer(Accion accion)
    {
        switch (accion)
        {
            case Accion.Continuar:
                Cerrar();
                break;
            case Accion.NuevoJuego:
                Abierto = false;
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            case Accion.Configuracion:
                Debug.Log("[Pausa] Configuracion: todavia no hay pantalla de configuracion.");
                break;
            case Accion.Salir:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}
