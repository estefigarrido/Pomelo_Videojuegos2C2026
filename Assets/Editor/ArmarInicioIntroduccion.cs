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

        // Si la escena ya existe, se rearma ENCIMA de ella y se conservan los ajustes hechos a mano (ver
        // ConservarAjustes): se saca una foto de como esta, se arma todo de nuevo y se le vuelven a poner los cambios
        // a mano. Si esta abierta, se usa tal cual (con lo que no se haya guardado todavia).
        Scene escena;
        ConservarAjustes.Foto actual = null, anterior = null;
        var viejas = new List<GameObject>();
        if (File.Exists(RutaEscena))
        {
            var activa = SceneManager.GetActiveScene();
            if (activa.path == RutaEscena) escena = activa;
            else
            {
                // si la escena abierta (Nivel1_Templo) tiene cambios sin guardar, pregunta antes de cerrarla
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
            viejas = escena.GetRootGameObjects().Where(g => RaicesDelMenu.Contains(g.name)).ToList();
            actual = ConservarAjustes.Tomar(viejas);
            anterior = ConservarAjustes.LeerBase(RutaBase);
            foreach (var g in viejas) g.name = "__viejo " + g.name;   // asi no se confunden con las nuevas
        }
        else
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        carpeta = null;   // la busca de nuevo, por si se movio
        PrepararImagenes();

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
        GameObject chica = null;
        colocadas.Clear();
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
            // los bloques negros y la vasija: separados en piezas y agrandados para la chica, que va parada en el
            // primero (adelante de los bloques)
            if (capa == "cascada_10_plataformas")
            {
                // los de segundo plano van detras del arco (que esta 4 capas antes: orden - 40)
                ArmarBloques(cascada, capa, x, y, orden, orden - 45);
                chica = PonerChica(cascada, orden + 5);
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
            // la plantita de la cueva se mece con la brisa
            if (capa == "inicio_13_planta") sr.sharedMaterial = MaterialAnimado("Pomelo/PlantaViento", "Planta_viento");
            // las tiras de agua de las plataformas de la derecha corren para abajo
            if (capa == "cascada_07_plataformas_de_agua_derecha")
            {
                var m = MaterialAnimado("Pomelo/CascaditasAgua", "Cascaditas_agua");
                if (m != null) { m.SetFloat("_AltoPx", sprite.rect.height); sr.sharedMaterial = m; }
                // agrandadas para la chica, contra el borde derecho
                AgrandarPlataformasDerecha(go.transform, capa, y, sprite);
            }
        }

        ArmarPisosParte2(cascada);
        ArmarPantalla(cam, chica);

        // la "base" para la proxima vez: lo que arma el menu, sin los ajustes a mano
        var nuevas = escena.GetRootGameObjects().Where(g => RaicesDelMenu.Contains(g.name)).ToList();
        var nuevo = ConservarAjustes.Tomar(nuevas);
        ConservarAjustes.GuardarBase(nuevo, RutaBase);

        int ajustes = 0;
        if (actual != null)
        {
            ajustes = ConservarAjustes.Mezclar(actual, nuevo, anterior, escena);
            // la primera vez (sin base) no se puede distinguir un ajuste a mano de un cambio del codigo: los bloques
            // de segundo plano se dejan con el color nuevo (Caro pidio que se noten mas)
            if (anterior == null)
                foreach (var sr in cascada.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.name.EndsWith("(fondo)")) sr.color = ColorFondo;
        }
        foreach (var g in viejas) if (g != null) Object.DestroyImmediate(g);

        Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnElBuild();
        Debug.Log($"Escena {RutaEscena} armada con {Capas.Length - faltan} capas. Quedo primera en el build." +
                  (actual != null ? $" Se conservaron {ajustes} ajustes hechos a mano." : ""));
    }

    // los objetos de primer nivel que arma este menu (los demas que haya en la escena no se tocan)
    private static readonly HashSet<string> RaicesDelMenu = new HashSet<string>
    {
        "Main Camera", "Luz global", "Escenario", "Pantalla de inicio",
    };
    // como dejo el menu la escena la ultima vez (para saber que se cambio a mano)
    private const string RutaBase = "Assets/Editor/ArmarInicioIntroduccion_base.json";

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

    // un material con uno de los shaders de animacion de la pantalla (se guarda en CarpetaAnimaciones)
    private static Material MaterialAnimado(string nombreShader, string nombre)
    {
        var shader = Shader.Find(nombreShader);
        if (shader == null) { Debug.LogWarning("No encontre el shader " + nombreShader + ": queda sin animar."); return null; }
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/{nombre}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- la chica en la parte 2 ----------

    // el dibujo de la chica quieta (el de ChicaIdle): se busca por su GUID, asi no importa si se mueve de carpeta
    private const string GuidChicaQuieta = "f246479d5783035438e7d2f1468faa88";
    // donde pisa: sobre el primer bloque negro de las plataformas (el circulo rojo de Caro), a 50 px (sin agrandar)
    // del borde izquierdo del bloque
    private const float ChicaEnElBloquePx = 50f;
    // tamano: en el nivel mide 2.4 unidades; aca el dibujo de la cueva esta a otra escala y se ve como en la captura
    // de Caro con 1.4
    private const float EscalaChica = 1.4f;

    // ---------- bloques y plataformas agrandados para la chica ----------
    // Caro: las estructuras de abajo tienen que ser proporcionales a la chica, para que se pueda mover comoda.
    // Los bloques negros y la vasija (cascada_10_plataformas) se separan en piezas y cada una se agranda
    // EscalaBloques desde su base; quedan en el mismo orden, uno al lado del otro, con los espacios entre ellos un
    // poco mas chicos (EspacioEntreBloques) para que entren en la parte 2. Las plataformas con agua de la derecha
    // (cascada_07) se agrandan EscalaPlataformasDerecha, apoyadas en su base y contra el borde derecho.
    private const float EscalaBloques = 1.6f;
    private const float EspacioEntreBloques = 0.75f;
    private const float InicioBloquesPx = 3680f;              // borde izquierdo del primer bloque
    private const float EscalaPlataformasDerecha = 1.4f;
    private const float BordeDerechoPlataformasPx = 7192f;    // borde derecho de las plataformas de la derecha

    // lo que se agrando: la imagen, la zona de ella que se usa, donde quedo (esquina de arriba a la izquierda, px de
    // la escena) y cuanto se agrando. Con esto se arman los pisos invisibles y se ubica a la chica.
    // go = el objeto (los pisos van como componentes de el, asi lo siguen si se mueve o agranda a mano);
    // f = pixeles de la imagen por pixel de la escena (3 en las piedras en alta resolucion)
    private class Colocada { public string ruta; public RectInt zona; public float x, y, escala, f = 1f; public GameObject go; }
    private static readonly List<Colocada> colocadas = new List<Colocada>();

    // separa la capa de bloques en piezas (cada tramo de columnas con algo dibujado), guarda cada pieza como PNG
    // y la pone agrandada
    private static void ArmarBloques(Transform padre, string capa, int capaX, int capaY, int orden, int ordenFondo)
    {
        string ruta = $"{Carpeta}/{capa}.png";
        if (!File.Exists(ruta)) { Debug.LogWarning("Falta la capa " + ruta); return; }
        // las piedras se ven agrandadas: si esta la version en alta resolucion (capa_hd.png, redibujada desde el
        // SVG a 3x), se usa esa, asi no se pixelan. f = cuantos pixeles de la imagen hay por pixel de la escena.
        float anchoEscena = AssetDatabase.LoadAssetAtPath<Sprite>(ruta)?.rect.width ?? 0f;
        string rutaHd = $"{Carpeta}/{capa}_hd.png";
        if (File.Exists(rutaHd)) ruta = rutaHd;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(ruta));
        int W = tex.width, H = tex.height;
        float f = anchoEscena > 0f ? W / anchoEscena : 1f;
        var hay = new bool[W];
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H && !hay[x]; y++)
                hay[x] = tex.GetPixel(x, y).a > 0.15f;
        var piezas = new List<RectInt>();
        for (int x = 0, desde = -1; x <= W; x++)
        {
            bool on = x < W && hay[x];
            if (on && desde < 0) desde = x;
            if (!on && desde >= 0) { piezas.Add(new RectInt(desde, 0, x - desde, H)); desde = -1; }
        }

        // cada pieza como PNG aparte (en la carpeta piezas, al lado de las capas)
        string carpetaPiezas = $"{Carpeta}/piezas";
        Directory.CreateDirectory(carpetaPiezas);
        var rutas = new List<string>();
        for (int i = 0; i < piezas.Count; i++)
        {
            var z = piezas[i];
            var p = new Texture2D(z.width, z.height, TextureFormat.RGBA32, false);
            p.SetPixels(tex.GetPixels(z.x, 0, z.width, z.height));
            p.Apply();
            string rp = $"{carpetaPiezas}/{capa}_pieza_{i + 1}.png";
            File.WriteAllBytes(rp, p.EncodeToPNG());
            Object.DestroyImmediate(p);
            rutas.Add(rp);
        }
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();

        float x0 = InicioBloquesPx;
        float baseY = capaY + H / f;   // la base de los bloques (debajo del agua) queda donde estaba
        for (int i = 0; i < piezas.Count; i++)
        {
            var z = piezas[i];
            if (i > 0) x0 += (z.x - (piezas[i - 1].x + piezas[i - 1].width)) / f * EspacioEntreBloques;
            var sprite = ImportarPieza(rutas[i], PixelesPorUnidad * f);
            if (sprite == null) continue;
            bool fondo = PiezasDeFondo.Contains(i + 1);
            bool vasija = i + 1 == PiezaVasija;
            float escala = fondo ? EscalaBloquesFondo : EscalaBloques;
            float anchoPx = z.width / f, altoPx = z.height / f;   // en px de la escena, sin agrandar
            float yArriba = baseY - (fondo ? SubidaBloquesFondo : 0f) - altoPx * escala;

            if (vasija)
            {
                // la vasija flota (como las columnas), con agua delante de su base. No se puede pisar.
                float centro = x0 + anchoPx * escala / 2f;
                var raiz = Flotar(padre, "Vasija flotante", sprite, escala, centro, LineaDelAguaPx, x0, yArriba, orden, 0.9f);
                AguaDelante(padre, centro, anchoPx * escala * 0.8f, orden + 3);
                // un brillo suave con su misma forma, detras, que late despacio: se nota que es "especial"
                PonerBrillo(raiz, rutas[i], f, escala, orden - 1);
            }
            else
            {
                var go = new GameObject($"{capa} pieza {i + 1}" + (fondo ? " (fondo)" : ""));
                go.transform.SetParent(padre, false);
                go.transform.localPosition = new Vector3(x0 / PixelesPorUnidad, (AltoEscenaPx - yArriba) / PixelesPorUnidad, 0f);
                go.transform.localScale = Vector3.one * escala;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if (fondo)
                {
                    // mas atras: mas chicas, un poco mas arriba, apagadas por la niebla del agua, detras del arco y
                    // de las columnas, con agua delante de la base, y sin piso (no se pueden pisar)
                    sr.color = ColorFondo;
                    sr.sortingOrder = ordenFondo;
                    AguaDelante(padre, x0 + anchoPx * escala / 2f, anchoPx * escala, ordenFondo + 1);
                }
                else
                {
                    sr.sortingOrder = orden;
                    colocadas.Add(new Colocada { ruta = ruta, zona = z, x = x0, y = yArriba, escala = escala, f = f, go = go });
                }
            }
            x0 += anchoPx * escala;
        }
    }

    // ---------- brillo de la vasija ----------

    private static readonly Color ColorBrillo = new Color32(0xee, 0xf4, 0xe2, 255);   // la luz clara de la cueva
    private const float MargenBrillo = 22f;    // cuanto se abre el brillo alrededor del dibujo (px de la escena)
    private const float DesenfoqueBrillo = 9f; // que tan difuminado (px de la escena)

    // Arma una imagen con la forma del dibujo, agrandada y difuminada (un halo), la guarda al lado de la pieza y la
    // pone detras del dibujo, dentro del objeto que flota (asi flota con el). Late suave con Resplandor.
    private static void PonerBrillo(Transform raiz, string rutaPieza, float f, float escala, int orden)
    {
        var dibujo = raiz.Find("Dibujo");
        if (dibujo == null || !File.Exists(rutaPieza)) return;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(rutaPieza));
        int pad = Mathf.CeilToInt((MargenBrillo + DesenfoqueBrillo * 2f) * f);
        int W = tex.width + 2 * pad, H = tex.height + 2 * pad;
        var a = new float[W * H];
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
                a[(y + pad) * W + x + pad] = tex.GetPixel(x, y).a;
        Object.DestroyImmediate(tex);

        // agrandar la forma (MargenBrillo) y difuminarla (DesenfoqueBrillo): varias pasadas de promedio
        int abre = Mathf.Max(1, Mathf.RoundToInt(MargenBrillo * f / 3f));
        for (int i = 0; i < 3; i++) a = Promedio(a, W, H, abre, true);
        for (int i = 0; i < 3; i++) a = Promedio(a, W, H, Mathf.Max(1, Mathf.RoundToInt(DesenfoqueBrillo * f / 2f)), false);

        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++)
        {
            var c = ColorBrillo;
            c.a = Mathf.Clamp01(a[i]);
            px[i] = c;
        }
        var salida = new Texture2D(W, H, TextureFormat.RGBA32, false);
        salida.SetPixels(px);
        salida.Apply();
        string ruta = Path.Combine(Path.GetDirectoryName(rutaPieza), Path.GetFileNameWithoutExtension(rutaPieza) + "_brillo.png").Replace('\\', '/');
        File.WriteAllBytes(ruta, salida.EncodeToPNG());
        Object.DestroyImmediate(salida);
        AssetDatabase.Refresh();
        var sprite = ImportarPieza(ruta, PixelesPorUnidad * f);
        if (sprite == null) return;

        var go = new GameObject("Brillo");
        go.transform.SetParent(raiz, false);
        float corre = pad / (PixelesPorUnidad * f) * escala;   // el margen de la imagen, en unidades
        go.transform.localPosition = dibujo.localPosition + new Vector3(-corre, corre, 0f);
        go.transform.localScale = Vector3.one * escala;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        sr.color = new Color(1f, 1f, 1f, 0.45f);
        go.AddComponent<Resplandor>();
    }

    // promedio en una caja de radio r, primero horizontal y despues vertical. 'maximo' = en vez de promediar, toma
    // el valor mas alto (para agrandar la forma)
    private static float[] Promedio(float[] a, int W, int H, int r, bool maximo)
    {
        var tmp = new float[a.Length];
        var res = new float[a.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float acc = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int xx = Mathf.Clamp(x + k, 0, W - 1);
                    float v = a[y * W + xx];
                    acc = maximo ? Mathf.Max(acc, v) : acc + v;
                }
                tmp[y * W + x] = maximo ? acc : acc / (2 * r + 1);
            }
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float acc = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int yy = Mathf.Clamp(y + k, 0, H - 1);
                    float v = tmp[yy * W + x];
                    acc = maximo ? Mathf.Max(acc, v) : acc + v;
                }
                res[y * W + x] = maximo ? acc : acc / (2 * r + 1);
            }
        return res;
    }

    // las piezas de la capa de bloques (contando desde 1, de izquierda a derecha) que van "en segundo plano": los
    // bloquecitos con la raya de agua clara (los circulos rojos de Caro). Y cual es la vasija.
    private static readonly HashSet<int> PiezasDeFondo = new HashSet<int> { 2, 4, 6 };
    private const int PiezaVasija = 5;
    private const float EscalaBloquesFondo = 1.2f;
    private const float SubidaBloquesFondo = 30f;   // px: un poco mas arriba, como si estuvieran mas lejos
    // semitransparentes: como son casi negras, oscurecerlas no alcanza; asi se mezclan con el agua y el fondo y se
    // ven lejos, como en la niebla
    // (Caro: al 50 % no se veian; al 85 % se notan, pero siguen un poco apagadas, como mas lejos)
    private static readonly Color ColorFondo = new Color(0.85f, 0.92f, 0.88f, 0.85f);

    private static Sprite ImportarPieza(string ruta, float pixelesPorUnidad = PixelesPorUnidad)
    {
        if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.TopLeft;
            ajustes.spritePivot = new Vector2(0f, 1f);
            ajustes.spritePixelsPerUnit = pixelesPorUnidad;
            ajustes.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(ajustes);
            imp.maxTextureSize = 4096;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // las plataformas de la derecha: agrandadas desde su base, contra el borde derecho
    private static void AgrandarPlataformasDerecha(Transform t, string capa, int capaY, Sprite sprite)
    {
        float w = sprite.rect.width, h = sprite.rect.height;
        float x = BordeDerechoPlataformasPx - w * EscalaPlataformasDerecha;
        float y = capaY + h - h * EscalaPlataformasDerecha;
        t.localPosition = new Vector3(x / PixelesPorUnidad, (AltoEscenaPx - y) / PixelesPorUnidad, 0f);
        t.localScale = Vector3.one * EscalaPlataformasDerecha;
        colocadas.Add(new Colocada { ruta = $"{Carpeta}/{capa}.png", zona = new RectInt(0, 0, (int)w, (int)h),
                                     x = x, y = y, escala = EscalaPlataformasDerecha, go = t.gameObject });
    }

    // La chica jugable: el prefab Assets/_Prefabs/Chica.prefab es una copia de la del nivel (con todos sus
    // componentes: moverse, correr, saltar, dash, atacar, pasos, vida). Arranca quieta y PantallaInicio le da los
    // controles cuando la camara llega a la parte 2.
    private const string RutaPrefabChica = "Assets/_Prefabs/Chica.prefab";

    private static GameObject PonerChica(Transform padre, int orden)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabChica);
        GameObject go;
        if (prefab != null) go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            // sin el prefab, al menos el dibujo quieto
            Debug.LogWarning("No encontre " + RutaPrefabChica + ": la chica queda quieta, sin controles.");
            var sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(GuidChicaQuieta))
                .OfType<Sprite>().FirstOrDefault();
            if (sprite == null) return null;
            go = new GameObject("Chica");
            go.AddComponent<SpriteRenderer>().sprite = sprite;
        }
        go.name = "Chica";
        go.transform.SetParent(padre, false);
        go.transform.localScale = Vector3.one * EscalaChica;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerID = 0;   // la capa Default, como todo el dibujo de la escena
        sr.sortingOrder = orden;
        // sobre el primer bloque (ya agrandado): x a ChicaEnElBloquePx del borde, y en el borde de arriba ahi
        float xPx = 4025f, pisoPx = 1932f;
        var primero = colocadas.FirstOrDefault(c => c.ruta.Contains("cascada_10_plataformas"));
        if (primero != null)
        {
            var arriba = BordeDeArriba(primero);
            int col = Mathf.Clamp(Mathf.RoundToInt(ChicaEnElBloquePx * primero.f), 0, arriba.Length - 1);
            xPx = primero.x + ChicaEnElBloquePx * primero.escala;
            pisoPx = primero.y + Mathf.Max(0, arriba[col]) / primero.f * primero.escala;
        }
        // los pies DIBUJADOS quedan sobre la piedra. El colisionador de la chica termina un poquito mas arriba que
        // sus pies dibujados: los pisos se suben esa diferencia (pisoSobreLaPiedra), asi parada se ve apoyada.
        float pies = sr.sprite != null ? sr.sprite.bounds.min.y : 0f;
        pisoSobreLaPiedra = 0f;
        if (go.TryGetComponent<CapsuleCollider2D>(out var capsula))
            pisoSobreLaPiedra = Mathf.Max(0f, (capsula.offset.y - capsula.size.y / 2f - pies) * EscalaChica);
        go.transform.position = new Vector3(xPx / PixelesPorUnidad,
                                            (AltoEscenaPx - pisoPx) / PixelesPorUnidad - pies * EscalaChica + 0.02f, 0f);
        return go;
    }

    // cuanto (unidades) quedan los pisos invisibles por encima del borde dibujado de las piedras (ver PonerChica)
    private static float pisoSobreLaPiedra;

    // para cada columna de la zona: la fila (desde arriba) donde empieza lo dibujado, -1 si no hay nada
    private static int[] BordeDeArriba(Colocada c)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(c.ruta));
        int H = tex.height;
        var arriba = new int[c.zona.width];
        for (int i = 0; i < arriba.Length; i++)
        {
            arriba[i] = -1;
            for (int yArriba = c.zona.y; yArriba < c.zona.y + c.zona.height; yArriba++)
                if (tex.GetPixel(c.zona.x + i, H - 1 - yArriba).a > 0.6f) { arriba[i] = yArriba - c.zona.y; break; }
        }
        Object.DestroyImmediate(tex);
        return arriba;
    }

    // ---------- pisos de la parte 2 (para que la chica pueda caminar y saltar) ----------

    // Pisos invisibles sobre algo agrandado: lo recorre de izquierda a derecha y, donde el borde de arriba de lo
    // dibujado se mantiene parejo, le agrega una caja (BoxCollider2D) AL MISMO OBJETO, en su espacio: asi, si se
    // mueve o se agranda a mano, el piso lo acompana. 'piedra' = suena como plataforma de piedra al caer.
    private static void PisosDe(Colocada c, bool piedra)
    {
        if (c.go == null) return;
        var arriba = BordeDeArriba(c);
        int W = arriba.Length, H = c.zona.height;
        float ppu = PixelesPorUnidad * c.f;                       // pixeles de la imagen por unidad (sin agrandar)
        float subir = pisoSobreLaPiedra / Mathf.Max(0.01f, c.escala);   // en unidades del objeto
        int margen = Mathf.RoundToInt(3 * c.f), minimo = Mathf.RoundToInt(8 * c.f);
        int desde = -1, tope = 0;
        for (int x = 0; x <= W; x++)
        {
            bool sigue = x < W && arriba[x] >= 0 && desde >= 0 && Mathf.Abs(arriba[x] - tope) <= margen;
            if (sigue) { tope = Mathf.Min(tope, arriba[x]); continue; }
            if (desde >= 0 && x - desde >= minimo)
            {
                var caja = c.go.AddComponent<BoxCollider2D>();
                float ancho = (x - desde) / ppu, alto = (H - tope) / ppu + subir;
                caja.size = new Vector2(ancho, alto);
                // el objeto tiene el punto arriba a la izquierda: y para abajo es negativo
                caja.offset = new Vector2(desde / ppu + ancho / 2f, -tope / ppu + subir - alto / 2f);
            }
            desde = x < W && arriba[x] >= 0 ? x : -1;
            tope = desde >= 0 ? arriba[x] : 0;
        }
        if (piedra && c.go.GetComponent<PisoPlataformaPiedra>() == null) c.go.AddComponent<PisoPlataformaPiedra>();
    }

    // caja invisible: x, y = esquina de arriba a la izquierda en px de la escena
    private static void Caja(Transform padre, string nombre, float xPx, float yPx, float anchoPx, float altoPx, bool piedra)
    {
        var go = new GameObject("Piso " + nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3((xPx + anchoPx / 2f) / PixelesPorUnidad,
                                            (AltoEscenaPx - yPx - altoPx / 2f) / PixelesPorUnidad, 0f);
        var caja = go.AddComponent<BoxCollider2D>();
        caja.size = new Vector2(anchoPx / PixelesPorUnidad, altoPx / PixelesPorUnidad);
        if (piedra) go.AddComponent<PisoPlataformaPiedra>();
    }

    private static void ArmarPisosParte2(Transform padre)
    {
        var pisos = new GameObject("Pisos (invisibles)").transform;
        pisos.SetParent(padre, false);
        foreach (var c in colocadas) PisosDe(c, true);
        // el agua: se puede pisar apenas metida (los pies quedan 30 px debajo de la superficie), asi no se cae al vacio
        float inicioX = 3611f, finX = 7202f;
        Caja(pisos, "agua", inicioX, LineaDelAguaPx + 30f, finX - inicioX, 200f, false);
        // paredes en los bordes de la parte 2
        Caja(pisos, "pared izquierda", inicioX - 100f, -2000f, 100f, 4160f, false);
        Caja(pisos, "pared derecha", finX, -2000f, 100f, 4160f, false);
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
        // cada columna se recorta de la capa como pieza propia (queda en la escena, se ve tambien sin darle Play)
        // y flota sobre el medio de su base, a la altura del agua
        string capa = "cascada_08_columnas_sueltas";
        for (int i = 0; i < Columnas.Length; i++)
        {
            var r = Columnas[i].recorte;
            var sprite = GuardarPieza($"{Carpeta}/{capa}.png", r, $"{capa}_columna_{i + 1}");
            if (sprite == null) continue;
            Flotar(padre, "Columna flotante " + (i + 1), sprite, 1f, capaX + Columnas[i].baseX, LineaDelAguaPx,
                   capaX + r.x, capaY + r.y, orden, i * 1.7f);
            // el agua de adelante de la base: la columna sube y baja detras de ella
            AguaDelante(padre, capaX + Columnas[i].baseX, MedirAnchoBajoElAgua(capa, r, LineaDelAguaPx - capaY), orden + 3);
        }
    }

    // recorta una zona de una imagen (x, y desde arriba a la izquierda) y la guarda como PNG en la carpeta piezas
    private static Sprite GuardarPieza(string rutaImagen, RectInt zona, string nombre)
    {
        if (!File.Exists(rutaImagen)) { Debug.LogWarning("Falta " + rutaImagen); return null; }
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(rutaImagen));
        var p = new Texture2D(zona.width, zona.height, TextureFormat.RGBA32, false);
        p.SetPixels(tex.GetPixels(zona.x, tex.height - (zona.y + zona.height), zona.width, zona.height));
        p.Apply();
        string carpetaPiezas = $"{Carpeta}/piezas";
        Directory.CreateDirectory(carpetaPiezas);
        string ruta = $"{carpetaPiezas}/{nombre}.png";
        File.WriteAllBytes(ruta, p.EncodeToPNG());
        Object.DestroyImmediate(p);
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();
        return ImportarPieza(ruta);
    }

    // algo que flota: un objeto en el punto de la base (a la altura del agua) con el componente Flotante, y el
    // dibujo de hijo, en su lugar. pivote / esquina en px de la escena (esquina = arriba a la izquierda del dibujo).
    private static Transform Flotar(Transform padre, string nombre, Sprite sprite, float escala, float pivoteX, float pivoteY,
                                    float esquinaX, float esquinaY, int orden, float desfase)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = new Vector3(pivoteX / PixelesPorUnidad, (AltoEscenaPx - pivoteY) / PixelesPorUnidad, 0f);
        var dibujo = new GameObject("Dibujo");
        dibujo.transform.SetParent(raiz.transform, false);
        dibujo.transform.localPosition = new Vector3((esquinaX - pivoteX) / PixelesPorUnidad, (pivoteY - esquinaY) / PixelesPorUnidad, 0f);
        dibujo.transform.localScale = Vector3.one * escala;
        var sr = dibujo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        var so = new SerializedObject(raiz.AddComponent<Flotante>());
        so.FindProperty("desfase").floatValue = desfase;
        so.ApplyModifiedPropertiesWithoutUndo();
        return raiz.transform;
    }

    // agua translucida delante de la base de algo (como la de la columna de roca de la cueva): tapa la base y hace
    // que se vea metido en el agua. centroX y ancho en px de la escena (el ancho sin los margenes).
    private static void AguaDelante(Transform padre, float centroX, float anchoPx, int orden)
    {
        var agua = AguaDeAdelante();
        if (agua == null) return;
        var mat = MaterialAgua("frente_columnas", agua);
        float altoPx = AltoEscenaPx - LineaDelAguaPx + 2f;
        float ancho = anchoPx + 2f * MargenAguaColumna;
        var a = new GameObject("Agua adelante");
        a.transform.SetParent(padre, false);
        a.transform.position = new Vector3(centroX / PixelesPorUnidad, (AltoEscenaPx - LineaDelAguaPx + 2f) / PixelesPorUnidad, 0f);
        a.transform.localScale = new Vector3(ancho / agua.rect.width, altoPx / agua.rect.height, 1f);
        var sr = a.AddComponent<SpriteRenderer>();
        sr.sprite = agua;
        sr.sortingOrder = orden;
        if (mat != null) sr.sharedMaterial = mat;
    }

    // ancho de lo que una zona de una capa tiene debajo de la linea del agua (px)
    private static float MedirAnchoBajoElAgua(string capa, RectInt r, float lineaEnCapa)
    {
        var leer = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        leer.LoadImage(File.ReadAllBytes($"{Carpeta}/{capa}.png"));
        int min = int.MaxValue, max = int.MinValue;
        for (int yArriba = Mathf.Max(r.y, Mathf.CeilToInt(lineaEnCapa)); yArriba < r.y + r.height && yArriba < leer.height; yArriba++)
            for (int x = r.x; x < r.x + r.width && x < leer.width; x++)
                if (leer.GetPixel(x, leer.height - 1 - yArriba).a > 0.08f) { if (x < min) min = x; if (x > max) max = x; }
        Object.DestroyImmediate(leer);
        return max < min ? r.width : max + 1 - min;
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
        // cada puntito se dibuja solo, como en el SVG (Destellos lo arma con su radio): ya no se recorta de la capa
        const float k = 3612f / 1234f;   // px del SVG -> px de la escena
        int n = PuntitosSvg.Length;
        var go = new GameObject("Destellos (puntitos de luz)");
        go.transform.SetParent(padre, false);
        var so = new SerializedObject(go.AddComponent<Destellos>());
        var pp = so.FindProperty("puntos");
        pp.arraySize = n;
        for (int i = 0; i < n; i++)
        {
            var p = PuntitosSvg[i];
            pp.GetArrayElementAtIndex(i).vector3Value =
                new Vector3(p.x * k / PixelesPorUnidad, (AltoEscenaPx - p.y * k) / PixelesPorUnidad, p.z * k);
        }
        so.FindProperty("desenfoque").floatValue = 1.6f * k;
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

    private static void ArmarPantalla(Camera cam, GameObject chica)
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
        so.FindProperty("chica").objectReferenceValue = chica;
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
            // ya existe: se respetan los ajustes a mano (brillo, sombra...). Solo se asegura que use el atlas de la
            // fuente actual (por si la fuente se rearmo).
            if (fuente.material != null && fuente.material.HasProperty("_MainTex") && mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", fuente.material.GetTexture("_MainTex"));
            EditorUtility.SetDirty(mat);
            return mat;
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
