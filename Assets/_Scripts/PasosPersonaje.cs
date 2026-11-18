using UnityEngine;

// Pasos de la chica al caminar (zapatilla sobre madera, Assets/_Sonidos/Paso_madera_1..4).
// Suenan segun la distancia recorrida en el piso (cada cierta distancia, un paso), al ritmo de los pies de la
// animacion de caminar o de correr. Tocar la tecla un ratito no hace sonar un paso.
// Cada paso usa una variante al azar (nunca la misma dos veces seguidas), asi no suena repetido.
[RequireComponent(typeof(Animator))]
public class PasosPersonaje : MonoBehaviour
{
    [SerializeField] private AudioClip[] pasos;
    [Range(0f, 1f)]
    [SerializeField] private float volumen = 0.5f;
    [Tooltip("Cuanto puede variar el tono de un paso a otro (0.04 = 4 % para arriba o para abajo).")]
    [SerializeField] private float variacionTono = 0.04f;

    [Header("Corriendo (ChicaSprint)")]
    [Tooltip("Al correr pisa mas fuerte: el mismo paso, un poco mas alto de volumen.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumenCorriendo = 0.65f;
    [Tooltip("Y un poquito mas agudo (apoyo mas corto y rapido).")]
    [SerializeField] private float tonoCorriendo = 1.06f;

    [Header("Sobre las hojas de loto")]
    [Tooltip("Pasos sobre las hojas del lago (Paso_hoja_*): hoja blanda que cruje, con un poquito de agua abajo. " +
             "Se usan en vez de los pasos comunes mientras la chica esta parada sobre una HojaLotoQueSeHunde.")]
    [SerializeField] private AudioClip[] pasosHoja;

    [Header("Cada cuanto suena un paso (distancia recorrida en el piso)")]
    // camina a 6 u/s y ChicaCaminando apoya 2 pies en 1.08 s -> 3.25 u por paso; corre a 10.8 u/s y
    // ChicaSprint apoya 2 pies en 1.17 s -> 6.3 u por paso. Si cambia la velocidad, cambiar esto igual.
    // OJO: se llaman distinto que antes (distanciaPorPaso) a proposito: la escena tenia guardados los valores viejos
    // (1.2 y 3.5, pasos demasiado seguidos) y volvian con cada merge. Con el nombre nuevo se usan 5 y 9.7.
    [Tooltip("Unidades caminadas entre paso y paso (Caro eligio 5).")]
    [SerializeField] private float distanciaEntrePasos = 5f;
    [Tooltip("Unidades corridas entre paso y paso con el sprint (9.7: la misma proporcion que caminando).")]
    [SerializeField] private float distanciaEntrePasosCorriendo = 9.7f;
    private int ultimo = -1;
    private MovimientoPersonaje movimiento;
    private float recorrido;     // distancia en el piso desde el ultimo paso
    private float xAnterior;
    private bool estabaEnPiso;

    private void Awake()
    {
        movimiento = GetComponent<MovimientoPersonaje>();
        xAnterior = transform.position.x;
    }

    // Los pasos no dependen de cada toque de la tecla: suenan cada 'distanciaEntrePasos' recorrida en el piso.
    // Toques cortos no suenan.
    private void Update()
    {
        float x = transform.position.x;
        float dx = Mathf.Abs(x - xAnterior);
        xAnterior = x;
        bool enPiso = movimiento == null || (movimiento.enabled && movimiento.EnPiso);
        // al aterrizar suena el primer paso en la mitad de la distancia, para que no quede mudo
        if (enPiso && !estabaEnPiso) recorrido = Mathf.Max(recorrido, PasoActual() * 0.5f);
        estabaEnPiso = enPiso;
        if (!enPiso || dx > 1f) return;   // en el aire no pisa; un salto grande (reaparecer) no cuenta
        if (dx < 0.0005f) return;
        recorrido += dx;
        float distancia = PasoActual();
        if (recorrido < distancia) return;
        recorrido -= distancia;
        bool corriendo = movimiento != null && movimiento.SprintActivo;
        Sonar(corriendo ? volumenCorriendo : volumen, corriendo ? tonoCorriendo : 1f);
    }

    private float PasoActual() =>
        Mathf.Max(0.1f, movimiento != null && movimiento.SprintActivo ? distanciaEntrePasosCorriendo : distanciaEntrePasos);

    // Las animaciones todavia llaman a estos eventos ("Paso" al caminar, "PasoCorriendo" al correr): ya no suenan
    // desde aca, quedan vacios para que Unity no avise que falta el metodo.
    public void Paso() { }
    public void PasoCorriendo() { }

    private void Sonar(float vol, float tono)
    {
        if (MenuPausa.BloqueaControles) return;
        // sobre una hoja de loto suena a hoja; en el resto, el paso de siempre
        var lista = movimiento != null && movimiento.SobreHoja && pasosHoja != null && pasosHoja.Length > 0 ? pasosHoja : pasos;
        if (lista == null || lista.Length == 0) return;
        int i = Random.Range(0, lista.Length);
        if (lista.Length > 1 && i == ultimo) i = (i + 1) % lista.Length;
        ultimo = i;
        Sonido.Reproducir(lista[i], vol, tono + Random.Range(-variacionTono, variacionTono));
    }
}
