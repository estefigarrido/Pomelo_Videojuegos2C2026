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

    [Header("Destello al recibir un golpe")]
    [Tooltip("Color que toma un instante al recibir un golpe.")]
    [SerializeField] private Color colorGolpe = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float duracionDestello = 0.12f;

    private float vida;
    private float proximaRegeneracion;
    private SpriteRenderer dibujo;
    private Color colorOriginal = Color.white;
    private float finDestello = -1f;
    private Vector3 puntoGuardado;
    private Rigidbody2D rb;

    public float Vida => vida;
    public float VidaMaxima => vidaMaxima;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        puntoGuardado = transform.position;
        vida = vidaMaxima;
        dibujo = GetComponent<SpriteRenderer>();
        if (dibujo != null) colorOriginal = dibujo.color;
    }

    private void Update()
    {
        if (finDestello >= 0f && Time.time >= finDestello)
        {
            dibujo.color = colorOriginal;
            finDestello = -1f;
        }

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

        if (dibujo != null)
        {
            dibujo.color = colorGolpe;
            finDestello = Time.time + duracionDestello;
        }

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
        Vector3 destino = puntoDeReaparicion != null ? puntoDeReaparicion.position : puntoGuardado;
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
