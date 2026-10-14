using UnityEngine;

// Inclina al comelibros hacia adelante para que apoye todas las patas en el piso.
// Como el dibujo se espeja con flipX al darse vuelta, la inclinacion tambien se espeja.
public class InclinacionComelibros : MonoBehaviour
{
    [Tooltip("Grados de inclinacion cuando mira hacia su lado original (izquierda).")]
    [SerializeField] private float angulo = -4.88f;

    private SpriteRenderer dibujo;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        float z = dibujo != null && dibujo.flipX ? -angulo : angulo;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }
}
