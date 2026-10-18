using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Pone una sola vez el agua venenosa en los dos lagos del mapa (los rectangulos verde claro de 4897 x 488 px
// del Figma, en (28451, 6542) y (23911, 11132) del frame del mapa):
//  - las espinas violetas con flores de loto (espinas_agua_venenosa.png), que asoman 102 px por arriba del lago;
//  - encima, el agua violeta del Figma (#9C4791 al 43 % con desenfoque de fondo progresivo de 0 a 18.2 px):
//    Unity no tiene ese desenfoque, asi que se exporto ya aplicado sobre las espinas (agua_violeta_blur.png).
// La escena hay que guardarla (Ctrl+S) si tenia otros cambios; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class PonerAguaVenenosa
{
    private const string Esta = "Assets/Editor/PonerAguaVenenosa.cs";
    private const string Carpeta = "Assets/_entorno/_AguaVenenosa/";
    private const string Nombre = "Agua venenosa";

    private static readonly Vector2[] Lagos = { new Vector2(28451f, 6542f), new Vector2(23911f, 11132f) };

    // Figma (px del frame del mapa) -> Unity
    private static Vector3 AUnity(float fx, float fy) => new Vector3((fx - 12883f) / 100f - 31.98f, -(fy - 10621f) / 100f - 30.27f, 0f);

    static PonerAguaVenenosa()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Poner;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || GameObject.Find(Nombre) == null) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static Sprite Importar(string archivo)
    {
        string ruta = Carpeta + archivo;
        var ti = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 8192;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.TopLeft;
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void Poner()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || GameObject.Find(Nombre) != null) return;
        bool teniaCambios = escena.isDirty;

        var espinas = Importar("espinas_agua_venenosa.png");
        var agua = Importar("agua_violeta_blur.png");
        if (espinas == null || agua == null) { Debug.LogError("[Agua] No encontre los PNG en " + Carpeta); return; }

        var raiz = new GameObject(Nombre);
        Undo.RegisterCreatedObjectUndo(raiz, "Poner agua venenosa");
        for (int i = 0; i < Lagos.Length; i++)
        {
            var lago = new GameObject("Lago " + (i + 1)).transform;
            lago.SetParent(raiz.transform, false);

            // espinas: el PNG empieza 2 px a la izquierda y 102 px arriba del lago
            var e = new GameObject("espinas");
            e.transform.SetParent(lago, false);
            e.transform.position = AUnity(Lagos[i].x - 2f, Lagos[i].y - 102f);
            var sre = e.AddComponent<SpriteRenderer>();
            sre.sprite = espinas;
            sre.sortingOrder = 3;   // por delante del mapa (-2 a 1) y de la columna espiral (2)

            // agua violeta desenfocada, justo del tamano del lago y encima de las espinas
            var a = new GameObject("agua violeta");
            a.transform.SetParent(lago, false);
            a.transform.position = AUnity(Lagos[i].x, Lagos[i].y);
            var sra = a.AddComponent<SpriteRenderer>();
            sra.sprite = agua;
            sra.sortingOrder = 4;
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Agua] La escena tenia otros cambios sin guardar: guardala (Ctrl+S) para que quede el agua venenosa.");
        Debug.Log("[Agua] Agua venenosa puesta en los 2 lagos.");
    }
}
