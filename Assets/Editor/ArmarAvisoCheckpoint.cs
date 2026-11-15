using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Pone en la escena el aviso "Nuevo checkpoint" que aparece al tomar un checkpoint (AvisoCheckpoint), con las
// imagenes de Caro (Checkpoint_titulo y Checkpoint_ornamento). Si ya esta, le vuelve a poner imagenes y medidas.
// Menu:  Pomelo -> Avisos -> Armar aviso de checkpoint
public static class ArmarAvisoCheckpoint
{
    private const string RutaFuente = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string RutaOrnamento = "Assets/_UI/Spawnpoint/Checkpoint_ornamento.png";
    private const string RutaTitulo = "Assets/_UI/Spawnpoint/Checkpoint_titulo.png";

    [MenuItem("Pomelo/Avisos/Armar aviso de checkpoint")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Para el Play antes de armar el aviso."); return; }

        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuente);
        if (fuente == null) { Debug.LogError("No encontre la fuente " + RutaFuente); return; }

        var aviso = Object.FindFirstObjectByType<AvisoCheckpoint>(FindObjectsInactive.Include);
        if (aviso == null)
        {
            var go = new GameObject("Aviso checkpoint", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(go, "Armar aviso de checkpoint");
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var escalador = go.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.matchWidthOrHeight = 0.5f;
            aviso = go.AddComponent<AvisoCheckpoint>();
        }

        var so = new SerializedObject(aviso);
        so.FindProperty("fuente").objectReferenceValue = fuente;
        so.FindProperty("texto").stringValue = "Nuevo checkpoint";

        // el formato de Caro: titulo arriba y ornamento abajo, las dos imagenes al 68 %, a la altura del piso
        so.FindProperty("titulo").objectReferenceValue = PrimerSprite(RutaTitulo);
        so.FindProperty("ornamento").objectReferenceValue = PrimerSprite(RutaOrnamento);
        so.FindProperty("escala").floatValue = 0.68f;
        so.FindProperty("distanciaTituloOrnamento").floatValue = 44f;
        so.FindProperty("alturaDesdeAbajo").floatValue = 296f;   // la linea roja de Caro, 4 px mas abajo
        // dura 1.5 s en total: aparece, se queda y se va
        so.FindProperty("duracionEntrada").floatValue = 0.25f;
        so.FindProperty("duracionQuieto").floatValue = 0.9f;
        so.FindProperty("duracionSalida").floatValue = 0.35f;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = aviso.gameObject;
        Debug.Log("Aviso de checkpoint listo (Checkpoint_titulo + Checkpoint_ornamento). Guarda la escena con Ctrl+S.");
    }

    // Unity habia importado las imagenes como "Multiple" y partio el ornamento en 3 pedazos (volutas y flor):
    // se pasan a "Single" para usar la imagen entera
    private static Sprite PrimerSprite(string ruta)
    {
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador != null && (importador.textureType != TextureImporterType.Sprite || importador.spriteImportMode != SpriteImportMode.Single))
        {
            importador.textureType = TextureImporterType.Sprite;
            importador.spriteImportMode = SpriteImportMode.Single;
            importador.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite == null) Debug.LogWarning("No encontre el sprite de " + ruta);
        return sprite;
    }
}
