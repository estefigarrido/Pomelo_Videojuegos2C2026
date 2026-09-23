using UnityEngine;

// La bola que lanza AtaqueSabiduria: vuela derecho girando, le pega al primer enemigo que toca
// (o se frena contra una pared/piso) y termina con un destello. Se crea y se destruye sola.
public class ProyectilSabiduria : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad, alcance, danio, giro, radio, fpsDestello;
    private Collider2D ignorar;
    private Sprite[] destellos;
    private SpriteRenderer dibujo;
    private float recorrido;
    private bool impacto;
    private float inicioDestello;

    public void Iniciar(Vector2 direccion, float velocidad, float alcance, float danio, float giro, float radio,
                        Collider2D ignorar, Sprite[] destellos, float fpsDestello)
    {
        this.direccion = direccion.normalized;
        this.velocidad = velocidad;
        this.alcance = alcance;
        this.danio = danio;
        this.giro = giro;
        this.radio = radio;
        this.ignorar = ignorar;
        this.destellos = destellos;
        this.fpsDestello = fpsDestello;
        dibujo = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (impacto)
        {
            AnimarDestello();
            return;
        }

        float paso = velocidad * Time.deltaTime;
        transform.position += (Vector3)(direccion * paso);
        transform.Rotate(0f, 0f, -giro * Mathf.Sign(direccion.x) * Time.deltaTime);
        recorrido += paso;

        foreach (var col in Physics2D.OverlapCircleAll(transform.position, radio))
        {
            if (col == ignorar || col.GetComponentInParent<SaludPersonaje>() != null) continue;

            var enemigo = col.GetComponentInParent<VidaEnemigo>();
            if (enemigo != null)
            {
                if (!enemigo.Vivo) continue;
                enemigo.RecibirDanio(danio);
                Impactar();
                return;
            }
            // paredes, piso, pinchos, plataformas (los triggers como checkpoints no la frenan)
            if (!col.isTrigger)
            {
                Impactar();
                return;
            }
        }

        if (recorrido >= alcance) Impactar();
    }

    private void Impactar()
    {
        impacto = true;
        inicioDestello = Time.time;
        transform.rotation = Quaternion.identity;
        if (destellos == null || destellos.Length == 0) Destroy(gameObject);
    }

    private void AnimarDestello()
    {
        float t = (Time.time - inicioDestello) * fpsDestello;
        int i = Mathf.FloorToInt(t);
        if (i < destellos.Length)
        {
            dibujo.sprite = destellos[i];
            return;
        }
        // despues del ultimo frame se desvanece rapido
        float a = 1f - (t - destellos.Length) / (fpsDestello * 0.15f);
        if (a <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        var c = dibujo.color;
        c.a = a;
        dibujo.color = c;
    }
}
