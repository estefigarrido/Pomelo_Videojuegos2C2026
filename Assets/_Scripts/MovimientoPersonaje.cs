using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Teclas")]
    [SerializeField] private Key teclaIzquierda = Key.LeftArrow;
    [SerializeField] private Key teclaDerecha = Key.RightArrow;
    [Tooltip("Segunda tecla para ir a la izquierda (sirve igual, sprint incluido).")]
    [SerializeField] private Key teclaIzquierda2 = Key.A;
    [Tooltip("Segunda tecla para ir a la derecha (sirve igual, sprint incluido).")]
    [SerializeField] private Key teclaDerecha2 = Key.D;
    [SerializeField] private Key teclaDash = Key.F;

    [Header("Correr  (flechas o A / D)")]
    [Tooltip("En unidades por segundo. 1 unidad = 100 pixeles de pantalla, asi que 3.5 son 350 px/s.")]
    [SerializeField] private float velocidad = 3.5f;

    [Header("Sprint  (doble toque de una flecha o de A/D, o Shift + direccion)")]
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
    [Tooltip("Segundos que hay para el segundo toque de espacio, contados desde el primero. Un doble click tipico de juego va de 0.2 a 0.3.")]
    [SerializeField] private float ventanaSuperSalto = 0.5f;
    [Tooltip("Cuantos pixeles del dibujo entran en 1 unidad de Unity. El personaje esta importado a 100.")]
    [SerializeField] private float pixelesPorUnidad = 100f;

    [Header("Animacion del salto")]
    [Tooltip("Segundos que tarda la agachada (frames 004-006) al despegar.")]
    [SerializeField] private float duracionDespegue = 0.05f;
    [Tooltip("Segundos que tarda el aterrizaje (frames 023-027) al tocar el piso.")]
    [SerializeField] private float duracionAterrizaje = 0.1f;

    [Header("Dash  (F)")]
    [Tooltip("Distancia del dash, en pixeles (100 px = 1 unidad).")]
    [SerializeField] private float distanciaDashPx = 600f;
    [Tooltip("Segundos que tarda en recorrer esa distancia.")]
    [SerializeField] private float duracionDash = 0.6f;
    [Tooltip("Segundos de espera desde que termina un dash hasta que se puede hacer otro.")]
    [SerializeField] private float esperaDash = 2f;

    [Header("Destello del dash")]
    [Tooltip("El ultimo frame del dash desenfocado. Se ve detras de la chica mientras queda quieto el ultimo frame.")]
    [SerializeField] private Sprite destelloDash;
    [Tooltip("El ultimo frame de la animacion del dash (dash_003). Cuando se muestra este, aparece el destello.")]
    [SerializeField] private Sprite frameFinalDash;
    [SerializeField] private Color colorDestello = Color.white;

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

    // Frames de ChicaSaltoPorAltura.anim (los 27 de chica_saltando, repartidos parejo). Cada fase usa un tramo.
    private const float FramesSalto = 27f;
    private const float FrameDespegue = 4f;      // 004-006 agachada
    private const float FrameSubida = 7f;        // 007-013 subiendo
    private const float FramePuntaSube = 14f;    // 014 llegando arriba
    private const float FramePuntaBaja = 15f;    // 015 empezando a bajar
    private const float FrameBajada = 16f;       // 016-022 bajando
    private const float FrameAterrizaje = 23f;   // 023-027 aterrizando
    // en el ultimo 15% de la subida y el primer 15% de la bajada se queda en la punta
    private const float ZonaPunta = 0.15f;

    private enum FaseSalto { Ninguna, Despegue, Subida, Caida, Aterrizaje }

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer dibujo;
    private SpriteRenderer destello;

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
    private float ignorarPisoHasta = -99f;

    private FaseSalto fase = FaseSalto.Ninguna;
    private float inicioFase;
    private float alturaObjetivo;
    private float progresoSubida;
    private float alturaInicioSubida;
    private float alturaPunta;

    private bool animatorTieneDash;
    private bool animatorTieneSalto;

    private bool pedidoDash;
    private bool dashActivo;
    private float dashRestante;
    private float direccionDash;
    private float finUltimoDash = -99f;
    private float gravedadNormal = 1f;

    private readonly DetectorDobleToque toqueIzquierda = new DetectorDobleToque();
    private readonly DetectorDobleToque toqueDerecha = new DetectorDobleToque();
    private readonly DetectorDobleToque toqueIzquierda2 = new DetectorDobleToque();
    private readonly DetectorDobleToque toqueDerecha2 = new DetectorDobleToque();

    public bool SprintActivo => sprintActivo;
    public bool DashActivo => dashActivo;
    public bool EnPiso => enPiso;
    public float SegundosRestantesSprint => Mathf.Max(0f, finSprint - Time.time);
    public float SegundosRestantesCooldown => Mathf.Max(0f, finCooldown - Time.time);

    // los pixeles pasados a unidades de Unity
    private float AlturaSalto => alturaSaltoPx / Mathf.Max(1f, pixelesPorUnidad);
    private float AlturaSuperSalto => alturaSuperSaltoPx / Mathf.Max(1f, pixelesPorUnidad);
    // 600 px en 0.3 s = 20 unidades por segundo (2000 px/s)
    private float VelocidadDash => (distanciaDashPx / Mathf.Max(1f, pixelesPorUnidad)) / Mathf.Max(0.01f, duracionDash);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        dibujo = GetComponent<SpriteRenderer>();

        // Si Unity duerme el cuerpo cuando esta quieta, deja de avisar que toca el piso
        // y no se puede saltar hasta moverse. Con esto no se duerme nunca.
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        gravedadNormal = rb.gravityScale;

        animatorTieneDash = TieneParametro("dash");
        animatorTieneSalto = TieneParametro("saltando") && TieneParametro("tiempoSalto");

        CrearDestello();
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // el input se lee aca, en el frame exacto en que pasa
        // cada lado tiene dos teclas (flecha y A/D); cualquiera de las dos sirve
        KeyControl izquierda = teclado[teclaIzquierda], izquierda2 = teclado[teclaIzquierda2];
        KeyControl derecha = teclado[teclaDerecha], derecha2 = teclado[teclaDerecha2];
        direccion = 0f;
        if (izquierda.isPressed || izquierda2.isPressed) direccion -= 1f;
        if (derecha.isPressed || derecha2.isPressed) direccion += 1f;

        // el doble toque se cuenta por tecla (dos toques de la misma tecla)
        bool dobleToque = toqueIzquierda.Evaluar(izquierda, duracionMaximaToque, ventanaEntreToques);
        dobleToque |= toqueDerecha.Evaluar(derecha, duracionMaximaToque, ventanaEntreToques);
        dobleToque |= toqueIzquierda2.Evaluar(izquierda2, duracionMaximaToque, ventanaEntreToques);
        dobleToque |= toqueDerecha2.Evaluar(derecha2, duracionMaximaToque, ventanaEntreToques);
        // el sprint sale con doble toque o manteniendo Shift mientras camina; en los dos
        // casos dura lo mismo y despues hay que esperar igual
        bool conShift = teclado.shiftKey.isPressed && direccion != 0f;
        if (dobleToque || conShift) ActivarSprint();

        ApagarSprintSiSeVencio();

        // DASH: se puede siempre, en el piso o en el aire, respetando la espera
        if (teclado[teclaDash].wasPressedThisFrame && PuedeDashear()) pedidoDash = true;

        if (dashActivo)
        {
            // durante el dash no salta, pero se guarda el pedido por si fue justo al final
            if (teclado.spaceKey.wasPressedThisFrame) ultimoPedidoDeSalto = Time.time;
        }
        else
        {
            LeerSalto(teclado);
        }

        // La animacion de correr va siempre que se mueva, con una flecha o A/D.
        // El sprint no cambia la animacion, solo la velocidad.
        if (animator != null) animator.SetBool("sprint", direccion != 0f);

        // mientras dashea sigue mirando para el lado del dash
        if (direccion != 0f && !dashActivo && !pedidoDash) MirarHacia(direccion);

        ActualizarAnimacionSalto();
    }

    private void LateUpdate()
    {
        // el Animator ya puso el frame de este cuadro: el destello va solo con el ultimo frame del dash
        if (destello != null)
            destello.enabled = dashActivo && frameFinalDash != null && dibujo.sprite == frameFinalDash;
    }

    private void MirarHacia(float lado)
    {
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * (lado < 0f ? -1f : 1f);
        transform.localScale = escala;
    }

    private bool TieneParametro(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var parametro in animator.parameters)
            if (parametro.name == nombre) return true;
        return false;
    }

    // ---------------- DASH ----------------

    private void CrearDestello()
    {
        if (destelloDash == null || dibujo == null) return;

        // copia desenfocada del ultimo frame, detras de la chica. Es hija, asi que gira con ella.
        var objeto = new GameObject("Destello dash");
        objeto.transform.SetParent(transform, false);
        destello = objeto.AddComponent<SpriteRenderer>();
        destello.sprite = destelloDash;
        destello.sharedMaterial = dibujo.sharedMaterial;
        destello.sortingLayerID = dibujo.sortingLayerID;
        destello.sortingOrder = dibujo.sortingOrder - 1;
        destello.color = colorDestello;
        destello.enabled = false;
    }

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
        MirarHacia(lado);

        // va recto: sin gravedad, y corta cualquier subida o caida que tuviera
        gravedadNormal = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(lado * VelocidadDash, 0f);

        if (animator != null && animatorTieneDash) animator.SetBool("dash", true);
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
        toqueIzquierda.Reiniciar();
        toqueDerecha.Reiniciar();
        toqueIzquierda2.Reiniciar();
        toqueDerecha2.Reiniciar();

        if (mostrarDebug) Debug.Log("[Sprint] termino. Espera de " + cooldownSprint + "s");
    }

    // ---------------- SALTO ----------------

    private void LeerSalto(Keyboard teclado)
    {
        if (teclado.spaceKey.wasPressedThisFrame)
        {
            // Segundo toque estando en el aire: lo lleva hasta la altura del supersalto.
            bool acabaDeDespegar = Time.time - momentoDespegue <= ventanaSuperSalto;
            if (!enPiso && !superSaltoUsado && acabaDeDespegar)
            {
                float loQueFalta = (alturaDespegue + AlturaSuperSalto) - rb.position.y;
                if (loQueFalta > 0f)
                {
                    ImpulsarHasta(loQueFalta);
                    superSaltoUsado = true;

                    // la misma animacion se estira hasta la nueva altura: no se reinicia
                    alturaObjetivo = AlturaSuperSalto;
                    if (fase == FaseSalto.Caida || fase == FaseSalto.Ninguna) CambiarFase(FaseSalto.Subida);

                    if (mostrarDebug) Debug.Log("[SuperSalto] hasta " + alturaSuperSaltoPx + "px del despegue");
                }
                return;
            }

            ultimoPedidoDeSalto = Time.time;
        }

        // Vale el espacio apretado un toque antes de aterrizar, y tambien salta
        // si se acaba de ir del borde. Los dos margenes son chiquitos pero hacen
        // que el salto responda siempre.
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
        // Justo al despegar, Unity todavia avisa un instante que toca el piso. Si un
        // doble toque muy rapido cae en ese instante, saldria otro salto normal en vez
        // del supersalto. Durante 0,1 s no se le cree al piso.
        ignorarPisoHasta = Time.time + 0.1f;

        alturaObjetivo = AlturaSalto;
        CambiarFase(FaseSalto.Despegue);
        // si el Animator todavia no tiene los parametros nuevos, usa el trigger viejo
        if (animator != null && !animatorTieneSalto) animator.SetTrigger("salto");

        if (mostrarDebug) Debug.Log("[Salto] " + alturaSaltoPx + "px");
    }

    // le da la velocidad justa para que llegue a esa altura y no mas
    private void ImpulsarHasta(float alturaEnUnidades)
    {
        float gravedad = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (gravedad <= 0f || alturaEnUnidades <= 0f) return;

        // La fisica avanza en pasos, y en cada salto pierde medio paso de subida
        // (con la formula comun llegaba a 115 px en vez de 120). Esto lo compensa.
        float medioPaso = gravedad * Time.fixedDeltaTime * 0.5f;
        float impulso = medioPaso + Mathf.Sqrt(medioPaso * medioPaso + 2f * gravedad * alturaEnUnidades);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, impulso);
    }

    private void CambiarFase(FaseSalto nueva)
    {
        FaseSalto anterior = fase;
        fase = nueva;
        inicioFase = Time.time;
        float altura = rb.position.y - alturaDespegue;

        if (nueva == FaseSalto.Despegue)
        {
            progresoSubida = 0f;
            alturaInicioSubida = 0f;
        }
        // durante la agachada ya subio un poco: la subida se cuenta desde ahi, asi se ve el 007
        if (nueva == FaseSalto.Subida && anterior == FaseSalto.Despegue) alturaInicioSubida = altura;
        if (nueva == FaseSalto.Caida) alturaPunta = altura;
    }

    // La animacion del salto no corre sola: el frame sale de la altura real.
    // Cerca de la punta la altura cambia despacio, asi que esos frames duran mas.
    private void ActualizarAnimacionSalto()
    {
        float vy = rb.linearVelocity.y;

        // 1. cambios de fase
        switch (fase)
        {
            case FaseSalto.Ninguna:
                // se cayo de un borde, o se le rompio la piedra, sin saltar
                if (!enPiso && !dashActivo && vy < -0.5f && Time.time - ultimaVezEnPiso > tiempoCoyote)
                {
                    alturaDespegue = rb.position.y;
                    alturaObjetivo = AlturaSalto;
                    CambiarFase(FaseSalto.Caida);
                }
                break;

            case FaseSalto.Despegue:
                if (Time.time - inicioFase >= duracionDespegue)
                    CambiarFase(vy > 0f ? FaseSalto.Subida : FaseSalto.Caida);
                break;

            case FaseSalto.Subida:
                if (enPiso) CambiarFase(FaseSalto.Aterrizaje);
                else if (vy <= 0f) CambiarFase(FaseSalto.Caida);
                break;

            case FaseSalto.Caida:
                if (enPiso) CambiarFase(FaseSalto.Aterrizaje);
                break;

            case FaseSalto.Aterrizaje:
                if (Time.time - inicioFase >= duracionAterrizaje) CambiarFase(FaseSalto.Ninguna);
                break;
        }

        // 2. que frame mostrar
        float altura = rb.position.y - alturaDespegue;
        float frame = 1f;
        switch (fase)
        {
            case FaseSalto.Despegue:
                frame = Tramo(FrameDespegue, FrameSubida, (Time.time - inicioFase) / Mathf.Max(0.01f, duracionDespegue));
                break;

            case FaseSalto.Subida:
                // nunca retrocede: si el supersalto sube el objetivo, el frame espera a que la altura lo alcance
                float tramoSubida = Mathf.Max(0.01f, alturaObjetivo - alturaInicioSubida);
                progresoSubida = Mathf.Max(progresoSubida, (altura - alturaInicioSubida) / tramoSubida);
                frame = progresoSubida < 1f - ZonaPunta
                    ? Tramo(FrameSubida, FramePuntaSube, progresoSubida / (1f - ZonaPunta))
                    : FramePuntaSube;
                break;

            case FaseSalto.Caida:
                // la bajada se reparte sobre lo que realmente tiene que caer (por ejemplo,
                // despues de un dash a media altura), como minimo lo de un salto normal
                float caidaEsperada = Mathf.Max(alturaPunta, AlturaSalto);
                float bajada = (alturaPunta - altura) / Mathf.Max(0.01f, caidaEsperada);
                frame = bajada < ZonaPunta
                    ? FramePuntaBaja
                    : Tramo(FrameBajada, FrameAterrizaje, (bajada - ZonaPunta) / (1f - ZonaPunta));
                break;

            case FaseSalto.Aterrizaje:
                frame = Tramo(FrameAterrizaje, FramesSalto + 1f, (Time.time - inicioFase) / Mathf.Max(0.01f, duracionAterrizaje));
                break;
        }

        if (animator == null || !animatorTieneSalto) return;
        bool saltando = fase != FaseSalto.Ninguna;
        animator.SetBool("saltando", saltando);
        // al terminar se deja el ultimo frame, asi no asoma el 001 antes de volver a Idle
        if (saltando)
            animator.SetFloat("tiempoSalto", (Mathf.Floor(frame) - 1f + 0.5f) / FramesSalto);
    }

    // un frame entre "desde" y "hasta" (sin llegar a "hasta")
    private static float Tramo(float desde, float hasta, float t)
    {
        return Mathf.Min(Mathf.Lerp(desde, hasta, t), hasta - 0.01f);
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
        enPiso = pisoDetectado && Time.time >= ignorarPisoHasta;
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
