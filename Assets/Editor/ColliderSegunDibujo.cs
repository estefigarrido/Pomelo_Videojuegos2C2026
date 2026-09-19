using System.Linq;
using UnityEditor;
using UnityEngine;

// Deja el Capsule Collider 2D QUIETO y le saca a las animaciones cualquier curva que lo mueva.
//
// Un collider que sube y baja mientras la chica esta parada en el piso la empuja: cuando
// el collider baja queda metido dentro del piso, Unity lo expulsa y la chica sale disparada
// como si saltara. Por eso el collider no se anima. Para que el dibujo no se despegue del
// collider se corrige el punto de apoyo de los PNG (ver AlinearSalto).
//
// Menu:  Pomelo -> Dejar el collider quieto
public static class ColliderSegunDibujo
{
    private const string Carpeta = "Assets/Assets/animaciones/";

    private static readonly string[] rutasClips =
    {
        Carpeta + "ChicaIdle.anim",
        Carpeta + "ChicaSprint.anim",
        Carpeta + "ChicaSaltoPorAltura.anim",
        Carpeta + "ChicaDash.anim",
        Carpeta + "chica_salto.anim",
        Carpeta + "chicagolpe1.anim",
    };

    private static readonly string[] propiedades = { "m_Offset.x", "m_Offset.y", "m_Size.x", "m_Size.y" };

    [MenuItem("Pomelo/Dejar el collider quieto")]
    public static void Ajustar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de tocar el collider.");
            return;
        }

        int sacadas = 0;
        foreach (var ruta in rutasClips)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta);
            if (clip == null) continue;

            foreach (var propiedad in propiedades)
            {
                var binding = EditorCurveBinding.FloatCurve("", typeof(CapsuleCollider2D), propiedad);
                if (AnimationUtility.GetEditorCurve(clip, binding) == null) continue;
                AnimationUtility.SetEditorCurve(clip, binding, null);
                sacadas++;
            }
            EditorUtility.SetDirty(clip);
        }

        // el collider vuelve a su forma de siempre
        var chica = Object.FindFirstObjectByType<MovimientoPersonaje>();
        var capsula = chica != null ? chica.GetComponent<CapsuleCollider2D>() : null;
        if (capsula != null)
        {
            Undo.RecordObject(capsula, "Collider quieto");
            capsula.offset = Vector2.zero;
            capsula.size = new Vector2(0.79f, 2.4f);
            EditorUtility.SetDirty(capsula);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Collider quieto: saque " + sacadas + " curvas de las animaciones" +
                  (capsula != null ? " y lo deje en 0.79 x 2.4 centrado." : " (no encontre la capsula en la escena)."));
    }
}
