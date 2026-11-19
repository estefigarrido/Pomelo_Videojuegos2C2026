using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu:  Pomelo -> Pantallas -> Armar instruccion ataque
// Arma (o rearma) la pantalla "Instruccion ataque" (Figma, pagina PANTALLAS LORE, 1920 x 1080) en el Canvas del HUD,
// por encima de todo, y la conecta a la runa del conocimiento (el Dialogo de "runa_conocimiento..."):
//  - el dialogo se abre solo la primera vez que la chica pasa; despues, con el cartel Interactuar;
//  - al terminar el dialogo, aparece esta pantalla (Dialogo -> Al Terminar -> InstruccionAtaque.Mostrar).
// Las medidas son las del Figma, desde la esquina de arriba a la izquierda de la pantalla.
// Despues se puede retocar a mano: el componente InstruccionAtaque solo usa las referencias.
public static class ArmarInstruccionAtaque
{
    private const string Carpeta = "Assets/_UI/Dialogo_y_lore/InstruccionAtaque/";
    private const string Teclado = "Assets/_UI/Carteles/Teclas_y_botones/Teclado/";
    private const string Joystick = "Assets/_UI/Carteles/Teclas_y_botones/Joystick/";
    private const string CarpetaFuente = "Assets/_Fuentes/Alegreya/";
    private const string Fuente = CarpetaFuente + "Alegreya SDF.asset";

    private static readonly Color Blanco80 = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color Fondo = new Color(0.0082f, 0.0312f, 0.0157f, 1f);   // #020804
    private const float MargenBrillo = 60f;

    [MenuItem("Pomelo/Pantallas/Armar instruccion ataque")]
    public static void Armar()
    {
        if (Application.isPlaying) { Debug.LogError("[Instruccion ataque] Salí del modo Play para armarla."); return; }

        // (Estefi, 19/11) si ya hay una pantalla, se rearma en el mismo Canvas: antes elegia un Canvas cualquiera y
        // quedaban copias en varios (UI, Menu pausa, Aviso checkpoint) que respondian todas a la B
        var existentes = Object.FindObjectsByType<InstruccionAtaque>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var canvas = existentes.Length > 0 && existentes[0].GetComponentInParent<Canvas>() != null
            ? existentes[0].GetComponentInParent<Canvas>().rootCanvas
            : Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                .OrderByDescending(c => c.name == "Canvas").FirstOrDefault();
        if (canvas != null) foreach (var e in existentes) if (e != null && e.transform.parent != canvas.transform) Undo.DestroyObjectImmediate(e.gameObject);
        if (canvas == null) { Debug.LogError("[Instruccion ataque] No encontré el Canvas del HUD (Screen Space - Overlay)."); return; }

        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fuente);
        if (fuente == null) { Debug.LogError("[Instruccion ataque] No encontré " + Fuente); return; }
        var matNormal = fuente.material;
        var matMedio = AssetDatabase.LoadAssetAtPath<Material>(CarpetaFuente + "Alegreya SDF Medio.mat");
        if (matMedio == null) matMedio = matNormal;
        foreach (var r in new[] { "instruccion_latigo.png", "instruccion_latigo_glow.png", "instruccion_disparo.png", "instruccion_disparo_glow.png" })
            ImportarSprite(Carpeta + r);
        // (Tecla_B queda como esta: el cartel 'Cambiar de ataque' usa su sprite)

        var viejo = canvas.transform.Find("Instruccion ataque");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo.gameObject);

        var raiz = Nuevo("Instruccion ataque", canvas.transform);
        Estirar(raiz);
        raiz.gameObject.AddComponent<CanvasGroup>();

        // fondo de pantalla completa (tapa el juego y los clicks)
        var fondo = Nuevo("Fondo", raiz);
        Estirar(fondo);
        var imgFondo = fondo.gameObject.AddComponent<Image>();
        imgFondo.color = Fondo;
        imgFondo.raycastTarget = true;

        Texto("Texto", raiz, fuente, matNormal,
              "En el camino encontrarás sombras peligrosas y engañosas. Deberás aprender a defenderte de ellas.\n" +
              "Recuerda que un buen maestro no se encariña con una sola herramienta...",
              32f, FontStyles.Normal, Blanco80, 274f, 193f, 1372f, 88f, TextAlignmentOptions.Center);

        // los dos golpes: dibujo (con su brillo detras) y nombre. Los PNG del dibujo traen 40 px de margen
        var latigo = Golpe("Latigo", raiz, fuente, matMedio, matNormal, "instruccion_latigo",
                           330.5f, 368f, 566f, 247f, "Látigo de enredaderas", "Más dañino", 448f, 332f);
        var sabiduria = Golpe("Disparo floral", raiz, fuente, matMedio, matNormal, "instruccion_disparo",
                              1029f, 376.5f, 560f, 244f, "Disparo floral", "Mayor alcance", 1200f, 217f);

        // linea vertical entre los dos
        Imagen("Separador", raiz, null, new Color(1f, 1f, 1f, 0.25f), 959.5f, 367f, 1f, 269f);

        // atacar: computadora / joystick arriba, click izquierdo o ○ abajo
        Imagen("Computadora", raiz, CargarSprite(Teclado + "Computadora.png"), Color.white, 909f, 722f, 32f, 32f);
        Imagen("Joystick", raiz, CargarSprite(Joystick + "Joystick.png"), Color.white, 978f, 722f, 32f, 32f);
        Imagen("Linea izq", raiz, null, new Color(1f, 1f, 1f, 0.5f), 905f, 759f, 39f, 1f);
        Imagen("Linea der", raiz, null, new Color(1f, 1f, 1f, 0.5f), 975f, 759f, 39f, 1f);
        Imagen("Click", raiz, CargarSprite(Teclado + "Click_izquierdo.png"), Color.white, 907f, 770f, 36f, 36f);
        Texto("o", raiz, fuente, matNormal, "o", 15f, FontStyles.SmallCaps, Color.white, 955f, 778f, 9f, 20f, TextAlignmentOptions.Center);
        Imagen("Circulo", raiz, CargarSprite(Joystick + "Boton_Circulo.png"), Color.white, 976f, 769f, 38f, 38f);

        // cambiar de ataque: B o R1 R2
        Texto("Cambiar de ataque", raiz, fuente, matNormal, "Cambiar de ataque", 32f, FontStyles.SmallCaps, Color.white,
              741f, 893f, 267f, 44f, TextAlignmentOptions.Left);
        Imagen("Tecla B", raiz, CargarSprite(Teclado + "Tecla_B.png"), Color.white, 1044f, 897f, 36f, 36f);
        Texto("o cambiar", raiz, fuente, matNormal, "o", 15f, FontStyles.SmallCaps, Color.white, 1086f, 905f, 9f, 20f, TextAlignmentOptions.Center);
        Imagen("R1", raiz, CargarSprite(Joystick + "Boton_R1.png"), Color.white, 1101f, 897f, 36f, 36f);
        Imagen("R2", raiz, CargarSprite(Joystick + "Boton_R2.png"), Color.white, 1143f, 897f, 36f, 36f);

        // continuar (al 60 %): flecha derecha o joystick derecho
        var continuar = Nuevo("Continuar", raiz);
        Estirar(continuar);
        continuar.gameObject.AddComponent<CanvasGroup>().alpha = 0.6f;
        Texto("Continuar", continuar, fuente, matNormal, "Continuar", 32f, FontStyles.SmallCaps, Color.white,
              1503f, 937f, 154f, 44f, TextAlignmentOptions.Left);
        Imagen("Flecha", continuar, CargarSprite(Teclado + "Tecla_flechaderecha.png"), Color.white, 1693f, 941f, 36f, 36f);
        Texto("o continuar", continuar, fuente, matNormal, "o", 15f, FontStyles.SmallCaps, Color.white, 1735f, 949f, 9f, 20f, TextAlignmentOptions.Center);
        Imagen("Joystick derecho", continuar, CargarSprite(Joystick + "Joystick_derecho.png"), Color.white, 1750f, 940f, 38f, 38f);

        var ia = raiz.gameObject.AddComponent<InstruccionAtaque>();
        var so = new SerializedObject(ia);
        Asignar(so.FindProperty("latigo"), latigo);
        Asignar(so.FindProperty("sabiduria"), sabiduria);
        so.FindProperty("fondo").objectReferenceValue = imgFondo;
        so.FindProperty("colorFondo").colorValue = Fondo;
        so.ApplyModifiedPropertiesWithoutUndo();

        raiz.SetAsLastSibling();   // por encima de todo el HUD
        Undo.RegisterCreatedObjectUndo(raiz.gameObject, "Armar instruccion ataque");
        Conectar(ia);
        EditorSceneManager.MarkSceneDirty(raiz.gameObject.scene);
        Selection.activeGameObject = raiz.gameObject;
        Debug.Log("[Instruccion ataque] Pantalla armada en '" + canvas.name + "'. Guardá la escena.");
    }

    private struct PiezasGolpe { public CanvasGroup grupo; public RectTransform dibujo; public Image brillo; }

    private static PiezasGolpe Golpe(string nombre, Transform padre, TMP_FontAsset fuente, Material matMedio, Material matNormal,
                                     string archivo, float x, float y, float w, float h,
                                     string titulo, string bajada, float xTexto, float wTexto)
    {
        var rt = Nuevo(nombre, padre);
        Estirar(rt);
        var g = rt.gameObject.AddComponent<CanvasGroup>();
        g.interactable = false; g.blocksRaycasts = false;
        // el brillo es un drop shadow blanco del dibujo, con 60 px mas de margen de cada lado (para que no se corte)
        var brillo = Imagen("Brillo", rt, CargarSprite(Carpeta + archivo + "_glow.png"), new Color(1f, 1f, 1f, 0.45f),
                            x - MargenBrillo, y - MargenBrillo, w + 2f * MargenBrillo, h + 2f * MargenBrillo);
        var dibujo = Imagen("Dibujo", rt, CargarSprite(Carpeta + archivo + ".png"), Color.white, x, y, w, h);
        Texto("Nombre", rt, fuente, matMedio, titulo, 32f, FontStyles.SmallCaps, Color.white, xTexto, 725f, wTexto, 44f, TextAlignmentOptions.Center);
        Texto("Bajada", rt, fuente, matNormal, bajada, 24f, FontStyles.Normal, Blanco80, xTexto, 770f, wTexto, 24f, TextAlignmentOptions.Center);
        return new PiezasGolpe { grupo = g, dibujo = dibujo.rectTransform, brillo = brillo };
    }

    private static void Asignar(SerializedProperty p, PiezasGolpe g)
    {
        p.FindPropertyRelative("grupo").objectReferenceValue = g.grupo;
        p.FindPropertyRelative("dibujo").objectReferenceValue = g.dibujo;
        p.FindPropertyRelative("brillo").objectReferenceValue = g.brillo;
    }

    // La runa: dialogo solo la primera vez al pasar, despues con Interactuar; al terminar, esta pantalla
    private static void Conectar(InstruccionAtaque ia)
    {
        var runa = Object.FindObjectsByType<Dialogo>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(d => d.name.ToLowerInvariant().StartsWith("runa"));
        if (runa == null) { Debug.LogWarning("[Instruccion ataque] No encontré la runa (un objeto 'runa...' con Dialogo): conectala a mano en Al Terminar."); return; }
        Undo.RecordObject(runa, "Conectar runa");
        var so = new SerializedObject(runa);
        so.FindProperty("abrirAlEntrar").boolValue = true;
        so.FindProperty("abrirAlEntrarSoloLaPrimeraVez").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        // saca conexiones viejas a esta pantalla y agrega una sola
        var evento = (UnityEngine.Events.UnityEvent)typeof(Dialogo)
            .GetField("alTerminar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(runa);
        for (int i = evento.GetPersistentEventCount() - 1; i >= 0; i--)
            if (evento.GetPersistentTarget(i) == null || evento.GetPersistentTarget(i) is InstruccionAtaque)
                UnityEventTools.RemovePersistentListener(evento, i);
        UnityEventTools.AddPersistentListener(evento, ia.Mostrar);
        EditorUtility.SetDirty(runa);
        Debug.Log("[Instruccion ataque] Conectada a '" + runa.name + "': al terminar su dialogo aparece la pantalla.");
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
        img.preserveAspect = sprite != null;
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

    private static void ImportarSprite(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { AssetDatabase.ImportAsset(ruta); imp = AssetImporter.GetAtPath(ruta) as TextureImporter; }
        // siempre Single (la imagen entera): en modo Multiple Unity la corta en pedazos solo y se usaba un pedazo
        if (imp == null || (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single
                            && imp.alphaIsTransparency && !imp.mipmapEnabled)) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.SaveAndReimport();
    }

    private static Sprite CargarSprite(string ruta)
    {
        var s = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (s == null) Debug.LogWarning("[Instruccion ataque] No encontré el sprite " + ruta);
        return s;
    }
}
