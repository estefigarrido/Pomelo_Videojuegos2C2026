using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Una sola vez, en SampleScene: corre el portal de entrada (con la puerta) un poquito a la izquierda
// y centra a la chica en la puerta, asi sale del medio del portal.
// La escena hay que guardarla (Ctrl+S) si tenia otros cambios; al guardarse, este archivo se borra solo.
[InitializeOnLoad]
public static class AjustarPortal
{
    private const string Esta = "Assets/Editor/AjustarPortal.cs";
    private const string Hecho = "Pomelo.AjustarPortal.hecho";
    private const float Corrimiento = -0.4f;   // unidades (40 px del Figma) hacia la izquierda

    static AjustarPortal()
    {
        EditorSceneManager.sceneSaved += AlGuardar;
        EditorApplication.delayCall += Ajustar;
    }

    private static void AlGuardar(UnityEngine.SceneManagement.Scene escena)
    {
        if (escena.name != "SampleScene" || !SessionState.GetBool(Hecho, false)) return;
        EditorSceneManager.sceneSaved -= AlGuardar;
        EditorApplication.delayCall += () => AssetDatabase.DeleteAsset(Esta);
    }

    private static float CentroX(SpriteRenderer sr)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (var v in sr.sprite.vertices)
        {
            float x = sr.transform.TransformPoint(v).x;
            min = Mathf.Min(min, x); max = Mathf.Max(max, x);
        }
        return (min + max) / 2f;
    }

    private static void Ajustar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Hecho, false)) return;
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "SampleScene") return;
        var intro = Object.FindFirstObjectByType<InicioNivel>(FindObjectsInactive.Include);
        if (intro == null) return;
        var so = new SerializedObject(intro);
        var portal = so.FindProperty("portal").objectReferenceValue as SpriteRenderer;
        var puerta = so.FindProperty("puerta").objectReferenceValue as SpriteRenderer;
        if (portal == null || puerta == null || puerta.sprite == null) { Debug.LogError("[Portal] Falta el portal o la puerta en InicioNivel."); return; }
        bool teniaCambios = escena.isDirty;

        // 1) el portal (y la puerta, que estan en el mismo grupo) un poquito a la izquierda
        var grupo = portal.transform.parent != null ? portal.transform.parent : portal.transform;
        Undo.RecordObject(grupo, "Correr portal");
        grupo.position += Vector3.right * Corrimiento;
        if (!puerta.transform.IsChildOf(grupo))
        {
            Undo.RecordObject(puerta.transform, "Correr puerta");
            puerta.transform.position += Vector3.right * Corrimiento;
        }

        // 2) la chica, centrada en la puerta
        float centro = CentroX(puerta);
        var pc = so.FindProperty("posicionChica");
        float xViejo = pc.vector2Value.x;
        pc.vector2Value = new Vector2(centro, pc.vector2Value.y);
        so.ApplyModifiedProperties();
        var salud = Object.FindFirstObjectByType<SaludPersonaje>(FindObjectsInactive.Include);
        if (salud != null)
        {
            Undo.RecordObject(salud.transform, "Centrar chica");
            var p = salud.transform.position; p.x = centro; salud.transform.position = p;
        }

        SessionState.SetBool(Hecho, true);
        EditorSceneManager.MarkSceneDirty(escena);
        if (!teniaCambios) EditorSceneManager.SaveScene(escena);
        else Debug.LogWarning("[Portal] La escena tenia otros cambios sin guardar: guardala (Ctrl+S).");
        Debug.Log("[Portal] Portal corrido " + Corrimiento + " u; la chica sale de x = " + centro.ToString("0.00") + " (antes " + xViejo.ToString("0.00") + ").");
    }
}
