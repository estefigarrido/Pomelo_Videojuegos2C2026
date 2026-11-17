using System.Collections.Generic;
using UnityEngine;

// La estatua botanica empieza gris y con enredaderas. Cuando la chica llega con la gema al ultimo
// escalon de la escalera, en 'duracion' segundos la estatua recupera su color dorado y las enredaderas
// se desvanecen; al terminar aparecen destellos titilando sobre la estatua. Las 'piezas' (las columnas rotas
// del altar) cambian a su version arreglada con el mismo fundido.
// El dibujo dorado original queda abajo; encima se ponen la copia gris y las enredaderas.
[RequireComponent(typeof(SpriteRenderer))]
public class EstatuaRestaurada : MonoBehaviour
{
    [System.Serializable]
    public struct Colocacion
    {
        [Tooltip("Posicion en el mundo.")]
        public Vector2 posicion;
        [Tooltip("Tamano en el mundo respecto del sprite (negativo = espejado).")]
        public Vector2 escala;
    }

    [System.Serializable]
    public struct PiezaQueSeArregla
    {
        [Tooltip("La pieza rota que esta en la escena (por ejemplo una columna rota).")]
        public SpriteRenderer rota;
        [Tooltip("El dibujo arreglado: aparece encima con un fundido mientras la rota se desvanece.")]
        public Sprite arreglada;
    }

    [Header("Piezas que se arreglan junto con la estatua")]
    [Tooltip("Cambian con el mismo fundido que la estatua (apoyadas en el mismo piso que la rota).")]
    [SerializeField] private PiezaQueSeArregla[] piezas;

    [Header("Estado inicial")]
    [SerializeField] private Sprite estatuaGris;
    [SerializeField] private Sprite enredadera;
    [SerializeField] private Colocacion[] enredaderas;

    [Header("Cuando llega la chica con la gema")]
    [Tooltip("X del ultimo escalon: cuando la chica (con la gema) llega aca o mas a la izquierda, empieza.")]
    [SerializeField] private float xUltimoEscalon = -73.1f;
    [Tooltip("La chica tiene que estar arriba de la plataforma (Y minima).")]
    [SerializeField] private float yMinima = 33.5f;
    [SerializeField] private float duracion = 4f;

    [Header("Destellos al terminar")]
    [SerializeField] private Sprite[] framesDestello;
    [SerializeField] private float fpsDestello = 14f;
    [SerializeField] private Colocacion[] destellos;
    [SerializeField] private Material materialBrillo;

    [Header("Luz desde arriba (al repararla)")]
    [Tooltip("Color del haz de luz (crema, como en el Figma).")]
    [SerializeField] private Color colorLuz = new Color(0.96f, 0.94f, 0.86f, 1f);
    [Tooltip("Opacidad maxima del haz.")]
    [Range(0f, 1f)] [SerializeField] private float alfaLuz = 0.55f;
    [Tooltip("Opacidad del rayo del centro (el unico que pasa por delante de la estatua), respecto de alfaLuz.")]
    [Range(0f, 1f)] [SerializeField] private float alfaCentro = 0.5f;
    [Tooltip("Cuanto arriba de la cabeza de la estatua arranca el haz (unidades).")]
    [SerializeField] private float altoSobreLaEstatua = 9f;
    [Tooltip("Ancho del haz abajo, respecto del ancho de la estatua.")]
    [SerializeField] private float anchoAbajo = 2.4f;
    [Tooltip("Segundos que tarda el haz en bajar desde arriba y despues en abrirse a los costados.")]
    [SerializeField] private float segundosBajar = 0.9f;
    [SerializeField] private float segundosAbrir = 1.3f;
    [Tooltip("Brillitos (estrellas de 4 puntas) que titilan dentro del haz.")]
    [SerializeField] private int cantidadBrillitos = 26;

    private SpriteRenderer haz, nucleo, manchaPiso;
    private float inicioLuz = -1f;
    private Bounds cuerpo;
    private class Brillito { public SpriteRenderer sr; public float nace, dura, tam; public Vector3 pos, vel; }
    private readonly List<Brillito> brillitos = new List<Brillito>();
    private static Sprite spriteHaz, spriteMancha, spriteEstrella;

    // true cuando termino de recuperar el color (la animacion de la reparacion termino)
    public bool Terminada => estado == 2;

    private SpriteRenderer dibujo;
    private SpriteRenderer gris;
    private SpriteRenderer[] arregladas;
    private readonly List<SpriteRenderer> plantas = new List<SpriteRenderer>();
    private Transform chica;
    private int estado; // 0 gris, 1 recuperando el color, 2 dorada con destellos
    private float inicio;

    private class Destello { public SpriteRenderer sr; public float proximo; public float inicio; }
    private readonly List<Destello> activos = new List<Destello>();

    private void Start()
    {
        dibujo = GetComponent<SpriteRenderer>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;

        // copia gris exactamente encima de la dorada
        if (estatuaGris != null)
        {
            gris = CrearDibujo("Estatua gris", estatuaGris, dibujo.sortingOrder + 1, dibujo.sharedMaterial);
            gris.transform.SetParent(transform, false);
        }
        if (enredadera != null && enredaderas != null)
            foreach (var c in enredaderas)
            {
                var sr = CrearDibujo("Enredadera", enredadera, dibujo.sortingOrder + 2, dibujo.sharedMaterial);
                sr.transform.SetParent(transform, true);
                sr.transform.position = new Vector3(c.posicion.x, c.posicion.y, transform.position.z);
                sr.transform.localScale = new Vector3(c.escala.x / transform.lossyScale.x, c.escala.y / transform.lossyScale.y, 1f);
                plantas.Add(sr);
            }
        CrearArregladas();
    }

    // version arreglada de cada pieza, invisible, encima de la rota: misma capa y orden, apoyada en el mismo
    // piso (abajo y al centro de la rota), asi el cambio de dibujo no se corre
    private void CrearArregladas()
    {
        if (piezas == null) return;
        arregladas = new SpriteRenderer[piezas.Length];
        for (int i = 0; i < piezas.Length; i++)
        {
            var rota = piezas[i].rota;
            if (rota == null || rota.sprite == null || piezas[i].arreglada == null) continue;
            var go = new GameObject(rota.name + " (arreglada)");
            go.transform.SetParent(rota.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = piezas[i].arreglada;
            sr.sharedMaterial = rota.sharedMaterial;
            sr.sortingLayerID = rota.sortingLayerID;
            sr.sortingOrder = rota.sortingOrder;
            sr.flipX = rota.flipX;
            Vector3 pieRota = new Vector3(rota.sprite.bounds.center.x, rota.sprite.bounds.min.y, 0f);
            Vector3 pieArreglada = new Vector3(sr.sprite.bounds.center.x, sr.sprite.bounds.min.y, 0f);
            go.transform.localPosition = pieRota - pieArreglada + new Vector3(0f, 0f, -0.001f);
            SetAlfa(sr, 0f);
            arregladas[i] = sr;
        }
    }

    private void FundirPiezas(float k)
    {
        if (arregladas == null) return;
        for (int i = 0; i < arregladas.Length; i++)
        {
            if (arregladas[i] == null) continue;
            SetAlfa(piezas[i].rota, 1f - k);
            SetAlfa(arregladas[i], k);
        }
    }

    private SpriteRenderer CrearDibujo(string nombre, Sprite sprite, int orden, Material material)
    {
        var go = new GameObject(nombre);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = orden;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }

    private void Update()
    {
        if (estado == 0)
        {
            if (chica != null && GemaEnMano.TieneGema && chica.position.x <= xUltimoEscalon && chica.position.y >= yMinima)
            {
                estado = 1;
                inicio = Time.time;
                EncenderLuz();
            }
            return;
        }
        AnimarLuz();

        if (estado == 1)
        {
            float k = Mathf.Clamp01((Time.time - inicio) / Mathf.Max(0.01f, duracion));
            SetAlfa(gris, 1f - k);
            foreach (var p in plantas) SetAlfa(p, 1f - k);
            FundirPiezas(k);
            if (k >= 1f)
            {
                if (gris != null) Destroy(gris.gameObject);
                foreach (var p in plantas) Destroy(p.gameObject);
                plantas.Clear();
                CrearDestellos();
                estado = 2;
            }
            return;
        }

        AnimarDestellos();
    }

    // ---------- luz desde arriba ----------

    // El haz entra desde arriba (crece hacia abajo, finito) y al llegar al piso se abre a los costados.
    // Encima va un nucleo mas brillante, abajo una mancha de luz en el piso, y adentro titilan brillitos.
    private void EncenderLuz()
    {
        if (spriteHaz == null) CrearSpritesLuz();
        cuerpo = dibujo.bounds;
        if (gris != null) cuerpo.Encapsulate(gris.bounds);
        var mat = materialBrillo != null ? materialBrillo : dibujo.sharedMaterial;
        // el cono ancho y la mancha del piso van detras de la estatua (mismo orden, un poco mas lejos de la camara,
        // asi quedan delante del arco); solo el rayo del centro pasa por delante de ella
        haz = CrearDibujo("Luz desde arriba", spriteHaz, dibujo.sortingOrder, mat);
        nucleo = CrearDibujo("Luz desde arriba (centro)", spriteHaz, dibujo.sortingOrder + 3, mat);
        manchaPiso = CrearDibujo("Luz en el piso", spriteMancha, dibujo.sortingOrder, mat);
        foreach (var sr in new[] { haz, nucleo, manchaPiso }) { sr.transform.SetParent(transform, true); sr.color = Color.clear; }
        inicioLuz = Time.time;
    }

    private void AnimarLuz()
    {
        if (inicioLuz < 0f || haz == null) return;
        float t = Time.time - inicioLuz;
        float baja = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, segundosBajar));
        float abre = Mathf.SmoothStep(0f, 1f, (t - segundosBajar) / Mathf.Max(0.01f, segundosAbrir));
        float latido = 0.92f + 0.08f * Mathf.Sin(t * 1.6f);
        float arriba = cuerpo.max.y + altoSobreLaEstatua, abajo = cuerpo.min.y - 0.15f;
        float largo = (arriba - abajo) * baja;
        float ancho = cuerpo.size.x * anchoAbajo * Mathf.Lerp(0.18f, 1f, abre);
        Vector2 tam = spriteHaz.bounds.size;
        // el sprite tiene el pivote arriba al centro: crece hacia abajo desde 'arriba'
        haz.transform.position = new Vector3(cuerpo.center.x, arriba, transform.position.z + 0.05f);
        haz.transform.localScale = Escala(ancho / tam.x, Mathf.Max(0.001f, largo) / tam.y);
        haz.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, alfaLuz * Mathf.Min(1f, baja * 1.5f) * latido);
        nucleo.transform.position = new Vector3(cuerpo.center.x, arriba, transform.position.z - 0.02f);
        nucleo.transform.localScale = Escala(ancho * 0.45f / tam.x, Mathf.Max(0.001f, largo) / tam.y);
        nucleo.color = new Color(1f, 1f, 1f, alfaLuz * alfaCentro * Mathf.Min(1f, baja * 1.5f) * latido);
        Vector2 tm = spriteMancha.bounds.size;
        manchaPiso.transform.position = new Vector3(cuerpo.center.x, abajo, transform.position.z + 0.05f);
        manchaPiso.transform.localScale = Escala(ancho * 1.1f / tm.x, ancho * 0.22f / tm.y);
        manchaPiso.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, 0.45f * abre * latido);
        AnimarBrillitos(arriba, abajo, ancho, Mathf.Max(baja, 0f), abre);
    }

    private Vector3 Escala(float x, float y)
    {
        var p = transform.lossyScale;
        return new Vector3(x / Mathf.Max(0.0001f, Mathf.Abs(p.x)), y / Mathf.Max(0.0001f, Mathf.Abs(p.y)), 1f);
    }

    // aparecen de a poco mientras el haz se abre; cada uno crece, titila y se apaga, y vuelve a nacer en otro lado
    private void AnimarBrillitos(float arriba, float abajo, float ancho, float baja, float abre)
    {
        while (brillitos.Count < Mathf.RoundToInt(cantidadBrillitos * abre))
        {
            var sr = CrearDibujo("Brillito", spriteEstrella, dibujo.sortingOrder + 4, materialBrillo != null ? materialBrillo : dibujo.sharedMaterial);
            sr.transform.SetParent(transform, true);
            var b = new Brillito { sr = sr };
            Renacer(b, arriba, abajo, ancho);
            b.nace = Time.time - Random.value * b.dura;   // los primeros arrancan desparejos
            brillitos.Add(b);
        }
        foreach (var b in brillitos)
        {
            float u = (Time.time - b.nace) / b.dura;
            if (u >= 1f) { Renacer(b, arriba, abajo, ancho); u = 0f; }
            b.pos += b.vel * Time.deltaTime;
            b.sr.transform.position = b.pos;
            float s = Mathf.Sin(u * Mathf.PI);   // crece y se achica
            b.sr.transform.localScale = Escala(b.tam * s, b.tam * s);
            b.sr.transform.localRotation = Quaternion.Euler(0f, 0f, u * 40f);
            b.sr.color = new Color(1f, 1f, 1f, 0.95f * s);
        }
    }

    private void Renacer(Brillito b, float arriba, float abajo, float ancho)
    {
        float t = Random.Range(0.08f, 1f);   // mas abajo = el haz es mas ancho
        float y = Mathf.Lerp(arriba, abajo, t);
        float medio = Mathf.Lerp(ancho * 0.2f, ancho, t) * 0.45f;
        b.pos = new Vector3(cuerpo.center.x + Random.Range(-medio, medio), y, transform.position.z - 0.03f);
        b.vel = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.25f, 0.1f), 0f);
        b.dura = Random.Range(0.9f, 2.2f);
        b.tam = Random.Range(0.35f, 0.95f) / Mathf.Max(0.01f, spriteEstrella.bounds.size.x);
        b.nace = Time.time;
    }

    // sprites hechos por codigo: el haz (trapecio que se ensancha hacia abajo, con bordes suaves y que se
    // apaga hacia el piso), la mancha del piso (elipse difusa) y la estrella de 4 puntas de los brillitos
    // smoothstep de verdad (0 en 'desde', 1 en 'hasta'); Mathf.SmoothStep hace otra cosa (interpola entre dos valores)
    private static float Suave(float desde, float hasta, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(desde, hasta, x));

    private static void CrearSpritesLuz()
    {
        const int w = 128, h = 256;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "luz haz" };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (h - 1f);                       // 0 abajo, 1 arriba
            float medio = Mathf.Lerp(0.5f, 0.035f, v);    // cono: casi en punta arriba y abierto abajo
            float vert = Mathf.Lerp(0.35f, 1f, v) * Suave(0f, 0.08f, v);
            for (int x = 0; x < w; x++)
            {
                float d = Mathf.Abs((x + 0.5f) / w - 0.5f);
                float borde = 1f - Suave(medio * 0.6f, medio, d);
                px[y * w + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(borde * vert)));
            }
        }
        t.SetPixels32(px); t.Apply();
        spriteHaz = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 100f);

        const int m = 128;
        var tm = new Texture2D(m, m, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "luz mancha" };
        var pm = new Color32[m * m];
        for (int y = 0; y < m; y++) for (int x = 0; x < m; x++)
        {
            float dx = (x + 0.5f) / m * 2f - 1f, dy = (y + 0.5f) / m * 2f - 1f;
            float a = 1f - Suave(0f, 1f, Mathf.Sqrt(dx * dx + dy * dy));
            pm[y * m + x] = new Color32(255, 255, 255, (byte)(255 * a * a));
        }
        tm.SetPixels32(pm); tm.Apply();
        spriteMancha = Sprite.Create(tm, new Rect(0, 0, m, m), new Vector2(0.5f, 0.5f), 100f);

        const int e = 64;
        var te = new Texture2D(e, e, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "luz brillito" };
        var pe = new Color32[e * e];
        for (int y = 0; y < e; y++) for (int x = 0; x < e; x++)
        {
            float dx = Mathf.Abs((x + 0.5f) / e * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / e * 2f - 1f);
            // cuatro puntas finitas (como los destellos del Figma) y un centro redondo
            // cada punta se afina hacia afuera (el ancho baja con la distancia al centro)
            float puntaV = (1f - Suave(0f, 0.16f * (1f - dy), dx)) * (1f - dy);
            float puntaH = (1f - Suave(0f, 0.16f * (1f - dx), dy)) * (1f - dx);
            float punta = Mathf.Max(puntaV, puntaH);
            float centro = 1f - Suave(0.05f, 0.22f, Mathf.Sqrt(dx * dx + dy * dy));
            pe[y * e + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(Mathf.Max(punta, centro))));
        }
        te.SetPixels32(pe); te.Apply();
        spriteEstrella = Sprite.Create(te, new Rect(0, 0, e, e), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        var c = sr.color; c.a = a; sr.color = c;
    }

    private void CrearDestellos()
    {
        if (framesDestello == null || framesDestello.Length == 0 || destellos == null) return;
        foreach (var c in destellos)
        {
            var sr = CrearDibujo("Destello", framesDestello[0], dibujo.sortingOrder + 3, materialBrillo != null ? materialBrillo : dibujo.sharedMaterial);
            sr.transform.SetParent(transform, true);
            sr.transform.position = new Vector3(c.posicion.x, c.posicion.y, transform.position.z);
            sr.transform.localScale = new Vector3(c.escala.x / transform.lossyScale.x, c.escala.y / transform.lossyScale.y, 1f);
            sr.enabled = false;
            // cada destello arranca en un momento distinto, asi titilan desparejo
            activos.Add(new Destello { sr = sr, proximo = Time.time + Random.Range(0f, 0.8f) });
        }
    }

    // cada destello crece (001 -> 005), se achica (005 -> 001) y espera un rato antes de volver a brillar
    private void AnimarDestellos()
    {
        int n = framesDestello.Length;
        int largo = n * 2 - 1;
        foreach (var d in activos)
        {
            if (d.inicio <= 0f)
            {
                if (Time.time < d.proximo) continue;
                d.inicio = Time.time;
                d.sr.enabled = true;
            }
            int f = Mathf.FloorToInt((Time.time - d.inicio) * fpsDestello);
            if (f >= largo)
            {
                d.sr.enabled = false;
                d.inicio = 0f;
                d.proximo = Time.time + Random.Range(0.3f, 1.3f);
                continue;
            }
            d.sr.sprite = framesDestello[f < n ? f : largo - 1 - f];
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        Gizmos.DrawLine(new Vector3(xUltimoEscalon, yMinima - 1f), new Vector3(xUltimoEscalon, yMinima + 4f));
    }
}
