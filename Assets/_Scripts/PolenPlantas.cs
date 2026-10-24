using System.Collections.Generic;
using UnityEngine;

// Va en "Plantas y botellitas (elementos)/plantas". Cuando la chica camina por una plantita del piso
// (flores, dientes de leon, musgos y helechos parados), se levantan dientes de leon como polen:
// salen a destiempo, suben suave meciendose, con una estela como la del dash, y se desvanecen.
// La cantidad es proporcional al ancho de la plantita ('proporcion' = 60 %).
public class PolenPlantas : MonoBehaviour
{
    [SerializeField] private Sprite[] dientesDeLeon;
    [SerializeField] private Sprite estela;
    [SerializeField] private Material materialSinLuz;

    [Header("Cantidad")]
    [Tooltip("Dientes de leon por cada unidad de ancho de la plantita, antes de aplicar la proporcion.")]
    [SerializeField] private float porUnidadDeAncho = 2f;
    [Tooltip("Proporcion de la plantita que se levanta como polen.")]
    [Range(0f, 1f)] [SerializeField] private float proporcion = 0.6f;
    [Tooltip("Segundos hasta que la misma plantita vuelve a soltar polen.")]
    [SerializeField] private float esperaPorPlanta = 1.2f;
    [Tooltip("La chica tiene que ir al menos a esta velocidad (caminando) para levantar polen.")]
    [SerializeField] private float velocidadMinima = 0.6f;
    [SerializeField] private int maximo = 150;

    [Header("Movimiento")]
    [Tooltip("Alto de cada diente de leon (unidades).")]
    [SerializeField] private Vector2 altura = new Vector2(0.45f, 0.75f);
    [SerializeField] private Vector2 duracion = new Vector2(1.4f, 2.4f);
    [Tooltip("Hasta cuanto tarda en salir cada uno: asi no salen todos juntos.")]
    [SerializeField] private float desfaseMaximo = 0.45f;
    [Tooltip("Velocidad con la que sale para arriba (unidades por segundo).")]
    [SerializeField] private Vector2 subida = new Vector2(0.9f, 1.8f);
    [Tooltip("Cuanto lo empuja la chica hacia donde camina.")]
    [SerializeField] private float empujeChica = 0.6f;
    [SerializeField] private float vaiven = 0.35f;

    [Header("Estela")]
    [SerializeField] private Color colorEstela = new Color(1f, 0.92f, 0.45f, 0.45f);
    [SerializeField] private float largoEstela = 0.35f;
    [SerializeField] private int ordenDeDibujo = 102;

    private class Planta { public SpriteRenderer sr; public float listaDesde; }
    private class Polen
    {
        public Transform t; public SpriteRenderer sr, srEstela;
        public Vector2 pos, vel; public float vida, dur, espera, tam, fase, giro;
    }

    private readonly List<Planta> plantas = new List<Planta>();
    private readonly List<Polen> activos = new List<Polen>();
    private readonly Stack<Polen> libres = new Stack<Polen>();
    private Collider2D cuerpoChica;
    private Rigidbody2D rbChica;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) { cuerpoChica = salud.GetComponent<Collider2D>(); rbChica = salud.GetComponent<Rigidbody2D>(); }

        // solo las plantitas paradas en el piso (no colgantes, ramas ni las de techo o pared)
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            string n = sr.gameObject.name;
            bool tipo = n.StartsWith("Flores_") || n.StartsWith("Dientedeleon") || n.StartsWith("Helecho_")
                        || (n.StartsWith("Musgo_") && !n.StartsWith("Musgo_colgante"));
            if (!tipo) continue;
            float signo = Mathf.Sign(sr.transform.lossyScale.y);
            if ((sr.transform.up * signo).y < 0.7f) continue;
            plantas.Add(new Planta { sr = sr });
        }
    }

    private void Update()
    {
        if (cuerpoChica != null && rbChica != null && dientesDeLeon != null && dientesDeLeon.Length > 0
            && Mathf.Abs(rbChica.linearVelocity.x) >= velocidadMinima)
        {
            Bounds c = cuerpoChica.bounds;
            foreach (var p in plantas)
            {
                if (p.sr == null || Time.time < p.listaDesde) continue;
                Bounds b = p.sr.bounds;
                // la chica tiene que estar pasando por la parte de abajo de la plantita
                if (c.max.x < b.min.x || c.min.x > b.max.x || c.min.y > b.min.y + b.size.y * 0.6f || c.max.y < b.min.y) continue;
                p.listaDesde = Time.time + esperaPorPlanta;
                Levantar(b, c.center.x, Mathf.Sign(rbChica.linearVelocity.x));
            }
        }
        Mover();
    }

    // cantidad proporcional al ancho de la plantita; salen cerca de donde pasa la chica
    private void Levantar(Bounds b, float xChica, float sentido)
    {
        int cantidad = Mathf.Max(1, Mathf.RoundToInt(b.size.x * porUnidadDeAncho * proporcion));
        for (int i = 0; i < cantidad && activos.Count < maximo; i++)
        {
            var p = libres.Count > 0 ? libres.Pop() : Nuevo();
            float x = Mathf.Clamp(xChica + Random.Range(-1.2f, 1.2f), b.min.x, b.max.x);
            p.pos = new Vector2(x, Random.Range(b.min.y + b.size.y * 0.3f, b.max.y));
            p.vel = new Vector2(sentido * empujeChica * Random.Range(0.3f, 1f) + Random.Range(-0.3f, 0.3f), Random.Range(subida.x, subida.y));
            p.vida = 0f;
            p.espera = Random.Range(0f, desfaseMaximo);
            p.dur = Random.Range(duracion.x, duracion.y);
            p.fase = Random.value * 10f;
            p.giro = Random.Range(-40f, 40f);
            p.sr.sprite = dientesDeLeon[Random.Range(0, dientesDeLeon.Length)];
            p.tam = Random.Range(altura.x, altura.y) / Mathf.Max(0.01f, p.sr.sprite.bounds.size.y);
            p.t.position = new Vector3(p.pos.x, p.pos.y, transform.position.z - 0.05f);
            p.t.gameObject.SetActive(false);
            activos.Add(p);
        }
    }

    private void Mover()
    {
        float dt = Time.deltaTime;
        for (int i = activos.Count - 1; i >= 0; i--)
        {
            var p = activos[i];
            if (p.espera > 0f) { p.espera -= dt; if (p.espera > 0f) continue; p.t.gameObject.SetActive(true); }
            p.vida += dt;
            float u = p.vida / p.dur;
            if (u >= 1f) { p.t.gameObject.SetActive(false); activos.RemoveAt(i); libres.Push(p); continue; }

            // sube suave: frena de a poco y queda flotando, meciendose como polen
            p.vel *= Mathf.Exp(-1.6f * dt);
            p.vel.y = Mathf.Max(p.vel.y, 0.15f);
            p.pos += p.vel * dt + new Vector2(Mathf.Sin(p.vida * 2.2f + p.fase) * vaiven * dt, 0f);
            p.t.position = new Vector3(p.pos.x, p.pos.y, transform.position.z - 0.05f);

            // aparece creciendo un poquito (no de golpe) y se desvanece al final
            float aparecer = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p.vida / 0.25f));
            float alfa = aparecer * (u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f);
            float s = p.tam * Mathf.Lerp(0.6f, 1f, aparecer);
            p.t.localScale = new Vector3(s, s, 1f);
            p.t.rotation = Quaternion.Euler(0f, 0f, p.giro * u + Mathf.Sin(p.vida * 1.7f + p.fase) * 12f - p.vel.x * 10f);
            p.sr.color = new Color(1f, 1f, 1f, alfa);

            // estela: apunta para atras del movimiento y se alarga con la velocidad
            if (p.srEstela != null)
            {
                float rapidez = p.vel.magnitude;
                var et = p.srEstela.transform;
                et.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg);
                float largo = Mathf.Clamp(rapidez * largoEstela, 0.05f, 0.8f) / Mathf.Max(0.01f, estela.bounds.size.x);
                et.localScale = new Vector3(largo / s, 0.12f / Mathf.Max(0.01f, estela.bounds.size.y) / s, 1f);
                p.srEstela.color = new Color(colorEstela.r, colorEstela.g, colorEstela.b, colorEstela.a * alfa * Mathf.Clamp01(rapidez / 1.2f));
            }
        }
    }

    private Polen Nuevo()
    {
        var go = new GameObject("polen");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = ordenDeDibujo;
        SpriteRenderer srE = null;
        if (estela != null)
        {
            var e = new GameObject("estela");
            e.transform.SetParent(go.transform, false);
            srE = e.AddComponent<SpriteRenderer>();
            srE.sprite = estela;
            if (materialSinLuz != null) srE.sharedMaterial = materialSinLuz;
            srE.sortingOrder = ordenDeDibujo - 1;
        }
        return new Polen { t = go.transform, sr = sr, srEstela = srE };
    }
}
