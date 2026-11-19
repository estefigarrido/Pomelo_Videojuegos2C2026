using System.Collections;
using UnityEngine;

// La puerta dorada de la parte 2 de Inicio_introduccion (como la puerta del final del Nivel 1, FinalNivel):
//  1) la chica llega frente a la puerta: se le sacan los controles y camina (con su animacion) hasta quedar centrada
//     con el portal;
//  2) la puerta sube (sin asomarse por arriba del portal);
//  3) la chica se da vuelta (chica_entradaportal) y, ya de espaldas, pasa detras de la puerta y del portal;
//  4) la puerta baja delante de ella y aparece la pantalla "Ingresaste al Templo de botanica" (EntradaTemplo).
// Va en el portal (Group 290). Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción.
public class PuertaDelTemplo : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    [Header("Portal y puerta")]
    [Tooltip("El marco del portal (Group 290): la puerta solo se ve por debajo de su borde de arriba.")]
    [SerializeField] private SpriteRenderer portal;
    [Tooltip("La puerta que sube y baja (Group 289, detras del portal).")]
    [SerializeField] private SpriteRenderer puerta;
    [Tooltip("La pantalla que aparece cuando la chica ya entro.")]
    [SerializeField] private EntradaTemplo pantalla;

    [Header("Donde empieza")]
    [Tooltip("Parte del ancho de la puerta (en el medio) donde cuenta que la chica llego (0 a 1).")]
    [Range(0.05f, 1.5f)]
    [SerializeField] private float anchoEntrada = 0.7f;
    [Tooltip("Cuanto mas abajo que la puerta pueden estar los pies de la chica (unidades): llega caminando por el agua.")]
    [SerializeField] private float margenAbajo = 3f;

    [Header("La chica")]
    [SerializeField] private Sprite quieta;
    [Tooltip("La chica se da vuelta (chica_entradaportal).")]
    [SerializeField] private Sprite[] framesGiro;
    [SerializeField] private float duracionGiro = 0.45f;
    [Tooltip("Velocidad a la que camina hasta el centro del portal (unidades por segundo; en el nivel camina a 3.5).")]
    [SerializeField] private float velocidadCaminando = 3.5f;

    [Header("Tiempos (como en el Nivel 1)")]
    [SerializeField] private float esperaAntesDeSubir = 0.3f;
    [Tooltip("Segundos que tarda en subir (1.75 = lo que dura el arrastre de piedra, como la puerta del Nivel 1).")]
    [SerializeField] private float duracionSubidaPuerta = 1.75f;
    [SerializeField] private float esperaAntesDeBajar = 0.5f;
    [Tooltip("Segundos que tarda en bajar (1.75 = lo que dura el arrastre de piedra, como la puerta del Nivel 1).")]
    [SerializeField] private float duracionBajadaPuerta = 1.75f;
    [Tooltip("Segundos despues de que se cierra la puerta hasta que aparece la pantalla de entrada.")]
    [SerializeField] private float esperaPantalla = 3f;

    [Header("Sonido")]
    [Tooltip("Al abrirse y al cerrarse: el mismo arrastre de piedra que la puerta del Nivel 1 (Puerta_piedra_subiendo).")]
    [SerializeField] private AudioClip sonidoPuerta;
    [SerializeField] private float volumenPuerta = 0.6f;

    private Transform chica;
    private Collider2D colChica;
    private SpriteRenderer dibujoChica;
    private bool empezo;
    private float piesDesdePosicion, anchoMedio;

    public bool Empezo => empezo;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null)
        {
            chica = salud.transform;
            colChica = salud.GetComponent<Collider2D>();
            dibujoChica = salud.GetComponent<SpriteRenderer>();
        }
        // la puerta solo se ve por debajo del borde de arriba del portal (asi al subir no asoma por arriba)
        if (puerta != null && portal != null)
        {
            CrearMascara();
            puerta.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    private void OnDestroy()
    {
        if (empezo) QuietaPorEscena.Soltar();
    }

    private void Update()
    {
        if (empezo || chica == null || puerta == null || !chica.gameObject.activeInHierarchy) return;
        if (CuadroDialogo.Abierto || QuietaPorEscena.Activa) return;
        if (!Llego()) return;
        empezo = true;
        if (mostrarDebug) Debug.Log("[Puerta del templo] la chica llego a la puerta");
        StartCoroutine(Secuencia());
    }

    // la chica esta frente a la puerta (de costado, cerca del medio) y a su altura (de los pies para arriba)
    private bool Llego()
    {
        var b = puerta.bounds;
        Vector3 c = colChica != null ? colChica.bounds.center : chica.position;
        float pies = colChica != null ? colChica.bounds.min.y : chica.position.y;
        // (de los pies: hasta 'margenAbajo' por debajo de la puerta, porque la chica puede llegar caminando por el agua,
        // mas abajo que los pilares donde esta apoyada la puerta)
        return Mathf.Abs(c.x - b.center.x) <= b.size.x * anchoEntrada / 2f && pies >= b.min.y - margenAbajo && pies <= b.max.y;
    }

    private IEnumerator Secuencia()
    {
        // la chica ya no se controla
        QuietaPorEscena.Frenar();
        // la musica de la intro se apaga (lo hace tambien PortalFinIntro con su trigger; asi no depende de que la chica
        // llegue a tocarlo)
        if (MusicaIntro.Instancia != null) MusicaIntro.Instancia.SalirPorElPortal();
        var mov = chica.GetComponent<MovimientoPersonaje>();
        if (mov != null) mov.enabled = false;
        var animator = chica.GetComponent<Animator>();
        var rb = chica.GetComponent<Rigidbody2D>();
        // donde estan los pies con respecto a su posicion (sin fisica, el colisionador no se actualiza al moverla)
        piesDesdePosicion = colChica != null ? colChica.bounds.min.y - chica.position.y : 0f;
        anchoMedio = colChica != null ? colChica.bounds.center.x - chica.position.x : 0f;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        // camina (con su animacion de caminar, sin deslizarse) hasta quedar centrada con el portal
        float centro = portal != null ? portal.bounds.center.x : puerta.bounds.center.x;
        float dx = centro - (colChica != null ? colChica.bounds.center.x : chica.position.x);
        if (Mathf.Abs(dx) > 0.02f)
        {
            var escala = chica.localScale;
            escala.x = Mathf.Abs(escala.x) * Mathf.Sign(dx);   // mira hacia donde camina
            chica.localScale = escala;
            if (animator != null)
            {
                animator.enabled = true;
                PonerBool(animator, "sprint", false);
                PonerBool(animator, "caminando", true);
            }
            Vector3 desde = chica.position, hasta = desde + Vector3.right * dx;
            float duracion = Mathf.Abs(dx) / Mathf.Max(0.1f, velocidadCaminando);
            for (float t = 0f; t < duracion; t += Time.deltaTime)
            {
                chica.position = Vector3.Lerp(desde, hasta, t / duracion);
                yield return null;
            }
            chica.position = hasta;
        }
        if (animator != null) { PonerBool(animator, "caminando", false); animator.enabled = false; }
        if (quieta != null && dibujoChica != null) dibujoChica.sprite = quieta;
        // (Estefi, 19/11) sin fisica la chica no cae: si llego saltando, o el piso del medio del portal esta mas abajo,
        // quedaba flotando. Se apoya en el piso que tiene debajo.
        ApoyarEnElPiso();
        yield return new WaitForSeconds(esperaAntesDeSubir);

        // la puerta sube, con el arrastre de piedra (como la del Nivel 1 al abrirse)
        if (sonidoPuerta != null) Sonido.Reproducir(sonidoPuerta, volumenPuerta);
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
        if (dibujoChica != null) { dibujoChica.sortingLayerID = puerta.sortingLayerID; dibujoChica.sortingOrder = puerta.sortingOrder - 1; }
        yield return new WaitForSeconds(esperaAntesDeBajar);

        // la puerta baja (tapandola: ya entro), con el arrastre de piedra de la puerta del Nivel 1, y a los
        // 'esperaPantalla' segundos aparece la pantalla
        if (sonidoPuerta != null) Sonido.Reproducir(sonidoPuerta, volumenPuerta);
        yield return Deslizar(puerta.transform, abierta, cerrada, duracionBajadaPuerta);
        yield return new WaitForSeconds(esperaPantalla);
        if (mostrarDebug) Debug.Log("[Puerta del templo] entro: aparece la pantalla");
        if (pantalla != null) pantalla.MostrarAhora();
    }

    // baja (o sube un poquito) a la chica hasta que sus pies tocan el primer piso solido que tiene debajo
    private void ApoyarEnElPiso()
    {
        if (colChica == null) return;
        float pies = chica.position.y + piesDesdePosicion;
        var desde = new Vector2(chica.position.x + anchoMedio, pies + 0.3f);
        foreach (var h in Physics2D.RaycastAll(desde, Vector2.down, 20f))
        {
            if (h.collider == null || h.collider.isTrigger || h.collider.transform.IsChildOf(chica)) continue;
            float bajar = pies - h.point.y;
            chica.position += Vector3.down * bajar;
            if (mostrarDebug) Debug.Log("[Puerta del templo] la chica se apoya en " + h.collider.name + " (bajo " + bajar.ToString("F2") + ")");
            return;
        }
    }

    private static void PonerBool(Animator a, string nombre, bool valor)
    {
        foreach (var p in a.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Bool) { a.SetBool(nombre, valor); return; }
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

    private void CrearMascara()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        var go = new GameObject("Mascara puerta");
        go.transform.SetParent(transform, true);
        var m = go.AddComponent<SpriteMask>();
        m.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); // 1x1 unidad
        Bounds b = portal.bounds;
        float arriba = b.max.y, abajo = b.min.y - 6f;
        go.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        var escalaPadre = transform.lossyScale;
        go.transform.localScale = new Vector3((b.size.x + 2f) / escalaPadre.x, (arriba - abajo) / escalaPadre.y, 1f);
        // solo afecta a la puerta (su orden), no al resto del dibujo
        m.isCustomRangeActive = true;
        m.frontSortingLayerID = puerta.sortingLayerID; m.frontSortingOrder = puerta.sortingOrder;
        m.backSortingLayerID = puerta.sortingLayerID; m.backSortingOrder = puerta.sortingOrder - 1;
    }

    private void OnDrawGizmosSelected()
    {
        if (puerta == null) return;
        var b = puerta.bounds;
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(new Vector3(b.center.x, (b.min.y - margenAbajo + b.max.y) / 2f, 0f),
                            new Vector3(b.size.x * anchoEntrada, b.max.y - b.min.y + margenAbajo, 0f));
    }
}
