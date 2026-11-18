using UnityEngine;
using UnityEngine.SceneManagement;

// Para que una escena deje a la chica quieta un momento (por ejemplo en la pantalla de inicio, mientras sale el
// Lore de la vasija): no camina, no salta, no dashea ni ataca, y queda en reposo (lo leen MovimientoPersonaje y
// SelectorAtaque, igual que cuando hay un dialogo abierto). Se vuelve a soltar sola al cambiar de escena.
public static class QuietaPorEscena
{
    public static bool Activa { get; private set; }

    public static void Frenar() => Activa = true;
    public static void Soltar() => Activa = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void AlArrancar()
    {
        Activa = false;
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Activa = false;
}
