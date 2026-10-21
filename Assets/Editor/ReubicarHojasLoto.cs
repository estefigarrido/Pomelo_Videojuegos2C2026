using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez: pone 5 hojas de loto por lago como en el Figma, sobre el agua venenosa nueva (Group 187).
//  - tamano: 399.5 x 64 px (escala 0.8 de la hoja de 500 x 80);
//  - altura: 9 px abajo del borde de arriba de las espinas (que asoman 69 px por arriba del lago) => 60 px arriba del lago;
//  - x desde el borde izquierdo del lago: 413, 1315, 2207.5, 3081 y 4000;
//  - orientacion: en el Figma la 2da, 4ta y 5ta van espejadas; el PNG de Unity ya esta espejado respecto
//    del componente, asi que en Unity se espejan la 1ra y la 3ra.
// Las hojas que ya estaban (con HojaLotoQueSeHunde y su collider) se mueven; la 5ta es una copia.
// Como quedan 39 px mas abajo que antes, caen 0.39 u menos hasta el fondo.
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class ReubicarHojasLoto
{
    private const string Esta = "Assets/Editor/ReubicarHojasLoto.cs";
    private static readonly Vector2[] Lagos = { new Vector2(28451f, 6542f), new Vector2(23911f, 11132f) };
    private static readonly float[] HojasX = { 413f, 1315f, 2207.5f, 3081f, 4000f };
    private static readonly bool[] Espejo = { true, false, true, false, false };
    private const float Ancho = 399.5f, Alto = 64f, ArribaDelLago = 60f;
    private const float LineaCentroX = 5.25f;   // px: el collider (vena del medio) esta corrido hacia la punta en el PNG

    private static Vector3 AUnity(float fx, float fy) => new Vector3((fx - 12883f) / 100f - 31.98f, -(fy - 10621f) / 100f - 30.27f, 0f);

    static ReubicarHojasLoto()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Reubicar;
    }

    private static Transform Grupo(int i)
    {
        var raiz = GameObject.Find("Agua venenosa");
        var lago = raiz != null ? raiz.transform.Find("Lago " + (i + 1)) : null;
        return lago != null ? lago.Find("hojas de loto") : null;
    }

    private static bool Listo()
    {
        for (int i = 0; i < Lagos.Length; i++) { var g = Grupo(i); if (g == null || g.childCount != HojasX.Length) return false; }
        return true;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Reubicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        if (Grupo(0) == null) { Debug.LogWarning("[Hojas] No encontre 'Agua venenosa/Lago N/hojas de loto'."); return; }
        bool teniaCambios = escena.isDirty;

        for (int i = 0; i < Lagos.Length; i++)
        {
            var g = Grupo(i); if (g == null || g.childCount == 0) continue;
            Undo.RegisterFullObjectHierarchyUndo(g.gameObject, "Reubicar hojas");
            while (g.childCount < HojasX.Length)
            {
                var copia = Object.Instantiate(g.GetChild(0).gameObject, g);
                Undo.RegisterCreatedObjectUndo(copia, "Hoja de loto");
            }
            for (int k = 0; k < HojasX.Length; k++)
            {
                var h = g.GetChild(k);
                h.name = "hoja de loto " + (k + 1);
                var sr = h.GetComponent<SpriteRenderer>();
                float escala = Ancho / 100f / (sr.sprite.rect.width / sr.sprite.pixelsPerUnit);
                h.localScale = new Vector3(escala, escala, 1f);
                h.rotation = Quaternion.identity;
                h.position = AUnity(Lagos[i].x + HojasX[k] + Ancho / 2f, Lagos[i].y - ArribaDelLago + Alto / 2f);
                sr.flipX = Espejo[k];
                var col = h.GetComponent<BoxCollider2D>();
                if (col != null) col.offset = new Vector2((Espejo[k] ? -LineaCentroX : LineaCentroX) / 100f, col.offset.y);
                var hunde = h.GetComponent<HojaLotoQueSeHunde>();
                if (hunde != null)
                {
                    var so = new SerializedObject(hunde);
                    var caida = so.FindProperty("caida");
                    if (caida != null && caida.floatValue > 3.5f) caida.floatValue -= 0.39f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Hojas] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Hojas] 5 hojas de loto por lago, como en el Figma.");
    }
}
