using UnityEngine;

// La bola que lanza AtaqueSabiduria: sale despacio, acelera y gira cada vez mas rapido,
// con una estela desenfocada detras (como la del dash) y soltando flores por el camino.
// Le pega al primer enemigo que toca o se frena contra una pared/piso. Se crea y se destruye sola.
public class ProyectilSabiduria : MonoBehaviour
{
    private Vector2 direccion;
    private float velocidad, velocidadMaxima, aceleracion, alcance, danio, giroMaximo, radio, distanciaEntreFlores;
    private Collider2D ignorar;
    private Transform bola;
    private SpriteRenderer estela;
    private ParticleSystem flores;
    private float recorrido;
    private float proximaFlor;
    private bool termino;
    private float finTiempo;

    private const float DuracionFinal = 0.12f;

    public void Iniciar(Vector2 direccion, float velocidadInicial, float velocidadMaxima, float aceleracion, float alcance,
                        float danio, float giroMaximo, float radio, Collider2D ignorar, Transform bola,
                        SpriteRenderer estela, ParticleSystem flores, float distanciaEntreFlores)
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
        this.estela = estela;
        this.flores = flores;
        this.distanciaEntreFlores = Mathf.Max(0.05f, distanciaEntreFlores);

        // la estela no gira con la bola: apunta siempre para atras del movimiento
        if (estela != null)
            estela.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(this.direccion.y, this.direccion.x) * Mathf.Rad2Deg);
        ActualizarEstela();
    }

    private void Update()
    {
        if (termino)
        {
            Terminar();
            return;
        }

        velocidad = Mathf.MoveTowards(velocidad, velocidadMaxima, aceleracion * Time.deltaTime);
        float paso = velocidad * Time.deltaTime;
        bool choco = Avanzar(paso);

        float rapidez = velocidadMaxima > 0f ? velocidad / velocidadMaxima : 1f;
        if (bola != null) bola.Rotate(0f, 0f, -Mathf.Sign(direccion.x) * giroMaximo * Mathf.Lerp(0.3f, 1f, rapidez) * Time.deltaTime);
        ActualizarEstela();
        SoltarFlores();

        if (choco || recorrido >= alcance) Frenar();
    }

    // Revisa todo el tramo que recorre en este cuadro (no solo el punto de llegada): a mucha
    // velocidad el paso es mas largo que la bola y podria atravesar un enemigo flaco o una pared.
    // Avanza hasta el primer choque y devuelve si choco.
    private bool Avanzar(float paso)
    {
        Vector2 desde = transform.position;
        var choques = Physics2D.CircleCastAll(desde, radio, direccion, paso);
        System.Array.Sort(choques, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var choque in choques)
        {
            var col = choque.collider;
            if (col == ignorar || col.GetComponentInParent<SaludPersonaje>() != null) continue;

            var enemigo = col.GetComponentInParent<VidaEnemigo>();
            if (enemigo != null)
            {
                if (!enemigo.Vivo) continue;
                IrHasta(desde, choque.distance);
                enemigo.RecibirDanio(danio);
                return true;
            }
            // paredes, piso, pinchos, plataformas (los triggers como checkpoints no la frenan)
            if (!col.isTrigger)
            {
                IrHasta(desde, choque.distance);
                return true;
            }
        }

        IrHasta(desde, paso);
        return false;
    }

    private void IrHasta(Vector2 desde, float distancia)
    {
        transform.position = desde + direccion * distancia;
        recorrido += distancia;
    }

    // la estela se alarga y se hace mas visible a medida que la bola toma velocidad
    private void ActualizarEstela()
    {
        if (estela == null) return;
        float rapidez = velocidadMaxima > 0f ? Mathf.Clamp01(velocidad / velocidadMaxima) : 1f;
        estela.transform.localScale = new Vector3(Mathf.Lerp(0.25f, 1f, rapidez), 1f, 1f);
        var c = estela.color;
        c.a = Mathf.Lerp(0.15f, 0.85f, rapidez);
        estela.color = c;
    }

    private void SoltarFlores()
    {
        if (flores == null) return;
        while (recorrido >= proximaFlor)
        {
            proximaFlor += distanciaEntreFlores;
            Vector2 costado = new Vector2(-direccion.y, direccion.x);
            var p = new ParticleSystem.EmitParams
            {
                // detras de la bola, un poco al azar a los costados
                position = (Vector2)transform.position - direccion * Random.Range(0.1f, 0.35f) + costado * Random.Range(-0.22f, 0.22f),
                velocity = -direccion * Random.Range(0.2f, 0.9f) + costado * Random.Range(-0.5f, 0.5f) + Vector2.up * 0.15f,
                applyShapeToPosition = false
            };
            flores.Emit(p, 1);
        }
    }

    private void Frenar()
    {
        termino = true;
        finTiempo = Time.time;
    }

    // al chocar se achica rapido y desaparece (las flores que ya solto siguen flotando)
    private void Terminar()
    {
        float k = 1f - (Time.time - finTiempo) / DuracionFinal;
        if (k <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        if (bola != null) bola.localScale = Vector3.one * k;
        if (estela != null)
        {
            var c = estela.color;
            c.a = Mathf.Min(c.a, k * 0.85f);
            estela.color = c;
        }
    }
}
