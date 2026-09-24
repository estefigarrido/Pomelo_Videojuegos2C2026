using System.Collections;
using UnityEngine;

// Pasa un dibujo por una lista de frames fundiendo cada uno con el siguiente, asi un giro de pocos
// frames no queda "a los saltos". Lo usan la salida del portal de entrada y la entrada al portal final.
public static class GiroSuave
{
    public static IEnumerator Reproducir(SpriteRenderer dibujo, Sprite[] frames, float duracion)
    {
        if (dibujo == null || frames == null || frames.Length == 0) yield break;
        dibujo.sprite = frames[0];
        if (frames.Length == 1) yield break;

        // copia encima del dibujo, donde aparece el frame siguiente
        var go = new GameObject("Giro (fundido)");
        go.transform.SetParent(dibujo.transform, false);
        var encima = go.AddComponent<SpriteRenderer>();
        encima.sharedMaterial = dibujo.sharedMaterial;
        encima.sortingLayerID = dibujo.sortingLayerID;
        encima.sortingOrder = dibujo.sortingOrder + 1;
        encima.flipX = dibujo.flipX;
        encima.maskInteraction = dibujo.maskInteraction;

        float porTramo = duracion / (frames.Length - 1);
        for (int i = 1; i < frames.Length; i++)
        {
            encima.sprite = frames[i];
            for (float t = 0f; t < porTramo; t += Time.deltaTime)
            {
                SetAlfa(encima, dibujo.color.a * Mathf.SmoothStep(0f, 1f, t / porTramo));
                yield return null;
            }
            dibujo.sprite = frames[i];
        }
        Object.Destroy(go);
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        var c = sr.color; c.a = a; sr.color = c;
    }
}
