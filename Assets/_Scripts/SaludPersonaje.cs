using UnityEngine;
using UnityEngine.InputSystem;

public class SaludPersonaje : MonoBehaviour
{
    [SerializeField] private float vidaMaxima = 100f;
    [Tooltip("Donde reaparece al morir. Si queda vacio, reaparece donde empezo el nivel.")]
    [SerializeField] private Transform puntoDeReaparicion;

    [Header("Sabiduria (el liquido del circulo del HUD)")]
    [Tooltip("Liquido que entra en el circulo. Se junta pegandole a los enemigos: cada golpe suma lo que ese " +
             "enemigo le saca a ella (sabiduriaPorGolpe en VidaEnemigo).")]
    [SerializeField] private float sabiduriaMaxima = 100f;

    [Header("Regeneracion (gasta sabiduria)")]
    [Tooltip("Tecla provisoria para regenerar una vida (la definitiva se elige despues).")]
    [SerializeField] private Key teclaRegenerar = Key.R;
    [Tooltip("Vida que recupera cada vez (20 = una de las 5 vidas del HUD).")]
    [SerializeField] private float regeneracion = 20f;
    [Tooltip("Sabiduria que gasta cada vez (20 = 20% del circulo).")]
    [SerializeField] private float costoRegeneracion = 20f;

    [Header("Parpadeo al recibir un golpe")]
    [SerializeField] private ParpadeoDanio parpadeo = new ParpadeoDanio();

    [Header("Vibracion del joystick al recibir un golpe")]
    [Range(0f, 1f)] [SerializeField] private float vibracionGrave = 0.45f;
    [Range(0f, 1f)] [SerializeField] private float vibracionAguda = 0.25f;
    [SerializeField] private float duracionVibracion = 0.18f;

    [Header("Muerte con aviso (un pincho la levanto)")]
    [Tooltip("Segundos de flashes rojos (en la chica y una vineta en la pantalla) antes de morir.")]
    [SerializeField] private float segundosAvisoMuerte = 4f;
    [SerializeField] private float flashesPorSegundo = 3f;
    [Tooltip("Rojo oscuro, bordo.")]
    [SerializeField] private Color colorAviso = new Color(0.45f, 0.03f, 0.09f, 1f);
    [Tooltip("Opacidad maxima de la vineta roja en los bordes de la pantalla.")]
    [SerializeField] private float alfaVineta = 0.3f;
    [Tooltip("Ancho de la vineta desde cada borde (0.05 = 5% de la pantalla).")]
    [SerializeField] private float anchoVineta = 0.05f;

    private float vida;
    private float sabiduria;
    private bool sentenciada;
    public bool Sentenciada => sentenciada;
    private Vector3 puntoGuardado;
    private Rigidbody2D rb;

    public float Vida => vida;
    public float VidaMaxima => vidaMaxima;
    public float Sabiduria => sabiduria;
    public float SabiduriaMaxima => sabiduriaMaxima;

    // Avisa cuando muere, antes de reaparecer (la pelea con Umbrae lo usa).
    public event System.Action AlMorir;
    // Avisa despues de que reaparece, sea por morir o por caer al agua venenosa (las hojas de loto lo usan).
    public static event System.Action Reaparecio;
    private Vector3? reaparicionUnaVez;

    // Solo para la proxima muerte: reaparece aca en vez del ultimo checkpoint.
    public void ReaparecerUnaVezEn(Vector3 posicion) => reaparicionUnaVez = posicion;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        puntoGuardado = transform.position;
        vida = vidaMaxima;
        parpadeo.Iniciar(GetComponent<SpriteRenderer>());
    }

    private void OnEnable() => VidaEnemigo.Golpeado += SumarSabiduria;
    private void OnDisable() => VidaEnemigo.Golpeado -= SumarSabiduria;

    private void Update()
    {
        parpadeo.Actualizar();

        var teclado = Keyboard.current;
        if (!MenuPausa.BloqueaControles &&
            ((teclado != null && teclado[teclaRegenerar].wasPressedThisFrame) || Mando.RecargarVida)) Regenerar();
    }

    public void SumarSabiduria(float cantidad)
    {
        if (cantidad <= 0f || sabiduria >= sabiduriaMaxima) return;

        sabiduria = Mathf.Min(sabiduriaMaxima, sabiduria + cantidad);
        Debug.Log("[Sabiduria] +" + cantidad + " -> " + sabiduria + "/" + sabiduriaMaxima);
    }

    // Una vida por vez, y solo si le falta vida y le alcanza la sabiduria.
    private void Regenerar()
    {
        if (vida <= 0f || vida >= vidaMaxima || sabiduria < costoRegeneracion) return;

        sabiduria -= costoRegeneracion;
        vida = Mathf.Min(vidaMaxima, vida + regeneracion);
        Debug.Log("[Vida] +" + regeneracion + " (gasta " + costoRegeneracion + " de sabiduria) -> " + vida + "/" + vidaMaxima);
    }

    // vibrar: los golpes de los mobs hacen vibrar el joystick; los pinchos pasan false
    public void RecibirDanio(float cantidad, bool vibrar = true)
    {
        if (vida <= 0f || sentenciada) return;

        vida = Mathf.Max(0f, vida - cantidad);
        Debug.Log("[Vida] -" + cantidad + " -> " + vida + "/" + vidaMaxima);

        // el joystick vibra un poco, como en los juegos: un golpe seco (motor grave) con algo de zumbido,
        // mas fuerte si con ese golpe se muere
        if (vibrar)
        {
            if (vida <= 0f) Mando.Vibrar(0.8f, 0.6f, 0.45f);
            else Mando.Vibrar(vibracionGrave, vibracionAguda, duracionVibracion);
        }

        // solo cambia el color: no frena el movimiento ni los ataques
        parpadeo.Empezar();

        if (vida <= 0f) Morir();
    }

    // Cae al agua venenosa: pierde 'danio' de vida (una vida = 20) y reaparece en 'lugar'.
    // Si con eso se queda sin vida, muere como siempre (reaparece en el ultimo checkpoint con la vida llena).
    public void CaerAlAguaVenenosa(float danio, Vector3 lugar)
    {
        if (vida <= 0f || sentenciada) return;

        vida = Mathf.Max(0f, vida - danio);
        Debug.Log("[Vida] cayo al agua venenosa: -" + danio + " -> " + vida + "/" + vidaMaxima);
        if (vida <= 0f) { Morir(); return; }

        parpadeo.Empezar();
        transform.position = lugar;
        if (rb != null)
        {
            rb.position = lugar;
            rb.linearVelocity = Vector2.zero;
        }
        Reaparecio?.Invoke();
    }

    public void DefinirPuntoDeReaparicion(Transform punto)
    {
        puntoDeReaparicion = punto;
    }

    // guarda una posicion fija (el checkpoint se mueve y despues desaparece)
    public void DefinirPuntoDeReaparicion(Vector3 posicion)
    {
        puntoDeReaparicion = null;
        puntoGuardado = posicion;
        Debug.Log("[Vida] nuevo punto de reaparicion: " + posicion);
    }

    // Un pincho la levanto: queda quieta donde esta, parpadea en rojo junto con una vineta roja en los bordes
    // de la pantalla durante 'segundosAvisoMuerte' (para que se entienda que va a morir) y despues muere.
    public void MorirConAviso()
    {
        if (sentenciada || vida <= 0f) return;
        StartCoroutine(AvisoMuerte());
    }

    private System.Collections.IEnumerator AvisoMuerte()
    {
        sentenciada = true;
        Debug.Log("[Vida] la levanto un pincho: muere en " + segundosAvisoMuerte + " s");
        parpadeo.Cortar();
        var dibujo = GetComponent<SpriteRenderer>();
        Color original = dibujo != null ? dibujo.color : Color.white;
        var mov = GetComponent<MovimientoPersonaje>();
        bool movActivo = mov != null && mov.enabled;
        if (mov != null) mov.enabled = false;
        bool simulado = rb != null && rb.simulated;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        Mando.Vibrar(0.5f, 0.4f, 0.3f);

        var vineta = CrearVineta();
        for (float t = 0f; t < segundosAvisoMuerte; t += Time.deltaTime)
        {
            // cada flash: sube rapido a rojo y se apaga; cada vez un poco mas seguido hacia el final
            float fase = t * flashesPorSegundo * Mathf.Lerp(1f, 1.6f, t / segundosAvisoMuerte);
            float k = Mathf.Pow(0.5f + 0.5f * Mathf.Cos((fase % 1f) * Mathf.PI * 2f), 2f);
            if (dibujo != null) { var c = Color.Lerp(original, colorAviso, k); c.a = original.a; dibujo.color = c; }
            if (vineta != null) vineta.color = new Color(colorAviso.r, colorAviso.g, colorAviso.b, alfaVineta * (0.35f + 0.65f * k));
            yield return null;
        }
        if (vineta != null) { Destroy(vineta.texture); Destroy(vineta.canvas.gameObject); }
        if (dibujo != null) dibujo.color = original;
        if (rb != null) rb.simulated = simulado;
        if (mov != null) mov.enabled = movActivo;
        sentenciada = false;
        vida = 0f;
        Mando.Vibrar(0.8f, 0.6f, 0.45f);
        Morir();
    }

    // vineta roja: una franja finita pegada a los bordes de la pantalla (anchoVineta), por encima de todo
    private UnityEngine.UI.RawImage CrearVineta()
    {
        var go = new GameObject("vineta muerte");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var img = new GameObject("vineta").AddComponent<UnityEngine.UI.RawImage>();
        img.transform.SetParent(go.transform, false);
        img.raycastTarget = false;
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        const int n = 512;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));   // distancia al borde mas cercano
                float a = 1f - Mathf.SmoothStep(0f, 1f, d / Mathf.Max(0.001f, anchoVineta));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px); tex.Apply();
        img.texture = tex;
        img.color = Color.clear;
        return img;
    }

    private void Morir()
    {
        AlMorir?.Invoke();
        Vector3 destino = reaparicionUnaVez ?? (puntoDeReaparicion != null ? puntoDeReaparicion.position : puntoGuardado);
        reaparicionUnaVez = null;
        Debug.Log("[Vida] murio, reaparece en " + destino);

        transform.position = destino;
        if (rb != null)
        {
            rb.position = destino;
            rb.linearVelocity = Vector2.zero;
        }
        vida = vidaMaxima;
        Reaparecio?.Invoke();
    }
}
