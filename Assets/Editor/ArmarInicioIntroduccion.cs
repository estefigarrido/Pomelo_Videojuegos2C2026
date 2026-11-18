using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Arma la escena de la pantalla de inicio (Assets/Scenes/Inicio_introducción.unity) con el escenario de Caro:
// la cueva con la plataforma de inicio (capas "inicio_*") y, a la derecha, la catarata y el templo (capas "cascada_*").
// Las capas son los PNG de Assets/_Entorno/_Inicio_introduccion (salieron de pomelo_escena_completa_capas.zip) y van
// en el mismo lugar y orden que en el SVG de la escena completa (posiciones.json del zip).
// Tambien pone la escena primera en la lista del build, asi el juego arranca por la pantalla de inicio.
// Menu:  Pomelo -> Escenas -> Armar escena Inicio_introducción
public static class ArmarInicioIntroduccion
{
    // la carpeta donde esten las capas (se busca por inicio_01_fondo.png, asi se puede mover sin romper nada;
    // hoy esta en Assets/_Entorno/_Inicio_introduccion)
    private static string carpeta;
    private static string Carpeta
    {
        get
        {
            if (carpeta != null && AssetDatabase.IsValidFolder(carpeta)) return carpeta;
            string ruta = AssetDatabase.FindAssets("inicio_01_fondo t:Texture2D")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(r => Path.GetFileName(r) == "inicio_01_fondo.png");
            carpeta = ruta != null ? Path.GetDirectoryName(ruta).Replace('\\', '/') : "Assets/_Entorno/_Inicio_introduccion";
            return carpeta;
        }
    }
    private const string RutaEscena = "Assets/Scenes/Inicio_introducción.unity";
    private const float PixelesPorUnidad = 100f;
    private const float AltoEscenaPx = 2160f;   // la escena completa mide 7202 x 2160 px
    private const float AnchoCuevaPx = 3612f;   // la parte de la cueva de inicio (lo que muestra la pantalla de inicio)

    // capa, x, y (esquina de arriba a la izquierda, en px, con y para abajo). Ordenadas de atras hacia adelante.
    private static readonly (string capa, int x, int y)[] Capas =
    {
        ("inicio_01_fondo", 0, 0),
        ("inicio_02_niebla", 349, 920),
        ("inicio_03_rayos_de_luz", 439, 0),
        ("inicio_04_columnas_de_luz", 965, 0),
        ("inicio_05_cascada", 3061, 0),
        ("inicio_06_agua", 0, 1996),
        ("inicio_07_lianas", 1870, 0),
        ("inicio_08_techo", 0, 0),
        ("inicio_09_plataforma_inicio", 0, 175),
        ("inicio_10_isla_roca", 2048, 1867),
        ("inicio_11_columna_roca", 3389, 0),   // en el SVG estaba en x 2938: Caro la paso a la esquina derecha
                                               // (3612 - 223 de ancho) para que la cueva quede enmarcada
        ("inicio_12_agua_frente", 2926, 2013),
        ("inicio_13_planta", 674, 1132),
        ("inicio_14_hojas_oscuras", 35, 1811),
        ("inicio_15_musgo", 513, 1472),
        ("inicio_16_hilos_colgantes", 1235, 117),
        ("inicio_17_particulas", 1088, 1107),
        ("cascada_00_fondo", 3611, 0),
        ("cascada_00b_agua_base", 3611, 1990),
        ("cascada_01_cielo_y_niebla", 3611, 0),
        ("cascada_02_selva_lejana_pilares_facetados", 3665, 0),
        ("cascada_02b_acantilado_de_la_catarata", 4281, 0),
        ("cascada_03_cueva_lejana_arcos_del_templo", 5181, 493),
        ("cascada_04_rayos_de_luz", 5123, 0),
        ("cascada_05_catarata_recta", 4252, 0),
        ("cascada_06_fragmento_de_templo_roto", 4799, 1396),
        ("cascada_07_plataformas_de_agua_derecha", 5961, 1787),
        ("cascada_08_columnas_sueltas", 4472, 1375),
        ("cascada_09_agua", 3744, 2005),
        ("cascada_10_plataformas", 3975, 1947),
        ("cascada_11_techo_de_la_cueva", 4799, 0),
        ("cascada_12_plantas_de_fondo", 3660, 1714),
    };

    [MenuItem("Pomelo/Escenas/Armar escena Inicio_introducción")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Para el Play antes de armar la escena."); return; }
        if (File.Exists(RutaEscena) &&
            !EditorUtility.DisplayDialog("Inicio_introducción", "La escena ya existe. ¿La armo de nuevo desde cero?",
                "Sí, rearmarla", "Cancelar"))
            return;
        // si la escena abierta (Nivel1_Templo) tiene cambios sin guardar, pregunta antes de cerrarla
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        carpeta = null;   // la busca de nuevo, por si se movio
        PrepararImagenes();

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // camara: ve la cueva de inicio entera (PantallaInicio la reencuadra segun la pantalla)
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = AnchoCuevaPx / PixelesPorUnidad / (16f / 9f) / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x14, 0x1a, 0x18, 0xff);
        camGo.transform.position = new Vector3(AnchoCuevaPx / PixelesPorUnidad / 2f, AltoEscenaPx / PixelesPorUnidad / 2f, -10f);
        camGo.AddComponent<AudioListener>();

        // luz global, como en el nivel (los sprites usan el material iluminado del 2D)
        var luzGo = new GameObject("Luz global");
        var luz = luzGo.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;

        var raiz = new GameObject("Escenario").transform;
        var inicio = new GameObject("Inicio (cueva)").transform;
        var cascada = new GameObject("Cascada (templo)").transform;
        inicio.SetParent(raiz, false);
        cascada.SetParent(raiz, false);

        int faltan = 0;
        for (int i = 0; i < Capas.Length; i++)
        {
            var (capa, x, y) = Capas[i];
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Carpeta}/{capa}.png");
            if (sprite == null) { Debug.LogWarning("Falta la capa " + capa + ".png en " + Carpeta); faltan++; continue; }
            var go = new GameObject(capa);
            go.transform.SetParent(capa.StartsWith("inicio") ? inicio : cascada, false);
            // el pivot de cada imagen esta arriba a la izquierda; el piso de la escena queda en y = 0
            go.transform.localPosition = new Vector3(x / PixelesPorUnidad, (AltoEscenaPx - y) / PixelesPorUnidad, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = i - Capas.Length;   // todo el fondo queda detras (numeros negativos), en el orden del SVG
        }

        ArmarPantalla(cam);

        Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnElBuild();
        Debug.Log($"Escena {RutaEscena} armada con {Capas.Length - faltan} capas. Quedo primera en el build.");
    }

    // las importa como sprite, con el pivot arriba a la izquierda (como las coordenadas del SVG) y sin achicarlas
    private static void PrepararImagenes()
    {
        foreach (var (capa, _, _) in Capas)
        {
            string ruta = $"{Carpeta}/{capa}.png";
            if (AssetImporter.GetAtPath(ruta) is not TextureImporter imp) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize = 4096;   // el fondo mide 3612 px de ancho
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.TopLeft;
            ajustes.spritePivot = new Vector2(0f, 1f);
            ajustes.spritePixelsPerUnit = PixelesPorUnidad;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }
    }

    // ---------- titulo y "Comenzar" ----------
    // Medidas en px del diseno de Caro: el cuadro de la cueva mide 1234 x 738 (el SVG pantalla_inicio) y las
    // posiciones van desde el centro de ese cuadro (y para arriba). El lienzo escala con el ancho, asi coincide
    // con la cueva que encuadra la camara.
    private const string CarpetaUI = "Assets/_UI/Inicio/";
    private const string CarpetaIosevka = "Assets/_Fuentes/Iosevka";
    private const string RutaAlegreya = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string Teclado = "Assets/_UI/Carteles/Teclas_y_botones/Teclado/";
    private const string Joystick = "Assets/_UI/Carteles/Teclas_y_botones/Joystick/";
    private static readonly Vector2 Diseno = new Vector2(1234f, 738f);

    private const float TamanoTitulo = 190f;                 // Iosevka Charon Mono Regular, 190 px
    private static readonly Vector2 LugarTitulo = new Vector2(-13f, -36f);   // centro en x, linea de base en y
    // fila "Comenzar  [espacio] o [X]": el mismo formato que los carteles (Alegreya 32 en versalitas, "o" a 15,
    // iconos a su tamano, todo un 4 % mas grande, y la franja oscura difuminada detras)
    private const float EscalaCartel = 78f / 75f;
    private static readonly Vector2 LugarComenzar = new Vector2(-126.5f, -110f);   // borde izquierdo, linea de base
    private static readonly Vector2 LugarEspacio = new Vector2(57f, -99f);
    private static readonly Vector2 LugarO = new Vector2(87f, -100f);
    private static readonly Vector2 LugarEquis = new Vector2(119.5f, -99f);
    private static readonly Vector2 CentroFranja = new Vector2(3f, -100f);
    private static readonly Vector2 TamanoFranja = new Vector2(348f, 64f);   // la fila + 44 px a los costados y 16 arriba/abajo
    private static readonly Color ColorFranja = new Color(0f, 0f, 0f, 0.55f);  // igual que los carteles

    private static void ArmarPantalla(Camera cam)
    {
        Directory.CreateDirectory(CarpetaUI);
        var alegreya = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaAlegreya);
        var iosevka = FuenteIosevka();

        var go = new GameObject("Pantalla de inicio", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                typeof(PantallaInicio));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = go.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = Diseno;
        escalador.matchWidthOrHeight = 0f;   // con el ancho, como la camara
        var lienzo = (RectTransform)go.transform;

        // titulo, con una sombra suave alrededor para que se lea sobre el fondo claro
        var fuenteTitulo = iosevka != null ? iosevka : alegreya;
        var titulo = Texto("TÉCHNI", lienzo, fuenteTitulo, TamanoTitulo, FontStyles.Normal,
                           TextAlignmentOptions.Baseline, LugarTitulo, new Vector2(1000f, 260f));
        if (fuenteTitulo != null) titulo.fontSharedMaterial = MaterialConSombra(fuenteTitulo);

        // fila de Comenzar
        var fila = new GameObject("Comenzar", typeof(RectTransform)).GetComponent<RectTransform>();
        fila.SetParent(lienzo, false);
        fila.anchorMin = fila.anchorMax = fila.pivot = new Vector2(0.5f, 0.5f);
        fila.sizeDelta = Vector2.zero;
        var franja = Franja();
        if (franja != null)
        {
            var img = Imagen("Sombra", fila, franja, CentroFranja, TamanoFranja);
            img.color = ColorFranja;
        }
        var comenzar = Texto("Comenzar", fila, alegreya, 32f * EscalaCartel, FontStyles.SmallCaps,
                             TextAlignmentOptions.BaselineLeft, LugarComenzar + new Vector2(150f, 0f), new Vector2(300f, 60f));
        // sin kerning: con las versalitas, TextMeshPro le aplicaba a la "m" y la "e" el ajuste de las minusculas
        // y la M quedaba metida adentro de la E. Sin kerning todas las letras quedan parejas.
        comenzar.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>();
        Icono(Teclado + "Tecla_barraespaciadora.png", fila, LugarEspacio);
        Texto("o", fila, alegreya, 15f * EscalaCartel, FontStyles.Normal, TextAlignmentOptions.Midline, LugarO,
              new Vector2(30f, 30f));
        Icono(Joystick + "Boton_Equis.png", fila, LugarEquis);

        // fundido a negro al comenzar (arriba de todo)
        var negro = new GameObject("Fundido", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        negro.SetParent(lienzo, false);
        negro.anchorMin = Vector2.zero;
        negro.anchorMax = Vector2.one;
        negro.offsetMin = negro.offsetMax = Vector2.zero;
        var imgNegro = negro.GetComponent<Image>();
        imgNegro.color = new Color(0f, 0f, 0f, 0f);
        imgNegro.raycastTarget = false;

        var so = new SerializedObject(go.GetComponent<PantallaInicio>());
        so.FindProperty("camara").objectReferenceValue = cam;
        so.FindProperty("fundido").objectReferenceValue = imgNegro;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (iosevka == null)
            Debug.LogWarning("No encontre la fuente Iosevka Charon Mono (un .ttf con 'Charon' y 'Mono' en el nombre, " +
                             "Regular) dentro de " + CarpetaIosevka + ". El titulo quedo con Alegreya: pone el .ttf ahi y " +
                             "volve a armar la escena.");
    }

    // el TTF de Iosevka Charon Mono Regular que este en Assets/_Fuentes/Iosevka -> su fuente de TextMeshPro (estatica)
    private static TMP_FontAsset FuenteIosevka()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaIosevka)) return null;
        string ttf = AssetDatabase.FindAssets("t:Font", new[] { CarpetaIosevka })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(r => { string n = Path.GetFileNameWithoutExtension(r).ToLowerInvariant();
                          return n.Contains("charon") && n.Contains("mono") &&
                                 (n.EndsWith("-regular") || n.EndsWith("regular")) && !n.Contains("italic"); })
            .FirstOrDefault();
        if (ttf == null) return null;

        string ruta = CarpetaIosevka + "/Iosevka Charon Mono SDF.asset";
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (existente != null) return existente;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        // padding grande para que entre la sombra difuminada del titulo
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, 16, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Debug.LogError("No pude crear la fuente de TextMeshPro a partir de " + ttf); return null; }
        fa.name = "Iosevka Charon Mono SDF";
        AssetDatabase.CreateAsset(fa, ruta);
        fa.atlasTextures[0].name = "Iosevka Charon Mono SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = "Iosevka Charon Mono SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        FijarFuentes.Fijar(fa);   // todas las letras y modo estatico, asi el archivo no cambia solo
        AssetDatabase.SaveAssets();
        return fa;
    }

    // material del titulo: sombra oscura y difuminada alrededor de las letras (underlay de TextMeshPro)
    private static Material MaterialConSombra(TMP_FontAsset fuente)
    {
        string ruta = CarpetaUI + "Titulo_TECHNI_sombra.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(fuente.material);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        else
        {
            mat.shader = fuente.material.shader;
            mat.CopyPropertiesFromMaterial(fuente.material);
        }
        // el shader completo de TextMeshPro: es el que tiene sombra (underlay) y brillo (glow) juntos
        var completo = Shader.Find("TextMeshPro/Distance Field");
        if (completo != null) mat.shader = completo;

        // sombra: oscura y difuminada mas afuera, para que el titulo se separe del fondo claro
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color32(0x14, 0x1a, 0x18, 120));   // el verde casi negro de la cueva
        mat.SetFloat("_UnderlayOffsetX", 0f);
        mat.SetFloat("_UnderlayOffsetY", -0.1f);
        mat.SetFloat("_UnderlayDilate", 0.4f);
        mat.SetFloat("_UnderlaySoftness", 1f);

        // brillo: un halo claro, del color de la luz de la cueva, pegado a las letras
        mat.EnableKeyword("GLOW_ON");
        mat.SetColor("_GlowColor", new Color(0.89f, 0.94f, 0.83f, 0.6f));
        mat.SetFloat("_GlowOffset", 0f);
        mat.SetFloat("_GlowInner", 0.05f);
        mat.SetFloat("_GlowOuter", 0.4f);
        mat.SetFloat("_GlowPower", 0.85f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static TextMeshProUGUI Texto(string texto, RectTransform padre, TMP_FontAsset fuente, float tamano,
                                         FontStyles estilo, TextAlignmentOptions alineacion, Vector2 centro, Vector2 caja)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = caja;
        rt.anchoredPosition = centro;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) { t.font = fuente; t.fontSharedMaterial = fuente.material; }
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Image Imagen(string nombre, RectTransform padre, Sprite sprite, Vector2 centro, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = centro;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static void Icono(string ruta, RectTransform padre, Vector2 centro)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) { Debug.LogError("No encontre el icono " + ruta); return; }
        Imagen(Path.GetFileNameWithoutExtension(ruta), padre, sprite, centro, sprite.rect.size * EscalaCartel);
    }

    // la misma franja que los carteles: pareja en el medio, difuminada a los costados y apenas arriba y abajo
    private static Sprite Franja()
    {
        string ruta = CarpetaUI + "Comenzar_sombra.png";
        int W = Mathf.CeilToInt(TamanoFranja.x), H = Mathf.CeilToInt(TamanoFranja.y);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                float horizontal = Mathf.SmoothStep(0f, 1f, u / 0.15f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.15f);
                float vertical = Mathf.SmoothStep(0f, 1f, v / 0.12f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.12f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(horizontal * vertical)));
            }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void PonerPrimeraEnElBuild()
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaEscena);
        lista.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}
