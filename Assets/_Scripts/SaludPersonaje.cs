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
    [Tooltip("Imagenes que cubren toda la pantalla, por encima de todo, una despues de la otra (pantalla_danio01..03).")]
    [SerializeField] private Texture2D[] pantallaDanio;
    [Tooltip("Segundos de cada imagen. La muerte dura la suma (2 + 2 + 1 = 5 s).")]
    [SerializeField] private float[] segundosPantallaDanio = { 2f, 2f, 1f };
    [Header("Pantalla al reaparecer (despues de cualquier muerte)")]
    [Tooltip("Imagenes que cubren la pantalla al reaparecer, una despues de la otra (pantalla_respawn00..04).")]
    [SerializeField] private Texture2D[] pantallaRespawn;
    [Tooltip("Segundos que tardan en pasar todas (se reparten parejo).")]
    [SerializeField] private float segundosPantallaRespawn = 1f;
    [Tooltip("Opacidad de toda la animacion de respawn (0.3 = 30%).")]
    [SerializeField] private float opacidadRespawn = 0.3f;
    [Tooltip("Parte de cada imagen que se usa para aparecer y para irse (0.4 = 40% entra, 40% sale).")]
    [Range(0.05f, 0.5f)] [SerializeField] private float fundidoRespawn = 0.4f;
    [Tooltip("Segundos del fundido suave de una imagen a la siguiente.")]
    [SerializeField] private float segundosFundido = 0.35f;
    private UnityEngine.UI.RawImage pantallaAnterior;
    [Tooltip("Flashes rojos de la chica mientras dura.")]
    [SerializeField] private float flashesPorSegundo = 3f;
    [Tooltip("Rojo oscuro, bordo.")]
    [SerializeField] private Color colorAviso = new Color(0.45f, 0.03f, 0.09f, 1f);

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
        float segundosAvisoMuerte = 0f;
        foreach (var s in segundosPantallaDanio) segundosAvisoMuerte += s;
        if (segundosAvisoMuerte <= 0f) segundosAvisoMuerte = 5f;
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

        var pantalla = CrearPantallaDanio();
        for (float t = 0f; t < segundosAvisoMuerte; t += Time.deltaTime)
        {
            // cada flash: sube rapido a rojo y se apaga; cada vez un poco mas seguido hacia el final
            float fase = t * flashesPorSegundo * Mathf.Lerp(1f, 1.6f, t / segundosAvisoMuerte);
            float k = Mathf.Pow(0.5f + 0.5f * Mathf.Cos((fase % 1f) * Mathf.PI * 2f), 2f);
            if (dibujo != null) { var c = Color.Lerp(original, colorAviso, k); c.a = original.a; dibujo.color = c; }
            // la imagen de la pantalla que toca en este momento (2 s, 2 s y 1 s)
            if (pantalla != null) PintarPantallas(pantalla, pantallaAnterior, pantallaDanio, segundosPantallaDanio, t, 1f);
            yield return null;
        }
        if (pantalla != null) Destroy(pantalla.canvas.gameObject);
        if (dibujo != null) dibujo.color = original;
        if (rb != null) rb.simulated = simulado;
        if (mov != null) mov.enabled = movActivo;
        sentenciada = false;
        vida = 0f;
        Mando.Vibrar(0.8f, 0.6f, 0.45f);
        Morir();
    }

    // Pone en 'arriba' la imagen que toca a los 't' segundos (cada una dura lo que dice 'duraciones') y en
    // 'abajo' la anterior: al empezar cada una se funde sobre la anterior en 'segundosFundido' (la primera
    // aparece desde transparente). 'alfa' multiplica todo (para apagar la ultima al final).
    private void PintarPantallas(UnityEngine.UI.RawImage arriba, UnityEngine.UI.RawImage abajo, Texture2D[] imagenes, float[] duraciones, float t, float alfa)
    {
        if (imagenes == null || imagenes.Length == 0) return;
        int i = 0; float acumulado = 0f;
        while (i < imagenes.Length - 1 && i < duraciones.Length && t >= acumulado + duraciones[i]) { acumulado += duraciones[i]; i++; }
        var tex = imagenes[i];
        if (arriba.texture != tex) Debug.Log("[Vida] " + arriba.canvas.name + ": " + (tex != null ? tex.name : "-") + " a los " + t.ToString("0.00") + " s");
        float f = Mathf.SmoothStep(0f, 1f, (t - acumulado) / Mathf.Max(0.01f, segundosFundido));
        arriba.texture = tex;
        arriba.color = new Color(1f, 1f, 1f, f * alfa);
        abajo.texture = i > 0 ? imagenes[i - 1] : null;
        abajo.color = new Color(1f, 1f, 1f, i > 0 ? alfa : 0f);
    }

    // Al reaparecer: las imagenes de pantalla_respawn (00 a 04) pasan en 'segundosPantallaRespawn' en total,
    // fundiendose una sobre otra; la ultima se apaga suave al final.
    private System.Collections.IEnumerator PantallaRespawn()
    {
        if (pantallaRespawn == null || pantallaRespawn.Length == 0) yield break;
        var img = CrearCapaPantalla("pantalla respawn", out var abajo);
        abajo.color = Color.clear;
        float cada = segundosPantallaRespawn / pantallaRespawn.Length;
        int anterior = -1;
        for (float t = 0f; t < segundosPantallaRespawn; t += Time.deltaTime)
        {
            // cada imagen aparece desde transparente y se va a transparente antes de la siguiente: el cambio
            // de una pantalla a otra se nota como un latido de opacidad. Todo a 'opacidadRespawn'.
            int i = Mathf.Min(pantallaRespawn.Length - 1, Mathf.FloorToInt(t / cada));
            float u = (t - i * cada) / cada;
            float entra = Mathf.SmoothStep(0f, 1f, u / fundidoRespawn);
            float sale = Mathf.SmoothStep(0f, 1f, (1f - u) / fundidoRespawn);
            img.texture = pantallaRespawn[i];
            img.color = new Color(1f, 1f, 1f, opacidadRespawn * Mathf.Min(entra, sale));
            if (i != anterior) { Debug.Log("[Vida] pantalla respawn: " + pantallaRespawn[i].name + " a los " + t.ToString("0.00") + " s"); anterior = i; }
            yield return null;
        }
        Destroy(img.canvas.gameObject);
    }

    // las imagenes de pantalla_danio estiradas a toda la pantalla, por encima de todo (incluido el HUD)
    private UnityEngine.UI.RawImage CrearPantallaDanio()
    {
        if (pantallaDanio == null || pantallaDanio.Length == 0) return null;
        return CrearCapaPantalla("pantalla danio", out pantallaAnterior);
    }

    // un canvas por encima de todo con dos imagenes a pantalla completa: abajo la anterior, arriba la que se funde
    private UnityEngine.UI.RawImage CrearCapaPantalla(string nombre, out UnityEngine.UI.RawImage anterior)
    {
        var go = new GameObject(nombre);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        // abajo la imagen anterior, arriba la que se esta fundiendo
        anterior = new GameObject("imagen anterior").AddComponent<UnityEngine.UI.RawImage>();
        anterior.transform.SetParent(go.transform, false);
        anterior.raycastTarget = false;
        var ra = anterior.rectTransform; ra.anchorMin = Vector2.zero; ra.anchorMax = Vector2.one; ra.offsetMin = ra.offsetMax = Vector2.zero;
        anterior.color = Color.clear;
        var img = new GameObject("imagen").AddComponent<UnityEngine.UI.RawImage>();
        img.transform.SetParent(go.transform, false);
        img.color = Color.clear;
        img.raycastTarget = false;
        var r = img.rectTransform; r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        img.texture = null;
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
        if (pantallaRespawn != null && pantallaRespawn.Length > 0) StartCoroutine(PantallaRespawn());
    }
}
