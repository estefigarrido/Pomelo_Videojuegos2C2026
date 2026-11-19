using UnityEngine;

// El "Lore" que sale de la vasija flotante (pantalla de inicio, parte 2):
//   1) cuando la chica llega a 'dondeSeActiva' (en x), de la boca de la vasija sale una bolita de luz;
//   2) la bolita sube haciendo una curva suave hasta 'destino';
//   3) ahi se abre: la bolita se agranda y se apaga mientras aparece el emblema de Lore (Assets/_Entorno/_Items/
//      Lore.png) con una luz suave detras, que queda flotando y latiendo despacio.
// Los tres puntos son objetos hijos que se pueden mover en la escena. Usa el tiempo del juego (en pausa se frena).
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción
public class LoreDeLaVasija : MonoBehaviour
{
    [Header("Puntos (se pueden mover en la escena)")]
    [Tooltip("Cuando la chica pasa esta x, sale la bolita.")]
    [SerializeField] private Transform dondeSeActiva;
    [Tooltip("De donde sale la bolita (la boca de la vasija; flota con ella).")]
    [SerializeField] private Transform boca;
    [Tooltip("Donde aparece el Lore.")]
    [SerializeField] private Transform destino;

    [Header("Dibujos")]
    [SerializeField] private Sprite lore;
    [Tooltip("Alto del emblema de Lore en el mundo (unidades).")]
    [SerializeField] private float altoLore = 1.3f;
    [Tooltip("Tamano de la bolita (unidades).")]
    [SerializeField] private float tamanoBolita = 0.75f;   // (Caro: un poco mas grande; antes 0.45)
    [SerializeField] private Color colorLuz = new Color32(0xee, 0xf4, 0xe2, 255);
    [SerializeField] private int ordenDibujo = 0;

    [Header("Dialogo")]
    [Tooltip("El Dialogo del Lore (arranca apagado). Cuando aparece el Lore se prende y se pone sobre el: muestra el " +
             "cartel 'Interactuar' y, al apretar E (o L1), abre el cuadro de dialogo. La chica queda quieta desde que " +
             "llega hasta que se cierra el dialogo.")]
    [SerializeField] private Dialogo dialogo;
    [Tooltip("Cartel de tutorial que aparece cuando se cierra el dialogo (el de Saltar).")]
    [SerializeField] private CartelTutorial cartelAlTerminar;    [Tooltip("Tamano del cartel 'Interactuar' con respecto al del nivel (aca la chica esta a 1.4).")]
    [SerializeField] private float escalaCartel = 1.4f;

    [Header("Tiempos")]
    [SerializeField] private float duracionSalida = 1.8f;
    [SerializeField] private float duracionAparecer = 0.7f;
    [Tooltip("Cuanto se curva el camino de la bolita (unidades).")]
    [SerializeField] private float curva = 0.8f;

    private enum Estado { Esperando, Saliendo, Apareciendo, Quieto }
    private enum Charla { Nada, Esperando, Abierto, Terminado }
    private Charla charla = Charla.Nada;
    private bool cartelAgrandado;

    private void Awake()
    {
        // el dialogo no se puede usar hasta que aparece el Lore
        if (dialogo != null) dialogo.enabled = false;
    }
    private Estado estado = Estado.Esperando;
    private Transform chica;
    private Transform bolita, emblema, luz;
    private SpriteRenderer srBolita, srEmblema, srLuz;
    private Vector3 desde;
    private float t, tiempo;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;

        var punto = Punto();
        bolita = Crear("Bolita", punto, ordenDibujo + 2, out srBolita);
        luz = Crear("Luz del lore", punto, ordenDibujo, out srLuz);
        emblema = Crear("Lore", lore, ordenDibujo + 1, out srEmblema);
        // todo arranca apagado, chico y transparente (nunca se ve un cuadro con el emblema a su tamano original)
        Ubicar(destino != null ? destino.position : transform.position, 0f);
        bolita.localScale = Vector3.zero;
        srBolita.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, 0f);
        bolita.gameObject.SetActive(false);
        luz.gameObject.SetActive(false);
        emblema.gameObject.SetActive(false);
    }

    private Transform Crear(string nombre, Sprite s, int orden, out SpriteRenderer sr)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = orden;
        return go.transform;
    }

    private void Update()
    {
        float dt = Time.deltaTime;   // con la pausa (timeScale 0) no avanza
        tiempo += dt;
        switch (estado)
        {
            case Estado.Esperando:
                if (chica != null && dondeSeActiva != null && chica.position.x >= dondeSeActiva.position.x)
                {
                    // la chica se queda quieta (en reposo) hasta que termina el dialogo
                    QuietaPorEscena.Frenar();
                    estado = Estado.Saliendo;
                    t = 0f;
                    desde = boca != null ? boca.position : transform.position;
                    bolita.gameObject.SetActive(true);
                }
                break;

            case Estado.Saliendo:
            {
                t += dt / Mathf.Max(0.01f, duracionSalida);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 hasta = destino != null ? destino.position : desde + Vector3.up * 2f;
                // curva: sube primero y despues va hacia el destino, con un vaiven chiquito
                Vector3 medio = (desde + hasta) / 2f + Vector3.up * curva;
                Vector3 p = Vector3.Lerp(Vector3.Lerp(desde, medio, k), Vector3.Lerp(medio, hasta, k), k);
                p.x += Mathf.Sin(t * 9f) * 0.05f * (1f - k);
                bolita.position = p;
                float aparece = Mathf.Clamp01(t * 4f);   // sale chiquita de la vasija y crece rapido
                bolita.localScale = Vector3.one * tamanoBolita * aparece * (1f + 0.12f * Mathf.Sin(tiempo * 8f));
                srBolita.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, aparece);
                if (t >= 1f)
                {
                    estado = Estado.Apareciendo;
                    t = 0f;
                    // se ubica (chico y transparente) ANTES de prenderse: si no, el primer cuadro se veia el emblema
                    // a su tamano original, gigante
                    Ubicar(destino != null ? destino.position : bolita.position, 0f);
                    emblema.gameObject.SetActive(true);
                    luz.gameObject.SetActive(true);
                }
                break;
            }

            case Estado.Apareciendo:
            {
                t += dt / Mathf.Max(0.01f, duracionAparecer);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                Vector3 lugar = destino != null ? destino.position : bolita.position;
                // la bolita se abre (crece y se apaga) y aparece el emblema con su luz
                bolita.position = lugar;
                bolita.localScale = Vector3.one * tamanoBolita * (1f + 2.5f * k);
                srBolita.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, 1f - k);
                Ubicar(lugar, k);
                if (t >= 1f) { estado = Estado.Quieto; bolita.gameObject.SetActive(false); PrenderDialogo(); }
                break;
            }

            case Estado.Quieto:
                Ubicar((destino != null ? destino.position : emblema.position) + Vector3.up * (Mathf.Sin(tiempo * 1.6f) * 0.06f), 1f);
                break;
        }

        // el dialogo: el cartel 'Interactuar' esta sobre el Lore; al apretar E (o L1) se abre el cuadro (lo hace el
        // Dialogo) y, cuando se cierra, la chica se puede mover
        switch (charla)
        {
            case Charla.Esperando:
                AgrandarCartel();
                if (CuadroDialogo.Abierto) charla = Charla.Abierto;
                break;
            case Charla.Abierto:
                if (!CuadroDialogo.Abierto)
                {
                    charla = Charla.Terminado;
                    QuietaPorEscena.Soltar();
                    if (cartelAlTerminar != null) cartelAlTerminar.Mostrar();   // aparece el cartel de Saltar
                }
                break;
        }
    }

    private void PrenderDialogo()
    {
        if (dialogo == null) { charla = Charla.Terminado; QuietaPorEscena.Soltar(); return; }
        // el dialogo se pone sobre el Lore (su cartel 'Interactuar' aparece arriba del emblema) y se prende
        if (destino != null) dialogo.transform.position = destino.position;
        dialogo.enabled = true;
        charla = Charla.Esperando;
    }

    // el cartel 'Interactuar' lo crea el Dialogo del tamano del nivel; aca va proporcional a la chica (1.4)
    private void AgrandarCartel()
    {
        if (cartelAgrandado || dialogo == null) return;
        var cartel = dialogo.GetComponentInChildren<CartelTutorial>(true);
        if (cartel == null) return;
        cartel.transform.localScale *= escalaCartel;
        cartelAgrandado = true;
    }

    private void OnDestroy()
    {
        if (charla != Charla.Terminado && estado != Estado.Esperando) QuietaPorEscena.Soltar();
    }

    // el emblema (a su alto) y la luz detras, que late despacio
    private void Ubicar(Vector3 lugar, float k)
    {
        float escala = lore != null && lore.bounds.size.y > 0f ? altoLore / lore.bounds.size.y : 1f;
        emblema.position = lugar;
        emblema.localScale = Vector3.one * escala * Mathf.Lerp(0.6f, 1f, k);
        srEmblema.color = new Color(1f, 1f, 1f, k);
        luz.position = lugar;
        luz.localScale = Vector3.one * altoLore * 2.2f * (1f + 0.05f * Mathf.Sin(tiempo * 2f));
        srLuz.color = new Color(colorLuz.r, colorLuz.g, colorLuz.b, k * (0.55f + 0.15f * Mathf.Sin(tiempo * 2f)));
    }

    // una bolita de luz (centro claro y halo que se apaga), dibujada al arrancar
    private static Sprite punto;
    private static Sprite Punto()
    {
        if (punto != null) return punto;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float centro = Mathf.Clamp01((0.22f - d) / 0.06f);
                float halo = Mathf.Exp(-d * d * 7f) * (1f - Mathf.Clamp01((d - 0.85f) / 0.15f));
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(centro + halo * 0.8f));
            }
        tex.SetPixels(px);
        tex.Apply();
        punto = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 unidad de lado
        return punto;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        if (dondeSeActiva != null)
            Gizmos.DrawLine(dondeSeActiva.position + Vector3.down * 2f, dondeSeActiva.position + Vector3.up * 3f);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        if (destino != null) Gizmos.DrawWireSphere(destino.position, 0.3f);
        if (boca != null) Gizmos.DrawWireSphere(boca.position, 0.15f);
    }
}
