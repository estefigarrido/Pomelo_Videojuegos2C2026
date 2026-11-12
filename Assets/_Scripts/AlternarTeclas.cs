using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Para los carteles de tutorial y el menu de pausa, que muestran dos formas de hacer lo mismo
// (por ejemplo flechas o A / D, o mouse o X):
// muestra un grupo de iconos, a los "intervalo" segundos se funde al otro grupo, y despues vuelve, sin parar.
// Los dos grupos estan en el mismo lugar, uno encima del otro.
// Lo arma el menu  Pomelo -> Carteles -> Armar carteles  (Assets/Editor/ArmarCarteles.cs).
public class AlternarTeclas : MonoBehaviour
{
    [Tooltip("Iconos que se ven primero (por ejemplo las flechas).")]
    [SerializeField] private Image[] primero;
    [Tooltip("Iconos que se ven despues (por ejemplo A y D).")]
    [SerializeField] private Image[] segundo;
    [Tooltip("Segundos que se ve cada grupo antes de cambiar al otro.")]
    [SerializeField] private float intervalo = 2.5f;
    [Tooltip("Segundos que tarda en fundirse de un grupo al otro.")]
    [SerializeField] private float duracionFundido = 0.5f;

    private void OnEnable()
    {
        Mostrar(1f);
        StartCoroutine(Alternar());
    }

    private IEnumerator Alternar()
    {
        float desde = 1f;   // 1 = se ve "primero", 0 = se ve "segundo"
        while (true)
        {
            // tiempo real: sigue andando con el juego en pausa (menu de pausa)
            yield return new WaitForSecondsRealtime(intervalo);
            float hasta = 1f - desde;
            for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
            {
                Mostrar(Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracionFundido)));
                yield return null;
            }
            Mostrar(hasta);
            desde = hasta;
        }
    }

    private void Mostrar(float cuantoPrimero)
    {
        SetAlfa(primero, cuantoPrimero);
        SetAlfa(segundo, 1f - cuantoPrimero);
    }

    private static void SetAlfa(Image[] imagenes, float a)
    {
        if (imagenes == null) return;
        foreach (var img in imagenes)
        {
            if (img == null) continue;
            var c = img.color; c.a = a; img.color = c;
        }
    }
}
