using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Dialogo de un objeto o personaje. Se agrega a cualquier objeto (Add Component -> Dialogo) y se escriben
// sus lineas en el Inspector: quien habla, que dice y, si hace falta, una ilustracion de pantalla completa
// (si la linea no tiene ilustracion, se ve la escena detras del cuadro).
// Cuando la chica esta cerca (dentro de 'distancia'), aparece el 'indicador' (si hay uno puesto) y con
// E (o L1 en el joystick) se abre el cuadro de dialogo (CuadroDialogo). La chica se queda quieta hasta que
// termina; el resto del juego sigue.
public class Dialogo : MonoBehaviour
{
    [System.Serializable]
    public class Linea
    {
        [Tooltip("Prendido: la linea no lleva nombre (por ejemplo al tocar un objeto) y el texto sube a ocupar su lugar.")]
        public bool sinNombre;
        [Tooltip("Quien habla (por ejemplo: Maestra Li Hua).")]
        public string personaje;
        [TextArea(2, 5)]
        public string texto;
        [Tooltip("Opcional: ilustracion de pantalla completa detras del cuadro.")]
        public Sprite ilustracion;
    }

    [SerializeField] private Linea[] lineas = new Linea[1];

    [Header("Interaccion")]
    [Tooltip("Distancia (desde este objeto) a la que la chica puede interactuar.")]
    [SerializeField] private Vector2 distancia = new Vector2(2f, 2.5f);
    [Tooltip("Centro de la zona, relativo a este objeto.")]
    [SerializeField] private Vector2 centro = Vector2.zero;
    [SerializeField] private Key tecla = Key.E;
    [Tooltip("Cartel 'Interactuar' (prefab Assets/_UI/Carteles/Cartel_interactuar). Se crea solo sobre el objeto y " +
             "aparece cuando la chica entra a la zona; se va cuando se aleja o se abre el dialogo. Vacio = sin cartel.")]
    [SerializeField] private GameObject cartelInteractuar;
    [Tooltip("Donde va el cartel, relativo a este objeto (unidades).")]
    [SerializeField] private Vector2 posicionCartel = new Vector2(0f, 1.6f);
    [Tooltip("Opcional, en vez del cartel: algo de la escena que se prende cuando la chica puede interactuar.")]
    [SerializeField] private GameObject indicador;
    [Tooltip("Si esta prendido, el dialogo pasa una sola vez.")]
    [SerializeField] private bool soloUnaVez;
    [Tooltip("Si esta prendido, se abre solo cuando la chica entra a la zona (sin apretar la tecla).")]
    [SerializeField] private bool abrirAlEntrar;
    [Tooltip("Con 'abrir al entrar': se abre solo la primera vez. Despues hay que interactuar (cartel 'Interactuar'), " +
             "asi no frena a la chica cada vez que pasa (por ejemplo la runa del principio).")]
    [SerializeField] private bool abrirAlEntrarSoloLaPrimeraVez;

    [Header("Al terminar")]
    [SerializeField] private UnityEvent alTerminar;

    // tamano de los carteles en el mundo (el mismo que los de tutorial)
    private const float EscalaCartel = 0.013f;

    private Transform chica;
    private bool usado;
    private bool estabaAdentro;
    private CartelTutorial cartel;   // el cartel 'Interactuar' de este objeto (si hay)
    private bool mostrando;

    public Linea[] Lineas => lineas;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;

        if (indicador == null && cartelInteractuar != null)
        {
            // se crea sobre el objeto, del mismo tamano que los carteles de tutorial (aunque el objeto este escalado)
            var go = Instantiate(cartelInteractuar, transform);
            go.name = cartelInteractuar.name;
            go.transform.position = new Vector3(transform.position.x + posicionCartel.x,
                                                transform.position.y + posicionCartel.y, transform.position.z - 0.05f);
            go.transform.rotation = Quaternion.identity;
            Vector3 l = transform.lossyScale;
            go.transform.localScale = new Vector3(EscalaCartel / Mathf.Max(0.0001f, Mathf.Abs(l.x)),
                                                  EscalaCartel / Mathf.Max(0.0001f, Mathf.Abs(l.y)), 1f);
            cartel = go.GetComponent<CartelTutorial>();
            indicador = go;
        }
        else if (indicador != null) cartel = indicador.GetComponent<CartelTutorial>();

        if (cartel != null) cartel.Cerca(false);
        else if (indicador != null) indicador.SetActive(false);
    }

    private void Update()
    {
        bool adentro = PuedeHablar();
        bool mostrar = adentro && !CuadroDialogo.Abierto && !InstruccionAtaque.Abierta;
        if (mostrar != mostrando)
        {
            mostrando = mostrar;
            if (cartel != null) cartel.Cerca(mostrar);
            else if (indicador != null) indicador.SetActive(mostrar);
        }

        if (adentro && !CuadroDialogo.Abierto && !InstruccionAtaque.Abierta && !MenuPausa.BloqueaControles)
        {
            var teclado = Keyboard.current;
            bool apreto = (teclado != null && teclado[tecla].wasPressedThisFrame) || Mando.Interactuar;
            bool entro = abrirAlEntrar && !estabaAdentro && !(abrirAlEntrarSoloLaPrimeraVez && usado);
            if ((apreto || entro) && CuadroDialogo.Instancia != null) CuadroDialogo.Instancia.Abrir(this);
        }
        estabaAdentro = adentro;
    }

    private bool PuedeHablar()
    {
        if (chica == null || (soloUnaVez && usado) || lineas == null || lineas.Length == 0) return false;
        Vector2 c = (Vector2)transform.position + centro;
        Vector2 d = (Vector2)chica.position - c;
        return Mathf.Abs(d.x) <= distancia.x && Mathf.Abs(d.y) <= distancia.y;
    }

    // Lo llama el cuadro cuando se cierra.
    public void AlTerminar()
    {
        usado = true;
        alTerminar?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.85f, 0.5f, 0.9f);
        Gizmos.DrawWireCube((Vector2)transform.position + centro, distancia * 2f);
        // donde va a aparecer el cartel 'Interactuar' (ancho y alto aproximados)
        if (cartelInteractuar != null && indicador == null)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.7f);
            Gizmos.DrawWireCube((Vector2)transform.position + posicionCartel, new Vector2(4.2f, 0.6f));
        }
    }
}
