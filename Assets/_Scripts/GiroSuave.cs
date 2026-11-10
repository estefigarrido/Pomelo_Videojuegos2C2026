using System.Collections;
using UnityEngine;

// Pasa un dibujo por una lista de frames fundiendo cada uno con el siguiente, asi un giro de pocos
// frames no queda "a los saltos". Lo usan la salida del portal de entrada y la entrada al portal final.
//  - el giro entero tiene UNA sola curva suave (arranca despacio, acelera y frena al final): no se
//    detiene en cada frame intermedio, que era lo que lo hacia ver entrecortado.
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

        int tramos = frames.Length - 1;

        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / duracion) * tramos;   // por donde va el giro, de 0 a "tramos"
            int i = Mathf.Min(Mathf.FloorToInt(p), tramos - 1);
            dibujo.sprite = frames[i];
            encima.sprite = frames[i + 1];
            SetAlfa(encima, dibujo.color.a * (p - i));
            yield return null;
        }

        dibujo.sprite = frames[tramos];
        Object.Destroy(go);
    }

    // Deja una copia del dibujo actual encima y la va desvaneciendo, asi el cambio al dibujo que viene
    // (por ejemplo cuando el Animator vuelve a mandar y pone la pose de perfil) no es de golpe.
    // Hay que llamarla justo antes de cambiar el dibujo.
    public static IEnumerator FundirHaciaLoQueSigue(SpriteRenderer dibujo, float duracion)
    {
        if (dibujo == null || dibujo.sprite == null || duracion <= 0f) yield break;

        var go = new GameObject("Giro (fundido final)");
        go.transform.SetParent(dibujo.transform, false);
        var encima = go.AddComponent<SpriteRenderer>();
        encima.sprite = dibujo.sprite;
        encima.sharedMaterial = dibujo.sharedMaterial;
        encima.sortingLayerID = dibujo.sortingLayerID;
        encima.sortingOrder = dibujo.sortingOrder + 1;
        encima.flipX = dibujo.flipX;
        encima.maskInteraction = dibujo.maskInteraction;

        float alfa = dibujo.color.a;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            SetAlfa(encima, alfa * (1f - Mathf.SmoothStep(0f, 1f, t / duracion)));
            yield return null;
        }
        Object.Destroy(go);
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        var c = sr.color; c.a = a; sr.color = c;
    }
}
