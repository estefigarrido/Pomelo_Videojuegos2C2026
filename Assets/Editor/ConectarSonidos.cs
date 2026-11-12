using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Conecta los sonidos de Assets/_Sonidos con los scripts de la escena que los usan.
// Cada sonido nuevo se suma a la lista de Conectar().
// Menu:  Pomelo -> Sonidos -> Conectar sonidos
public static class ConectarSonidos
{
    private const string Carpeta = "Assets/_Sonidos/";
    // Puerta_piedra_subiendo.wav es la toma 3 (5.86 a 7.72 s) de "stone-slide-sound-effects" que bajo Caro:
    // el arrastre dura 1.75 s, asi que la puerta sube en 1.75 s y el sonido se apaga justo cuando termina
    private const float DuracionSubidaPuerta = 1.75f;

    [MenuItem("Pomelo/Sonidos/Conectar sonidos")]
    public static void Conectar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de conectar los sonidos.");
            return;
        }

        int conectados = 0;
        // puerta del portal de entrada que se levanta: se ajusta tambien la duracion de la subida, asi el
        // arrastre de piedra arranca y termina junto con la puerta
        conectados += Poner<InicioNivel>("sonidoPuerta", "Puerta_piedra_subiendo.wav");
        Numero<InicioNivel>("duracionSubidaPuerta", DuracionSubidaPuerta);
        Numero<InicioNivel>("volumenPuerta", 0.6f);   // de fondo, que no tape la musica

        // latigo de plantas (al estilo "Organic Swish - Bass String Swing"), sincronizado con _latigo_plantas
        conectados += Poner<AtaqueLatigo>("sonidoLatigo", "Latigo_plantas.wav");

        // pasos al caminar: eventos en la animacion + el componente que los hace sonar
        conectados += ConectarPasos();

        // botellitas que se rompen: el mismo vidrio (universfield-glass-bottle-smash) adaptado a cada forma
        conectados += ConectarBotellitas();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Sonidos conectados: " + conectados + ". Guarda la escena con Ctrl+S.");
    }

    // Pasos: la animacion de caminar (ChicaCaminando, 12 cuadros a 12 por segundo) apoya el talon en el
    // cuadro 1 (0 s) y en el cuadro 7 (0.5 s): ahi van los eventos "Paso". El componente PasosPersonaje
    // (en la chica, junto al Animator) los recibe y hace sonar una de las 4 variantes de Paso_madera.
    private const string RutaCaminar = "Assets/_animaciones/ChicaCaminando.anim";
    private static readonly float[] TiemposPaso = { 0f, 0.5f };

    private static int ConectarPasos()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RutaCaminar);
        if (clip == null) { Debug.LogWarning("No encontre " + RutaCaminar); return 0; }
        var eventos = new System.Collections.Generic.List<AnimationEvent>();
        foreach (var e in AnimationUtility.GetAnimationEvents(clip)) if (e.functionName != "Paso") eventos.Add(e);   // conserva otros eventos
        foreach (float t in TiemposPaso) eventos.Add(new AnimationEvent { time = t, functionName = "Paso" });
        AnimationUtility.SetAnimationEvents(clip, eventos.ToArray());
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();

        var chica = Object.FindFirstObjectByType<SaludPersonaje>(FindObjectsInactive.Include);
        if (chica == null) { Debug.LogWarning("No encontre a la chica para los pasos"); return 0; }
        var pasos = chica.GetComponent<PasosPersonaje>();
        if (pasos == null) pasos = Undo.AddComponent<PasosPersonaje>(chica.gameObject);
        var lista = new System.Collections.Generic.List<AudioClip>();
        for (int i = 1; i <= 8; i++)
        {
            var a = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "Paso_madera_" + i + ".wav");
            if (a != null) lista.Add(a);
        }
        var so = new SerializedObject(pasos);
        var prop = so.FindProperty("pasos");
        prop.arraySize = lista.Count;
        for (int i = 0; i < lista.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
        so.ApplyModifiedProperties();
        Debug.Log("Pasos: " + lista.Count + " variantes, en los cuadros 1 y 7 de " + clip.name);
        return lista.Count > 0 ? 1 : 0;
    }

    // Cada botellita lleva el sonido de su forma, segun la animacion de rotura que usa:
    //   botellita1 (_botellarota_01): amarilla en forma de pera, cuello largo   -> Botella_rompe_pera
    //   botellita2 (_botellarota_02): verde oscura, alta y angosta              -> Botella_rompe_alta (mas aguda y corta)
    //   botellita3 (_botellarota_03): turquesa, redonda y panzona               -> Botella_rompe_redonda (mas grave, con cuerpo)
    private static int ConectarBotellitas()
    {
        var pera = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "Botella_rompe_pera.wav");
        var alta = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "Botella_rompe_alta.wav");
        var redonda = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + "Botella_rompe_redonda.wav");
        if (pera == null || alta == null || redonda == null) { Debug.LogWarning("Faltan los sonidos Botella_rompe_* en " + Carpeta); return 0; }

        int pera_ = 0, alta_ = 0, redonda_ = 0, sinSaber = 0;
        foreach (var b in Object.FindObjectsByType<BotellitaRompible>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(b);
            var cuadros = so.FindProperty("cuadrosRota");
            string nombre = cuadros != null && cuadros.arraySize > 0 && cuadros.GetArrayElementAtIndex(0).objectReferenceValue != null
                ? cuadros.GetArrayElementAtIndex(0).objectReferenceValue.name.ToLowerInvariant() : "";
            AudioClip clip = null;
            if (nombre.Contains("botellita1")) { clip = pera; pera_++; }
            else if (nombre.Contains("botellita2")) { clip = alta; alta_++; }
            else if (nombre.Contains("botellita3")) { clip = redonda; redonda_++; }
            else { sinSaber++; Debug.LogWarning("No se de que forma es la botellita '" + b.name + "' (cuadros: " + nombre + ")", b); continue; }
            so.FindProperty("sonidoRotura").objectReferenceValue = clip;
            so.ApplyModifiedProperties();
        }
        Debug.Log("Botellitas con sonido: " + pera_ + " pera, " + alta_ + " altas, " + redonda_ + " redondas" +
                  (sinSaber > 0 ? " (" + sinSaber + " sin reconocer)" : ""));
        return pera_ + alta_ + redonda_;
    }

    // pone un numero en el campo indicado (por ejemplo una duracion que tiene que coincidir con un sonido)
    private static void Numero<T>(string campo, float valor) where T : Object
    {
        var componente = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (componente == null) return;
        var so = new SerializedObject(componente);
        var prop = so.FindProperty(campo);
        if (prop == null) { Debug.LogWarning(typeof(T).Name + " no tiene el campo " + campo); return; }
        prop.floatValue = valor;
        so.ApplyModifiedProperties();
        Debug.Log(typeof(T).Name + "." + campo + " = " + valor + " (para que coincida con su sonido)");
    }

    // pone el sonido en el campo indicado del primer componente de ese tipo que haya en la escena
    private static int Poner<T>(string campo, string archivo) where T : Object
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Carpeta + archivo);
        if (clip == null) { Debug.LogWarning("No encontre el sonido " + Carpeta + archivo); return 0; }
        var componente = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (componente == null) { Debug.LogWarning("No encontre " + typeof(T).Name + " en la escena para " + archivo); return 0; }
        var so = new SerializedObject(componente);
        var prop = so.FindProperty(campo);
        if (prop == null) { Debug.LogWarning(typeof(T).Name + " no tiene el campo " + campo); return 0; }
        prop.objectReferenceValue = clip;
        so.ApplyModifiedProperties();
        Debug.Log(archivo + " -> " + typeof(T).Name + "." + campo);
        return 1;
    }
}
