using System.Collections;
using TMPro;
using UnityEngine;

// Pantalla "Estadistica final" (Figma, pagina PANTALLAS LORE). Aparece cuando se cierra la ultima puerta
// (FinalNivel.Termino): el juego se oscurece (negro al 75 %) y van apareciendo, una por una, las partes:
// el emblema, "completaste el / Templo de botanica", el ornamento, "¡Bien hecho!", el texto, el desempeno y la
// nota de abajo. Lo unico que cambia es el desempeno: el porcentaje de reparacion del templo
// (RestauracionTemplo.PorcentajeReparado), que cuenta desde 0 hasta su valor.
// La arma el menu  Pomelo -> Pantallas -> Armar estadistica final  (Assets/Editor/ArmarEstadisticaFinal.cs).
[RequireComponent(typeof(CanvasGroup))]
public class EstadisticaFinal : MonoBehaviour
{
    public static bool Visible { get; private set; }

    [Header("Piezas (las pone el menu que arma la pantalla)")]
    [SerializeField] private CanvasGroup fondo;
    [Tooltip("Partes que aparecen una por una, en este orden.")]
    [SerializeField] private CanvasGroup[] partes;
    [SerializeField] private TMP_Text valor;
    [Tooltip("La parte del desempeno: cuando aparece, el numero empieza a contar.")]
    [SerializeField] private CanvasGroup parteDesempeno;

    [Header("Cuando aparece")]
    [Tooltip("Vacio = la busca sola.")]
    [SerializeField] private FinalNivel final;
    [Tooltip("Segundos despues de que se cierra la puerta.")]
    [SerializeField] private float espera = 0.6f;
    [SerializeField] private float segundosFondo = 1.2f;
    [Tooltip("Segundos entre que aparece una parte y la siguiente.")]
    [SerializeField] private float entrePartes = 0.35f;
    [SerializeField] private float segundosParte = 0.7f;
    [Tooltip("Cuanto sube cada parte mientras aparece (px de la pantalla de 1920 x 1080).")]
    [SerializeField] private float subida = 14f;
    [Tooltip("Segundos que tarda el numero en contar hasta el desempeno.")]
    [SerializeField] private float segundosConteo = 1.4f;

    private CanvasGroup grupo;
    private bool mostrado;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (fondo != null) fondo.alpha = 0f;
        if (partes != null) foreach (var p in partes) if (p != null) p.alpha = 0f;
        // sin kerning: los pares importados de Alegreya vienen exagerados y separan / pegan letras al azar
        foreach (var t in GetComponentsInChildren<TMP_Text>(true)) t.fontFeatures.Clear();
        Visible = false;
    }

    private void Start()
    {
        if (final == null) final = FindFirstObjectByType<FinalNivel>();
    }

    private void Update()
    {
        if (!mostrado && final != null && final.Termino)
        {
            mostrado = true;
            StartCoroutine(Mostrar());
        }
    }

    // Tambien se puede llamar a mano (por ejemplo para probarla).
    [ContextMenu("Mostrar")]
    public void MostrarAhora()
    {
        if (mostrado) return;
        mostrado = true;
        StartCoroutine(Mostrar());
    }

    private IEnumerator Mostrar()
    {
        int desempeno = Mathf.RoundToInt(RestauracionTemplo.Instancia != null ? RestauracionTemplo.Instancia.PorcentajeReparado : 0f);
        Debug.Log("[Estadistica final] desempeno " + desempeno + " / 100");
        if (valor != null) valor.text = "0 / 100";

        yield return new WaitForSeconds(espera);
        Visible = true;
        grupo.alpha = 1f;
        grupo.blocksRaycasts = true;
        if (fondo != null) yield return Fundir(fondo, null, 0f, segundosFondo);

        if (partes == null) yield break;
        foreach (var p in partes)
        {
            if (p == null) continue;
            StartCoroutine(Fundir(p, p.transform as RectTransform, subida, segundosParte));
            if (p == parteDesempeno) StartCoroutine(Contar(desempeno));
            yield return new WaitForSeconds(entrePartes);
        }
    }

    // aparece: de transparente a opaco, subiendo un poquito hasta su lugar
    private static IEnumerator Fundir(CanvasGroup g, RectTransform rt, float subida, float tiempo)
    {
        Vector2 lugar = rt != null ? rt.anchoredPosition : Vector2.zero;
        for (float t = 0f; t < tiempo; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / tiempo);
            g.alpha = k;
            if (rt != null) rt.anchoredPosition = lugar + Vector2.down * subida * (1f - k);
            yield return null;
        }
        g.alpha = 1f;
        if (rt != null) rt.anchoredPosition = lugar;
    }

    private IEnumerator Contar(int hasta)
    {
        if (valor == null) yield break;
        for (float t = 0f; t < segundosConteo; t += Time.unscaledDeltaTime)
        {
            // frena al llegar (como un contador que se asienta)
            float k = 1f - Mathf.Pow(1f - t / segundosConteo, 3f);
            valor.text = Mathf.RoundToInt(hasta * k) + " / 100";
            yield return null;
        }
        valor.text = hasta + " / 100";
    }
}
