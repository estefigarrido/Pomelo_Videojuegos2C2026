using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Deja listo el DASH:
//  1. Reimporta los 3 PNG de chica_dash igual que los del sprint (mismo tamano y mismo punto de apoyo).
//  2. Crea la animacion ChicaDash: los 3 frames en 0,1 s y el tercero quieto hasta el final (0,2 s).
//  3. Agrega el parametro "dash" y el estado "Dash" al Animator, y rearma las transiciones.
// Menu:  Pomelo -> Configurar dash
public static class ConfigurarDash
{
    private const string Carpeta = "Assets/Assets/animaciones/";
    private const string ReferenciaSprint = Carpeta + "chica_sprinting_12fps/001.png";
    private const string RutaClip = Carpeta + "ChicaDash.anim";
    private const string RutaController = Carpeta + "Personaje.controller";

    private static readonly string[] frames =
    {
        Carpeta + "chica_dash/dash_001.png",
        Carpeta + "chica_dash/dash_002.png",
        Carpeta + "chica_dash/dash_003.png",
    };

    private const float DuracionArranque = 0.1f;   // los 3 frames
    private const float DuracionDash = 0.2f;       // el tercero queda hasta aca

    [MenuItem("Pomelo/Configurar dash")]
    public static void Configurar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de configurar el dash.");
            return;
        }

        if (!ReimportarFrames()) return;

        var clip = CrearClip();
        if (clip == null) return;

        if (!PrepararController(clip)) return;

        ArreglarAnimator.Arreglar();
        Debug.Log("Dash configurado: " + RutaClip + " y estado Dash en el Animator.");
    }

    private static bool ReimportarFrames()
    {
        var referencia = AssetImporter.GetAtPath(ReferenciaSprint) as TextureImporter;
        if (referencia == null)
        {
            Debug.LogError("No encontre el frame de referencia del sprint: " + ReferenciaSprint);
            return false;
        }

        var ajustes = new TextureImporterSettings();
        referencia.ReadTextureSettings(ajustes);

        foreach (var ruta in frames)
        {
            var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
            if (importador == null)
            {
                Debug.LogError("No encontre " + ruta);
                return false;
            }

            // mismo modo, PPU y pivote que el sprint: el lienzo es del mismo tamano
            importador.SetTextureSettings(ajustes);
            importador.textureCompression = referencia.textureCompression;
            importador.maxTextureSize = referencia.maxTextureSize;
            importador.SaveAndReimport();
        }
        return true;
    }

    private static AnimationClip CrearClip()
    {
        var sprites = frames.Select(r => AssetDatabase.LoadAssetAtPath<Sprite>(r)).ToArray();
        if (sprites.Any(s => s == null))
        {
            Debug.LogError("Algun frame del dash no quedo importado como sprite.");
            return null;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaClip);
        bool esNuevo = clip == null;
        if (esNuevo) clip = new AnimationClip();

        clip.frameRate = 30f;
        float paso = DuracionArranque / sprites.Length;
        var claves = new[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprites[0] },
            new ObjectReferenceKeyframe { time = paso, value = sprites[1] },
            new ObjectReferenceKeyframe { time = paso * 2f, value = sprites[2] },
            // el tercero se queda hasta que termina el dash
            new ObjectReferenceKeyframe { time = DuracionDash, value = sprites[2] },
        };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, claves);

        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        if (esNuevo) AssetDatabase.CreateAsset(clip, RutaClip);
        else EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    private static bool PrepararController(AnimationClip clip)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
        if (controller == null)
        {
            Debug.LogError("No encontre el controller en " + RutaController);
            return false;
        }

        if (!controller.parameters.Any(p => p.name == "dash"))
            controller.AddParameter("dash", AnimatorControllerParameterType.Bool);

        var maquina = controller.layers[0].stateMachine;
        var estado = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == "Dash");
        if (estado == null) estado = maquina.AddState("Dash", new Vector3(520f, 220f, 0f));
        estado.motion = clip;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return true;
    }
}
