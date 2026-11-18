using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu:  Pomelo -> Pantallas -> Armar estadistica final
// Arma (o rearma) la pantalla "Estadistica final" (Figma, pagina PANTALLAS LORE, 1920 x 1080) en el Canvas del HUD,
// por encima de todo. Las medidas son las del Figma, desde la esquina de arriba a la izquierda de la pantalla.
//  - fondo negro al 75 % sobre el juego;
//  - emblema de botanica (con su resplandor), "completaste el" (Alegreya 40, versalitas, blanco al 80 %),
//    "Templo de botanica" (64, mayusculas, con brillo), el ornamento, "¡Bien hecho!" (40, versalitas, con brillo),
//    el texto (32, blanco al 80 %), "Desempeno: 67 / 100" entre dos lineas (32; el numero en verde C6D94C) y la nota
//    de abajo (24, blanco al 80 %).
// El Figma usa Alegreya Medium y Alegreya SC: en el proyecto esta Alegreya Regular, asi que los textos "Medium" usan
// un material con la letra un poco engrosada, y las mayusculas de Alegreya SC salen con Regular en mayusculas.
// Despues se puede retocar a mano: el componente EstadisticaFinal solo usa las referencias.
public static class ArmarEstadisticaFinal
{
    private const string Carpeta = "Assets/_UI/Dialogo_y_lore/";
    private const string CarpetaFuente = "Assets/_Fuentes/Alegreya/";
    private const string Fuente = CarpetaFuente + "Alegreya SDF.asset";

    private static readonly Color Blanco80 = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color Verde = new Color32(0xC6, 0xD9, 0x4C, 0xFF);

    [MenuItem("Pomelo/Pantallas/Armar estadistica final")]
    public static void Armar()
    {
        if (Application.isPlaying) { Debug.LogError("[Estadistica final] Salí del modo Play para armarla."); return; }

        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .OrderByDescending(c => c.name == "Canvas").FirstOrDefault();
        if (canvas == null) { Debug.LogError("[Estadistica final] No encontré el Canvas del HUD (Screen Space - Overlay)."); return; }

        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fuente);
        if (fuente == null) { Debug.LogError("[Estadistica final] No encontré " + Fuente); return; }
        var matNormal = fuente.material;
        var matMedio = CrearMaterial("Alegreya SDF Medio", fuente, 0.12f, false);
        var matBrillo = CrearMaterial("Alegreya SDF Brillo", fuente, 0f, true);
        var matMedioBrillo = CrearMaterial("Alegreya SDF Medio Brillo", fuente, 0.12f, true);

        var viejo = canvas.transform.Find("Estadistica final");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        var raiz = Nuevo("Estadistica final", canvas.transform);
        Estirar(raiz);
        raiz.gameObject.AddComponent<CanvasGroup>();

        // fondo negro al 75 % sobre el juego
        var fondo = Nuevo("Fondo", raiz);
        Estirar(fondo);
        var cgFondo = fondo.gameObject.AddComponent<CanvasGroup>();
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.color = new Color(0f, 0f, 0f, 0.75f);
        imgFondo.raycastTarget = true;   // tapa los clicks al juego mientras se ve

        var halo = CargarSprite(Carpeta + "estadistica_halo.png");

        // 1) emblema (el PNG ya trae el resplandor: 190 x 220 alrededor de los 110 x 140 del dibujo)
        var pEmblema = Parte("Emblema", raiz);
        Imagen("Emblema", pEmblema, CargarSprite(Carpeta + "estadistica_emblema.png"), Color.white, 865f, 105f, 190f, 220f);

        // 2) "completaste el" + "Templo de botanica"
        var pTitulo = Parte("Titulo", raiz);
        Imagen("Brillo", pTitulo, halo, new Color(1f, 1f, 1f, 0.12f), 560f, 350f, 800f, 130f);
        Texto("Completaste el", pTitulo, fuente, matNormal, "completaste el", 40f, FontStyles.SmallCaps, Blanco80,
              660f, 318f, 600f, 54f, TextAlignmentOptions.Center);
        Texto("Templo", pTitulo, fuente, matBrillo, "Templo de botánica", 64f, FontStyles.UpperCase, Color.white,
              460f, 372f, 1000f, 87f, TextAlignmentOptions.Center);

        // 3) ornamento
        var pOrnamento = Parte("Ornamento", raiz);
        Imagen("Ornamento", pOrnamento, CargarSprite(Carpeta + "estadistica_ornamento.png"), Color.white, 758f, 480f, 395f, 78f);

        // 4) "¡Bien hecho!"
        var pBien = Parte("Bien hecho", raiz);
        Imagen("Brillo", pBien, halo, new Color(1f, 1f, 1f, 0.10f), 760f, 590f, 400f, 90f);
        Texto("Bien hecho", pBien, fuente, matMedioBrillo, "¡Bien hecho!", 40f, FontStyles.SmallCaps, Color.white,
              660f, 607f, 600f, 54f, TextAlignmentOptions.Center);

        // 5) texto
        var pTexto = Parte("Texto", raiz);
        Texto("Texto", pTexto, fuente, matNormal,
              "Lograste derrotar a las sombras que destruían al templo y restauraste la estatua del maestro",
              32f, FontStyles.Normal, Blanco80, 210f, 686f, 1500f, 44f, TextAlignmentOptions.Center);

        // 6) desempeno entre dos lineas (Frame 1: 357 x 76, en 758 / 773)
        var pDesempeno = Parte("Desempeno", raiz);
        Imagen("Linea arriba", pDesempeno, null, Color.white, 758f, 773f, 357f, 1f);
        Imagen("Linea abajo", pDesempeno, null, Color.white, 758f, 848f, 357f, 1f);
        Texto("Desempeno", pDesempeno, fuente, matMedio, "Desempeño:", 32f, FontStyles.SmallCaps, Color.white,
              758f, 789f, 220f, 44f, TextAlignmentOptions.Left);
        var valor = Texto("Valor", pDesempeno, fuente, matMedio, "67 / 100", 32f, FontStyles.SmallCaps, Verde,
                          935f, 789f, 180f, 44f, TextAlignmentOptions.Right);

        // 7) nota de abajo
        var pPie = Parte("Pie", raiz);
        Texto("Pie", pPie, fuente, matNormal, "Siempre puedes volver a este templo para terminar de restaurarlo",
              24f, FontStyles.Normal, Blanco80, 210f, 886f, 1500f, 32f, TextAlignmentOptions.Center);

        var ef = raiz.gameObject.AddComponent<EstadisticaFinal>();
        var so = new SerializedObject(ef);
        so.FindProperty("fondo").objectReferenceValue = cgFondo;
        var partes = new[] { pEmblema, pTitulo, pOrnamento, pBien, pTexto, pDesempeno, pPie };
        var lista = so.FindProperty("partes");
        lista.arraySize = partes.Length;
        for (int i = 0; i < partes.Length; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = partes[i];
        so.FindProperty("valor").objectReferenceValue = valor;
        so.FindProperty("parteDesempeno").objectReferenceValue = pDesempeno;
        so.ApplyModifiedPropertiesWithoutUndo();

        raiz.SetAsLastSibling();   // por encima de todo el HUD
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Armar estadistica final");
        EditorSceneManager.MarkSceneDirty(raiz.gameObject.scene);
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("[Estadistica final] Pantalla armada en '" + canvas.name + "'. Aparece sola al cerrarse la ultima puerta. Guardá la escena.");
    }

    // material de la fuente: 'dilatar' engrosa la letra (Medium). El brillo de los titulos NO va en la letra
    // (el underlay de TextMeshPro se corta en el borde de cada caracter y queda una mancha gris): lo hace el halo
    // suave que va detras ('brillo' queda solo para distinguir los materiales).
    private static Material CrearMaterial(string nombre, TMP_FontAsset fuente, float dilatar, bool brillo)
    {
        string ruta = CarpetaFuente + nombre + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        bool nuevo = m == null;
        if (nuevo) m = new Material(fuente.material) { name = nombre };
        m.SetFloat("_FaceDilate", dilatar);
        m.DisableKeyword("UNDERLAY_ON");
        m.SetColor("_UnderlayColor", new Color(1f, 1f, 1f, 0f));
        if (nuevo) AssetDatabase.CreateAsset(m, ruta); else EditorUtility.SetDirty(m);
        return m;
    }

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
                                         TextAlignmentOptions alineacion)
    {
        var rt = Nuevo(nombre, padre.transform);
        Ubicar(rt, x, y, w, h);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = fuente;
        t.fontSharedMaterial = material;
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        t.fontFeatures.Clear();
        return t;
    }

    private static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogWarning("[Estadistica final] No encontré el sprite " + ruta);
        return s;
    }
}
