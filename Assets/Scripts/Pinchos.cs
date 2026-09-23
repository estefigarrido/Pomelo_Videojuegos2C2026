using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Pinchos : MonoBehaviour
{
    [Tooltip("Vida que saca cada golpe (la vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Segundos entre golpe y golpe mientras sigue tocando los pinchos.")]
    [SerializeField] private float intervalo = 2f;

    private SaludPersonaje victima;
    private float proximoGolpe;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        var salud = otro.GetComponentInParent<SaludPersonaje>();
        if (salud == null) return;

        victima = salud;
        Golpear();
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        if (victima == null || otro.GetComponentInParent<SaludPersonaje>() != victima) return;
        if (Time.time >= proximoGolpe) Golpear();
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponentInParent<SaludPersonaje>() == victima) victima = null;
    }

    private void Golpear()
    {
        victima.RecibirDanio(danio);
        proximoGolpe = Time.time + intervalo;
    }
}
