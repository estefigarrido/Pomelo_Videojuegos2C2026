using System.Collections.Generic;
using UnityEngine;

// Va en "Plantas y botellitas (elementos)/plantas". Las plantas de esta carpeta (las del piso y las que
// cuelgan) son las que todavia estan un poco vivas: arrancan desaturadas y cada una guarda polen, en
// proporcion a su tamano. Las botellitas (carpeta "botellitas") tambien guardan polen.
// - Cuando la chica pasa por una planta o botellita (caminando, saltando o cayendo) suelta polen de a
//   poco. El polen es grande (+20 %), tiene glow amarillo y lo acompanan destellos amarillos del Figma.
//   Sube, flota un momento 5 px arriba de la planta y siempre vuela solo a la barra del templo (BarraTemplo).
// - La planta recupera el color a medida que suelta su polen: un blob de borde suave e irregular (la
//   traduccion del blurblob del Figma) crece desde donde la toco la chica. Recien cuando ya no le queda
//   polen tiene todo su color, y ahi larga una rafaga de destellos: asi se nota que ya se junto todo.
// - Al romperse una botellita suelta de golpe el polen que le quedaba (BotellitaRompible llama a SoltarTodo).
// Cantidad: cada piso tiene el polen justo para restaurar sus sectores ('porcentajePorSector' por sector,
// 10 % = un sector), repartido entre sus plantas y botellitas segun su tamano.
public class PolenPlantas : MonoBehaviour
{
    [SerializeField] private Sprite[] dientesDeLeon;
    [Tooltip("Los polenes sueltos (polen1..3): son el resto de lo que se levanta.")]
    [SerializeField] private Sprite[] polenes;
    [Tooltip("De cada 'polenPorDiente' polenes, uno es un diente de leon entero.")]
    [SerializeField] private int polenPorDiente = 3;
    [Tooltip("Alto de los polenes sueltos respecto del de los dientes de leon.")]
    [SerializeField] private float tamanoPolen = 0.45f;
    [Tooltip("Los destellos amarillos desenfocados del Figma.")]
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Material materialSinLuz;

    [Header("Color de las plantas")]
    [Tooltip("Material con el shader 'Pomelo/Sprite Lit Restaurable' (PlantasRestaurables).")]
    [SerializeField] private Material materialPlantas;
    [Tooltip("Cuanto tarda el blob en alcanzar lo que ya se solto (segundos, suaviza).")]
    [SerializeField] private float suavidadColor = 0.35f;
    [Tooltip("Borde suave del blob, respecto de su radio final (el blur del Figma).")]
    [SerializeField] private float bordeBlob = 0.3f;
    [Tooltip("Destellos que larga la planta cuando recupera todo el color.")]
    [SerializeField] private int destellosAlCompletar = 14;

    [Header("Cantidad")]
    [Tooltip("Polen que vale un sector del templo (la barra gasta esto por sector).")]
    [SerializeField] private float porcentajePorSector = 10f;
    [Tooltip("Cuanto vale cada polen que se levanta (aprox.; se ajusta para que la planta de justo lo suyo).")]
    [SerializeField] private float valorPolen = 0.5f;
    [Tooltip("La planta se vacia cuando la chica recorrio adentro esta cantidad de veces su ancho.")]
    [SerializeField] private float pasadasHastaVaciarse = 3f;
    [Tooltip("La chica tiene que moverse al menos a esta velocidad para levantar polen.")]
    [SerializeField] private float velocidadMinima = 0.6f;
    [Tooltip("Destellos sueltos por cada polen.")]
    [SerializeField] private float destellosSueltos = 1.6f;
    [SerializeField] private int maximo = 220;

    [Header("Movimiento")]
    [Tooltip("Alto de cada diente de leon (unidades): 20 % mas grande que antes.")]
    [SerializeField] private Vector2 altura = new Vector2(0.54f, 0.9f);
    [Tooltip("Hasta cuanto tarda en salir cada uno: asi no salen todos juntos.")]
    [SerializeField] private float desfaseMaximo = 0.35f;
    [SerializeField] private Vector2 subida = new Vector2(0.9f, 1.8f);
    [SerializeField] private float empujeChica = 0.6f;
    [SerializeField] private float vaiven = 0.35f;
    [Tooltip("Altura a la que flota arriba de lo mas alto de la planta (5 px = 0.05).")]
    [SerializeField] private float sobreLaPlanta = 0.05f;
    [SerializeField] private float alturaNube = 0.35f;
    [Tooltip("Cuanto flota antes de irse solo a la barra (segundos).")]
    [SerializeField] private Vector2 tiempoFlotando = new Vector2(0.6f, 1.3f);
    [SerializeField] private float segundosVuelo = 0.8f;

    [Header("Glow y destellos")]
    [SerializeField] private float tamanoDestello = 2f;
    [SerializeField] private float alfaDestello = 0.85f;
    [SerializeField] private Color colorGlow = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private float retrasoDestello = 0.12f;
    [SerializeField] private int ordenDeDibujo = 102;

    public static PolenPlantas Instancia { get; private set; }

    private enum Estado { Subiendo, Flotando, Volando }

    private class Fuente
    {
        public SpriteRenderer sr; public SpriteRenderer[] partes; public BotellitaRompible botellita;
        public float porcentaje; public int total, soltados; public float recorrido; public bool tocada;
        public Vector2 centroBlob; public float radioFinal, semilla, colorMostrado; public bool completa;
        public MaterialPropertyBlock mpb; public SpriteRenderer[] glows; public float[] alfaGlows;
        public Bounds Limites
        {
            get
            {
                bool hay = false; Bounds b = default;
                foreach (var s in partes) { if (s == null || !s.enabled) continue; if (!hay) { b = s.bounds; hay = true; } else b.Encapsulate(s.bounds); }
                return hay ? b : (sr != null ? sr.bounds : default);
            }
        }
        public float Soltado => total > 0 ? (float)soltados / total : 1f;
    }
    private class Polen
    {
        public Transform t, td; public SpriteRenderer sr, srd;
        public Vector2 pos, vel, posDestello, desdeVuelo; public float vida, espera, tam, tamD, fase, giro, yFlotar, xFlotar, inicioVuelo, vidaMax, valor, flotarHasta;
        public bool soloDestello; public Estado estado;
    }

    private readonly List<Fuente> fuentes = new List<Fuente>();
    private readonly List<Polen> activos = new List<Polen>();
    private readonly Stack<Polen> libres = new Stack<Polen>();
    private Collider2D cuerpoChica;
    private Rigidbody2D rbChica;
    private Camera camara;
    private bool armado;
    private float pendiente;   // polen que no entro en el maximo de particulas: se suma igual

    private static readonly int IdGris = Shader.PropertyToID("_Gris");
    private static readonly int IdBlob = Shader.PropertyToID("_Blob");
    private static readonly int IdSemilla = Shader.PropertyToID("_BlobSemilla");

    private void Awake() => Instancia = this;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) { cuerpoChica = salud.GetComponent<Collider2D>(); rbChica = salud.GetComponent<Rigidbody2D>(); }
        camara = BuscarCamara();
    }

    // se arma en el primer Update: asi las plantas que cuelgan ya se cortaron en franjas (ColganteFlexible)
    private void Armar()
    {
        armado = true;
        foreach (Transform h in transform)
        {
            var sr = h.GetComponent<SpriteRenderer>();
            if (sr == null || h.name == "polen" || h.name == "destello polen") continue;
            var f = new Fuente { sr = sr, partes = h.GetComponentsInChildren<SpriteRenderer>(true), mpb = new MaterialPropertyBlock(), semilla = Random.Range(0f, 6.28f) };
            if (materialPlantas != null)
            {
                f.mpb.SetFloat(IdGris, 1f);
                f.mpb.SetVector(IdBlob, new Vector4(0f, 0f, 0f, 0.01f));
                f.mpb.SetFloat(IdSemilla, f.semilla);
                foreach (var p in f.partes) { p.sharedMaterial = materialPlantas; p.SetPropertyBlock(f.mpb); }
            }
            fuentes.Add(f);
        }
        var carpeta = transform.parent != null ? transform.parent.Find("botellitas") : null;
        if (carpeta != null)
            foreach (var b in carpeta.GetComponentsInChildren<BotellitaRompible>())
            {
                var sr = b.GetComponent<SpriteRenderer>();
                var glows = new List<SpriteRenderer>();
                foreach (Transform h in b.transform) if (h.name.StartsWith("glow")) glows.Add(h.GetComponent<SpriteRenderer>());
                var f = new Fuente { sr = sr, partes = new[] { sr }, botellita = b, glows = glows.ToArray() };
                f.alfaGlows = new float[f.glows.Length];
                for (int i = 0; i < f.glows.Length; i++) f.alfaGlows[i] = f.glows[i].color.a;
                fuentes.Add(f);
            }
        Repartir();
    }

    // cada piso (fila de sectores) tiene el polen justo para sus sectores, repartido segun el tamano
    private void Repartir()
    {
        var templo = RestauracionTemplo.Instancia;
        var filas = new Dictionary<int, List<Fuente>>();
        var demanda = new Dictionary<int, float>();
        if (templo != null)
            foreach (var s in templo.Sectores) { int k = templo.Fila(s.center.y); demanda[k] = (demanda.TryGetValue(k, out float d) ? d : 0f) + porcentajePorSector; }
        foreach (var f in fuentes)
        {
            int k = templo != null ? templo.Fila(f.Limites.center.y) : 0;
            if (!filas.ContainsKey(k)) filas[k] = new List<Fuente>();
            filas[k].Add(f);
        }
        // las filas sin plantas le pasan su demanda a la fila con plantas mas cercana
        if (templo == null) demanda[0] = 300f;
        foreach (var k in new List<int>(demanda.Keys))
        {
            if (filas.ContainsKey(k)) continue;
            int mejor = int.MaxValue; foreach (var j in filas.Keys) if (mejor == int.MaxValue || Mathf.Abs(j - k) < Mathf.Abs(mejor - k)) mejor = j;
            if (mejor == int.MaxValue) continue;
            demanda[mejor] = (demanda.TryGetValue(mejor, out float d) ? d : 0f) + demanda[k];
            demanda.Remove(k);
        }
        foreach (var par in filas)
        {
            float area = 0f;
            foreach (var f in par.Value) area += Tamano(f);
            float total = demanda.TryGetValue(par.Key, out float dem) ? dem : 0f;
            foreach (var f in par.Value)
            {
                f.porcentaje = area > 0f ? total * Tamano(f) / area : 0f;
                f.total = Mathf.Max(1, Mathf.RoundToInt(f.porcentaje / Mathf.Max(0.05f, valorPolen)));
            }
        }
    }

    private static float Tamano(Fuente f) { var b = f.Limites; return Mathf.Max(0.05f, b.size.x * b.size.y); }

    private void Update()
    {
        if (!armado) Armar();
        if (cuerpoChica != null && rbChica != null)
        {
            Bounds c = cuerpoChica.bounds;
            float vel = rbChica.linearVelocity.magnitude;
            foreach (var f in fuentes)
            {
                if (f.soltados >= f.total || f.sr == null) continue;
                if (f.botellita != null && !f.sr.enabled) continue;
                Bounds b = f.Limites;
                if (!b.Intersects(c)) continue;
                if (!f.tocada) Tocar(f, c.center);
                if (vel < velocidadMinima) continue;
                f.recorrido += vel * Time.deltaTime;
                float largo = Mathf.Max(0.5f, Mathf.Max(b.size.x, b.size.y * 0.5f)) * pasadasHastaVaciarse;
                int deberia = Mathf.Min(f.total, Mathf.FloorToInt(f.total * f.recorrido / largo) + 1);
                if (deberia > f.soltados) Levantar(f, c.center.x, Mathf.Sign(rbChica.linearVelocity.x), deberia - f.soltados);
            }
        }
        Colorear();
        Mover();
    }

    private void Tocar(Fuente f, Vector2 dondeChica)
    {
        f.tocada = true;
        Bounds b = f.Limites;
        f.centroBlob = new Vector2(Mathf.Clamp(dondeChica.x, b.min.x, b.max.x), Mathf.Clamp(dondeChica.y, b.min.y, b.max.y));
        float dx = Mathf.Max(f.centroBlob.x - b.min.x, b.max.x - f.centroBlob.x);
        float dy = Mathf.Max(f.centroBlob.y - b.min.y, b.max.y - f.centroBlob.y);
        // distancia a la esquina mas lejana de la planta
        f.radioFinal = Mathf.Sqrt(dx * dx + dy * dy);
    }

    // la botellita se rompio: suelta de golpe lo que le quedaba
    public void SoltarTodo(BotellitaRompible b)
    {
        foreach (var f in fuentes)
            if (f.botellita == b && f.soltados < f.total)
            {
                if (!f.tocada) Tocar(f, f.Limites.center);
                Levantar(f, f.Limites.center.x, 0f, f.total - f.soltados);
            }
    }

    private void Colorear()
    {
        foreach (var f in fuentes)
        {
            if (!f.tocada || f.completa) continue;
            float objetivo = f.Soltado;
            f.colorMostrado = Mathf.MoveTowards(f.colorMostrado, objetivo, Time.deltaTime / Mathf.Max(0.01f, suavidadColor));
            bool lleno = f.colorMostrado >= 0.999f;

            if (f.botellita != null)
            {
                // la botellita se va apagando a medida que se vacia
                for (int i = 0; i < f.glows.Length; i++)
                    if (f.glows[i] != null) { var c = f.glows[i].color; c.a = f.alfaGlows[i] * (1f - 0.75f * f.colorMostrado); f.glows[i].color = c; }
                if (lleno) f.completa = true;
                continue;
            }
            if (f.mpb == null) continue;
            // el blob crece con lo que ya se solto; recien al 100 % tapa toda la planta
            // al 40 % de polen se ve mas o menos un tercio de la planta con color; al 100 % toda
            float e = f.colorMostrado;
            float radio = lleno ? f.radioFinal * 1.8f : f.radioFinal * (0.08f + 0.8f * e * e);
            f.mpb.SetVector(IdBlob, new Vector4(f.centroBlob.x, f.centroBlob.y, radio, Mathf.Max(0.05f, f.radioFinal * bordeBlob)));
            f.mpb.SetFloat(IdGris, lleno ? 0f : 1f);
            foreach (var p in f.partes) if (p != null) p.SetPropertyBlock(f.mpb);
            if (lleno) { f.completa = true; Rafaga(f); }
        }
    }

    // la planta ya tiene todo su color: una rafaga de destellos para que se note
    private void Rafaga(Fuente f)
    {
        if (destellos == null || destellos.Length == 0) return;
        Bounds b = f.Limites;
        for (int i = 0; i < destellosAlCompletar && activos.Count < maximo; i++)
        {
            var p = libres.Count > 0 ? libres.Pop() : Nuevo();
            p.soloDestello = true;
            p.pos = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
            p.posDestello = p.pos;
            p.vel = new Vector2(Random.Range(-0.4f, 0.4f), Random.Range(0.3f, 1.1f));
            p.vida = 0f; p.espera = Random.Range(0f, 0.25f); p.fase = Random.value * 10f; p.giro = 0f;
            p.estado = Estado.Subiendo; p.yFlotar = b.max.y + Random.Range(0.1f, 0.8f); p.vidaMax = Random.Range(1.2f, 2.2f); p.valor = 0f;
            p.sr.enabled = false;
            p.srd.sprite = destellos[Random.Range(0, destellos.Length)];
            p.tamD = Random.Range(0.35f, 0.8f) / Mathf.Max(0.01f, p.srd.sprite.bounds.size.y);
            p.t.gameObject.SetActive(false); p.td.gameObject.SetActive(false);
            activos.Add(p);
        }
    }

    // salen cerca de donde pasa la chica
    private void Levantar(Fuente f, float xChica, float sentido, int cantidad)
    {
        Bounds b = f.Limites;
        float valor = f.porcentaje / Mathf.Max(1, f.total);
        f.soltados += cantidad;
        float extra = cantidad * destellosSueltos;
        int sueltos = Mathf.FloorToInt(extra) + (Random.value < extra - Mathf.Floor(extra) ? 1 : 0);
        for (int i = 0; i < cantidad + sueltos; i++)
        {
            bool esPolen = i < cantidad;
            if (activos.Count >= maximo)
            {
                if (esPolen) pendiente += valor;   // no se ve, pero se suma igual
                continue;
            }
            var p = libres.Count > 0 ? libres.Pop() : Nuevo();
            p.soloDestello = !esPolen;
            p.valor = esPolen ? valor : 0f;
            float x = Mathf.Clamp(xChica + Random.Range(-1.2f, 1.2f), b.min.x, b.max.x);
            p.pos = new Vector2(x, Random.Range(b.min.y + b.size.y * 0.3f, b.max.y));
            p.posDestello = p.pos;
            float lento = p.soloDestello ? 0.6f : 1f;
            p.vel = new Vector2(sentido * empujeChica * Random.Range(0.3f, 1f) + Random.Range(-0.3f, 0.3f), Random.Range(subida.x, subida.y) * lento);
            p.vida = 0f;
            p.estado = Estado.Subiendo;
            p.espera = Random.Range(0f, desfaseMaximo);
            p.fase = Random.value * 10f;
            p.giro = Random.Range(-40f, 40f);
            p.yFlotar = b.max.y + sobreLaPlanta + Random.value * Random.value * alturaNube;
            p.flotarHasta = Random.Range(tiempoFlotando.x, tiempoFlotando.y);
            p.vidaMax = p.soloDestello ? Random.Range(1.6f, 3f) : float.MaxValue;
            float alto = Random.Range(altura.x, altura.y);
            p.sr.enabled = esPolen;
            bool diente = esPolen && (Random.Range(0, Mathf.Max(1, polenPorDiente)) == 0 || polenes == null || polenes.Length == 0);
            if (diente) p.sr.sprite = dientesDeLeon[Random.Range(0, dientesDeLeon.Length)];
            else if (polenes != null && polenes.Length > 0) { p.sr.sprite = polenes[Random.Range(0, polenes.Length)]; if (esPolen) alto *= tamanoPolen * 1.6f; }
            p.tam = alto / Mathf.Max(0.01f, p.sr.sprite.bounds.size.y);
            if (p.srd != null && destellos != null && destellos.Length > 0)
            {
                p.srd.sprite = destellos[Random.Range(0, destellos.Length)];
                p.tamD = alto * tamanoDestello * (p.soloDestello ? Random.Range(0.4f, 0.9f) : 1f) / Mathf.Max(0.01f, p.srd.sprite.bounds.size.y);
            }
            p.t.gameObject.SetActive(false);
            if (p.td != null) p.td.gameObject.SetActive(false);
            activos.Add(p);
        }
    }

    private void Volar(Polen p)
    {
        p.estado = Estado.Volando;
        p.inicioVuelo = Time.time;
        p.desdeVuelo = p.pos;
    }

    private Vector2 DestinoBarra()
    {
        if (camara == null) camara = BuscarCamara();
        if (BarraTemplo.Instancia == null || camara == null) return (Vector2)transform.position + Vector2.up * 20f;
        Vector2 s = BarraTemplo.Instancia.PuntoEnPantalla();
        return camara.ScreenToWorldPoint(new Vector3(s.x, s.y, Mathf.Abs(camara.transform.position.z - transform.position.z)));
    }

    private void Mover()
    {
        if (pendiente > 0f && BarraTemplo.Instancia != null) { BarraTemplo.Instancia.Sumar(pendiente); pendiente = 0f; }
        float dt = Time.deltaTime;
        float z = transform.position.z - 0.05f;
        Vector2 destino = Vector2.zero; bool hayDestino = false;

        for (int i = activos.Count - 1; i >= 0; i--)
        {
            var p = activos[i];
            if (p.espera > 0f)
            {
                p.espera -= dt;
                if (p.espera > 0f) continue;
                p.t.gameObject.SetActive(true);
                if (p.td != null) p.td.gameObject.SetActive(true);
            }
            p.vida += dt;
            float alfa = 1f, s;
            float aparecer = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p.vida / 0.25f));

            if (p.estado == Estado.Volando)
            {
                if (!hayDestino) { destino = DestinoBarra(); hayDestino = true; }
                float u = Mathf.Clamp01((Time.time - p.inicioVuelo) / segundosVuelo);
                float e = u * u * (3f - 2f * u);
                Vector2 control = p.desdeVuelo + Vector2.up * 2.5f;
                p.pos = Vector2.Lerp(Vector2.Lerp(p.desdeVuelo, control, e), Vector2.Lerp(control, destino, e), e);
                s = Mathf.Lerp(1f, 0.55f, e);
                if (u >= 1f)
                {
                    if (BarraTemplo.Instancia != null) BarraTemplo.Instancia.Sumar(p.valor);
                    Apagar(p, i); continue;
                }
            }
            else
            {
                p.vel *= Mathf.Exp(-1.6f * dt);
                float falta = p.yFlotar - p.pos.y;
                if (p.estado == Estado.Subiendo)
                {
                    p.vel.y = Mathf.Max(p.vel.y, Mathf.Min(0.6f, falta * 2f));
                    if (falta <= 0.02f) { p.estado = Estado.Flotando; p.xFlotar = p.pos.x; p.flotarHasta += p.vida; }
                    p.pos += p.vel * dt + new Vector2(Mathf.Sin(p.vida * 2.2f + p.fase) * vaiven * dt, 0f);
                }
                else
                {
                    p.xFlotar += p.vel.x * dt;
                    float bx = p.xFlotar + Mathf.Sin(p.vida * 0.9f + p.fase) * 0.12f;
                    float by = p.yFlotar + Mathf.Sin(p.vida * 1.3f + p.fase * 1.7f) * 0.04f;
                    p.pos = Vector2.Lerp(p.pos, new Vector2(bx, by), 1f - Mathf.Exp(-3f * dt));
                    // siempre se va a la barra despues de flotar un momento
                    if (!p.soloDestello && p.vida >= p.flotarHasta) Volar(p);
                }
                if (p.soloDestello)
                {
                    float fin = Mathf.Clamp01((p.vidaMax - p.vida) / 0.8f);
                    alfa = fin * (0.6f + 0.4f * Mathf.Sin(p.vida * 3.1f + p.fase));
                    if (p.vida >= p.vidaMax) { Apagar(p, i); continue; }
                }
                s = Mathf.Lerp(0.6f, 1f, aparecer);
            }
            alfa *= aparecer;

            p.t.position = new Vector3(p.pos.x, p.pos.y, z);
            p.t.localScale = Vector3.one * p.tam * s;
            p.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p.vida * 1.7f + p.fase) * 12f + p.giro * Mathf.Clamp01(p.vida / 2f) - p.vel.x * 10f);
            p.sr.color = new Color(1f, 1f, 1f, alfa);

            if (p.td != null)
            {
                p.posDestello = p.soloDestello ? p.pos : Vector2.Lerp(p.posDestello, p.pos, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, retrasoDestello)));
                p.td.position = new Vector3(p.posDestello.x, p.posDestello.y, z + 0.01f);
                float titila = 0.8f + 0.2f * Mathf.Sin(p.vida * 5f + p.fase);
                p.td.localScale = Vector3.one * p.tamD * s * titila;
                var c = colorGlow; c.a = alfaDestello * alfa * titila;
                p.srd.color = c;
            }
        }
    }

    // la camara del juego (la de CamaraSeguidora; la escena no tiene una con el tag MainCamera)
    private static Camera BuscarCamara()
    {
        var seguidora = FindFirstObjectByType<CamaraSeguidora>();
        if (seguidora != null && seguidora.TryGetComponent(out Camera c)) return c;
        return Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }

    private void Apagar(Polen p, int indice)
    {
        p.t.gameObject.SetActive(false);
        if (p.td != null) p.td.gameObject.SetActive(false);
        activos.RemoveAt(indice); libres.Push(p);
    }

    private Polen Nuevo()
    {
        var go = new GameObject("polen");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ordenDeDibujo;
        var gd = new GameObject("destello polen");
        gd.transform.SetParent(transform, true);
        var srd = gd.AddComponent<SpriteRenderer>();
        if (materialSinLuz != null) srd.sharedMaterial = materialSinLuz;
        srd.sortingOrder = ordenDeDibujo - 1;
        return new Polen { t = go.transform, sr = sr, td = gd.transform, srd = srd };
    }
}
