using TMPro;
using UnityEditor;
using UnityEngine;

// Por que GitHub mostraba "Alegreya SDF.asset" como modificada aunque nadie la tocara:
// las fuentes estaban en modo DINAMICO. En ese modo, cada vez que el juego muestra una letra que todavia no
// estaba en la fuente (al darle Play, al abrir una escena, al cambiar un texto), TextMeshPro la agrega y
// MODIFICA el archivo. Le pasaba a cada uno del equipo, y cada uno subia su version.
//
// Este menu carga de una vez todas las letras que puede usar el juego (en castellano, con acentos, ñ, ¿¡,
// comillas, numeros...) y pasa las fuentes a modo ESTATICO: desde ahi el archivo ya no cambia solo.
// Despues de correrlo, hay que subir los dos .asset UNA vez (y que el resto haga Pull).
// Si algun dia hace falta una letra rara que no este, se agrega en Caracteres y se vuelve a correr.
// Menu:  Pomelo -> Fuentes -> Fijar fuentes (que no cambien solas)
public static class FijarFuentes
{
    private static readonly string[] Rutas =
    {
        "Assets/_Fuentes/Alegreya/Alegreya SDF.asset",
        "Assets/_Fuentes/Alegreya/Alegreya Bold SDF.asset",
    };

    // todo lo imprimible de ASCII + Latin-1 (acentos, ñ, ü, ¿, ¡, º, ª, «, »...) + comillas y rayas tipograficas
    public static string Caracteres()
    {
        var sb = new System.Text.StringBuilder();
        for (int c = 32; c <= 126; c++) sb.Append((char)c);
        for (int c = 160; c <= 255; c++) sb.Append((char)c);
        sb.Append("‘’“”–—…•€");   // ‘ ’ “ ” – — … • €
        return sb.ToString();
    }

    [MenuItem("Pomelo/Fuentes/Fijar fuentes (que no cambien solas)")]
    public static void Fijar()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Para el Play antes de fijar las fuentes."); return; }
        foreach (var ruta in Rutas)
        {
            var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
            if (fuente == null) { Debug.LogWarning("No encontre " + ruta); continue; }
            Fijar(fuente);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Fuentes fijas (estaticas): ya no se modifican solas. Subi los dos .asset de Alegreya una vez.");
    }

    // carga todas las letras y deja la fuente en modo estatico
    public static void Fijar(TMP_FontAsset fuente)
    {
        if (fuente == null) return;
        // para poder agregar letras tiene que estar en dinamico un momento
        fuente.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fuente.TryAddCharacters(Caracteres(), out string faltan);
        if (!string.IsNullOrEmpty(faltan))
            Debug.Log(fuente.name + ": la tipografia no trae estas letras (no pasa nada): " + faltan);
        fuente.atlasPopulationMode = AtlasPopulationMode.Static;
        // que al hacer el build no borre las letras (en esta version de TMP no es publico: va por SerializedObject)
        var so = new SerializedObject(fuente);
        var limpiar = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (limpiar != null) { limpiar.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(fuente);
        foreach (var tex in fuente.atlasTextures) if (tex != null) EditorUtility.SetDirty(tex);
        Debug.Log(fuente.name + ": " + fuente.characterTable.Count + " letras, modo estatico");
    }
}
