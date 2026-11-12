using UnityEngine;
using UnityEngine.InputSystem;

// Controles del joystick (PlayStation, Xbox o cualquiera que Unity reconozca como Gamepad).
// Son los mismos que muestran los carteles de tutorial:
//   Caminar ............ stick derecho (tambien sirven el stick izquierdo y la cruceta)
//   Saltar / doble ..... X (Cruz)                   (en Xbox: A)
//   Atacar ............. Circulo                    (en Xbox: B)
//   Correr ............. apretar el stick derecho (R3) mientras se camina
//   Cambiar de ataque .. R1 o R2
//   Reparar templo ..... apretar el stick izquierdo (L3)
//   Impulsarte (dash) .. Triangulo                  (en Xbox: Y)
//   Recargar vida ...... Cuadrado                   (en Xbox: X)
//   Pausa .............. Options / Start
//   En la pausa: mover el stick derecho arriba / abajo elige, X acepta
// El teclado y el mouse siguen andando igual; esto se suma.
public static class Mando
{
    private const float ZonaMuerta = 0.35f;   // con el stick apenas movido no camina

    private static Gamepad G => Gamepad.current;

    // -1 izquierda, 0 quieta, 1 derecha
    public static float Horizontal()
    {
        var g = G;
        if (g == null) return 0f;
        float derecho = g.rightStick.x.ReadValue();
        float izquierdo = g.leftStick.x.ReadValue();
        float valor = Mathf.Abs(derecho) >= Mathf.Abs(izquierdo) ? derecho : izquierdo;
        float cruceta = g.dpad.x.ReadValue();
        if (Mathf.Abs(cruceta) > 0.5f) valor = cruceta;
        return Mathf.Abs(valor) < ZonaMuerta ? 0f : Mathf.Sign(valor);
    }

    public static bool Saltar => G != null && G.buttonSouth.wasPressedThisFrame;
    public static bool Atacar => G != null && G.buttonEast.wasPressedThisFrame;
    public static bool Correr => G != null && G.rightStickButton.wasPressedThisFrame;
    public static bool CambiarAtaque => G != null && (G.rightShoulder.wasPressedThisFrame || G.rightTrigger.wasPressedThisFrame);
    public static bool RepararTemplo => G != null && G.leftStickButton.wasPressedThisFrame;
    public static bool Impulsarte => G != null && G.buttonNorth.wasPressedThisFrame;
    public static bool RecargarVida => G != null && G.buttonWest.wasPressedThisFrame;

    // ---------- menu de pausa ----------
    // Pausa: el boton Options / Start. Aceptar: X (Cruz). (El Circulo no hace nada en la pausa.)
    public static bool Pausa => G != null && G.startButton.wasPressedThisFrame;
    public static bool Aceptar => G != null && G.buttonSouth.wasPressedThisFrame;

    // -1 arriba, 0 nada, 1 abajo: stick derecho (el "R" del menu), tambien el izquierdo y la cruceta
    public static float Vertical()
    {
        var g = G;
        if (g == null) return 0f;
        float derecho = g.rightStick.y.ReadValue();
        float izquierdo = g.leftStick.y.ReadValue();
        float valor = Mathf.Abs(derecho) >= Mathf.Abs(izquierdo) ? derecho : izquierdo;
        float cruceta = g.dpad.y.ReadValue();
        if (Mathf.Abs(cruceta) > 0.5f) valor = cruceta;
        // en el stick, arriba es positivo; en la lista, arriba es el anterior
        return Mathf.Abs(valor) < 0.5f ? 0f : -Mathf.Sign(valor);
    }
}
