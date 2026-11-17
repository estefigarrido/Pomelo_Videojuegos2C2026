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
        [Tooltip("Quien habla (por ejemplo: Lady Li Hua). Vacio = no se muestra nombre.")]
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
    [Tooltip("Opcional: algo que se prende cuando la chica puede interactuar (por ejemplo un icono de la tecla).")]
    [SerializeField] private GameObject indicador;
    [Tooltip("Si esta prendido, el dialogo pasa una sola vez.")]
    [SerializeField] private bool soloUnaVez;
    [Tooltip("Si esta prendido, se abre solo cuando la chica entra a la zona (sin apretar la tecla).")]
    [SerializeField] private bool abrirAlEntrar;

    [Header("Al terminar")]
    [SerializeField] private UnityEvent alTerminar;

    private Transform chica;
    private bool usado;
    private bool estabaAdentro;

    public Linea[] Lineas => lineas;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
        if (indicador != null) indicador.SetActive(false);
    }

    private void Update()
    {
        bool adentro = PuedeHablar();
        if (indicador != null && indicador.activeSelf != (adentro && !CuadroDialogo.Abierto))
            indicador.SetActive(adentro && !CuadroDialogo.Abierto);

        if (adentro && !CuadroDialogo.Abierto && !MenuPausa.BloqueaControles)
        {
            var teclado = Keyboard.current;
            bool apreto = (teclado != null && teclado[tecla].wasPressedThisFrame) || Mando.Interactuar;
            bool entro = abrirAlEntrar && !estabaAdentro;
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
    }
}
