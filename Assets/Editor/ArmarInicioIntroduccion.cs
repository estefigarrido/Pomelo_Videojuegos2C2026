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
    // cuanto se sube el agua (la de la cueva y la de la parte 2, que son una sola) y lo que esta en ella, respecto
    // del SVG, en px de la escena. OJO: la isla de roca y sus piedritas (inicio_10 / inicio_15) estan redibujadas
    // ya subidas 70: si se cambia este numero, hay que redibujarlas tambien.
    private const int SubidaAgua = 70;

    // capa, x, y (esquina de arriba a la izquierda, en px, con y para abajo). Ordenadas de atras hacia adelante.
    private static readonly (string capa, int x, int y)[] Capas =
    {
        ("inicio_01_fondo", 0, 0),
        ("inicio_02_niebla", 349, 920),
        ("inicio_03_rayos_de_luz", 439, 0),
        ("inicio_04_columnas_de_luz", 965, 0),
        ("inicio_05_cascada", 3061, 0),
        // el agua de la cueva sube igual que la de la parte 2 (es una sola agua), y se estira hasta abajo
        ("inicio_06_agua", 0, 1996 - SubidaAgua),
        ("inicio_07_lianas", 1870, 0),
        ("inicio_08_techo", 0, 0),
        ("inicio_09_plataforma_inicio", 0, 175),
        // la isla de roca, redibujada subida con el agua y alargada hasta abajo (en el SVG: 2048, 1867). Las
        // piedritas de arriba de ella (en inicio_15_musgo) tambien estan redibujadas subidas.
        ("inicio_10_isla_roca", 2027, 1795),
        ("inicio_13_planta", 674, 1132),
        ("inicio_14_hojas_oscuras", 35, 1811),
        ("inicio_15_musgo", 513, 1472),
        ("inicio_16_hilos_colgantes", 1235, 117),
        ("inicio_17_particulas", 1088, 1107),
        ("cascada_00_fondo", 3611, 0),
        ("cascada_01_cielo_y_niebla", 3611, 0),
        ("cascada_02_selva_lejana_pilares_facetados", 3665, 0),
        ("cascada_02b_acantilado_de_la_catarata", 4281, 0),
        ("cascada_03_cueva_lejana_arcos_del_templo", 5181, 493),
        ("cascada_04_rayos_de_luz", 5123, 0),
        ("cascada_05_catarata_recta", 4252, 0),
        // PARTE 2: el agua y todo lo que esta en el agua va SubidaAgua px mas arriba que en el SVG (Caro: el
        // agua quedaba muy abajo). En x no cambia nada. El agua de base va despues del fondo (en el SVG iba antes),
        // asi tapa la parte de abajo de la catarata y los pilares, que ahora quedaria debajo del agua.
        ("cascada_00b_agua_base", 3611, 1990 - SubidaAgua),
        // las hojas: en el SVG iban adelante de todo; Caro las paso atras de los objetos (arco, columnas, bloques)
        ("cascada_12_plantas_de_fondo", 3660, 1714 - SubidaAgua),
        ("cascada_06_fragmento_de_templo_roto", 4799, 1396 - SubidaAgua),
        ("cascada_07_plataformas_de_agua_derecha", 5961, 1787 - SubidaAgua),
        ("cascada_08_columnas_sueltas", 4472, 1375 - SubidaAgua),
        ("cascada_09_agua", 3744, 2005 - SubidaAgua),
        ("cascada_10_plataformas", 3975, 1947 - SubidaAgua),
        ("cascada_11_techo_de_la_cueva", 4799, 0),
        // la columna de roca: en el SVG estaba en x 2938. Va cortada al medio por el borde derecho de la cueva
        // (3612 - 223 / 2): en la pantalla de inicio se ve la mitad izquierda y, al ir la camara a la cascada, la
        // otra mitad. Va adelante de todo (con su agua de adelante), si no el fondo de la cascada la tapa.
        ("inicio_11_columna_roca", 3500, 0),
        ("inicio_12_agua_frente", 2926, 2013 - SubidaAgua),
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
            // orden de dibujo: de a 10 por capa, asi entre una capa y la siguiente entran cosas (el agua de las columnas)
            int orden = (i - Capas.Length) * 10;
            // los puntitos de luz no van como imagen quieta: se arman uno por uno para que floten y destellen
            if (capa == "inicio_17_particulas")
            {
                ArmarDestellos(inicio, orden);
                continue;
            }
            // las columnas grises caidas: sueltas, flotando, con la base metida en el agua
            if (capa == "cascada_08_columnas_sueltas")
            {
                ArmarColumnasFlotantes(cascada, x, y, orden);
                continue;
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Carpeta}/{capa}.png");
            if (sprite == null) { Debug.LogWarning("Falta la capa " + capa + ".png en " + Carpeta); faltan++; continue; }
            var go = new GameObject(capa);
            go.transform.SetParent(capa.StartsWith("inicio") ? inicio : cascada, false);
            // el pivot de cada imagen esta arriba a la izquierda; el piso de la escena queda en y = 0
            go.transform.localPosition = new Vector3(x / PixelesPorUnidad, (AltoEscenaPx - y) / PixelesPorUnidad, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;   // todo el fondo queda detras (numeros negativos), en el orden del SVG
            // el agua de base de la parte 2 se subio: se estira para abajo, asi sigue llegando al borde de la pantalla
            if (capa == "cascada_00b_agua_base" || capa == "inicio_06_agua" || capa == "inicio_12_agua_frente")
                go.transform.localScale = new Vector3(1f, (AltoEscenaPx - y) / sprite.rect.height, 1f);
            if (CapasDeAgua.Contains(capa)) sr.sharedMaterial = MaterialAgua(capa, sprite);
        }

        ArmarPantalla(cam);

        Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnElBuild();
        Debug.Log($"Escena {RutaEscena} armada con {Capas.Length - faltan} capas. Quedo primera en el build.");
    }

    // ---------- agua que se mece ----------

    private static readonly HashSet<string> CapasDeAgua = new HashSet<string>
    {
        "inicio_06_agua", "inicio_12_agua_frente", "cascada_00b_agua_base", "cascada_09_agua",
    };

    // un material por capa de agua (cada imagen tiene otro tamano), con el shader Pomelo/AguaOndulante
    private static Material MaterialAgua(string capa, Sprite sprite)
    {
        var shader = Shader.Find("Pomelo/AguaOndulante");
        if (shader == null) { Debug.LogWarning("No encontre el shader Pomelo/AguaOndulante: el agua queda quieta."); return null; }
        // todo lo que se anima va en Assets/_Animaciones/_inicio_introduccion (pedido de Caro)
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/Agua_{capa}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        mat.SetFloat("_AnchoPx", sprite.rect.width);
        mat.SetFloat("_AltoPx", sprite.rect.height);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- columnas grises que flotan (cascada_08_columnas_sueltas) ----------

    // altura del agua en la parte 2 (px de la escena, desde arriba): 1995 en el SVG, subida con el resto del agua
    private const float LineaDelAguaPx = 1995f - SubidaAgua;

    // cada columna: su recorte dentro de la capa (px, desde arriba a la izquierda) y la x del medio de su base
    // (lo que queda debajo del agua). Medido sobre el PNG de la capa.
    private static readonly (RectInt recorte, float baseX)[] Columnas =
    {
        (new RectInt(0, 214, 366, 502), 83f),       // la chica, inclinada a la izquierda
        (new RectInt(1103, 0, 387, 756), 1380f),    // la alta, a la derecha
    };
    private const float MargenAguaColumna = 60f;   // cuanto se pasa el agua de adelante a cada lado de la base

    private static void ArmarColumnasFlotantes(Transform padre, int capaX, int capaY, int orden)
    {
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Carpeta}/cascada_08_columnas_sueltas.png");
        if (textura == null) { Debug.LogWarning("Falta la capa cascada_08_columnas_sueltas.png en " + Carpeta); return; }

        int n = Columnas.Length;
        var recortes = new Rect[n];
        var pivotes = new Vector2[n];
        var lugares = new Vector2[n];
        float lineaEnCapa = LineaDelAguaPx - capaY;   // la linea del agua dentro de la capa (desde arriba)
        for (int i = 0; i < n; i++)
        {
            var r = Columnas[i].recorte;
            // en Unity los recortes se miden desde abajo
            recortes[i] = new Rect(r.x, textura.height - (r.y + r.height), r.width, r.height);
            // se mece sobre el medio de su base, a la altura del agua
            pivotes[i] = new Vector2((Columnas[i].baseX - r.x) / r.width, (r.y + r.height - lineaEnCapa) / r.height);
            lugares[i] = new Vector2((capaX + Columnas[i].baseX) / PixelesPorUnidad, (AltoEscenaPx - LineaDelAguaPx) / PixelesPorUnidad);
        }

        var go = new GameObject("Columnas flotantes");
        go.transform.SetParent(padre, false);
        var so = new SerializedObject(go.AddComponent<ColumnasFlotantes>());
        so.FindProperty("textura").objectReferenceValue = textura;
        var pr = so.FindProperty("recortes");
        var pp = so.FindProperty("pivotes");
        var pl = so.FindProperty("lugares");
        pr.arraySize = pp.arraySize = pl.arraySize = n;
        for (int i = 0; i < n; i++)
        {
            pr.GetArrayElementAtIndex(i).rectValue = recortes[i];
            pp.GetArrayElementAtIndex(i).vector2Value = pivotes[i];
            pl.GetArrayElementAtIndex(i).vector2Value = lugares[i];
        }
        so.FindProperty("pixelesPorUnidad").floatValue = PixelesPorUnidad;
        so.FindProperty("ordenDibujo").intValue = orden;
        so.ApplyModifiedPropertiesWithoutUndo();

        // el agua de adelante de cada base (como la de la columna de roca de la cueva): agua translucida con la
        // linea clara arriba, que tapa la base y hace que la columna se vea metida en el agua. Esta quieta: la
        // columna sube y baja detras de ella.
        var agua = AguaDeAdelante();
        if (agua == null) return;
        var mat = MaterialAgua("frente_columnas", agua);
        float altoPx = AltoEscenaPx - LineaDelAguaPx + 2f;
        for (int i = 0; i < n; i++)
        {
            var r = Columnas[i].recorte;
            // el ancho de lo que queda debajo del agua, medido en el PNG
            float desde = MedirBase(textura, r, lineaEnCapa, true), hasta = MedirBase(textura, r, lineaEnCapa, false);
            float anchoPx = hasta - desde + 2f * MargenAguaColumna;
            var a = new GameObject("Agua adelante de la columna " + (i + 1));
            a.transform.SetParent(go.transform, false);
            a.transform.position = new Vector3((capaX + (desde + hasta) / 2f) / PixelesPorUnidad,
                                               (AltoEscenaPx - LineaDelAguaPx + 2f) / PixelesPorUnidad, 0f);
            a.transform.localScale = new Vector3(anchoPx / agua.rect.width, altoPx / agua.rect.height, 1f);
            var sr = a.AddComponent<SpriteRenderer>();
            sr.sprite = agua;
            sr.sortingOrder = orden + 5;   // adelante de las columnas, atras de la capa que sigue
            if (mat != null) sr.sharedMaterial = mat;
        }
    }

    // x (dentro de la capa) donde empieza o termina lo que la columna tiene debajo del agua
    private static float MedirBase(Texture2D textura, RectInt r, float lineaEnCapa, bool izquierda)
    {
        string ruta = AssetDatabase.GetAssetPath(textura);
        var leer = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        leer.LoadImage(File.ReadAllBytes(ruta));
        int min = int.MaxValue, max = int.MinValue;
        for (int yAbajo = 0; yAbajo < leer.height; yAbajo++)
        {
            int yArriba = leer.height - 1 - yAbajo;
            if (yArriba < lineaEnCapa || yArriba < r.y || yArriba >= r.y + r.height) continue;
            for (int x = r.x; x < r.x + r.width && x < leer.width; x++)
                if (leer.GetPixel(x, yAbajo).a > 0.08f) { if (x < min) min = x; if (x > max) max = x; }
        }
        Object.DestroyImmediate(leer);
        if (max < min) return izquierda ? r.x : r.x + r.width;
        return izquierda ? min : max + 1;
    }

    // la imagen del agua de adelante: los colores del agua del dibujo (#7b988d arriba -> #374a43 abajo) al 60 %,
    // con la linea clara (#d5decf) en la superficie, y que se desvanece a los costados
    private static Sprite AguaDeAdelante()
    {
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/Agua_frente_columnas.png";
        const int W = 256, H = 168;
        Color arriba = new Color32(0x7b, 0x98, 0x8d, 255), abajo = new Color32(0x37, 0x4a, 0x43, 255);
        Color linea = new Color32(0xd5, 0xde, 0xcf, 255);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, desdeArriba = (H - 1 - y) / (float)(H - 1);
                float costados = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.22f)) *
                                 Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.22f));
                Color c = Color.Lerp(arriba, abajo, desdeArriba);
                float alfa = 0.6f;
                if (H - 1 - y < 4) { c = linea; alfa = 0.9f; }   // la linea de la superficie
                c.a = alfa * costados;
                px[y * W + x] = c;
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
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.TopCenter;   // se ubica por el medio de la linea del agua
            ajustes.spritePivot = new Vector2(0.5f, 1f);
            ajustes.spritePixelsPerUnit = PixelesPorUnidad;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ---------- puntitos de luz (inicio_17_particulas) ----------

    // los 40 puntitos del SVG de Caro: x, y en px del cuadro de 1234 x 738; z = radio del halo
    private static readonly Vector3[] PuntitosSvg =
    {
        new Vector3(388f, 655f, 4.7f), new Vector3(888f, 585f, 3.5f), new Vector3(936f, 442f, 3.4f), new Vector3(530f, 413f, 4.6f),
        new Vector3(829f, 591f, 4.6f), new Vector3(773f, 517f, 2.7f), new Vector3(864f, 445f, 3.0f), new Vector3(847f, 648f, 2.9f),
        new Vector3(556f, 594f, 3.7f), new Vector3(626f, 446f, 4.0f), new Vector3(383f, 398f, 2.5f), new Vector3(592f, 386f, 3.9f),
        new Vector3(689f, 561f, 2.7f), new Vector3(870f, 433f, 3.4f), new Vector3(956f, 442f, 4.4f), new Vector3(616f, 481f, 4.0f),
        new Vector3(807f, 391f, 3.1f), new Vector3(783f, 651f, 3.7f), new Vector3(911f, 484f, 4.4f), new Vector3(906f, 491f, 3.5f),
        new Vector3(915f, 680f, 4.4f), new Vector3(598f, 557f, 4.5f), new Vector3(683f, 541f, 4.5f), new Vector3(559f, 491f, 4.2f),
        new Vector3(559f, 429f, 2.4f), new Vector3(605f, 447f, 4.0f), new Vector3(625f, 578f, 2.3f), new Vector3(791f, 658f, 4.2f),
        new Vector3(489f, 482f, 3.1f), new Vector3(378f, 429f, 2.6f), new Vector3(726f, 564f, 2.3f), new Vector3(877f, 555f, 3.4f),
        new Vector3(553f, 416f, 3.3f), new Vector3(384f, 399f, 4.1f), new Vector3(887f, 671f, 3.3f), new Vector3(554f, 474f, 2.3f),
        new Vector3(536f, 460f, 4.8f), new Vector3(457f, 676f, 2.2f), new Vector3(833f, 419f, 4.1f), new Vector3(694f, 580f, 3.3f),
    };

    private static void ArmarDestellos(Transform padre, int orden)
    {
        // la capa original de los puntitos: cada uno se recorta de ahi, asi se ven igual que en el dibujo
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Carpeta}/inicio_17_particulas.png");
        if (textura == null) { Debug.LogWarning("Falta la capa inicio_17_particulas.png en " + Carpeta); return; }
        var lugarCapa = Capas.First(c => c.capa == "inicio_17_particulas");
        const float k = 3612f / 1234f;   // px del SVG -> px de la escena

        int n = PuntitosSvg.Length;
        var recortes = new Rect[n];
        var centros = new Vector2[n];
        var lugares = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            var p = PuntitosSvg[i];
            float cx = p.x * k - lugarCapa.x;                         // centro dentro de la capa (desde la izquierda)
            float cy = textura.height - (p.y * k - lugarCapa.y);      // (desde abajo)
            float medio = Mathf.Ceil(p.z * k + 1.6f * k * 3f + 2f);   // el halo + su desenfoque
            float x0 = Mathf.Max(0f, Mathf.Floor(cx - medio)), y0 = Mathf.Max(0f, Mathf.Floor(cy - medio));
            float x1 = Mathf.Min(textura.width, Mathf.Ceil(cx + medio)), y1 = Mathf.Min(textura.height, Mathf.Ceil(cy + medio));
            recortes[i] = Rect.MinMaxRect(x0, y0, x1, y1);
            centros[i] = new Vector2((cx - x0) / (x1 - x0), (cy - y0) / (y1 - y0));
            lugares[i] = new Vector2(p.x * k / PixelesPorUnidad, (AltoEscenaPx - p.y * k) / PixelesPorUnidad);
        }

        var go = new GameObject("Destellos (puntitos de luz)");
        go.transform.SetParent(padre, false);
        var so = new SerializedObject(go.AddComponent<Destellos>());
        so.FindProperty("textura").objectReferenceValue = textura;
        var pr = so.FindProperty("recortes");
        var pc = so.FindProperty("centros");
        var pl = so.FindProperty("lugares");
        pr.arraySize = pc.arraySize = pl.arraySize = n;
        for (int i = 0; i < n; i++)
        {
            pr.GetArrayElementAtIndex(i).rectValue = recortes[i];
            pc.GetArrayElementAtIndex(i).vector2Value = centros[i];
            pl.GetArrayElementAtIndex(i).vector2Value = lugares[i];
        }
        so.FindProperty("pixelesPorUnidad").floatValue = PixelesPorUnidad;
        so.FindProperty("ordenDibujo").intValue = orden;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // las importa como sprite, con el pivot arriba a la izquierda (como las coordenadas del SVG) y sin achicarlas
    private static void PrepararImagenes()
    {
        foreach (var (capa, _, _) in Capas)
        {
            string ruta = $"{Carpeta}/{capa}.png";
            if (AssetImporter.GetAtPath(ruta) is not TextureImporter imp) continue;
            // si ya estan bien importadas no se tocan: reimportar las 32 imagenes grandes (hasta 3612 x 2160)
            // cada vez que se rearma la escena tarda mucho y llena la memoria
            var actuales = new TextureImporterSettings();
            imp.ReadTextureSettings(actuales);
            if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single &&
                imp.maxTextureSize == 4096 && actuales.spriteAlignment == (int)SpriteAlignment.TopLeft &&
                Mathf.Approximately(actuales.spritePixelsPerUnit, PixelesPorUnidad))
                continue;
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
    // los assets de las animaciones de la pantalla (el shader del agua y sus materiales). Los scripts van en _Scripts.
    private const string CarpetaAnimaciones = "Assets/_Animaciones/_inicio_introduccion";
    private const string CarpetaIosevka = "Assets/_Fuentes/Iosevka";
    private const int PaddingIosevka = 48;   // margen de cada letra en el atlas (a 90 px): da lugar al brillo ancho
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
        var raizLienzo = (RectTransform)go.transform;

        // el titulo y "Comenzar" van juntos para poder desvanecerlos al comenzar
        var lienzo = new GameObject("Textos", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        lienzo.SetParent(raizLienzo, false);
        lienzo.anchorMin = Vector2.zero;
        lienzo.anchorMax = Vector2.one;
        lienzo.offsetMin = lienzo.offsetMax = Vector2.zero;
        var grupoTextos = lienzo.GetComponent<CanvasGroup>();
        grupoTextos.interactable = false;
        grupoTextos.blocksRaycasts = false;

        // titulo, con brillo
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
        negro.SetParent(raizLienzo, false);
        negro.anchorMin = Vector2.zero;
        negro.anchorMax = Vector2.one;
        negro.offsetMin = negro.offsetMax = Vector2.zero;
        var imgNegro = negro.GetComponent<Image>();
        imgNegro.color = new Color(0f, 0f, 0f, 0f);
        imgNegro.raycastTarget = false;

        var so = new SerializedObject(go.GetComponent<PantallaInicio>());
        so.FindProperty("camara").objectReferenceValue = cam;
        so.FindProperty("fundido").objectReferenceValue = imgNegro;
        so.FindProperty("textos").objectReferenceValue = grupoTextos;
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
        if (existente != null && existente.atlasPadding >= PaddingIosevka) return existente;
        if (existente != null) AssetDatabase.DeleteAsset(ruta);   // la vieja tenia poco margen para el brillo

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        // margen MUY grande alrededor de cada letra: el brillo de TextMeshPro solo llega hasta donde llega ese margen,
        // y el del titulo es ancho (como la referencia de Caro)
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, PaddingIosevka, GlyphRenderMode.SDFAA, 2048, 2048,
                                               AtlasPopulationMode.Dynamic, false);
        if (fa == null) { Debug.LogError("No pude crear la fuente de TextMeshPro a partir de " + ttf); return null; }
        fa.name = "Iosevka Charon Mono SDF";
        AssetDatabase.CreateAsset(fa, ruta);
        fa.atlasTextures[0].name = "Iosevka Charon Mono SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = "Iosevka Charon Mono SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        // solo las letras de titulos (con tanto margen no entran todas): mayusculas, minusculas, numeros y acentos.
        // Despues queda estatica, asi el archivo no cambia solo.
        fa.TryAddCharacters(" ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚÑÜabcdefghijklmnopqrstuvwxyzáéíóúñü0123456789.,:;!¡?¿-'\"",
                            out string faltan);
        if (!string.IsNullOrEmpty(faltan)) Debug.Log("Iosevka: no entraron estas letras: " + faltan);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        var so = new SerializedObject(fa);
        var limpiar = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (limpiar != null) { limpiar.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(fa);
        EditorUtility.SetDirty(fa.atlasTextures[0]);
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

        // brillo como la referencia de Caro: blanco, fuerte pegado a las letras y que se va apagando lejos
        // (ancho: llega hasta el margen de la fuente, PaddingIosevka)
        mat.EnableKeyword("GLOW_ON");
        // (Caro: "demasiado brillo, bajale un monton" -> mucho mas transparente y mas cerca de las letras)
        // (y despues "todavia mas": casi transparente y mas cerca, asi tampoco se nota el borde cuadrado del halo)
        mat.SetColor("_GlowColor", new Color(1f, 1f, 1f, 0.14f));
        mat.SetFloat("_GlowOffset", 0f);
        mat.SetFloat("_GlowInner", 0f);
        mat.SetFloat("_GlowOuter", 0.3f);
        mat.SetFloat("_GlowPower", 0.45f);   // menos de 1: cae rapido cerca de la letra y deja una cola suave

        // sombra apenas marcada, muy difuminada, solo para que el brillo no se pierda sobre lo claro del fondo
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color32(0x14, 0x1a, 0x18, 70));
        mat.SetFloat("_UnderlayOffsetX", 0f);
        mat.SetFloat("_UnderlayOffsetY", 0f);
        mat.SetFloat("_UnderlayDilate", 0.6f);
        mat.SetFloat("_UnderlaySoftness", 1f);

        ShaderUtilities.UpdateShaderRatios(mat);
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
