using UnityEditor;
using UnityEngine;

// Deja a la chica adentro del portal de entrada, donde empieza la intro (lo mismo que el clic derecho
// en el componente InicioNivel del objeto de Inicio, pero sin tener que buscarlo).
// Menu:  Pomelo -> Llevar la chica al portal
public static class LlevarChicaAlPortal
{
    [MenuItem("Pomelo/Llevar la chica al portal")]
    public static void Llevar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de llevar la chica al portal.");
            return;
        }

        var inicio = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (inicio == null)
        {
            Debug.LogError("No encontre el inicio del nivel (InicioNivel) en la escena.");
            return;
        }
        inicio.LlevarChicaAlPortal();
    }
}
