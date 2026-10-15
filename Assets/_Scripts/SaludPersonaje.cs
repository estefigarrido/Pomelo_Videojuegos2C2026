using UnityEngine;

public class SaludPersonaje : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Donde reaparece al morir. Si queda vacio, reaparece donde empezo el nivel.")]
    [SerializeField] private Transform puntoDeReaparicion;

    [Header("Regeneracion")]
    [Tooltip("Vida que recupera en cada tic (20 = 20% de la vida maxima).")]
    [SerializeField] private float regeneracion = 20f;
    [Tooltip("Segundos entre cada tic. Cada golpe nuevo reinicia la cuenta.")]
    [SerializeField] private float intervaloRegeneracion = 5f;

    [Header("Parpadeo al recibir un golpe")]
    [SerializeField] private ParpadeoDanio parpadeo = new ParpadeoDanio();

    private float vida;
    private float proximaRegeneracion;
    private Vector3 puntoGuardado;
    private Rigidbody2D rb;

    public float Vida => vida;
    public float VidaMaxima => vidaMaxima;

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

    private void Update()
    {
        parpadeo.Actualizar();

        if (vida <= 0f || vida >= vidaMaxima || Time.time < proximaRegeneracion) return;

        vida = Mathf.Min(vidaMaxima, vida + regeneracion);
        proximaRegeneracion = Time.time + intervaloRegeneracion;
        Debug.Log("[Vida] +" + regeneracion + " -> " + vida + "/" + vidaMaxima);
    }

    public void RecibirDanio(float cantidad)
    {
        if (vida <= 0f) return;

        vida = Mathf.Max(0f, vida - cantidad);
        proximaRegeneracion = Time.time + intervaloRegeneracion;
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
