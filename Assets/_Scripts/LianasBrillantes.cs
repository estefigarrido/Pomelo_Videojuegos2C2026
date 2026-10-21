using System.Collections.Generic;
using UnityEngine;

// Va en "Fondo roto/lianas": de las lianas que se ven en pantalla van saliendo particulitas violetas y rosas
// que caen despacio meciendose y se apagan. (El resplandor de cada liana es un dibujo aparte, "brillo".)
public class LianasBrillantes : MonoBehaviour
{
    [SerializeField] private Sprite particula;
    [SerializeField] private Material material;
    [Tooltip("Particulas por segundo por cada liana que se ve en pantalla.")]
    [SerializeField] private float porSegundoPorLiana = 0.9f;
    [SerializeField] private Vector2 velocidadCaida = new Vector2(0.12f, 0.35f);
    [SerializeField] private Vector2 vida = new Vector2(2.5f, 5f);
    [SerializeField] private Vector2 tamano = new Vector2(0.18f, 0.45f);
    [SerializeField] private float vaiven = 0.25f;
    [SerializeField] private Color rosa = new Color(1f, 0.38f, 0.7f, 1f);
    [SerializeField] private Color violeta = new Color(0.68f, 0.35f, 1f, 1f);
    [SerializeField] private int ordenDeDibujo = -630;   // arriba del fondo, abajo del mapa y la chica
    [Tooltip("Maximo de particulas a la vez.")]
    [SerializeField] private int maximo = 220;

    private class P { public Transform t; public SpriteRenderer sr; public Vector2 pos; public float vel, vida, dur, fase, tam; public Color col; }

    private readonly List<P> activas = new List<P>();
    private readonly Stack<P> libres = new Stack<P>();
    private readonly List<SpriteRenderer> lianas = new List<SpriteRenderer>();
    private readonly Dictionary<SpriteRenderer, float> acumulado = new Dictionary<SpriteRenderer, float>();
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        foreach (Transform hijo in transform)
        {
            var sr = hijo.GetComponent<SpriteRenderer>();
            if (sr != null) { lianas.Add(sr); acumulado[sr] = Random.value; }
        }
    }

    private void Update()
    {
        if (particula == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // lo que se ve, con un margen
        float alto = cam.orthographicSize + 2f, ancho = alto * cam.aspect + 2f;
        var c = cam.transform.position;
        var vista = new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(ancho * 2f, alto * 2f, 1000f));

        foreach (var l in lianas)
        {
            var b = l.bounds;
            if (!vista.Intersects(new Bounds(new Vector3(b.center.x, b.center.y, 0f), new Vector3(b.size.x, b.size.y, 1000f)))) continue;
            acumulado[l] += porSegundoPorLiana * Time.deltaTime;
            while (acumulado[l] >= 1f && activas.Count < maximo) { acumulado[l] -= 1f; Soltar(b); }
        }

        for (int i = activas.Count - 1; i >= 0; i--)
        {
            var p = activas[i];
            p.vida += Time.deltaTime;
            float u = p.vida / p.dur;
            if (u >= 1f) { p.t.gameObject.SetActive(false); activas.RemoveAt(i); libres.Push(p); continue; }
            p.pos.y -= p.vel * Time.deltaTime;
            float x = p.pos.x + Mathf.Sin((p.vida * 0.8f + p.fase) * Mathf.PI * 2f) * vaiven;
            p.t.position = new Vector3(x, p.pos.y, transform.position.z - 0.02f);
            var col = p.col; col.a = Mathf.Clamp01(Mathf.Min(u * 4f, (1f - u) * 2.5f)) * 0.9f; p.sr.color = col;
            float s = p.tam * (0.85f + 0.15f * Mathf.Sin(p.vida * 6f + p.fase));
            p.t.localScale = new Vector3(s, s, 1f);
        }
    }

    private void Soltar(Bounds b)
    {
        var p = libres.Count > 0 ? libres.Pop() : Nueva();
        p.pos = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y + b.size.y * 0.15f, b.max.y - b.size.y * 0.1f));
        p.vel = Random.Range(velocidadCaida.x, velocidadCaida.y);
        p.dur = Random.Range(vida.x, vida.y);
        p.vida = 0f;
        p.fase = Random.value * 10f;
        p.tam = Random.Range(tamano.x, tamano.y);
        p.col = Color.Lerp(rosa, violeta, Random.value);
        p.t.gameObject.SetActive(true);
        activas.Add(p);
    }

    private P Nueva()
    {
        var go = new GameObject("particula liana");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = particula;
        if (material != null) sr.sharedMaterial = material;
        sr.sortingOrder = ordenDeDibujo;
        return new P { t = go.transform, sr = sr };
    }
}
