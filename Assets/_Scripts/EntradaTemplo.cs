using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla "Ingresaste al Templo de botanica" (Figma, Entrada_templo_botanica). La abre la puerta dorada de la parte 2
// de Inicio_introduccion (PuertaDelTemplo) cuando la chica ya entro y la puerta bajo. Se anima igual que la
// Estadistica final: primero aparece el fondo (el templo oscurecido con su gradiente) y despues, una por una, las
// partes, subiendo un poquito mientras aparecen: el emblema, "Ingresaste al / Templo de botanica", el ornamento y los
// dos textos.
// Cuando ya aparecio todo, cualquier tecla (o el boton A / X del mando) sigue: se carga el Nivel 1 detras de la
// pantalla y, ya cargado, la pantalla se desvanece y queda el comienzo del nivel (sin fundido a negro).
// Si no se toca nada, sigue sola a los 'segundosHastaSeguirSola'.
// La arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción  (ArmarEntradaTemplo, en la carpeta Editor de
// al lado). Despues se puede retocar a mano: este componente solo usa las referencias.
[RequireComponent(typeof(CanvasGroup))]
public class EntradaTemplo : MonoBehaviour
{
    public static bool Visible { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    [Header("Piezas (las pone el menu que arma la pantalla)")]
    [SerializeField] private CanvasGroup fondo;
    [Tooltip("Partes que aparecen una por una, en este orden.")]
    [SerializeField] private CanvasGroup[] partes;

    [Header("Cuando aparece")]
    [Tooltip("Segundos desde que la puerta termina de bajar.")]
    [SerializeField] private float espera = 0.1f;
    [SerializeField] private float segundosFondo = 0.6f;
    [Tooltip("Segundos entre que aparece una parte y la siguiente.")]
    [SerializeField] private float entrePartes = 0.25f;
    [SerializeField] private float segundosParte = 0.5f;
    [Tooltip("Cuanto sube cada parte mientras aparece (px de la pantalla de 1920 x 1080).")]
    [SerializeField] private float subida = 14f;

    [Header("Despues")]
    [Tooltip("Segundos (despues de que aparece todo) en que todavia no se puede seguir: asi no se saltea sin querer.")]
    [SerializeField] private float minimoAntesDeSeguir = 0.5f;
    [Tooltip("Segundos para leerla (desde que aparecio todo): despues sigue sola (0 = espera a que se toque algo).")]
    [SerializeField] private float segundosHastaSeguirSola = 3.5f;
    [Tooltip("Segundos que la pantalla se queda sobre el nivel ya cargado antes de irse.")]
    [SerializeField] private float esperaEnElNivel = 0f;
    [Tooltip("Segundos que tarda la pantalla en desvanecerse y dejar ver el nivel.")]
    [SerializeField] private float segundosSalida = 0.6f;
    [Tooltip("Escena que se carga al seguir (tiene que estar en la lista del build). Vacio = se queda en la pantalla.")]
    [SerializeField] private string escenaSiguiente = "Nivel1_Templo";

    private CanvasGroup grupo;
    private bool mostrado;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (fondo != null) fondo.alpha = 0f;
        if (partes != null) foreach (var p in partes) if (p != null) p.alpha = 0f;
        // sin kerning: los pares importados de Alegreya vienen exagerados y separan / pegan letras al azar
        foreach (var t in GetComponentsInChildren<TMP_Text>(true)) t.fontFeatures.Clear();
        Visible = false;
    }

    private void OnDestroy()
    {
        if (mostrado) { Visible = false; QuietaPorEscena.Soltar(); }
    }

    // La llama la puerta (PuertaDelTemplo). Tambien se puede llamar a mano para probarla.
    [ContextMenu("Mostrar")]
    public void MostrarAhora()
    {
        if (mostrado) return;
        mostrado = true;
        QuietaPorEscena.Frenar();   // la chica se queda quieta mientras se ve la pantalla
        if (mostrarDebug) Debug.Log("[Entrada templo] aparece la pantalla");
        StartCoroutine(Mostrar());
    }

    private IEnumerator Mostrar()
    {
        // el nivel se va cargando mientras se lee la pantalla (sin activarse): al seguir, el cambio es inmediato
        AsyncOperation carga = null;
        if (!string.IsNullOrEmpty(escenaSiguiente))
        {
            carga = SceneManager.LoadSceneAsync(escenaSiguiente);
            if (carga != null) carga.allowSceneActivation = false;
        }

        yield return new WaitForSecondsRealtime(espera);
        Visible = true;
        grupo.alpha = 1f;
        grupo.blocksRaycasts = true;
        if (fondo != null) yield return Fundir(fondo, null, 0f, segundosFondo);

        if (partes != null)
            foreach (var p in partes)
            {
                if (p == null) continue;
                StartCoroutine(Fundir(p, p.transform as RectTransform, subida, segundosParte));
                yield return new WaitForSecondsRealtime(entrePartes);
            }
        yield return new WaitForSecondsRealtime(segundosParte);

        // espera a que se toque algo (o sigue sola)
        for (float t = 0f; ; t += Time.unscaledDeltaTime)
        {
            if (t >= minimoAntesDeSeguir && Seguir()) break;
            if (segundosHastaSeguirSola > 0f && t >= segundosHastaSeguirSola) break;
            yield return null;
        }
        if (carga == null) yield break;

        // la pantalla pasa al nivel con ella (sin fundido a negro): el nivel (ya cargado detras) se activa y la pantalla
        // se desvanece y deja ver su comienzo
        if (mostrarDebug) Debug.Log("[Entrada templo] pasando a " + escenaSiguiente);
        SeparDelCanvas();
        DontDestroyOnLoad(gameObject);
        carga.allowSceneActivation = true;
        while (!carga.isDone) yield return null;
        QuietaPorEscena.Frenar();   // (al cargar la escena se suelta sola) la chica espera a que se vaya la pantalla
        yield return new WaitForSecondsRealtime(esperaEnElNivel);

        grupo.blocksRaycasts = false;
        for (float t = 0f; t < segundosSalida; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / segundosSalida);
            yield return null;
        }
        grupo.alpha = 0f;
        Visible = false;
        QuietaPorEscena.Soltar();
        mostrado = false;
        Destroy(gameObject);
    }

    // la pantalla vive dentro del Canvas de la intro: para pasar al nivel tiene que ser su propio Canvas (con el mismo
    // escalado de 1920 x 1080), por encima de todo lo del nivel
    private void SeparDelCanvas()
    {
        transform.SetParent(null, false);
        var c = GetComponent<Canvas>();
        if (c == null) c = gameObject.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.overrideSorting = false;
        c.sortingOrder = 500;
        var escala = GetComponent<CanvasScaler>();
        if (escala == null) escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1920f, 1080f);
        escala.matchWidthOrHeight = 0.5f;
        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
    }

    private static bool Seguir()
    {
        if (MenuPausa.BloqueaControles) return false;
        var teclado = Keyboard.current;
        if (teclado != null && teclado.anyKey.wasPressedThisFrame && !teclado.escapeKey.wasPressedThisFrame) return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        return Mando.Aceptar || Mando.Interactuar;
    }

    // aparece: de transparente a opaco, subiendo un poquito hasta su lugar
    private static IEnumerator Fundir(CanvasGroup g, RectTransform rt, float subida, float tiempo)
    {
        Vector2 lugar = rt != null ? rt.anchoredPosition : Vector2.zero;
        for (float t = 0f; t < tiempo; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / tiempo);
            g.alpha = k;
            if (rt != null) rt.anchoredPosition = lugar + Vector2.down * subida * (1f - k);
            yield return null;
        }
        g.alpha = 1f;
        if (rt != null) rt.anchoredPosition = lugar;
    }
}
