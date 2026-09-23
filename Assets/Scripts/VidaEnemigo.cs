using UnityEngine;

public class VidaEnemigo : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Color que toma un instante al recibir un golpe.")]
    [SerializeField] private Color colorGolpe = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float duracionDestello = 0.12f;

    private float vida;
    private SpriteRenderer dibujo;
    private Color colorOriginal = Color.white;
    private float finDestello = -1f;

    public float Vida => vida;
    public bool Vivo => vida > 0f;

    private void Awake()
    {
        vida = vidaMaxima;
        dibujo = GetComponent<SpriteRenderer>();
        if (dibujo != null) colorOriginal = dibujo.color;
    }

    public void RecibirDanio(float cantidad)
    {
        if (!Vivo) return;

        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log("[Enemigo] " + name + " -" + cantidad + " -> " + vida + "/" + vidaMaxima);

        if (!Vivo)
        {
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
}
