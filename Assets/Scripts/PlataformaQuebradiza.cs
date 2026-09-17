using System.Collections.Generic;
using UnityEngine;

// Piedra que levita. Cuando algo con peso se para encima empieza a quebrarse (tiembla cada
// vez mas). Si sigue encima 5 s, cae con la gravedad; se queda 3 s en el piso y vuelve
// flotando a su lugar. Si se bajan antes de los 5 s, se recupera y el contador vuelve a cero.
// Mientras cae y vuelve solo choca con el mapa: atraviesa a la chica y a las otras piedras.
[RequireComponent(typeof(Rigidbody2D))]
public class PlataformaQuebradiza : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos que hay que estar parada encima para que se caiga (lo que dura el temblor).")]
    [SerializeField] private float segundosHastaCaer = 5f;
    [Tooltip("Segundos que se queda en el piso, contados desde que lo toca.")]
    [SerializeField] private float segundosEnElPiso = 3f;
    [Tooltip("Segundos que tarda en volver flotando a su lugar.")]
    [SerializeField] private float segundosParaVolver = 1.5f;

    [Header("Temblor")]
    [Tooltip("El hijo con el dibujo. Tiembla solo el dibujo y no el collider, asi no sacude a la chica.")]
    [SerializeField] private Transform dibujo;
    [SerializeField] private float temblorInicial = 0.015f;
    [SerializeField] private float temblorFinal = 0.12f;
    [SerializeField] private float velocidadTemblor = 30f;

    [Header("Caida")]
    [SerializeField] private float escalaGravedad = 1f;
    [Tooltip("Si cae mas que esto sin tocar el mapa, vuelve igual.")]
    [SerializeField] private float segundosMaximosCayendo = 8f;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    // cuanto puede faltar el contacto sin que cuente como que se bajo (al caminar por el borde parpadea)
    private const float MargenContacto = 0.1f;

    private enum Estado { Quieta, Quebrandose, Cayendo, EnElPiso, Volviendo, Acomodandose }

    private static readonly List<PlataformaQuebradiza> todas = new List<PlataformaQuebradiza>();

    private Rigidbody2D rb;
    private Collider2D[] propios;
    private readonly List<Collider2D> jugadores = new List<Collider2D>();

    private Estado estado = Estado.Quieta;
    private Vector2 posicionOriginal;
    private Vector2 desdeDondeVuelve;
    private float momentoCambio;
    private float tiempoPisada;
    private float ultimaVezPisada = -99f;
    private float semilla;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarLista() => todas.Clear();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        propios = GetComponents<Collider2D>();
        posicionOriginal = rb.position;
        semilla = Random.value * 100f;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void OnEnable()
    {
        // las piedras nunca chocan entre ellas
        foreach (var otra in todas)
            foreach (var a in propios)
                foreach (var b in otra.propios)
                    if (a != null && b != null) Physics2D.IgnoreCollision(a, b, true);
        todas.Add(this);
    }

    private void OnDisable() => todas.Remove(this);

    private void Start()
    {
        foreach (var chica in FindObjectsByType<MovimientoPersonaje>(FindObjectsSortMode.None))
            jugadores.AddRange(chica.GetComponentsInChildren<Collider2D>());
    }

    private void FixedUpdate()
    {
        bool pisada = Time.time - ultimaVezPisada <= MargenContacto;

        switch (estado)
        {
            case Estado.Quieta:
                if (pisada)
                {
                    estado = Estado.Quebrandose;
                    tiempoPisada = 0f;
                    Avisar("empieza a quebrarse");
                }
                break;

            case Estado.Quebrandose:
                if (!pisada)
                {
                    Recuperarse();
                    break;
                }
                tiempoPisada += Time.fixedDeltaTime;
                if (tiempoPisada >= segundosHastaCaer) Caer();
                break;

            case Estado.Cayendo:
                if (Time.time - momentoCambio >= segundosMaximosCayendo) Aterrizar();
                break;

            case Estado.EnElPiso:
                if (Time.time - momentoCambio >= segundosEnElPiso) EmpezarAVolver();
                break;

            case Estado.Volviendo:
                float t = segundosParaVolver > 0f ? Mathf.Clamp01((Time.time - momentoCambio) / segundosParaVolver) : 1f;
                float suave = t * t * (3f - 2f * t);
                rb.MovePosition(Vector2.Lerp(desdeDondeVuelve, posicionOriginal, suave));
                if (t >= 1f) estado = Estado.Acomodandose;
                break;

            case Estado.Acomodandose:
                // no se vuelve solida hasta que la chica no este adentro de ella
                if (!TocaAlgunJugador())
                {
                    IgnorarJugadores(false);
                    estado = Estado.Quieta;
                    ultimaVezPisada = -99f;
                    Avisar("volvio a su lugar");
                }
                break;
        }
    }

    private void Update()
    {
        if (dibujo == null) return;

        if (estado != Estado.Quebrandose)
        {
            if (dibujo.localPosition != Vector3.zero) dibujo.localPosition = Vector3.zero;
            return;
        }

        // tiembla cada vez mas fuerte a medida que se acerca la caida
        float avance = Mathf.Clamp01(tiempoPisada / segundosHastaCaer);
        float amplitud = Mathf.Lerp(temblorInicial, temblorFinal, avance * avance);
        float t = Time.time * velocidadTemblor;
        dibujo.localPosition = new Vector3(
            (Mathf.PerlinNoise(t, semilla) - 0.5f) * 2f * amplitud,
            (Mathf.PerlinNoise(semilla, t) - 0.5f) * 2f * amplitud,
            0f);
    }

    // ---------------- cambios de estado ----------------

    private void Recuperarse()
    {
        estado = Estado.Quieta;
        tiempoPisada = 0f;
        Avisar("se bajaron antes de tiempo, se recupera");
    }

    private void Caer()
    {
        estado = Estado.Cayendo;
        momentoCambio = Time.time;

        IgnorarJugadores(true);
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = escalaGravedad;
        // cae derecho, sin girar ni resbalar por las pendientes
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
        Avisar("se cae");
    }

    private void Aterrizar()
    {
        estado = Estado.EnElPiso;
        momentoCambio = Time.time;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        Avisar("toco el piso");
    }

    private void EmpezarAVolver()
    {
        estado = Estado.Volviendo;
        momentoCambio = Time.time;
        desdeDondeVuelve = rb.position;
        Avisar("vuelve flotando");
    }

    // ---------------- contactos ----------------

    private void OnCollisionEnter2D(Collision2D choque) => RevisarContacto(choque);
    private void OnCollisionStay2D(Collision2D choque) => RevisarContacto(choque);

    private void RevisarContacto(Collision2D choque)
    {
        // cayendo, lo unico con lo que puede chocar es el mapa (lo demas esta ignorado)
        if (estado == Estado.Cayendo)
        {
            Aterrizar();
            return;
        }

        if (estado != Estado.Quieta && estado != Estado.Quebrandose) return;

        var cuerpo = choque.rigidbody;
        if (cuerpo == null || cuerpo.bodyType != RigidbodyType2D.Dynamic) return;
        if (cuerpo.GetComponent<PlataformaQuebradiza>() != null) return;

        float centroDelOtro = choque.collider.bounds.center.y;
        for (int i = 0; i < choque.contactCount; i++)
        {
            var contacto = choque.GetContact(i);
            // contacto de arriba o de abajo, y el otro con el centro por encima: esta parado arriba
            if (Mathf.Abs(contacto.normal.y) >= 0.6f && centroDelOtro > contacto.point.y)
            {
                ultimaVezPisada = Time.time;
                return;
            }
        }
    }

    private void IgnorarJugadores(bool ignorar)
    {
        foreach (var jugador in jugadores)
            foreach (var propio in propios)
                if (jugador != null && propio != null) Physics2D.IgnoreCollision(propio, jugador, ignorar);
    }

    private bool TocaAlgunJugador()
    {
        foreach (var jugador in jugadores)
        {
            if (jugador == null || !jugador.enabled || !jugador.gameObject.activeInHierarchy) continue;
            foreach (var propio in propios)
                if (propio != null && propio.Distance(jugador).isOverlapped) return true;
        }
        return false;
    }

    private void Avisar(string que)
    {
        if (mostrarDebug) Debug.Log("[" + name + "] " + que, this);
    }

    private void OnDrawGizmosSelected()
    {
        // en el editor muestra el lugar al que vuelve
        Vector3 origen = Application.isPlaying ? (Vector3)posicionOriginal : transform.position;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(origen, 0.25f);
    }
}
