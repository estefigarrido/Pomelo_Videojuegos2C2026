using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Va en cada botellita. Con un solo golpe (latigo o bola de flores) se agrieta (cambia al dibujo agrietado),
// cae al piso y rebota. Los dientes de leon que tenia adentro quedan flotando como polen (repartidos en
// 'radioPolen'), y despues de 1 s la chica los absorbe al pasar cerca. En las colgantes la soga se queda.
public class BotellitaRompible : MonoBehaviour
{
    [Header("Dibujos (mismo lienzo que la botellita)")]
    [Tooltip("La botellita agrietada. Si esta vacio, cae con el dibujo normal.")]
    [SerializeField] private Sprite agrietada;
    [Tooltip("Solo las colgantes: el cuerpo de la botellita sin la soga (es lo que cae).")]
    [SerializeField] private Sprite cuerpo;
    [Tooltip("Solo las colgantes: la soga que queda colgando.")]
    [SerializeField] private Sprite soga;

    [Header("Caida")]
    [Tooltip("Segundos que se ve agrietada antes de caer.")]
    [SerializeField] private float segundosAgrietada = 0.15f;
    [SerializeField] private float gravedad = 22f;

    [Header("Polen")]
    [Tooltip("Radio (en px, 100 px = 1 unidad) en el que se reparten los dientes de leon al quedar flotando.")]
    [SerializeField] private float radioPolen = 20f;
    [Tooltip("Segundos hasta que la chica puede absorber el polen.")]
    [SerializeField] private float esperaAbsorcion = 1f;
    [Tooltip("Distancia a la chica para que lo absorba (unidades).")]
    [SerializeField] private float distanciaAbsorcion = 1.4f;
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private Material materialSinLuz;

    private bool rota;
    private SpriteRenderer dibujo;
    private Transform chica;

    private class Polen { public Transform t, td; public SpriteRenderer sr, srd; public Vector2 centro, objetivo; public float fase, escala, escalaD; public bool absorbiendo; public float inicioAbs; public Vector3 desdeAbs; }

    private void Awake() => dibujo = GetComponent<SpriteRenderer>();

    public void Romper()
    {
        if (rota) return;
        rota = true;
        var col = GetComponent<Collider2D>(); if (col != null) col.enabled = false;
        var salud = FindFirstObjectByType<SaludPersonaje>(); if (salud != null) chica = salud.transform;
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        // 1) se agrieta y suelta el polen de adentro
        if (agrietada != null) dibujo.sprite = agrietada;
        var polen = SoltarPolen();
        float inicioPolen = Time.time;
        float hasta = Time.time + segundosAgrietada;
        while (Time.time < hasta) { ActualizarPolen(polen, Time.time - inicioPolen); yield return null; }

        // 2) cae: en las colgantes la soga se queda y cae solo el cuerpo
        Transform cae = transform;
        SpriteRenderer srCae = dibujo;
        if (soga != null && cuerpo != null)
        {
            dibujo.sprite = soga;
            // el dibujo agrietado de las colgantes tiene que venir sin la soga
            srCae = Suelto("botellita cayendo", agrietada != null ? agrietada : cuerpo);
            cae = srCae.transform;
        }
        float piso = Piso(srCae.bounds.center.x, srCae.bounds.min.y);
        Vector2 vel = new Vector2(Random.Range(-0.4f, 0.4f), 0.6f);
        float giro = Random.Range(-90f, 90f);
        int rebotes = 0; bool quieta = false;

        // 3) cae, rebota y queda en el piso; el polen flota hasta que la chica lo absorbe
        while (true)
        {
            float dt = Time.deltaTime;
            if (!quieta)
            {
                vel.y -= gravedad * dt;
                cae.position += (Vector3)(vel * dt);
                cae.Rotate(0f, 0f, giro * dt);
                float abajo = srCae.bounds.min.y;
                if (vel.y < 0f && abajo <= piso)
                {
                    cae.position += Vector3.up * (piso - abajo);
                    if (rebotes < 2) { vel = new Vector2(vel.x * 0.5f, -vel.y * 0.3f); giro *= 0.4f; rebotes++; }
                    else quieta = true;
                }
            }
            bool quedaPolen = ActualizarPolen(polen, Time.time - inicioPolen);
            if (quieta && !quedaPolen) yield break;
            yield return null;
        }
    }

    private List<Polen> SoltarPolen()
    {
        var lista = new List<Polen>();
        var flores = new List<SpriteRenderer>();
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) if (sr.gameObject.name == "diente de leon") flores.Add(sr);
        float radio = radioPolen / 100f;
        foreach (var f in flores)
        {
            var t = f.transform;
            Vector3 escalaMundo = t.lossyScale;
            t.SetParent(transform.parent, true);
            t.localScale = escalaMundo;
            f.maskInteraction = SpriteMaskInteraction.None;
            f.enabled = true;   // en las botellitas que ya traen el polen dibujado estan ocultos hasta romperse
            f.sortingOrder = 102;
            var p = new Polen { t = t, sr = f, centro = t.position, fase = Random.value * 10f, escala = Mathf.Abs(escalaMundo.x) };
            p.objetivo = p.centro + Random.insideUnitCircle * radio + Vector2.up * radio * 0.5f;
            if (destellos != null && destellos.Length > 0)
            {
                var gd = new GameObject("destello polen"); gd.transform.SetParent(transform.parent, false);
                gd.transform.position = t.position;
                var srd = gd.AddComponent<SpriteRenderer>();
                srd.sprite = destellos[Random.Range(0, destellos.Length)];
                if (materialSinLuz != null) srd.sharedMaterial = materialSinLuz;
                srd.sortingOrder = 101;
                p.td = gd.transform; p.srd = srd;
                p.escalaD = f.bounds.size.y * 1.4f / Mathf.Max(0.01f, srd.sprite.bounds.size.y);
            }
            lista.Add(p);
        }
        return lista;
    }

    // flota meciendose en su lugar; pasado 1 s, si la chica esta cerca vuela hacia ella y desaparece
    private bool ActualizarPolen(List<Polen> polen, float tiempo)
    {
        bool queda = false;
        Vector3 haciaChica = chica != null ? chica.position + Vector3.up * 1.1f : Vector3.zero;
        foreach (var p in polen)
        {
            if (p.t == null) continue;
            queda = true;
            if (!p.absorbiendo)
            {
                float llegar = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / 0.6f));
                Vector2 pos = Vector2.Lerp(p.centro, p.objetivo, llegar)
                              + new Vector2(Mathf.Sin(Time.time * 1.3f + p.fase), Mathf.Sin(Time.time * 1.7f + p.fase * 1.3f)) * (radioPolen / 100f) * 0.35f;
                p.t.position = new Vector3(pos.x, pos.y, p.t.position.z);
                p.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.1f + p.fase) * 15f);
                if (p.td != null)
                {
                    p.td.position = p.t.position + Vector3.forward * 0.01f;
                    float titila = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f + p.fase);
                    p.td.localScale = Vector3.one * p.escalaD * titila;
                    p.srd.color = new Color(1f, 1f, 1f, 0.55f * titila * Mathf.Clamp01(tiempo / 0.4f));
                }
                if (chica != null && tiempo >= esperaAbsorcion && Vector2.Distance(p.t.position, haciaChica) <= distanciaAbsorcion)
                { p.absorbiendo = true; p.inicioAbs = Time.time; p.desdeAbs = p.t.position; }
                continue;
            }
            float u = Mathf.Clamp01((Time.time - p.inicioAbs) / 0.35f);
            float k = u * u;
            p.t.position = Vector3.Lerp(p.desdeAbs, haciaChica, k);
            p.t.localScale = Vector3.one * p.escala * (1f - 0.8f * k);
            p.sr.color = new Color(p.sr.color.r, p.sr.color.g, p.sr.color.b, 1f - k * 0.6f);
            if (p.td != null) { p.td.position = p.t.position; p.td.localScale = Vector3.one * p.escalaD * (1f - 0.8f * k); }
            if (u >= 1f)
            {
                if (p.td != null) Destroy(p.td.gameObject);
                Destroy(p.t.gameObject);
                p.t = null;
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
        sr.sortingLayerID = dibujo.sortingLayerID; sr.flipX = dibujo.flipX; sr.flipY = dibujo.flipY;
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
