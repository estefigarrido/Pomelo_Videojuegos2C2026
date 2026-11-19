using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Arma los botones de la pantalla de inicio debajo del titulo, en lugar de la fila "Comenzar [espacio] o [X]".
// Diseno de Caro: Figma, pagina "intro juego". Por ahora solo "Comenzar juego".
// Menu: Pomelo -> Inicio -> Poner botones de inicio  (con la escena Inicio_introducción abierta; despues guardar).
//
// Para AGREGAR un boton:
//  1. poner en Assets/_UI/Inicio/Botones/ sus dos imagenes: Boton_<nombre>_texto.png y Boton_<nombre>_brillo.png
//     (mismo formato que las de comenzar: el texto blanco y su brillo, 1032 x 137 px, con 30 de margen del Figma);
//  2. sumarlo a la Lista de abajo (accion Evento = lo que hace se conecta en el Inspector, en alTocar);
//  3. volver a correr el menu. Cada boton va debajo del anterior (cada 267 px del Figma).
public static class BotonesInicioEditor
{
    private const string Carpeta = "Assets/_UI/Inicio/Botones/";

    // En Figma cada boton mide 2003 x 214. La cueva del Figma (5031 px de ancho) ocupa los 1234 px del lienzo
    // -> escala 0.245.
    private const float Escala = 0.245f;
    private static readonly Vector2 Tamano = new Vector2(2003f, 214f) * Escala;
    private const float MargenTexto = 30f * Escala;      // las imagenes del texto tienen 30 de margen (lugar del brillo)
    private const float Separacion = 267f * Escala;      // de un boton al siguiente (Figma: 214 + 53 de espacio)
    private static readonly Vector2 PrimerBoton = new Vector2(-13f, -150f);   // centro del primero (debajo del titulo)

    private static readonly (BotonesInicio.Accion accion, string nombre, string archivo)[] Lista =
    {
        (BotonesInicio.Accion.Comenzar, "Comenzar juego", "comenzar"),
        // (BotonesInicio.Accion.Evento, "Controles", "controles"),
    };

    [MenuItem("Pomelo/Inicio/Poner botones de inicio")]
    private static void PonerEnEscena()
    {
        var pantalla = Object.FindFirstObjectByType<PantallaInicio>(FindObjectsInactive.Include);
        var textos = pantalla != null ? pantalla.transform.Find("Textos") as RectTransform : null;
        if (textos == null)
        {
            EditorUtility.DisplayDialog("Botones de inicio",
                "No encontre 'Pantalla de inicio/Textos' en la escena abierta (abri Inicio_introducción).", "Ok");
            return;
        }
        var anterior = textos.Find("Botones");
        if (anterior != null) Undo.DestroyObjectImmediate(anterior.gameObject);
        var viejo = textos.Find("Comenzar");
        if (viejo != null)
        {
            Undo.RecordObject(viejo.gameObject, "Botones de inicio");
            viejo.gameObject.SetActive(false);   // queda apagado por si se quiere volver
        }
        var go = Crear(textos, pantalla);
        Undo.RegisterCreatedObjectUndo(go, "Botones de inicio");
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
    }

    public static GameObject Crear(RectTransform padre, PantallaInicio pantalla)
    {
        var go = new GameObject("Botones", typeof(RectTransform), typeof(BotonesInicio));
        var raiz = (RectTransform)go.transform;
        raiz.SetParent(padre, false);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
        raiz.anchoredPosition = PrimerBoton;
        raiz.sizeDelta = Vector2.zero;

        var fondo = CargarSprite("Boton_fondo.png");
        var so = new SerializedObject(go.GetComponent<BotonesInicio>());
        so.FindProperty("pantalla").objectReferenceValue = pantalla;
        var lista = so.FindProperty("botones");
        lista.arraySize = Lista.Length;
        for (int i = 0; i < Lista.Length; i++)
        {
            var (accion, nombre, archivo) = Lista[i];
            var zona = new GameObject(nombre, typeof(RectTransform)).GetComponent<RectTransform>();
            zona.SetParent(raiz, false);
            zona.anchorMin = zona.anchorMax = zona.pivot = new Vector2(0.5f, 0.5f);
            zona.anchoredPosition = new Vector2(0f, -Separacion * i);
            zona.sizeDelta = Tamano;

            var imgFondo = Capa("Fondo", zona, fondo, 0f, new Color(0f, 0f, 0f, 0.2f));
            var imgBrillo = Capa("Brillo", zona, CargarSprite("Boton_" + archivo + "_brillo.png"), MargenTexto,
                                 new Color(1f, 1f, 1f, 0f));
            Capa("Texto", zona, CargarSprite("Boton_" + archivo + "_texto.png"), MargenTexto, Color.white);

            var e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("accion").enumValueIndex = (int)accion;
            e.FindPropertyRelative("zona").objectReferenceValue = zona;
            e.FindPropertyRelative("fondo").objectReferenceValue = imgFondo;
            e.FindPropertyRelative("brillo").objectReferenceValue = imgBrillo;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    // imagen que llena la zona (con 'margen' de mas para cada lado)
    private static Image Capa(string nombre, RectTransform padre, Sprite sprite, float margen, Color color)
    {
        var rt = new GameObject(nombre, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-margen, -margen);
        rt.offsetMax = new Vector2(margen, margen);
        var img = rt.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Sprite CargarSprite(string archivo)
    {
        string ruta = Carpeta + archivo;
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { AssetDatabase.ImportAsset(ruta); imp = AssetImporter.GetAtPath(ruta) as TextureImporter; }
        if (imp != null && !(imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single
                             && imp.alphaIsTransparency && !imp.mipmapEnabled))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (s == null) Debug.LogWarning("No encontre el sprite " + ruta);
        return s;
    }
}
