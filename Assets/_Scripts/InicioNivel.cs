using System.Collections;
using UnityEngine;

// Comienzo del nivel en el portal de entrada:
//  1) la chica esta adentro del portal, detras de la puerta cerrada, y no se puede controlar.
//     La camara se aleja (deszoom) para mostrar el portal y lo que lo rodea.
//  2) a los 3 segundos de empezar, la puerta sube (sin asomarse por arriba del portal) y desaparece;
//     a medida que sube se ve a la chica de frente (chica_salidaportal 000).
//  3) poco despues de que termina de abrirse, la chica se gira de perfil (chica_salidaportal),
//     la camara vuelve al zoom normal y recien ahi se le devuelven los controles: empieza el juego.
public class InicioNivel : MonoBehaviour
{
    [Tooltip("Apagalo para probar el nivel desde otro lado sin la intro.")]
    [SerializeField] private bool activa = true;
    [Tooltip("Solo en el editor: si al darle Play la chica esta a mas de esta distancia del portal, se saltea la intro " +
             "y arranca (y reaparece al morir) donde la dejaste. En el juego compilado no hace nada.")]
    [SerializeField] private float distanciaParaProbarDesdeOtroLado = 3f;

    [Header("Portal y puerta")]
    [SerializeField] private SpriteRenderer portal;
    [SerializeField] private SpriteRenderer puerta;

    [Header("La chica")]
    [Tooltip("Donde esta parada la chica adentro del portal.")]
    [SerializeField] private Vector2 posicionChica = new Vector2(-32f, -29f);
    [Tooltip("chica_salidaportal: 000 de frente ... el ultimo de perfil.")]
    [SerializeField] private Sprite[] framesSalida;
    [Tooltip("Cuanto tarda en girarse de perfil: pasa por los frames de la salida y termina en su pose de " +
             "perfil normal, todo fundido y con una sola curva suave. Antes 0.13 s, que se veia trabado.")]
    [SerializeField] private float duracionGiroSalida = 1f;
    [Tooltip("Cuanto tarda el ultimo frame del giro (de tres cuartos) en fundirse con la pose de perfil " +
             "cuando empieza el juego. 0 = cambio de golpe.")]
    [SerializeField] private float duracionFundidoPerfil = 0.4f;

    [Header("Sonido")]
    [Tooltip("Puerta de piedra que se levanta (Assets/_Sonidos/Puerta_piedra_subiendo.wav). El arrastre dura " +
             "1.75 s, lo mismo que la subida de la puerta (lo ajusta Pomelo -> Sonidos -> Conectar sonidos).")]
    [SerializeField] private AudioClip sonidoPuerta;
    [Range(0f, 1f)]
    [SerializeField] private float volumenPuerta = 0.6f;

    [Header("Tiempos")]
    [Tooltip("Segundos hasta que la puerta empieza a subir. Antes 5 s.")]
    [SerializeField] private float esperaInicial = 3f;
    [Tooltip("Lo que tarda la puerta en subir. Antes 2.5 s.")]
    [SerializeField] private float duracionSubidaPuerta = 1.5f;
    [Tooltip("Espera entre que la puerta termina de abrirse y la chica se gira. Antes 2 s.")]
    [SerializeField] private float esperaDespuesDeAbrir = 0.8f;

    [Header("Camara")]
    [Tooltip("Zoom de la intro (tamano de la camara ortografica; el normal es 5.4, mas grande = mas lejos). " +
             "6.7 muestra de la columna espiral hasta los arcos de la derecha, como el encuadre del Figma.")]
    [SerializeField] private float tamanoEncuadreIntro = 6.7f;
    [Tooltip("Centro del encuadre de la intro, en el mundo. (-25.86, -26.19) es el centro del encuadre del Figma " +
             "(x 12420 a 14570 px, y 9545 a 10881 px del frame del mapa): la columna espiral queda en el borde izquierdo.")]
    [SerializeField] private Vector2 centroEncuadreIntro = new Vector2(-25.86f, -26.19f);
    [Tooltip("Segundos que tarda en alejarse al empezar.")]
    [SerializeField] private float duracionDeszoom = 1.5f;
    [Tooltip("Segundos que tarda en volver al zoom normal, justo antes de poder caminar.")]
    [SerializeField] private float duracionZoom = 1f;

    // Avisa cuando la chica termino de salir por la puerta (ya girada de perfil), o enseguida si no hay intro.
    // Lo usan los carteles de tutorial para aparecer en ese momento.
    public static event System.Action SalidaTerminada;
    public static bool YaSalio { get; private set; }

    private Transform chica;
    private Sprite spritePerfil;   // la pose de perfil normal (la que pone el Animator al quedarse quieta)
    private SpriteRenderer dibujoChica;

    private void Awake()
    {
        YaSalio = false;
    }

    private static void AvisarSalida()
    {
        YaSalio = true;
        SalidaTerminada?.Invoke();
    }
    private SpriteMask mascaraChica;
    private float bordeCerrada = float.NaN;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (!activa || salud == null || puerta == null) { AvisarSalida(); return; }
#if UNITY_EDITOR
        // para probar: si la arrastraste lejos del portal en la Scene, arranca ahi y ese lugar hace de checkpoint
        if (Vector2.Distance(salud.transform.position, posicionChica) > distanciaParaProbarDesdeOtroLado)
        {
            puerta.enabled = false;
            salud.DefinirPuntoDeReaparicion(salud.transform.position);
            Debug.LogWarning("[Inicio] la chica no esta en el portal: se saltea la intro y arranca en " +
                             salud.transform.position + " (solo en el editor)");
            AvisarSalida();
            return;
        }
#endif
        chica = salud.transform;
        dibujoChica = salud.GetComponent<SpriteRenderer>();
        if (dibujoChica != null) spritePerfil = dibujoChica.sprite;
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        // sin controles hasta que termine la intro
        var mov = chica.GetComponent<MovimientoPersonaje>();
        var latigo = chica.GetComponent<AtaqueLatigo>();
        var sabiduria = chica.GetComponent<AtaqueSabiduria>();
        var animator = chica.GetComponent<Animator>();
        var rb = chica.GetComponent<Rigidbody2D>();
        if (mov != null) mov.enabled = false;
        if (latigo != null) latigo.enabled = false;
        if (sabiduria != null) sabiduria.enabled = false;
        if (animator != null) animator.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        // parada adentro del portal, de frente, mirando hacia la derecha al terminar
        // en z = 0 como todo el nivel: si quedara mas cerca de la camara, la mascara no la tapa detras de la puerta
        chica.position = new Vector3(posicionChica.x, posicionChica.y, 0f);
        var escala = chica.localScale; escala.x = Mathf.Abs(escala.x); chica.localScale = escala;
        MostrarChica(framesSalida != null && framesSalida.Length > 0 ? framesSalida[0] : null);

        // deszoom: la camara se aleja para mostrar el portal
        var camara = FindFirstObjectByType<CamaraSeguidora>();
        if (camara != null) camara.FijarEncuadre(centroEncuadreIntro, tamanoEncuadreIntro, duracionDeszoom);

        // la puerta solo se ve por debajo del borde de arriba del portal; la chica solo por fuera de la puerta
        CrearMascaraPuerta();
        CrearMascaraChica();

        yield return new WaitForSeconds(esperaInicial);

        // la puerta sube y desaparece (con el sonido de piedra, que arranca justo cuando empieza a subir)
        Sonido.Reproducir(sonidoPuerta, volumenPuerta);
        Vector3 cerrada = puerta.transform.position;
        Vector3 abierta = cerrada + Vector3.up * puerta.bounds.size.y * 1.05f;
        for (float t = 0f; t < duracionSubidaPuerta; t += Time.deltaTime)
        {
            puerta.transform.position = Vector3.Lerp(cerrada, abierta, Mathf.SmoothStep(0f, 1f, t / duracionSubidaPuerta));
            AcomodarMascaraChica();
            yield return null;
        }
        puerta.transform.position = abierta;
        puerta.enabled = false;
        if (dibujoChica != null) dibujoChica.maskInteraction = SpriteMaskInteraction.None;
        if (mascaraChica != null) Destroy(mascaraChica.gameObject);

        yield return new WaitForSeconds(esperaDespuesDeAbrir);

        // se gira de perfil
        // un solo movimiento: frente -> intermedio -> tres cuartos -> perfil normal, todo fundido
        yield return GiroSuave.Reproducir(dibujoChica, FramesDelGiro(), duracionGiroSalida);
        AvisarSalida();

        // zoom: la camara vuelve a seguirla de cerca y recien despues puede caminar
        if (camara != null)
        {
            camara.Liberar(duracionZoom);
            yield return new WaitForSeconds(duracionZoom);
        }

        // empieza el juego. El Animator vuelve a mandar y pone la pose de perfil: el ultimo frame del giro
        // (de tres cuartos) se funde con ella en vez de cambiar de golpe.
        StartCoroutine(GiroSuave.FundirHaciaLoQueSigue(dibujoChica, duracionFundidoPerfil));
        if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
        if (animator != null) animator.enabled = true;
        if (mov != null) mov.enabled = true;
        if (latigo != null) latigo.enabled = true;
        if (sabiduria != null) sabiduria.enabled = true;
    }

#if UNITY_EDITOR
    // clic derecho en el componente > "Llevar la chica al portal" (o menu Pomelo): la deja donde empieza la intro
    [ContextMenu("Llevar la chica al portal")]
    public void LlevarChicaAlPortal()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud == null) return;
        UnityEditor.Undo.RecordObject(salud.transform, "Llevar la chica al portal");
        salud.transform.position = new Vector3(posicionChica.x, posicionChica.y, 0f);
    }
#endif

    // los frames de la salida + la pose de perfil normal al final, asi el giro termina donde arranca el juego
    private Sprite[] FramesDelGiro()
    {
        var lista = new System.Collections.Generic.List<Sprite>();
        if (framesSalida != null) foreach (var s in framesSalida) if (s != null) lista.Add(s);
        if (spritePerfil != null && (lista.Count == 0 || lista[lista.Count - 1] != spritePerfil)) lista.Add(spritePerfil);
        return lista.ToArray();
    }

    private void MostrarChica(Sprite s)
    {
        if (s != null && dibujoChica != null) dibujoChica.sprite = s;
    }

    private static SpriteMask NuevaMascara(string nombre, int atras, int adelante)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        var go = new GameObject(nombre);
        var m = go.AddComponent<SpriteMask>();
        m.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); // 1x1 unidad
        // solo afecta a los dibujos con orden entre "atras" (sin incluir) y "adelante"
        m.isCustomRangeActive = true;
        m.backSortingOrder = atras;
        m.frontSortingOrder = adelante;
        return m;
    }

    private void CrearMascaraPuerta()
    {
        if (portal == null) return;
        var m = NuevaMascara("Mascara puerta entrada", puerta.sortingOrder - 1, puerta.sortingOrder);
        m.backSortingLayerID = m.frontSortingLayerID = puerta.sortingLayerID;
        Bounds b = portal.bounds;
        float arriba = b.max.y, abajo = b.min.y - 6f;
        m.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        m.transform.localScale = new Vector3(b.size.x + 2f, arriba - abajo, 1f);
        puerta.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    private void CrearMascaraChica()
    {
        if (dibujoChica == null) return;
        mascaraChica = NuevaMascara("Mascara chica", dibujoChica.sortingOrder - 1, dibujoChica.sortingOrder);
        mascaraChica.backSortingLayerID = mascaraChica.frontSortingLayerID = dibujoChica.sortingLayerID;
        dibujoChica.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        AcomodarMascaraChica();
    }

    // tapa a la chica en el hueco de la puerta. Con la puerta cerrada tapa tambien un poco mas abajo
    // (los pies, que quedan debajo del borde de la puerta); ese margen se va apenas la puerta empieza a subir.
    private void AcomodarMascaraChica()
    {
        if (mascaraChica == null) return;
        Bounds b = puerta.bounds;
        if (float.IsNaN(bordeCerrada)) bordeCerrada = b.min.y;
        float subio = b.min.y - bordeCerrada;
        float arriba = b.max.y, abajo = Mathf.Min(b.min.y, bordeCerrada - 0.6f + subio * 2f);
        mascaraChica.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        mascaraChica.transform.localScale = new Vector3(b.size.x, arriba - abajo, 1f);
    }
}
