using UnityEngine;

// Para hacer sonar un efecto (los de Assets/_Sonidos) una vez, sin tener que poner un AudioSource a mano:
//   Sonido.Reproducir(clip, volumen);
// Suena "en 2D" (igual de fuerte este donde este la camara) y el objeto que lo hace sonar se borra solo.
// Todos los efectos pasan por VolumenEfectos: bajandolo (o subiendolo) se ajustan todos juntos, sin tocar la musica.
public static class Sonido
{
    // volumen general de los efectos de sonido (puerta, botellitas, menu de pausa...), de 0 a 1
    // 0.30: nivelado con la musica de fondo (antes 0.45, un poco por encima)
    public const float VolumenEfectos = 0.30f;

    public static AudioSource Reproducir(AudioClip clip, float volumen = 1f, float tono = 1f)
    {
        if (clip == null) return null;
        var go = new GameObject("Sonido: " + clip.name);
        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = clip;
        fuente.volume = volumen * VolumenEfectos;
        fuente.pitch = tono;
        fuente.spatialBlend = 0f;
        fuente.playOnAwake = false;
        fuente.Play();
        // se borra con el tiempo del juego: en pausa (timeScale 0) espera, asi el sonido puede seguir al reanudar
        Object.Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(tono)) + 0.1f);
        sonando.RemoveAll(f => f == null);
        sonando.Add(fuente);
        if (enPausa) fuente.Pause();
        return fuente;
    }

    // ---------- pausa ----------
    // El menu de pausa congela los efectos que estan sonando (la puerta, una botellita...) y los sigue al cerrar.
    // La musica no pasa por aca, asi que sigue sonando.
    private static readonly System.Collections.Generic.List<AudioSource> sonando = new System.Collections.Generic.List<AudioSource>();
    private static bool enPausa;

    public static void Pausar()
    {
        enPausa = true;
        sonando.RemoveAll(f => f == null);
        foreach (var f in sonando) f.Pause();
    }

    public static void Reanudar()
    {
        enPausa = false;
        sonando.RemoveAll(f => f == null);
        foreach (var f in sonando) f.UnPause();
    }
}
