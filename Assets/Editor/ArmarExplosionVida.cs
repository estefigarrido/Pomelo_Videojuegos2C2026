using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Arma la animacion de cuando se pierde una vida y la deja en Assets/_UI/Barravida_y_sabiduria/Explosion_vida:
//  - destello_vida.png: el destello redondo y suave de la explosion
//  - particula_vida.png: una particula con forma de estrellita de 4 puntas (como la de los iconos de vida)
//  - Explosion_vida.prefab: la animacion (ExplosionVida), que se ajusta desde el prefab
// y se la conecta al HUD de la vida (HUDVidaSabiduria).
// Para verla sin tener que perder una vida: con el juego en Play, clic derecho en el componente
// HUDVidaSabiduria (objeto HUD_vida_sabiduria) -> "Probar explosion de vida".
// Menu:  Pomelo -> HUD -> Armar explosion de vida
public static class ArmarExplosionVida
{
    private const string Carpeta = "Assets/_UI/Barravida_y_sabiduria/Explosion_vida/";
    private const string RutaDestello = Carpeta + "destello_vida.png";
    private const string RutaParticula = Carpeta + "particula_vida.png";
    private const string RutaPrefab = Carpeta + "Explosion_vida.prefab";

    [MenuItem("Pomelo/HUD/Armar explosion de vida")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de armar la explosion de vida.");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/_UI/Barravida_y_sabiduria"))
        {
            Debug.LogError("No encontre la carpeta Assets/_UI/Barravida_y_sabiduria.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Carpeta.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/_UI/Barravida_y_sabiduria", "Explosion_vida");

        // 1. dibujos (si ya existen, se usan los que hay: se pueden reemplazar por otros con el mismo nombre)
        if (!File.Exists(RutaDestello)) Guardar(RutaDestello, Destello(96));
        if (!File.Exists(RutaParticula)) Guardar(RutaParticula, Estrellita(48));
        var destello = AssetDatabase.LoadAssetAtPath<Sprite>(RutaDestello);
        var particula = AssetDatabase.LoadAssetAtPath<Sprite>(RutaParticula);

        // 2. prefab (si ya existe, solo se le actualizan los dibujos: los ajustes que tenga se respetan)
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab == null)
        {
            var go = new GameObject("Explosion_vida", typeof(RectTransform), typeof(RectMask2D), typeof(ExplosionVida));
            prefab = PrefabUtility.SaveAsPrefabAsset(go, RutaPrefab);
            Object.DestroyImmediate(go);
        }
        var explosion = prefab.GetComponent<ExplosionVida>();
        var soE = new SerializedObject(explosion);
        soE.FindProperty("destello").objectReferenceValue = destello;
        soE.FindProperty("particula").objectReferenceValue = particula;
        soE.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        // 3. conectarla al HUD de la vida
        var hud = Object.FindFirstObjectByType<HUDVidaSabiduria>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogWarning("Arme la explosion en " + RutaPrefab + ", pero no encontre el HUD de la vida en la escena para conectarla.");
            return;
        }
        var soH = new SerializedObject(hud);
        soH.FindProperty("explosionVida").objectReferenceValue = explosion;
        soH.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        Selection.activeObject = prefab;
        Debug.Log("Explosion de vida armada en " + Carpeta + " y conectada al HUD. Guarda la escena con Ctrl+S. " +
                  "Para verla: Play, y clic derecho en el componente HUDVidaSabiduria -> Probar explosion de vida.");
    }

    // ---------- dibujos ----------

    // circulo blanco suave: fuerte en el centro y se va apagando hacia el borde
    private static Texture2D Destello(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * n / 2f) / (n / 2f);
                float a = Mathf.Clamp01(1f - r);
                a = a * a * (0.6f + 0.4f * a);   // nucleo brillante y borde muy suave
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // estrellita de 4 puntas con un brillito suave alrededor
    private static Texture2D Estrellita(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        const int SS = 3;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float suma = 0f;
                for (int j = 0; j < SS; j++)
                    for (int i = 0; i < SS; i++)
                    {
                        float u = ((x + (i + 0.5f) / SS) / n) * 2f - 1f;
                        float v = ((y + (j + 0.5f) / SS) / n) * 2f - 1f;
                        // forma de estrella de 4 puntas: |u|^0.5 + |v|^0.5 <= 1 (curvas hacia adentro)
                        float estrella = Mathf.Sqrt(Mathf.Abs(u)) + Mathf.Sqrt(Mathf.Abs(v)) <= 0.95f ? 1f : 0f;
                        float r = Mathf.Sqrt(u * u + v * v);
                        float brillo = Mathf.Clamp01(1f - r / 0.55f);
                        suma += Mathf.Max(estrella, brillo * brillo * 0.45f);
                    }
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * suma / (SS * SS)));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    private static void Guardar(string ruta, Texture2D tex)
    {
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }
}
