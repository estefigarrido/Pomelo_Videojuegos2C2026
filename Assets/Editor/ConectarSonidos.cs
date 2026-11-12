using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Conecta los sonidos de Assets/_Sonidos con los scripts de la escena que los usan.
// Cada sonido nuevo se suma a la lista de Conectar().
// Menu:  Pomelo -> Sonidos -> Conectar sonidos
public static class ConectarSonidos
{
    private const string Carpeta = "Assets/_Sonidos/";
    // Puerta_piedra_subiendo.wav es la toma 3 (5.86 a 7.72 s) de "stone-slide-sound-effects" que bajo Caro:
    // el arrastre dura 1.75 s, asi que la puerta sube en 1.75 s y el sonido se apaga justo cuando termina
    private const float DuracionSubidaPuerta = 1.75f;

    [MenuItem("Pomelo/Sonidos/Conectar sonidos")]
    public static void Conectar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de conectar los sonidos.");
            return;
        }

        int conectados = 0;
        // puerta del portal de entrada que se levanta: se ajusta tambien la duracion de la subida, asi el
        // arrastre de piedra arranca y termina junto con la puerta
        conectados += Poner<InicioNivel>("sonidoPuerta", "Puerta_piedra_subiendo.wav");
        Numero<InicioNivel>("duracionSubidaPuerta", DuracionSubidaPuerta);
        Numero<InicioNivel>("volumenPuerta", 0.6f);   // de fondo, que no tape la musica

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Sonidos conectados: " + conectados + ". Guarda la escena con Ctrl+S.");
    }

    // pone un numero en el campo indicado (por ejemplo una duracion que tiene que coincidir con un sonido)
    private static void Numero<T>(string campo, float valor) where T : Object
    {
        var componente = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (componente == null) return;
        var so = new SerializedObject(componente);
        var prop = so.FindProperty(campo);
        if (prop == null) { Debug.LogWarning(typeof(T).Name + " no tiene el campo " + campo); return; }
        prop.floatValue = valor;
        so.ApplyModifiedProperties();
        Debug.Log(typeof(T).Name + "." + campo + " = " + valor + " (para que coincida con su sonido)");
    }

    // pone el sonido en el campo indicado del primer componente de ese tipo que haya en la escena
    private static int Poner<T>(string campo, string archivo) where T : Object
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + archivo);
        if (clip == null) { Debug.LogWarning("No encontre el sonido " + Carpeta + archivo); return 0; }
        var componente = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (componente == null) { Debug.LogWarning("No encontre " + typeof(T).Name + " en la escena para " + archivo); return 0; }
        var so = new SerializedObject(componente);
        var prop = so.FindProperty(campo);
        if (prop == null) { Debug.LogWarning(typeof(T).Name + " no tiene el campo " + campo); return 0; }
        prop.objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        Debug.Log(archivo + " -> " + typeof(T).Name + "." + campo);
        return 1;
    }
}
