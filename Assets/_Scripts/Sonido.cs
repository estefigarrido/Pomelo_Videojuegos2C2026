using UnityEngine;

// Para hacer sonar un efecto (los de Assets/_Sonidos) una vez, sin tener que poner un AudioSource a mano:
//   Sonido.Reproducir(clip, volumen);
// Suena "en 2D" (igual de fuerte este donde este la camara) y el objeto que lo hace sonar se borra solo.
public static class Sonido
{
    public static AudioSource Reproducir(AudioClip clip, float volumen = 1f, float tono = 1f)
    {
        if (clip == null) return null;
        var go = new GameObject("Sonido: " + clip.name);
        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.volume = volumen;
        fuente.pitch = tono;
        fuente.spatialBlend = 0f;
        fuente.playOnAwake = false;
        fuente.Play();
        Object.Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(tono)) + 0.1f);
        return fuente;
    }
}
