using UnityEngine;

// Pasa los frames una vez y se borra.
public class EfectoMuerte : MonoBehaviour
{
    private Sprite[] frames;
    private float fps;
    private float inicio;
    private SpriteRenderer sr;

    public void Iniciar(Sprite[] frames, float fps)
    {
        this.frames = frames;
        this.fps = Mathf.Max(1f, fps);
        inicio = Time.time;
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
    }

    private void Update()
    {
        int i = Mathf.FloorToInt((Time.time - inicio) * fps);
        if (i >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        sr.sprite = frames[i];
    }
}
