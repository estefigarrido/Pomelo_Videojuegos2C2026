using UnityEngine;

// Va en Umbrae. Mientras no se le puede pegar (su hitbox esta apagado: subiendo a tirar los rayos,
// teletransportandose) y la chica se le acerca mucho, se vuelve "intangible": su opacidad baja y sube
// como un pulso. Nunca llega a desaparecer del todo. Cuando vuelve a poder recibir golpes o la chica se
// aleja, recupera la opacidad de a poco. La sombra exterior y el brillo de los rayos siguen esta opacidad.
[RequireComponent(typeof(SpriteRenderer))]
public class UmbraeIntangible : MonoBehaviour
{
    [Tooltip("Distancia (desde el centro del dibujo) a la que la chica lo hace volverse intangible.")]
    [SerializeField] private float distancia = 4f;
    [Tooltip("Opacidad mas baja del pulso (nunca deja de verse).")]
    [Range(0f, 1f)] [SerializeField] private float opacidadMinima = 0.3f;
    [Tooltip("Opacidad mas alta del pulso.")]
    [Range(0f, 1f)] [SerializeField] private float opacidadMaxima = 0.85f;
    [Tooltip("Pulsos por segundo.")]
    [SerializeField] private float pulsosPorSegundo = 1.6f;
    [Tooltip("Segundos que tarda en entrar y salir del efecto.")]
    [SerializeField] private float segundosTransicion = 0.25f;

    private SpriteRenderer dibujo;
    private Collider2D hitbox;
    private VidaEnemigo vida;
    private Transform chica;
    private float efecto;          // 0 = normal, 1 = intangible del todo
    private float tiempoPulso;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<Collider2D>();
        vida = GetComponent<VidaEnemigo>();
    }

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
    }

    // despues de que UmbraeBoss cambio el cuadro
    private void LateUpdate()
    {
        bool sinGolpes = hitbox != null && !hitbox.enabled && (vida == null || vida.Vivo);
        bool cerca = chica != null && Vector2.Distance(chica.position, dibujo.bounds.center) <= distancia;
        float objetivo = sinGolpes && cerca ? 1f : 0f;
        efecto = Mathf.MoveTowards(efecto, objetivo, Time.deltaTime / Mathf.Max(0.01f, segundosTransicion));

        if (efecto <= 0f && objetivo <= 0f)
        {
            tiempoPulso = 0f;
            if (dibujo.color.a != 1f) SetAlfa(1f);
            return;
        }

        // arranca bajando desde la opacidad maxima
        tiempoPulso += Time.deltaTime;
        float onda = 0.5f + 0.5f * Mathf.Cos(tiempoPulso * pulsosPorSegundo * Mathf.PI * 2f);
        float pulso = Mathf.Lerp(opacidadMinima, opacidadMaxima, onda);
        SetAlfa(Mathf.Lerp(1f, pulso, efecto));
    }

    private void SetAlfa(float a)
    {
        var c = dibujo.color; c.a = a; dibujo.color = c;
    }

    private void OnDrawGizmosSelected()
    {
        var sr = GetComponent<SpriteRenderer>();
        Gizmos.color = new Color(0.7f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireSphere(sr != null ? sr.bounds.center : transform.position, distancia);
    }
}
