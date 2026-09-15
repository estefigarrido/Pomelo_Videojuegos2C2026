using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;

    [Header("Sprint")]
    [SerializeField] private float multiplicadorSprint = 1.5f;
    [SerializeField] private float duracionSprint = 8f;
    [SerializeField] private float cooldownSprint = 3f;

    [Header("Deteccion del doble toque")]
    [Tooltip("Tiempo maximo que puede durar una pulsacion para contar como toque y no como mantenido.")]
    [SerializeField] private float duracionMaximaToque = 0.3f;
    [Tooltip("Tiempo maximo entre que soltas la tecla y la volves a presionar.")]
    [SerializeField] private float ventanaEntreToques = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    private Rigidbody2D rb;
    private Animator animator;

    private float direccion;
    private bool sprintActivo;
    private float finSprint;
    private float finCooldown;

    private float inicioPulsacion = -1f;
    private float momentoUltimoToque = -99f;

    public bool SprintActivo => sprintActivo;
    public float SegundosRestantesSprint => Mathf.Max(0f, finSprint - Time.time);
    public float SegundosRestantesCooldown => Mathf.Max(0f, finCooldown - Time.time);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        // el input se lee aca, en el frame exacto en que pasa
        direccion = 0f;
        if (teclado.aKey.isPressed) direccion -= 1f;
        if (teclado.dKey.isPressed) direccion += 1f;

        DetectarDobleToque(teclado);

        if (sprintActivo && Time.time >= finSprint)
        {
            sprintActivo = false;
            finCooldown = Time.time + cooldownSprint;
            if (mostrarDebug) Debug.Log("[Sprint] termino. Cooldown por " + cooldownSprint + "s");
        }

        // si esta quieta corta la animacion de correr al instante
        if (animator != null) animator.SetBool("sprint", sprintActivo && direccion != 0f);

        if (direccion != 0f)
        {
            Vector3 escala = transform.localScale;
            escala.x = Mathf.Abs(escala.x) * (direccion < 0f ? -1f : 1f);
            transform.localScale = escala;
        }
    }

    private void DetectarDobleToque(Keyboard teclado)
    {
        if (teclado.dKey.wasPressedThisFrame)
        {
            bool segundoToqueATiempo = Time.time - momentoUltimoToque <= ventanaEntreToques;

            if (segundoToqueATiempo && !sprintActivo && Time.time >= finCooldown)
            {
                sprintActivo = true;
                finSprint = Time.time + duracionSprint;
                momentoUltimoToque = -99f;
                inicioPulsacion = -1f;
                if (mostrarDebug) Debug.Log("[Sprint] ACTIVADO por " + duracionSprint + "s");
                return;
            }

            inicioPulsacion = Time.time;
        }

        if (teclado.dKey.wasReleasedThisFrame && inicioPulsacion >= 0f)
        {
            float duracion = Time.time - inicioPulsacion;
            inicioPulsacion = -1f;

            // solo una pulsacion corta cuenta como "toque"; mantener apretado no
            momentoUltimoToque = duracion <= duracionMaximaToque ? Time.time : -99f;

            if (mostrarDebug)
                Debug.Log("[Sprint] pulsacion de " + duracion.ToString("F2") + "s -> " +
                          (duracion <= duracionMaximaToque ? "cuenta como toque" : "fue mantenida, no cuenta"));
        }
    }

    private void FixedUpdate()
    {
        float velocidadActual = sprintActivo ? velocidad * multiplicadorSprint : velocidad;
        rb.linearVelocity = new Vector2(direccion * velocidadActual, rb.linearVelocity.y);
    }
}
