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

    private int ultimo = -1;

    // la llama la animacion de caminar (evento "Paso")
    public void Paso()
    {
        if (pasos == null || pasos.Length == 0 || MenuPausa.BloqueaControles) return;
        int i = Random.Range(0, pasos.Length);
        if (pasos.Length > 1 && i == ultimo) i = (i + 1) % pasos.Length;
        ultimo = i;
        Sonido.Reproducir(pasos[i], volumen, 1f + Random.Range(-variacionTono, variacionTono));
    }
}
