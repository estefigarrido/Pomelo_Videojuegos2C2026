using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu:  Pomelo -> Dialogos -> Armar cuadro de dialogo
// Arma (o rearma) el cuadro de dialogo en el Canvas del HUD, con las medidas del Figma
// (pagina PANTALLAS LORE, "Frame 3": 1631 x 256, en x 155, y 721 de la pantalla de 1920 x 1080):
//  - fondo negro al 60 % (1621 x 244, corrido 7 / 6);
//  - marco a la izquierda y el mismo marco girado 180 grados a la derecha (162 x 256);
//  - nombre en Alegreya Bold 32 en versalitas (104 / 46) y texto en Alegreya Regular 36 (104 / 100, 1427 de ancho);
//  - icono de seguir (») o de fin (tilde) arriba a la derecha (1553 / 42), blanco al 60 %;
//  - detras de todo, la ilustracion de pantalla completa (apagada hasta que una linea la usa).
// Despues se puede retocar a mano: el componente CuadroDialogo solo usa las referencias.
public static class ArmarCuadroDialogo
{
    private const string Carpeta = "Assets/_UI/Dialogo_y_lore/";
    private const string FuenteNombre = "Assets/_Fuentes/Alegreya/Alegreya Bold SDF.asset";
    private const string FuenteTexto = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";

    [MenuItem("Pomelo/Dialogos/Armar cuadro de dialogo")]
    public static void Armar()
    {
        if (Application.isPlaying) { Debug.LogError("[Dialogos] Salí del modo Play para armar el cuadro."); return; }

        var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
            .OrderByDescending(c => c.name == "Canvas").FirstOrDefault();
        if (canvas == null) { Debug.LogError("[Dialogos] No encontré el Canvas del HUD (Screen Space - Overlay)."); return; }

        var viejo = canvas.transform.Find("Cuadro de dialogo");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        // raiz: ocupa toda la pantalla, invisible hasta que se abre un dialogo
        var raiz = Nuevo("Cuadro de dialogo", canvas.transform);
        Estirar(raiz);
        var grupo = raiz.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f; grupo.interactable = false; grupo.blocksRaycasts = false;

        // ilustracion de pantalla completa
        var ilu = Nuevo("Ilustracion", raiz);
        Estirar(ilu);
        var grupoIlu = ilu.gameObject.AddComponent<CanvasGroup>();
        grupoIlu.alpha = 0f; grupoIlu.interactable = false; grupoIlu.blocksRaycasts = false;
        var imgIlu = ilu.gameObject.AddComponent<Image>();
        imgIlu.raycastTarget = false;

        // el cuadro: abajo, como en el Figma (x 155 de 1920 => corrido 10.5 a la derecha del centro; 103 del borde)
        var cuadro = Nuevo("Cuadro", raiz);
        cuadro.anchorMin = cuadro.anchorMax = new Vector2(0.5f, 0f);
        cuadro.pivot = new Vector2(0.5f, 0f);
        cuadro.sizeDelta = new Vector2(1631f, 256f);
        cuadro.anchoredPosition = new Vector2(10.5f, 103f);

        var fondo = Imagen("Fondo", cuadro, null, new Color(0f, 0f, 0f, 0.6f), new Vector2(7f, 6f), new Vector2(1621f, 244f));
        var marco = SpriteDe(Carpeta + "marco_dialogo.png");
        Imagen("Marco izquierdo", cuadro, marco, Color.white, Vector2.zero, new Vector2(162f, 256f));
        var der = Imagen("Marco derecho", cuadro, marco, Color.white, new Vector2(1469f, 0f), new Vector2(162f, 256f));
        // el mismo marco girado 180 grados alrededor de su centro
        der.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        der.rectTransform.anchoredPosition = new Vector2(1469f + 81f, -128f);
        der.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);

        var nombre = Texto("Nombre", cuadro, FuenteNombre, 32f, FontStyles.SmallCaps, new Vector2(104f, 46f), new Vector2(1300f, 44f), false);
        nombre.text = "Lady Li Hua";
        var texto = Texto("Texto", cuadro, FuenteTexto, 36f, FontStyles.Normal, new Vector2(104f, 100f), new Vector2(1427f, 130f), true);
        texto.text = "“Es admirable tu valor para entrar aquí y querer conocer los saberes olvidados.”";

        var gris = new Color(1f, 1f, 1f, 0.6f);
        var seguir = Imagen("Icono seguir", cuadro, SpriteDe(Carpeta + "dialogo_icono_A.png"), gris, new Vector2(1553f, 42f), new Vector2(30f, 30f));
        var fin = Imagen("Icono fin", cuadro, SpriteDe(Carpeta + "dailogo_icono_B.png"), gris, new Vector2(1553f, 42f), new Vector2(30f, 31f));
        fin.enabled = false;

        var cd = raiz.gameObject.AddComponent<CuadroDialogo>();
        var so = new SerializedObject(cd);
        so.FindProperty("cuadro").objectReferenceValue = cuadro;
        so.FindProperty("nombre").objectReferenceValue = nombre;
        so.FindProperty("texto").objectReferenceValue = texto;
        so.FindProperty("iconoSeguir").objectReferenceValue = seguir;
        so.FindProperty("iconoFin").objectReferenceValue = fin;
        so.FindProperty("ilustracion").objectReferenceValue = imgIlu;
        so.FindProperty("grupoIlustracion").objectReferenceValue = grupoIlu;
        so.ApplyModifiedPropertiesWithoutUndo();

        raiz.SetAsLastSibling();   // por encima del resto del HUD
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Armar cuadro de dialogo");
        EditorSceneManager.MarkSceneDirty(raiz.gameObject.scene);
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("[Dialogos] Cuadro de dialogo armado en '" + canvas.name + "'. Guardá la escena.");
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

    // posicion medida desde la esquina de arriba a la izquierda del cuadro (como en el Figma)
    private static void Ubicar(RectTransform rt, Vector2 desdeArribaIzquierda, Vector2 tamano)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = new Vector2(desdeArribaIzquierda.x, -desdeArribaIzquierda.y);
    }

    private static Image Imagen(string nombre, Transform padre, Sprite sprite, Color color, Vector2 pos, Vector2 tamano)
    {
        var rt = Nuevo(nombre, padre);
        Ubicar(rt, pos, tamano);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Texto(string nombre, Transform padre, string fuente, float tamano, FontStyles estilo,
                                         Vector2 pos, Vector2 caja, bool varias)
    {
        var rt = Nuevo(nombre, padre);
        Ubicar(rt, pos, caja);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fuente);
        if (f != null) t.font = f; else Debug.LogWarning("[Dialogos] No encontré la fuente " + fuente);
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = varias ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Sprite SpriteDe(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogWarning("[Dialogos] No encontré el sprite " + ruta);
        return s;
    }
}
