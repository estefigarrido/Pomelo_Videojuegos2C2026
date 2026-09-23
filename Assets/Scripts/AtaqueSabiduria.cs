using UnityEngine;
using UnityEngine.InputSystem;

// Segundo golpe: la chica hace el gesto de "chica_golpe2" y lanza una bola de sabiduria.
// Igual que el latigo, es independiente del movimiento y del Animator: mientras dura,
// dibuja sus propios frames en LateUpdate (despues del Animator) y una estela encima.
[RequireComponent(typeof(SpriteRenderer))]
public class AtaqueSabiduria : MonoBehaviour
{
    [Header("Control")]
    [SerializeField] private Key tecla = Key.R;
    [Tooltip("Segundos de espera desde que termina un golpe hasta poder tirar otro.")]
    [SerializeField] private float espera = 0.3f;

    [Header("Animacion (chica_golpe2)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 24f;
    [Tooltip("Frame (empieza en 1) en el que sale la bola.")]
    [SerializeField] private int frameLanzamiento = 15;

    [Header("Estela")]
    [Tooltip("Una mascara por frame (misma cantidad que 'frames').")]
    [SerializeField] private Sprite[] estela;
    [SerializeField] private Color colorEstela = new Color(0.85f, 0.25f, 0.27f, 0.37f);
    [Tooltip("Opacidad de la estela en los frames repetidos de una misma pose (1 = igual que el primero).")]
    [Range(0f, 1f)]
    [SerializeField] private float opacidadSostenida = 0.6f;

    [Header("Bola de sabiduria")]
    [SerializeField] private Sprite bola;
    [SerializeField] private Material materialBrillo;
    [Tooltip("De donde sale, relativo al personaje mirando a la derecha (la mano de adelante).")]
    [SerializeField] private Vector2 salida = new Vector2(0.75f, 0.3f);
    [SerializeField] private float velocidad = 10f;
    [SerializeField] private float alcance = 14f;
    [Tooltip("Vida que saca a un enemigo (la vida maxima de los mobs es 100).")]
    [SerializeField] private float danio = 25f;
    [Tooltip("Grados por segundo que gira mientras vuela.")]
    [SerializeField] private float giro = 240f;
    [SerializeField] private float radio = 0.3f;
    [Tooltip("Si hay un enemigo adelante dentro de este angulo (grados), la bola va hacia el; si no, sale derecha.")]
    [SerializeField] private float anguloPunteria = 30f;

    [Header("Destello al impactar")]
    [SerializeField] private Sprite[] destellos;
    [SerializeField] private float fpsDestello = 16f;

    private SpriteRenderer dibujo;
    private SpriteRenderer dibujoEstela;
    private Collider2D colliderPropio;
    private AtaqueLatigo latigo;
    private bool atacando;
    private bool lanzo;
    private float inicio;
    private float finUltimo = -99f;

    public bool Atacando => atacando;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        colliderPropio = GetComponent<Collider2D>();
        latigo = GetComponent<AtaqueLatigo>();

        var go = new GameObject("EstelaGolpe2");
        go.transform.SetParent(transform, false);
        dibujoEstela = go.AddComponent<SpriteRenderer>();
        dibujoEstela.sortingLayerID = dibujo.sortingLayerID;
        dibujoEstela.sortingOrder = dibujo.sortingOrder + 1;
        dibujoEstela.sharedMaterial = dibujo.sharedMaterial;
        dibujoEstela.enabled = false;
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
            dibujoEstela.enabled = false;
        }
    }

    public bool Atacar()
    {
        if (atacando || frames == null || frames.Length == 0 || Time.time < finUltimo + espera) return false;
        if (latigo != null && latigo.Atacando) return false;
        atacando = true;
        lanzo = false;
        inicio = Time.time;
        return true;
    }

    private void LateUpdate()
    {
        if (!atacando) return;

        int i = Mathf.Clamp(FrameActual() - 1, 0, frames.Length - 1);
        dibujo.sprite = frames[i];

        if (estela == null || i >= estela.Length || estela[i] == null)
        {
            dibujoEstela.enabled = false;
            return;
        }
        // la estela se ve fuerte en el primer frame de cada pose y mas suave mientras se sostiene
        bool poseNueva = i == 0 || estela[i] != estela[i - 1];
        var c = colorEstela;
        c.a *= poseNueva ? 1f : opacidadSostenida;
        dibujoEstela.sprite = estela[i];
        dibujoEstela.color = c;
        dibujoEstela.flipX = dibujo.flipX;
        dibujoEstela.enabled = true;
    }

    private int FrameActual() => Mathf.FloorToInt((Time.time - inicio) * fps) + 1;

    private void Lanzar()
    {
        if (bola == null) return;
        // mirando a la izquierda la escala en X es negativa (igual que en el latigo)
        float sentido = Mathf.Sign(transform.lossyScale.x) * (dibujo.flipX ? -1f : 1f);
        Vector3 origen = transform.position + new Vector3(salida.x * sentido, salida.y, 0f);

        var go = new GameObject("BolaDeSabiduria");
        go.transform.position = origen;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bola;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = dibujo.sortingOrder + 2;
        if (materialBrillo != null) sr.sharedMaterial = materialBrillo;
        Vector2 direccion = Apuntar(origen, sentido);
        go.AddComponent<ProyectilSabiduria>().Iniciar(direccion, velocidad, alcance, danio, giro, radio, colliderPropio, destellos, fpsDestello);
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
    }
}
