using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Trae el PNG del fondo al proyecto, lo importa al mismo ancho que el mapa y lo pone
// detras de todo, centrado con el mapa.
// Menu:  Pomelo -> Poner el fondo
public static class PonerFondo
{
    private const string Origen = @"D:\AAVIDEOJUEGOS\fondoultimo.png";
    private const string Destino = "Assets/Assets/fondoultimo.png";
    private const string NombreObjeto = "Fondo";
    private const int OrdenDelFondo = -20;   // el mapa esta en 0, las piedras en -1 y la puerta del portal en -12
    private const int TamanoMaximo = 8192;

    // El fondo y el mapa salen del mismo diseno, asi que van a la misma escala: PPU 100.
    // Estos dos numeros salen de medir la imagen de referencia (el puntito claro y los
    // adornos del fondo caen en el mismo lugar): la esquina de arriba a la izquierda del
    // fondo va corrida respecto de la del mapa.
    private const float CorrimientoXenPixeles = 24.4f;
    private const float CorrimientoYenPixeles = 414.5f;   // hacia abajo

    [MenuItem("Pomelo/Poner el fondo")]
    public static void Poner()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de poner el fondo.");
            return;
        }

        // 1. copiarlo al proyecto
        if (!File.Exists(Origen))
        {
            Debug.LogError("No encontre el archivo en " + Origen);
            return;
        }
        string destinoAbsoluto = Path.Combine(Application.dataPath, "Assets/fondoultimo.png");
        File.Copy(Origen, destinoAbsoluto, true);
        AssetDatabase.ImportAsset(Destino, ImportAssetOptions.ForceUpdate);

        // 2. el mapa manda: el fondo tiene que medir lo mismo de ancho
        // (el dibujo del mapa es "Piso", adentro del grupo "Ground")
        var suelo = GameObject.Find("Piso");
        if (suelo == null) suelo = GameObject.Find("Ground");
        var srSuelo = suelo != null ? suelo.GetComponent<SpriteRenderer>() : null;
        if (srSuelo == null || srSuelo.sprite == null)
        {
            Debug.LogError("No encontre el objeto Piso (dentro de Ground) con su sprite para tomarle la medida.");
            return;
        }
        float anchoMapa = srSuelo.sprite.bounds.size.x;

        var importador = AssetImporter.GetAtPath(Destino) as TextureImporter;
        if (importador == null)
        {
            Debug.LogError("No pude importar " + Destino);
            return;
        }

        // OJO: hay que usar el tamano del PNG original, no el de la textura que Unity
        // importa (la achica al maximo permitido). El tamano en el juego se calcula
        // siempre con el original dividido el PPU.
        int anchoOriginal, altoOriginal;
        importador.GetSourceTextureWidthAndHeight(out anchoOriginal, out altoOriginal);
        // El fondo va al mismo PPU configurado que el mapa, porque salen del mismo diseno.
        // OJO: sprite.pixelsPerUnit NO sirve, porque Unity lo ajusta cuando achica la
        // textura por el tamano maximo. Hay que leer el valor del importador.
        var impMapa = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(srSuelo.sprite)) as TextureImporter;
        if (impMapa == null)
        {
            Debug.LogError("No pude leer la configuracion del mapa.");
            return;
        }
        float ppu = impMapa.spritePixelsPerUnit;

        var ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        ajustes.textureType = TextureImporterType.Sprite;
        ajustes.spriteMode = (int)SpriteImportMode.Single;
        ajustes.spriteAlignment = (int)SpriteAlignment.Center;
        ajustes.spritePixelsPerUnit = ppu;
        ajustes.alphaIsTransparency = true;
        ajustes.mipmapEnabled = false;
        importador.SetTextureSettings(ajustes);
        importador.maxTextureSize = TamanoMaximo;
        importador.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Destino);
        if (sprite == null)
        {
            Debug.LogError("El fondo no quedo importado como sprite.");
            return;
        }

        // 3. ponerlo en la escena, centrado con el mapa
        // si ya habia un fondo (suelto o dentro de un grupo como "Fondo y ambiente"), se reemplaza en el mismo lugar
        var escena = EditorSceneManager.GetActiveScene();
        Transform grupo = null;
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
                if (t != null && t.name == NombreObjeto && t.GetComponent<SpriteRenderer>() != null)
                {
                    grupo = t.parent;
                    Undo.DestroyObjectImmediate(t.gameObject);
                }

        var fondo = new GameObject(NombreObjeto);
        Undo.RegisterCreatedObjectUndo(fondo, "Poner el fondo");
        if (grupo != null) fondo.transform.SetParent(grupo, false);

        // la esquina de arriba a la izquierda del mapa, corrida lo que midio la referencia
        Vector2 esquinaMapa = new Vector2(srSuelo.bounds.min.x, srSuelo.bounds.max.y);
        Vector2 esquinaFondo = esquinaMapa + new Vector2(CorrimientoXenPixeles / ppu, -CorrimientoYenPixeles / ppu);
        fondo.transform.position = new Vector3(
            esquinaFondo.x + sprite.bounds.size.x * 0.5f,
            esquinaFondo.y - sprite.bounds.size.y * 0.5f,
            0f);

        var sr = fondo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = OrdenDelFondo;
        sr.sortingLayerID = srSuelo.sortingLayerID;
        sr.sharedMaterial = srSuelo.sharedMaterial;

        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = fondo;

        Debug.Log(string.Format(
            "Fondo puesto.\n  PNG original: {0} x {1} px\n  PPU: {2:0.00} (para medir {3:0.00} de ancho, igual que el mapa)\n  mide {4:0.00} x {5:0.00} unidades\n  centrado en {6}\n  orden {7}: detras del mapa y de las piedras\nGuarda la escena con Ctrl+S.",
            anchoOriginal, altoOriginal, ppu, anchoMapa,
            sprite.bounds.size.x, sprite.bounds.size.y, fondo.transform.position, OrdenDelFondo));
    }
}
