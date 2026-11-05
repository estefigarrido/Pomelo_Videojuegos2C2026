using UnityEngine;
using UnityEngine.UI;

// Barra de abajo del HUD que muestra cuanto sprint queda (duracionSprint de MovimientoPersonaje).
// Aparece con un fundido corto al empezar a sprintear, se va vaciando y se apaga al dejar de sprintear.
// Si el sprint se acabo por tiempo, no vuelve a aparecer hasta que pase el cooldownSprint (antes no se
// puede sprintear). Si suelta Shift y vuelve a sprintear, el sprint y la barra arrancan completos.
[RequireComponent(typeof(CanvasGroup))]
public class HUDBarraSprint : MonoBehaviour
{
    [Tooltip("La parte blanca de adentro, la que se va achicando.")]
    [SerializeField] private RectTransform relleno;
    [Tooltip("Separacion entre el borde de la barra y la parte de adentro, en pixeles del HUD.")]
    [SerializeField] private float margen = 4f;
    [Tooltip("Segundos del fundido al aparecer.")]
    [SerializeField] private float segundosAparecer = 0.15f;
    [Tooltip("Segundos del fundido al desaparecer.")]
    [SerializeField] private float segundosDesaparecer = 0.2f;

    private CanvasGroup grupo;
    private MovimientoPersonaje movimiento;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    private void Start()
    {
        movimiento = FindFirstObjectByType<MovimientoPersonaje>();
    }

    private void Update()
    {
        bool sprint = movimiento != null && movimiento.SprintActivo;
        float segundos = sprint ? segundosAparecer : segundosDesaparecer;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, sprint ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, segundos));

        // al dejar de sprintear se apaga tal como estaba, sin llenarse ni vaciarse
        if (!sprint) return;

        // con duracionSprint en 0 el sprint no se acaba: la barra queda llena
        float duracion = movimiento.DuracionSprint;
        float queda = duracion > 0f ? Mathf.Clamp01(movimiento.SegundosRestantesSprint / duracion) : 1f;
        MostrarCuanto(queda);
    }

    private void MostrarCuanto(float queda)
    {
        if (relleno == null) return;
        float anchoTotal = ((RectTransform)transform).rect.width - margen * 2f;
        relleno.sizeDelta = new Vector2(anchoTotal * queda, relleno.sizeDelta.y);
        relleno.gameObject.SetActive(queda > 0f);
    }
}
