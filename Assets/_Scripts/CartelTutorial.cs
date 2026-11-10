using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Cartelito de tutorial (por ejemplo "Atacar" con el mouse o el boton circulo) puesto en el mundo,
// al lado del portal de entrada. Es un Canvas en World Space (prefab en Assets/_UI/Carteles):
//  - se dibuja justo detras de la chica (orden de ella - 1), asi queda adelante del fondo,
//    el portal y las columnas, pero la chica pasa por delante.
//  - aparece suavemente apenas la chica termina de salir por la puerta (InicioNivel) y se queda ahi.
//    Si "esperarAOtroCartel" esta prendido, no aparece solo: lo muestra otro cartel (ver abajo).
//  - no se va solo con el tiempo: a medida que la chica le pasa por delante, se va borrando de
//    izquierda a derecha (lo que ella ya paso no vuelve). Cuando llega a la mitad del cartel, lo que
//    queda se borra solo (en "duracionBorrado" segundos) y desaparece.
//    Usa la RectMask2D "Mascara": le va subiendo el padding izquierdo.
//  - se puede encadenar con otros carteles: cuando este empieza a borrarse, desvanece los de
//    "desvanecerAlEmpezarABorrarme"; cuando le falta un cuarto por borrar ("mostrarCuandoFalte"),
//    muestra los de "mostrarAlTerminarDeBorrarme".
//  - si tiene una hoja de loto en "desvanecerAlPisarHoja", se desvanece cuando la chica la pisa.
// Los arma el menu  Pomelo -> Carteles -> Armar carteles  (Assets/Editor/ArmarCarteles.cs).
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public class CartelTutorial : MonoBehaviour
{
    [Tooltip("Segundos que tarda en aparecer (suave).")]
    [SerializeField] private float duracionAparicion = 1.2f;
    [Tooltip("Si esta prendido, espera a que la chica termine de salir por la puerta del portal.")]
    [SerializeField] private bool esperarSalidaDelPortal = true;

    [Header("Borrado al pasar la chica")]
    [Tooltip("Si esta apagado, no se borra cuando la chica le pasa por delante (por ejemplo el de Recargar vida, " +
             "que se va recien cuando ella pisa la hoja de loto).")]
    [SerializeField] private bool borrarAlPasar = true;
    [Tooltip("Mascara que tapa lo que la chica ya paso.")]
    [SerializeField] private RectMask2D mascara;
    [Tooltip("Ancho (en px del cartel) del borde difuminado del borrado.")]
    [SerializeField] private int bordeBorrado = 40;
    [Tooltip("Segundos que tarda en borrarse lo que queda del cartel, desde que la chica llega a la mitad.")]
    [SerializeField] private float duracionBorrado = 1f;

    [Header("Encadenar con otros carteles")]
    [Tooltip("No aparece al salir la chica del portal: espera a que otro cartel lo muestre.")]
    [SerializeField] private bool esperarAOtroCartel;
    [Tooltip("Carteles que se desvanecen cuando este empieza a borrarse.")]
    [SerializeField] private CartelTutorial[] desvanecerAlEmpezarABorrarme;
    [Tooltip("Carteles que aparecen cuando a este le falta poco para terminar de borrarse.")]
    [SerializeField] private CartelTutorial[] mostrarAlTerminarDeBorrarme;
    [Tooltip("Cuanto le tiene que faltar por borrar a este cartel para que aparezcan los de arriba " +
             "(0.25 = cuando falta un cuarto; 0 = cuando termina).")]
    [Range(0f, 1f)]
    [SerializeField] private float mostrarCuandoFalte = 0.25f;
    [Tooltip("Segundos que tarda en desvanecerse cuando otro cartel se lo pide.")]
    [SerializeField] private float duracionDesvanecido = 0.8f;
    [Tooltip("Si hay una hoja de loto aca, el cartel se desvanece cuando la chica la pisa.")]
    [SerializeField] private HojaLotoQueSeHunde desvanecerAlPisarHoja;

    private Canvas canvas;
    private CanvasGroup grupo;
    private SpriteRenderer dibujoChica;
    private float borrado;
    private bool salio;
    private float inicioBorrado = -1f;   // momento en que la chica llego a la mitad (-1 = todavia no)
    private float desdeBorrado;          // hasta donde estaba borrado en ese momento
    private bool visible;                // ya empezo a aparecer
    private bool estuvoALaIzquierda;     // la chica estuvo antes de la mitad del cartel desde que aparecio
    private bool empezoABorrarse;
    private bool mostroLosSiguientes;
    private bool yendose;                // se esta desvaneciendo porque otro cartel se lo pidio
    private float margen;                // px de la mascara a cada lado del contenido (ahi no hay nada)

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (mascara != null)
        {
            mascara.padding = Vector4.zero;
            mascara.softness = Vector2Int.zero;
            if (mascara.transform.childCount > 0)
            {
                var contenido = (RectTransform)mascara.transform.GetChild(0);
                margen = (mascara.rectTransform.rect.width - contenido.rect.width * contenido.localScale.x) / 2f;
            }
        }
    }

    private void OnEnable()
    {
        InicioNivel.SalidaTerminada += AlSalir;
        if (desvanecerAlPisarHoja != null) desvanecerAlPisarHoja.Pisada += Desvanecer;
    }

    private void OnDisable()
    {
        InicioNivel.SalidaTerminada -= AlSalir;
        if (desvanecerAlPisarHoja != null) desvanecerAlPisarHoja.Pisada -= Desvanecer;
    }
    private void AlSalir() { salio = true; }

    private IEnumerator Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        dibujoChica = salud != null ? salud.GetComponent<SpriteRenderer>() : null;

        // justo detras de la chica y adelante de todo lo que esta detras de ella
        if (dibujoChica != null)
        {
            canvas.sortingLayerID = dibujoChica.sortingLayerID;
            canvas.sortingOrder = dibujoChica.sortingOrder - 1;
        }

        // este lo muestra otro cartel (Mostrar)
        if (esperarAOtroCartel) yield break;

        if (esperarSalidaDelPortal && FindFirstObjectByType<InicioNivel>() != null)
            while (!salio && !InicioNivel.YaSalio) yield return null;

        if (!visible && !yendose) yield return Aparecer();
    }

    // Lo llama otro cartel cuando termina de borrarse.
    public void Mostrar()
    {
        if (visible || yendose || !gameObject.activeInHierarchy) return;
        StartCoroutine(Aparecer());
    }

    // Lo llama otro cartel cuando empieza a borrarse: se desvanece entero y desaparece.
    public void Desvanecer()
    {
        if (yendose || !gameObject.activeInHierarchy) return;
        yendose = true;
        StopAllCoroutines();
        StartCoroutine(Irse());
    }

    private IEnumerator Aparecer()
    {
        // aparece suave y se queda: solo se va cuando la chica le pasa por delante (LateUpdate)
        visible = true;
        yield return Fundido(grupo.alpha, 1f, duracionAparicion);
    }

    private IEnumerator Irse()
    {
        yield return Fundido(grupo.alpha, 0f, duracionDesvanecido);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!borrarAlPasar || mascara == null || dibujoChica == null || yendose) return;

        // donde esta la chica, medido desde el borde izquierdo de la mascara (en px del cartel)
        var rt = mascara.rectTransform;
        float ancho = rt.rect.width;
        float x = rt.InverseTransformPoint(dibujoChica.bounds.center).x - rt.rect.xMin;

        // recien se puede borrar cuando ya aparecio, y solo si la chica le pasa por delante de verdad:
        // tiene que haber estado antes de la mitad (a la izquierda). Si el cartel quedo detras de ella
        // (a su izquierda), no se borra solo por estar ahi.
        if (!visible) return;
        if (!estuvoALaIzquierda)
        {
            if (x >= ancho / 2f) return;
            estuvoALaIzquierda = true;
        }

        float nuevo;
        if (inicioBorrado < 0f)
        {
            // hasta la mitad del cartel: se borra lo que ella va pasando
            nuevo = Mathf.Max(borrado, x);
            if (x >= ancho / 2f) { inicioBorrado = Time.time; desdeBorrado = nuevo; }
        }
        else
        {
            // desde la mitad: lo que queda se borra solo, parejo, hasta el final
            // (si ella va mas rapido que el borrado, lo que paso se borra igual)
            float t = Mathf.SmoothStep(0f, 1f, (Time.time - inicioBorrado) / Mathf.Max(duracionBorrado, 0.01f));
            nuevo = Mathf.Max(borrado, Mathf.Lerp(desdeBorrado, ancho, t), x);
        }
        if (nuevo <= borrado) return;
        borrado = nuevo;

        // empezo a borrarse de verdad (el borde ya llego al dibujo): los carteles encadenados se desvanecen
        if (!empezoABorrarse && borrado + bordeBorrado > margen)
        {
            empezoABorrarse = true;
            if (desvanecerAlEmpezarABorrarme != null)
                foreach (var c in desvanecerAlEmpezarABorrarme) if (c != null && c != this) c.Desvanecer();
        }

        // cuando falta "mostrarCuandoFalte" del dibujo por borrar (o al terminar), aparecen los encadenados
        float dibujo = Mathf.Max(ancho - 2f * margen, 1f);
        float falta = 1f - Mathf.Clamp01((borrado - margen) / dibujo);
        if (!mostroLosSiguientes && (falta <= mostrarCuandoFalte || borrado >= ancho))
        {
            mostroLosSiguientes = true;
            if (mostrarAlTerminarDeBorrarme != null)
                foreach (var c in mostrarAlTerminarDeBorrarme) if (c != null && c != this) c.Mostrar();
        }

        if (borrado >= ancho) { gameObject.SetActive(false); return; }
        mascara.softness = new Vector2Int(bordeBorrado, 0);
        mascara.padding = new Vector4(borrado, 0f, 0f, 0f);
    }

    private IEnumerator Fundido(float desde, float hasta, float tiempo)
    {
        for (float t = 0f; t < tiempo; t += Time.deltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / tiempo));
            yield return null;
        }
        grupo.alpha = hasta;
    }
}
