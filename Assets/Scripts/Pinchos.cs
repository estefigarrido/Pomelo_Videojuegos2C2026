using UnityEngine;

// Los pinchos son solidos: la chica camina por encima (no los atraviesa) y mientras los toca
// recibe dano cada 'intervalo' segundos. Sirve tanto con collider solido como con trigger.
[RequireComponent(typeof(Collider2D))]
public class Pinchos : MonoBehaviour
{
    [Tooltip("Vida que saca cada golpe (la vida maxima es 100).")]
    [SerializeField] private float danio = 20f;
    [Tooltip("Segundos entre golpe y golpe mientras sigue tocando los pinchos.")]
    [SerializeField] private float intervalo = 2f;

    private SaludPersonaje victima;
    private float proximoGolpe;

    private void OnCollisionEnter2D(Collision2D choque) => Entrar(choque.collider);
    private void OnCollisionExit2D(Collision2D choque) => Salir(choque.collider);
    private void OnTriggerEnter2D(Collider2D otro) => Entrar(otro);
    private void OnTriggerExit2D(Collider2D otro) => Salir(otro);

    private void Entrar(Collider2D otro)
    {
        var salud = otro.GetComponentInParent<SaludPersonaje>();
        if (salud == null) return;

        bool nuevo = victima != salud;
        victima = salud;
        if (nuevo && Time.time >= proximoGolpe) Golpear();
    }

    private void Salir(Collider2D otro)
    {
        if (victima != null && otro.GetComponentInParent<SaludPersonaje>() == victima) victima = null;
    }

    // Se controla aca y no con OnCollisionStay2D: si la chica se queda quieta, la fisica
    // la "duerme" y deja de avisar el contacto, pero sigue parada sobre los pinchos.
    private void Update()
    {
        if (victima != null && Time.time >= proximoGolpe) Golpear();
    }

    private void Golpear()
    {
        victima.RecibirDanio(danio);
        proximoGolpe = Time.time + intervalo;
    }
}
