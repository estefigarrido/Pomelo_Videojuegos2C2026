using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cinematica del final del nivel, frente al portal de salida:
//  1) la chica llega con la gema a la zona del portal: se le sacan los controles y hace un doble salto
//     (con la gema en la mano) hasta la altura del engarce del portal.
//  2) la gema queda encajada en el engarce, con destellos; la puerta recupera su color.
//  3) a los 2 segundos de los destellos, el portal recupera su color y se van las enredaderas.
//  4) la puerta sube (sin asomarse por arriba del portal), la chica se da vuelta (chica_entradaportal)
//     ya de espaldas pasa detras de la puerta y del portal, y la puerta baja delante de ella.
public class FinalNivel : MonoBehaviour
{
    [Header("Portal y puerta")]
    [SerializeField] private SpriteRenderer portal;
    [SerializeField] private Sprite portalGris;
    [SerializeField] private SpriteRenderer puerta;
    [SerializeField] private Sprite puertaGris;
    [SerializeField] private Sprite enredadera;
    [SerializeField] private EstatuaRestaurada.Colocacion[] enredaderas;

    [Header("Donde empieza (la zona frente al portal)")]
    [SerializeField] private Rect zona = new Rect(-87.93f, 33.8f, 1.63f, 4.5f);

    [Header("Doble salto")]
    [SerializeField] private Sprite quieta;
    [SerializeField] private Sprite[] framesDespegue;
    [SerializeField] private Sprite[] framesSubida;
    [SerializeField] private Sprite[] framesPunta;
    [SerializeField] private Sprite[] framesBajada;
    [SerializeField] private Sprite[] framesAterrizaje;
    [Tooltip("Parte del alto total que sube con el primer salto (el resto con el segundo).")]
    [SerializeField] private float partePrimerSalto = 0.45f;
    [SerializeField] private float duracionPrimerSalto = 0.4f;
    [SerializeField] private float duracionSegundoSalto = 0.45f;
    [SerializeField] private float pausaArriba = 0.35f;
    [SerializeField] private float duracionCaida = 0.6f;

    [Header("Engarce")]
    [Tooltip("Centro del engarce (la gota del arco) en el mundo.")]
    [SerializeField] private Vector2 engarce = new Vector2(-87.10f, 41.81f);
    [Tooltip("Tamano de la gema encajada (1 = el de la gema suelta, que llena la gota).")]
    [SerializeField] private float tamanoGema = 1f;
    [SerializeField] private float duracionEncaje = 0.25f;
    [SerializeField] private Sprite[] framesDestello;
    [SerializeField] private float fpsDestello = 14f;
    [Tooltip("Destellos sobre la gema: posicion relativa al centro de la gema y escala.")]
    [SerializeField] private EstatuaRestaurada.Colocacion[] destellos;
    [SerializeField] private Material materialBrillo;

    [Header("Tiempos")]
    [SerializeField] private float duracionColorPuerta = 1f;
    [Tooltip("Segundos de destellos de la gema antes de que el portal recupere su color.")]
    [SerializeField] private float esperaDestellos = 2f;
    [SerializeField] private float duracionColorPortal = 2f;
    [SerializeField] private float duracionSubidaPuerta = 2.5f;
    [Header("La chica se da vuelta (chica_entradaportal)")]
    [SerializeField] private Sprite[] framesGiro;
    [Tooltip("Cuanto tarda en darse vuelta (los frames se funden entre si).")]
    [SerializeField] private float duracionGiro = 0.45f;
    [SerializeField] private float esperaAntesDeBajar = 0.5f;
    [SerializeField] private float duracionBajadaPuerta = 2.5f;

    [Header("Camara")]
    [Tooltip("Con tamano 5 y pantalla 16:9, el centro no puede estar mas a la izquierda de -85.8 (ahi termina el fondo).")]
    [SerializeField] private Vector2 centroCamara = new Vector2(-85.8f, 38.7f);
    [SerializeField] private float tamanoCamara = 5f;

    private Transform chica;
    private SpriteRenderer dibujoChica;
    private SpriteRenderer portalGrisSR, puertaGrisSR;
    private readonly List<SpriteRenderer> plantas = new List<SpriteRenderer>();
    private bool empezo;
    private bool termino;

    private class Destello { public SpriteRenderer sr; public float proximo; public float inicio; }
    private readonly List<Destello> activos = new List<Destello>();

    public bool Termino => termino;
    public bool Empezo => empezo;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) { chica = salud.transform; dibujoChica = salud.GetComponent<SpriteRenderer>(); }

        // portal y puerta empiezan grises (copias grises encima de las de color) y el portal con enredaderas
        if (portal != null && portalGris != null) portalGrisSR = CopiaEncima(portal, portalGris, "Portal gris");
        if (puerta != null && puertaGris != null) puertaGrisSR = CopiaEncima(puerta, puertaGris, "Puerta gris");
        if (portal != null && enredadera != null && enredaderas != null)
            foreach (var c in enredaderas)
            {
                var sr = NuevoDibujo("Enredadera portal", enredadera, portal.sortingLayerID, portal.sortingOrder + 2, portal.sharedMaterial);
                sr.transform.SetParent(portal.transform, true);
                sr.transform.position = new Vector3(c.posicion.x, c.posicion.y, portal.transform.position.z);
                sr.transform.localScale = new Vector3(c.escala.x / portal.transform.lossyScale.x, c.escala.y / portal.transform.lossyScale.y, 1f);
                plantas.Add(sr);
            }

        // la puerta solo se ve por debajo del borde de arriba del portal (asi al subir no asoma por arriba)
        if (puerta != null && portal != null)
        {
            CrearMascara();
            puerta.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            if (puertaGrisSR != null) puertaGrisSR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        // mientras estan grises, las versiones de color no se dibujan (se prenden recien en el fundido)
        if (portalGrisSR != null) portal.enabled = false;
        if (puertaGrisSR != null) puerta.enabled = false;
    }

    private SpriteRenderer CopiaEncima(SpriteRenderer original, Sprite sprite, string nombre)
    {
        var sr = NuevoDibujo(nombre, sprite, original.sortingLayerID, original.sortingOrder + 1, original.sharedMaterial);
        sr.transform.SetParent(original.transform, false);
        // un poquito mas cerca de la camara, asi queda adelante de la de color tambien por distancia
        sr.transform.localPosition = new Vector3(0f, 0f, -0.02f / Mathf.Max(0.0001f, original.transform.lossyScale.z));
        return sr;
    }

    private static SpriteRenderer NuevoDibujo(string nombre, Sprite sprite, int capa, int orden, Material material)
    {
        var go = new GameObject(nombre);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = capa;
        sr.sortingOrder = orden;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }

    private void CrearMascara()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        var go = new GameObject("Mascara puerta");
        var m = go.AddComponent<SpriteMask>();
        m.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); // 1x1 unidad
        Bounds b = portal.bounds;
        float arriba = b.max.y, abajo = b.min.y - 6f;
        go.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        go.transform.localScale = new Vector3(b.size.x + 2f, arriba - abajo, 1f);
    }

    private void Update()
    {
        if (!empezo && chica != null && GemaEnMano.TieneGema && zona.Contains((Vector2)chica.position))
        {
            empezo = true;
            StartCoroutine(Secuencia());
        }
        AnimarDestellos();
    }

    // ---------------- la secuencia ----------------

    private IEnumerator Secuencia()
    {
        // el nivel termino: la chica ya no se controla
        var mov = chica.GetComponent<MovimientoPersonaje>();
        if (mov != null) mov.enabled = false;
        var latigo = chica.GetComponent<AtaqueLatigo>();
        if (latigo != null) latigo.enabled = false;
        var sabiduria = chica.GetComponent<AtaqueSabiduria>();
        if (sabiduria != null) sabiduria.enabled = false;
        var animator = chica.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        var rb = chica.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        var camara = FindFirstObjectByType<CamaraSeguidora>();
        if (camara != null) camara.FijarEncuadre(centroCamara, tamanoCamara);

        var gemaMano = chica.GetComponent<GemaEnMano>();
        Vector3 piso = chica.position;
        Vector3 centro = new Vector3(engarce.x, piso.y, piso.z);

        // altura a la que tiene que llegar para que la gema (en la mano) quede en el engarce
        Sprite punta = framesPunta != null && framesPunta.Length > 0 ? framesPunta[0] : null;
        Vector3 offsetGema = gemaMano != null ? gemaMano.PosicionGema(punta, Vector3.zero) : Vector3.zero;
        Vector3 arriba = new Vector3(engarce.x - offsetGema.x, engarce.y - offsetGema.y, piso.z);
        Vector3 medio = Vector3.Lerp(piso, arriba, partePrimerSalto);
        medio.x = Mathf.Lerp(piso.x, centro.x, 0.5f);

        // primer salto
        yield return Frames(framesDespegue, 0.12f);
        yield return Mover(piso, medio, duracionPrimerSalto, framesSubida, true);
        // segundo salto (desde el aire)
        yield return Mover(medio, arriba, duracionSegundoSalto, framesSubida, true);
        MostrarChica(punta);

        // la gema pasa de la mano al engarce
        Transform gema = gemaMano != null ? gemaMano.Soltar() : null;
        if (gema != null) StartCoroutine(Encajar(gema));
        yield return new WaitForSeconds(pausaArriba);

        // baja al piso (en el centro del portal) y aterriza
        yield return Mover(arriba, centro, duracionCaida, framesBajada, false);
        yield return Frames(framesAterrizaje, 0.3f);
        MostrarChica(quieta);

        // espera a que terminen los 2 s de destellos de la gema (cuentan desde que se encaja)
        while (tiempoEncaje < 0f) yield return null;
        while (Time.time < tiempoEncaje + esperaDestellos) yield return null;

        // vuelve el color del portal y se van las enredaderas
        portal.enabled = true;
        for (float t = 0f; t < duracionColorPortal; t += Time.deltaTime)
        {
            float k = t / duracionColorPortal;
            SetAlfa(portalGrisSR, 1f - k);
            foreach (var p in plantas) SetAlfa(p, 1f - k);
            yield return null;
        }
        if (portalGrisSR != null) Destroy(portalGrisSR.gameObject);
        foreach (var p in plantas) Destroy(p.gameObject);
        plantas.Clear();
        // por si la puerta todavia no termino de recuperar su color
        while (puertaGrisSR != null) yield return null;

        // la puerta sube
        Vector3 cerrada = puerta.transform.position;
        Vector3 abierta = cerrada + Vector3.up * puerta.bounds.size.y;
        yield return Deslizar(puerta.transform, cerrada, abierta, duracionSubidaPuerta);

        // la chica se da vuelta y queda de espaldas (desde el dibujo que tenia, fundiendo los frames)
        if (framesGiro != null && framesGiro.Length > 0 && dibujoChica != null)
        {
            var giro = new Sprite[framesGiro.Length + 1];
            giro[0] = dibujoChica.sprite;
            framesGiro.CopyTo(giro, 1);
            yield return GiroSuave.Reproducir(dibujoChica, giro, duracionGiro);
        }
        // ya de espaldas, pasa detras de la puerta y del portal
        if (dibujoChica != null && puerta != null) dibujoChica.sortingOrder = puerta.sortingOrder - 1;
        yield return new WaitForSeconds(esperaAntesDeBajar);

        // la puerta baja (tapandola: ya entro al portal)
        yield return Deslizar(puerta.transform, abierta, cerrada, duracionBajadaPuerta);
        termino = true;
    }

    private float tiempoEncaje = -1f;

    private IEnumerator Encajar(Transform gema)
    {
        Vector3 desde = gema.position;
        Vector3 escalaDesde = gema.localScale;
        Vector3 hasta = new Vector3(engarce.x, engarce.y, gema.position.z);
        var sr = gema.GetComponent<SpriteRenderer>();
        var rayos = gema.Find("Destello") != null ? gema.Find("Destello").GetComponent<SpriteRenderer>() : null;
        if (sr != null && portal != null) sr.sortingOrder = portal.sortingOrder + 3;
        if (rayos != null && portal != null) rayos.sortingOrder = portal.sortingOrder + 2;
        for (float t = 0f; t < duracionEncaje; t += Time.deltaTime)
        {
            float k = t / duracionEncaje;
            gema.position = Vector3.Lerp(desde, hasta, k);
            gema.localScale = Vector3.Lerp(escalaDesde, Vector3.one * tamanoGema, k);
            if (rayos != null) SetAlfa(rayos, 1f - k);
            yield return null;
        }
        gema.position = hasta;
        gema.localScale = Vector3.one * tamanoGema;
        if (rayos != null) Destroy(rayos.gameObject);
        tiempoEncaje = Time.time;
        CrearDestellos(hasta);

        // la puerta recupera su color apenas aparece la gema en el engarce
        puerta.enabled = true;
        for (float t = 0f; t < duracionColorPuerta; t += Time.deltaTime)
        {
            SetAlfa(puertaGrisSR, 1f - t / duracionColorPuerta);
            yield return null;
        }
        if (puertaGrisSR != null) Destroy(puertaGrisSR.gameObject);
        puertaGrisSR = null;
    }

    private IEnumerator Frames(Sprite[] lista, float duracion)
    {
        if (lista == null || lista.Length == 0) yield break;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            MostrarChica(lista[Mathf.Min(lista.Length - 1, Mathf.FloorToInt(t / duracion * lista.Length))]);
            yield return null;
        }
    }

    // mueve a la chica mostrando los frames; subiendo frena al final, bajando acelera (como un salto)
    private IEnumerator Mover(Vector3 desde, Vector3 hasta, float duracion, Sprite[] lista, bool subiendo)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = t / duracion;
            float ky = subiendo ? 1f - (1f - k) * (1f - k) : k * k;
            chica.position = new Vector3(Mathf.Lerp(desde.x, hasta.x, k), Mathf.Lerp(desde.y, hasta.y, ky), desde.z);
            if (lista != null && lista.Length > 0) MostrarChica(lista[Mathf.Min(lista.Length - 1, Mathf.FloorToInt(k * lista.Length))]);
            yield return null;
        }
        chica.position = hasta;
    }

    private static IEnumerator Deslizar(Transform t, Vector3 desde, Vector3 hasta, float duracion)
    {
        for (float x = 0f; x < duracion; x += Time.deltaTime)
        {
            t.position = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, x / duracion));
            yield return null;
        }
        t.position = hasta;
    }

    private void MostrarChica(Sprite s)
    {
        if (s != null && dibujoChica != null) dibujoChica.sprite = s;
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        var c = sr.color; c.a = a; sr.color = c;
    }

    // ---------------- destellos de la gema (igual que los de la estatua) ----------------

    private void CrearDestellos(Vector3 centroGema)
    {
        if (framesDestello == null || framesDestello.Length == 0 || destellos == null) return;
        foreach (var c in destellos)
        {
            var sr = NuevoDibujo("Destello gema", framesDestello[0], portal.sortingLayerID, portal.sortingOrder + 4, materialBrillo);
            sr.transform.position = centroGema + new Vector3(c.posicion.x, c.posicion.y, 0f);
            sr.transform.localScale = new Vector3(c.escala.x, c.escala.y, 1f);
            sr.enabled = false;
            activos.Add(new Destello { sr = sr, proximo = Time.time + Random.Range(0f, 0.3f) });
        }
    }

    private void AnimarDestellos()
    {
        if (activos.Count == 0) return;
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
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(zona.center, zona.size);
        Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(engarce, 0.3f);
    }
}
