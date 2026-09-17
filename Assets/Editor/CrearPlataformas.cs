using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Pone las 14 piedras quebradizas en la escena, cada una con su PNG, su tamano y su lugar.
// Menu:  Pomelo -> Crear plataformas
// Si ya existen, las borra y las vuelve a crear (se puede deshacer con Ctrl+Z).
public static class CrearPlataformas
{
    private const string Carpeta = "Assets/Assets/Plataformas/";
    private const string NombreRaiz = "Plataformas";

    // Pixels Per Unit de cada archivo, para que midan lo mismo que en el mapa de referencia.
    // Los archivos compartidos (1-2, 3-4-5-6, 8-10, 13-14) tienen un solo tamano.
    private static readonly Dictionary<string, float> ppuPorArchivo = new Dictionary<string, float>
    {
        { "Plataforma 1-2", 1574.8f },
        { "Plataforma 3-4-5-6", 1142.4f },
        { "Plataforma 7", 1392.2f },
        { "Plataforma 8-10", 1503.5f },
        { "Plataforma 9", 1510.7f },
        { "Plataforma 11", 1456.6f },
        { "Plataforma 12", 1434.8f },
        { "Plataforma 13-14", 1450.6f },
    };

    private class Datos
    {
        public int numero;
        public string archivo;
        public float x;   // centro de la piedra, en unidades de Unity
        public float y;
        public Datos(int numero, string archivo, float x, float y)
        {
            this.numero = numero; this.archivo = archivo; this.x = x; this.y = y;
        }
    }

    // Posiciones sacadas de la imagen de referencia sobre el mapa real.
    // 7, 8, 9 y 10 estan repartidas parejo entre el escalon (y -9.96) y el pasillo (y 4.30):
    // 285 px por salto, para que alcance el supersalto de 350.
    // La 14 esta a mitad de camino entre la 13 y el bloque de arriba (y 14.55).
    // La 9 esta corrida un poco a la derecha para no meterse en la pared.
    private static readonly Datos[] plataformas =
    {
        new Datos(1,  "Plataforma 1-2",      50.95f, -21.36f),
        new Datos(2,  "Plataforma 1-2",      42.51f, -20.26f),
        new Datos(3,  "Plataforma 3-4-5-6",   7.74f, -11.50f),
        new Datos(4,  "Plataforma 3-4-5-6",   0.70f,  -8.99f),
        new Datos(5,  "Plataforma 3-4-5-6",  -6.63f, -11.40f),
        new Datos(6,  "Plataforma 3-4-5-6", -15.17f,  -8.89f),
        new Datos(7,  "Plataforma 7",       -32.36f,  -7.742f),
        new Datos(8,  "Plataforma 8-10",    -27.43f,  -4.978f),
        new Datos(9,  "Plataforma 9",       -35.81f,  -2.126f),
        new Datos(10, "Plataforma 8-10",    -27.43f,   0.726f),
        new Datos(11, "Plataforma 11",       38.19f,   3.88f),
        new Datos(12, "Plataforma 12",       74.16f,   5.68f),
        new Datos(13, "Plataforma 13-14",    81.80f,   8.89f),
        new Datos(14, "Plataforma 13-14",    73.86f,  11.392f),
    };

    [MenuItem("Pomelo/Crear plataformas")]
    public static void Crear()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Pará el Play antes de crear las plataformas.");
            return;
        }

        AjustarTamanos();

        var escena = EditorSceneManager.GetActiveScene();

        // si ya estaban, las saco para no duplicarlas
        foreach (var viejo in escena.GetRootGameObjects().Where(g => g.name == NombreRaiz).ToArray())
            Undo.DestroyObjectImmediate(viejo);

        var raiz = new GameObject(NombreRaiz);
        Undo.RegisterCreatedObjectUndo(raiz, "Crear plataformas");

        // uso el mismo material que la chica, asi reciben la misma luz
        Material material = null;
        var chica = Object.FindFirstObjectByType<MovimientoPersonaje>();
        if (chica != null)
        {
            var srChica = chica.GetComponent<SpriteRenderer>();
            if (srChica != null) material = srChica.sharedMaterial;
        }

        var informe = new System.Text.StringBuilder("Plataformas creadas:\n");

        foreach (var d in plataformas)
        {
            Sprite sprite = CargarSprite(d.archivo);
            if (sprite == null)
            {
                Debug.LogError("No encontre el sprite de " + d.archivo + " para la plataforma " + d.numero);
                continue;
            }

            var piedra = new GameObject("Plataforma " + d.numero);
            piedra.transform.SetParent(raiz.transform, false);
            piedra.transform.position = new Vector3(d.x, d.y, 0f);

            var rb = piedra.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;

            // el dibujo va en un hijo: es lo unico que tiembla
            var dibujo = new GameObject("Dibujo");
            dibujo.transform.SetParent(piedra.transform, false);
            var sr = dibujo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -1;
            if (material != null) sr.sharedMaterial = material;

            // collider con el contorno real de la piedra: son solidas por todos lados
            AgregarCollider(piedra, sprite);

            var comportamiento = piedra.AddComponent<PlataformaQuebradiza>();
            var so = new SerializedObject(comportamiento);
            so.FindProperty("dibujo").objectReferenceValue = dibujo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            informe.AppendLine(string.Format("  {0,2}  {1,-20} x {2,7:0.00}  y {3,7:0.00}  ancho {4:0.00}",
                d.numero, d.archivo, d.x, d.y, sprite.bounds.size.x));
        }

        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeGameObject = raiz;
        EditorGUIUtility.PingObject(raiz);
        Debug.Log(informe + "\nGuarda la escena con Ctrl+S.");
    }

    private static void AjustarTamanos()
    {
        foreach (var par in ppuPorArchivo)
        {
            var ruta = Carpeta + par.Key + ".png";
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                Debug.LogError("No encontre " + ruta);
                continue;
            }
            if (Mathf.Abs(importador.spritePixelsPerUnit - par.Value) > 0.05f)
            {
                importador.spritePixelsPerUnit = par.Value;
                importador.SaveAndReimport();
            }
        }
    }

    private static Sprite CargarSprite(string archivo)
    {
        return AssetDatabase.LoadAllAssetsAtPath(Carpeta + archivo + ".png")
            .OfType<Sprite>()
            .FirstOrDefault();
    }

    private static void AgregarCollider(GameObject piedra, Sprite sprite)
    {
        int formas = sprite.GetPhysicsShapeCount();
        if (formas == 0)
        {
            // por si el PNG no tiene contorno generado: una caja del tamano del dibujo
            var caja = piedra.AddComponent<BoxCollider2D>();
            caja.size = sprite.bounds.size;
            caja.offset = sprite.bounds.center;
            return;
        }

        var poligono = piedra.AddComponent<PolygonCollider2D>();
        poligono.pathCount = formas;
        var puntos = new List<Vector2>();
        for (int i = 0; i < formas; i++)
        {
            puntos.Clear();
            sprite.GetPhysicsShape(i, puntos);
            poligono.SetPath(i, puntos);
        }
    }
}
