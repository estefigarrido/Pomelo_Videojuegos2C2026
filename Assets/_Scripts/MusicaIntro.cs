using UnityEngine;

// Musica de la escena de inicio (Inicio_introducción), con el mismo volumen y fundido que la musica del nivel:
//  - Pantalla de titulo: "Leaves from the Vine" entra con un fundido (y si dura mas que la cancion, vuelve a empezar
//    con un fundido cruzado). Al tocar Comenzar se apaga con un fundido.
//  - Al tocar Comenzar entra "Katara" con un fundido y queda en loop (al llegar al final vuelve a empezar con un
//    fundido cruzado) durante toda la intro, hasta que la chica entra al portal del final: ahi se apaga con un fundido.
// Estas canciones son solo de la intro: no estan en la lista de MusicaDeFondo del nivel.
// La pone el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción  (objeto "Musica").
public class MusicaIntro : MonoBehaviour
{
    [SerializeField] private AudioClip temaTitulo;
    [SerializeField] private AudioClip temaIntro;
    [Tooltip("Volumen (0 a 1). El mismo que la musica del nivel (MusicaDeFondo de Nivel1_Templo: 0.072).")]
    [Range(0f, 1f)] [SerializeField] private float volumen = 0.072f;
    [Tooltip("Segundos de cada fundido (entrar, salir y el cruce al volver a empezar). El del nivel: 3.")]
    [SerializeField] private float duracionFade = 3f;

    public static MusicaIntro Instancia { get; private set; }

    // una cancion que se repite con un fundido cruzado entre el final y el principio
    private class Pista
    {
        public AudioClip clip;
        public AudioSource actual, otra;
        public float nivel, objetivo;   // fundido de entrada / salida (0 a 1)
        public float cruce = -1f;       // 0 a 1 mientras cruza el final con el principio; -1 = no cruza
    }

    private Pista titulo, intro;

    private void Awake()
    {
        Instancia = this;
        titulo = Crear(temaTitulo);
        intro = Crear(temaIntro);
    }

    private Pista Crear(AudioClip clip)
    {
        return new Pista { clip = clip, actual = Fuente(), otra = Fuente() };
    }

    private AudioSource Fuente()
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.loop = false;
        f.spatialBlend = 0f;
        f.volume = 0f;
        return f;
    }

    private void Start() => Empezar(titulo);

    // Lo llama PantallaInicio al tocar Comenzar
    public void Comenzar()
    {
        titulo.objetivo = 0f;
        Empezar(intro);
    }

    // Lo llama el portal del final de la intro
    public void SalirPorElPortal()
    {
        intro.objetivo = 0f;
    }

    private void Empezar(Pista p)
    {
        if (p.clip == null || p.objetivo > 0f) return;
        p.objetivo = 1f;
        if (!p.actual.isPlaying) { p.actual.clip = p.clip; p.actual.time = 0f; p.actual.Play(); }
    }

    private bool pausada;

    private void Update()
    {
        // con el menu de pausa la musica se frena y sigue desde el mismo punto
        bool pausa = MenuPausa.Abierto;
        if (pausa != pausada)
        {
            pausada = pausa;
            foreach (var p in new[] { titulo, intro })
                foreach (var f in new[] { p.actual, p.otra })
                    if (pausa) f.Pause(); else f.UnPause();
        }
        if (pausada) return;
        Avanzar(titulo);
        Avanzar(intro);
    }

    private void Avanzar(Pista p)
    {
        if (p.clip == null) return;
        float paso = Time.unscaledDeltaTime / Mathf.Max(0.01f, duracionFade);
        p.nivel = Mathf.MoveTowards(p.nivel, p.objetivo, paso);

        // vuelve a empezar: cuando falta 'duracionFade' para el final, arranca la otra fuente desde el principio
        if (p.cruce < 0f && p.actual.isPlaying && p.objetivo > 0f && p.actual.time >= p.clip.length - duracionFade)
        {
            p.otra.clip = p.clip;
            p.otra.time = 0f;
            p.otra.Play();
            p.cruce = 0f;
        }
        if (p.cruce >= 0f)
        {
            p.cruce = Mathf.MoveTowards(p.cruce, 1f, paso);
            if (p.cruce >= 1f)
            {
                p.actual.Stop();
                (p.actual, p.otra) = (p.otra, p.actual);
                p.cruce = -1f;
            }
        }

        float k = p.cruce >= 0f ? p.cruce : 0f;
        p.actual.volume = volumen * p.nivel * (1f - k);
        p.otra.volume = p.cruce >= 0f ? volumen * p.nivel * k : 0f;

        // ya se apago del todo: se frena
        if (p.objetivo <= 0f && p.nivel <= 0f)
        {
            if (p.actual.isPlaying) p.actual.Stop();
            if (p.otra.isPlaying) p.otra.Stop();
            p.cruce = -1f;
        }
    }
}
