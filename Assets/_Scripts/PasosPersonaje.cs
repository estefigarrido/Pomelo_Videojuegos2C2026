using UnityEngine;

// Pasos de la chica al caminar (zapatilla sobre madera, Assets/_Sonidos/Paso_madera_1..4).
// La animacion ChicaCaminando llama a Paso() en los cuadros en que apoya el talon (cuadro 1 y cuadro 7,
// 0 s y 0.5 s de cada vuelta): esos "eventos" los pone el menu Pomelo -> Sonidos -> Conectar sonidos.
// Cada paso usa una variante al azar (nunca la misma dos veces seguidas), asi no suena repetido.
[RequireComponent(typeof(Animator))]
public class PasosPersonaje : MonoBehaviour
{
    [SerializeField] private AudioClip[] pasos;
    [Range(0f, 1f)]
    [SerializeField] private float volumen = 0.5f;
    [Tooltip("Cuanto puede variar el tono de un paso a otro (0.04 = 4 % para arriba o para abajo).")]
    [SerializeField] private float variacionTono = 0.04f;

    [Header("Corriendo (ChicaSprint)")]
    [Tooltip("Al correr pisa mas fuerte: el mismo paso, un poco mas alto de volumen.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumenCorriendo = 0.65f;
    [Tooltip("Y un poquito mas agudo (apoyo mas corto y rapido).")]
    [SerializeField] private float tonoCorriendo = 1.06f;

    [Header("Sobre las hojas de loto")]
    [Tooltip("Pasos sobre las hojas del lago (Paso_hoja_*): hoja blanda que cruje, con un poquito de agua abajo. " +
             "Se usan en vez de los pasos comunes mientras la chica esta parada sobre una HojaLotoQueSeHunde.")]
    [SerializeField] private AudioClip[] pasosHoja;

    private int ultimo = -1;
    private MovimientoPersonaje movimiento;

    private void Awake() => movimiento = GetComponent<MovimientoPersonaje>();

    // la llama la animacion de caminar (evento "Paso")
    public void Paso() => Sonar(volumen, 1f);

    // la llama la animacion de correr, ChicaSprint (evento "PasoCorriendo", cuadros 3 y 10)
    public void PasoCorriendo() => Sonar(volumenCorriendo, tonoCorriendo);

    private void Sonar(float vol, float tono)
    {
        if (MenuPausa.BloqueaControles) return;
        // sobre una hoja de loto suena a hoja; en el resto, el paso de siempre
        var lista = movimiento != null && movimiento.SobreHoja && pasosHoja != null && pasosHoja.Length > 0 ? pasosHoja : pasos;
        if (lista == null || lista.Length == 0) return;
        int i = Random.Range(0, lista.Length);
        if (lista.Length > 1 && i == ultimo) i = (i + 1) % lista.Length;
        ultimo = i;
        Sonido.Reproducir(lista[i], vol, tono + Random.Range(-variacionTono, variacionTono));
    }
}
