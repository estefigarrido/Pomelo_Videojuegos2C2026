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
//    cerrados y los pinchos se guardan para siempre.
[RequireComponent(typeof(VidaEnemigo))]
public class TroncoMeditador : MonoBehaviour
{
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
    [Tooltip("Distancia minima a un area de pinchos para que el lugar cuente como seguro.")]
    [SerializeField] private float margenSeguro = 0.3f;
    [SerializeField] private int ordenPinchos = 0;

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
    [Tooltip("Cuanto cae la pieza hasta el piso (unidades del dibujo).")]
    [SerializeField] private float caidaPieza = 1.65f;
    [Tooltip("Donde queda la cabeza en el piso (local) y cuanto gira.")]
    [SerializeField] private Vector2 cabezaEnElPiso = new Vector2(1.6f, -1.4f);
    [SerializeField] private float giroCabezaCaida = -24f;

    private class Pincho { public Transform t; public Vector3 escala; public float centroY, piso, retraso; public Collider2D[] cols; }
    private class Burbuja { public Transform t; public SpriteRenderer sr; public float vida, dur, vel, x0, fase, tam; }
    private class Brillo { public SpriteRenderer sr; public float fase, velocidad; }

    private Estado estado = Estado.Meditando;
    private VidaEnemigo vida;
    private Collider2D cuerpo;
    private CamaraSeguidora camara;
    private Transform chica;
    private Rigidbody2D rbChica;
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
    private readonly List<SpriteRenderer> manchasAgro = new List<SpriteRenderer>();
    private readonly List<Burbuja> burbujas = new List<Burbuja>();
    private readonly Stack<Burbuja> burbujasLibres = new Stack<Burbuja>();
    private readonly List<Brillo> brillosPinchos = new List<Brillo>();
    private float alfaBrillos, objetivoBrillos;
    private float alfaGlowAgro, objetivoGlowAgro;
    private bool soltandoBurbujas;
    private float acumuladoBurbujas;

    private void Awake()
    {
        vida = GetComponent<VidaEnemigo>();
        cuerpo = GetComponent<Collider2D>();
        vida.AlMorir += Morir;
        if (cabeza != null) cabezaInicial = cabeza.transform.localPosition;
        faseFlotar = Random.value;
    }

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null)
        {
            chica = salud.transform;
            rbChica = salud.GetComponent<Rigidbody2D>();
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
        if (estado == Estado.Muerto || chica == null) return;

        Vector2 p = chica.position;
        DaniarSiEstaCerca(p);
        float x0 = inicioAgro.x, x1 = inicioAgro.x + tamanoAgro.x;
        float piso = inicioAgro.y;

        // la camara se aleja al acercarse y vuelve al irse
        bool cerca = p.x > x0 - margenCamara && p.x < x1 + margenCamara && p.y > piso - 3f && p.y < piso + tamanoAgro.y + 4f;
        if (cerca != camaraAlejada && camara != null)
        {
            camaraAlejada = cerca;
            if (cerca) camara.FijarEncuadre(new Vector2((x0 + x1) / 2f, piso + alturaCamara), tamanoCamara);
            else camara.Liberar();
        }

        bool enAgro = p.x > x0 && p.x < x1 && p.y > piso - 1f && p.y < piso + tamanoAgro.y;
        bool enPiso = movimiento != null ? movimiento.EnPiso : true;
        // queda "pendiente" al entrar al agro o al saltar dentro de el: al pisar el piso, despierta
        if (!enAgro) pendiente = false;
        else if (!estabaEnAgro || !enPiso) pendiente = true;
        estabaEnAgro = enAgro;
        bool quieta = enPiso && rbChica != null && Mathf.Abs(rbChica.linearVelocity.x) < 0.15f;
        if (!quieta) quietaDesde = -1f;
        else if (quietaDesde < 0f) quietaDesde = Time.time;

        switch (estado)
        {
            case Estado.Meditando:
                // en el aire no pasa nada; al pisar (o caminar) dentro del agro, despierta
                if (enAgro && enPiso && (pendiente || !quieta)) { pendiente = false; StartCoroutine(Despertar()); }
                break;
            case Estado.Atacando:
                bool segura = !enAgro || !SobreUnArea(p.x);
                if (!cerca || (segura && quietaDesde >= 0f && Time.time - quietaDesde >= segundosQuieta))
                    StartCoroutine(Guardar(true));
                break;
        }
    }

    // mientras la chica este pegada al tronco, pierde vida de a un golpe por segundo
    private void DaniarSiEstaCerca(Vector2 p)
    {
        if (salud == null) return;
        var zona = new Rect((Vector2)transform.position + centroZonaDanio - tamanoZonaDanio / 2f, tamanoZonaDanio);
        if (!zona.Contains(p)) { proximoDanio = Time.time + 1f; return; }
        if (Time.time < proximoDanio) return;
        salud.RecibirDanio(danioPorSegundo);
        proximoDanio = Time.time + 1f;
    }

    private bool SobreUnArea(float x)
    {
        foreach (var a in areasActivas)
            if (Mathf.Abs(x - a.x) < a.y / 2f + margenSeguro) return true;
        return false;
    }

    // ---------- despertar y sacar los pinchos ----------

    private IEnumerator Despertar()
    {
        estado = Estado.Despertando;
        objetivoGlowAgro = 1f;
        StartCoroutine(CambiarOjos(true));

        // distribucion al azar (si hay mas de una, distinta a la anterior)
        int n = distribuciones != null ? distribuciones.Length : 0;
        if (n == 0) { estado = Estado.Atacando; yield break; }
        int elegida = Random.Range(0, n);
        if (n > 1 && elegida == ultimaDistribucion) elegida = (elegida + 1 + Random.Range(0, n - 1)) % n;
        ultimaDistribucion = elegida;
        areasActivas.Clear();
        foreach (var a in distribuciones[elegida].areas) areasActivas.Add(new Vector2(inicioAgro.x + a.x, a.y));

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

    private void CrearPinchos()
    {
        foreach (var a in areasActivas)
        {
            var prefab = a.y > anchoLimite ? grupoAncho : grupoFino;
            if (prefab == null) continue;
            var g = Instantiate(prefab, transform.parent);
            g.name = "pinchos tronco";
            // el grupo queda centrado en el area y apoyado en el piso
            var srs = g.GetComponentsInChildren<SpriteRenderer>();
            if (srs.Length == 0) { Destroy(g); continue; }
            Bounds b = srs[0].bounds;
            foreach (var sr in srs) { b.Encapsulate(sr.bounds); sr.sortingOrder = ordenPinchos; }
            g.transform.position += new Vector3(a.x - b.center.x, inicioAgro.y - 0.04f - b.min.y, 0f);
            grupos.Add(g);
            b.center += new Vector3(a.x - b.center.x, inicioAgro.y - 0.04f - b.min.y, 0f);
            CrearBrillos(g.transform, b, srs.Length + 1);
            foreach (var sr in srs)
            {
                var t = sr.transform;
                pinchos.Add(new Pincho
                {
                    t = t,
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
        soltandoBurbujas = false;
        BorrarGlows();
        if (cuerpo != null) cuerpo.enabled = false;
        if (camaraAlejada && camara != null) camara.Liberar();
        camaraAlejada = false;
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
        const float dur = 0.45f;
        for (float k = 0f; k < dur; k += Time.deltaTime)
        {
            float u = k / dur;
            t.localPosition = desde + new Vector3(0.25f * u, -caidaPieza * u * u, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, -6f * u);
            yield return null;
        }
        Vector3 fin = desde + new Vector3(0.25f, -caidaPieza, 0f);
        for (float k = 0f; k < 0.2f; k += Time.deltaTime)
        {
            t.localPosition = fin + Vector3.up * Mathf.Sin(k / 0.2f * Mathf.PI) * 0.06f;
            yield return null;
        }
        t.localPosition = fin;
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
            sr.sortingOrder = ordenPinchos - 2;
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
            sr.sortingOrder = ordenPinchos + 1;
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
            sr.sortingOrder = (dibujo != null ? dibujo.sortingOrder : 0) + 2;
            sr.color = Color.clear;
            manchasAgro.Add(sr);
        }
    }

    // el glow del agro aparece y se apaga suave; cada mancha late y se mueve un poquito, como particulas
    private void ActualizarGlowAgro()
    {
        alfaGlowAgro = Mathf.MoveTowards(alfaGlowAgro, objetivoGlowAgro, Time.deltaTime / Mathf.Max(0.01f, segundosGlowAgro));
        for (int i = 0; i < manchasAgro.Count; i++)
        {
            var sr = manchasAgro[i];
            if (sr == null) continue;
            var c = coloresManchas != null && i < coloresManchas.Length ? coloresManchas[i] : Color.white;
            float latido = 0.8f + 0.2f * Mathf.Sin(Time.time * 1.7f + i * 1.3f);
            sr.color = new Color(c.r, c.g, c.b, c.a * alfaGlowAgro * latido);
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
        sr.sortingOrder = ordenPinchos + 1;
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
