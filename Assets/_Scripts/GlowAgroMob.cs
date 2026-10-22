using System.Collections.Generic;
using UnityEngine;

// Mientras el mob tiene agro (AtaqueComelibros o AtaquePajaro "Atacando"), le aparecen encima
// manchas de luz violeta y lila que parpadean, como el glow del tronco. Se prenden y apagan suave.
public class GlowAgroMob : MonoBehaviour
{
    [SerializeField] private Sprite glow;
    [SerializeField] private Material materialSinLuz;
    [SerializeField] private int cantidad = 6;
    [Tooltip("Tamano de cada mancha, en proporcion al alto del mob.")]
    [SerializeField] private Vector2 tamano = new Vector2(0.6f, 1.1f);
    [SerializeField] private Color lila = new Color(0.79f, 0.66f, 0.96f, 0.52f);
    [SerializeField] private Color violeta = new Color(0.31f, 0.16f, 0.48f, 0.63f);
    [SerializeField] private float parpadeosPorSegundo = 0.8f;
    [SerializeField] private float segundosAparecer = 0.3f;

    private class Mancha { public SpriteRenderer sr; public Vector2 base_; public float fase, velocidad; public Color color; }

    private readonly List<Mancha> manchas = new List<Mancha>();
    private AtaqueComelibros comelibros;
    private AtaquePajaro pajaro;
    private SpriteRenderer dibujo;
    private float alfa;

    private void Start()
    {
        comelibros = GetComponent<AtaqueComelibros>();
        pajaro = GetComponent<AtaquePajaro>();
        dibujo = GetComponent<SpriteRenderer>();
        if (glow == null) return;

        // las manchas se reparten sobre el cuerpo (el collider), en coordenadas locales
        var col = GetComponent<Collider2D>();
        Bounds b = col != null ? col.bounds : (dibujo != null ? dibujo.bounds : new Bounds(transform.position, Vector3.one));
        Vector2 centro = transform.InverseTransformPoint(b.center);
        Vector2 medio = new Vector2(b.extents.x / Mathf.Abs(transform.lossyScale.x), b.extents.y / Mathf.Abs(transform.lossyScale.y));
        float lado = glow.bounds.size.x;
        for (int i = 0; i < cantidad; i++)
        {
            var go = new GameObject("glow agro");
            go.transform.SetParent(transform, false);
            float t = Random.Range(tamano.x, tamano.y) * medio.y * 2f / lado;
            go.transform.localScale = new Vector3(t, t, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = glow;
            if (materialSinLuz != null) sr.sharedMaterial = materialSinLuz;
            if (dibujo != null) { sr.sortingLayerID = dibujo.sortingLayerID; sr.sortingOrder = dibujo.sortingOrder + 1; }
            sr.color = Color.clear;
            float u = (i + Random.Range(0.2f, 0.8f)) / cantidad;   // repartidas a lo ancho, a distintas alturas
            manchas.Add(new Mancha
            {
                sr = sr,
                base_ = centro + new Vector2(Mathf.Lerp(-medio.x, medio.x, u), Random.Range(-medio.y, medio.y) * 0.8f),
                fase = Random.value * 10f,
                velocidad = parpadeosPorSegundo * Random.Range(0.7f, 1.3f),
                color = i % 2 == 0 ? lila : violeta,
            });
        }
    }

    private void Update()
    {
        bool agro = (comelibros != null && comelibros.Atacando) || (pajaro != null && pajaro.Atacando);
        alfa = Mathf.MoveTowards(alfa, agro ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, segundosAparecer));
        bool espejado = dibujo != null && dibujo.flipX;
        foreach (var m in manchas)
        {
            if (m.sr == null) continue;
            m.sr.enabled = alfa > 0f;
            if (alfa <= 0f) continue;
            float parpadeo = 0.25f + 0.75f * (0.5f + 0.5f * Mathf.Sin((Time.time * m.velocidad + m.fase) * Mathf.PI * 2f));
            m.sr.color = new Color(m.color.r, m.color.g, m.color.b, m.color.a * alfa * parpadeo);
            float x = espejado ? -m.base_.x : m.base_.x;
            m.sr.transform.localPosition = new Vector3(x + Mathf.Sin(Time.time * 0.6f + m.fase) * 0.08f,
                                                       m.base_.y + Mathf.Sin(Time.time * 0.45f + m.fase * 1.3f) * 0.1f, -0.01f);
        }
    }
}
