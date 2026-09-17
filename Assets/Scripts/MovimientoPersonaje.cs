using System.Collections.Generic;
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

    [Header("Dash  (E)")]
    [Tooltip("Distancia del dash, en pixeles (100 px = 1 unidad).")]
    [SerializeField] private float distanciaDashPx = 500f;
    [Tooltip("Segundos que tarda en recorrer esa distancia.")]
    [SerializeField] private float duracionDash = 0.2f;
    [Tooltip("Segundos de espera desde que termina un dash hasta que se puede hacer otro.")]
    [SerializeField] private float esperaDash = 0.5f;

    [Header("Estela del dash")]
    [Tooltip("Cada cuantos segundos deja una copia transparente mientras dashea.")]
    [SerializeField] private float intervaloEstela = 0.035f;
    [Tooltip("Segundos que tarda cada copia en desaparecer.")]
    [SerializeField] private float duracionEstela = 0.3f;
    [SerializeField] private Color colorEstela = new Color(1f, 1f, 1f, 0.5f);

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

    private SpriteRenderer dibujo;
    private bool animatorTieneDash;
    private bool pedidoDash;
    private bool dashActivo;
    private float dashRestante;
    private float direccionDash;
    private float finUltimoDash = -99f;
    private float gravedadNormal = 1f;
    private float proximaCopia;

    private struct CopiaEstela
    {
        public SpriteRenderer sr;
        public float nacimiento;
    }
    private readonly List<CopiaEstela> estela = new List<CopiaEstela>();

    private readonly DetectorDobleToque toqueA = new DetectorDobleToque();
    private readonly DetectorDobleToque toqueD = new DetectorDobleToque();

    public bool SprintActivo => sprintActivo;
    public bool DashActivo => dashActivo;
    public bool EnPiso => enPiso;

    // 500 px en 0.2 s = 25 unidades por segundo
    private float VelocidadDash => (distanciaDashPx / Mathf.Max(1f, pixelesPorUnidad)) / Mathf.Max(0.01f, duracionDash);
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

        dibujo = GetComponent<SpriteRenderer>();
        gravedadNormal = rb.gravityScale;
        animatorTieneDash = TieneParametro("dash");
    }

    private void OnDisable()
    {
        foreach (var copia in estela)
            if (copia.sr != null) Destroy(copia.sr.gameObject);
        estela.Clear();
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

        // DASH: se puede siempre, en el piso o en el aire, respetando la espera
        if (teclado.eKey.wasPressedThisFrame && PuedeDashear()) pedidoDash = true;

        if (dashActivo)
        {
            // durante el dash no salta, pero se guarda el pedido por si fue justo al final
            if (teclado.spaceKey.wasPressedThisFrame) ultimoPedidoDeSalto = Time.time;
        }
        else
        {
            LeerSalto(teclado);
        }

        // La animacion de correr va siempre que se mueva, con A o D sola.
        // El sprint no cambia la animacion, solo la velocidad.
        if (animator != null) animator.SetBool("sprint", direccion != 0f);

        // mientras dashea sigue mirando para el lado del dash
        if (direccion != 0f && !dashActivo && !pedidoDash) MirarHacia(direccion);

        if (dashActivo && Time.time >= proximaCopia)
        {
            DejarCopia();
            proximaCopia = Time.time + Mathf.Max(0.01f, intervaloEstela);
        }
        ActualizarEstela();
    }

    private void MirarHacia(float lado)
    {
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (lado < 0f ? -1f : 1f);
        transform.localScale = escala;
    }

    // ---------------- DASH ----------------

    private bool PuedeDashear()
    {
        return !dashActivo && !pedidoDash && Time.time >= finUltimoDash + esperaDash;
    }

    private void EmpezarDash()
    {
        // sale para el lado de la tecla apretada; si no hay ninguna, para donde mira
        float lado = direccion != 0f ? Mathf.Sign(direccion) : Mathf.Sign(transform.localScale.x);
        if (lado == 0f) lado = 1f;

        dashActivo = true;
        direccionDash = lado;
        dashRestante = duracionDash;
        proximaCopia = Time.time;
        MirarHacia(lado);

        // va recto: sin gravedad, y corta cualquier subida o caida que tuviera
        gravedadNormal = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(lado * VelocidadDash, 0f);

        if (animator != null)
        {
            animator.ResetTrigger("salto");
            if (animatorTieneDash) animator.SetBool("dash", true);
        }
        if (mostrarDebug) Debug.Log("[Dash] hacia " + (lado > 0f ? "la derecha" : "la izquierda"));
    }

    private void TerminarDash()
    {
        dashActivo = false;
        finUltimoDash = Time.time;

        // al terminar vuelve a caer normalmente
        rb.gravityScale = gravedadNormal;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        if (animator != null && animatorTieneDash) animator.SetBool("dash", false);
    }

    private void DejarCopia()
    {
        if (dibujo == null || dibujo.sprite == null) return;

        var copia = new GameObject("Estela dash");
        copia.transform.SetPositionAndRotation(dibujo.transform.position, dibujo.transform.rotation);
        copia.transform.localScale = dibujo.transform.lossyScale;

        var sr = copia.AddComponent<SpriteRenderer>();
        sr.sprite = dibujo.sprite;
        sr.flipX = dibujo.flipX;
        sr.flipY = dibujo.flipY;
        sr.sharedMaterial = dibujo.sharedMaterial;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = dibujo.sortingOrder - 1;
        sr.color = colorEstela;

        estela.Add(new CopiaEstela { sr = sr, nacimiento = Time.time });
    }

    private void ActualizarEstela()
    {
        for (int i = estela.Count - 1; i >= 0; i--)
        {
            var copia = estela[i];
            if (copia.sr == null)
            {
                estela.RemoveAt(i);
                continue;
            }

            float avance = (Time.time - copia.nacimiento) / Mathf.Max(0.01f, duracionEstela);
            if (avance >= 1f)
            {
                Destroy(copia.sr.gameObject);
                estela.RemoveAt(i);
                continue;
            }

            Color color = colorEstela;
            color.a *= 1f - avance;
            copia.sr.color = color;
        }
    }

    private bool TieneParametro(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var parametro in animator.parameters)
            if (parametro.name == nombre) return true;
        return false;
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

        if (pedidoDash)
        {
            pedidoDash = false;
            EmpezarDash();
        }

        // El dash se cuenta en pasos de fisica para que recorra exactamente la distancia
        if (dashActivo)
        {
            rb.linearVelocity = new Vector2(direccionDash * VelocidadDash, 0f);
            dashRestante -= Time.fixedDeltaTime;
            if (dashRestante <= 0.0001f) TerminarDash();
            return;
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
