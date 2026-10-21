using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez: la version nueva de las espinas del agua venenosa (Group 187 del Figma, 4913 x 525)
// asoma 69 px por arriba del lago (la anterior 102) y empieza justo en el borde izquierdo del lago (antes 2 px antes).
// Acomoda las espinas de los dos lagos y baja las hojas de loto lo mismo (33 px), para que sigan apoyadas
// 3 px abajo del borde de arriba de las espinas, como antes.
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class AcomodarAguaVenenosa2
{
    private const string Esta = "Assets/Editor/AcomodarAguaVenenosa2.cs";
    private static readonly Vector2[] Lagos = { new Vector2(28451f, 6542f), new Vector2(23911f, 11132f) };
    private static Vector3 AUnity(float fx, float fy) => new Vector3((fx - 12883f) / 100f - 31.98f, -(fy - 10621f) / 100f - 30.27f, 0f);

    static AcomodarAguaVenenosa2()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Acomodar;
    }

    private static Transform Lago(int i)
    {
        var raiz = GameObject.Find("Agua venenosa");
        return raiz != null ? raiz.transform.Find("Lago " + (i + 1)) : null;
    }

    private static bool Listo()
    {
        var l = Lago(0);
        var e = l != null ? l.Find("espinas") : null;
        return e != null && Vector2.Distance(e.position, AUnity(Lagos[0].x, Lagos[0].y - 69f)) < 0.005f;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Acomodar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        if (Lago(0) == null) { Debug.LogWarning("[Agua] No encontre 'Agua venenosa/Lago N'."); return; }
        bool teniaCambios = escena.isDirty;

        for (int i = 0; i < Lagos.Length; i++)
        {
            var lago = Lago(i); if (lago == null) continue;
            var e = lago.Find("espinas");
            if (e != null) { Undo.RecordObject(e, "Acomodar espinas"); e.position = AUnity(Lagos[i].x, Lagos[i].y - 69f); }
            // las hojas las pone ReubicarHojasLoto, en su lugar exacto del Figma
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Agua] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Agua] Espinas nuevas acomodadas y hojas de loto bajadas 0.33 u en los 2 lagos.");
    }
}
