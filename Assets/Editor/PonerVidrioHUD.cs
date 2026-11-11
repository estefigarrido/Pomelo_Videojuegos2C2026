using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Le pone efecto de vidrio esmerilado al HUD de la vida (arriba a la izquierda):
//  - el circulo (fondo_circulo): vidrio, con el fondo desenfocado, un tono verdoso y un brillo suave arriba
//  - la barra de las vidas (fondo_barra): fondo desenfocado y apenas oscurecido
// Para eso:
//  1. prende en el Renderer 2D la copia de la escena (Camera Sorting Layer Texture) hasta la capa "Efectos",
//     a 1/4 de resolucion (ya sale suave y es liviana). El shader del vidrio lee esa copia.
//  2. crea los materiales en Assets/_UI/Barravida_y_sabiduria/Vidrio (si ya existen no los toca: se pueden ajustar ahi)
//  3. se los pone a las Image fondo_circulo y fondo_barra
// Menu:  Pomelo -> HUD -> Poner vidrio en la vida
public static class PonerVidrioHUD
{
    private const string RutaRenderer = "Assets/Settings/Renderer2D.asset";
    private const string Carpeta = "Assets/_UI/Barravida_y_sabiduria/Vidrio/";
    private const string RutaShader = Carpeta + "UI_Vidrio.shader";
    private const string RutaMatCirculo = Carpeta + "HUD_vidrio_circulo.mat";
    private const string RutaMatBarra = Carpeta + "HUD_desenfoque_barra.mat";
    private const string CapaHasta = "Efectos";   // la copia incluye todo el juego hasta esta capa (no la interfaz)

    [MenuItem("Pomelo/HUD/Poner vidrio en la vida")]
    public static void Poner()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de poner el vidrio.");
            return;
        }

        // 1. copia de la escena en el Renderer 2D
        var renderer = AssetDatabase.LoadMainAssetAtPath(RutaRenderer);
        if (renderer == null) { Debug.LogError("No encontre " + RutaRenderer); return; }
        var so = new SerializedObject(renderer);
        var usar = so.FindProperty("m_UseCameraSortingLayersTexture");
        var hasta = so.FindProperty("m_CameraSortingLayersTextureBound");
        var reduccion = so.FindProperty("m_CameraSortingLayerDownsamplingMethod");
        if (usar == null || hasta == null || reduccion == null)
        {
            Debug.LogError("El Renderer 2D no tiene la opcion Camera Sorting Layer Texture (version distinta de URP).");
            return;
        }
        usar.boolValue = true;
        hasta.intValue = SortingLayer.NameToID(CapaHasta);
        reduccion.intValue = 3;   // 4x Bilinear: un cuarto de resolucion
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        // 2. materiales
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShader);
        if (shader == null) { Debug.LogError("No encontre el shader " + RutaShader); return; }
        var circulo = Material(RutaMatCirculo, shader, desenfoque: 12.6f, color: new Color(0.10f, 0.17f, 0.14f, 0.35f), brillo: 0.18f, grano: 0.4f);
        var barra = Material(RutaMatBarra, shader, desenfoque: 9f, color: new Color(0.04f, 0.07f, 0.06f, 0.30f), brillo: 0f, grano: 0.3f);

        // 3. ponerselos a los fondos del HUD
        int puestos = Asignar("fondo_circulo", circulo) + Asignar("fondo_barra", barra);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Vidrio puesto en " + puestos + " de 2 fondos del HUD (circulo y barra de vidas). " +
                  "Los materiales para ajustarlo estan en " + Carpeta + ". Guarda la escena con Ctrl+S.");
    }

    private static Material Material(string ruta, Shader shader, float desenfoque, Color color, float brillo, float grano)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat != null) return mat;   // ya existe: se respetan los ajustes que tenga
        mat = new Material(shader);
        mat.SetFloat("_Desenfoque", desenfoque);
        mat.SetColor("_ColorVidrio", color);
        mat.SetFloat("_Brillo", brillo);
        mat.SetFloat("_Grano", grano);
        mat.SetFloat("_UmbralForma", 0.2f);
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    private static int Asignar(string nombre, Material mat)
    {
        foreach (var img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.name != nombre) continue;
            var padre = img.transform.parent;
            bool esDelHudDeVida = img.GetComponentInParent<HUDVidaSabiduria>(true) != null ||
                                  (padre != null && padre.name == "HUD_vida_sabiduria");
            if (!esDelHudDeVida) continue;
            Undo.RecordObject(img, "Poner vidrio en la vida");
            img.material = mat;
            img.color = Color.white;
            EditorUtility.SetDirty(img);
            return 1;
        }
        Debug.LogWarning("No encontre la Image '" + nombre + "' en el HUD de la vida.");
        return 0;
    }
}
