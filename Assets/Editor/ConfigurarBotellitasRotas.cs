using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Le pone a cada botellita de la escena su animacion de romperse (botellita1 -> _botellarota_01, etc.)
// y mide cuanto del dibujo ocupa el vidrio, asi la animacion sale del mismo tamano y en el mismo lugar.
// Menu:  Pomelo -> Configurar botellitas rotas
public static class ConfigurarBotellitasRotas
{
    private const string Carpeta = "Assets/_animaciones/_botellarota_0";
    private const byte AlfaMinima = 40;

    [MenuItem("Pomelo/Configurar botellitas rotas")]
    public static void Configurar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de configurar las botellitas.");
            return;
        }

        // 1. los cuadros de las tres animaciones, cada uno un solo sprite apoyado abajo al centro
        var cuadros = new Dictionary<int, Sprite[]>();
        var medidaRota = new Dictionary<int, Rect>();
        for (int n = 1; n <= 3; n++)
        {
            string carpeta = Carpeta + n;
            var rutas = Directory.GetFiles(carpeta, "*.png").Select(r => r.Replace('\\', '/')).OrderBy(r => r).ToArray();
            if (rutas.Length == 0) { Debug.LogError("No hay cuadros en " + carpeta); return; }
            foreach (var r in rutas) ImportarCuadro(r);
            cuadros[n] = rutas.Select(r => AssetDatabase.LoadAssetAtPath<Sprite>(r)).ToArray();
            medidaRota[n] = MedirDibujo(cuadros[n][0]);
        }

        // 2. cada botellita: su animacion y la medida del dibujo que se rompe
        int listas = 0;
        foreach (var b in Object.FindObjectsByType<BotellitaRompible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var m = Regex.Match(b.gameObject.name, @"botellita(\d)");
            int n = m.Success ? int.Parse(m.Groups[1].Value) : 0;
            if (!cuadros.ContainsKey(n))
            {
                Debug.LogWarning("No se que animacion le toca a '" + b.gameObject.name + "' (el nombre tiene que decir botellita1, 2 o 3).", b);
                continue;
            }

            var so = new SerializedObject(b);
            var cuerpo = so.FindProperty("cuerpo").objectReferenceValue as Sprite;
            var dibujo = b.GetComponent<SpriteRenderer>();
            Sprite seRompe = cuerpo != null ? cuerpo : dibujo != null ? dibujo.sprite : null;
            if (seRompe == null) { Debug.LogWarning("'" + b.gameObject.name + "' no tiene dibujo.", b); continue; }

            var lista = so.FindProperty("cuadrosRota");
            lista.arraySize = cuadros[n].Length;
            for (int i = 0; i < cuadros[n].Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = cuadros[n][i];
            so.FindProperty("dibujoBotellita").rectValue = MedirDibujo(seRompe);
            so.FindProperty("dibujoRota").rectValue = medidaRota[n];
            so.ApplyModifiedProperties();
            listas++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Botellitas rotas configuradas: " + listas + ". Guarda la escena con Ctrl+S.");
    }

    private static void ImportarCuadro(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) return;
        var ajustes = new TextureImporterSettings();
        imp.ReadTextureSettings(ajustes);
        if (imp.textureType == TextureImporterType.Sprite && ajustes.spriteMode == (int)SpriteImportMode.Single
            && ajustes.spriteAlignment == (int)SpriteAlignment.BottomCenter) return;
        imp.textureType = TextureImporterType.Sprite;
        ajustes.spriteMode = (int)SpriteImportMode.Single;
        ajustes.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        ajustes.alphaIsTransparency = true;
        imp.SetTextureSettings(ajustes);
        imp.SaveAndReimport();
    }

    // Parte del sprite (de 0 a 1, desde abajo a la izquierda) donde hay dibujo, sin el lienzo transparente.
    // Lee el PNG original, asi no hace falta marcar las texturas como legibles.
    private static Rect MedirDibujo(Sprite sprite)
    {
        string ruta = AssetDatabase.GetAssetPath(sprite);
        var tex = new Texture2D(2, 2);
        if (!tex.LoadImage(File.ReadAllBytes(ruta))) return new Rect(0f, 0f, 1f, 1f);

        // el sprite puede ser solo una parte del PNG; se mide en proporcion al tamano original
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        int anchoOriginal = tex.width, altoOriginal = tex.height;
        if (imp != null) imp.GetSourceTextureWidthAndHeight(out anchoOriginal, out altoOriginal);
        float k = tex.width / (float)Mathf.Max(1, anchoOriginal);
        var marco = sprite.rect;
        int x0 = Mathf.FloorToInt(marco.x * k), y0 = Mathf.FloorToInt(marco.y * k);
        int ancho = Mathf.Min(tex.width - x0, Mathf.CeilToInt(marco.width * k));
        int alto = Mathf.Min(tex.height - y0, Mathf.CeilToInt(marco.height * k));

        var px = tex.GetPixels32();
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                if (px[(y0 + y) * tex.width + x0 + x].a > AlfaMinima)
                {
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
        Object.DestroyImmediate(tex);
        if (maxX < 0) return new Rect(0f, 0f, 1f, 1f);
        return new Rect(minX / (float)ancho, minY / (float)alto, (maxX - minX + 1) / (float)ancho, (maxY - minY + 1) / (float)alto);
    }
}
