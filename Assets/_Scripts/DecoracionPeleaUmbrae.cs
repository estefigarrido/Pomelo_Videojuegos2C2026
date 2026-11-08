using System.Collections.Generic;
using UnityEngine;

// Va en el objeto con la decoracion de la pelea con Umbrae (pinchos del techo y del piso con su glow, banda
// violeta, columnas con la cabeza de Umbrae, llamas y cristales). Esta oculta y aparece suave cuando empieza la
// pelea (ZonaCamaraFija.PeleaEnCurso); se vuelve a ocultar cuando la chica sale de la arena o Umbrae muere.
public class DecoracionPeleaUmbrae : MonoBehaviour
{
    [SerializeField] private ZonaCamaraFija zona;
    [Tooltip("Segundos que tarda en aparecer y desaparecer.")]
    [SerializeField] private float segundosAparecer = 0.6f;
    [Tooltip("Si esta activado, la decoracion queda a la vista en el editor (para acomodarla).")]
    [SerializeField] private bool verEnElEditor = true;

    private readonly List<SpriteRenderer> dibujos = new List<SpriteRenderer>();
    private readonly List<Color> colores = new List<Color>();
    private float alfa = -1f;

    private void Awake()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) { dibujos.Add(sr); colores.Add(sr.color); }
        if (zona == null) zona = FindFirstObjectByType<ZonaCamaraFija>();
        Pintar(0f);
    }

    private void Update()
    {
        float objetivo = zona != null && zona.PeleaEnCurso ? 1f : 0f;
        float nuevo = Mathf.MoveTowards(alfa, objetivo, Time.deltaTime / Mathf.Max(0.01f, segundosAparecer));
        if (!Mathf.Approximately(nuevo, alfa)) Pintar(nuevo);
    }

    private void Pintar(float a)
    {
        alfa = a;
        for (int i = 0; i < dibujos.Count; i++)
        {
            if (dibujos[i] == null) continue;
            dibujos[i].enabled = a > 0f;
            var c = colores[i]; c.a *= a; dibujos[i].color = c;
        }
    }

    private void OnValidate()
    {
        // en el editor (sin jugar) se puede ver u ocultar para acomodarla
        if (Application.isPlaying) return;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = verEnElEditor;
    }
}
