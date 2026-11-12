using System.Collections.Generic;
using UnityEngine;

// Restaura el templo de a sectores (los 30 rectangulos azules y verdes del Figma, sobre "image 306").
// Cada vez que BarraTemplo pide restaurar, arranca el sector sin restaurar mas cercano a la chica:
// el mapa roto se va y aparece el mapa arreglado con un barrido de borde irregular que avanza desde el
// lado de la chica, con una estela de luz en el borde (shader "Pomelo/Sprite Lit Restaurable") y
// destellos que suben desde el frente.
// La mascara es una textura chica (pixelesPorUnidad) que cubre todos los sectores. Canal R: 0 = roto,
// 1 = arreglado. Canal G: la estela de luz, solo en el frente mientras avanza (al terminar se apaga). Los dos mapas la leen como textura global (_MascaraSectores / _RectSectores).
public class RestauracionTemplo : MonoBehaviour
{
    [Tooltip("Los sectores en el mundo (los pone el menu de configuracion a partir del Figma).")]
    [SerializeField] private Rect[] sectores;
    [Tooltip("Sectores que se arreglan con la gema de Umbrae (sin gastar polen): la chica pasa con la gema en la mano " +
             "y aprieta click derecho (o el boton de reparar del joystick). No cuentan para el reparto del polen.")]
    [SerializeField] private Rect[] sectoresGema;
    [SerializeField] private float pixelesPorUnidad = 4f;
    [Tooltip("Cuanto se esfuma el borde del sector hacia afuera (unidades), para que no se vea el corte recto.")]
    [SerializeField] private float margen = 0.8f;
    [SerializeField] private float segundosRestaurar = 2.4f;
    [Tooltip("Ancho del borde suave del barrido (0 a 1 del sector).")]
    [SerializeField] private float suavidad = 0.14f;

    [Header("Estela")]
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Material materialSinLuz;
    [SerializeField] private Color colorEstela = new Color(1f, 0.93f, 0.62f, 0.8f);
    [SerializeField] private float destellosPorSegundo = 30f;
    [Tooltip("Orden adentro de la capa FondoDetalles (arriba de las lianas del mapa).")]
    [SerializeField] private int ordenEstela = 300;

    public static RestauracionTemplo Instancia { get; private set; }
    public bool QuedanSectores => restaurados < (sectores != null ? sectores.Length : 0);
    public int Restaurados => restaurados;
    public event System.Action<int> SectorRestaurado;

    private class Animacion { public int sector; public float inicio; public bool desdeDerecha; public float acumulado; }
    private class Destello { public SpriteRenderer sr; public Vector2 pos, vel; public float vida, dur, tam; }

    private Rect[] todos;          // los de polen y despues los de la gema
    private Texture2D mascara;
    private byte[] pixeles;
    private Rect total;
    private int ancho, alto;
    private bool[] hecho;
    private int restaurados;
    private readonly List<Animacion> animando = new List<Animacion>();
    private readonly List<Destello> activos = new List<Destello>();
    private readonly Stack<Destello> libres = new Stack<Destello>();
    private Transform chica;

    private static readonly int IdMascara = Shader.PropertyToID("_MascaraSectores");
    private static readonly int IdRect = Shader.PropertyToID("_RectSectores");

    private void Awake()
    {
        Instancia = this;
        if (sectores == null || sectores.Length == 0) { enabled = false; return; }
        var lista = new List<Rect>(sectores);
        if (sectoresGema != null) lista.AddRange(sectoresGema);
        todos = lista.ToArray();
        total = todos[0];
        foreach (var s in todos) total = Rect.MinMaxRect(Mathf.Min(total.xMin, s.xMin), Mathf.Min(total.yMin, s.yMin), Mathf.Max(total.xMax, s.xMax), Mathf.Max(total.yMax, s.yMax));
        total = Rect.MinMaxRect(total.xMin - margen, total.yMin - margen, total.xMax + margen, total.yMax + margen);
        ancho = Mathf.CeilToInt(total.width * pixelesPorUnidad);
        alto = Mathf.CeilToInt(total.height * pixelesPorUnidad);
        mascara = new Texture2D(ancho, alto, TextureFormat.RG16, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Mascara sectores" };
        pixeles = new byte[ancho * alto * 2];
        mascara.SetPixelData(pixeles, 0); mascara.Apply(false);
        hecho = new bool[todos.Length];
        Shader.SetGlobalTexture(IdMascara, mascara);
        Shader.SetGlobalVector(IdRect, new Vector4(total.xMin, total.yMin, 1f / total.width, 1f / total.height));
    }

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
    }

    private void OnDestroy()
    {
        // sin el script, todo se ve como roto
        Shader.SetGlobalVector(IdRect, new Vector4(0f, 0f, 0f, 0f));
        if (mascara != null) Destroy(mascara);
    }

    public Rect[] Sectores => sectores;

    // el sector donde esta parada la chica (-1 si no esta en ninguno). Indice en 'todos':
    // los primeros sectores.Length son los de polen y los que siguen, los de la gema
    public int SectorDeLaChica()
    {
        if (chica == null || todos == null) return -1;
        var col = chica.GetComponent<Collider2D>();
        Vector2 p = col != null ? (Vector2)col.bounds.center : (Vector2)chica.position;
        int mejor = -1; float dist = 0.6f;   // un poco de tolerancia en los bordes
        for (int i = 0; i < todos.Length; i++)
        {
            float d = DistanciaARect(p, todos[i]);
            if (d <= 0f) return i;
            if (d < dist) { dist = d; mejor = i; }
        }
        return mejor;
    }

    private bool EsDeGema(int i) => i >= sectores.Length;

    // la chica esta en una parte del templo que todavia no se arreglo y que se arregla con polen (el boton titila mas fuerte)
    public bool ChicaEnSectorSinRestaurar { get { int s = SectorDeLaChica(); return s >= 0 && !hecho[s] && !EsDeGema(s); } }

    // la chica tiene la gema en la mano y esta en una parte que se arregla con la gema y todavia no se arreglo
    public bool PuedeRestaurarConGema { get { int s = SectorDeLaChica(); return enabled && GemaEnMano.TieneGema && s >= 0 && EsDeGema(s) && !hecho[s]; } }

    // arregla el sector de la gema donde esta la chica (gratis, sin polen). False si no se puede
    public bool RestaurarConGema()
    {
        if (!PuedeRestaurarConGema) return false;
        int s = SectorDeLaChica();
        hecho[s] = true;
        Vector2 desde = (Vector2)chica.position;
        animando.Add(new Animacion { sector = s, inicio = Time.time, desdeDerecha = desde.x > todos[s].center.x });
        return true;
    }

    // fila (piso) de sectores a la que pertenece una altura: la que la contiene, o la mas cercana
    public int Fila(float y)
    {
        int mejor = 0; float dist = float.MaxValue;
        foreach (var s in sectores)
        {
            float d = y < s.yMin ? s.yMin - y : (y > s.yMax ? y - s.yMax : 0f);
            if (d < dist) { dist = d; mejor = Mathf.RoundToInt(s.yMin); }
        }
        return mejor;
    }

    // restaura el sector donde esta la chica. Si no esta en un sector, o ya esta arreglado, no hace nada
    // (devuelve false y la barra no gasta polen)
    public bool Restaurar()
    {
        if (!enabled || !QuedanSectores) return false;
        Vector2 desde = chica != null ? (Vector2)chica.position : total.center;
        int mejor = SectorDeLaChica();
        if (mejor < 0 || hecho[mejor] || EsDeGema(mejor)) return false;   // los de la gema no se arreglan con polen
        hecho[mejor] = true;
        restaurados++;
        animando.Add(new Animacion { sector = mejor, inicio = Time.time, desdeDerecha = desde.x > todos[mejor].center.x });
        return true;
    }

    private static float DistanciaARect(Vector2 p, Rect r)
    {
        float dx = Mathf.Max(r.xMin - p.x, 0f, p.x - r.xMax);
        float dy = Mathf.Max(r.yMin - p.y, 0f, p.y - r.yMax);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private void Update()
    {
        bool cambio = false;
        for (int i = animando.Count - 1; i >= 0; i--)
        {
            var a = animando[i];
            float t = Mathf.Clamp01((Time.time - a.inicio) / Mathf.Max(0.01f, segundosRestaurar));
            float suave = t * t * (3f - 2f * t);
            Pintar(a, suave);
            Estela(a, suave);
            cambio = true;
            if (t >= 1f) { animando.RemoveAt(i); SectorRestaurado?.Invoke(a.sector); }
        }
        if (cambio) { mascara.SetPixelData(pixeles, 0); mascara.Apply(false); }
        MoverDestellos();
    }

    // valor del barrido en un punto: 0 = todavia roto, 1 = arreglado
    private float Barrido(Animacion a, Rect s, float x, float y, float t)
    {
        float u = (x - s.xMin) / s.width;
        if (a.desdeDerecha) u = 1f - u;
        // borde irregular, como el blob de las plantas
        float ruido = (Mathf.PerlinNoise(x * 0.45f + a.sector * 7.3f, y * 0.45f) - 0.5f) * 0.3f;
        float frente = t * (1f + suavidad + 0.3f) - suavidad * 0.5f;
        return Mathf.Clamp01((frente - u - ruido) / suavidad + 0.5f);
    }

    private void Pintar(Animacion a, float t)
    {
        Rect s = todos[a.sector];
        int x0 = Mathf.Max(0, Mathf.FloorToInt((s.xMin - margen - total.xMin) * pixelesPorUnidad));
        int x1 = Mathf.Min(ancho - 1, Mathf.CeilToInt((s.xMax + margen - total.xMin) * pixelesPorUnidad));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((s.yMin - margen - total.yMin) * pixelesPorUnidad));
        int y1 = Mathf.Min(alto - 1, Mathf.CeilToInt((s.yMax + margen - total.yMin) * pixelesPorUnidad));
        for (int py = y0; py <= y1; py++)
        {
            float y = total.yMin + (py + 0.5f) / pixelesPorUnidad;
            for (int px = x0; px <= x1; px++)
            {
                float x = total.xMin + (px + 0.5f) / pixelesPorUnidad;
                // afuera del sector se esfuma en 'margen'
                float fuera = Mathf.Max(Mathf.Max(s.xMin - x, x - s.xMax), Mathf.Max(s.yMin - y, y - s.yMax));
                float borde = Mathf.Clamp01(1f - fuera / Mathf.Max(0.01f, margen));
                float barrido = Barrido(a, s, Mathf.Clamp(x, s.xMin, s.xMax), y, t);
                float v = barrido * borde;
                byte b = (byte)Mathf.RoundToInt(v * 255f);
                int idx = (py * ancho + px) * 2;
                if (b > pixeles[idx]) pixeles[idx] = b;
                // estela: banda en el frente del barrido, que se apaga al final
                float estela = (1f - Mathf.Abs(barrido * 2f - 1f)) * borde * Mathf.Clamp01((1f - t) * 4f);
                pixeles[idx + 1] = (byte)Mathf.RoundToInt(estela * 255f);
            }
        }
    }

    // destellos que nacen en el frente del barrido y suben despacio
    private void Estela(Animacion a, float t)
    {
        if (destellos == null || destellos.Length == 0 || t >= 1f) return;
        Rect s = todos[a.sector];
        a.acumulado += destellosPorSegundo * Time.deltaTime;
        while (a.acumulado >= 1f)
        {
            a.acumulado -= 1f;
            float y = Random.Range(s.yMin, s.yMax);
            float frente = t * (1f + suavidad + 0.3f) - suavidad * 0.5f;
            float u = Mathf.Clamp01(frente + Random.Range(-0.04f, 0.04f));
            float x = a.desdeDerecha ? s.xMax - u * s.width : s.xMin + u * s.width;
            var d = libres.Count > 0 ? libres.Pop() : Nuevo();
            d.pos = new Vector2(x, y);
            d.vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1.2f));
            d.vida = 0f; d.dur = Random.Range(0.9f, 1.6f);
            d.sr.sprite = destellos[Random.Range(0, destellos.Length)];
            d.tam = Random.Range(0.35f, 0.9f) / Mathf.Max(0.01f, d.sr.sprite.bounds.size.y);
            d.sr.gameObject.SetActive(true);
            activos.Add(d);
        }
    }

    private Destello Nuevo()
    {
        var go = new GameObject("estela restauracion");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
        sr.sortingLayerName = Capas.FondoDetalles; sr.sortingOrder = ordenEstela;
        return new Destello { sr = sr };
    }

    private void MoverDestellos()
    {
        float dt = Time.deltaTime;
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            var d = activos[i];
            d.vida += dt;
            float u = d.vida / d.dur;
            if (u >= 1f) { d.sr.gameObject.SetActive(false); activos.RemoveAt(i); libres.Push(d); continue; }
            d.pos += d.vel * dt;
            d.vel *= Mathf.Exp(-0.8f * dt);
            float alfa = Mathf.Sin(u * Mathf.PI);
            d.sr.transform.position = new Vector3(d.pos.x, d.pos.y, 0f);
            d.sr.transform.localScale = Vector3.one * d.tam * Mathf.Lerp(0.6f, 1.1f, u);
            var c = colorEstela; c.a *= alfa;
            d.sr.color = c;
        }
    }
}
