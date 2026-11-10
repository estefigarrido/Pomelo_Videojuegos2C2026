using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Sistema de carteles de tutorial. Todos salen del mismo molde:
//  - titulo en Alegreya Regular a 32 px, en versalitas (como el Figma); la "o" entre opciones a 15 px
//  - iconos de Assets/_UI/Carteles/Teclas_y_botones a su tamano original
//  - lineas grises debajo de los iconos de compu / joystick (cuando el cartel las lleva)
//  - todo agrandado 3 px (sobre 75 px de alto, el mismo factor para todos)
//  - una franja oscura difuminada y centrada detras de todo el cartel (Frame 57), para que se lea sobre el fondo
//  - CartelTutorial: aparece suave cuando la chica sale por la puerta, se queda, y se va
//    borrando cuando la chica le pasa por delante (no se va solo con el tiempo)
// Cada cartel se guarda como prefab en Assets/_UI/Carteles. Para sumar uno nuevo, se agrega su
// descripcion en Carteles() (medidas en px del Figma, desde la esquina de arriba a la izquierda).
//
// Menu:  Pomelo -> Carteles -> Armar carteles
//   rearma todos los prefabs y deja en la escena el de Caminar al lado del portal de entrada
//   (si ya habia un cartel ahi, toma su lugar). El de Atacar queda solo como prefab.
public static class ArmarCarteles
{
    // ---------- los carteles ----------

    private static List<Cartel> Carteles()
    {
        return new List<Cartel>
        {
            // Frame 57:          [compu]          [joystick]
            //                    -------          ----------
            //          CAMINAR   [<]  [>]    o     [stick R]
            //                    [A]  [D]   (las flechas y A / D se van turnando cada 2.5 s, Frame 60)
            new Cartel("Cartel_caminar", new Vector2(300f, 68f), enEscena: true)
                .Titulo("Caminar", new Vector2(0f, -52.5f))
                .Icono(Teclado + "Computadora.png", new Vector2(199f, -10f))
                .Linea(new Vector2(198.5f, -28f), 71f)
                .Alternar(Teclado + "Tecla_flechaizquierda.png", Teclado + "Tecla_A.png", new Vector2(178f, -52.5f))
                .Alternar(Teclado + "Tecla_flechaderecha.png", Teclado + "Tecla_D.png", new Vector2(220f, -52.5f))
                .O(new Vector2(250f, -54f))
                .Icono(Joystick + "Joystick.png", new Vector2(278f, -11f))
                .Linea(new Vector2(278.5f, -28f), 39f)
                .Icono(Joystick + "Joystick_derecho.png", new Vector2(279f, -51.5f)),

            //          ATACAR   [mouse]   o   [circulo]
            new Cartel("Cartel Atacar", new Vector2(243f, 40f), enEscena: false)
                .Titulo("Atacar", new Vector2(0f, -20f))
                .Icono(Teclado + "Click_derecho.png", new Vector2(161f, -20f)) // el dibujo tiene marcado el boton izquierdo
                .O(new Vector2(194f, -21f))
                .Icono(Joystick + "Boton_Circulo.png", new Vector2(227f, -20f)),
        };
    }

    // ---------- rutas y medidas comunes ----------

    private const string CarpetaCarteles = "Assets/_UI/Carteles/";
    private const string Teclado = CarpetaCarteles + "Teclas_y_botones/Teclado/";
    private const string Joystick = CarpetaCarteles + "Teclas_y_botones/Joystick/";

    private const string RutaTtf = "Assets/_Fuentes/Alegreya/Alegreya-Regular.ttf";
    private const string RutaFontAsset = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string PaqueteEsencialesTMP = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    private const float TamanoTitulo = 32f;
    private const float TamanoO = 15f;
    private const float Escala = 78f / 75f;            // 3 px mas sobre los 75 px del primer cartel
    private const float MargenMascaraX = 90f;          // que la mascara no corte la franja + borde difuminado del borrado
    // franja de sombra (Frame 57): centrada en el cartel, sobresale 44 px a cada costado y 16 px arriba
    // y abajo; negra al 55% en lo mas oscuro
    private static readonly Vector2 MargenFranja = new Vector2(44f, 16f);
    private static readonly Color ColorSombra = new Color(0f, 0f, 0f, 0.55f);
    private const float MargenMascaraY = 24f;          // que la mascara no corte la franja
    private static readonly Color ColorLinea = new Color32(179, 179, 179, 255); // mismo gris que los iconos de compu y joystick

    // lugar del primer cartel (recuadro rojo de la captura): 240 px a la derecha de la chica
    // y 116 px arriba de sus pies cuando sale del portal
    private static readonly Vector2 CentroDesdePies = new Vector2(2.40f, 1.16f);

    [MenuItem("Pomelo/Carteles/Armar carteles")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de armar los carteles.");
            return;
        }

        // TextMeshPro necesita sus archivos basicos (TMP Settings, shaders) dentro de Assets
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            AssetDatabase.ImportPackage(PaqueteEsencialesTMP, false);
            AssetDatabase.Refresh();
            Debug.LogWarning("Importe los archivos basicos de TextMeshPro. Cuando termine, volve a usar  Pomelo -> Carteles -> Armar carteles.");
            return;
        }
        var fuente = FontAssetAlegreya();
        if (fuente == null) return;


        var chica = Object.FindFirstObjectByType<SaludPersonaje>();
        var dibujoChica = chica != null ? chica.GetComponent<SpriteRenderer>() : null;

        // donde estaba el cartel de la escena (si lo habias movido, se respeta)
        var carteles = Carteles();
        Vector3? lugar = null;
        var enLaEscena = new List<GameObject>();
        foreach (var c in carteles)
        {
            var go = GameObject.Find(c.nombre);
            if (go == null) continue;
            if (c.enEscena || lugar == null) lugar = go.transform.position;
            enLaEscena.Add(go);
        }
        foreach (var go in enLaEscena) Undo.DestroyObjectImmediate(go);

        // prefabs
        var prefabs = new Dictionary<Cartel, GameObject>();
        foreach (var c in carteles)
        {
            var p = ArmarPrefab(c, fuente, dibujoChica);
            if (p == null) return;
            prefabs[c] = p;
        }

        // escena: solo los que van en la escena, en el lugar del cartel de antes
        GameObject ultimo = null;
        foreach (var c in carteles.Where(x => x.enEscena))
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[c]);
            Undo.RegisterCreatedObjectUndo(inst, "Armar carteles");
            inst.transform.position = lugar ?? PosicionJuntoAlPortal(chica, dibujoChica);
            ultimo = inst;
        }

        var escena = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        if (ultimo != null) Selection.activeGameObject = ultimo;
        Debug.Log("Carteles armados en " + CarpetaCarteles + ": " + string.Join(", ", carteles.Select(x => x.nombre)) +
                  ". En la escena quedo: " + string.Join(", ", carteles.Where(x => x.enEscena).Select(x => x.nombre)) +
                  ". Guarda la escena con Ctrl+S.");
    }

    // ---------- descripcion de un cartel ----------

    private enum Tipo { Texto, Icono, Linea, Alterno }

    private class Pieza
    {
        public Tipo tipo;
        public string nombre, texto, ruta, ruta2;
        public float tamano;
        public FontStyles estilo;
        public Vector2 posicion, pivot, caja;
    }

    private class Cartel
    {
        public readonly string nombre;
        public readonly Vector2 tamano;
        public readonly bool enEscena;
        public readonly List<Pieza> piezas = new List<Pieza>();

        public Cartel(string nombre, Vector2 tamano, bool enEscena)
        {
            this.nombre = nombre; this.tamano = tamano; this.enEscena = enEscena;
        }

        // titulo: posicion = borde izquierdo, a media altura
        public Cartel Titulo(string texto, Vector2 posicion)
        {
            piezas.Add(new Pieza { tipo = Tipo.Texto, nombre = texto, texto = texto, tamano = TamanoTitulo,
                                   estilo = FontStyles.SmallCaps, posicion = posicion, pivot = new Vector2(0f, 0.5f),
                                   caja = new Vector2(200f, 44f) });
            return this;
        }

        // la "o" entre las opciones: posicion = centro
        public Cartel O(Vector2 posicion)
        {
            piezas.Add(new Pieza { tipo = Tipo.Texto, nombre = "o", texto = "o", tamano = TamanoO, estilo = FontStyles.Normal,
                                   posicion = posicion, pivot = new Vector2(0.5f, 0.5f), caja = new Vector2(20f, 24f) });
            return this;
        }

        // icono a su tamano original: posicion = centro
        public Cartel Icono(string ruta, Vector2 posicion)
        {
            piezas.Add(new Pieza { tipo = Tipo.Icono, nombre = Path.GetFileNameWithoutExtension(ruta), ruta = ruta,
                                   posicion = posicion, pivot = new Vector2(0.5f, 0.5f) });
            return this;
        }

        // dos iconos en el mismo lugar que se van turnando (AlternarTeclas): primero "ruta", despues "ruta2"
        public Cartel Alternar(string ruta, string ruta2, Vector2 posicion)
        {
            piezas.Add(new Pieza { tipo = Tipo.Alterno, ruta = ruta, ruta2 = ruta2,
                                   posicion = posicion, pivot = new Vector2(0.5f, 0.5f) });
            return this;
        }

        // linea gris de 1 px: posicion = centro
        public Cartel Linea(Vector2 posicion, float largo)
        {
            piezas.Add(new Pieza { tipo = Tipo.Linea, nombre = "Linea", posicion = posicion, pivot = new Vector2(0.5f, 0.5f),
                                   caja = new Vector2(largo, 1f) });
            return this;
        }
    }

    // ---------- armado ----------

    private static GameObject ArmarPrefab(Cartel c, TMP_FontAsset fuente, SpriteRenderer dibujoChica)
    {
        // raiz: Canvas en el mundo, 1 px = 0.01 unidades
        var raiz = new GameObject(c.nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(CartelTutorial));
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
        Vector2 tamanoMascara = c.tamano * Escala + 2f * new Vector2(MargenMascaraX, MargenMascaraY);
        rtRaiz.sizeDelta = tamanoMascara;
        rtRaiz.localScale = Vector3.one * 0.01f;
        // z un poquito hacia la camara: le gana a los dibujos con el mismo orden (dientes de leon)
        rtRaiz.position = new Vector3(0f, 0f, -0.05f);

        var rtMascara = Hijo("Mascara", rtRaiz, tamanoMascara);
        var mascara = rtMascara.gameObject.AddComponent<RectMask2D>();
        var contenido = Hijo("Contenido", rtMascara, c.tamano);

        var primeros = new List<Image>();
        var segundos = new List<Image>();
        foreach (var p in c.piezas)
        {
            switch (p.tipo)
            {
                case Tipo.Texto: Texto(contenido, p, fuente); break;
                case Tipo.Linea: Linea(contenido, p); break;
                case Tipo.Icono:
                {
                    var sprite = CargarSprite(p.ruta);
                    if (sprite == null) { Object.DestroyImmediate(raiz); return null; }
                    Icono(contenido, p, sprite, Path.GetFileNameWithoutExtension(p.ruta));
                    break;
                }
                case Tipo.Alterno:
                {
                    var s1 = CargarSprite(p.ruta);
                    var s2 = CargarSprite(p.ruta2);
                    if (s1 == null || s2 == null) { Object.DestroyImmediate(raiz); return null; }
                    primeros.Add(Icono(contenido, p, s1, Path.GetFileNameWithoutExtension(p.ruta)));
                    var img2 = Icono(contenido, p, s2, Path.GetFileNameWithoutExtension(p.ruta2));
                    img2.color = new Color(1f, 1f, 1f, 0f);   // arranca escondido
                    segundos.Add(img2);
                    break;
                }
            }
        }
        if (primeros.Count > 0)
        {
            var alternar = new SerializedObject(contenido.gameObject.AddComponent<AlternarTeclas>());
            var pr = alternar.FindProperty("primero");
            var se = alternar.FindProperty("segundo");
            pr.arraySize = primeros.Count;
            se.arraySize = segundos.Count;
            for (int i = 0; i < primeros.Count; i++) pr.GetArrayElementAtIndex(i).objectReferenceValue = primeros[i];
            for (int i = 0; i < segundos.Count; i++) se.GetArrayElementAtIndex(i).objectReferenceValue = segundos[i];
            alternar.ApplyModifiedPropertiesWithoutUndo();
        }

        // franja oscura difuminada detras de todo el cartel
        var franja = CrearFranja(c.tamano + 2f * MargenFranja, CarpetaCarteles + c.nombre + "_sombra.png");
        if (franja != null)
        {
            var rtS = Hijo("Sombra", contenido, franja.rect.size);
            rtS.SetAsFirstSibling();
            var img = rtS.gameObject.AddComponent<Image>();
            img.sprite = franja;
            img.color = ColorSombra;
            img.raycastTarget = false;
        }

        contenido.localScale = Vector3.one * Escala;

        var so = new SerializedObject(raiz.GetComponent<CartelTutorial>());
        so.FindProperty("mascara").objectReferenceValue = mascara;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, CarpetaCarteles + c.nombre + ".prefab");
        Object.DestroyImmediate(raiz);
        return prefab;
    }

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
    private static RectTransform EnContenido(GameObject go, RectTransform contenido, Pieza p, Vector2 tamano)
    {
        var rt = (RectTransform)go.transform;
        rt.SetParent(contenido, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = p.pivot;
        rt.sizeDelta = tamano;
        rt.anchoredPosition = p.posicion;
        return rt;
    }

    private static void Texto(RectTransform contenido, Pieza p, TMP_FontAsset fuente)
    {
        var go = new GameObject(p.nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        EnContenido(go, contenido, p, p.caja);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.font = fuente;
        t.fontSharedMaterial = fuente.material;
        t.text = p.texto;
        t.fontSize = p.tamano;
        t.fontStyle = p.estilo;
        t.color = Color.white;
        t.alignment = p.pivot.x == 0f ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }

    private static Image Icono(RectTransform contenido, Pieza p, Sprite sprite, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        EnContenido(go, contenido, p, sprite.rect.size);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static void Linea(RectTransform contenido, Pieza p)
    {
        var go = new GameObject(p.nombre, typeof(RectTransform), typeof(Image));
        EnContenido(go, contenido, p, p.caja);
        var img = go.GetComponent<Image>();
        img.color = ColorLinea;
        img.raycastTarget = false;
    }

    private static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogError("No encontre el icono " + ruta);
        return s;
    }

    // Donde sale la chica del portal (InicioNivel.posicionChica) + el lugar del recuadro rojo de la captura.
    private static Vector3 PosicionJuntoAlPortal(SaludPersonaje chica, SpriteRenderer dibujoChica)
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
        return new Vector3(salida.x + centroX + CentroDesdePies.x, salida.y + pies + CentroDesdePies.y, -0.05f);
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

    // ---------- importacion ----------

    private static void ImportarComoSprite(string ruta)
    {
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

    // ---------- franja de sombra ----------

    // Franja oscura detras de todo el cartel, iconos de arriba incluidos (como el Frame 57), centrada:
    // es pareja en el medio y se difumina igual por la izquierda y por la derecha, y apenas arriba y abajo.
    // Se guarda como PNG blanco; el negro lo pone la Image.
    private static Sprite CrearFranja(Vector2 tamano, string ruta)
    {
        int W = Mathf.CeilToInt(tamano.x), H = Mathf.CeilToInt(tamano.y);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                // lo mismo de los dos lados: bordes laterales mas largos, arriba y abajo cortitos
                float horizontal = Mathf.SmoothStep(0f, 1f, u / 0.15f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.15f);
                float vertical = Mathf.SmoothStep(0f, 1f, v / 0.12f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.12f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(horizontal * vertical)));
            }

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        ImportarComoSprite(ruta);
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
