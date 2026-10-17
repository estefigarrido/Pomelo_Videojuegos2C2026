using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Pone una sola vez los tiempos nuevos de la intro en el InicioNivel de SampleScene
// (la escena guarda sus propios valores, que pisan los del codigo).
// Si la escena no tenia otros cambios la guarda; al guardarse con los valores nuevos, este archivo se borra solo.
[InitializeOnLoad]
public static class AcelerarIntro
{
    private const string Esta = "Assets/Editor/AcelerarIntro.cs";

    static AcelerarIntro()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Aplicar;
    }

    private static bool YaAplicado(InicioNivel intro)
    {
        var so = new SerializedObject(intro);
        return Mathf.Approximately(so.FindProperty("esperaInicial").floatValue, 3f)
            && Mathf.Approximately(so.FindProperty("duracionGiro").floatValue, 0.18f);
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        var intro = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (escena.name != "SampleScene" || intro == null || !YaAplicado(intro)) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene") return;
        var intro = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (intro == null) { Debug.LogWarning("[Intro] No encontre InicioNivel en la escena."); return; }
        if (YaAplicado(intro)) return;

        bool teniaCambios = escena.isDirty;
        var so = new SerializedObject(intro);
        so.FindProperty("esperaInicial").floatValue = 3f;          // antes 5
        so.FindProperty("duracionSubidaPuerta").floatValue = 1.5f;  // antes 2.5
        so.FindProperty("esperaDespuesDeAbrir").floatValue = 0.8f;  // antes 2
        so.FindProperty("duracionGiro").floatValue = 0.18f;         // antes 0.35
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Intro] La escena tenia otros cambios sin guardar: guardala (Ctrl+S) para que queden los tiempos nuevos.");
        Debug.Log("[Intro] Tiempos nuevos: puerta a los 3 s, sube en 1.5 s, gira 0.8 s despues en 0.18 s.");
    }
}
