using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Rearma las transiciones de CORRER, SALTO y DASH.
// El estado "Golpe 1" y su animacion NO se tocan.
// Menu:  Pomelo -> Arreglar Animator del personaje
public static class ArreglarAnimator
{
    private const string RutaController = "Assets/Assets/animaciones/Personaje.controller";
    private const string RutaSaltoOriginal = "Assets/Assets/animaciones/chica_salto.anim";
    // copia de chica_salto con los mismos PNG pero repartidos parejo, para que el script
    // pueda elegir cada frame por numero. La original no se toca.
    private const string RutaSaltoPorAltura = "Assets/Assets/animaciones/ChicaSaltoPorAltura.anim";
    private const int FramesEsperados = 27;

    [MenuItem("Pomelo/Arreglar Animator del personaje")]
    public static void Arreglar()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
        if (controller == null)
        {
            Debug.LogError("No encontre el controller en " + RutaController);
            return;
        }

        // el salto ya no usa el trigger: el script dice si esta saltando y que frame mostrar
        AsegurarParametro(controller, "saltando", AnimatorControllerParameterType.Bool);
        AsegurarParametro(controller, "tiempoSalto", AnimatorControllerParameterType.Float);

        var maquina = controller.layers[0].stateMachine;

        AnimatorState idle = Buscar(maquina, "Idle");
        AnimatorState sprint = Buscar(maquina, "Sprint");
        AnimatorState salto = Buscar(maquina, "Salto");
        AnimatorState dash = Buscar(maquina, "Dash");
        bool hayDash = dash != null && TieneParametro(controller, "dash");

        if (idle == null || sprint == null || salto == null)
        {
            Debug.LogError("Faltan estados: necesito Idle, Sprint y Salto.");
            return;
        }

        // la animacion del salto no corre sola: su tiempo lo marca el parametro
        var saltoParejo = CrearSaltoParejo();
        if (saltoParejo != null) salto.motion = saltoParejo;
        salto.timeParameterActive = true;
        salto.timeParameter = "tiempoSalto";
        salto.speed = 1f;

        // Saco las transiciones viejas de estos estados. Esto tambien elimina la
        // Idle -> Golpe 1 sin condicion que la mandaba sola al golpe.
        LimpiarTransiciones(idle);
        LimpiarTransiciones(sprint);
        LimpiarTransiciones(salto);
        if (dash != null) LimpiarTransiciones(dash);

        foreach (var t in maquina.anyStateTransitions) Object.DestroyImmediate(t, true);
        maquina.anyStateTransitions = new AnimatorStateTransition[0];

        maquina.defaultState = idle;

        // ---- DASH primero: desde cualquier estado, le gana al salto ----
        if (hayDash)
        {
            var aDash = maquina.AddAnyStateTransition(dash);
            aDash.hasExitTime = false;
            aDash.duration = 0f;
            aDash.canTransitionToSelf = false;
            aDash.AddCondition(AnimatorConditionMode.If, 0f, "dash");

            var dashAIdle = dash.AddTransition(idle);
            dashAIdle.hasExitTime = false;
            dashAIdle.duration = 0f;
            dashAIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "dash");
        }

        // ---- CORRER: Idle <-> Sprint, al toque ----
        var aSprint = idle.AddTransition(sprint);
        aSprint.hasExitTime = false;
        aSprint.duration = 0f;
        aSprint.AddCondition(AnimatorConditionMode.If, 0f, "sprint");

        var aIdle = sprint.AddTransition(idle);
        aIdle.hasExitTime = false;
        aIdle.duration = 0f;
        aIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "sprint");

        // ---- SALTO: mientras "saltando" este prendido, menos en medio de un dash.
        // Si un dash interrumpe el salto en el aire, al terminar vuelve al frame que corresponde.
        var aSalto = maquina.AddAnyStateTransition(salto);
        aSalto.hasExitTime = false;
        aSalto.duration = 0f;
        aSalto.canTransitionToSelf = false;
        aSalto.AddCondition(AnimatorConditionMode.If, 0f, "saltando");
        if (hayDash) aSalto.AddCondition(AnimatorConditionMode.IfNot, 0f, "dash");

        var saltoAIdle = salto.AddTransition(idle);
        saltoAIdle.hasExitTime = false;
        saltoAIdle.duration = 0f;
        saltoAIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "saltando");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animator arreglado:\n" +
                  (hayDash ? "  Any State -> Dash  por el bool 'dash', vuelve al terminar\n" : "  (todavia no hay estado Dash)\n") +
                  "  Idle <-> Sprint  por el bool 'sprint'\n" +
                  "  Any State <-> Salto  por el bool 'saltando'; el frame lo marca 'tiempoSalto'\n" +
                  "  Golpe 1 sin tocar (sin transiciones que lleven a el)");
    }

    private static AnimationClip CrearSaltoParejo()
    {
        var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaSaltoOriginal);
        if (original == null)
        {
            Debug.LogError("No encontre " + RutaSaltoOriginal);
            return null;
        }

        var binding = AnimationUtility.GetObjectReferenceCurveBindings(original)
            .FirstOrDefault(b => b.propertyName == "m_Sprite");
        if (binding.propertyName != "m_Sprite")
        {
            Debug.LogError(RutaSaltoOriginal + " no tiene frames de sprite.");
            return null;
        }

        var sprites = AnimationUtility.GetObjectReferenceCurve(original, binding)
            .OrderBy(k => k.time)
            .Select(k => k.value)
            .ToArray();
        if (sprites.Length != FramesEsperados)
            Debug.LogWarning("chica_salto tiene " + sprites.Length + " frames y MovimientoPersonaje espera " +
                             FramesEsperados + ". Hay que actualizar los numeros de frame del script.");

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaSaltoPorAltura);
        bool esNuevo = clip == null;
        if (esNuevo) clip = new AnimationClip();

        // un frame por "segundo de animacion": el frame N queda en N-1 / total
        clip.frameRate = sprites.Length;
        var claves = sprites
            .Select((s, i) => new ObjectReferenceKeyframe { time = i / (float)sprites.Length, value = s })
            .ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, binding, claves);

        var ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        if (esNuevo) AssetDatabase.CreateAsset(clip, RutaSaltoPorAltura);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorState Buscar(AnimatorStateMachine maquina, string nombre)
    {
        foreach (var hijo in maquina.states)
            if (hijo.state.name == nombre) return hijo.state;
        return null;
    }

    private static bool TieneParametro(AnimatorController controller, string nombre)
    {
        foreach (var p in controller.parameters)
            if (p.name == nombre) return true;
        return false;
    }

    private static void AsegurarParametro(AnimatorController controller, string nombre, AnimatorControllerParameterType tipo)
    {
        if (!TieneParametro(controller, nombre)) controller.AddParameter(nombre, tipo);
    }

    private static void LimpiarTransiciones(AnimatorState estado)
    {
        foreach (var t in estado.transitions) Object.DestroyImmediate(t, true);
        estado.transitions = new AnimatorStateTransition[0];
    }
}
