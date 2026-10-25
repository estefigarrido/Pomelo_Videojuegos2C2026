using System.Collections.Generic;
using UnityEngine;

// Va en "Plantas y botellitas (elementos)/plantas". Cuando la chica camina por una plantita del piso
// (flores, dientes de leon, musgos y helechos parados), se levantan dientes de leon como polen:
// salen a destiempo, suben suave meciendose y se desvanecen. Cada uno lleva atras un destello del Figma
// que lo sigue con un poquito de retraso (como la estela del dash), y suben tambien destellos sueltos.
// La cantidad es proporcional al ancho de la plantita ('proporcion' = 60 %).
public class PolenPlantas : MonoBehaviour
{
    [SerializeField] private Sprite[] dientesDeLeon;
    [Tooltip("Los destellos amarillos desenfocados del Figma.")]
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Material materialSinLuz;

    [Header("Cantidad")]
    [Tooltip("Dientes de leon por cada unidad de ancho de la plantita, antes de aplicar la proporcion.")]
    [SerializeField] private float porUnidadDeAncho = 2f;
    [Tooltip("Proporcion de la plantita que se levanta como polen.")]
    [Range(0f, 1f)] [SerializeField] private float proporcion = 0.6f;
    [Tooltip("Destellos sueltos por cada diente de leon.")]
    [SerializeField] private float destellosSueltos = 0.5f;
    [Tooltip("La plantita suelta todo su polen de a poco: se termina cuando la chica camino adentro de ella esta cantidad de veces su ancho.")]
    [SerializeField] private float pasadasHastaVaciarse = 5f;
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

    [Header("Destellos")]
    [Tooltip("Tamano del destello respecto del alto del diente de leon.")]
    [SerializeField] private float tamanoDestello = 1.4f;
    [SerializeField] private float alfaDestello = 0.55f;
    [Tooltip("Cuanto tarda el destello en alcanzar al diente de leon (mas = estela mas larga).")]
    [SerializeField] private float retrasoDestello = 0.12f;
    [SerializeField] private int ordenDeDibujo = 102;

    private class Planta { public SpriteRenderer sr; public float recorrido; public int soltados; }
    private class Polen
    {
        public Transform t, td; public SpriteRenderer sr, srd;
        public Vector2 pos, vel, posDestello; public float vida, dur, espera, tam, tamD, fase, giro; public bool soloDestello;
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
            float avance = Mathf.Abs(rbChica.linearVelocity.x) * Time.deltaTime;
            foreach (var p in plantas)
            {
                if (p.sr == null) continue;
                Bounds b = p.sr.bounds;
                // la chica tiene que estar pasando por la parte de abajo de la plantita
                if (c.max.x < b.min.x || c.min.x > b.max.x || c.min.y > b.min.y + b.size.y * 0.6f || c.max.y < b.min.y) continue;
                // el polen total se reparte a lo largo del recorrido: se vacia despues de caminar 'pasadasHastaVaciarse' veces su ancho
                int total = Mathf.Max(1, Mathf.RoundToInt(b.size.x * porUnidadDeAncho * proporcion));
                if (p.soltados >= total) continue;
                p.recorrido += avance;
                int deberia = Mathf.Min(total, Mathf.FloorToInt(total * p.recorrido / Mathf.Max(0.01f, b.size.x * pasadasHastaVaciarse)) + 1);
                if (deberia > p.soltados)
                {
                    Levantar(b, c.center.x, Mathf.Sign(rbChica.linearVelocity.x), deberia - p.soltados);
                    p.soltados = deberia;
                }
            }
        }
        Mover();
    }

    // salen cerca de donde pasa la chica
    private void Levantar(Bounds b, float xChica, float sentido, int cantidad)
    {
        // de a pocos: la parte que no llega a uno entero sale con esa probabilidad
        float extra = cantidad * destellosSueltos;
        int sueltos = Mathf.FloorToInt(extra) + (Random.value < extra - Mathf.Floor(extra) ? 1 : 0);
        for (int i = 0; i < cantidad + sueltos && activos.Count < maximo; i++)
        {
            var p = libres.Count > 0 ? libres.Pop() : Nuevo();
            p.soloDestello = i >= cantidad;
            float x = Mathf.Clamp(xChica + Random.Range(-1.2f, 1.2f), b.min.x, b.max.x);
            p.pos = new Vector2(x, Random.Range(b.min.y + b.size.y * 0.3f, b.max.y));
            p.posDestello = p.pos;
            float lento = p.soloDestello ? 0.6f : 1f;   // los destellos sueltos suben mas despacio
            p.vel = new Vector2(sentido * empujeChica * Random.Range(0.3f, 1f) + Random.Range(-0.3f, 0.3f), Random.Range(subida.x, subida.y) * lento);
            p.vida = 0f;
            p.espera = Random.Range(0f, desfaseMaximo);
            p.dur = Random.Range(duracion.x, duracion.y);
            p.fase = Random.value * 10f;
            p.giro = Random.Range(-40f, 40f);
            float alto = Random.Range(altura.x, altura.y);
            p.sr.enabled = !p.soloDestello;
            p.sr.sprite = dientesDeLeon[Random.Range(0, dientesDeLeon.Length)];
            p.tam = alto / Mathf.Max(0.01f, p.sr.sprite.bounds.size.y);
            if (p.srd != null && destellos != null && destellos.Length > 0)
            {
                p.srd.sprite = destellos[Random.Range(0, destellos.Length)];
                p.tamD = alto * tamanoDestello * (p.soloDestello ? Random.Range(0.4f, 0.8f) : 1f) / Mathf.Max(0.01f, p.srd.sprite.bounds.size.y);
            }
            p.t.gameObject.SetActive(false);
            if (p.td != null) p.td.gameObject.SetActive(false);
            activos.Add(p);
        }
    }

    private void Mover()
    {
        float dt = Time.deltaTime;
        float z = transform.position.z - 0.05f;
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
            float u = p.vida / p.dur;
            if (u >= 1f)
            {
                p.t.gameObject.SetActive(false);
                if (p.td != null) p.td.gameObject.SetActive(false);
                activos.RemoveAt(i); libres.Push(p); continue;
            }

            // sube suave: frena de a poco y queda flotando, meciendose como polen
            p.vel *= Mathf.Exp(-1.6f * dt);
            p.vel.y = Mathf.Max(p.vel.y, 0.15f);
            p.pos += p.vel * dt + new Vector2(Mathf.Sin(p.vida * 2.2f + p.fase) * vaiven * dt, 0f);

            // aparece creciendo un poquito (no de golpe) y se desvanece al final
            float aparecer = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p.vida / 0.25f));
            float alfa = aparecer * (u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f);
            float s = Mathf.Lerp(0.6f, 1f, aparecer);

            p.t.position = new Vector3(p.pos.x, p.pos.y, z);
            p.t.localScale = Vector3.one * p.tam * s;
            p.t.rotation = Quaternion.Euler(0f, 0f, p.giro * u + Mathf.Sin(p.vida * 1.7f + p.fase) * 12f - p.vel.x * 10f);
            p.sr.color = new Color(1f, 1f, 1f, alfa);

            // el destello sigue al diente de leon con retraso: queda atras como una estela y titila
            if (p.td != null)
            {
                p.posDestello = p.soloDestello ? p.pos : Vector2.Lerp(p.posDestello, p.pos, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, retrasoDestello)));
                p.td.position = new Vector3(p.posDestello.x, p.posDestello.y, z + 0.01f);
                float titila = 0.8f + 0.2f * Mathf.Sin(p.vida * 5f + p.fase);
                p.td.localScale = Vector3.one * p.tamD * s * titila;
                p.srd.color = new Color(1f, 1f, 1f, alfaDestello * alfa * titila);
            }
        }
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
