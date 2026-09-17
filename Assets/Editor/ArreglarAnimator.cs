using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Rearma las transiciones de CORRER, SALTO y DASH.
// El estado "Golpe 1" y su animacion NO se tocan.
// Menu:  Pomelo -> Arreglar Animator del personaje
public static class ArreglarAnimator
{
    private const string RutaController = "Assets/Assets/animaciones/Personaje.controller";

    [MenuItem("Pomelo/Arreglar Animator del personaje")]
    public static void Arreglar()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaController);
        if (controller == null)
        {
            Debug.LogError("No encontre el controller en " + RutaController);
            return;
        }

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

        // ---- SALTO: desde cualquier estado, menos en medio de un dash ----
        var aSalto = maquina.AddAnyStateTransition(salto);
        aSalto.hasExitTime = false;
        aSalto.duration = 0.05f;
        aSalto.canTransitionToSelf = false;
        aSalto.AddCondition(AnimatorConditionMode.If, 0f, "salto");
        if (hayDash) aSalto.AddCondition(AnimatorConditionMode.IfNot, 0f, "dash");

        // vuelve sola al terminar la animacion, sin apretar nada de nuevo
        var saltoAIdle = salto.AddTransition(idle);
        saltoAIdle.hasExitTime = true;
        saltoAIdle.exitTime = 0.9f;
        saltoAIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animator arreglado:\n" +
                  (hayDash ? "  Any State -> Dash  por el bool 'dash', vuelve a Idle al terminar\n" : "  (todavia no hay estado Dash)\n") +
                  "  Idle <-> Sprint  por el bool 'sprint'\n" +
                  "  Any State -> Salto  por el trigger 'salto', vuelve solo al terminar\n" +
                  "  Golpe 1 sin tocar (sin transiciones que lleven a el)");
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

    private static void LimpiarTransiciones(AnimatorState estado)
    {
        foreach (var t in estado.transitions) Object.DestroyImmediate(t, true);
        estado.transitions = new AnimatorStateTransition[0];
    }
}
