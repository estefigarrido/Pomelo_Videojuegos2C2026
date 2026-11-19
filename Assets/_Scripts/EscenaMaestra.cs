using System.Collections;
using UnityEngine;

// Escena de la maestra (Maestra Li Hua) en Inicio_introduccion.
//  - Cuando la camara termina de alejarse (PantallaInicio: apenas la chica llega a la piedra que sigue al jarron), la
//    hojita del mural de botanica empieza a brillar verde clarito, con el destello encima, y queda asi hasta que se
//    sale del templo al Nivel 1.
//  - Cuando la chica se para sobre el bloque negro de adelante de la columna inclinada (justo antes de la cascadita),
//    se queda quieta y:
//     1. aparece la pantalla de la maestra (Assets/_Pantallas_info/dialogo_maestrabotanica) en 'segundosFundido',
//        de transparente a opaca, con un easing suave (como las pantallas del final);
//     2. 'esperaDialogo' segundos despues de estar entera se abre el dialogo (Dialogo + CuadroDialogo de Franco: se
//        pasa con la flecha derecha / D / cruceta, igual que el resto);
//     3. al cerrar el dialogo la pantalla se va con la misma transicion y la chica se puede mover.
// Pasa una sola vez. Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción.
public class EscenaMaestra : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    [Header("Piezas (las pone el menu que arma la escena)")]
    [Tooltip("La pantalla de la maestra (Image a pantalla completa en el Canvas, detras del cuadro de dialogo).")]
    [SerializeField] private CanvasGroup pantalla;
    [Tooltip("Las lineas de la maestra (el componente queda apagado: lo abre esta escena).")]
    [SerializeField] private Dialogo dialogo;
    [Tooltip("La hojita del mural de botanica, que brilla verde mientras tanto.")]
    [SerializeField] private SpriteRenderer hoja;

    [Header("Donde")]
    [Tooltip("La escena empieza cuando la chica cruza 'desdeX' (la linea roja del Figma sobre el bloque de antes de la " +
             "cascadita). 'hastaX' es el fin del bloque (solo de referencia).")]
    [SerializeField] private float desdeX;
    [SerializeField] private float hastaX;

    [Header("Ritmo")]
    [Tooltip("Segundos desde que la chica cruza la linea (ya quieta) hasta que empieza a aparecer la pantalla.")]
    [SerializeField] private float esperaAntes = 1f;
    [Tooltip("Segundos que tarda la pantalla en aparecer (y en irse).")]
    [SerializeField] private float segundosFundido = 2f;
    [Tooltip("Segundos con la pantalla entera antes de que aparezca el dialogo.")]
    [SerializeField] private float esperaDialogo = 1f;

    [Header("Brillo de la hojita")]
    [Tooltip("Verde clarito.")]
    [SerializeField] private Color colorBrillo = new Color(0.78f, 1f, 0.74f, 1f);
    [Tooltip("Destello encima de la hojita (Assets/_Entorno/_Items/destello_gema).")]
    [SerializeField] private Sprite destello;
    [Tooltip("La imagen del destello es muy tenue: 1 = entera. Hay ademas una segunda copia girada (rayos mas densos).")]
    [Range(0f, 1f)] [SerializeField] private float opacidadDestello = 1f;
    [Tooltip("Tamano del destello con respecto a la hojita.")]
    [SerializeField] private float tamanoDestello = 4.5f;
    [Tooltip("Que tan fuerte brilla (0 a 1): leve.")]
    [Range(0f, 1f)] [SerializeField] private float intensidadBrillo = 0.45f;
    [Tooltip("Tamano del halo con respecto a la hojita.")]
    [SerializeField] private float tamanoHalo = 2.2f;
    [Tooltip("Segundos que tarda en prenderse y apagarse.")]
    [SerializeField] private float segundosBrillo = 1.2f;

    private MovimientoPersonaje chica;
    private PantallaInicio pantallaInicio;
    private bool usada, enEscena;
    private SpriteRenderer halo, brilloDestello, brilloDestello2;
    private Color colorHoja;
    private float brillo;   // 0 a 1

    private void Start()
    {
        chica = FindFirstObjectByType<MovimientoPersonaje>();
        pantallaInicio = FindFirstObjectByType<PantallaInicio>();
        if (pantalla != null)
        {
            pantalla.alpha = 0f;
            pantalla.interactable = false;
            pantalla.blocksRaycasts = false;
        }
        if (hoja != null)
        {
            colorHoja = hoja.color;
            var go = new GameObject("Brillo de la hojita");
            go.transform.SetParent(hoja.transform, false);
            go.transform.position = hoja.bounds.center;
            halo = go.AddComponent<SpriteRenderer>();
            halo.sprite = Halo();
            halo.sortingLayerID = hoja.sortingLayerID;
            halo.sortingOrder = hoja.sortingOrder + 1;
            float lado = Mathf.Max(hoja.bounds.size.x, hoja.bounds.size.y) * tamanoHalo;
            Vector3 l = hoja.transform.lossyScale;
            go.transform.localScale = new Vector3(lado / Mathf.Max(0.0001f, Mathf.Abs(l.x)), lado / Mathf.Max(0.0001f, Mathf.Abs(l.y)), 1f);
            halo.color = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, 0f);

            // el destello, encima de la hojita y del halo
            if (destello != null)
            {
                var gd = new GameObject("Destello de la hojita");
                gd.transform.SetParent(hoja.transform, false);
                gd.transform.position = hoja.bounds.center;
                brilloDestello = gd.AddComponent<SpriteRenderer>();
                brilloDestello.sprite = destello;
                brilloDestello.sortingLayerID = hoja.sortingLayerID;
                brilloDestello.sortingOrder = hoja.sortingOrder + 2;
                float ladoD = Mathf.Max(hoja.bounds.size.x, hoja.bounds.size.y) * tamanoDestello;
                float s = ladoD / Mathf.Max(0.0001f, Mathf.Max(destello.bounds.size.x, destello.bounds.size.y));
                gd.transform.localScale = new Vector3(s / Mathf.Max(0.0001f, Mathf.Abs(l.x)), s / Mathf.Max(0.0001f, Mathf.Abs(l.y)), 1f);
                brilloDestello.color = new Color(1f, 1f, 1f, 0f);
                // segunda copia, girada medio rayo y un poco mas chica: los rayos se ven mas y mas parejos
                var gd2 = Instantiate(gd, hoja.transform);
                gd2.name = "Destello de la hojita 2";
                gd2.transform.position = gd.transform.position;
                gd2.transform.localScale = gd.transform.localScale * 0.8f;
                gd2.transform.Rotate(0f, 0f, 7.5f);
                brilloDestello2 = gd2.GetComponent<SpriteRenderer>();
                brilloDestello2.sortingOrder = hoja.sortingOrder + 3;
            }
        }
    }

    private void Update()
    {
        Brillar();

        // mientras dura la escena la chica no se mueve (aunque otra cosa la haya soltado)
        if (enEscena && !QuietaPorEscena.Activa) QuietaPorEscena.Frenar();
        if (usada || chica == null || dialogo == null) return;
        if (CuadroDialogo.Abierto || QuietaPorEscena.Activa || MenuPausa.BloqueaControles) return;
        // pasa al cruzar la linea marcada en el Figma ('desdeX'), pisando o en el aire
        if (chica.transform.position.x < desdeX) return;
        usada = true;
        StartCoroutine(Escena());
    }

    // la hojita brilla desde que la camara termino de alejarse y queda brillando hasta que se sale del templo al
    // Nivel 1 (al cambiar de escena se va con ella)
    private void Brillar()
    {
        if (hoja == null || halo == null) return;
        bool prendida = pantallaInicio != null && pantallaInicio.TerminoDeAlejarse;
        brillo = Mathf.MoveTowards(brillo, prendida ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, segundosBrillo));
        float k = Mathf.SmoothStep(0f, 1f, brillo) * intensidadBrillo;
        float latido = 0.8f + 0.2f * Mathf.Sin(Time.time * 2.2f);
        halo.color = new Color(colorBrillo.r, colorBrillo.g, colorBrillo.b, k * latido);
        hoja.color = Color.Lerp(colorHoja, colorHoja * colorBrillo, k * 0.6f);
        if (brilloDestello != null)
        {
            float b = Mathf.SmoothStep(0f, 1f, brillo);
            brilloDestello.color = new Color(1f, 1f, 1f, opacidadDestello * b * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.2f)));
            brilloDestello.transform.Rotate(0f, 0f, -8f * Time.deltaTime);   // gira muy despacio
            if (brilloDestello2 != null)
            {
                brilloDestello2.color = new Color(1f, 1f, 1f, opacidadDestello * b * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.2f + 1.5f)));
                brilloDestello2.transform.Rotate(0f, 0f, 6f * Time.deltaTime);   // para el otro lado
            }
        }
    }

    private IEnumerator Escena()
    {
        if (mostrarDebug) Debug.Log("[Maestra] la chica llego: aparece la pantalla");
        enEscena = true;
        QuietaPorEscena.Frenar();
        yield return new WaitForSeconds(esperaAntes);   // (Estefi, 19/11) la pantalla aparece 1 s despues de cruzar
        if (pantalla != null) yield return Fundir(pantalla, 1f, segundosFundido);
        yield return new WaitForSeconds(esperaDialogo);

        if (CuadroDialogo.Instancia != null && CuadroDialogo.Instancia.Abrir(dialogo))
        {
            if (mostrarDebug) Debug.Log("[Maestra] dialogo abierto");
            // Abierto sigue en true hasta que se suelta la tecla con que se cerro (asi no camina sola)
            while (CuadroDialogo.Abierto) yield return null;
        }
        else if (mostrarDebug) Debug.Log("[Maestra] no hay cuadro de dialogo en la escena");

        if (pantalla != null) yield return Fundir(pantalla, 0f, segundosFundido);
        enEscena = false;
        QuietaPorEscena.Soltar();
        if (mostrarDebug) Debug.Log("[Maestra] termino");
    }

    // easing leve (seno de entrada y salida): arranca y termina suave, sin ser abrupto. Se frena con la pausa.
    private static IEnumerator Fundir(CanvasGroup g, float hasta, float tiempo)
    {
        float desde = g.alpha;
        for (float t = 0f; t < tiempo; t += Time.deltaTime)
        {
            float k = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * t / tiempo);
            g.alpha = Mathf.Lerp(desde, hasta, k);
            yield return null;
        }
        g.alpha = hasta;
    }

    // un halo redondo que se apaga hacia afuera (1 unidad de lado), dibujado al arrancar
    private static Sprite halo_;
    private static Sprite Halo()
    {
        if (halo_ != null) return halo_;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float a = Mathf.Exp(-d * d * 4f) * (1f - Mathf.Clamp01((d - 0.8f) / 0.2f));
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px);
        tex.Apply();
        halo_ = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return halo_;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 1f, 0.7f, 0.9f);
        Gizmos.DrawLine(new Vector3(desdeX, transform.position.y - 3f), new Vector3(desdeX, transform.position.y + 6f));
        Gizmos.DrawLine(new Vector3(hastaX, transform.position.y - 3f), new Vector3(hastaX, transform.position.y + 6f));
    }
}
