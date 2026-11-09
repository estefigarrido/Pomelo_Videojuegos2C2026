using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Va en cada botellita. Con un solo golpe (latigo o bola de flores) se rompe con su animacion
// (_botellarota_01, 02 o 03) y suelta los dientes de leon que tenia adentro: suben meciendose, con su
// destello atras, y se desvanecen, igual que el polen de los arbustos (PolenPlantas).
// Las colgantes no se rompen en el aire: la soga se queda, el cuerpo cae entero y se rompe al tocar el piso.
// Los cuadros y las medidas de cada botellita los pone el menu Pomelo -> Configurar botellitas rotas.
public class BotellitaRompible : MonoBehaviour
{
    [Header("Colgantes")]
    [Tooltip("Solo las colgantes: el cuerpo de la botellita sin la soga (es lo que cae).")]
    [SerializeField] private Sprite cuerpo;
    [Tooltip("Solo las colgantes: la soga que queda colgando.")]
    [SerializeField] private Sprite soga;
    [SerializeField] private float gravedad = 22f;

    [Header("Rotura")]
    [Tooltip("Cuadros de la animacion de romperse; el primero es la botellita entera.")]
    [SerializeField] private Sprite[] cuadrosRota;
    [SerializeField] private float cuadrosPorSegundo = 12f;
    [Tooltip("Parte del dibujo de la botellita (en las colgantes, del cuerpo) que ocupa el vidrio, de 0 a 1. La mide el menu.")]
    [SerializeField] private Rect dibujoBotellita = new Rect(0f, 0f, 1f, 1f);
    [Tooltip("Parte del primer cuadro de la rotura que ocupa la botellita entera, de 0 a 1. La mide el menu.")]
    [SerializeField] private Rect dibujoRota = new Rect(0f, 0f, 1f, 1f);

    [Header("Dientes de leon (como el polen de los arbustos)")]
    [Tooltip("Velocidad con la que salen para arriba (unidades por segundo).")]
    [SerializeField] private Vector2 subida = new Vector2(0.9f, 1.8f);
    [Tooltip("Segundos que dura cada uno hasta desvanecerse.")]
    [SerializeField] private Vector2 duracion = new Vector2(1.4f, 2.4f);
    [Tooltip("Hasta cuanto tarda en salir cada uno: asi no salen todos juntos.")]
    [SerializeField] private float desfaseMaximo = 0.2f;
    [SerializeField] private float vaiven = 0.35f;
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Material materialSinLuz;
    [Tooltip("Tamano del destello respecto del alto del diente de leon.")]
    [SerializeField] private float tamanoDestello = 1.4f;
    [SerializeField] private float alfaDestello = 0.55f;
    [Tooltip("Cuanto tarda el destello en alcanzar al diente de leon (mas = estela mas larga).")]
    [SerializeField] private float retrasoDestello = 0.12f;

    private bool rota;
    private SpriteRenderer dibujo;

    private class Polen
    {
        public Transform t, td; public SpriteRenderer sr, srd;
        public Vector2 pos, vel, posDestello; public float vida, dur, espera, escala, escalaD, fase, giro;
    }

    private void Awake() => dibujo = GetComponent<SpriteRenderer>();

    public void Romper()
    {
        if (rota) return;
        rota = true;
        var col = GetComponent<Collider2D>(); if (col != null) col.enabled = false;
        // suelta de golpe el polen que le quedaba (carga la barra del templo)
        if (PolenPlantas.Instancia != null) PolenPlantas.Instancia.SoltarTodo(this);
        // los glows del Figma (hijos "glow N") se apagan con la botellita
        foreach (Transform h in transform) if (h.name.StartsWith("glow")) h.gameObject.SetActive(false);
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        // 1) las colgantes: la soga se queda y el cuerpo cae entero, con los dientes de leon adentro
        SpriteRenderer seRompe = dibujo;
        var sogaFisica = GetComponent<SogaBotellita>();
        if (soga != null && cuerpo != null)
        {
            // con soga de verdad (SogaBotellita) la soga queda suelta y el dibujo se apaga; si no, queda la soga dibujada
            if (sogaFisica != null) { sogaFisica.Soltar(); dibujo.enabled = false; }
            else dibujo.sprite = soga;
            seRompe = Suelto("botellita cayendo", cuerpo);
            var grupo = transform.Find("dientes de leon");
            if (grupo != null) grupo.SetParent(seRompe.transform, true);
            yield return Caer(seRompe);
        }

        // 2) se rompe donde esta: la animacion reemplaza al dibujo y los dientes de leon salen
        Rect vidrio = VidrioEnMundo(seRompe);
        var polen = SoltarDientes(seRompe.transform);
        SpriteRenderer rotura = CrearRotura(seRompe, vidrio);
        if (seRompe == dibujo) dibujo.enabled = false;
        else Destroy(seRompe.gameObject);

        // 3) corre la animacion (queda en el ultimo cuadro, los pedazos en el piso) mientras sube el polen
        float inicio = Time.time;
        bool quedaPolen = true;
        while (true)
        {
            bool animando = false;
            if (rotura != null)
            {
                int cuadro = Mathf.FloorToInt((Time.time - inicio) * cuadrosPorSegundo);
                animando = cuadro < cuadrosRota.Length - 1;
                rotura.sprite = cuadrosRota[Mathf.Min(cuadro, cuadrosRota.Length - 1)];
            }
            if (quedaPolen) quedaPolen = MoverPolen(polen);
            if (!animando && !quedaPolen) yield break;
            yield return null;
        }
    }

    // cae derecha (sin girar, asi la animacion arranca parada) hasta apoyar el vidrio en el piso
    private IEnumerator Caer(SpriteRenderer sr)
    {
        Rect vidrio = VidrioEnMundo(sr);
        float piso = Piso(vidrio.center.x, vidrio.yMin);
        float vel = 0f;
        while (true)
        {
            vel -= gravedad * Time.deltaTime;
            sr.transform.position += Vector3.up * vel * Time.deltaTime;
            float abajo = VidrioEnMundo(sr).yMin;
            if (abajo <= piso)
            {
                sr.transform.position += Vector3.up * (piso - abajo);
                yield break;
            }
            yield return null;
        }
    }

    // el rectangulo que ocupa el vidrio de la botellita en el mundo (sin el lienzo vacio del PNG)
    private Rect VidrioEnMundo(SpriteRenderer sr)
    {
        Bounds b = sr.bounds;
        return new Rect(b.min.x + b.size.x * dibujoBotellita.x, b.min.y + b.size.y * dibujoBotellita.y,
                        b.size.x * dibujoBotellita.width, b.size.y * dibujoBotellita.height);
    }

    // la animacion de romperse, del mismo alto que la botellita y apoyada en el mismo lugar
    private SpriteRenderer CrearRotura(SpriteRenderer original, Rect vidrio)
    {
        if (cuadrosRota == null || cuadrosRota.Length == 0 || cuadrosRota[0] == null) return null;
        Sprite primero = cuadrosRota[0];

        var go = new GameObject("botellita rota");
        go.transform.SetParent(transform.parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = primero;
        sr.sharedMaterial = original.sharedMaterial;
        sr.sortingLayerID = original.sortingLayerID;
        sr.sortingOrder = original.sortingOrder;

        float escala = vidrio.height / Mathf.Max(0.001f, dibujoRota.height * primero.bounds.size.y);
        float lado = Mathf.Sign(original.transform.lossyScale.x) * (original.flipX ? -1f : 1f);
        go.transform.localScale = new Vector3(escala * lado, escala, 1f);

        // el pie de la botellita del primer cuadro va donde estaba el pie de la botellita
        Bounds c = primero.bounds;
        Vector2 pieEnCuadro = new Vector2(c.min.x + c.size.x * dibujoRota.center.x, c.min.y + c.size.y * dibujoRota.yMin);
        Vector3 pie = new Vector3(vidrio.center.x, vidrio.yMin, original.transform.position.z);
        go.transform.position = pie - new Vector3(pieEnCuadro.x * escala * lado, pieEnCuadro.y * escala, 0f);
        return sr;
    }

    // los dientes de leon de adentro quedan sueltos y empiezan a subir
    private List<Polen> SoltarDientes(Transform desde)
    {
        var lista = new List<Polen>();
        foreach (var f in desde.GetComponentsInChildren<SpriteRenderer>())
        {
            if (f.gameObject.name != "diente de leon") continue;
            var t = f.transform;
            Vector3 escalaMundo = t.lossyScale;
            t.SetParent(transform.parent, true);
            t.localScale = escalaMundo;
            f.maskInteraction = SpriteMaskInteraction.None;
            f.enabled = true;   // en las botellitas que ya traen el polen dibujado estan ocultos hasta romperse
            f.sortingOrder = 102;

            var p = new Polen
            {
                t = t, sr = f, pos = t.position, fase = Random.value * 10f, escala = Mathf.Abs(escalaMundo.x),
                vel = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(subida.x, subida.y)),
                dur = Random.Range(duracion.x, duracion.y), espera = Random.Range(0f, desfaseMaximo),
                giro = Random.Range(-40f, 40f)
            };
            p.posDestello = p.pos;
            if (destellos != null && destellos.Length > 0)
            {
                var gd = new GameObject("destello polen");
                gd.transform.SetParent(transform.parent, false);
                gd.transform.position = t.position;
                var srd = gd.AddComponent<SpriteRenderer>();
                srd.sprite = destellos[Random.Range(0, destellos.Length)];
                if (materialSinLuz != null) srd.sharedMaterial = materialSinLuz;
                srd.sortingOrder = 101;
                srd.color = Color.clear;
                p.td = gd.transform; p.srd = srd;
                p.escalaD = f.bounds.size.y * tamanoDestello / Mathf.Max(0.01f, srd.sprite.bounds.size.y);
            }
            lista.Add(p);
        }
        return lista;
    }

    // como en PolenPlantas: sube suave, frena de a poco meciendose y se desvanece al final
    private bool MoverPolen(List<Polen> polen)
    {
        float dt = Time.deltaTime;
        bool queda = false;
        foreach (var p in polen)
        {
            if (p.t == null) continue;
            queda = true;
            if (p.espera > 0f) { p.espera -= dt; continue; }

            p.vida += dt;
            float u = p.vida / p.dur;
            if (u >= 1f)
            {
                if (p.td != null) Destroy(p.td.gameObject);
                Destroy(p.t.gameObject);
                p.t = null;
                continue;
            }

            p.vel *= Mathf.Exp(-1.6f * dt);
            p.vel.y = Mathf.Max(p.vel.y, 0.15f);
            p.pos += p.vel * dt + new Vector2(Mathf.Sin(p.vida * 2.2f + p.fase) * vaiven * dt, 0f);
            float alfa = u < 0.6f ? 1f : 1f - (u - 0.6f) / 0.4f;

            p.t.position = new Vector3(p.pos.x, p.pos.y, p.t.position.z);
            p.t.rotation = Quaternion.Euler(0f, 0f, p.giro * u + Mathf.Sin(p.vida * 1.7f + p.fase) * 12f - p.vel.x * 10f);
            p.sr.color = new Color(p.sr.color.r, p.sr.color.g, p.sr.color.b, alfa);

            // el destello sigue al diente de leon con retraso: queda atras como una estela y titila
            if (p.td != null)
            {
                p.posDestello = Vector2.Lerp(p.posDestello, p.pos, 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, retrasoDestello)));
                p.td.position = new Vector3(p.posDestello.x, p.posDestello.y, p.t.position.z + 0.01f);
                float titila = 0.8f + 0.2f * Mathf.Sin(p.vida * 5f + p.fase);
                float aparecer = Mathf.Clamp01(p.vida / 0.25f);
                p.td.localScale = Vector3.one * p.escalaD * titila;
                p.srd.color = new Color(1f, 1f, 1f, alfaDestello * alfa * aparecer * titila);
            }
        }
        return queda;
    }

    // el cuerpo se suelta de la botellita (que se queda con la soga) para poder caer
    private SpriteRenderer Suelto(string nombre, Sprite sp)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform.parent, false);
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);
        go.transform.localScale = transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sp; sr.sortingOrder = dibujo.sortingOrder;
        sr.sortingLayerID = dibujo.sortingLayerID; sr.sharedMaterial = dibujo.sharedMaterial;
        sr.flipX = dibujo.flipX; sr.flipY = dibujo.flipY;
        return sr;
    }

    // el piso debajo (ignora triggers, la chica y los mobs); si no hay, cae un poco y queda
    private float Piso(float x, float desde)
    {
        foreach (var h in Physics2D.RaycastAll(new Vector2(x, desde + 0.05f), Vector2.down, 20f))
        {
            if (h.collider.isTrigger || h.collider.GetComponentInParent<SaludPersonaje>() != null || h.collider.GetComponentInParent<VidaEnemigo>() != null) continue;
            return h.point.y;
        }
        return desde - 3f;
    }
}
