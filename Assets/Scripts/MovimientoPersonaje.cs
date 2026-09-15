using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoPersonaje : MonoBehaviour
{
    [SerializeField] private float velocidad = 5f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        float direccion = 0f;
        if (teclado.aKey.isPressed) direccion -= 1f;
        if (teclado.dKey.isPressed) direccion += 1f;

        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);
    }
}
