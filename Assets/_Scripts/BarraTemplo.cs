using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Barra del templo (HUD, Barra 4 del Figma): el relleno amarillo de la barra curva y el boton dorado del
// templo. El polen que junta la chica la va cargando (Sumar) hasta 100 %. Con click derecho se restaura
// un sector del templo (RestauracionTemplo) y se gasta 'costoRestaurar' (10 %): 10 sectores por barra
// llena, 30 sectores = 3 barras.
// Cuando alcanza para restaurar, el boton late; si no alcanza, tiembla.
public class BarraTemplo : MonoBehaviour
{
    [SerializeField] private ArcoUI relleno;
    [Tooltip("El disco dorado y el icono del templo (dentro del circulo de la izquierda de la barra).")]
    [SerializeField] private RectTransform boton;
    [SerializeField] private Image brilloBoton;

    [Header("Carga")]
    [Range(0f, 100f)] [SerializeField] private float carga = 0f;
    [SerializeField] private float costoRestaurar = 10f;
    [Tooltip("Lo maximo que se guarda. Con la barra llena el polen no se absorbe: queda flotando hasta que se libere lugar.")]
    [SerializeField] private float cargaMaxima = 100f;
    [Tooltip("Velocidad con la que el relleno alcanza a la carga (porcentaje por segundo).")]
    [SerializeField] private float velocidadRelleno = 60f;

    [Header("Animaciones")]
    [SerializeField] private Color colorRelleno = new Color(1f, 0.776f, 0.169f, 1f);
    [SerializeField] private Color colorDestello = new Color(1f, 0.97f, 0.75f, 1f);

    public static BarraTemplo Instancia { get; private set; }
    public float Carga => carga;

    private float mostrado;
    private float ultimoSumado = -99f, ultimoUso = -99f, ultimoNo = -99f;
    private Vector3 escalaBoton = Vector3.one;
    private Vector2 posBoton;

    private void Awake()
    {
        Instancia = this;
        mostrado = carga;
        if (boton != null) { escalaBoton = boton.localScale; posBoton = boton.anchoredPosition; }
    }

    // lugar que le queda a la barra (el polen solo vuela si entra)
    public float Libre => Mathf.Max(0f, cargaMaxima - carga);

    // suma hasta llenar la barra y devuelve cuanto entro
    public float Sumar(float porcentaje)
    {
        float entra = Mathf.Min(porcentaje, Libre);
        carga += entra;
        if (entra > 0f) ultimoSumado = Time.time;
        return entra;
    }

    // punto de la pantalla adonde vuela el polen (la punta del relleno)
    public Vector2 PuntoEnPantalla()
    {
        if (relleno == null) return new Vector2(Screen.width, Screen.height);
        var canvas = relleno.canvas;
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 mundo = relleno.rectTransform.TransformPoint(relleno.PuntaLocal(mostrado / 100f));
        return RectTransformUtility.WorldToScreenPoint(cam, mundo);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (!MenuPausa.BloqueaControles &&
            ((mouse != null && mouse.rightButton.wasPressedThisFrame) || Mando.RepararTemplo)) Restaurar();

        mostrado = Mathf.MoveTowards(mostrado, Mathf.Min(100f, carga), velocidadRelleno * Time.deltaTime);
        if (relleno != null)
        {
            relleno.Relleno = mostrado / 100f;
            // destello corto cada vez que entra polen
            float k = Mathf.Clamp01(1f - (Time.time - ultimoSumado) / 0.3f);
            relleno.color = Color.Lerp(colorRelleno, colorDestello, k * 0.8f);
        }
        AnimarBoton();
    }

    private void Restaurar()
    {
        var templo = RestauracionTemplo.Instancia;
        if (carga + 0.001f < costoRestaurar || templo == null || !templo.Restaurar())
        {
            ultimoNo = Time.time;
            return;
        }
        carga -= costoRestaurar;
        ultimoUso = Time.time;
    }

    private void AnimarBoton()
    {
        if (boton == null) return;
        var templo = RestauracionTemplo.Instancia;
        bool alcanza = carga + 0.001f >= costoRestaurar && templo != null && templo.QuedanSectores;
        // mas fuerte si la chica esta parada en una parte del templo que todavia no se arreglo
        bool fuerte = alcanza && templo.ChicaEnSectorSinRestaurar;
        float t = Time.time;
        // titila como una llama: dos ondas y ruido, mas rapido y mas intenso cuando es "fuerte"
        float vel = fuerte ? 2.2f : 1f;
        float ruido = Mathf.PerlinNoise(t * 6f * vel, 0.37f);
        float titila = Mathf.Clamp01(0.55f * ruido + 0.3f * (0.5f + 0.5f * Mathf.Sin(t * 9f * vel)) + 0.15f * (0.5f + 0.5f * Mathf.Sin(t * 23f * vel)));
        float latido = !alcanza ? 1f : fuerte ? 1f + 0.1f * titila : 1f + 0.03f * titila;
        float uso = Mathf.Clamp01(1f - (t - ultimoUso) / 0.35f);
        boton.localScale = escalaBoton * (latido + 0.3f * Mathf.Sin(uso * Mathf.PI));
        // no se puede: tiembla de costado
        float no = Mathf.Clamp01(1f - (t - ultimoNo) / 0.3f);
        boton.anchoredPosition = posBoton + Vector2.right * Mathf.Sin(t * 70f) * 6f * no;
        if (brilloBoton != null)
        {
            float a = !alcanza ? 0f : fuerte ? 0.55f + 0.45f * titila : 0.2f + 0.4f * titila;
            brilloBoton.color = new Color(1f, 0.85f, 0.35f, Mathf.Max(a, uso));
            brilloBoton.rectTransform.localScale = Vector3.one * (fuerte ? 1.15f + 0.15f * titila : 1f);
        }
    }
}
