using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enemigo "tronco": medita abrazado a un pedazo de la estatua, con los ojos cerrados y la cabeza flotando.
//  - Cuando la chica se acerca al agro, la camara se aleja para mostrar todo el piso donde salen los pinchos.
//  - Si ella pisa el piso dentro del agro (en el aire no pasa nada): abre los ojos, aparece su glow,
//    tiembla la camara, las areas se anticipan con un glow violeta y burbujas, y salen los pinchos
//    rapido, cada uno apenas desfasado. La distribucion de las areas se elige al azar cada vez.
//  - Si ella se queda quieta en un lugar seguro, cierra los ojos y guarda los pinchos.
//  - Al morir (VidaEnemigo llega a 0) suelta la pieza de la estatua, se le cae la cabeza con los ojos
//    cerrados y los pinchos se guardan para siempre. La pieza cae al piso y despues vuela a su lugar
//    en la barra de poderes del HUD (cada tronco tiene una pieza distinta: brazo, pecho o peineta).
[RequireComponent(typeof(VidaEnemigo))]
public class TroncoMeditador : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Muestra en la consola los mensajes de este script (apagado en el juego final).")]
    [SerializeField] private bool mostrarDebug = false;

    [System.Serializable]
    public class Distribucion
    {
        [Tooltip("Cada area: x = centro (unidades desde el borde izquierdo del agro), y = ancho.")]
        public Vector2[] areas;
    }

    private enum Estado { Meditando, Despertando, Atacando, Guardando, Muerto }

    [Header("Cabeza")]
    [SerializeField] private SpriteRenderer cabeza;
    [Tooltip("Ojos cerrados, entreabiertos y abiertos.")]
    [SerializeField] private Sprite[] ojos = new Sprite[3];
    [SerializeField] private float alturaFlotando = 0.1f;
    [SerializeField] private float vueltasPorSegundo = 0.3f;
    [SerializeField] private float segundosAbrirOjos = 0.25f;

    [Header("Agro: el piso donde salen los pinchos")]
    [Tooltip("Esquina de abajo a la izquierda del agro, sobre el piso (mundo).")]
    [SerializeField] private Vector2 inicioAgro;
    [SerializeField] private Vector2 tamanoAgro = new Vector2(44.61f, 6.28f);

    [Header("Camara")]
    [Tooltip("Cuanto antes de llegar al agro (de cada lado) se aleja la camara.")]
    [SerializeField] private float margenCamara = 6f;
    [Tooltip("Tamano de la camara para ver todo el agro.")]
    [SerializeField] private float tamanoCamara = 13.5f;
    [Tooltip("Altura del centro de la camara sobre el piso del agro.")]
    [SerializeField] private float alturaCamara = 8f;
    [SerializeField] private float intensidadTemblor = 0.18f;

    [Header("Pinchos")]
    [SerializeField] private GameObject grupoAncho;
    [SerializeField] private GameObject grupoFino;
    [Tooltip("Las areas mas anchas que esto usan el grupo ancho.")]
    [SerializeField] private float anchoLimite = 1f;
    [SerializeField] private Distribucion[] distribuciones;
    [SerializeField] private float segundosAnticipacion = 0.7f;
    [SerializeField] private float segundosSubida = 0.12f;
    [Tooltip("Desfase maximo entre un pincho y otro al salir.")]
    [SerializeField] private float desfaseMaximo = 0.18f;
    [SerializeField] private float segundosBajada = 0.35f;
    [Tooltip("Segundos quieta en un lugar seguro para que guarde los pinchos.")]
    [SerializeField] private float segundosQuieta = 0.8f;
    [Tooltip("Distancia minima al borde de la base de un pincho para que el lugar cuente como seguro. " +
             "Tiene que ser menor que medio ancho de la chica, asi pegada a un pincho cuenta como segura.")]
    [SerializeField] private float margenSeguro = 0.3f;
    [Tooltip("Capa (sorting layer) de los pinchos que saca del piso.")]
    [SerializeField] private string capaPinchos = Capas.Personajes;
    [Tooltip("Orden de los pinchos dentro de su capa. -1: detras de los pisos de Personajes (el del nivel 3 tiene " +
             "orden 1) y delante del tronco, que esta en Escenario.")]
    [SerializeField] private int ordenPinchos = -1;
    [Tooltip("Vida que saca cada golpe de los pinchos del tronco: 20 = una de las 5 vidas.")]
    [SerializeField] private float danioPinchos = 20f;
    [Tooltip("Cuanto se estiran los pinchos para arriba (1 = tamano del dibujo). El mas bajo tiene que quedar " +
             "bien por encima del salto mas alto de la chica (salto + doble salto en la punta = 5 unidades; el mas bajo " +
             "mide 5,6), asi no los puede saltar ni subirse a la punta.")]
    [SerializeField] private float estiramientoPinchos = 1.35f;
    [Tooltip("Cuanto se hunde la base de cada pincho en el piso, para que nunca parezca que flota.")]
    [SerializeField] private float hundirPinchos = 0.1f;
    [Tooltip("Cuanto mas se hunden todos los pinchos del piso (ademas de 'hundirPinchos'). Se estiran lo mismo, asi la " +
             "punta queda a la misma altura y no se pueden saltar: solo se baja la base, que a veces parecia flotar.")]
    [SerializeField] private float bajarPinchos = 0.25f;

    [Header("Anticipacion: glow violeta y burbujas en cada area")]
    [SerializeField] private Sprite glowArea;
    [SerializeField] private Color colorGlowArea = new Color(0.75f, 0.6f, 0.94f, 0.62f);
    [SerializeField] private float altoGlowArea = 3.6f;
    [SerializeField] private Sprite burbuja;
    [SerializeField] private float burbujasPorSegundo = 9f;
    [SerializeField] private Vector2 tamanoBurbujas = new Vector2(0.12f, 0.3f);
    [SerializeField] private Material materialSinLuz;

    [Header("Glow del agro, alrededor del tronco (del Figma)")]
    [SerializeField] private Sprite glowAgro;
    [Tooltip("x, y = centro local; z, w = tamano (unidades del dibujo).")]
    [SerializeField] private Vector4[] manchas;
    [SerializeField] private Color[] coloresManchas;
    [SerializeField] private float segundosGlowAgro = 0.5f;
    [Tooltip("Cuanto se ven las manchas mientras medita (0 = nada): avisan que cerca del tronco se pierde vida.")]
    [SerializeField] private float alfaManchasMeditando = 0.45f;
    [Tooltip("Multiplica la opacidad de las manchas (1 = la del color).")]
    [SerializeField] private float intensidadManchas = 1.7f;
    [Tooltip("Con la chica dentro de la zona de dano, las manchas laten mas fuerte y mas rapido.")]
    [SerializeField] private float latidosPorSegundoEnZona = 2f;

    [Header("Glow que parpadea sobre los pinchos que salieron")]
    [SerializeField] private Color colorBrilloPinchos = new Color(0.79f, 0.66f, 0.96f, 0.5f);
    [SerializeField] private Vector2 tamanoBrilloPinchos = new Vector2(0.9f, 1.4f);
    [SerializeField] private float parpadeosPorSegundo = 0.8f;

    [Header("Dano por estar cerca")]
    [Tooltip("Zona alrededor del tronco (centro relativo y tamano, en unidades del mundo).")]
    [SerializeField] private Vector2 centroZonaDanio = new Vector2(0f, -0.3f);
    [SerializeField] private Vector2 tamanoZonaDanio = new Vector2(5.5f, 4.5f);
    [Tooltip("Vida que le saca por cada segundo dentro de la zona (la vida maxima es 100).")]
    [SerializeField] private float danioPorSegundo = 5f;

    [Header("Muerte")]
    [SerializeField] private SpriteRenderer piezaEstatua;
    [Tooltip("Lugar de la pieza en la barra de poderes del HUD (0 brazo, 1 pecho, 2 peineta).")]
    [SerializeField] private int lugarEnHUD;
    [Tooltip("Cuanto gira la pieza al caer (grados).")]
    [SerializeField] private float giroPieza = -6f;
    [SerializeField] private float segundosEnElPiso = 0.5f;
    [SerializeField] private float segundosVueloHUD = 0.9f;
    [Tooltip("Donde queda la cabeza en el piso (local) y cuanto gira.")]
    [SerializeField] private Vector2 cabezaEnElPiso = new Vector2(1.6f, -1.4f);
    [SerializeField] private float giroCabezaCaida = -24f;

    [Header("Musica de la pelea")]
    [Tooltip("Suena apenas la chica entra al agro (empieza la pelea) y se va cuando se aleja o el tronco muere.")]
    [SerializeField] private AudioClip temaPelea;
    [Tooltip("Segundo de la cancion desde el que arranca (y al que vuelve cuando da la vuelta).")]
    [SerializeField] private float temaDesdeSegundo = 42f;
    [Tooltip("Volumen respecto de la musica del mapa (1.2 = 20% mas fuerte).")]
    [SerializeField] private float volumenTema = 1.2f;

    private MusicaDeFondo musica;
    private bool sonandoTema;

    private void PonerMusicaPelea()
    {
        if (sonandoTema || temaPelea == null) return;
        if (musica == null) musica = FindFirstObjectByType<MusicaDeFondo>();
        if (musica == null) return;
        musica.PonerTema(temaPelea, musica.Volumen * volumenTema, -1f, -1f, -1f, temaDesdeSegundo);
        sonandoTema = true;
    }

    private void QuitarMusicaPelea()
    {
        if (!sonandoTema) return;
        sonandoTema = false;
        if (musica != null) musica.QuitarTema();
    }

    private class Pincho { public Transform t; public SpriteRenderer sr; public Vector3 escala; public float centroY, piso, retraso; public Collider2D[] cols; }
    private class Burbuja { public Transform t; public SpriteRenderer sr; public float vida, dur, vel, x0, fase, tam; }
    private class Brillo { public SpriteRenderer sr; public float fase, velocidad; }

    private Estado estado = Estado.Meditando;
    private VidaEnemigo vida;
    private Collider2D cuerpo;
    private CamaraSeguidora camara;
    private Transform chica;
    private MovimientoPersonaje movimiento;
    private SaludPersonaje salud;
    private float proximoDanio;

    private Vector3 cabezaInicial;
    private float faseFlotar;
    private bool camaraAlejada;
    private bool estabaEnAgro;
    private bool pendiente;
    private float quietaDesde = -1f;
    private int ultimaDistribucion = -1;

    private readonly List<GameObject> grupos = new List<GameObject>();
    private readonly List<Pincho> pinchos = new List<Pincho>();
    private readonly List<SpriteRenderer> glows = new List<SpriteRenderer>();
    private readonly List<Vector2> areasActivas = new List<Vector2>();
    private readonly List<Vector2> rangosPinchos = new List<Vector2>();   // x minima y maxima de la base de cada pincho que salio
    private float xQuieta;   // donde quedo quieta (se mueve si se aleja de ahi)
    private readonly List<SpriteRenderer> manchasAgro = new List<SpriteRenderer>();
    private readonly List<Burbuja> burbujas = new List<Burbuja>();
    private readonly Stack<Burbuja> burbujasLibres = new Stack<Burbuja>();
    private readonly List<Brillo> brillosPinchos = new List<Brillo>();
    private float alfaBrillos, objetivoBrillos;
    private float alfaGlowAgro, objetivoGlowAgro;
    private bool chicaEnZona;
    private Collider2D colChica;
    private float atacandoDesde;

    [Header("Cuanto quedan arriba los pinchos")]
    [Tooltip("Segundos que quedan arriba los pinchos del piso; despues bajan siempre, aunque la chica este cerca.")]
    [SerializeField] private float segundosArriba = 2f;
    [Tooltip("Los ultimos segundos arriba laten (opacidad 50% / 100% intercalada) para avisar que bajan.")]
    [SerializeField] private float segundosLatido = 1f;
    [SerializeField] private float segundosPorDestello = 0.125f;
    [SerializeField] private float alfaLatido = 0.5f;

    [Header("Pinchos normales tambien sobre el tronco")]
    [Tooltip("Los pinchos del piso tambien salen donde esta el tronco: la distribucion elegida se continua, con su " +
             "mismo ritmo, hacia el lado del tronco hasta cubrir su cuerpo. Asi la chica sigue esquivando el mismo " +
             "patron que venia esquivando en el agro.")]
    [SerializeField] private bool pinchosSobreElTronco = true;
    [Tooltip("Cuanto mas alla del dibujo del tronco (de cada lado) llegan los pinchos extra.")]
    [SerializeField] private float margenSobreTronco = 0.5f;

    [Header("Defensa: pinchos a los costados del cuerpo")]
    [Tooltip("Si esta apagado, el tronco no saca los pinchos de defensa al tener a la chica cerca (y siempre se " +
             "le puede pegar). Probando: apagado, con los pinchos normales extendidos sobre el tronco.")]
    [SerializeField] private bool usarDefensa = false;
    [Tooltip("Zona donde se defiende, desde el borde izquierdo del dibujo del tronco: x = cuanto antes empieza " +
             "(hacia la izquierda), y = hasta donde llega hacia la derecha (unidades).")]
    [SerializeField] private Vector2 rangoDefensa = new Vector2(1.5f, 26f);
    [Tooltip("Segundos arriba (lo protegen: no se le puede pegar) y abajo (se le puede pegar).")]
    [SerializeField] private float segundosDefensaArriba = 2f;
    [SerializeField] private float segundosDefensaAbajo = 2f;
    [SerializeField] private float segundosSubirDefensa = 0.18f;
    [SerializeField] private float segundosBajarDefensa = 0.3f;
    [Tooltip("Donde salen los dos grupos, en fraccion del ancho del dibujo (0 = borde izquierdo, 1 = derecho).")]
    [SerializeField] private Vector2 lugaresDefensa = new Vector2(0.12f, 0.86f);
    [Tooltip("Alto de los pinchos de la defensa respecto del alto del dibujo del tronco.")]
    [SerializeField] private float altoDefensa = 1.05f;
    [Tooltip("Cuanto se ensancha cada pincho de la defensa (1 = como el dibujo).")]
    [SerializeField] private float anchoDefensa = 1.5f;
    [Tooltip("La cabeza con los ojos violeta (cerrados, entreabiertos, abiertos) mientras se defiende.")]
    [SerializeField] private Sprite[] ojosDefensa = new Sprite[3];
    [Tooltip("Centro de cada ojo en la cabeza (local), para el glow violeta.")]
    [SerializeField] private Vector2[] posicionOjos = { new Vector2(-0.64f, -0.21f), new Vector2(0.31f, -0.21f) };
    [SerializeField] private Color colorGlowOjos = new Color(0.72f, 0.45f, 1f, 0.75f);
    [SerializeField] private float tamanoGlowOjos = 0.75f;

    private readonly List<Pincho> pinchosDefensa = new List<Pincho>();
    private readonly List<GameObject> gruposDefensa = new List<GameObject>();
    private readonly List<SpriteRenderer> glowOjos = new List<SpriteRenderer>();
    private float nivelDefensa;        // 0 abajo, 1 arriba
    private float nivelGlowOjos;
    private bool defendiendo;          // la chica esta en el rango y el ciclo corre
    private Coroutine cicloDefensa;
    public bool DefensaArriba => nivelDefensa > 0.5f;

    private Rect ZonaDefensa()
    {
        var d = GetComponent<SpriteRenderer>();
        Bounds b = d != null ? d.bounds : new Bounds(transform.position, Vector3.one * 4f);
        return Rect.MinMaxRect(b.min.x - rangoDefensa.x, b.min.y - 1f, b.min.x + rangoDefensa.y, b.max.y + 3f);
    }

    private void ActualizarDefensa(Vector2 p)
    {
        if (!usarDefensa)
        {
            defendiendo = false;   // si estaba corriendo el ciclo, termina solo al bajar
            if (vida != null && estado != Estado.Muerto) vida.Invulnerable = DefensaArriba;
            return;
        }
        bool enRango = estado != Estado.Muerto && ZonaDefensa().Contains(p);
        if (enRango && !defendiendo) { defendiendo = true; if (mostrarDebug) Debug.Log("[Tronco] " + name + ": se defiende (pinchos a los costados)"); cicloDefensa = StartCoroutine(CicloDefensa()); }
        else if (!enRango && defendiendo) { defendiendo = false; }   // el ciclo termina solo al bajar
        if (vida != null && estado != Estado.Muerto) vida.Invulnerable = DefensaArriba;
        if (DefensaArriba) RevisarSiLaLevanto(pinchosDefensa);
    }

    // 2 s arriba (lo protegen), 2 s abajo (se le puede pegar), mientras la chica siga en el rango
    private IEnumerator CicloDefensa()
    {
        while (defendiendo && estado != Estado.Muerto)
        {
            CrearDefensa();
            for (float t = 0f; t < segundosSubirDefensa; t += Time.deltaTime) { MoverDefensa(1f - (1f - t / segundosSubirDefensa) * (1f - t / segundosSubirDefensa)); yield return null; }
            MoverDefensa(1f);
            yield return new WaitForSeconds(segundosDefensaArriba);
            for (float t = 0f; t < segundosBajarDefensa; t += Time.deltaTime) { float u = 1f - t / segundosBajarDefensa; MoverDefensa(u * u); yield return null; }
            BorrarDefensa();
            if (!defendiendo) break;
            yield return new WaitForSeconds(segundosDefensaAbajo);
        }
        cicloDefensa = null;
        defendiendo = false;
    }

    private void CrearDefensa()
    {
        BorrarDefensa();
        var d = GetComponent<SpriteRenderer>();
        if (grupoAncho == null || d == null) return;
        Bounds cuerpo = d.bounds;
        foreach (float f in new[] { lugaresDefensa.x, lugaresDefensa.y })
        {
            var g = Instantiate(grupoAncho, transform.parent);
            g.name = "pinchos defensa tronco";
            foreach (var pi in g.GetComponentsInChildren<Pinchos>()) pi.ConfigurarDanio(danioPinchos);
            var srs = g.GetComponentsInChildren<SpriteRenderer>();
            if (srs.Length == 0) { Destroy(g); continue; }
            Bounds b0 = Encapsular(srs);
            float estirar = cuerpo.size.y * altoDefensa / Mathf.Max(0.1f, b0.size.y);
            foreach (var sr in srs)
            {
                // por delante del cuerpo, asi lo tapan como un escudo
                sr.sortingLayerID = d.sortingLayerID; sr.sortingOrder = d.sortingOrder + 3;
                var e = sr.transform.localScale; sr.transform.localScale = new Vector3(e.x * anchoDefensa, e.y * estirar, e.z);
                var lp = sr.transform.localPosition; sr.transform.localPosition = new Vector3(lp.x * anchoDefensa, lp.y, lp.z);
            }
            Bounds b = Encapsular(srs);
            g.transform.position += new Vector3(Mathf.Lerp(cuerpo.min.x, cuerpo.max.x, f) - b.center.x, 0f, 0f);
            float baseVisual = BasePinchos(g.transform);
            foreach (var sr in srs) { var pp = sr.transform.position; pp.y = baseVisual; sr.transform.position = pp; }
            gruposDefensa.Add(g);
            foreach (var sr in srs)
                pinchosDefensa.Add(new Pincho { t = sr.transform, sr = sr, escala = sr.transform.localScale, centroY = sr.transform.position.y, piso = sr.bounds.min.y, retraso = 0f, cols = sr.GetComponents<Collider2D>() });
        }
        MoverDefensa(0f);
    }

    private void MoverDefensa(float s)
    {
        nivelDefensa = s;
        s = Mathf.Max(0.02f, s);
        foreach (var p in pinchosDefensa)
        {
            if (p.t == null) continue;
            p.t.localScale = new Vector3(p.escala.x, p.escala.y * s, p.escala.z);
            var pos = p.t.position; pos.y = p.piso + (p.centroY - p.piso) * s; p.t.position = pos;
            foreach (var c in p.cols) if (c != null) c.enabled = s > 0.3f;
        }
    }

    private void BorrarDefensa()
    {
        nivelDefensa = 0f;
        foreach (var g in gruposDefensa) if (g != null) Destroy(g);
        gruposDefensa.Clear();
        pinchosDefensa.Clear();
    }

    // mientras se defiende: ojos violeta abiertos con un glow chiquito; al bajar, vuelven los de siempre
    private void LateUpdate()
    {
        if (cabeza == null || ojosDefensa == null || ojosDefensa.Length < 3 || ojosDefensa[2] == null) return;
        bool violeta = DefensaArriba && estado != Estado.Muerto;
        if (violeta) cabeza.sprite = ojosDefensa[2];
        else if (System.Array.IndexOf(ojosDefensa, cabeza.sprite) >= 0)
            cabeza.sprite = (estado == Estado.Atacando || estado == Estado.Despertando) && ojos.Length > 2 ? ojos[2] : ojos[0];
        if (glowOjos.Count == 0 && glowAgro != null) CrearGlowOjos();
        nivelGlowOjos = Mathf.MoveTowards(nivelGlowOjos, violeta ? 1f : 0f, Time.deltaTime / 0.15f);
        float latido = 0.8f + 0.2f * Mathf.Sin(Time.time * 6f);
        foreach (var g in glowOjos)
        {
            if (g == null) continue;
            var c = colorGlowOjos; c.a *= nivelGlowOjos * latido;
            g.color = c;
        }
    }

    private void CrearGlowOjos()
    {
        float lado = glowAgro.bounds.size.x;
        foreach (var o in posicionOjos)
        {
            var go = new GameObject("glow ojo");
            go.transform.SetParent(cabeza.transform, false);
            go.transform.localPosition = new Vector3(o.x, o.y, -0.01f);
            go.transform.localScale = Vector3.one * (tamanoGlowOjos / lado);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glowAgro;
            if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
            sr.sortingLayerID = cabeza.sortingLayerID; sr.sortingOrder = cabeza.sortingOrder + 1;
            sr.color = new Color(colorGlowOjos.r, colorGlowOjos.g, colorGlowOjos.b, 0f);
            glowOjos.Add(sr);
        }
    }

    // opacidad de todos los pinchos que salieron
    private void PintarLatido(float alfa)
    {
        foreach (var p in pinchos)
        {
            if (p.sr == null) continue;
            var c = p.sr.color; c.a = alfa; p.sr.color = c;
        }
    }
    [Tooltip("Si un pincho la tiene con los pies a esta altura (o mas) sobre el piso del agro, la levanto: muere con aviso.")]
    [SerializeField] private float alturaLevantada = 0.25f;
    private bool soltandoBurbujas;
    private float acumuladoBurbujas;

    private void Awake()
    {
        vida = GetComponent<VidaEnemigo>();
        cuerpo = GetComponent<Collider2D>();
        vida.AlMorir += Morir;
        if (cabeza != null) cabezaInicial = cabeza.transform.localPosition;
        // la cabeza va en la misma capa que el cuerpo, justo encima: asi los pinchos que salen delante del tronco
        // tapan cabeza y cuerpo juntos (antes la cabeza quedaba delante de los pinchos y el cuerpo detras)
        var dibujoCuerpo = GetComponent<SpriteRenderer>();
        if (cabeza != null && dibujoCuerpo != null)
        {
            cabeza.sortingLayerID = dibujoCuerpo.sortingLayerID;
            // +4: por encima del glow violeta del agro (+2) y de los pinchos de defensa (+3), como antes
            cabeza.sortingOrder = dibujoCuerpo.sortingOrder + 4;
        }
        // la pieza de la estatua que tiene en el cuerpo, igual: en la capa del tronco, asi los pinchos la tapan
        // (al morir pasa a Efectos para volar al HUD por encima de todo)
        if (piezaEstatua != null && dibujoCuerpo != null)
        {
            piezaEstatua.sortingLayerID = dibujoCuerpo.sortingLayerID;
            piezaEstatua.sortingOrder = dibujoCuerpo.sortingOrder + 5;
        }
        faseFlotar = Random.value;
    }

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null)
        {
            chica = salud.transform;
            movimiento = salud.GetComponent<MovimientoPersonaje>();
        }
        if (cabeza != null && ojos.Length > 0) cabeza.sprite = ojos[0];
        CrearManchasAgro();
    }

    private void Update()
    {
        FlotarCabeza();
        ActualizarGlowAgro();
        ActualizarBrillosPinchos();
        ActualizarBurbujas();
        ActualizarAtrapada();
        if (estado == Estado.Muerto || chica == null) return;

        Vector2 p = chica.position;
        DaniarSiEstaCerca(p);
        ActualizarDefensa(p);
        float x0 = inicioAgro.x, x1 = inicioAgro.x + tamanoAgro.x;
        float piso = inicioAgro.y;

        // la camara se aleja al acercarse y vuelve al irse
        bool cerca = p.x > x0 - margenCamara && p.x < x1 + margenCamara && p.y > piso - 3f && p.y < piso + tamanoAgro.y + 4f;
        if (cerca != camaraAlejada && camara != null)
        {
            camaraAlejada = cerca;
            if (cerca) camara.FijarEncuadre(new Vector2((x0 + x1) / 2f, piso + alturaCamara), tamanoCamara);
            else { camara.Liberar(); QuitarMusicaPelea(); }
        }

        bool enAgro = p.x > x0 && p.x < x1 && p.y > piso - 1f && p.y < piso + tamanoAgro.y;
        bool enPiso = movimiento != null ? movimiento.EnPiso : true;
        // queda "pendiente" al entrar al agro o al saltar dentro de el: al pisar el piso, despierta
        if (!enAgro) pendiente = false;
        else if (!estabaEnAgro || !enPiso) pendiente = true;
        // la musica de la pelea arranca apenas entra al agro (aunque todavia este en el aire)
        if (enAgro) PonerMusicaPelea();
        estabaEnAgro = enAgro;
        // quieta = en el piso y sin alejarse del lugar donde se freno. Se mide la distancia y no la velocidad:
        // empujando contra un pincho la fisica la sacude un poquito cada paso, pero no avanza
        bool quieta = enPiso && Mathf.Abs(p.x - xQuieta) <= 0.1f;
        if (!quieta) xQuieta = p.x;
        if (!quieta) quietaDesde = -1f;
        else if (quietaDesde < 0f) quietaDesde = Time.time;

        if (estado == Estado.Despertando || estado == Estado.Atacando) RevisarSiLaLevanto();

        switch (estado)
        {
            case Estado.Meditando:
                // en el aire no pasa nada; al pisar (o caminar) dentro del agro, despierta
                if (enAgro && enPiso && (pendiente || !quieta)) { pendiente = false; StartCoroutine(Despertar()); }
                break;
            case Estado.Atacando:
                // quedan arriba 'segundosArriba' y bajan siempre, aunque la chica este cerca (la pelea no se traba).
                // El ultimo 'segundosLatido' laten entre 50% y 100% de opacidad para avisar que van a bajar.
                float arriba = Time.time - atacandoDesde;
                bool latiendo = arriba >= segundosArriba - segundosLatido;
                PintarLatido(latiendo ? (Mathf.FloorToInt((arriba - (segundosArriba - segundosLatido)) / segundosPorDestello) % 2 == 0 ? alfaLatido : 1f) : 1f);
                if (!cerca || arriba >= segundosArriba)
                {
                    PintarLatido(1f);
                    if (mostrarDebug) Debug.Log("[Tronco] " + name + ": los pinchos bajan despues de " + arriba.ToString("0.00") + " s arriba" + (cerca ? "" : " (la chica se fue)"));
                    StartCoroutine(Guardar(true));
                }
                break;
        }
    }

    // mientras la chica este pegada al tronco, pierde vida de a un golpe por segundo
    private void DaniarSiEstaCerca(Vector2 p)
    {
        if (salud == null) return;
        var zona = new Rect((Vector2)transform.position + centroZonaDanio - tamanoZonaDanio / 2f, tamanoZonaDanio);
        chicaEnZona = zona.Contains(p);
        if (!chicaEnZona) { proximoDanio = Time.time + 1f; return; }
        if (Time.time < proximoDanio) return;
        salud.RecibirDanio(danioPorSegundo);
        proximoDanio = Time.time + 1f;
    }

    // Si un pincho la levanto (o quedo trabada arriba de uno): esta encima de la base de un pincho, tocandolo,
    // y con los pies por encima del piso del agro. Antes podia quedar ahi sin que pasara nada; ahora
    // queda atrapada arriba del pincho (SaludPersonaje.AtraparEnPincho): mientras el pincho siga arriba la
    // lastima cada 'segundosDanioAtrapada' (ella puede regenerar vidas para aguantar) y cuando baja la suelta.
    private void RevisarSiLaLevanto() => RevisarSiLaLevanto(pinchos);

    [Header("Atrapada en un pincho")]
    [Tooltip("Cada cuantos segundos la lastima el pincho que la levanto, mientras siga arriba.")]
    [SerializeField] private float segundosDanioAtrapada = 1f;
    private List<Pincho> atrapadaEn;
    private float proximoDanioAtrapada;

    private void ActualizarAtrapada()
    {
        if (atrapadaEn == null || salud == null) return;
        if (!salud.Atrapada) { atrapadaEn = null; return; }   // murio y reaparecio
        bool sigueArriba = false;
        foreach (var p in atrapadaEn) foreach (var c in p.cols) if (c != null && c.enabled) { sigueArriba = true; break; }
        if (!sigueArriba || estado == Estado.Muerto)
        {
            salud.SoltarDelPincho();
            atrapadaEn = null;
            return;
        }
        if (Time.time >= proximoDanioAtrapada)
        {
            salud.RecibirDanio(danioPinchos, false);
            proximoDanioAtrapada = Time.time + segundosDanioAtrapada;
        }
    }

    private void RevisarSiLaLevanto(List<Pincho> lista)
    {
        if (salud == null || salud.Sentenciada) return;
        if (colChica == null) colChica = salud.GetComponent<Collider2D>();
        if (colChica == null) return;
        Bounds b = colChica.bounds;
        if (b.min.y < inicioAgro.y + alturaLevantada) return;
        b.Expand(0.1f);
        foreach (var p in lista)
            foreach (var c in p.cols)
            {
                if (c == null || !c.enabled) continue;
                Bounds bp = c.bounds;
                if (b.center.x < bp.min.x || b.center.x > bp.max.x || !b.Intersects(bp)) continue;
                salud.AtraparEnPincho();
                atrapadaEn = lista;
                proximoDanioAtrapada = Time.time + segundosDanioAtrapada;
                return;
            }
    }

    // se mide contra la base de cada pincho que salio (no contra el ancho del area, que es mas ancho que el dibujo):
    // asi, pegada a un pincho, cuenta como segura y los pinchos bajan
    private bool SobreUnArea(float x)
    {
        foreach (var r in rangosPinchos)
            if (x > r.x - margenSeguro && x < r.y + margenSeguro) return true;
        return false;
    }

    // ---------- despertar y sacar los pinchos ----------

    private IEnumerator Despertar()
    {
        estado = Estado.Despertando;
        objetivoGlowAgro = 1f;
        PonerMusicaPelea();
        StartCoroutine(CambiarOjos(true));

        // distribucion al azar (si hay mas de una, distinta a la anterior)
        int n = distribuciones != null ? distribuciones.Length : 0;
        if (n == 0) { estado = Estado.Atacando; yield break; }
        int elegida = Random.Range(0, n);
        if (n > 1 && elegida == ultimaDistribucion) elegida = (elegida + 1 + Random.Range(0, n - 1)) % n;
        ultimaDistribucion = elegida;
        areasActivas.Clear();
        foreach (var a in distribuciones[elegida].areas) areasActivas.Add(new Vector2(inicioAgro.x + a.x, a.y));
        if (pinchosSobreElTronco) ExtenderSobreElTronco(distribuciones[elegida].areas);

        // anticipacion: glow violeta y burbujas en cada area; tiembla la camara hasta que terminan de salir
        if (camara != null) camara.Temblar(segundosAnticipacion + segundosSubida + desfaseMaximo + 0.1f, intensidadTemblor);
        CrearGlows();
        soltandoBurbujas = true;
        for (float t = 0f; t < segundosAnticipacion; t += Time.deltaTime)
        {
            PintarGlows(Mathf.SmoothStep(0f, 1f, t / (segundosAnticipacion * 0.6f)) * (0.85f + 0.15f * Mathf.Sin(t * 18f)));
            yield return null;
        }
        soltandoBurbujas = false;

        CrearPinchos();
        objetivoBrillos = 1f;
        estado = Estado.Atacando;
        atacandoDesde = Time.time;
        if (quietaDesde >= 0f) quietaDesde = Time.time;   // lo quieta que estuvo antes de que salieran no cuenta
        float total = segundosSubida + desfaseMaximo;
        for (float t = 0f; t < total + 0.35f; t += Time.deltaTime)
        {
            MoverPinchos(t, true);
            PintarGlows(1f - Mathf.Clamp01(t / (total + 0.35f)));
            yield return null;
        }
        MoverPinchos(total, true);
        BorrarGlows();
    }

    // Continua la distribucion (con su mismo ritmo) hacia el lado donde esta el tronco, hasta cubrir su dibujo.
    // El ritmo es el periodo de la secuencia de anchos (ej: 1.16 / 2.53 alternados = cada 2 areas); si no se
    // repite, se toma la distribucion entera como un periodo. Las areas nuevas no se pisan con las que ya estan.
    private void ExtenderSobreElTronco(Vector2[] originales)
    {
        if (originales == null || originales.Length == 0) return;
        var a = new List<Vector2>(originales);
        a.Sort((u, v) => u.x.CompareTo(v.x));
        int n = a.Count;

        var dibujo = GetComponent<SpriteRenderer>();
        Bounds b = dibujo != null ? dibujo.bounds : (cuerpo != null ? cuerpo.bounds : new Bounds(transform.position, Vector3.one * 4f));
        float zMin = b.min.x - margenSobreTronco - inicioAgro.x;   // en unidades desde el borde del agro, como las areas
        float zMax = b.max.x + margenSobreTronco - inicioAgro.x;
        float primeraIzq = a[0].x - a[0].y / 2f, ultimaDer = a[n - 1].x + a[n - 1].y / 2f;

        int m = 0;
        for (int k = 1; k < n && m == 0; k++)
        {
            bool repite = true;
            for (int i = 0; i + k < n && repite; i++) repite = Mathf.Abs(a[i].y - a[i + k].y) < 0.05f;
            if (repite) m = k;
        }
        float periodo = m > 0 ? a[m].x - a[0].x : (n > 1 ? (a[n - 1].x - a[0].x) * n / (n - 1f) : a[0].y * 3f);
        if (periodo <= 0.01f) return;

        int agregadas = 0;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            if (lado < 0 && zMin >= primeraIzq) continue;
            if (lado > 0 && zMax <= ultimaDer) continue;
            for (int k = 1; k < 60; k++)
            {
                bool quedanEnZona = false;
                foreach (var area in a)
                {
                    float x = area.x + lado * k * periodo, izq = x - area.y / 2f, der = x + area.y / 2f;
                    if (lado < 0 ? der > primeraIzq - 0.05f : izq < ultimaDer + 0.05f) continue;   // pisaria las de siempre
                    if (lado < 0 ? der >= zMin : izq <= zMax) quedanEnZona = true;
                    if (der < zMin || izq > zMax) continue;                                     // fuera del tronco
                    // con un periodo de varias areas, copias de distintas vueltas caen en el mismo lugar: una sola
                    // (dos grupos superpuestos se ven opacos cuando titilan al bajar)
                    float xMundo = inicioAgro.x + x;
                    bool repetida = false;
                    foreach (var ya in areasActivas) if (Mathf.Abs(ya.x - xMundo) < 0.05f) { repetida = true; break; }
                    if (repetida) continue;
                    areasActivas.Add(new Vector2(xMundo, area.y));
                    agregadas++;
                }
                if (!quedanEnZona && k > 1) break;
            }
        }
        if (mostrarDebug && agregadas > 0)
            Debug.Log("[Tronco] " + name + ": " + agregadas + " areas de pinchos extra sobre el tronco (periodo " + periodo.ToString("0.00") + ")");
    }

    private void CrearPinchos()
    {
        foreach (var a in areasActivas)
        {
            var prefab = a.y > anchoLimite ? grupoAncho : grupoFino;
            if (prefab == null) continue;
            var g = Instantiate(prefab, transform.parent);
            g.name = "pinchos tronco";
            // los pinchos del tronco sacan una sola vida (20 de 100), no matan de una
            foreach (var pi in g.GetComponentsInChildren<Pinchos>()) pi.ConfigurarDanio(danioPinchos);
            var srs = g.GetComponentsInChildren<SpriteRenderer>();
            if (srs.Length == 0) { Destroy(g); continue; }
            // mas altos que el dibujo, para que la chica no los pueda saltar
            foreach (var sr in srs)
            {
                sr.sortingLayerName = capaPinchos; sr.sortingOrder = ordenPinchos;
                var e = sr.transform.localScale;
                sr.transform.localScale = new Vector3(e.x, e.y * estiramientoPinchos, e.z);
                // se hunden 'bajarPinchos' mas: se alargan eso mismo para que la punta no baje
                float alto = sr.bounds.size.y;
                if (bajarPinchos > 0f && alto > 0.01f)
                {
                    e = sr.transform.localScale;
                    sr.transform.localScale = new Vector3(e.x, e.y * (alto + bajarPinchos) / alto, e.z);
                }
            }
            // el grupo queda centrado en el area, y cada pincho apoyado (un poco hundido) en el piso de abajo suyo
            Bounds b = Encapsular(srs);
            g.transform.position += new Vector3(a.x - b.center.x, inicioAgro.y - b.min.y, 0f);
            // todos salen de la misma altura: la del pasto donde apoyan las plantas. El pivote de cada pincho es el
            // centro de su base, asi los inclinados no quedan mas altos o mas bajos que los rectos
            float baseVisual = BasePinchos(g.transform);
            foreach (var sr in srs)
            {
                var pp = sr.transform.position; pp.y = baseVisual; sr.transform.position = pp;
            }
            grupos.Add(g);
            // lo que ocupa cada pincho en el piso (su base): los inclinados tienen la punta corrida hacia afuera,
            // y pegada a la base la chica no esta encima aunque la punta pase por arriba de ella
            foreach (var sr in srs)
            {
                float x = sr.transform.position.x;   // el pivote de los pinchos es el centro de la base
                float medio = sr.sprite.bounds.extents.x * Mathf.Abs(sr.transform.lossyScale.x);
                rangosPinchos.Add(new Vector2(x - medio, x + medio));
            }
            CrearBrillos(g.transform, Encapsular(srs), srs.Length + 1);
            foreach (var sr in srs)
            {
                var t = sr.transform;
                pinchos.Add(new Pincho
                {
                    t = t,
                    sr = sr,
                    escala = t.localScale,
                    centroY = t.position.y,
                    piso = sr.bounds.min.y,
                    retraso = Random.Range(0f, desfaseMaximo),
                    cols = t.GetComponents<Collider2D>(),
                });
            }
        }
        MoverPinchos(0f, true);
    }

    private static Bounds Encapsular(SpriteRenderer[] srs)
    {
        Bounds b = srs[0].bounds;
        foreach (var sr in srs) b.Encapsulate(sr.bounds);
        return b;
    }

    [Tooltip("Cuanto mas arriba del piso solido (collider) esta la linea del pasto donde apoyan las plantas: " +
             "de ahi salen todos los pinchos.")]
    [SerializeField] private float alturaPastoSobrePiso = 0.15f;

    // la misma altura para todos los pinchos: el piso solido en el medio del agro, subido hasta el pasto
    private float BasePinchos(Transform grupo) =>
        PisoDebajo(inicioAgro.x + tamanoAgro.x * 0.5f, grupo) + alturaPastoSobrePiso - hundirPinchos - bajarPinchos;

    // altura del piso solido debajo de x (cerca del piso del agro); si no encuentra, el piso del agro
    private float PisoDebajo(float x, Transform grupo)
    {
        var golpes = Physics2D.RaycastAll(new Vector2(x, inicioAgro.y + 1f), Vector2.down, 3f);
        foreach (var h in golpes)
        {
            var c = h.collider;
            if (c.isTrigger || c.transform.IsChildOf(grupo) || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<Pinchos>() != null || c.GetComponentInParent<SaludPersonaje>() != null) continue;
            return h.point.y;
        }
        return inicioAgro.y;
    }

    // crece (o se achica) desde el piso: la base queda quieta y se estira hacia arriba
    private void MoverPinchos(float t, bool subiendo)
    {
        foreach (var p in pinchos)
        {
            if (p.t == null) continue;
            float u = subiendo ? Mathf.Clamp01((t - p.retraso) / segundosSubida)
                               : 1f - Mathf.Clamp01((t - p.retraso * 0.5f) / segundosBajada);
            float s = subiendo ? 1f - (1f - u) * (1f - u) : u * u;   // sale de golpe, se guarda mas suave
            s = Mathf.Max(0.02f, s);
            p.t.localScale = new Vector3(p.escala.x, p.escala.y * s, p.escala.z);
            var pos = p.t.position; pos.y = p.piso + (p.centroY - p.piso) * s; p.t.position = pos;
            foreach (var c in p.cols) if (c != null) c.enabled = s > 0.3f;
        }
    }

    // ---------- guardar los pinchos ----------

    private IEnumerator Guardar(bool volverAMeditar)
    {
        estado = volverAMeditar ? Estado.Guardando : Estado.Muerto;
        objetivoGlowAgro = 0f;
        objetivoBrillos = 0f;
        StartCoroutine(CambiarOjos(false));
        float total = segundosBajada + desfaseMaximo * 0.5f;
        for (float t = 0f; t < total; t += Time.deltaTime)
        {
            MoverPinchos(t, false);
            yield return null;
        }
        foreach (var g in grupos) if (g != null) Destroy(g);
        grupos.Clear();
        pinchos.Clear();
        brillosPinchos.Clear();
        areasActivas.Clear();
        rangosPinchos.Clear();
        if (volverAMeditar) estado = Estado.Meditando;
    }

    private IEnumerator CambiarOjos(bool abrir)
    {
        if (cabeza == null || ojos.Length < 3) yield break;
        float paso = segundosAbrirOjos / 2f;
        cabeza.sprite = ojos[1];
        yield return new WaitForSeconds(paso);
        cabeza.sprite = abrir ? ojos[2] : ojos[0];
    }

    // ---------- muerte ----------

    private void Morir()
    {
        StopAllCoroutines();
        defendiendo = false;
        BorrarDefensa();
        soltandoBurbujas = false;
        BorrarGlows();
        if (cuerpo != null) cuerpo.enabled = false;
        if (camaraAlejada && camara != null) camara.Liberar();
        camaraAlejada = false;
        QuitarMusicaPelea();
        if (cabeza != null && ojos.Length > 0) cabeza.sprite = ojos[0];
        // los pinchos dejan de lastimar enseguida y se guardan para siempre
        foreach (var p in pinchos) foreach (var c in p.cols) if (c != null) c.enabled = false;
        foreach (var p in pinchos) p.cols = new Collider2D[0];
        StartCoroutine(Guardar(false));
        StartCoroutine(CaerCabeza());
        StartCoroutine(SoltarPieza());
    }

    private IEnumerator CaerCabeza()
    {
        if (cabeza == null) yield break;
        var t = cabeza.transform;
        Vector3 desde = t.localPosition;
        Vector3 hasta = new Vector3(cabezaEnElPiso.x, cabezaEnElPiso.y, desde.z);
        const float dur = 0.7f;
        for (float k = 0f; k < dur; k += Time.deltaTime)
        {
            float u = k / dur;
            float caida = u * u;                                  // cae acelerando
            float salto = Mathf.Sin(u * Mathf.PI) * 0.5f;         // un poquito para arriba al soltarse
            t.localPosition = new Vector3(Mathf.Lerp(desde.x, hasta.x, u), Mathf.Lerp(desde.y, hasta.y, caida) + salto, desde.z);
            t.localRotation = Quaternion.Euler(0f, 0f, giroCabezaCaida * u);
            yield return null;
        }
        // rebotito al tocar el piso
        for (float k = 0f; k < 0.25f; k += Time.deltaTime)
        {
            t.localPosition = hasta + Vector3.up * Mathf.Sin(k / 0.25f * Mathf.PI) * 0.12f;
            yield return null;
        }
        t.localPosition = hasta;
    }

    private IEnumerator SoltarPieza()
    {
        if (piezaEstatua == null) yield break;
        var t = piezaEstatua.transform;
        Vector3 desde = t.localPosition;
        Quaternion giroDesde = t.localRotation;
        Quaternion giroHasta = giroDesde * Quaternion.Euler(0f, 0f, giroPieza);

        // cae hasta apoyarse en el piso (la base del dibujo del tronco), ya girada
        var dibujo = GetComponent<SpriteRenderer>();
        t.localRotation = giroHasta;
        float caida = dibujo != null ? (piezaEstatua.bounds.min.y - dibujo.bounds.min.y) / Mathf.Max(0.0001f, transform.lossyScale.y) : 1.65f;
        t.localRotation = giroDesde;
        float dur = 0.45f * Mathf.Sqrt(Mathf.Max(0.1f, caida) / 1.65f);

        for (float k = 0f; k < dur; k += Time.deltaTime)
        {
            float u = k / dur;
            t.localPosition = desde + new Vector3(0.25f * u, -caida * u * u, 0f);
            t.localRotation = Quaternion.Slerp(giroDesde, giroHasta, u);
            yield return null;
        }
        Vector3 fin = desde + new Vector3(0.25f, -caida, 0f);
        t.localRotation = giroHasta;
        for (float k = 0f; k < 0.2f; k += Time.deltaTime)
        {
            t.localPosition = fin + Vector3.up * Mathf.Sin(k / 0.2f * Mathf.PI) * 0.06f;
            yield return null;
        }
        t.localPosition = fin;

        yield return new WaitForSeconds(segundosEnElPiso);
        yield return VolarAlHUD(giroDesde);
    }

    // la pieza vuela haciendo una curva hasta su lugar en la barra de poderes, achicandose hasta el tamano
    // del icono; al llegar se apaga y aparece el icono del HUD
    private IEnumerator VolarAlHUD(Quaternion giroFinalLocal)
    {
        var hud = HUDBarraPoderes.Instancia;
        Camera cam = camara != null ? camara.GetComponent<Camera>() : Camera.main;
        if (hud == null || cam == null) yield break;

        var t = piezaEstatua.transform;
        Quaternion giroFinal = transform.rotation * giroFinalLocal;   // como la tenia abrazada
        t.SetParent(null, true);
        piezaEstatua.sortingLayerName = Capas.Efectos;   // por encima de todo mientras vuela
        piezaEstatua.sortingOrder = 5;
        Vector3 desde = t.position;
        Quaternion giroDesde = t.rotation;
        Vector3 escalaDesde = t.localScale;
        Vector2 tam = piezaEstatua.sprite.bounds.size;
        float lado = Mathf.Max(tam.x * escalaDesde.x, tam.y * escalaDesde.y);
        Vector3 control = desde + new Vector3(Random.Range(-0.8f, 0.8f), 3f, 0f);
        Color color = piezaEstatua.color;

        for (float k = 0f; k < segundosVueloHUD; k += Time.deltaTime)
        {
            float u = k / segundosVueloHUD;
            float s = u * u * (3f - 2f * u);
            // el lugar del HUD esta en pantalla: se pasa al mundo cada frame (la camara se mueve)
            Vector2 pantalla = hud.PuntoEnPantalla(lugarEnHUD);
            Vector3 destino = cam.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, Mathf.Abs(cam.transform.position.z - desde.z)));
            destino.z = desde.z;
            Vector2 tamIcono = hud.TamanoEnPantalla(lugarEnHUD) * (2f * cam.orthographicSize / Screen.height);
            float escalaIcono = Mathf.Max(tamIcono.x, tamIcono.y) / Mathf.Max(0.0001f, lado);

            Vector3 a = Vector3.Lerp(desde, control, s), b = Vector3.Lerp(control, destino, s);
            t.position = Vector3.Lerp(a, b, s);
            t.rotation = Quaternion.Slerp(giroDesde, giroFinal, s);
            t.localScale = escalaDesde * Mathf.Lerp(1f, escalaIcono, s);
            piezaEstatua.color = new Color(color.r, color.g, color.b, color.a * (u < 0.8f ? 1f : 1f - (u - 0.8f) / 0.2f));
            yield return null;
        }
        hud.Coleccionar(lugarEnHUD);
        t.gameObject.SetActive(false);
    }

    // ---------- efectos ----------

    private void FlotarCabeza()
    {
        if (cabeza == null || estado == Estado.Muerto) return;
        float y = Mathf.Sin((Time.time * vueltasPorSegundo + faseFlotar) * Mathf.PI * 2f) * alturaFlotando;
        cabeza.transform.localPosition = cabezaInicial + new Vector3(0f, y, 0f);
    }

    private void CrearGlows()
    {
        BorrarGlows();
        if (glowArea == null) return;
        float anchoSprite = glowArea.bounds.size.x, altoSprite = glowArea.bounds.size.y;
        foreach (var a in areasActivas)
        {
            var go = new GameObject("glow pinchos");
            go.transform.SetParent(transform.parent, false);
            go.transform.position = new Vector3(a.x, inicioAgro.y, transform.position.z + 0.01f);
            go.transform.localScale = new Vector3((a.y + 0.55f) / anchoSprite, altoGlowArea / altoSprite, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glowArea;
            if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
            sr.sortingLayerName = capaPinchos; sr.sortingOrder = ordenPinchos - 2;
            sr.color = new Color(colorGlowArea.r, colorGlowArea.g, colorGlowArea.b, 0f);
            glows.Add(sr);
        }
    }

    private void PintarGlows(float k)
    {
        foreach (var g in glows)
            if (g != null) g.color = new Color(colorGlowArea.r, colorGlowArea.g, colorGlowArea.b, colorGlowArea.a * k);
    }

    private void BorrarGlows()
    {
        foreach (var g in glows) if (g != null) Destroy(g.gameObject);
        glows.Clear();
    }

    // manchas de luz lila sobre los pinchos (como en el Figma): cada una parpadea a su ritmo
    private void CrearBrillos(Transform grupo, Bounds b, int cantidad)
    {
        if (glowAgro == null) return;
        float lado = glowAgro.bounds.size.x;
        for (int i = 0; i < cantidad; i++)
        {
            var go = new GameObject("brillo pinchos");
            go.transform.SetParent(grupo, true);
            float u = (i + Random.Range(0.2f, 0.8f)) / cantidad;   // repartidas a lo ancho, a distintas alturas
            go.transform.position = new Vector3(Mathf.Lerp(b.min.x, b.max.x, u), Random.Range(b.min.y + 0.4f, b.max.y - 1f), transform.position.z - 0.02f);
            float tam = Random.Range(tamanoBrilloPinchos.x, tamanoBrilloPinchos.y) / lado;
            go.transform.localScale = new Vector3(tam, tam, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glowAgro;
            if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
            sr.sortingLayerName = capaPinchos; sr.sortingOrder = ordenPinchos + 1;
            sr.color = Color.clear;
            brillosPinchos.Add(new Brillo { sr = sr, fase = Random.value * 10f, velocidad = parpadeosPorSegundo * Random.Range(0.7f, 1.3f) });
        }
    }

    private void ActualizarBrillosPinchos()
    {
        alfaBrillos = Mathf.MoveTowards(alfaBrillos, objetivoBrillos, Time.deltaTime / 0.3f);
        foreach (var b in brillosPinchos)
        {
            if (b.sr == null) continue;
            float parpadeo = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin((Time.time * b.velocidad + b.fase) * Mathf.PI * 2f));
            b.sr.color = new Color(colorBrilloPinchos.r, colorBrilloPinchos.g, colorBrilloPinchos.b, colorBrilloPinchos.a * alfaBrillos * parpadeo);
        }
    }

    private void CrearManchasAgro()
    {
        if (glowAgro == null || manchas == null) return;
        float lado = glowAgro.bounds.size.x;
        for (int i = 0; i < manchas.Length; i++)
        {
            var m = manchas[i];
            var go = new GameObject("glow agro");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(m.x, m.y, -0.01f);
            go.transform.localScale = new Vector3(m.z / lado, m.w / lado, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glowAgro;
            if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
            var dibujo = GetComponent<SpriteRenderer>();
            if (dibujo != null) sr.sortingLayerID = dibujo.sortingLayerID;
            sr.sortingOrder = (dibujo != null ? dibujo.sortingOrder : 0) + 2;
            sr.color = Color.clear;
            manchasAgro.Add(sr);
        }
    }

    // el glow del agro aparece y se apaga suave; cada mancha late y se mueve un poquito, como particulas.
    // Mientras medita se ve un poco (avisa que cerca se pierde vida); con la chica en la zona de dano late fuerte.
    private void ActualizarGlowAgro()
    {
        bool vivo = estado != Estado.Muerto;
        float objetivo = vivo ? Mathf.Max(objetivoGlowAgro, chicaEnZona ? 1f : alfaManchasMeditando) : 0f;
        alfaGlowAgro = Mathf.MoveTowards(alfaGlowAgro, objetivo, Time.deltaTime / Mathf.Max(0.01f, segundosGlowAgro));
        for (int i = 0; i < manchasAgro.Count; i++)
        {
            var sr = manchasAgro[i];
            if (sr == null) continue;
            var c = coloresManchas != null && i < coloresManchas.Length ? coloresManchas[i] : Color.white;
            float latido = vivo && chicaEnZona
                ? 0.55f + 0.45f * Mathf.Sin((Time.time * latidosPorSegundoEnZona + i * 0.21f) * Mathf.PI * 2f)
                : 0.8f + 0.2f * Mathf.Sin(Time.time * 1.7f + i * 1.3f);
            sr.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(c.a * intensidadManchas) * alfaGlowAgro * latido);
            var m = manchas[i];
            sr.transform.localPosition = new Vector3(m.x + Mathf.Sin(Time.time * 0.6f + i * 2.1f) * 0.12f,
                                                     m.y + Mathf.Sin(Time.time * 0.45f + i * 1.7f) * 0.18f, -0.01f);
        }
    }

    private void ActualizarBurbujas()
    {
        if (soltandoBurbujas && burbuja != null)
        {
            acumuladoBurbujas += burbujasPorSegundo * areasActivas.Count * Time.deltaTime;
            while (acumuladoBurbujas >= 1f && areasActivas.Count > 0)
            {
                acumuladoBurbujas -= 1f;
                SoltarBurbuja(areasActivas[Random.Range(0, areasActivas.Count)]);
            }
        }
        for (int i = burbujas.Count - 1; i >= 0; i--)
        {
            var b = burbujas[i];
            b.vida += Time.deltaTime;
            float u = b.vida / b.dur;
            if (u >= 1f) { b.t.gameObject.SetActive(false); burbujas.RemoveAt(i); burbujasLibres.Push(b); continue; }
            float y = inicioAgro.y + 0.1f + b.vel * b.vida;
            float x = b.x0 + Mathf.Sin(b.vida * 5f + b.fase) * 0.06f;
            b.t.position = new Vector3(x, y, transform.position.z - 0.02f);
            b.t.localScale = Vector3.one * b.tam * (u > 0.85f ? 1f + (u - 0.85f) * 2f : 1f);   // se infla un poquito antes de reventar
            var c = b.sr.color; c.a = Mathf.Clamp01(Mathf.Min(u * 6f, (1f - u) * 5f)); b.sr.color = c;
        }
    }

    private void SoltarBurbuja(Vector2 area)
    {
        var b = burbujasLibres.Count > 0 ? burbujasLibres.Pop() : NuevaBurbuja();
        b.x0 = area.x + Random.Range(-area.y / 2f - 0.15f, area.y / 2f + 0.15f);
        b.vel = Random.Range(0.9f, 2.2f);
        b.dur = Random.Range(0.45f, 1f);
        b.vida = 0f;
        b.fase = Random.value * 10f;
        b.tam = Random.Range(tamanoBurbujas.x, tamanoBurbujas.y) / Mathf.Max(0.01f, burbuja.bounds.size.x);
        b.t.gameObject.SetActive(true);
        burbujas.Add(b);
    }

    private Burbuja NuevaBurbuja()
    {
        var go = new GameObject("burbuja tronco");
        go.transform.SetParent(transform.parent, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = burbuja;
        if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
        sr.sortingLayerName = capaPinchos; sr.sortingOrder = ordenPinchos + 1;
        return new Burbuja { t = go.transform, sr = sr };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(inicioAgro + tamanoAgro / 2f, tamanoAgro);
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireCube((Vector2)transform.position + centroZonaDanio, tamanoZonaDanio);
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireCube(inicioAgro + new Vector2(tamanoAgro.x / 2f, tamanoAgro.y / 2f + 0.5f),
                            new Vector2(tamanoAgro.x + margenCamara * 2f, tamanoAgro.y + 7f));
    }
}
