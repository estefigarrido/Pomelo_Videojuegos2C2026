using UnityEngine;
using UnityEngine.InputSystem;

// La chica usa un solo golpe a la vez: con V cambia entre el latigo y la bola de sabiduria,
// y con click izquierdo tira el que esta elegido. El circulo de la barra de poderes muestra el elegido.
[RequireComponent(typeof(AtaqueLatigo), typeof(AtaqueSabiduria))]
public class SelectorAtaque : MonoBehaviour
{
    public enum Ataque { Latigo, Sabiduria }

    [SerializeField] private Key teclaCambiar = Key.V;
    [SerializeField] private Ataque inicial = Ataque.Latigo;

    private AtaqueLatigo latigo;
    private AtaqueSabiduria sabiduria;

    public static Ataque Elegido { get; private set; }

    // Avisa cada vez que cambia el golpe elegido (lo usa el HUD).
    public static event System.Action<Ataque> Cambio;

    private void Awake()
    {
        latigo = GetComponent<AtaqueLatigo>();
        sabiduria = GetComponent<AtaqueSabiduria>();
        Elegido = inicial;
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado != null && teclado[teclaCambiar].wasPressedThisFrame)
        {
            Elegido = Elegido == Ataque.Latigo ? Ataque.Sabiduria : Ataque.Latigo;
            Cambio?.Invoke(Elegido);
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (Elegido == Ataque.Latigo) latigo.Atacar();
            else sabiduria.Atacar();
        }
    }
}
