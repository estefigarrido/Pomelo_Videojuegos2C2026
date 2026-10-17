using UnityEngine;
using UnityEngine.InputSystem;

public class SaludPersonaje : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Donde reaparece al morir. Si queda vacio, reaparece donde empezo el nivel.")]
    [SerializeField] private Transform puntoDeReaparicion;

    [Header("Sabiduria (el liquido del circulo del HUD)")]
    [Tooltip("Liquido que entra en el circulo. Se junta pegandole a los enemigos: cada golpe suma lo que ese " +
             "enemigo le saca a ella (sabiduriaPorGolpe en VidaEnemigo).")]
    [SerializeField] private float sabiduriaMaxima = 100f;

    [Header("Regeneracion (gasta sabiduria)")]
    [Tooltip("Tecla provisoria para regenerar una vida (la definitiva se elige despues).")]
    [SerializeField] private Key teclaRegenerar = Key.R;
    [Tooltip("Vida que recupera cada vez (20 = una de las 5 vidas del HUD).")]
    [SerializeField] private float regeneracion = 20f;
    [Tooltip("Sabiduria que gasta cada vez (20 = 20% del circulo).")]
    [SerializeField] private float costoRegeneracion = 20f;

    [Header("Parpadeo al recibir un golpe")]
    [SerializeField] private ParpadeoDanio parpadeo = new ParpadeoDanio();

    private float vida;
    private float sabiduria;
    private Vector3 puntoGuardado;
    private Rigidbody2D rb;

    public float Vida => vida;
    public float VidaMaxima => vidaMaxima;
    public float Sabiduria => sabiduria;
    public float SabiduriaMaxima => sabiduriaMaxima;

    // Avisa cuando muere, antes de reaparecer (la pelea con Umbrae lo usa).
    public event System.Action AlMorir;
    private Vector3? reaparicionUnaVez;

    // Solo para la proxima muerte: reaparece aca en vez del ultimo checkpoint.
    public void ReaparecerUnaVezEn(Vector3 posicion) => reaparicionUnaVez = posicion;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        puntoGuardado = transform.position;
        vida = vidaMaxima;
        parpadeo.Iniciar(GetComponent<SpriteRenderer>());
    }

    private void OnEnable() => VidaEnemigo.Golpeado += SumarSabiduria;
    private void OnDisable() => VidaEnemigo.Golpeado -= SumarSabiduria;

    private void Update()
    {
        parpadeo.Actualizar();

        var teclado = Keyboard.current;
        if (teclado != null && teclado[teclaRegenerar].wasPressedThisFrame) Regenerar();
    }

    public void SumarSabiduria(float cantidad)
    {
        if (cantidad <= 0f || sabiduria >= sabiduriaMaxima) return;

        sabiduria = Mathf.Min(sabiduriaMaxima, sabiduria + cantidad);
        Debug.Log("[Sabiduria] +" + cantidad + " -> " + sabiduria + "/" + sabiduriaMaxima);
    }

    // Una vida por vez, y solo si le falta vida y le alcanza la sabiduria.
    private void Regenerar()
    {
        if (vida <= 0f || vida >= vidaMaxima || sabiduria < costoRegeneracion) return;

        sabiduria -= costoRegeneracion;
        vida = Mathf.Min(vidaMaxima, vida + regeneracion);
        Debug.Log("[Vida] +" + regeneracion + " (gasta " + costoRegeneracion + " de sabiduria) -> " + vida + "/" + vidaMaxima);
    }

    public void RecibirDanio(float cantidad)
    {
        if (vida <= 0f) return;

        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log("[Vida] -" + cantidad + " -> " + vida + "/" + vidaMaxima);

        // solo cambia el color: no frena el movimiento ni los ataques
        parpadeo.Empezar();

        if (vida <= 0f) Morir();
    }

    public void DefinirPuntoDeReaparicion(Transform punto)
    {
        puntoDeReaparicion = punto;
    }

    // guarda una posicion fija (el checkpoint se mueve y despues desaparece)
    public void DefinirPuntoDeReaparicion(Vector3 posicion)
    {
        puntoDeReaparicion = null;
        puntoGuardado = posicion;
        Debug.Log("[Vida] nuevo punto de reaparicion: " + posicion);
    }

    private void Morir()
    {
        AlMorir?.Invoke();
        Vector3 destino = reaparicionUnaVez ?? (puntoDeReaparicion != null ? puntoDeReaparicion.position : puntoGuardado);
        reaparicionUnaVez = null;
        Debug.Log("[Vida] murio, reaparece en " + destino);

        transform.position = destino;
        if (rb != null)
        {
            rb.position = destino;
            rb.linearVelocity = Vector2.zero;
        }
        vida = vidaMaxima;
    }
}
