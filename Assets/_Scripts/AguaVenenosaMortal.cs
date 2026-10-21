using UnityEngine;

// Agua venenosa: si la chica la toca, sale al instante del agua y reaparece en el punto de reaparicion
// del lago (en el piso de al lado), perdiendo una vida (20 de 100). Si era la ultima, muere como siempre.
// Va en el "agua violeta" de cada lago, con un BoxCollider2D trigger del tamano del agua.
[RequireComponent(typeof(Collider2D))]
public class AguaVenenosaMortal : MonoBehaviour
{
    [Tooltip("Donde reaparece la chica (centro del cuerpo, apoyada en el piso).")]
    [SerializeField] private Transform puntoReaparicion;
    [Tooltip("Vida que saca (20 = una de las 5 vidas).")]
    [SerializeField] private float danio = 20f;

    private float proximoGolpe;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D otro) => Tocar(otro);
    private void OnTriggerStay2D(Collider2D otro) => Tocar(otro);

    private void Tocar(Collider2D otro)
    {
        if (Time.time < proximoGolpe || puntoReaparicion == null) return;
        var salud = otro.GetComponentInParent<SaludPersonaje>();
        if (salud == null) return;
        proximoGolpe = Time.time + 0.5f;   // que no cuente dos veces la misma caida
        salud.CaerAlAguaVenenosa(danio, puntoReaparicion.position);
    }

    private void OnDrawGizmos()
    {
        if (puntoReaparicion == null) return;
        Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.9f);
        Gizmos.DrawWireSphere(puntoReaparicion.position, 0.3f);
        Gizmos.DrawLine(transform.position, puntoReaparicion.position);
    }
}
