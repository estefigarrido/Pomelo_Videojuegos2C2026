using UnityEngine;

// La bola de ignorancia que tira Umbrae: igual que la bola de sabiduria de la chica (sale despacio,
// acelera, gira y lleva una estela desenfocada detras), pero negra, animada (bola_de_ignorancia)
// y le pega a la chica. Atraviesa a los mobs y se frena contra paredes y piso.
// Con el latigo se repele (AtaqueLatigo llama a Repeler): sale despedida para el otro lado y se rompe en pedazos.
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

    // las bolas que estan volando (el latigo las busca aca: no tienen collider)
    public static readonly System.Collections.Generic.List<ProyectilIgnorancia> Activas = new System.Collections.Generic.List<ProyectilIgnorancia>();
    public float Radio => radio;
    private bool repelida;

    private void OnEnable() => Activas.Add(this);
    private void OnDisable() => Activas.Remove(this);

    // el latigo la golpea: sale despedida lejos de la chica, ya no lastima y a los 0.15 s se rompe en pedazos
    public void Repeler(Vector2 desde)
    {
        if (repelida || termino) return;
        repelida = true;
        Vector2 lejos = (Vector2)transform.position - desde;
        direccion = lejos.sqrMagnitude > 0.0001f ? lejos.normalized : -direccion;
        velocidad = Mathf.Max(velocidad, velocidadMaxima) * 1.3f;
        finTiempo = Time.time + 0.15f;
        if (estela != null) estela.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg);
    }

    // la bola se parte en pedacitos que salen volando y se apagan
    private void Romper()
    {
        if (bola == null || bola.sprite == null) return;
        for (int i = 0; i < 7; i++)
        {
            var go = new GameObject("pedazo bola ignorancia");
            go.transform.position = bola.transform.position;
            go.transform.localScale = bola.transform.lossyScale * Random.Range(0.22f, 0.38f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = bola.sprite; sr.sharedMaterial = bola.sharedMaterial;
            sr.sortingLayerID = bola.sortingLayerID; sr.sortingOrder = bola.sortingOrder;
            float ang = (i / 7f) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            var pedazo = go.AddComponent<PedazoQueVuela>();
            pedazo.Lanzar(new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(2.5f, 5f) + direccion * 2f, 0.45f);
        }
    }

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

        // repelida: vuela para el otro lado sin lastimar y se rompe
        if (repelida)
        {
            transform.position += (Vector3)(direccion * velocidad * Time.deltaTime);
            ActualizarEstela();
            if (Time.time >= finTiempo) { Romper(); Destroy(gameObject); }
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
