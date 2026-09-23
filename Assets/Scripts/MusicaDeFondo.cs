using System.Collections;
using UnityEngine;

// Reproduce una lista de canciones en loop, muy bajito, con un fundido cruzado entre una y otra.
// Usa dos AudioSource: mientras una se apaga, la otra arranca la cancion siguiente.
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

    private AudioSource[] fuentes;
    private int actual;

    private void Awake()
    {
        fuentes = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            fuentes[i] = gameObject.AddComponent<AudioSource>();
            fuentes[i].playOnAwake = false;
            fuentes[i].loop = false;
            fuentes[i].spatialBlend = 0f;
            fuentes[i].volume = 0f;
        }
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

    private IEnumerator Reproducir()
    {
        int indice = 0;
        if (aleatorio) Mezclar();

        AudioSource sonando = fuentes[actual];
        sonando.clip = canciones[indice];
        sonando.Play();
        yield return Fundir(null, sonando);

        while (true)
        {
            // espera hasta que falte 'duracionFade' para que termine la cancion
            while (sonando.isPlaying && sonando.clip.length - sonando.time > duracionFade)
                yield return null;

            indice++;
            if (indice >= canciones.Length)
            {
                indice = 0;
                if (aleatorio) Mezclar();
            }

            actual = 1 - actual;
            AudioSource siguiente = fuentes[actual];
            siguiente.clip = canciones[indice];
            siguiente.Play();
            yield return Fundir(sonando, siguiente);
            sonando.Stop();
            sonando = siguiente;
        }
    }

    private IEnumerator Fundir(AudioSource saliente, AudioSource entrante)
    {
        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duracionFade));
            if (saliente != null) saliente.volume = volumen * (1f - k);
            entrante.volume = volumen * k;
            yield return null;
        }
        if (saliente != null) saliente.volume = 0f;
        entrante.volume = volumen;
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
