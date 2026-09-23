using UnityEngine;

public class SaludPersonaje : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Donde reaparece al morir. Si queda vacio, reaparece donde empezo el nivel.")]
    [SerializeField] private Transform puntoDeReaparicion;

    private float vida;
    private Vector3 posicionInicial;
    private Rigidbody2D rb;

    public float Vida => vida;
    public float VidaMaxima => vidaMaxima;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        posicionInicial = transform.position;
        vida = vidaMaxima;
    }

    public void RecibirDanio(float cantidad)
    {
        if (vida <= 0f) return;

        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log("[Vida] -" + cantidad + " -> " + vida + "/" + vidaMaxima);

        if (vida <= 0f) Morir();
    }

    public void DefinirPuntoDeReaparicion(Transform punto)
    {
        puntoDeReaparicion = punto;
    }

    private void Morir()
    {
        Vector3 destino = puntoDeReaparicion != null ? puntoDeReaparicion.position : posicionInicial;
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
