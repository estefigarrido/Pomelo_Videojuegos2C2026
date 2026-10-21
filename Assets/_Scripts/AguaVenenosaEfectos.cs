using System.Collections.Generic;
using UnityEngine;

// Efectos del agua venenosa violeta (va en cada "Lago N"):
//  - la superficie hierve: la linea brillante es una malla que ondula con dos ondas suaves
//    y "borbotones" que saltan en lugares al azar (y donde revienta cada burbuja que sube);
//  - suben muchas burbujitas desde el fondo meciendose y otras flotan sobre la superficie;
//  - el resplandor de adentro del agua late suave.
// El lago se mide con el dibujo del agua violeta.
public class AguaVenenosaEfectos : MonoBehaviour
{
    [SerializeField] private SpriteRenderer agua;
    [SerializeField] private SpriteRenderer brilloSuperficie;
    [SerializeField] private SpriteRenderer glowInterior;
    [SerializeField] private Sprite burbuja;

    [Header("Brillo de la superficie")]
    [Tooltip("Cuanto titila (0 = nada) y cuantas veces por segundo.")]
    [SerializeField] private float titileo = 0.18f;
    [SerializeField] private float velocidadTitileo = 0.6f;

    [Header("Superficie hirviendo")]
    [Tooltip("Alto de las ondas (unidades) y que tan rapido corren.")]
    [SerializeField] private float alturaOndas = 0.035f;
    [SerializeField] private float velocidadOndas = 1.8f;
    [Tooltip("Borbotones por segundo, su alto (unidades) y su ancho (unidades).")]
    [SerializeField] private float borbotonesPorSegundo = 7f;
    [SerializeField] private float alturaBorboton = 0.09f;
    [SerializeField] private float anchoBorboton = 0.25f;
    [Tooltip("Separacion entre los puntos de la malla de la superficie (unidades).")]
    [SerializeField] private float detalle = 0.1f;

    [Header("Burbujas que suben")]
    [SerializeField] private float subenPorSegundo = 14f;
    [SerializeField] private Vector2 velocidadSubida = new Vector2(0.5f, 1.3f);
    [SerializeField] private Vector2 tamanoQueSuben = new Vector2(0.15f, 0.5f);
    [SerializeField] private float vaiven = 0.12f;

    [Header("Burbujas en la superficie")]
    [SerializeField] private float flotanPorSegundo = 5f;
    [Tooltip("Segundos que dura cada una flotando.")]
    [SerializeField] private Vector2 vidaFlotando = new Vector2(1.2f, 3f);
    [SerializeField] private Vector2 tamanoFlotando = new Vector2(0.15f, 0.35f);

    [Header("Resplandor de adentro")]
    [SerializeField] private float latidoGlow = 0.15f;
    [SerializeField] private float velocidadLatido = 0.35f;

    private class Burbuja
    {
        public Transform t; public SpriteRenderer sr;
        public float x, y, vel, deriva, fase, vida, duracion, tam; public bool flota;
    }

    private struct Borboton { public float x, inicio, alto; }

    private readonly List<Burbuja> activas = new List<Burbuja>();
    private readonly Stack<Burbuja> libres = new Stack<Burbuja>();
    private readonly List<Borboton> borbotones = new List<Borboton>();
    private float acumSuben, acumFlotan, acumBorbotones, alfaBrillo = 1f, alfaGlow = 1f;

    // malla de la superficie
    private Mesh malla;
    private Vector3[] vertices;
    private Color[] colores;
    private int columnas;
    private float anchoMalla, altoMalla, xMalla;

    private void Start()
    {
        if (glowInterior != null) alfaGlow = glowInterior.color.a;
        if (brilloSuperficie != null) { alfaBrillo = brilloSuperficie.color.a; CrearMalla(); }
    }

    // reemplaza la linea quieta por una malla con la misma imagen que se puede ondular
    private void CrearMalla()
    {
        var sp = brilloSuperficie.sprite;
        if (sp == null) return;
        Bounds bb = brilloSuperficie.bounds;
        anchoMalla = bb.size.x; altoMalla = bb.size.y; xMalla = bb.min.x;
        columnas = Mathf.Max(2, Mathf.CeilToInt(anchoMalla / Mathf.Max(0.02f, detalle)));

        var go = new GameObject("superficie hirviendo");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(bb.min.x, bb.max.y, brilloSuperficie.transform.position.z);
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(brilloSuperficie.sharedMaterial) { mainTexture = sp.texture };
        mr.sharedMaterial = mat;
        mr.sortingLayerID = brilloSuperficie.sortingLayerID;
        mr.sortingOrder = brilloSuperficie.sortingOrder;

        vertices = new Vector3[(columnas + 1) * 2];
        colores = new Color[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var tris = new int[columnas * 6];
        for (int i = 0; i <= columnas; i++)
        {
            float u = (float)i / columnas;
            vertices[i * 2] = new Vector3(u * anchoMalla, 0f, 0f);
            vertices[i * 2 + 1] = new Vector3(u * anchoMalla, -altoMalla, 0f);
            uvs[i * 2] = new Vector2(0.5f, 1f);
            uvs[i * 2 + 1] = new Vector2(0.5f, 0f);
            if (i < columnas)
            {
                int k = i * 6, v = i * 2;
                tris[k] = v; tris[k + 1] = v + 2; tris[k + 2] = v + 1;
                tris[k + 3] = v + 1; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
            }
        }
        malla = new Mesh { name = "superficie agua venenosa" };
        malla.vertices = vertices; malla.uv = uvs; malla.triangles = tris;
        malla.MarkDynamic();
        mf.sharedMesh = malla;
        brilloSuperficie.enabled = false;
    }

    private void Update()
    {
        if (agua == null) return;
        Bounds b = agua.bounds;
        float t = Time.time;

        if (glowInterior != null)
        {
            var c = glowInterior.color;
            c.a = alfaGlow * (1f - latidoGlow * 0.5f + latidoGlow * 0.5f * Mathf.Sin(t * velocidadLatido * Mathf.PI * 2f));
            glowInterior.color = c;
        }

        acumBorbotones += borbotonesPorSegundo * Time.deltaTime;
        while (acumBorbotones >= 1f) { acumBorbotones -= 1f; Borbotear(Random.Range(b.min.x, b.max.x), 1f); }
        ActualizarMalla(t);

        if (burbuja == null) return;
        acumSuben += subenPorSegundo * Time.deltaTime;
        while (acumSuben >= 1f) { acumSuben -= 1f; Soltar(b, false); }
        acumFlotan += flotanPorSegundo * Time.deltaTime;
        while (acumFlotan >= 1f) { acumFlotan -= 1f; Soltar(b, true); }

        for (int i = activas.Count - 1; i >= 0; i--)
        {
            var p = activas[i];
            p.vida += Time.deltaTime;
            float u = p.vida / p.duracion;
            if (u >= 1f)
            {
                if (!p.flota) Borbotear(p.x, 0.6f);   // revienta en la superficie
                p.t.gameObject.SetActive(false); activas.RemoveAt(i); libres.Push(p); continue;
            }
            float x, y, a;
            if (p.flota)
            {
                p.x += p.deriva * Time.deltaTime;
                x = p.x; y = b.max.y - 0.04f + AlturaSuperficie(p.x, t) + Mathf.Sin((p.vida + p.fase) * 3f) * 0.02f;
                a = Mathf.Clamp01(Mathf.Min(u * 4f, (1f - u) * 3f));
            }
            else
            {
                p.y += p.vel * Time.deltaTime;
                x = p.x + Mathf.Sin((p.vida * 1.5f + p.fase) * Mathf.PI * 2f) * vaiven;
                y = p.y;
                a = Mathf.Clamp01(Mathf.Min(u * 4f, (b.max.y - p.y) / 0.4f));
            }
            p.t.position = new Vector3(x, y, transform.position.z - 0.02f);
            var col = p.sr.color; col.a = a; p.sr.color = col;
        }
    }

    private void Borbotear(float x, float fuerza) =>
        borbotones.Add(new Borboton { x = x, inicio = Time.time, alto = alturaBorboton * fuerza * Random.Range(0.6f, 1f) });

    // cuanto sube o baja la superficie en x (ondas + borbotones que saltan y se apagan)
    private float AlturaSuperficie(float x, float t)
    {
        float y = Mathf.Sin(x * 2.3f - t * velocidadOndas) * alturaOndas * 0.6f
                + Mathf.Sin(x * 5.7f + t * velocidadOndas * 1.6f) * alturaOndas * 0.4f;
        for (int i = 0; i < borbotones.Count; i++)
        {
            var bo = borbotones[i];
            float edad = t - bo.inicio, d = (x - bo.x) / anchoBorboton;
            if (d < -3f || d > 3f) continue;
            // salta rapido y se apaga oscilando
            y += bo.alto * Mathf.Exp(-d * d) * Mathf.Exp(-edad * 3.5f) * Mathf.Cos(edad * 14f);
        }
        return y;
    }

    private void ActualizarMalla(float t)
    {
        borbotones.RemoveAll(bo => t - bo.inicio > 1.5f);
        if (malla == null) return;
        float a = alfaBrillo * (1f - titileo * 0.5f + titileo * 0.5f * Mathf.Sin(t * velocidadTitileo * Mathf.PI * 2f));
        var c = new Color(1f, 1f, 1f, a);
        for (int i = 0; i <= columnas; i++)
        {
            float lx = (float)i / columnas * anchoMalla;
            float dy = AlturaSuperficie(xMalla + lx, t);
            vertices[i * 2] = new Vector3(lx, dy, 0f);
            vertices[i * 2 + 1] = new Vector3(lx, dy - altoMalla, 0f);
            colores[i * 2] = c; colores[i * 2 + 1] = c;
        }
        malla.vertices = vertices;
        malla.colors = colores;
        malla.RecalculateBounds();
    }

    private void Soltar(Bounds b, bool flota)
    {
        var p = libres.Count > 0 ? libres.Pop() : Nueva();
        p.flota = flota;
        p.vida = 0f;
        p.fase = Random.value * 10f;
        p.x = Random.Range(b.min.x + 0.2f, b.max.x - 0.2f);
        if (flota)
        {
            p.tam = Random.Range(tamanoFlotando.x, tamanoFlotando.y);
            p.duracion = Random.Range(vidaFlotando.x, vidaFlotando.y);
            p.deriva = Random.Range(-0.25f, 0.25f);
        }
        else
        {
            p.tam = Random.Range(tamanoQueSuben.x, tamanoQueSuben.y);
            p.y = b.min.y + Random.Range(0.1f, b.size.y * 0.7f);
            p.vel = Random.Range(velocidadSubida.x, velocidadSubida.y);
            p.duracion = Mathf.Max(0.1f, (b.max.y - p.y) / p.vel);
        }
        p.t.localScale = Vector3.one * p.tam;
        p.t.gameObject.SetActive(true);
        activas.Add(p);
    }

    private Burbuja Nueva()
    {
        var go = new GameObject("burbuja");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = burbuja;
        if (agua != null)
        {
            sr.sharedMaterial = agua.sharedMaterial;
            sr.sortingLayerID = agua.sortingLayerID;
            sr.sortingOrder = agua.sortingOrder;
        }
        return new Burbuja { t = go.transform, sr = sr };
    }
}
