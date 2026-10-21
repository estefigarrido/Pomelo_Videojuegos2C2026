using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez: a cada lago del agua venenosa le pone la linea brillante de la superficie
// (agua_brillo_superficie.png estirada al ancho del lago, con el nucleo justo en el borde de arriba del agua)
// y el componente AguaVenenosaEfectos (titileo + burbujitas, agua_burbuja.png).
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class AgregarBrilloAguaVenenosa
{
    private const string Esta = "Assets/Editor/AgregarBrilloAguaVenenosa.cs";
    private const string Carpeta = "Assets/_entorno/_AguaVenenosa y pinchos/";

    static AgregarBrilloAguaVenenosa()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Agregar;
    }

    private static Transform[] Lagos()
    {
        var raiz = GameObject.Find("Agua venenosa");
        if (raiz == null) return new Transform[0];
        var l = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in raiz.transform) if (t.name.StartsWith("Lago")) l.Add(t);
        return l.ToArray();
    }

    private static bool Listo()
    {
        var ls = Lagos();
        if (ls.Length == 0) return false;
        foreach (var l in ls) if (l.GetComponent<AguaVenenosaEfectos>() == null) return false;
        return true;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static Sprite Importar(string archivo, bool arribaIzquierda, bool sinComprimir)
    {
        var ti = AssetImporter.GetAtPath(Carpeta + archivo) as TextureImporter;
        if (ti == null) return null;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        if (sinComprimir) ti.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
        s.spriteAlignment = (int)(arribaIzquierda ? SpriteAlignment.TopLeft : SpriteAlignment.Center);
        ti.SetTextureSettings(s);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + archivo);
    }

    private static void Agregar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        var lagos = Lagos();
        if (lagos.Length == 0) { Debug.LogWarning("[Agua] No encontre 'Agua venenosa/Lago N'."); return; }
        bool teniaCambios = escena.isDirty;

        var brillo = Importar("agua_brillo_superficie.png", true, true);
        var burbuja = Importar("agua_burbuja.png", false, true);
        if (brillo == null || burbuja == null) { Debug.LogError("[Agua] Faltan agua_brillo_superficie.png o agua_burbuja.png en " + Carpeta); return; }

        foreach (var lago in lagos)
        {
            var aguaT = lago.Find("agua violeta");
            var agua = aguaT != null ? aguaT.GetComponent<SpriteRenderer>() : null;
            if (agua == null) continue;
            Undo.RegisterFullObjectHierarchyUndo(lago.gameObject, "Brillo agua venenosa");
            Bounds b = agua.bounds;

            var viejo = lago.Find("brillo superficie");
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
            var go = new GameObject("brillo superficie");
            go.transform.SetParent(lago, false);
            // la tira mide 8 x 160 px y el nucleo de la linea esta 48 px abajo de su borde de arriba
            go.transform.position = new Vector3(b.min.x, b.max.y + 0.48f, agua.transform.position.z - 0.01f);
            go.transform.localScale = new Vector3(b.size.x / (brillo.rect.width / brillo.pixelsPerUnit), 1f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = brillo;
            sr.sharedMaterial = agua.sharedMaterial;
            sr.sortingLayerID = agua.sortingLayerID;
            sr.sortingOrder = agua.sortingOrder;

            var ef = lago.GetComponent<AguaVenenosaEfectos>();
            if (ef == null) ef = lago.gameObject.AddComponent<AguaVenenosaEfectos>();
            var so = new SerializedObject(ef);
            so.FindProperty("agua").objectReferenceValue = agua;
            so.FindProperty("brilloSuperficie").objectReferenceValue = sr;
            so.FindProperty("burbuja").objectReferenceValue = burbuja;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Agua] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Agua] Brillo de la superficie y burbujitas agregados a " + lagos.Length + " lagos.");
    }
}
