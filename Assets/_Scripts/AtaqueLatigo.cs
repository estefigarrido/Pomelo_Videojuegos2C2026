using System.Collections.Generic;
using UnityEngine;

// Golpe de latigo (lo tira SelectorAtaque con click izquierdo). Es independiente del movimiento y del Animator:
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
    [Tooltip("Segundos desde que se tira un latigazo hasta poder tirar el siguiente. Si la animacion " +
             "todavia no termino, se corta y empieza de nuevo. Antes eran 1.03 s (animacion 0.83 s + 0.2 s de espera); ahora 0.5 s menos.")]
    [SerializeField] private float intervaloEntreUsos = 0.53f;
    [SerializeField] private ZonaDeGolpe[] zonas;

    private SpriteRenderer dibujo;
    private AtaqueSabiduria otroAtaque;
    private bool atacando;
    private float inicio;
    private float ultimoUso = -99f;
    private readonly HashSet<VidaEnemigo> golpeados = new HashSet<VidaEnemigo>();

    public bool Atacando => atacando;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        otroAtaque = GetComponent<AtaqueSabiduria>();
    }

    private void Update()
    {
        if (!atacando) return;

        int frame = FrameActual();
        if (frame > frames.Length)
        {
            atacando = false;
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
                // las botellitas se rompen con un solo golpe
                var botellita = col.GetComponent<BotellitaRompible>();
                if (botellita != null) botellita.Romper();
            }
        }
    }

    // Si el latigazo anterior todavia se esta dibujando, se corta y la animacion arranca de nuevo.
    public bool Atacar()
    {
        if (frames == null || frames.Length == 0 || Time.time < ultimoUso + intervaloEntreUsos) return false;
        if (otroAtaque != null && otroAtaque.Atacando) return false;
        atacando = true;
        inicio = Time.time;
        ultimoUso = Time.time;
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
