using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Correr  (A y D)")]
    [Tooltip("En unidades por segundo. 1 unidad = 100 pixeles de pantalla, asi que 3.5 son 350 px/s.")]
    [SerializeField] private float velocidad = 3.5f;

    [Header("Sprint  (doble toque de A o de D)")]
    [Tooltip("2 = corre el doble de rapido que el correr normal.")]
    [SerializeField] private float multiplicadorSprint = 2f;
    [Tooltip("Cuantos segundos dura el sprint. Poner 0 para que no se apague solo.")]
    [SerializeField] private float duracionSprint = 8f;
    [Tooltip("Cuanto hay que esperar para poder volver a sprintear.")]
    [SerializeField] private float cooldownSprint = 3f;

    [Header("Salto  (espacio)")]
    [Tooltip("Altura del salto normal, en pixeles del dibujo.")]
    [SerializeField] private float alturaSaltoPx = 120f;
    [Tooltip("Altura del supersalto (doble toque de espacio), en pixeles del dibujo.")]
    [SerializeField] private float alturaSuperSaltoPx = 350f;
    [Tooltip("Cuantos pixeles del dibujo entran en 1 unidad de Unity. El personaje esta importado a 100.")]
    [SerializeField] private float pixelesPorUnidad = 100f;

    [Header("Deteccion del doble toque")]
    [Tooltip("Tiempo maximo que puede durar una pulsacion para contar como toque y no como mantenida.")]
    [SerializeField] private float duracionMaximaToque = 0.3f;
    [Tooltip("Tiempo maximo entre un toque y el siguiente.")]
    [SerializeField] private float ventanaEntreToques = 0.45f;

    [Header("Piso")]
    [Tooltip("Inclinacion maxima que puede tener una superficie para que cuente como piso.")]
    [SerializeField] private float anguloMaximoPiso = 50f;
    [Tooltip("Margen para saltar justo despues de irse del borde, para que el salto responda siempre.")]
    [SerializeField] private float tiempoCoyote = 0.12f;
    [Tooltip("Margen para que valga el espacio apretado un poquito antes de tocar el piso.")]
    [SerializeField] private float bufferSalto = 0.12f;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    private Rigidbody2D rb;
    private Animator animator;

    private float direccion;

    private bool sprintActivo;
    private float finSprint;
    private float finCooldown;

    private bool enPiso;
    private bool pisoDetectado;

    private float alturaDespegue;
    private float momentoDespegue = -99f;
    private bool superSaltoUsado;

    private float ultimaVezEnPiso = -99f;
    private float ultimoPedidoDeSalto = -99f;

    private readonly DetectorDobleToque toqueA = new DetectorDobleToque();
    private readonly DetectorDobleToque toqueD = new DetectorDobleToque();

    public bool SprintActivo => sprintActivo;
    public bool EnPiso => enPiso;
    public float SegundosRestantesSprint => Mathf.Max(0f, finSprint - Time.time);
    public float SegundosRestantesCooldown => Mathf.Max(0f, finCooldown - Time.time);

    // los pixeles del dibujo pasados a unidades de Unity
    private float AlturaSalto => alturaSaltoPx / Mathf.Max(1f, pixelesPorUnidad);
    private float AlturaSuperSalto => alturaSuperSaltoPx / Mathf.Max(1f, pixelesPorUnidad);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Si Unity duerme el cuerpo cuando esta quieta, deja de avisar que toca el piso
        // y no se puede saltar hasta moverse. Con esto no se duerme nunca.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // el input se lee aca, en el frame exacto en que pasa
        direccion = 0f;
        if (teclado.aKey.isPressed) direccion -= 1f;
        if (teclado.dKey.isPressed) direccion += 1f;

        bool dobleToqueA = toqueA.Evaluar(teclado.aKey, duracionMaximaToque, ventanaEntreToques);
        bool dobleToqueD = toqueD.Evaluar(teclado.dKey, duracionMaximaToque, ventanaEntreToques);
        if (dobleToqueA || dobleToqueD) ActivarSprint();

        ApagarSprintSiSeVencio();
        LeerSalto(teclado);

        // La animacion de correr va siempre que se mueva, con A o D sola.
        // El sprint no cambia la animacion, solo la velocidad.
        if (animator != null) animator.SetBool("sprint", direccion != 0f);

        if (direccion != 0f)
        {
            Vector3 escala = transform.localScale;
            escala.x = Mathf.Abs(escala.x) * (direccion < 0f ? -1f : 1f);
            transform.localScale = escala;
        }
    }

    // ---------------- SPRINT ----------------

    private void ActivarSprint()
    {
        if (sprintActivo || Time.time < finCooldown) return;

        sprintActivo = true;
        finSprint = duracionSprint > 0f ? Time.time + duracionSprint : float.PositiveInfinity;

        if (mostrarDebug)
            Debug.Log(duracionSprint > 0f
                ? "[Sprint] activado por " + duracionSprint + "s"
                : "[Sprint] activado sin limite de tiempo");
    }

    private void ApagarSprintSiSeVencio()
    {
        if (!sprintActivo || Time.time < finSprint) return;

        sprintActivo = false;
        finCooldown = Time.time + cooldownSprint;
        toqueA.Reiniciar();
        toqueD.Reiniciar();

        if (mostrarDebug) Debug.Log("[Sprint] termino. Espera de " + cooldownSprint + "s");
    }

    // ---------------- SALTO ----------------

    private void LeerSalto(Keyboard teclado)
    {
        if (teclado.spaceKey.wasPressedThisFrame)
        {
            // Segundo toque estando en el aire: lo lleva hasta la altura del supersalto.
            bool acabaDeDespegar = Time.time - momentoDespegue <= ventanaEntreToques;
            if (!enPiso && !superSaltoUsado && acabaDeDespegar)
            {
                float loQueFalta = (alturaDespegue + AlturaSuperSalto) - rb.position.y;
                if (loQueFalta > 0f)
                {
                    ImpulsarHasta(loQueFalta);
                    superSaltoUsado = true;
                    if (animator != null) animator.SetTrigger("salto");
                    if (mostrarDebug) Debug.Log("[SuperSalto] hasta " + alturaSuperSaltoPx + "px del despegue");
                }
                return;
            }

            ultimoPedidoDeSalto = Time.time;
        }

        // Vale el espacio apretado un toque antes de aterrizar, y tambien salta
        // si se acaba de ir del borde. Los dos margenes son de una milesima de nada
        // pero hacen que el salto responda siempre.
        bool hayPedido = Time.time - ultimoPedidoDeSalto <= bufferSalto;
        bool puedeDespegar = Time.time - ultimaVezEnPiso <= tiempoCoyote;
        if (!hayPedido || !puedeDespegar) return;

        ImpulsarHasta(AlturaSalto);

        alturaDespegue = rb.position.y;
        momentoDespegue = Time.time;
        superSaltoUsado = false;
        enPiso = false;
        pisoDetectado = false;
        ultimaVezEnPiso = -99f;
        ultimoPedidoDeSalto = -99f;

        if (animator != null) animator.SetTrigger("salto");
        if (mostrarDebug) Debug.Log("[Salto] " + alturaSaltoPx + "px");
    }

    // le da la velocidad justa para que llegue a esa altura y no mas
    private void ImpulsarHasta(float alturaEnUnidades)
    {
        float gravedad = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (gravedad <= 0f || alturaEnUnidades <= 0f) return;

        float impulso = Mathf.Sqrt(2f * gravedad * alturaEnUnidades);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, impulso);
    }

    // ---------------- PISO ----------------

    private void OnCollisionEnter2D(Collision2D choque) => RevisarSiEsPiso(choque);
    private void OnCollisionStay2D(Collision2D choque) => RevisarSiEsPiso(choque);

    private void RevisarSiEsPiso(Collision2D choque)
    {
        float limite = Mathf.Cos(anguloMaximoPiso * Mathf.Deg2Rad);

        for (int i = 0; i < choque.contactCount; i++)
        {
            // si la superficie empuja hacia arriba, esta parada encima
            if (choque.GetContact(i).normal.y >= limite)
            {
                pisoDetectado = true;
                return;
            }
        }
    }

    private void FixedUpdate()
    {
        bool estabaEnElAire = !enPiso;
        enPiso = pisoDetectado;
        pisoDetectado = false;

        if (enPiso)
        {
            ultimaVezEnPiso = Time.time;
            if (estabaEnElAire) superSaltoUsado = false;
        }

        float velocidadActual = sprintActivo ? velocidad * multiplicadorSprint : velocidad;
        rb.linearVelocity = new Vector2(direccion * velocidadActual, rb.linearVelocity.y);
    }

    // Detecta "tocar dos veces rapido" una tecla. Mantenerla apretada no cuenta.
    private class DetectorDobleToque
    {
        private float inicioPulsacion = -1f;
        private float momentoUltimoToque = -99f;

        public bool Evaluar(KeyControl tecla, float duracionMaximaToque, float ventanaEntreToques)
        {
            if (tecla.wasPressedThisFrame)
            {
                if (Time.time - momentoUltimoToque <= ventanaEntreToques)
                {
                    Reiniciar();
                    return true;
                }

                inicioPulsacion = Time.time;
            }

            if (tecla.wasReleasedThisFrame && inicioPulsacion >= 0f)
            {
                float duracion = Time.time - inicioPulsacion;
                inicioPulsacion = -1f;

                // solo una pulsacion corta cuenta como toque; mantener apretado no
                momentoUltimoToque = duracion <= duracionMaximaToque ? Time.time : -99f;
            }

            return false;
        }

        public void Reiniciar()
        {
            inicioPulsacion = -1f;
            momentoUltimoToque = -99f;
        }
    }
}
