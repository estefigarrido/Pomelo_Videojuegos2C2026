using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Pone los puntitos verdes del selector de poder (selector_poder_A / B) en la barra de poderes del HUD,
// arriba del circulo de la hoja, y los conecta con HUDBarraPoderes para que cambien con V.
// Menu:  Pomelo -> Poner el selector de poder
public static class PonerSelectorPoder
{
    private const string RutaA = "Assets/_UI/selector_poder_A.png";
    private const string RutaB = "Assets/_UI/selector_poder_B.png";
    private const string NombreObjeto = "selector_poder";

    // Lugar de la esquina de arriba a la izquierda, en pixeles de HUD_barra_poderes.png (1066 x 417),
    // igual que las otras piezas de la barra. Sale de medir el Figma: los puntitos quedan arriba a la
    // izquierda del circulo de la hoja, donde termina el ornamento. Si hay que correrlo, se mueve el objeto.
    private const float Izquierda = 964f;
    private const float Arriba = 35f;

    [MenuItem("Pomelo/Poner el selector de poder")]
    public static void Poner()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de poner el selector de poder.");
            return;
        }

        // 1. que cada PNG sea un solo sprite (Unity los habia cortado en dos, uno por puntito)
        var spriteA = ImportarComoUnSoloSprite(RutaA);
        var spriteB = ImportarComoUnSoloSprite(RutaB);
        if (spriteA == null || spriteB == null) return;

        var hud = Object.FindFirstObjectByType<HUDBarraPoderes>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogError("No encontre la barra de poderes (HUDBarraPoderes) en la escena.");
            return;
        }
        var barra = (RectTransform)hud.transform;

        // 2. si ya estaba puesto, se reemplaza
        var viejo = barra.Find(NombreObjeto);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        var go = new GameObject(NombreObjeto, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Poner el selector de poder");
        var rt = (RectTransform)go.transform;
        rt.SetParent(barra, false);
        rt.localScale = Vector3.one;
        // como las otras piezas: anclado arriba a la derecha de la barra y medido desde su esquina
        rt.anchorMin = rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(Izquierda - barra.rect.width, -Arriba);
        rt.sizeDelta = spriteA.rect.size;

        var imagen = go.GetComponent<Image>();
        imagen.sprite = spriteA;
        imagen.raycastTarget = false;

        // 3. conectarlo con el HUD
        var so = new SerializedObject(hud);
        so.FindProperty("selectorPoder").objectReferenceValue = imagen;
        var sprites = so.FindProperty("spritesSelector");
        sprites.arraySize = 2;
        sprites.GetArrayElementAtIndex(0).objectReferenceValue = spriteA;
        sprites.GetArrayElementAtIndex(1).objectReferenceValue = spriteB;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Selection.activeGameObject = go;
        Debug.Log("Selector de poder puesto en la barra de poderes (A = latigo, B = sabiduria). Guarda la escena con Ctrl+S.");
    }

    private static Sprite ImportarComoUnSoloSprite(string ruta)
    {
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null)
        {
            Debug.LogError("No encontre " + ruta);
            return null;
        }
        if (importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Single)
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.alphaIsTransparency = true;
            importador.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
