using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Golpe de latigo con la Q. Es independiente del movimiento y del Animator:
// mientras dura, dibuja sus propios frames por encima de lo que puso el Animator
// (LateUpdate corre despues), y al terminar el Animator vuelve a mandar solo.
[RequireComponent(typeof(SpriteRenderer))]
public class AtaqueLatigo : MonoBehaviour
{
    [System.Serializable]
    public struct ZonaDeGolpe
    {
        [Tooltip("Frame de la animacion en el que pega (empieza en 1).")]
        public int frame;
        [Tooltip("Centro de la zona, en unidades, relativo al personaje mirando a la derecha.")]
        public Vector2 centro;
        public Vector2 tamano;
    }

    [Header("Animacion (latigo-plantas)")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;

    [Header("Golpe")]
    [Tooltip("Vida que saca a cada enemigo (la vida maxima de los mobs es 100).")]
    [SerializeField] private float danio = 50f;
    [Tooltip("Segundos de espera desde que termina un latigazo hasta poder tirar otro.")]
    [SerializeField] private float espera = 0.2f;
    [SerializeField] private ZonaDeGolpe[] zonas;

    private SpriteRenderer dibujo;
    private bool atacando;
    private float inicio;
    private float finUltimo = -99f;
    private readonly HashSet<VidaEnemigo> golpeados = new HashSet<VidaEnemigo>();

    public bool Atacando => atacando;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado != null && teclado.qKey.wasPressedThisFrame) Atacar();

        if (!atacando) return;

        int frame = FrameActual();
        if (frame > frames.Length)
        {
            atacando = false;
            finUltimo = Time.time;
            return;
        }

        foreach (var z in zonas)
        {
            if (z.frame != frame) continue;
            ZonaEnMundo(z, out Vector2 centro, out Vector2 tamano);
            foreach (var col in Physics2D.OverlapBoxAll(centro, tamano, 0f))
            {
                var enemigo = col.GetComponentInParent<VidaEnemigo>();
                // cada latigazo le pega una sola vez a cada enemigo
                if (enemigo != null && enemigo.Vivo && golpeados.Add(enemigo)) enemigo.RecibirDanio(danio);
            }
        }
    }

    public bool Atacar()
    {
        if (atacando || frames == null || frames.Length == 0 || Time.time < finUltimo + espera) return false;
        atacando = true;
        inicio = Time.time;
        golpeados.Clear();
        return true;
    }

    private void LateUpdate()
    {
        if (atacando) dibujo.sprite = frames[Mathf.Clamp(FrameActual() - 1, 0, frames.Length - 1)];
    }

    private int FrameActual() => Mathf.FloorToInt((Time.time - inicio) * fps) + 1;

    // la escala negativa en X (mirando a la izquierda) espeja la zona
    private void ZonaEnMundo(ZonaDeGolpe z, out Vector2 centro, out Vector2 tamano)
    {
        Vector2 escala = transform.lossyScale;
        centro = (Vector2)transform.position + Vector2.Scale(z.centro, escala);
        tamano = new Vector2(Mathf.Abs(z.tamano.x * escala.x), Mathf.Abs(z.tamano.y * escala.y));
    }

    private void OnDrawGizmosSelected()
    {
        if (zonas == null) return;
        foreach (var z in zonas)
        {
            ZonaEnMundo(z, out Vector2 centro, out Vector2 tamano);
            Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.9f);
            Gizmos.DrawWireCube(centro, tamano);
        }
    }
}
