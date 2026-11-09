using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Arma el cartel de tutorial "Atacar" como en el Figma (Frame 48), lo guarda como prefab en
// Assets/_UI/Carteles y lo pone al lado del portal de entrada:
//
//              [compu]        [joystick]
//     ATACAR   [mouse]   o    [circulo]
//
//  - texto en Alegreya Regular: "Atacar" a 32 px (en versalitas, como el Figma) y la "o" a 15 px
//  - iconos de Assets/_UI/Teclas_y_botones a su tamano original
//  - todo el cartel agrandado para que tenga 3 px mas de alto
//  - UNA sola sombra gris suave con la silueta de todo el cartel (texto + iconos juntos),
//    que no se expande mas de 4 px
//  - CartelTutorial: aparece suave cuando la chica termina de salir por la puerta, dura 3 s,
//    se desvanece, y se va borrando a medida que la chica le pasa por delante
// Las medidas estan en pixeles del juego (camara normal: 100 px = 1 unidad), igual que el Figma.
// Si el cartel ya estaba en la escena, se rearma en el mismo lugar (si lo habias movido, queda donde lo dejaste).
// Menu:  Pomelo -> Poner el cartel de Atacar
public static class PonerCartelAtacar
{
    private const string NombreObjeto = "Cartel Atacar";
    private const string CarpetaCarteles = "Assets/_UI/Carteles/";
    private const string RutaPrefab = CarpetaCarteles + NombreObjeto + ".prefab";
    private const string RutaSombra = CarpetaCarteles + NombreObjeto + "_sombra.png";

    // fuente
    private const string RutaTtf = "Assets/_Fuentes/Alegreya/Alegreya-Regular.ttf";
    private const string RutaFontAsset = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string PaqueteEsencialesTMP = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    // iconos
    private const string Carpeta = "Assets/_UI/Teclas_y_botones/";
    private const string IconoCompu = Carpeta + "Teclado/Computadora.png";
    private const string IconoMouse = Carpeta + "Teclado/Click_derecho.png"; // es el dibujo con el boton izquierdo marcado
    private const string IconoJoystick = Carpeta + "Joystick/Joystick.png";
    private const string IconoCirculo = Carpeta + "Joystick/Boton_Circulo.png";

    // restos de la primera version (una sombra por icono y una por texto): se borran
    private static readonly string[] Viejos =
    {
        Carpeta + "Sombras",
        "Assets/_Fuentes/Alegreya/Alegreya SDF - Sombra.mat",
    };

    // medidas (px)
    private static readonly Vector2 TamanoContenido = new Vector2(243f, 75f);
    private const float Agrandar = 3f;                                        // px de alto de mas
    private static float Escala => (TamanoContenido.y + Agrandar) / TamanoContenido.y;
    private const float MargenMascaraX = 48f;   // lugar para el borde difuminado del borrado
    private const float MargenMascaraY = 8f;

    // sombra: gris, suave, como mucho 4 px alrededor (2 pasadas de desenfoque de radio 2)
    private const int Expansion = 4;
    private static readonly Color ColorSombra = new Color(0.22f, 0.22f, 0.22f, 0.65f);

    // recuadro rojo de la captura: su centro queda 240 px a la derecha de la chica
    // y 116 px arriba de sus pies cuando sale del portal
    private static readonly Vector2 CentroDesdePies = new Vector2(2.40f, 1.16f);

    [MenuItem("Pomelo/Poner el cartel de Atacar")]
    public static void Poner()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de poner el cartel de Atacar.");
            return;
        }

        // 1. TextMeshPro necesita sus archivos basicos (TMP Settings, shaders) dentro de Assets
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            AssetDatabase.ImportPackage(PaqueteEsencialesTMP, false);
            AssetDatabase.Refresh();
            Debug.LogWarning("Importe los archivos basicos de TextMeshPro (carpeta 'TextMesh Pro'). " +
                             "Cuando termine de importar, volve a usar  Pomelo -> Poner el cartel de Atacar.");
            return;
        }

        var fuente = FontAssetAlegreya();
        if (fuente == null) return;
        var compu = CargarSprite(IconoCompu);
        var mouse = CargarSprite(IconoMouse);
        var joystick = CargarSprite(IconoJoystick);
        var circulo = CargarSprite(IconoCirculo);
        if (compu == null || mouse == null || joystick == null || circulo == null) return;

        var chica = Object.FindFirstObjectByType<SaludPersonaje>();
        var dibujoChica = chica != null ? chica.GetComponent<SpriteRenderer>() : null;

        // 2. si ya estaba, se saca (guardando donde estaba) junto con los restos de la version anterior
        Vector3 posicion = PosicionCartel(chica, dibujoChica);
        var viejo = GameObject.Find(NombreObjeto);
        if (viejo != null)
        {
            posicion = viejo.transform.position;
            Undo.DestroyObjectImmediate(viejo);
        }
        foreach (var ruta in Viejos)
            if (AssetDatabase.IsValidFolder(ruta) || File.Exists(ruta)) AssetDatabase.DeleteAsset(ruta);
        if (!AssetDatabase.IsValidFolder(CarpetaCarteles.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/_UI", "Carteles");

        // 3. el cartel: Canvas en el mundo, 1 px = 0.01 unidades
        var raiz = new GameObject(NombreObjeto, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(CartelTutorial));
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 |
                                           AdditionalCanvasShaderChannels.Normal |
                                           AdditionalCanvasShaderChannels.Tangent;
        if (dibujoChica != null)
        {
            canvas.sortingLayerID = dibujoChica.sortingLayerID;
            canvas.sortingOrder = dibujoChica.sortingOrder - 1;
        }
        var rtRaiz = (RectTransform)raiz.transform;
        Vector2 tamanoMascara = TamanoContenido * Escala + 2f * new Vector2(MargenMascaraX, MargenMascaraY);
        rtRaiz.sizeDelta = tamanoMascara;
        rtRaiz.localScale = Vector3.one * 0.01f;
        rtRaiz.position = posicion;

        // mascara para ir borrandolo cuando la chica pasa por delante
        var rtMascara = Hijo("Mascara", rtRaiz, tamanoMascara);
        var mascara = rtMascara.gameObject.AddComponent<RectMask2D>();

        // 4. el contenido, medido desde la esquina de arriba a la izquierda del Frame 48 (243 x 75 px)
        var contenido = Hijo("Contenido", rtMascara, TamanoContenido);
        Texto("Atacar", contenido, "Atacar", 32f, FontStyles.SmallCaps, fuente,
              new Vector2(0f, -61f), new Vector2(0f, 0.5f), new Vector2(130f, 44f));
        Icono("Compu", contenido, compu, new Vector2(163f, -11f));
        Icono("Mouse", contenido, mouse, new Vector2(161f, -56f));
        Texto("o", contenido, "o", 15f, FontStyles.Normal, fuente,
              new Vector2(194f, -61f), new Vector2(0.5f, 0.5f), new Vector2(20f, 24f));
        Icono("Joystick", contenido, joystick, new Vector2(227f, -11f));
        Icono("Circulo", contenido, circulo, new Vector2(227f, -58f));

        // 5. una sola sombra con la silueta de todo junto, detras de todo
        var sombra = CrearSombraConjunta(contenido);
        if (sombra != null)
        {
            var rtS = Hijo("Sombra", contenido, sombra.rect.size);
            rtS.anchoredPosition = CentroSombra(contenido, sombra);
            rtS.SetAsFirstSibling();
            var img = rtS.gameObject.AddComponent<Image>();
            img.sprite = sombra;
            img.color = ColorSombra;
            img.raycastTarget = false;
        }

        // 6. 3 px mas de alto (todo en proporcion)
        contenido.localScale = Vector3.one * Escala;

        var so = new SerializedObject(raiz.GetComponent<CartelTutorial>());
        so.FindProperty("mascara").objectReferenceValue = mascara;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 7. guardarlo como prefab en _UI/Carteles y dejar en la escena una copia conectada
        var instancia = PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, RutaPrefab, InteractionMode.AutomatedAction);
        Undo.RegisterCreatedObjectUndo(instancia, "Poner el cartel de Atacar");

        EditorSceneManager.MarkSceneDirty(instancia.scene);
        Selection.activeGameObject = instancia;
        Debug.Log("Cartel de Atacar guardado en " + RutaPrefab + " y puesto al lado del portal de entrada. " +
                  "Si hay que correrlo, move '" + NombreObjeto + "' en la Scene. Guarda la escena con Ctrl+S.");
    }

    // ---------- ubicacion ----------

    // Donde sale la chica del portal (InicioNivel.posicionChica) + el lugar del recuadro rojo de la captura.
    private static Vector3 PosicionCartel(SaludPersonaje chica, SpriteRenderer dibujoChica)
    {
        Vector2 salida = chica != null ? (Vector2)chica.transform.position : Vector2.zero;
        var inicio = Object.FindFirstObjectByType<InicioNivel>();
        if (inicio != null)
            salida = new SerializedObject(inicio).FindProperty("posicionChica").vector2Value;

        float centroX = 0f, pies = -1.2f;
        if (dibujoChica != null)
        {
            Bounds b = dibujoChica.bounds;
            centroX = b.center.x - dibujoChica.transform.position.x;
            pies = b.min.y - dibujoChica.transform.position.y;
        }
        // z un poquito hacia la camara: le gana a los dibujos con el mismo orden (dientes de leon)
        return new Vector3(salida.x + centroX + CentroDesdePies.x, salida.y + pies + CentroDesdePies.y, -0.05f);
    }

    // ---------- piezas del cartel ----------

    private static RectTransform Hijo(string nombre, RectTransform padre, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    // posicion medida desde la esquina de arriba a la izquierda del contenido
    private static RectTransform EnContenido(GameObject go, RectTransform contenido, Vector2 posicion, Vector2 pivot, Vector2 tamano)
    {
        var rt = (RectTransform)go.transform;
        rt.SetParent(contenido, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = pivot;
        rt.sizeDelta = tamano;
        rt.anchoredPosition = posicion;
        return rt;
    }

    private static void Texto(string nombre, RectTransform contenido, string texto, float tamano, FontStyles estilo,
                              TMP_FontAsset fuente, Vector2 posicion, Vector2 pivot, Vector2 caja)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        EnContenido(go, contenido, posicion, pivot, caja);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = fuente;
        t.fontSharedMaterial = fuente.material;
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.alignment = pivot.x == 0f ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }

    private static void Icono(string nombre, RectTransform contenido, Sprite sprite, Vector2 centro)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        EnContenido(go, contenido, centro, new Vector2(0.5f, 0.5f), sprite.rect.size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
    }

    private static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogError("No encontre el icono " + ruta);
        return s;
    }

    // ---------- fuente ----------

    private static TMP_FontAsset FontAssetAlegreya()
    {
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFontAsset);
        if (existente != null) return existente;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(RutaTtf);
        if (ttf == null)
        {
            Debug.LogError("No encontre la fuente " + RutaTtf);
            return null;
        }

        var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 12, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
        if (fa == null)
        {
            Debug.LogError("No pude crear la fuente de TextMeshPro a partir de " + RutaTtf);
            return null;
        }
        fa.name = "Alegreya SDF";
        AssetDatabase.CreateAsset(fa, RutaFontAsset);
        fa.atlasTextures[0].name = "Alegreya SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = "Alegreya SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters("AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZzÁáÉéÍíÓóÚúÑñ0123456789 .,:;!¡?¿-");
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ---------- sombra conjunta ----------

    // Junta la forma de todo el contenido (iconos + letras) en una sola silueta, la desenfoca
    // (2 pasadas de radio 2 = 4 px como mucho) y la guarda como PNG blanco; el gris lo pone la Image.
    private static Sprite CrearSombraConjunta(RectTransform contenido)
    {
        Canvas.ForceUpdateCanvases();
        Rect rc = contenido.rect;
        int W = Mathf.CeilToInt(rc.width) + 2 * Expansion, H = Mathf.CeilToInt(rc.height) + 2 * Expansion;
        float ox = rc.xMin - Expansion, oy = rc.yMin - Expansion;
        var a = new float[W * H];
        var texturas = new Dictionary<string, Texture2D>();

        foreach (RectTransform hijo in contenido)
        {
            var img = hijo.GetComponent<Image>();
            if (img != null && img.sprite != null) SumarIcono(a, W, H, ox, oy, contenido, hijo, img.sprite, texturas);
            var txt = hijo.GetComponent<TextMeshProUGUI>();
            if (txt != null) SumarTexto(a, W, H, ox, oy, contenido, txt);
        }
        foreach (var t in texturas.Values) Object.DestroyImmediate(t);

        for (int pasada = 0; pasada < 2; pasada++)
        {
            a = Desenfocar(a, W, H, 2, true);
            a = Desenfocar(a, W, H, 2, false);
        }

        var salida = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var px = new Color32[W * H];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a[i]) * 255f));
        salida.SetPixels32(px);
        salida.Apply();
        File.WriteAllBytes(RutaSombra, salida.EncodeToPNG());
        Object.DestroyImmediate(salida);

        AssetDatabase.ImportAsset(RutaSombra, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaSombra);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSombra);
    }

    // centro de la sombra en el contenido: la grilla arranca 4 px antes del borde de abajo a la izquierda
    private static Vector2 CentroSombra(RectTransform contenido, Sprite sombra)
    {
        Rect rc = contenido.rect;
        return new Vector2(rc.xMin - Expansion + sombra.rect.width / 2f, rc.yMin - Expansion + sombra.rect.height / 2f);
    }

    private static void SumarIcono(float[] a, int W, int H, float ox, float oy, RectTransform contenido,
                                   RectTransform rt, Sprite sprite, Dictionary<string, Texture2D> texturas)
    {
        string ruta = AssetDatabase.GetAssetPath(sprite.texture);
        if (!texturas.TryGetValue(ruta, out var tex))
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(ruta));
            texturas[ruta] = tex;
        }
        Vector2 min = contenido.InverseTransformPoint(rt.TransformPoint(rt.rect.min));
        Vector2 max = contenido.InverseTransformPoint(rt.TransformPoint(rt.rect.max));
        Rect sr = sprite.rect;
        Pintar(a, W, H, ox, oy, min, max, (u, v) =>
            tex.GetPixelBilinear((sr.x + u * sr.width) / tex.width, (sr.y + v * sr.height) / tex.height).a);
    }

    private static void SumarTexto(float[] a, int W, int H, float ox, float oy, RectTransform contenido, TextMeshProUGUI t)
    {
        t.ForceMeshUpdate();
        var atlas = t.font.atlasTexture;
        var info = t.textInfo;
        for (int i = 0; i < info.characterCount; i++)
        {
            var ci = info.characterInfo[i];
            if (!ci.isVisible) continue;
            Vector2 min = contenido.InverseTransformPoint(t.transform.TransformPoint(ci.vertex_BL.position));
            Vector2 max = contenido.InverseTransformPoint(t.transform.TransformPoint(ci.vertex_TR.position));
            Vector2 uv0 = ci.vertex_BL.uv, uv1 = ci.vertex_TR.uv;
            // SDF: 0.5 es el borde de la letra
            Pintar(a, W, H, ox, oy, min, max, (u, v) =>
            {
                float d = atlas.GetPixelBilinear(Mathf.Lerp(uv0.x, uv1.x, u), Mathf.Lerp(uv0.y, uv1.y, v)).a;
                return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.44f, 0.56f, d));
            });
        }
    }

    // pinta (con el maximo) un rectangulo del contenido en la grilla de la sombra
    private static void Pintar(float[] a, int W, int H, float ox, float oy, Vector2 min, Vector2 max,
                               System.Func<float, float, float> alfa)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(min.x - ox)), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(max.x - ox));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(min.y - oy)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(max.y - oy));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float u = (ox + x + 0.5f - min.x) / (max.x - min.x);
                float v = (oy + y + 0.5f - min.y) / (max.y - min.y);
                if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                int i = y * W + x;
                a[i] = Mathf.Max(a[i], alfa(u, v));
            }
    }

    private static float[] Desenfocar(float[] a, int W, int H, int radio, bool horizontal)
    {
        var b = new float[a.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float suma = 0f;
                for (int k = -radio; k <= radio; k++)
                {
                    int xx = horizontal ? x + k : x, yy = horizontal ? y : y + k;
                    if (xx >= 0 && xx < W && yy >= 0 && yy < H) suma += a[yy * W + xx];
                }
                b[y * W + x] = suma / (2 * radio + 1);
            }
        return b;
    }
}
