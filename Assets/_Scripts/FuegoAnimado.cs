using UnityEngine;

// Llama en loop: pasa los frames de la llama (cuerpo que late, punta que latiguea y llamitas que se
// desprenden). Cada fuego arranca en un frame distinto para que no se muevan todos iguales.
// Se suma a lo que ya hace InicioPeleaUmbrae (mecerse, titilar), que no toca el dibujo.
[RequireComponent(typeof(SpriteRenderer))]
public class FuegoAnimado : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [Tooltip("Si esta prendido, cada fuego arranca en un frame al azar.")]
    [SerializeField] private bool desfasarAlAzar = true;

    private SpriteRenderer dibujo;
    private float desfase;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        if (desfasarAlAzar && frames != null && frames.Length > 0) desfase = Random.Range(0, frames.Length) / fps;
    }

    private void LateUpdate()
    {
        if (frames == null || frames.Length == 0) return;
        int i = Mathf.FloorToInt((Time.time + desfase) * fps) % frames.Length;
        dibujo.sprite = frames[i];
    }
}
