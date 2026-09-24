using System.Collections.Generic;
using UnityEngine;

// Bandada de mariposas que guia a la chica hasta el portal.
// Cuando la chica agarra la gema del Umbrae, las mariposas entran volando por el costado izquierdo
// de la camara y siguen un recorrido (la linea guia no se dibuja en el juego, solo se ve en la escena
// con el objeto seleccionado). Van siempre un poco adelante de la chica: si ella se queda atras la
// esperan revoloteando, y si se queda muy atras la vuelven a buscar. Al llegar al portal dan vueltas
// alrededor, y cuando la gema queda encajada en el engarce se van volando.
public class MariposasGuia : MonoBehaviour
{
    [Header("Animacion (carpeta mariposa)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 24f;
    [SerializeField] private int ordenDibujo = 10;

    [Header("Bandada")]
    [SerializeField] private int cantidad = 7;
    [Tooltip("Ancho de las alas abiertas en unidades (minimo y maximo, cada mariposa sale al azar).")]
    [SerializeField] private Vector2 tamano = new Vector2(0.22f, 0.36f);
    [Tooltip("Cuanto van adelante de la chica, medido sobre el recorrido.")]
    [SerializeField] private float adelanteDeLaChica = 3.5f;
    [Tooltip("Si la chica queda mas atras que esto, la bandada vuelve a buscarla.")]
    [SerializeField] private float volverSiQuedaAtras = 10f;
    [SerializeField] private float velocidadMaxima = 9f;
    [Tooltip("Largo de la bandada a lo largo del recorrido.")]
    [SerializeField] private float largoBandada = 2.2f;
    [Tooltip("Ancho de la bandada hacia los costados del recorrido.")]
    [SerializeField] private float anchoBandada = 0.8f;
    [Tooltip("Cuanto se desvia cada una del lugar que le toca (vuelo semi-aleatorio).")]
    [SerializeField] private float desvio = 0.45f;

    [Header("Recorrido (de la arena del Umbrae al portal)")]
    [SerializeField] private Vector2[] recorrido;
    [Tooltip("Alrededor de que punto dan vueltas cuando llegan al portal.")]
    [SerializeField] private Vector2 centroPortal = new Vector2(-86.4f, 40.3f);
    [SerializeField] private Vector2 radioPortal = new Vector2(1.3f, 1.1f);

    [Header("Al encajar la gema se van")]
    [Tooltip("Segundos desde que empieza la cinematica del portal (el doble salto tarda ~1 s).")]
    [SerializeField] private float esperaAntesDeIrse = 2.2f;
    [SerializeField] private float duracionIda = 3f;

    private class Mariposa
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 pos, vel;
        public float escala, demora, frame, ritmo, suavidad;
        public float enRecorrido, costado;       // lugar en la bandada
        public float semilla, giro, velocidadGiro, radioGiro;
        public Vector2 huida;
    }

    private readonly List<Mariposa> mariposas = new List<Mariposa>();
    private float[] largos;                      // largo acumulado del recorrido en cada punto
    private float total;
    private Transform chica;
    private bool activas;
    private float tiempo;
    private float sGuia, sObjetivo, sChica, velocidadChica;
    private FinalNivel final;
    private float seVanDesde = -1f;

    private void OnEnable() => GemaUmbrae.Recolectada += Aparecer;
    private void OnDisable() => GemaUmbrae.Recolectada -= Aparecer;

    private void Awake()
    {
        if (recorrido == null || recorrido.Length < 2) return;
        largos = new float[recorrido.Length];
        for (int i = 1; i < recorrido.Length; i++) largos[i] = largos[i - 1] + Vector2.Distance(recorrido[i - 1], recorrido[i]);
        total = largos[largos.Length - 1];
    }

    // ---------------- aparicion ----------------

    private void Aparecer()
    {
        if (activas || largos == null || frames == null || frames.Length == 0) return;
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud == null) return;
        chica = salud.transform;
        final = FindFirstObjectByType<FinalNivel>();
        var dibujoChica = salud.GetComponent<SpriteRenderer>();
        activas = true;
        tiempo = 0f;

        sChica = Proyectar(chica.position, 0f, total);
        sGuia = sObjetivo = Mathf.Min(total, sChica + adelanteDeLaChica);

        // entran por el costado izquierdo de la camara (fuera de cuadro), a la altura de la bandada
        var seguidora = FindFirstObjectByType<CamaraSeguidora>();
        var cam = seguidora != null ? seguidora.GetComponent<Camera>() : Camera.main;
        float bordeIzq = cam != null ? cam.transform.position.x - cam.orthographicSize * cam.aspect : chica.position.x - 10f;
        float alturaBandada = Punto(sGuia).y;

        for (int i = 0; i < cantidad; i++)
        {
            var m = new Mariposa();
            var go = new GameObject("Mariposa " + (i + 1));
            go.transform.SetParent(transform, false);
            m.t = go.transform;
            m.sr = go.AddComponent<SpriteRenderer>();
            if (dibujoChica != null)
            {
                m.sr.sortingLayerID = dibujoChica.sortingLayerID;
                m.sr.sharedMaterial = dibujoChica.sharedMaterial;
            }
            m.sr.sortingOrder = ordenDibujo + i;
            m.escala = Random.Range(tamano.x, tamano.y);
            m.t.localScale = Vector3.one * m.escala;
            m.frame = Random.Range(0f, frames.Length);
            m.ritmo = Random.Range(0.85f, 1.2f);
            m.suavidad = Random.Range(0.22f, 0.4f);
            m.enRecorrido = Random.Range(-largoBandada, largoBandada * 0.3f);
            m.costado = Random.Range(-1f, 1f);
            m.semilla = Random.Range(0f, 100f);
            m.giro = Random.Range(0f, Mathf.PI * 2f);
            m.velocidadGiro = Random.Range(1.4f, 2.6f) * (Random.value < 0.5f ? -1f : 1f);
            m.radioGiro = Random.Range(0.15f, 0.35f);
            m.demora = i * 0.18f + Random.Range(0f, 0.25f);
            m.pos = new Vector2(bordeIzq - Random.Range(0.4f, 1.8f), alturaBandada + Random.Range(-1.2f, 1.6f));
            m.vel = new Vector2(velocidadMaxima * 0.6f, 0f);
            m.t.position = new Vector3(m.pos.x, m.pos.y, transform.position.z);
            m.sr.sprite = frames[(int)m.frame % frames.Length];
            m.sr.enabled = false;
            mariposas.Add(m);
        }
    }

    // ---------------- vuelo ----------------

    private void Update()
    {
        if (!activas) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        tiempo += dt;

        ActualizarGuia(dt);

        // cuando empieza la cinematica del portal (la chica salta a encajar la gema), se van
        if (seVanDesde < 0f && final != null && final.Empezo) seVanDesde = tiempo;
        bool seVan = seVanDesde >= 0f && tiempo - seVanDesde > esperaAntesDeIrse;
        bool enPortal = sGuia >= total - 0.05f;

        bool quedaAlguna = false;
        foreach (var m in mariposas)
        {
            if (tiempo < m.demora) { quedaAlguna = true; continue; }
            if (!m.sr.enabled) m.sr.enabled = true;

            // lugar que le toca: su puesto en la bandada + un revoloteo propio (vueltitas + ruido)
            m.giro += m.velocidadGiro * dt;
            float tt = tiempo * 0.55f + m.semilla;
            Vector2 ruido = new Vector2(Mathf.PerlinNoise(tt, m.semilla) - 0.5f, Mathf.PerlinNoise(m.semilla, tt) - 0.5f) * (2f * desvio);
            Vector2 vueltita = new Vector2(Mathf.Cos(m.giro), Mathf.Sin(m.giro) * 0.7f) * m.radioGiro;
            Vector2 objetivo;
            if (enPortal)
            {
                // dan vueltas alrededor del portal, cada una en su orbita
                float a = m.giro * 0.6f + m.semilla;
                float r = 0.75f + 0.25f * Mathf.Sin(m.semilla * 3f);
                objetivo = centroPortal + new Vector2(Mathf.Cos(a) * radioPortal.x, Mathf.Sin(a) * radioPortal.y) * r + ruido * 0.6f;
            }
            else
            {
                float s = Mathf.Clamp(sGuia + m.enRecorrido, 0f, total);
                Vector2 p = Punto(s), dir = Direccion(s);
                Vector2 normal = new Vector2(-dir.y, dir.x);
                objetivo = p + normal * (m.costado * anchoBandada) + ruido + vueltita;
            }

            if (seVan)
            {
                // se alejan volando hacia arriba y se desvanecen
                if (m.huida == Vector2.zero) m.huida = new Vector2(Random.Range(-1f, 1f), Random.Range(0.6f, 1.2f)).normalized;
                float k = (tiempo - seVanDesde - esperaAntesDeIrse) / duracionIda;
                objetivo = m.pos + m.huida * 0.5f + vueltita;
                SetAlfa(m.sr, 1f - Mathf.Clamp01(k));
                if (k < 1f) quedaAlguna = true;
            }
            else quedaAlguna = true;

            // aleteo: un saltito vertical rapido, como vuelan las mariposas
            objetivo.y += Mathf.Sin(tiempo * 9f * m.ritmo + m.semilla) * 0.05f;

            m.pos = Vector2.SmoothDamp(m.pos, objetivo, ref m.vel, m.suavidad, velocidadMaxima * 1.3f, dt);
            m.t.position = new Vector3(m.pos.x, m.pos.y, transform.position.z);

            // el dibujo mira a la izquierda: se da vuelta cuando vuela para la derecha
            if (Mathf.Abs(m.vel.x) > 0.15f) m.sr.flipX = m.vel.x > 0f;
            float inclinacion = Mathf.Clamp(m.vel.y * 6f, -18f, 18f) * (m.sr.flipX ? 1f : -1f);
            m.t.rotation = Quaternion.Slerp(m.t.rotation, Quaternion.Euler(0f, 0f, inclinacion), 1f - Mathf.Exp(-6f * dt));

            m.frame = (m.frame + fps * m.ritmo * dt) % frames.Length;
            m.sr.sprite = frames[(int)m.frame];
        }

        if (!quedaAlguna)
        {
            foreach (var m in mariposas) if (m.t != null) Destroy(m.t.gameObject);
            mariposas.Clear();
            activas = false;
        }
    }

    // avanza el lugar de la bandada sobre el recorrido segun por donde va la chica
    private void ActualizarGuia(float dt)
    {
        if (chica == null) return;
        float s = Proyectar(chica.position, sChica - 5f, sChica + 12f);
        if (Vector2.Distance(Punto(s), chica.position) > 5f) s = Proyectar(chica.position, 0f, total); // reaparecio en otro lado

        // que tan rapido avanza la chica sobre el recorrido (si corre, se adelantan mas)
        float avance = Mathf.Clamp((s - sChica) / dt, 0f, 10f);
        velocidadChica = Mathf.Lerp(velocidadChica, avance, 1f - Mathf.Exp(-4f * dt));
        sChica = s;

        float deseado = Mathf.Min(total, sChica + adelanteDeLaChica + velocidadChica * 0.6f);
        if (deseado > sObjetivo) sObjetivo = deseado;                                   // avanza con ella
        else if (deseado < sObjetivo - volverSiQuedaAtras) sObjetivo = deseado;         // quedo muy atras: la van a buscar

        float falta = Mathf.Abs(sObjetivo - sGuia);
        float velocidad = Mathf.Min(velocidadMaxima, 1f + falta * 5f);
        sGuia = Mathf.MoveTowards(sGuia, sObjetivo, velocidad * dt);
    }

    // ---------------- recorrido ----------------

    private Vector2 Punto(float s)
    {
        int i = Indice(s);
        float tramo = largos[i + 1] - largos[i];
        float t = tramo > 0f ? (s - largos[i]) / tramo : 0f;
        return Vector2.Lerp(recorrido[i], recorrido[i + 1], t);
    }

    private Vector2 Direccion(float s)
    {
        // promediada en un tramito, para que no gire de golpe en cada punto
        Vector2 d = Punto(Mathf.Min(total, s + 0.4f)) - Punto(Mathf.Max(0f, s - 0.4f));
        return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.left;
    }

    private int Indice(float s)
    {
        if (s <= 0f) return 0;
        if (s >= total) return largos.Length - 2;
        int lo = 0, hi = largos.Length - 1;
        while (hi - lo > 1)
        {
            int mid = (lo + hi) / 2;
            if (largos[mid] <= s) lo = mid; else hi = mid;
        }
        return lo;
    }

    // lugar del recorrido (entre desde y hasta) mas cercano a una posicion
    private float Proyectar(Vector2 pos, float desde, float hasta)
    {
        desde = Mathf.Max(0f, desde); hasta = Mathf.Min(total, hasta);
        float mejorS = desde, mejorD = float.MaxValue;
        for (int i = Indice(desde); i < recorrido.Length - 1 && largos[i] <= hasta; i++)
        {
            Vector2 a = recorrido[i], b = recorrido[i + 1], ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(pos - a, ab) / ab.sqrMagnitude) : 0f;
            float d = (a + ab * t - pos).sqrMagnitude;
            if (d < mejorD) { mejorD = d; mejorS = largos[i] + ab.magnitude * t; }
        }
        return Mathf.Clamp(mejorS, desde, hasta);
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        var c = sr.color; c.a = a; sr.color = c;
    }

    // la linea guia solo se ve en la escena, con el objeto seleccionado
    private void OnDrawGizmosSelected()
    {
        if (recorrido == null || recorrido.Length < 2) return;
        Gizmos.color = new Color(1f, 0f, 0.8f, 0.9f);
        for (int i = 1; i < recorrido.Length; i++) Gizmos.DrawLine(recorrido[i - 1], recorrido[i]);
        Gizmos.DrawWireSphere(centroPortal, 0.3f);
    }
}
