using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez: la camara se ve ~30% mas lejos.
//  - tamano de la camara ortografica (la que sigue a la chica): 5.4 -> 7.02;
//  - deszoom de la intro (InicioNivel.tamanoEncuadreIntro): 6.7 -> 8.7, asi sigue alejandose al empezar.
// El encuadre fijo de la pelea con Umbrae no cambia (ya muestra toda la jaula).
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class MenosZoomCamara
{
    private const string Esta = "Assets/Editor/MenosZoomCamara.cs";
    private const float Tamano = 7.02f, TamanoIntro = 8.7f;

    static MenosZoomCamara()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Aplicar;
    }

    private static Camera Cam()
    {
        var seg = Object.FindFirstObjectByType<CamaraSeguidora>(FindObjectsInactive.Include);
        return seg != null ? seg.GetComponent<Camera>() : null;
    }

    private static bool Listo() { var c = Cam(); return c != null && Mathf.Approximately(c.orthographicSize, Tamano); }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        var cam = Cam();
        if (cam == null) { Debug.LogWarning("[Camara] No encontre la camara con CamaraSeguidora."); return; }
        bool teniaCambios = escena.isDirty;

        Undo.RecordObject(cam, "Menos zoom");
        float antes = cam.orthographicSize;
        cam.orthographicSize = Tamano;

        var intro = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (intro != null)
        {
            var so = new SerializedObject(intro);
            so.FindProperty("tamanoEncuadreIntro").floatValue = TamanoIntro;
            so.ApplyModifiedProperties();
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Camara] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Camara] Tamano de la camara " + antes + " -> " + Tamano + " (intro " + TamanoIntro + ").");
    }
}
