using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aviso en pantalla cuando la chica toma un checkpoint (spawnpoint), como en los juegos.
// Formato (el de Caro, Assets/_UI/Spawnpoint):
//   - arriba, el titulo "NUEVO CHECKPOINT" (Checkpoint_titulo: mayusculas con un brillo dorado suave)
//   - abajo, el ornamento (Checkpoint_ornamento: volutas y la flor del medio, blanco translucido)
//   las dos imagenes estan dibujadas a la misma escala y se muestran juntas al 68 %
// Ubicacion: centrado en x con el icono del checkpoint (lo sigue si la camara se mueve) y siempre a la misma
// altura de la pantalla, a la altura del piso.
// Entra subiendo un poquito con un fundido, se queda unos segundos y se va. Usa el tiempo del juego: en pausa
// se queda quieto. Lo arma y conecta el menu Pomelo -> Avisos -> Armar aviso de checkpoint.
public class AvisoCheckpoint : MonoBehaviour
{
    [Header("Imagenes (Assets/_UI/Spawnpoint)")]
    [SerializeField] private Sprite titulo;
    [SerializeField] private Sprite ornamento;
    [Tooltip("Tamano de las imagenes (0.68 = como en la captura de Caro).")]
    [SerializeField] private float escala = 0.68f;
    [Tooltip("Distancia entre el centro del titulo y el centro del ornamento (px de una pantalla Full HD).")]
    [SerializeField] private float distanciaTituloOrnamento = 44f;

    [Header("Ubicacion")]
    [Tooltip("Altura fija del centro del aviso, medida desde el borde de abajo de la pantalla (px de una pantalla " +
             "Full HD). En x va siempre centrado con el icono del checkpoint.")]
    [SerializeField] private float alturaDesdeAbajo = 296f;   // la linea roja de Caro, 4 px mas abajo

    [Header("Si falta la imagen del titulo, se escribe con la fuente")]
    [SerializeField] private TMP_FontAsset fuente;
    [SerializeField] private string texto = "Nuevo checkpoint";
    [SerializeField] private float tamanoTexto = 34f;

    [Header("Tiempos")]
    // en total 1.5 s (lo que pidio Caro)
    [SerializeField] private float duracionEntrada = 0.25f;
    [SerializeField] private float duracionQuieto = 0.9f;
    [SerializeField] private float duracionSalida = 0.35f;
    [Tooltip("Cuanto sube al entrar (px).")]
    [SerializeField] private float subida = 14f;

    private static AvisoCheckpoint instancia;

    private RectTransform lienzo;
    private RectTransform caja;
    private CanvasGroup grupo;
    private Vector3 lugarIcono;    // centro del icono del checkpoint en el mundo
    private float inicio = -1f;

    // Lo llama el checkpoint al tomarlo. 'lugar' es el centro del icono en el mundo.
    public static void Mostrar(Sprite dibujo, Vector3 lugar)
    {
        if (instancia == null)
        {
            Debug.LogWarning("No hay aviso de checkpoint en la escena: correr Pomelo -> Avisos -> Armar aviso de checkpoint");
            return;
        }
        instancia.lugarIcono = lugar;
        instancia.inicio = Time.time;
        instancia.FijarLugar();
        instancia.Ubicar(-instancia.subida);   // ya en su lugar desde el primer cuadro
    }

    private void Awake()
    {
        instancia = this;
        Armar();
        grupo.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        if (inicio < 0f) return;
        float t = Time.time - inicio;
        float alfa, y = 0f;
        if (t < duracionEntrada)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracionEntrada);
            alfa = k;
            y = -subida * (1f - k);
        }
        else if (t < duracionEntrada + duracionQuieto) alfa = 1f;
        else
        {
            float k = Mathf.Clamp01((t - duracionEntrada - duracionQuieto) / duracionSalida);
            alfa = 1f - Mathf.SmoothStep(0f, 1f, k);
            if (k >= 1f) inicio = -1f;
        }
        grupo.alpha = alfa;
        Ubicar(y);
    }

    // Al tomar el checkpoint se elige el lugar del aviso EN EL MUNDO: en x, centrado con el icono; en y, el punto
    // que en ese momento esta a 'alturaDesdeAbajo' de la pantalla. Desde ahi queda clavado en ese lugar del
    // mundo, en x y en y: si la chica salta y la camara sube o baja, el aviso no la sigue.
    private Vector3 lugarAviso;
    private Camera camaraJuego;

    private void FijarLugar()
    {
        var cam = CamaraJuego();
        if (cam == null) { lugarAviso = lugarIcono; return; }
        Vector3 icono = cam.WorldToScreenPoint(lugarIcono);
        float yPantalla = alturaDesdeAbajo * Screen.height / 1080f;
        lugarAviso = cam.ScreenToWorldPoint(new Vector3(icono.x, yPantalla, icono.z));
    }

    // la camara que sigue a la chica (en la escena no tiene la etiqueta MainCamera, asi que Camera.main no la encuentra)
    private Camera CamaraJuego()
    {
        if (camaraJuego != null && camaraJuego.isActiveAndEnabled) return camaraJuego;
        var seguidora = FindFirstObjectByType<CamaraSeguidora>();
        camaraJuego = seguidora != null ? seguidora.GetComponent<Camera>() : Camera.main;
        if (camaraJuego == null && Camera.allCamerasCount > 0) camaraJuego = Camera.allCameras[0];
        return camaraJuego;
    }

    private void Ubicar(float subidaPx)
    {
        var cam = CamaraJuego();
        if (cam == null || lienzo == null) return;
        float escalaPantalla = Screen.height / 1080f;
        Vector3 aviso = cam.WorldToScreenPoint(lugarAviso);   // el lugar fijo del mundo, visto desde la camara de ahora
        var pantalla = new Vector2(aviso.x, aviso.y + subidaPx * escalaPantalla);
        var lienzoRaiz = GetComponentInParent<Canvas>()?.rootCanvas;
        Camera camLienzo = lienzoRaiz != null && lienzoRaiz.renderMode != RenderMode.ScreenSpaceOverlay ? lienzoRaiz.worldCamera : null;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(lienzo, pantalla, camLienzo, out var mundo))
            caja.position = mundo;
    }

    // ---------- armado (en tiempo de juego, asi no hay que tocar la escena) ----------

    private void Armar()
    {
        var canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;   // arriba del juego y del HUD, abajo del menu de pausa (100)
        var escalador = gameObject.GetComponent<CanvasScaler>();
        if (escalador == null) escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        lienzo = transform as RectTransform;
        caja = Nuevo("Aviso", lienzo);
        grupo = caja.gameObject.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // titulo arriba y ornamento abajo, con el centro del conjunto en el medio de la caja
        float arriba = ornamento != null ? distanciaTituloOrnamento / 2f : 0f;
        if (titulo != null) Imagen("Titulo", titulo, arriba);
        else Texto(arriba);
        if (ornamento != null) Imagen("Ornamento", ornamento, -distanciaTituloOrnamento / 2f);
    }

    private void Imagen(string nombre, Sprite sprite, float y)
    {
        var rt = Nuevo(nombre, caja);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        rt.sizeDelta = sprite.rect.size * escala;
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private void Texto(float y)
    {
        var rt = Nuevo("Titulo", caja);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) { t.font = fuente; t.fontSharedMaterial = fuente.material; }
        t.text = texto.ToUpperInvariant();
        t.fontSize = tamanoTexto;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        rt.sizeDelta = new Vector2(600f, tamanoTexto * 1.5f);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static RectTransform Nuevo(string nombre, RectTransform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }
}
