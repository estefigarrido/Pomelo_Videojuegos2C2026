using UnityEngine;
using UnityEngine.InputSystem;

// Segundo golpe: la chica hace el gesto de "chica_golpe2" y lanza una bola de sabiduria.
// Igual que el latigo, es independiente del movimiento y del Animator: mientras dura,
// dibuja sus propios frames en LateUpdate (despues del Animator).
// Durante el gesto salen petalos y hojas (flores_destello) en la zona que recorre el brazo,
// moviendose para el mismo lado que el brazo.
[RequireComponent(typeof(SpriteRenderer))]
public class AtaqueSabiduria : MonoBehaviour
{
    [System.Serializable]
    public struct ZonaBrazo
    {
        [Tooltip("Poligono (convexo) de la zona que recorre el brazo, en unidades locales mirando a la derecha.")]
        public Vector2[] puntos;
    }

    [Header("Control")]
    [SerializeField] private Key tecla = Key.E;
    [Tooltip("Segundos de espera desde que termina un golpe hasta poder tirar otro.")]
    [SerializeField] private float espera = 0.3f;

    [Header("Animacion (chica_golpe2)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 24f;
    [Tooltip("Frame (empieza en 1) en el que sale la bola.")]
    [SerializeField] private int frameLanzamiento = 15;

    [Header("Particulas del brazo")]
    [Tooltip("Sistema de particulas de flores (prefab). Lo usan el brazo y la estela de la bola.")]
    [SerializeField] private ParticleSystem floresPrefab;
    [Tooltip("Una zona por pose.")]
    [SerializeField] private ZonaBrazo[] zonas;
    [Tooltip("Para cada frame de la animacion, que zona usa (indice en 'zonas').")]
    [SerializeField] private int[] zonaDeFrame;
    [Tooltip("Particulas por segundo mientras dura el gesto.")]
    [SerializeField] private float particulasPorSegundo = 55f;
    [Tooltip("Particulas extra cada vez que cambia la pose.")]
    [SerializeField] private int particulasPorPose = 5;
    [Tooltip("Velocidad con la que las particulas siguen el movimiento del brazo.")]
    [SerializeField] private float velocidadParticulas = 1.4f;

    [Header("Bola de sabiduria")]
    [SerializeField] private Sprite bola;
    [Tooltip("Estela desenfocada de la bola (como la del dash).")]
    [SerializeField] private Sprite estelaBola;
    [SerializeField] private Material materialBrillo;
    [Tooltip("De donde sale, relativo al personaje mirando a la derecha (la mano de adelante).")]
    [SerializeField] private Vector2 salida = new Vector2(0.75f, 0.3f);
    [Tooltip("Velocidad al soltarla; despues acelera hasta la maxima.")]
    [SerializeField] private float velocidadInicial = 2.5f;
    [SerializeField] private float velocidadMaxima = 16f;
    [Tooltip("Unidades por segundo que gana de velocidad por cada segundo.")]
    [SerializeField] private float aceleracion = 38f;
    [SerializeField] private float alcance = 14f;
    [Tooltip("Vida que saca a un enemigo (la vida maxima de los mobs es 100).")]
    [SerializeField] private float danio = 25f;
    [Tooltip("Grados por segundo que gira cuando va a velocidad maxima.")]
    [SerializeField] private float giroMaximo = 720f;
    [SerializeField] private float radio = 0.3f;
    [Tooltip("Si hay un enemigo adelante dentro de este angulo (grados), la bola va hacia el; si no, sale derecha.")]
    [SerializeField] private float anguloPunteria = 30f;
    [Tooltip("Cada cuantas unidades recorridas suelta una flor.")]
    [SerializeField] private float distanciaEntreFlores = 0.2f;

    private SpriteRenderer dibujo;
    private Collider2D colliderPropio;
    private AtaqueLatigo latigo;
    private ParticleSystem flores;
    private bool atacando;
    private bool lanzo;
    private float inicio;
    private float finUltimo = -99f;
    private int zonaAnterior = -1;
    private Vector2 movimientoZona;
    private float acumulado;

    public bool Atacando => atacando;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        colliderPropio = GetComponent<Collider2D>();
        latigo = GetComponent<AtaqueLatigo>();

        if (floresPrefab != null)
        {
            // en el mundo (no hija): las flores quedan flotando donde nacieron
            flores = Instantiate(floresPrefab);
            flores.name = "FloresGolpe2";
            var ren = flores.GetComponent<ParticleSystemRenderer>();
            ren.sortingLayerID = dibujo.sortingLayerID;
            ren.sortingOrder = dibujo.sortingOrder + 2;
            flores.Play();
        }
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado != null && teclado[tecla].wasPressedThisFrame) Atacar();

        if (!atacando) return;

        int frame = FrameActual();
        if (!lanzo && frame >= frameLanzamiento)
        {
            lanzo = true;
            Lanzar();
        }
        if (frame > frames.Length)
        {
            atacando = false;
            finUltimo = Time.time;
            return;
        }
        EmitirDelBrazo(frame - 1);
    }

    public bool Atacar()
    {
        if (atacando || frames == null || frames.Length == 0 || Time.time < finUltimo + espera) return false;
        if (latigo != null && latigo.Atacando) return false;
        atacando = true;
        lanzo = false;
        inicio = Time.time;
        zonaAnterior = -1;
        acumulado = 0f;
        return true;
    }

    private void LateUpdate()
    {
        if (atacando) dibujo.sprite = frames[Mathf.Clamp(FrameActual() - 1, 0, frames.Length - 1)];
    }

    private int FrameActual() => Mathf.FloorToInt((Time.time - inicio) * fps) + 1;

    // ---------------- particulas del brazo ----------------

    private void EmitirDelBrazo(int indiceFrame)
    {
        if (flores == null || zonas == null || zonaDeFrame == null || indiceFrame >= zonaDeFrame.Length) return;
        int z = zonaDeFrame[indiceFrame];
        if (z < 0 || z >= zonas.Length || zonas[z].puntos == null || zonas[z].puntos.Length < 3) return;

        int cantidad = 0;
        if (z != zonaAnterior)
        {
            // cambio de pose: las particulas siguen el desplazamiento de la zona (el brazo)
            movimientoZona = zonaAnterior >= 0 ? Centro(zonas[z].puntos) - Centro(zonas[zonaAnterior].puntos) : Vector2.zero;
            zonaAnterior = z;
            cantidad += particulasPorPose;
        }
        acumulado += particulasPorSegundo * Time.deltaTime;
        cantidad += Mathf.FloorToInt(acumulado);
        acumulado -= Mathf.Floor(acumulado);

        for (int i = 0; i < cantidad; i++)
        {
            Vector2 local = PuntoAlAzar(zonas[z].puntos);
            Vector2 direccion = movimientoZona.sqrMagnitude > 0.0004f ? movimientoZona.normalized : new Vector2(0.4f, 0.6f).normalized;
            Vector2 velLocal = direccion * velocidadParticulas * Random.Range(0.5f, 1.3f) + Random.insideUnitCircle * 0.35f;
            var p = new ParticleSystem.EmitParams
            {
                position = transform.TransformPoint(local),
                velocity = transform.TransformVector(velLocal),
                applyShapeToPosition = false
            };
            flores.Emit(p, 1);
        }
    }

    private static Vector2 Centro(Vector2[] poligono)
    {
        Vector2 suma = Vector2.zero;
        foreach (var v in poligono) suma += v;
        return suma / poligono.Length;
    }

    // punto al azar dentro de un poligono convexo (triangulos en abanico, pesados por area)
    private static Vector2 PuntoAlAzar(Vector2[] p)
    {
        float total = 0f;
        for (int i = 1; i < p.Length - 1; i++) total += Area(p[0], p[i], p[i + 1]);
        float r = Random.value * total;
        for (int i = 1; i < p.Length - 1; i++)
        {
            float a = Area(p[0], p[i], p[i + 1]);
            if (r <= a || i == p.Length - 2)
            {
                float u = Random.value, v = Random.value;
                if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                return p[0] + (p[i] - p[0]) * u + (p[i + 1] - p[0]) * v;
            }
            r -= a;
        }
        return p[0];
    }

    private static float Area(Vector2 a, Vector2 b, Vector2 c) => Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;

    // ---------------- bola ----------------

    private void Lanzar()
    {
        if (bola == null) return;
        // mirando a la izquierda la escala en X es negativa (igual que en el latigo)
        float sentido = Mathf.Sign(transform.lossyScale.x) * (dibujo.flipX ? -1f : 1f);
        Vector3 origen = transform.position + new Vector3(salida.x * sentido, salida.y, 0f);

        var raiz = new GameObject("BolaDeSabiduria");
        raiz.transform.position = origen;

        var bolaGo = new GameObject("Bola");
        bolaGo.transform.SetParent(raiz.transform, false);
        var srBola = bolaGo.AddComponent<SpriteRenderer>();
        srBola.sprite = bola;
        srBola.sortingLayerID = dibujo.sortingLayerID;
        srBola.sortingOrder = dibujo.sortingOrder + 4;
        if (materialBrillo != null) srBola.sharedMaterial = materialBrillo;

        SpriteRenderer srEstela = null;
        if (estelaBola != null)
        {
            var estelaGo = new GameObject("Estela");
            estelaGo.transform.SetParent(raiz.transform, false);
            srEstela = estelaGo.AddComponent<SpriteRenderer>();
            srEstela.sprite = estelaBola;
            srEstela.sortingLayerID = dibujo.sortingLayerID;
            srEstela.sortingOrder = dibujo.sortingOrder + 3;
            if (materialBrillo != null) srEstela.sharedMaterial = materialBrillo;
        }

        Vector2 direccion = Apuntar(origen, sentido);
        raiz.AddComponent<ProyectilSabiduria>().Iniciar(direccion, velocidadInicial, velocidadMaxima, aceleracion, alcance, danio,
            giroMaximo, radio, colliderPropio, srBola.transform, srEstela, flores, distanciaEntreFlores);
    }

    // busca el enemigo vivo mas cercano que este adelante y dentro del angulo de punteria
    private Vector2 Apuntar(Vector3 origen, float sentido)
    {
        Vector2 mejor = new Vector2(sentido, 0f);
        float mejorDistancia = alcance;
        foreach (var enemigo in FindObjectsByType<VidaEnemigo>(FindObjectsSortMode.None))
        {
            if (!enemigo.Vivo) continue;
            var col = enemigo.GetComponent<Collider2D>();
            Vector2 objetivo = col != null ? (Vector2)col.bounds.center : (Vector2)enemigo.transform.position;
            Vector2 hacia = objetivo - (Vector2)origen;
            if (hacia.x * sentido <= 0.2f) continue;
            float distancia = hacia.magnitude;
            if (distancia > mejorDistancia) continue;
            if (Vector2.Angle(new Vector2(sentido, 0f), hacia) > anguloPunteria) continue;
            mejorDistancia = distancia;
            mejor = hacia / distancia;
        }
        return mejor;
    }

    private void OnDrawGizmosSelected()
    {
        float sentido = Mathf.Sign(transform.lossyScale.x);
        Vector3 origen = transform.position + new Vector3(salida.x * sentido, salida.y, 0f);
        Gizmos.color = new Color(0.6f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireSphere(origen, radio);
        Gizmos.DrawLine(origen, origen + Vector3.right * sentido * alcance);

        if (zonas == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0.7f, 0.8f);
        foreach (var z in zonas)
        {
            if (z.puntos == null) continue;
            for (int i = 0; i < z.puntos.Length; i++)
                Gizmos.DrawLine(transform.TransformPoint(z.puntos[i]), transform.TransformPoint(z.puntos[(i + 1) % z.puntos.Length]));
        }
    }
}
