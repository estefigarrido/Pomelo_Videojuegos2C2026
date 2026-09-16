using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Rearma las transiciones de CORRER y SALTO.
// El estado "Golpe 1" y su animacion NO se tocan: solo se saca la transicion
// rota que hacia que el personaje entrara solo al golpe sin apretar nada.
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

        if (idle == null || sprint == null || salto == null)
        {
            Debug.LogError("Faltan estados: necesito Idle, Sprint y Salto.");
            return;
        }

        // Saco las transiciones viejas de estos tres estados.
        // Esto tambien elimina la Idle -> Golpe 1 que no tenia condicion y
        // hacia que se fuera sola al golpe apenas quedaba quieta.
        LimpiarTransiciones(idle);
        LimpiarTransiciones(sprint);
        LimpiarTransiciones(salto);

        foreach (var t in maquina.anyStateTransitions) Object.DestroyImmediate(t, true);
        maquina.anyStateTransitions = new AnimatorStateTransition[0];

        maquina.defaultState = idle;

        // ---- CORRER: Idle <-> Sprint, al toque ----
        var aSprint = idle.AddTransition(sprint);
        aSprint.hasExitTime = false;
        aSprint.duration = 0f;
        aSprint.AddCondition(AnimatorConditionMode.If, 0f, "sprint");

        var aIdle = sprint.AddTransition(idle);
        aIdle.hasExitTime = false;
        aIdle.duration = 0f;
        aIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "sprint");

        // ---- SALTO: desde cualquier estado, asi tambien salta corriendo ----
        var aSalto = maquina.AddAnyStateTransition(salto);
        aSalto.hasExitTime = false;
        aSalto.duration = 0.05f;
        aSalto.canTransitionToSelf = false;
        aSalto.AddCondition(AnimatorConditionMode.If, 0f, "salto");

        // vuelve sola al terminar la animacion, sin apretar nada de nuevo
        var saltoAIdle = salto.AddTransition(idle);
        saltoAIdle.hasExitTime = true;
        saltoAIdle.exitTime = 0.9f;
        saltoAIdle.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Animator arreglado:\n" +
                  "  Idle <-> Sprint  por el bool 'sprint'\n" +
                  "  Any State -> Salto  por el trigger 'salto', vuelve solo al terminar\n" +
                  "  Golpe 1 quedo sin tocar y ya no se entra solo (lo conectamos manana)");
    }

    private static AnimatorState Buscar(AnimatorStateMachine maquina, string nombre)
    {
        foreach (var hijo in maquina.states)
            if (hijo.state.name == nombre) return hijo.state;
        return null;
    }

    private static void LimpiarTransiciones(AnimatorState estado)
    {
        foreach (var t in estado.transitions) Object.DestroyImmediate(t, true);
        estado.transitions = new AnimatorStateTransition[0];
    }
}
