using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// El titulo TECHNI como imagen (el SVG de Caro pasado a PNG) en lugar del texto de TextMeshPro.
// Tres capas del mismo tamano, una arriba de la otra:
//   Oscuro  - la sombra oscura y difuminada (como el underlay del material viejo)
//   Brillo  - el brillo blanco alrededor de las letras (como el glow del material viejo)
//   Letras  - las letras blancas
// El brillo y la sombra se ajustan a mano con el Color (sobre todo la A) de cada Image.
// Menu: Pomelo -> Inicio -> Poner titulo imagen  (en la escena Inicio_introduccion abierta; despues guardar).
public static class TituloImagen
{
    private const string Carpeta = "Assets/_UI/Inicio/";
    public const string Letras = Carpeta + "Titulo_TECHNI.png";
    public const string Brillo = Carpeta + "Titulo_TECHNI_brillo.png";
    public const string Oscuro = Carpeta + "Titulo_TECHNI_oscuro.png";

    // los PNG cubren el SVG (688 x 316) mas 90 de margen por lado (lugar para el brillo): 868 x 496
    private const float AnchoLetras = 500f;                    // ancho de las letras en px del diseno (1234 x 738)
    private const float Escala = AnchoLetras / 688.21f;
    private static readonly Vector2 Tamano = new Vector2(868.21f, 495.98f) * Escala;
    // las mayusculas quedan centradas en la pantalla (como el texto de antes); el acento de la E no cuenta.
    // En el SVG el centro de las mayusculas esta 29.5 por debajo del centro de la imagen.
    private static readonly Vector2 Lugar = new Vector2(-13f, 29.5f * Escala);

    public static readonly Color ColorBrillo = new Color(1f, 1f, 1f, 0.5f);
    public static readonly Color ColorOscuro = new Color(1f, 1f, 1f, 0.27f);

    [MenuItem("Pomelo/Inicio/Poner titulo imagen")]
    private static void PonerEnEscena()
    {
        TMP_Text viejo = null;
        foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.text == "TÉCHNI" || t.name == "TÉCHNI") { viejo = t; break; }
        Transform padre = viejo != null ? viejo.transform.parent : null;
        if (padre == null)
        {
            var textos = GameObject.Find("Pantalla de inicio/Textos");
            if (textos != null) padre = textos.transform;
        }
        if (padre == null)
        {
            EditorUtility.DisplayDialog("Titulo", "No encontre el titulo TÉCHNI ni 'Pantalla de inicio/Textos' en la escena abierta.", "Ok");
            return;
        }

        var anterior = padre.Find("Titulo");
        if (anterior != null) Undo.DestroyObjectImmediate(anterior.gameObject);

        var titulo = Crear((RectTransform)padre);
        Undo.RegisterCreatedObjectUndo(titulo, "Titulo imagen");
        if (viejo != null)
        {
            titulo.transform.SetSiblingIndex(viejo.transform.GetSiblingIndex());
            Undo.RecordObject(viejo.gameObject, "Titulo imagen");
            viejo.gameObject.SetActive(false);   // queda apagado por si se quiere volver al texto
        }
        Selection.activeGameObject = titulo;
        EditorSceneManager.MarkSceneDirty(titulo.scene);
    }

    // arma el titulo adentro de 'padre' (centrado) y lo devuelve
    public static GameObject Crear(RectTransform padre)
    {
        Importar(Letras); Importar(Brillo); Importar(Oscuro);
        var go = new GameObject("Titulo", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Lugar;
        rt.sizeDelta = Tamano;
        Capa("Oscuro", rt, Oscuro, ColorOscuro);
        Capa("Brillo", rt, Brillo, ColorBrillo);
        Capa("Letras", rt, Letras, Color.white);
        return go;
    }

    private static void Capa(string nombre, RectTransform padre, string ruta, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        img.color = color;
        img.raycastTarget = false;
    }

    private static void Importar(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { AssetDatabase.ImportAsset(ruta); imp = AssetImporter.GetAtPath(ruta) as TextureImporter; }
        if (imp == null || (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single && imp.alphaIsTransparency)) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }
}
