using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Arma la pantalla "Ingresaste al Templo de botanica" (Assets/_Pantallas_info/Entrada_templo_botánica) en el Canvas de
// Inicio_introduccion. La arma sola el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción ; para rearmar solo
// esta pantalla:  Pomelo -> Pantallas -> Armar entrada al templo  (con la escena abierta).
// Medidas del Figma (1920 x 1080), desde arriba a la izquierda, como la Estadistica final:
//  - fondo: el templo (Principio_del_nivel_fondo) con el gradiente oscuro encima (Principio_del_nivel_gradiente);
//  - el emblema de florecitas, "ingresaste al" (40, versalitas, blanco al 80 %), "Templo de botánica" (64,
//    mayusculas), el ornamento y los dos textos (32, una linea cada uno; "3 partes de la estatua" y "Umbrae" en verde
//    C6D94C y versalitas). Sin brillos detras.
public static class ArmarEntradaTemplo
{
    public const string Nombre = "Entrada al templo";
    private const string Carpeta = "Assets/_Pantallas_info/Entrada_templo_botánica/";
    private const string CarpetaFuente = "Assets/_Fuentes/Alegreya/";
    private const string Fuente = CarpetaFuente + "Alegreya SDF.asset";

    private static readonly Color Blanco80 = new Color(1f, 1f, 1f, 0.8f);
    private const string Verde = "#C6D94C";

    [MenuItem("Pomelo/Pantallas/Armar entrada al templo")]
    private static void ArmarDesdeElMenu()
    {
        if (Application.isPlaying) { Debug.LogError("[Entrada templo] Salí del modo Play para armarla."); return; }
        var canvas = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(g => (g.name == "UI" || g.name == "Canvas") && g.GetComponent<Canvas>() != null);
        if (canvas == null) { Debug.LogError("[Entrada templo] No encontré el Canvas ('UI', el del cuadro de dialogo) en la escena."); return; }
        var raiz = Armar(canvas.transform);
        Undo.RegisterCreatedObjectUndo(raiz, "Armar entrada al templo");
        PonerPuerta();
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("[Entrada templo] Pantalla armada. Guardá la escena.");
    }

    // Pone en el portal dorado de la parte 2 (Group 290) la puerta que se abre y deja entrar a la chica (la que sube y
    // baja es Group 289). Los busca por nombre en la escena abierta (sin contar las raices viejas que esta rearmando el
    // menu de la escena), asi funciona aunque se hayan movido o reacomodado a mano.
    public static bool PonerPuerta()
    {
        bool Valido(Component c) => c != null && !c.transform.root.name.StartsWith("__viejo");
        var dibujos = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(Valido).ToArray();
        var portal = dibujos.FirstOrDefault(s => s.name.EndsWith(" Group 290"));
        var puerta = dibujos.FirstOrDefault(s => s.name.EndsWith(" Group 289"));
        var pantalla = Object.FindObjectsByType<EntradaTemplo>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(Valido);
        // si no esta (por ejemplo, si la escena se habia armado sin ella y la mezcla de ajustes creyo que se borro a
        // mano), se arma de nuevo en el Canvas
        if (pantalla == null)
        {
            var canvas = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .FirstOrDefault(g => (g.name == "UI" || g.name == "Canvas") && g.GetComponent<Canvas>() != null);
            if (canvas == null) Debug.LogWarning("[Entrada templo] No encontré el Canvas ('UI') de la escena para armar la pantalla.");
            else
            {
                pantalla = Armar(canvas.transform).GetComponent<EntradaTemplo>();
                Debug.Log("[Entrada templo] La pantalla no estaba en la escena: se armo de nuevo.");
            }
        }
        if (portal == null || puerta == null || pantalla == null)
        {
            Debug.LogWarning("[Entrada templo] No pude poner la puerta: " + (portal == null ? "falta el portal (Group 290) " : "") +
                             (puerta == null ? "falta la puerta (Group 289) " : "") + (pantalla == null ? "falta la pantalla" : ""));
            return false;
        }
        foreach (var vieja in portal.GetComponents<PuertaDelTemplo>()) Object.DestroyImmediate(vieja);
        var pt = portal.gameObject.AddComponent<PuertaDelTemplo>();
        if (pt == null) { Debug.LogError("[Entrada templo] No se pudo agregar PuertaDelTemplo (¿hay errores de compilacion?)."); return false; }
        var so = new SerializedObject(pt);
        so.FindProperty("portal").objectReferenceValue = portal;
        so.FindProperty("puerta").objectReferenceValue = puerta;
        so.FindProperty("pantalla").objectReferenceValue = pantalla;
        so.FindProperty("quieta").objectReferenceValue = SpritePorGuid(GuidQuieta);
        var giro = GuidsGiro.Select(SpritePorGuid).Where(s => s != null).ToArray();
        var lista = so.FindProperty("framesGiro");
        lista.arraySize = giro.Length;
        for (int i = 0; i < giro.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = giro[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(portal.gameObject);
        Debug.Log("[Entrada templo] Puerta lista en '" + portal.name + "' (se abre '" + puerta.name + "').");
        return true;
    }

    // la chica quieta y dandose vuelta (chica_entradaportal): los mismos dibujos que usa la puerta del Nivel 1
    private const string GuidQuieta = "f246479d5783035438e7d2f1468faa88";
    private static readonly string[] GuidsGiro =
    {
        "b0cd9ed11f5eac5439952b11e38de262", "1d334c6df22bcec4b856d5740d052070", "8b627964fdfc39f46979326fc2b9d21c",
    };

    // Arma la pantalla dentro de 'canvas' (si ya estaba, la rehace). La puerta se pone aparte (PonerPuerta).
    public static GameObject Armar(Transform canvas) => ArmarPantalla(canvas);

    private static Sprite SpritePorGuid(string guid)
    {
        string ruta = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(ruta)) { Debug.LogWarning("[Entrada templo] No encontré el dibujo de la chica " + guid); return null; }
        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
    }

    private static GameObject ArmarPantalla(Transform canvas)
    {
        var vieja = canvas.Find(Nombre);
        if (vieja != null) Object.DestroyImmediate(vieja.gameObject);

        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fuente);
        var matNormal = fuente != null ? fuente.material : null;

        var raiz = Nuevo(Nombre, canvas);
        Estirar(raiz);
        raiz.gameObject.AddComponent<CanvasGroup>();
        // por encima de todo lo demas de la pantalla (cuadro de dialogo, carteles)
        var propio = raiz.gameObject.AddComponent<Canvas>();
        propio.overrideSorting = true;
        propio.sortingOrder = 50;
        raiz.gameObject.AddComponent<GraphicRaycaster>();

        // fondo: el templo y el gradiente oscuro encima (los dos de pantalla completa)
        var fondo = Nuevo("Fondo", raiz);
        Estirar(fondo);
        var cgFondo = fondo.gameObject.AddComponent<CanvasGroup>();
        var imgTemplo = Imagen("Templo", fondo, ImportarImagen(Carpeta + "Principio_del_nivel_fondo.png", false), Color.white, 0f, 0f, 1920f, 1080f);
        imgTemplo.raycastTarget = true;   // tapa los clicks al juego mientras se ve
        Estirar(imgTemplo.rectTransform);
        var imgGradiente = Imagen("Gradiente", fondo, ImportarImagen(Carpeta + "Principio_del_nivel_gradiente.png", true), Color.white, 0f, 0f, 1920f, 1080f);
        Estirar(imgGradiente.rectTransform);

        // como el diseno (Caro, captura del Figma): sin brillos detras, todo centrado y en peso normal.
        // El emblema y el ornamento son los SVG de la carpeta pasados a PNG (4x), porque la UI no muestra SVG vectoriales.

        // 1) emblema de florecitas (110 x 140)
        var pEmblema = Parte("Emblema", raiz);
        var emblema = Imagen("Emblema", pEmblema, ImportarImagen(Carpeta + "Florecitas_inicio_botánica.png", true), Color.white, 905f, 172f, 110f, 140f);
        emblema.preserveAspect = true;

        // 2) "ingresaste al" + "Templo de botanica"
        var pTitulo = Parte("Titulo", raiz);
        Texto("Ingresaste al", pTitulo, fuente, matNormal, "ingresaste al", 40f, FontStyles.SmallCaps, Blanco80,
              660f, 347f, 600f, 54f, TextAlignmentOptions.Center, false);
        Texto("Templo", pTitulo, fuente, matNormal, "Templo de botánica", 64f, FontStyles.UpperCase, Color.white,
              460f, 398f, 1000f, 87f, TextAlignmentOptions.Center, false);

        // 3) ornamento
        var pOrnamento = Parte("Ornamento", raiz);
        var orn = Imagen("Ornamento", pOrnamento, ImportarImagen(Carpeta + "Ornamento_inicio_botánica.png", true), Color.white, 779.5f, 507f, 361f, 71f);
        orn.preserveAspect = true;

        // 4) el objetivo (una linea), con lo importante en verde y versalitas
        var pObjetivo = Parte("Objetivo", raiz);
        Texto("Objetivo", pObjetivo, fuente, matNormal,
              "Consigue las " + Resaltar("3 partes de la estatua") + " de la maestra de botánica y derrota al " +
              Resaltar("Umbrae") + " para salir del templo",
              32f, FontStyles.Normal, Blanco80, 160f, 695f, 1600f, 44f, TextAlignmentOptions.Center, false);

        // 5) la nota de abajo
        var pNota = Parte("Nota", raiz);
        Texto("Nota", pNota, fuente, matNormal,
              "Reconstruye todo el templo para conseguir un mayor desempeño como maestra botánica",
              32f, FontStyles.Normal, Blanco80, 160f, 762f, 1600f, 44f, TextAlignmentOptions.Center, false);

        var et = raiz.gameObject.AddComponent<EntradaTemplo>();
        var so = new SerializedObject(et);
        so.FindProperty("fondo").objectReferenceValue = cgFondo;
        var partes = new[] { pEmblema, pTitulo, pOrnamento, pObjetivo, pNota };
        var lista = so.FindProperty("partes");
        lista.arraySize = partes.Length;
        for (int i = 0; i < partes.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = partes[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        raiz.SetAsLastSibling();
        return raiz.gameObject;
    }

    private static string Resaltar(string s) => "<smallcaps><color=" + Verde + ">" + s + "</color></smallcaps>";

    private static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = padre.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // cada parte ocupa toda la pantalla (asi sus hijos usan las medidas del Figma) y tiene su CanvasGroup
    private static CanvasGroup Parte(string nombre, Transform padre)
    {
        var rt = Nuevo(nombre, padre);
        Estirar(rt);
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        return cg;
    }

    // medidas del Figma: x, y desde arriba a la izquierda de la pantalla de 1920 x 1080
    private static void Ubicar(RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w / 2f - 960f, 540f - (y + h / 2f));
    }

    private static Image Imagen(string nombre, Component padre, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var rt = Nuevo(nombre, padre.transform);
        Ubicar(rt, x, y, w, h);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Texto(string nombre, Component padre, TMP_FontAsset fuente, Material material, string texto,
                                         float tamano, FontStyles estilo, Color color, float x, float y, float w, float h,
                                         TextAlignmentOptions alineacion, bool variasLineas)
    {
        var rt = Nuevo(nombre, padre.transform);
        Ubicar(rt, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        if (material != null) t.fontSharedMaterial = material;
        t.richText = true;
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = alineacion;
        t.textWrappingMode = variasLineas ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.fontFeatures.Clear();
        return t;
    }

    private static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogWarning("[Entrada templo] No encontré el sprite " + ruta);
        return s;
    }

    // las imagenes de pantalla completa: sprite simple, sin achicar (1920 x 1080); el gradiente sin comprimir para
    // que no se vean escalones
    private static Sprite ImportarImagen(string ruta, bool sinComprimir)
    {
        if (AssetImporter.GetAtPath(ruta) is TextureImporter ti)
        {
            var compresion = sinComprimir ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            if (ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single ||
                ti.maxTextureSize < 2048 || ti.mipmapEnabled || ti.textureCompression != compresion)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.maxTextureSize = 2048;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.textureCompression = compresion;
                ti.SaveAndReimport();
            }
        }
        return CargarSprite(ruta);
    }
}
