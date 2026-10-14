using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Color que toma un instante al recibir un golpe.")]
    [SerializeField] private Color colorGolpe = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float duracionDestello = 0.12f;

    [Header("Muerte")]
    [Tooltip("Frames de la animacion que queda en el lugar al morir (muerte_mob).")]
    [SerializeField] private Sprite[] framesMuerte;
    [SerializeField] private float fpsMuerte = 14f;
    [Tooltip("Activado (mobs): la animacion se apoya en la base del mob y se escala segun su ancho. " +
             "Desactivado (Umbrae): se dibuja en el lugar del mob, a su tamano y mirando para el mismo lado.")]
    [SerializeField] private bool ajustarMuerteAlTamano = true;

    private float vida;
    private SpriteRenderer dibujo;
    private Color colorOriginal = Color.white;
    private float finDestello = -1f;

    public float Vida => vida;
    public bool Vivo => vida > 0f;

    // Avisa justo antes de desactivarse al morir (por ejemplo, para la muerte especial de Umbrae).
    public event System.Action AlMorir;

    private void Awake()
    {
        vida = vidaMaxima;
        dibujo = GetComponent<SpriteRenderer>();
        if (dibujo != null) colorOriginal = dibujo.color;
    }

    // Vuelve a la vida maxima (por ejemplo, al reiniciar la pelea con Umbrae).
    public void Reiniciar()
    {
        vida = vidaMaxima;
        finDestello = -1f;
        if (dibujo != null) dibujo.color = colorOriginal;
    }

    public void RecibirDanio(float cantidad)
    {
        if (!Vivo) return;

        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log("[Enemigo] " + name + " -" + cantidad + " -> " + vida + "/" + vidaMaxima);

        if (!Vivo)
        {
            CrearEfectoMuerte();
            AlMorir?.Invoke();
            gameObject.SetActive(false);
            return;
        }

        if (dibujo != null)
        {
            dibujo.color = colorGolpe;
            finDestello = Time.time + duracionDestello;
        }
    }

    private void Update()
    {
        if (finDestello >= 0f && Time.time >= finDestello)
        {
            dibujo.color = colorOriginal;
            finDestello = -1f;
        }
    }

    // La animacion se apoya en la base del mob y se escala segun su ancho (el pajaro es mas chico
    // que el comelibros). Es un objeto aparte porque el mob se desactiva enseguida.
    private void CrearEfectoMuerte()
    {
        if (framesMuerte == null || framesMuerte.Length == 0) return;

        var col = GetComponent<Collider2D>();
        Bounds b = col != null ? col.bounds : (dibujo != null ? dibujo.bounds : new Bounds(transform.position, Vector3.one));
        float anchoDibujo = framesMuerte[framesMuerte.Length / 2].bounds.size.x;
        float escala = Mathf.Clamp(b.size.x * 1.1f / Mathf.Max(0.01f, anchoDibujo), 0.4f, 2f);

        var go = new GameObject("Muerte " + name);
        if (ajustarMuerteAlTamano)
        {
            go.transform.position = new Vector3(b.center.x, b.min.y, transform.position.z);
            go.transform.localScale = Vector3.one * escala;
        }
        else
        {
            go.transform.position = transform.position;
            go.transform.localScale = new Vector3(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), 1f);
        }
        var sr = go.AddComponent<SpriteRenderer>();
        if (dibujo != null)
        {
            sr.flipX = dibujo.flipX;
            sr.sharedMaterial = dibujo.sharedMaterial;
            sr.sortingLayerID = dibujo.sortingLayerID;
            sr.sortingOrder = dibujo.sortingOrder + 1;
        }
        go.AddComponent<EfectoMuerte>().Iniciar(framesMuerte, fpsMuerte);
    }
}
