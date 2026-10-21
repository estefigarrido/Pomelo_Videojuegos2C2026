using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez, en cada lago del agua venenosa:
//  - el agua violeta pasa a ser mortal: BoxCollider2D trigger del tamano del agua + AguaVenenosaMortal
//    (saca una vida y la chica reaparece en el punto de reaparicion del lago);
//  - punto de reaparicion en el piso de al lado del lago (px del Figma, centro del cuerpo de la chica):
//      lago 1 (arriba, x 28451): en x 28200, piso en y 6464;
//      lago 2 (abajo, x 23911): en x 23217, al final del piso de los arcos (termina en x 23417, piso en y 10621);
//  - resplandor de adentro del agua (agua_glow_interior.png estirada al tamano del agua);
//  - muchas mas burbujas (14 por segundo suben, 5 flotan).
// Si la escena no tenia otros cambios la guarda; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class ArmarAguaVenenosaMortal
{
    private const string Esta = "Assets/Editor/ArmarAguaVenenosaMortal.cs";
    private const string Carpeta = "Assets/_entorno/_AguaVenenosa y pinchos/";
    private static readonly Vector2[] Reaparicion = { new Vector2(28200f, 6464f - 125f), new Vector2(23217f, 10621f - 125f) };

    private static Vector3 AUnity(Vector2 f) => new Vector3((f.x - 12883f) / 100f - 31.98f, -(f.y - 10621f) / 100f - 30.27f, 0f);

    static ArmarAguaVenenosaMortal()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Armar;
    }

    private static Transform Lago(int i)
    {
        var raiz = GameObject.Find("Agua venenosa");
        return raiz != null ? raiz.transform.Find("Lago " + (i + 1)) : null;
    }

    private static bool Listo()
    {
        for (int i = 0; i < 2; i++)
        {
            var l = Lago(i); if (l == null) return false;
            var a = l.Find("agua violeta"); if (a == null || a.GetComponent<AguaVenenosaMortal>() == null) return false;
            if (l.Find("glow interior") == null) return false;
        }
        return true;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Armar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        if (Lago(0) == null) { Debug.LogWarning("[Agua] No encontre 'Agua venenosa/Lago N'."); return; }
        bool teniaCambios = escena.isDirty;

        var ti = AssetImporter.GetAtPath(Carpeta + "agua_glow_interior.png") as TextureImporter;
        ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 100f;
        ti.mipmapEnabled = false; ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        var s = new TextureImporterSettings(); ti.ReadTextureSettings(s); s.spriteAlignment = (int)SpriteAlignment.TopLeft; ti.SetTextureSettings(s);
        ti.SaveAndReimport();
        var glow = AssetDatabase.LoadAssetAtPath<Sprite>(Carpeta + "agua_glow_interior.png");

        for (int i = 0; i < 2; i++)
        {
            var lago = Lago(i); if (lago == null) continue;
            var aguaT = lago.Find("agua violeta"); if (aguaT == null) continue;
            var agua = aguaT.GetComponent<SpriteRenderer>();
            Undo.RegisterFullObjectHierarchyUndo(lago.gameObject, "Agua venenosa mortal");
            Bounds b = agua.bounds;

            // punto de reaparicion
            var punto = lago.Find("reaparicion");
            if (punto == null) { punto = new GameObject("reaparicion").transform; punto.SetParent(lago, false); }
            punto.position = AUnity(Reaparicion[i]);

            // agua mortal: collider del tamano del agua (el sprite tiene el pivote arriba a la izquierda)
            var col = aguaT.GetComponent<BoxCollider2D>();
            if (col == null) col = aguaT.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            var esc = aguaT.lossyScale;
            col.size = new Vector2(b.size.x / Mathf.Abs(esc.x), b.size.y / Mathf.Abs(esc.y));
            col.offset = new Vector2(col.size.x / 2f, -col.size.y / 2f);
            var mortal = aguaT.GetComponent<AguaVenenosaMortal>();
            if (mortal == null) mortal = aguaT.gameObject.AddComponent<AguaVenenosaMortal>();
            var sm = new SerializedObject(mortal);
            sm.FindProperty("puntoReaparicion").objectReferenceValue = punto;
            sm.ApplyModifiedPropertiesWithoutUndo();

            // resplandor de adentro, del tamano del agua, por delante del agua y detras del brillo de la superficie
            var gT = lago.Find("glow interior");
            if (gT == null) { gT = new GameObject("glow interior").transform; gT.SetParent(lago, false); }
            gT.position = new Vector3(b.min.x, b.max.y, agua.transform.position.z - 0.005f);
            gT.localScale = new Vector3(b.size.x / (glow.rect.width / glow.pixelsPerUnit), b.size.y / (glow.rect.height / glow.pixelsPerUnit), 1f);
            var gsr = gT.GetComponent<SpriteRenderer>();
            if (gsr == null) gsr = gT.gameObject.AddComponent<SpriteRenderer>();
            gsr.sprite = glow;
            gsr.sharedMaterial = agua.sharedMaterial;
            gsr.sortingLayerID = agua.sortingLayerID;
            gsr.sortingOrder = agua.sortingOrder;

            // efectos: glow + muchas mas burbujas
            var ef = lago.GetComponent<AguaVenenosaEfectos>();
            if (ef != null)
            {
                var so = new SerializedObject(ef);
                so.FindProperty("glowInterior").objectReferenceValue = gsr;
                so.FindProperty("subenPorSegundo").floatValue = 14f;
                so.FindProperty("flotanPorSegundo").floatValue = 5f;
                so.FindProperty("tamanoQueSuben").vector2Value = new Vector2(0.15f, 0.5f);
                so.FindProperty("vidaFlotando").vector2Value = new Vector2(1.2f, 3f);
                so.FindProperty("tamanoFlotando").vector2Value = new Vector2(0.15f, 0.35f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[Agua] " + lago.name + " no tiene AguaVenenosaEfectos (se lo pone AgregarBrilloAguaVenenosa).");
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Agua] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Agua] Agua venenosa mortal, con reaparicion al costado, resplandor de adentro y mas burbujas.");
    }
}
