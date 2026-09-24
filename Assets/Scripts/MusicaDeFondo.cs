using System.Collections;
using UnityEngine;

// Reproduce una lista de canciones en loop, muy bajito, con un fundido cruzado entre una y otra.
// Usa dos AudioSource: mientras una se apaga, la otra arranca la cancion siguiente.
// Tambien puede poner un tema especial (por ejemplo la pelea con Umbrae): la lista se apaga suave,
// entra el tema en loop, y al quitarlo vuelve la lista.
public class MusicaDeFondo : MonoBehaviour
{
    [Tooltip("Canciones de la playlist, en el orden en que suenan.")]
    [SerializeField] private AudioClip[] canciones;
    [Tooltip("Volumen de la musica (0 a 1). Muy de fondo: 0.05 a 0.1.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumen = 0.06f;
    [Tooltip("Segundos que dura el fundido entre canciones.")]
    [SerializeField] private float duracionFade = 3f;
    [Tooltip("Mezclar el orden de la lista cada vez que da la vuelta.")]
    [SerializeField] private bool aleatorio = false;
    [Tooltip("Segundos que tarda en pasar de la lista a un tema especial (y al volver).")]
    [SerializeField] private float duracionCambioTema = 1.5f;

    private AudioSource[] fuentes;
    private float[] nivel;           // fundido propio de cada fuente de la lista (0 a 1)
    private int actual;

    private AudioSource fuenteTema;      // el tema especial que suena
    private AudioSource fuenteSaliente;  // el tema anterior, mientras se apaga en un cruce
    private float volumenTema;
    private float volumenSaliente;
    private float inicioTema;            // segundo desde el que arranca el tema (y al que vuelve al dar la vuelta)
    private bool temaActivo;
    private float mezclaTema;        // 0 = suena la lista, 1 = suena el tema

    private void Awake()
    {
        fuentes = new AudioSource[2];
        nivel = new float[2];
        for (int i = 0; i < 2; i++) fuentes[i] = CrearFuente(false);
        fuenteTema = CrearFuente(true);
        fuenteSaliente = CrearFuente(true);
    }

    private AudioSource CrearFuente(bool loop)
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.loop = loop;
        f.spatialBlend = 0f;
        f.volume = 0f;
        return f;
    }

    private void Start()
    {
        if (canciones == null || canciones.Length == 0)
        {
            Debug.LogWarning("[Musica] No hay canciones cargadas en MusicaDeFondo.");
            return;
        }
        StartCoroutine(Reproducir());
    }

    // Volumen de la lista (la musica de todo el mapa).
    public float Volumen => volumen;

    // Pone un tema aparte en loop (la lista se apaga suave mientras tanto).
    // Si ya sonaba otro tema, se cruzan: el anterior se apaga mientras entra el nuevo.
    //  duracionCruce:   segundos del cruce (si es menor a 0, el cambio normal).
    //  duracionSubida:  segundos que tarda el tema nuevo en llegar a 'volumenFinal' (puede ser mas que el cruce).
    //  volumenAlCruzar: volumen al que llega el tema nuevo cuando termina el cruce (si es menor a 0, el final).
    //  desdeSegundo:    segundo de la cancion desde el que arranca (y al que vuelve cuando da la vuelta).
    public void PonerTema(AudioClip tema, float volumenFinal, float duracionCruce = -1f, float duracionSubida = -1f,
                          float volumenAlCruzar = -1f, float desdeSegundo = 0f)
    {
        if (tema == null) return;
        temaActivo = true;
        if (fuenteTema.clip == tema && cambioTema == null && (fuenteTema.isPlaying || mezclaTema > 0f))
        {
            volumenTema = volumenFinal;
            return;
        }
        if (cambioTema != null) StopCoroutine(cambioTema);
        float cruce = Mathf.Max(0.01f, duracionCruce < 0f ? duracionCambioTema : duracionCruce);
        float subida = Mathf.Max(cruce, duracionSubida < 0f ? cruce : duracionSubida);
        float alCruzar = volumenAlCruzar < 0f ? volumenFinal : volumenAlCruzar;
        cambioTema = StartCoroutine(CambiarTema(tema, volumenFinal, cruce, subida, alCruzar, desdeSegundo));
    }

    private Coroutine cambioTema;

    private IEnumerator CambiarTema(AudioClip tema, float volumenFinal, float cruce, float subida, float alCruzar, float desdeSegundo)
    {
        // si sonaba otro tema, pasa a la fuente saliente y se va apagando mientras entra el nuevo
        bool habiaOtro = fuenteTema.clip != null && fuenteTema.clip != tema && mezclaTema > 0f && volumenTema > 0f;
        if (habiaOtro)
        {
            (fuenteTema, fuenteSaliente) = (fuenteSaliente, fuenteTema);
            volumenSaliente = volumenTema;
        }
        else
        {
            fuenteSaliente.Stop();
            volumenSaliente = 0f;
        }

        inicioTema = Mathf.Clamp(desdeSegundo, 0f, Mathf.Max(0f, tema.length - 1f));
        volumenTema = 0f;
        fuenteTema.clip = tema;
        fuenteTema.time = inicioTema;
        fuenteTema.Play();

        float v0 = volumenSaliente;
        for (float t = 0f; t < subida; t += Time.deltaTime)
        {
            volumenSaliente = Mathf.Lerp(v0, 0f, t / cruce);
            volumenTema = t < cruce
                ? Mathf.Lerp(0f, alCruzar, t / cruce)
                : Mathf.Lerp(alCruzar, volumenFinal, (t - cruce) / Mathf.Max(0.01f, subida - cruce));
            yield return null;
        }
        volumenSaliente = 0f;
        fuenteSaliente.Stop();
        volumenTema = volumenFinal;
        cambioTema = null;
    }

    // Saca el tema especial y vuelve la lista.
    public void QuitarTema()
    {
        temaActivo = false;
    }

    private void Update()
    {
        // mismo reloj que la camara, asi el cambio de musica va junto con el de-zoom
        mezclaTema = Mathf.MoveTowards(mezclaTema, temaActivo ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, duracionCambioTema));
        for (int i = 0; i < 2; i++) fuentes[i].volume = volumen * nivel[i] * (1f - mezclaTema);
        fuenteTema.volume = volumenTema * mezclaTema;
        fuenteSaliente.volume = volumenSaliente * mezclaTema;
        // al dar la vuelta, el tema vuelve al segundo desde el que arranco (no a 0:00)
        if (inicioTema > 0f && fuenteTema.clip != null && fuenteTema.isPlaying && fuenteTema.time >= fuenteTema.clip.length - 0.1f)
            fuenteTema.time = inicioTema;
        if (!temaActivo && mezclaTema <= 0f)
        {
            if (fuenteTema.isPlaying) fuenteTema.Stop();
            if (fuenteSaliente.isPlaying) fuenteSaliente.Stop();
        }
    }

    private IEnumerator Reproducir()
    {
        int indice = 0;
        if (aleatorio) Mezclar();

        AudioSource sonando = fuentes[actual];
        sonando.clip = canciones[indice];
        sonando.Play();
        yield return Fundir(-1, actual);

        while (true)
        {
            // espera hasta que falte 'duracionFade' para que termine la cancion (sigue la posicion de la
            // cancion, asi una pausa no la hace saltar); si la fuente se corto sola, no se queda esperando
            float inicio = Time.unscaledTime;
            while (sonando.time < sonando.clip.length - duracionFade)
            {
                if (!sonando.isPlaying && !EnPausa() && Time.unscaledTime - inicio > 2f) break;
                yield return null;
            }

            indice++;
            if (indice >= canciones.Length)
            {
                indice = 0;
                if (aleatorio) Mezclar();
            }

            int saliente = actual;
            actual = 1 - actual;
            AudioSource siguiente = fuentes[actual];
            siguiente.clip = canciones[indice];
            siguiente.Play();
            yield return Fundir(saliente, actual);
            sonando.Stop();
            sonando = siguiente;
        }
    }

    private IEnumerator Fundir(int saliente, int entrante)
    {
        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duracionFade));
            if (saliente >= 0) nivel[saliente] = 1f - k;
            nivel[entrante] = k;
            yield return null;
        }
        if (saliente >= 0) nivel[saliente] = 0f;
        nivel[entrante] = 1f;
    }

    private static bool EnPausa()
    {
#if UNITY_EDITOR
        if (UnityEditor.EditorApplication.isPaused) return true;
#endif
        return AudioListener.pause;
    }

    private void Mezclar()
    {
        for (int i = canciones.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (canciones[i], canciones[j]) = (canciones[j], canciones[i]);
        }
    }
}
