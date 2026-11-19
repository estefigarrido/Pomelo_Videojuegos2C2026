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
    // (Caro, 19/11) el primer paso suena apenas empieza a caminar y despues uno cada 3 unidades caminadas (5.8
    // corriendo, la misma proporcion). Como referencia, el ritmo real de los pies de la animacion es un pie cada
    // 2.03 u caminando y cada 5.48 u corriendo.
    [Tooltip("Unidades caminadas entre paso y paso (Caro: 3).")]
    [SerializeField] private float distanciaEntrePasos = 3f;
    [Tooltip("Unidades corridas entre paso y paso con el sprint (5.8: la misma proporcion que caminando).")]
    [SerializeField] private float distanciaEntrePasosCorriendo = 5.8f;
    [Tooltip("Si estuvo quieta en el piso al menos estos segundos, al empezar a caminar suena el primer apoyo.")]
    [SerializeField] private float tiempoParaPrimerPaso = 0.15f;
    private float quieta = 1f;   // segundos quieta en el piso (arranca como quieta: el primer paso suena enseguida)
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

    // (Caro) Cada paso suena justo cuando la chica apoya el pie en el piso (los cuadros de apoyo de la animacion de
    // caminar o de correr), pero no en todos: el primero apenas empieza a caminar y despues solo cuando ya recorrio
    // 'distanciaEntrePasos' (el siguiente apoyo despues de esa distancia).
    [Header("Apoyos del pie en la animacion (parte del ciclo, de 0 a 1)")]
    [Tooltip("ChicaCaminando: apoya en el cuadro 1 (0) y en el 7 (0.5 s de 1.083 s = 0.46).")]
    [SerializeField] private float[] apoyosCaminando = { 0f, 0.4615f };
    [Tooltip("ChicaSprint: apoya en el cuadro 2 (0.083 s de 1.167 s = 0.071) y en el 9 (0.667 s = 0.571).")]
    [SerializeField] private float[] apoyosCorriendo = { 0.0714f, 0.5714f };
    [SerializeField] private string estadoCaminando = "Caminando";
    [SerializeField] private string estadoCorriendo = "Sprint";

    [Tooltip("Segundos minimos entre un sonido de paso y el siguiente (o la caida de un salto).")]
    [SerializeField] private float separacionMinima = 0.25f;

    private Animator animator;
    private float ultimoSonido = -10f;
    private bool primerPasoPendiente = true;
    private float faseAnterior = -1f;
    private int estadoAnterior;

    private void Update()
    {
        if (animator == null) animator = GetComponent<Animator>();
        float x = transform.position.x;
        float dx = Mathf.Abs(x - xAnterior);
        xAnterior = x;
        bool enPiso = movimiento == null || (movimiento.enabled && movimiento.EnPiso);
        // al aterrizar ya suena la caida (MovimientoPersonaje): cuenta como paso, asi no suenan dos juntos
        if (enPiso && !estabaEnPiso) { primerPasoPendiente = false; recorrido = 0f; ultimoSonido = Time.time; }
        estabaEnPiso = enPiso;
        if (!enPiso || dx > 1f) { quieta = 0f; faseAnterior = -1f; return; }   // en el aire no pisa
        if (dx < 0.0005f)
        {
            quieta += Time.deltaTime;
            if (quieta >= tiempoParaPrimerPaso) primerPasoPendiente = true;   // estuvo quieta: al arrancar suena
        }
        else
        {
            quieta = 0f; recorrido += dx;
            // (Caro) apenas arranca a caminar (estaba quieta) suena el primer paso, en el acto: el primer cuadro de la
            // animacion de caminar ya es un apoyo
            if (primerPasoPendiente && Time.time - ultimoSonido >= separacionMinima)
            {
                primerPasoPendiente = false;
                recorrido = 0f;
                ultimoSonido = Time.time;
                bool corre = movimiento != null && movimiento.SprintActivo;
                Sonar(corre ? volumenCorriendo : volumen, corre ? tonoCorriendo : 1f);
            }
        }

        // el momento del apoyo, leido de la animacion que esta sonando
        bool hayAnimacion = animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null;
        var info = hayAnimacion ? animator.GetCurrentAnimatorStateInfo(0) : default;
        bool caminando = hayAnimacion && info.IsName(estadoCaminando), corriendo = hayAnimacion && info.IsName(estadoCorriendo);
        if (!caminando && !corriendo)
        {
            faseAnterior = -1f; estadoAnterior = 0;
            // respaldo: si no hay animacion que leer, suena por distancia (nunca muda). Con animacion, en otros estados
            // (quieta, dash) no pisa.
            if (!hayAnimacion && dx >= 0.0005f && recorrido >= PasoActual() && Time.time - ultimoSonido >= separacionMinima)
            {
                recorrido = 0f; ultimoSonido = Time.time;
                Sonar(volumen, 1f);
            }
            return;
        }
        float fase = info.normalizedTime - Mathf.Floor(info.normalizedTime);
        // recien entro a la animacion: cuenta desde el principio (asi el apoyo del cuadro 1 suena)
        if (info.shortNameHash != estadoAnterior || faseAnterior < 0f) faseAnterior = -0.0001f;
        estadoAnterior = info.shortNameHash;
        bool apoyo = false;
        foreach (float a in corriendo ? apoyosCorriendo : apoyosCaminando)
            if (Cruzo(faseAnterior, fase, a)) { apoyo = true; break; }
        faseAnterior = fase;
        if (!apoyo) return;

        // un apoyo, un sonido: nunca dos pasos mas cerca que 'separacionMinima' segundos
        if (Time.time - ultimoSonido < separacionMinima) return;
        if (primerPasoPendiente || recorrido >= PasoActual())
        {
            ultimoSonido = Time.time;
            primerPasoPendiente = false;
            recorrido = 0f;
            Sonar(corriendo ? volumenCorriendo : volumen, corriendo ? tonoCorriendo : 1f);
        }
    }

    // la fase paso por 'a' entre el cuadro anterior y este (dando la vuelta del ciclo si hace falta)
    private static bool Cruzo(float antes, float ahora, float a)
    {
        if (ahora >= antes) return antes < a && a <= ahora;
        return a > antes || a <= ahora;   // dio la vuelta (de 0.95 a 0.05, por ejemplo)
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
