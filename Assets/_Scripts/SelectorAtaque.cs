using UnityEngine;
using UnityEngine.InputSystem;

// La chica usa un solo golpe a la vez: con B cambia entre el latigo y la bola de sabiduria,
// y con click izquierdo tira el que esta elegido. El circulo de la barra de poderes muestra el elegido.
[RequireComponent(typeof(AtaqueLatigo), typeof(AtaqueSabiduria))]
public class SelectorAtaque : MonoBehaviour
{
    public enum Ataque { Latigo, Sabiduria }

    [SerializeField] private Key teclaCambiar = Key.B;
    [SerializeField] private Ataque inicial = Ataque.Latigo;

    private AtaqueLatigo latigo;
    private AtaqueSabiduria sabiduria;

    public static Ataque Elegido { get; private set; }

    // Avisa cada vez que cambia el golpe elegido (lo usa el HUD).
    public static event System.Action<Ataque> Cambio;

    // Cambia el golpe elegido desde afuera (la pantalla de instruccion de ataque) y avisa al HUD.
    public static void Elegir(Ataque nuevo)
    {
        if (Elegido == nuevo) return;
        Elegido = nuevo;
        Cambio?.Invoke(Elegido);
    }

    private void Awake()
    {
        latigo = GetComponent<AtaqueLatigo>();
        sabiduria = GetComponent<AtaqueSabiduria>();
        Elegido = inicial;
    }

    private void Update()
    {
        // con el menu de pausa abierto no ataca ni cambia de golpe
        // tampoco mientras se arma la pelea con Umbrae (la chica queda quieta hasta que se carga su barra de vida)
        // ni mientras lee un dialogo
        // ni con la pantalla de instruccion de ataque abierta (ella maneja la B y el click)
        if (MenuPausa.BloqueaControles || InicioPeleaUmbrae.ChicaQuieta || CuadroDialogo.Abierto || QuietaPorEscena.Activa ||
            InstruccionAtaque.Abierta) return;

        var teclado = Keyboard.current;
        if ((teclado != null && teclado[teclaCambiar].wasPressedThisFrame) || Mando.CambiarAtaque)
        {
            Elegido = Elegido == Ataque.Latigo ? Ataque.Sabiduria : Ataque.Latigo;
            Cambio?.Invoke(Elegido);
        }

        var mouse = Mouse.current;
        if ((mouse != null && mouse.leftButton.wasPressedThisFrame) || Mando.Atacar)
        {
            if (Elegido == Ataque.Latigo) latigo.Atacar();
            else sabiduria.Atacar();
        }
    }
}
