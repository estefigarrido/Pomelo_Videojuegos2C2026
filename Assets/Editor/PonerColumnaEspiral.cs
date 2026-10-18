using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez, en SampleScene:
//  - pone (o corrige) la columna espiral chica donde esta en el Figma: "a_Columna espiral chica 24",
//    146.36 x 881 px en (12597, 9757) del frame del mapa. Va por delante de todo el mapa (tapa la terminacion
//    del piso), en el verde oscuro de las piezas del mapa y con un collider para que la chica no pase al abismo.
//  - corre el portal de entrada (con la puerta y la chica) para que quede justo a la derecha del cuadrado
//    que sobresale del piso de arriba ("Parte_abajo_piso_2 1": termina en x = 13243 px del Figma).
// La escena hay que guardarla (Ctrl+S) si tenia otros cambios; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class PonerColumnaEspiral
{
    private const string Esta = "Assets/Editor/PonerColumnaEspiral.cs";
    private const string Imagen = "Assets/_entorno/a_Columna espiral chica.png";
    private const string Nombre = "Columna espiral (portal de entrada)";

    // Figma (px del frame del mapa) -> Unity
    private static float X(float fx) => (fx - 12883f) / 100f - 31.98f;
    private static float Y(float fy) => -(fy - 10621f) / 100f - 30.27f;

    // la columna es gris claro; multiplicada por este color queda entre #121F16 y #213A2A,
    // los mismos verdes oscuros del mapa (#081810 con vetas #203828)
    private static readonly Color VerdeMapa = new Color(0.167f, 0.293f, 0.209f, 1f);

    static PonerColumnaEspiral()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Poner;
    }

    private static bool Listo()
    {
        var go = GameObject.Find(Nombre);
        return go != null && go.GetComponent<BoxCollider2D>() != null && go.GetComponent<SpriteRenderer>().color == VerdeMapa;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !Listo()) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static void Poner()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene" || Listo()) return;
        bool teniaCambios = escena.isDirty;

        // estaba importada a 2048 como maximo y se veia achicada
        var ti = AssetImporter.GetAtPath(Imagen) as TextureImporter;
        if (ti != null && ti.maxTextureSize < 4096)
        {
            ti.maxTextureSize = 4096;
            ti.SaveAndReimport();
        }
        Sprite sprite = null;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Imagen)) if (o is Sprite s) { sprite = s; break; }
        if (sprite == null) { Debug.LogError("[Columna] No encontre el sprite de " + Imagen); return; }

        // 1) la columna
        const float fx = 12597f, fy = 9757f, ancho = 146.36f, alto = 881f;
        var go = GameObject.Find(Nombre);
        if (go == null)
        {
            go = new GameObject(Nombre);
            Undo.RegisterCreatedObjectUndo(go, "Poner columna espiral");
            go.AddComponent<SpriteRenderer>();
        }
        else Undo.RegisterFullObjectHierarchyUndo(go, "Corregir columna espiral");
        float escala = ancho / 100f / (sprite.rect.width / sprite.pixelsPerUnit);
        go.transform.position = new Vector3(X(fx + ancho / 2f), Y(fy + alto / 2f), 0f);
        go.transform.localScale = new Vector3(escala, escala, 1f);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = VerdeMapa;
        sr.sortingOrder = 2;   // el mapa va de -2 a 1: queda por delante de todo

        // mismo layer que los pisos, asi choca igual que ellos
        foreach (var piso in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (piso == sr || piso.GetComponent<Collider2D>() == null) continue;
            var c = piso.color;
            if (Mathf.Abs(c.r - 0.353f) < 0.01f && Mathf.Abs(c.b - 0.706f) < 0.01f) { go.layer = piso.gameObject.layer; break; }
        }
        if (go.GetComponent<BoxCollider2D>() == null) go.AddComponent<BoxCollider2D>();   // toma el tamano del dibujo
        Debug.Log("[Columna] En " + go.transform.position + ", escala " + escala + ", layer " + LayerMask.LayerToName(go.layer));

        // 2) el portal, justo a la derecha del cuadrado que sobresale del piso de arriba
        var intro = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (intro != null)
        {
            var so = new SerializedObject(intro);
            var portal = so.FindProperty("portal").objectReferenceValue as SpriteRenderer;
            if (portal != null && portal.sprite != null)
            {
                // borde izquierdo de lo que se ve del portal (sin el margen transparente del PNG)
                float izquierda = float.MaxValue;
                foreach (var v in portal.sprite.vertices) izquierda = Mathf.Min(izquierda, portal.transform.TransformPoint(v).x);
                float corrimiento = X(13243f) - izquierda;
                if (Mathf.Abs(corrimiento) > 0.005f)
                {
                    var grupo = portal.transform.parent != null ? portal.transform.parent : portal.transform;
                    Undo.RecordObject(grupo, "Correr portal");
                    grupo.position += Vector3.right * corrimiento;

                    var pc = so.FindProperty("posicionChica");
                    float xViejo = pc.vector2Value.x;
                    pc.vector2Value = new Vector2(xViejo + corrimiento, pc.vector2Value.y);
                    so.ApplyModifiedProperties();

                    // la chica arranca en el portal (y reaparece donde empezo): se corre con el
                    var salud = Object.FindFirstObjectByType<SaludPersonaje>(FindObjectsInactive.Include);
                    if (salud != null && Mathf.Abs(salud.transform.position.x - xViejo) < 3f)
                    {
                        Undo.RecordObject(salud.transform, "Correr chica");
                        salud.transform.position += Vector3.right * corrimiento;
                    }
                    Debug.Log("[Columna] Portal corrido " + corrimiento.ToString("0.00") + " u: empieza en x = " + X(13243f).ToString("0.00"));
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Columna] La escena tenia otros cambios sin guardar: guardala (Ctrl+S) para que quede la columna y el portal.");
    }
}
