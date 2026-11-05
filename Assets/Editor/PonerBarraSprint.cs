using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Pone la barra del sprint abajo en el centro del HUD (como en el Figma) y la conecta con HUDBarraSprint.
// Menu:  Pomelo -> Poner la barra de sprint
public static class PonerBarraSprint
{
    private const string RutaMarco = "Assets/_UI/HUD_sprint_marco.png";
    private const string RutaRelleno = "Assets/_UI/HUD_sprint_relleno.png";
    private const string NombreObjeto = "HUD_barra_sprint";

    // Medidas en pixeles del Canvas (1920 x 1080), sacadas de la captura del juego con la UI del Figma:
    // la barra mide 718 x 14 y su centro queda a 83 px del borde de abajo.
    // Adentro, la parte blanca mide 6 de alto y queda separada 4 px del borde.
    private static readonly Vector2 Tamano = new Vector2(718f, 14f);
    private const float AlturaDesdeAbajo = 83f;
    private const float Margen = 4f;
    private const float AltoRelleno = 6f;

    // Los PNG estan dibujados al doble de tamano para que se vean nitidos: el borde redondeado
    // ocupa la mitad del alto (14 px del marco y 6 px del relleno, en el PNG).
    private static readonly Vector4 BordeMarco = new Vector4(14f, 13f, 14f, 13f);
    private static readonly Vector4 BordeRelleno = new Vector4(6f, 5f, 6f, 5f);
    private const float Resolucion = 2f;

    [MenuItem("Pomelo/Poner la barra de sprint")]
    public static void Poner()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de poner la barra de sprint.");
            return;
        }

        var marco = ImportarEstirable(RutaMarco, BordeMarco);
        var spriteRelleno = ImportarEstirable(RutaRelleno, BordeRelleno);
        if (marco == null || spriteRelleno == null) return;

        // va en el mismo Canvas que el resto del HUD
        var hud = Object.FindFirstObjectByType<HUDBarraPoderes>(FindObjectsInactive.Include);
        var canvas = hud != null ? hud.GetComponentInParent<Canvas>(true) : Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No encontre el Canvas del HUD en la escena.");
            return;
        }

        // si ya estaba puesta, se reemplaza
        var vieja = canvas.transform.Find(NombreObjeto);
        if (vieja != null) Undo.DestroyObjectImmediate(vieja.gameObject);

        // la barra: anclada abajo en el centro
        var barra = new GameObject(NombreObjeto, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(barra, "Poner la barra de sprint");
        var rt = (RectTransform)barra.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, AlturaDesdeAbajo);
        rt.sizeDelta = Tamano;
        ConfigurarImagen(barra.GetComponent<Image>(), marco);

        // la parte de adentro: crece desde la izquierda
        var relleno = new GameObject("relleno", typeof(RectTransform), typeof(Image));
        var rr = (RectTransform)relleno.transform;
        rr.SetParent(rt, false);
        rr.anchorMin = rr.anchorMax = new Vector2(0f, 0.5f);
        rr.pivot = new Vector2(0f, 0.5f);
        rr.anchoredPosition = new Vector2(Margen, 0f);
        rr.sizeDelta = new Vector2(Tamano.x - Margen * 2f, AltoRelleno);
        ConfigurarImagen(relleno.GetComponent<Image>(), spriteRelleno);

        var script = barra.AddComponent<HUDBarraSprint>();
        var so = new SerializedObject(script);
        so.FindProperty("relleno").objectReferenceValue = rr;
        so.FindProperty("margen").floatValue = Margen;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = barra;
        Debug.Log("Barra de sprint puesta abajo del HUD. En el juego arranca invisible y aparece al sprintear. Guarda la escena con Ctrl+S.");
    }

    private static void ConfigurarImagen(Image imagen, Sprite sprite)
    {
        imagen.sprite = sprite;
        imagen.type = Image.Type.Sliced;
        imagen.pixelsPerUnitMultiplier = Resolucion;
        imagen.raycastTarget = false;
    }

    // Sprite con bordes, asi se estira a lo largo sin deformar las puntas redondeadas
    private static Sprite ImportarEstirable(string ruta, Vector4 borde)
    {
        AssetDatabase.ImportAsset(ruta);
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null)
        {
            Debug.LogError("No encontre " + ruta);
            return null;
        }
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.spriteBorder = borde;
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
