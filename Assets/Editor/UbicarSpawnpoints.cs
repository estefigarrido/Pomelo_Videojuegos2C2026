using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez: pone los checkpoints (Spawnpoint_icono_0 ...) donde el Figma marca los spawnpoints
// (los circulos verde oliva de "mapa y assets", con el cuadradito apoyado en el piso).
// Los 4 primeros van a las 4 marcas; los que sobran se desactivan (no se borran). El de inicio no se toca.
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class UbicarSpawnpoints
{
    private const string Esta = "Assets/Editor/UbicarSpawnpoints.cs";

    // centro del cuadradito de cada marca (px del frame del mapa)
    private static readonly Vector2[] Marcas =
    {
        new Vector2(18899.5f, 10567.5f),   // piso de abajo, a la derecha de los arcos
        new Vector2(33345f, 9849.5f),      // cima de la escalera de la columna gotica
        new Vector2(19386.5f, 6452.5f),    // piso del medio, a la derecha
        new Vector2(15680.5f, 6590.5f),    // piso del medio, a la izquierda
    };

    private static Vector3 AUnity(Vector2 f) => new Vector3((f.x - 12883f) / 100f - 31.98f, -(f.y - 10621f) / 100f - 30.27f, 0f);

    static UbicarSpawnpoints()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Ubicar;
    }

    private static Checkpoint[] Todos() => Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .Where(c => !c.name.Contains("inicio")).OrderBy(c => c.name).ToArray();

    private static bool Listo()
    {
        var cs = Todos();
        if (cs.Length < Marcas.Length) return false;
        for (int i = 0; i < Marcas.Length; i++) if (Vector2.Distance(cs[i].transform.position, AUnity(Marcas[i])) > 0.01f) return false;
        return true;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Ubicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        var cs = Todos();
        if (cs.Length == 0) { Debug.LogWarning("[Spawn] No encontre checkpoints."); return; }
        bool teniaCambios = escena.isDirty;

        for (int i = 0; i < cs.Length; i++)
        {
            Undo.RecordObject(cs[i].transform, "Ubicar spawnpoint");
            Undo.RecordObject(cs[i].gameObject, "Ubicar spawnpoint");
            if (i < Marcas.Length)
            {
                cs[i].transform.position = AUnity(Marcas[i]);
                cs[i].gameObject.SetActive(true);
                Debug.Log("[Spawn] " + cs[i].name + " -> " + cs[i].transform.position);
            }
            else
            {
                cs[i].gameObject.SetActive(false);
                Debug.Log("[Spawn] " + cs[i].name + " desactivado (el Figma marca solo " + Marcas.Length + ").");
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Spawn] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
    }
}
