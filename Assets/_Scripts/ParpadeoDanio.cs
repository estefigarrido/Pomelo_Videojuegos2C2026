using UnityEngine;

// Parpadeo rojo al recibir dano. Solo cambia el color del dibujo: no frena el movimiento,
// ni los ataques, ni la animacion. Lo usan SaludPersonaje (la chica) y VidaEnemigo (los mobs).
[System.Serializable]
public class ParpadeoDanio
{
    [Tooltip("Color que toma en cada parpadeo.")]
    [SerializeField] private Color color = new Color(1f, 0.15f, 0.15f, 1f);
    [Tooltip("Cuantas veces se pone roja/o.")]
    [SerializeField] private int veces = 3;
    [Tooltip("Segundos que dura todo el parpadeo.")]
    [SerializeField] private float duracion = 0.36f;

    private SpriteRenderer dibujo;
    private Color colorOriginal = Color.white;
    private float inicio = -1f;

    public void Iniciar(SpriteRenderer dibujo)
    {
        this.dibujo = dibujo;
        if (dibujo != null) colorOriginal = dibujo.color;
    }

    // Si ya estaba parpadeando (otro golpe seguido), vuelve a empezar.
    public void Empezar()
    {
        if (dibujo == null) return;
        inicio = Time.time;
        Pintar(true);
    }

    public void Actualizar()
    {
        if (inicio < 0f || dibujo == null) return;

        float t = (Time.time - inicio) / Mathf.Max(0.01f, duracion);
        if (t >= 1f)
        {
            inicio = -1f;
            Pintar(false);
            return;
        }
        // cada parpadeo es mitad rojo y mitad color normal
        Pintar(Mathf.FloorToInt(t * Mathf.Max(1, veces) * 2f) % 2 == 0);
    }

    // Lo corta en seco y deja el color original (por ejemplo, al reiniciar la pelea con Umbrae).
    public void Cortar()
    {
        inicio = -1f;
        if (dibujo != null) dibujo.color = colorOriginal;
    }

    // Respeta la transparencia que tenga en ese momento (otros scripts hacen fundidos con el alfa).
    private void Pintar(bool rojo)
    {
        Color c = rojo ? color : colorOriginal;
        c.a = dibujo.color.a;
        dibujo.color = c;
    }
}
