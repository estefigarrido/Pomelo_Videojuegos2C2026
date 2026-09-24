using UnityEngine;

// La bola de ignorancia que tira Umbrae: igual que la bola de sabiduria de la chica (sale despacio,
// acelera, gira y lleva una estela desenfocada detras), pero negra, animada (bola_de_ignorancia)
// y le pega a la chica. Atraviesa a los mobs y se frena contra paredes y piso.
public class ProyectilIgnorancia : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad, velocidadMaxima, aceleracion, alcance, danio, giroMaximo, radio, fps;
    private Collider2D ignorar;
    private SpriteRenderer bola;
    private Sprite[] frames;
    private SpriteRenderer estela;
    private float recorrido;
    private float inicio;
    private bool termino;
    private float finTiempo;

    private const float DuracionFinal = 0.12f;

    public void Iniciar(Vector2 direccion, float velocidadInicial, float velocidadMaxima, float aceleracion, float alcance,
                        float danio, float giroMaximo, float radio, Collider2D ignorar, SpriteRenderer bola,
                        Sprite[] frames, float fps, SpriteRenderer estela)
    {
        this.direccion = direccion.normalized;
        velocidad = velocidadInicial;
        this.velocidadMaxima = velocidadMaxima;
        this.aceleracion = aceleracion;
        this.alcance = alcance;
        this.danio = danio;
        this.giroMaximo = giroMaximo;
        this.radio = radio;
        this.ignorar = ignorar;
        this.bola = bola;
        this.frames = frames;
        this.fps = Mathf.Max(1f, fps);
        this.estela = estela;
        inicio = Time.time;

        // la estela no gira con la bola: apunta siempre para atras del movimiento
        if (estela != null)
            estela.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(this.direccion.y, this.direccion.x) * Mathf.Rad2Deg);
        ActualizarEstela();
    }

    private void Update()
    {
        if (bola != null && frames != null && frames.Length > 0)
            bola.sprite = frames[Mathf.FloorToInt((Time.time - inicio) * fps) % frames.Length];

        if (termino)
        {
            Terminar();
            return;
        }

        velocidad = Mathf.MoveTowards(velocidad, velocidadMaxima, aceleracion * Time.deltaTime);
        float paso = velocidad * Time.deltaTime;
        transform.position += (Vector3)(direccion * paso);
        recorrido += paso;

        float rapidez = velocidadMaxima > 0f ? velocidad / velocidadMaxima : 1f;
        if (bola != null) bola.transform.Rotate(0f, 0f, -Mathf.Sign(direccion.x) * giroMaximo * Mathf.Lerp(0.3f, 1f, rapidez) * Time.deltaTime);
        ActualizarEstela();

        foreach (var col in Physics2D.OverlapCircleAll(transform.position, radio))
        {
            if (col == ignorar) continue;

            var salud = col.GetComponentInParent<SaludPersonaje>();
            if (salud != null)
            {
                salud.RecibirDanio(danio);
                Frenar();
                return;
            }
            // los mobs y los triggers (checkpoints, zonas) no la frenan
            if (col.isTrigger || col.GetComponentInParent<VidaEnemigo>() != null || col.GetComponentInParent<UmbraeBoss>() != null) continue;
            Frenar();
            return;
        }

        if (recorrido >= alcance) Frenar();
    }

    // la estela se alarga y se hace mas visible a medida que la bola toma velocidad
    private void ActualizarEstela()
    {
        if (estela == null) return;
        float rapidez = velocidadMaxima > 0f ? Mathf.Clamp01(velocidad / velocidadMaxima) : 1f;
        estela.transform.localScale = new Vector3(Mathf.Lerp(0.25f, 1f, rapidez), 1f, 1f);
        var c = estela.color;
        c.a = Mathf.Lerp(0.15f, 0.8f, rapidez);
        estela.color = c;
    }

    private void Frenar()
    {
        termino = true;
        finTiempo = Time.time;
    }

    // al chocar se achica rapido y desaparece
    private void Terminar()
    {
        float k = 1f - (Time.time - finTiempo) / DuracionFinal;
        if (k <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        if (bola != null) bola.transform.localScale = Vector3.one * k;
        if (estela != null)
        {
            var c = estela.color;
            c.a = Mathf.Min(c.a, k * 0.8f);
            estela.color = c;
        }
    }
}
