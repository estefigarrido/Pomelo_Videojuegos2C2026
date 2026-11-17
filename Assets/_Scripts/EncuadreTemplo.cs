using UnityEngine;

// Cuando la chica llega a 'distanciaActivar' (5 u = 500 px) de la columna del templo de la estatua, la camara
// deja de seguirla y se aleja suave hasta mostrar todo el templo (arco, estatua y las dos columnas).
// Mientras la chica siga dentro de la zona (entre las dos columnas, con ese margen a cada lado) la camara
// queda fija; al salir vuelve a seguirla.
public class EncuadreTemplo : MonoBehaviour
{
    [Tooltip("Las dos columnas del templo (la zona va de una a la otra, con 'distanciaActivar' de margen).")]
    [SerializeField] private Transform columnaIzquierda;
    [SerializeField] private Transform columnaDerecha;
    [Tooltip("A esta distancia (unidades) de una columna empieza el de-zoom. 5 = 500 px.")]
    [SerializeField] private float distanciaActivar = 5f;
    [Tooltip("Margen extra para salir, asi no parpadea si camina justo por el borde.")]
    [SerializeField] private float margenSalida = 0.6f;
    [Tooltip("La chica tiene que estar entre estas alturas (el piso del templo).")]
    [SerializeField] private float yMinima = 47f;
    [SerializeField] private float yMaxima = 72f;

    [Header("Encuadre")]
    [Tooltip("Centro de lo que muestra la camara (todo el templo).")]
    [SerializeField] private Vector2 centroEncuadre = new Vector2(-82.7f, 60.4f);
    [Tooltip("Mitad del alto de lo que muestra (tamano de la camara ortografica). El normal es 5.4.")]
    [SerializeField] private float tamanoEncuadre = 12.5f;
    [Tooltip("Segundos que tarda en alejarse y en volver.")]
    [SerializeField] private float duracion = 1.4f;

    [Header("Hasta el final (portal)")]
    [Tooltip("La camara sigue alejada mientras la chica va del templo hasta esta x (el portal del final, a la izquierda), " +
             "asi el de-zoom queda hasta que arranca el final.")]
    [SerializeField] private float xFinal = -116f;
    [Tooltip("x donde la camara ya termino de acercarse a la puerta. Entre el final de la franja oscura (borde de la " +
             "zona del templo) y esta x, el zoom pasa de a poco del templo a la puerta, siguiendo los pasos de la chica.")]
    [SerializeField] private float xLlegaPuerta = -110f;
    [Tooltip("Que tan lento sigue el zoom a los pasos (segundos aprox.). Mas alto = mas sutil.")]
    [SerializeField] private float suavidadAcercamiento = 1.2f;
    private float acercamiento, velAcercamiento;

    [Header("Despues de reparar la estatua")]
    [Tooltip("Segundos del paso (suave y gradual) del de-zoom del templo al encuadre de la puerta, y de vuelta.")]
    [SerializeField] private float duracionLenta = 3f;
    [Tooltip("Encuadre mas cercano sobre la puerta del final (centro y tamano de la camara).")]
    [SerializeField] private Vector2 centroPuerta = new Vector2(-111f, 55.6f);
    [SerializeField] private float tamanoPuerta = 7.2f;

    private CamaraSeguidora camara;
    private Transform chica;
    private EstatuaRestaurada estatua;
    private bool adentro;
    // 0 sigue a la chica, 1 el templo fijo, 2 corrido hacia el portal, 3 sigue a la chica con el zoom del templo
    private int encuadre;

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
        estatua = GetComponentInChildren<EstatuaRestaurada>();
        if (estatua == null) estatua = FindFirstObjectByType<EstatuaRestaurada>();
    }

    private void Update()
    {
        if (camara == null || chica == null || columnaIzquierda == null || columnaDerecha == null) return;
        // una vez que empezo el final, el encuadre queda fijo hasta la pantalla de victoria
        // (si estaba acercandose a la puerta, termina ese acercamiento suave)
        if (FinalNivel.EnCurso)
        {
            if (encuadre == 2) AcercarAPuerta(1f);
            return;
        }
        Vector2 p = chica.position;
        float x0 = Mathf.Min(Borde(columnaIzquierda, true), Borde(columnaDerecha, true)) - distanciaActivar;
        float x1 = Mathf.Max(Borde(columnaIzquierda, false), Borde(columnaDerecha, false)) + distanciaActivar;
        float margen = adentro ? margenSalida : 0f;
        bool enAltura = p.y > yMinima && p.y < yMaxima;
        bool enTemplo = p.x > x0 - margen && p.x < x1 + margen && enAltura;
        // en el templo: el de-zoom fijo de la estatua (antes y despues de repararla).
        // Al pasar el borde izquierdo de la zona del templo (la franja oscura antes de la columna) hacia la puerta del
        // final: la camara se acerca, suave y despacio ('duracionLenta'), a un encuadre mas cerrado sobre la puerta.
        bool haciaPuerta = !enTemplo && enAltura && p.x <= x0 && p.x > xFinal - margen && encuadre != 0;
        int ahora = enTemplo ? 1 : haciaPuerta ? 2 : 0;
        if (ahora == 2 && encuadre == 2)
        {
            // el acercamiento va con los pasos de la chica: recien empieza al terminar la franja oscura y llega
            // al encuadre de la puerta en 'xLlegaPuerta' (si vuelve para atras, se aleja igual de gradual)
            AcercarAPuerta(Mathf.InverseLerp(x0, xLlegaPuerta, p.x));
            return;
        }
        if (ahora == encuadre) return;
        bool lento = (encuadre == 1 && ahora == 2) || (encuadre == 2 && ahora == 1);
        float dur = lento ? duracionLenta : duracion;
        encuadre = ahora;
        adentro = ahora != 0;
        if (ahora == 1) camara.FijarEncuadre(centroEncuadre, tamanoEncuadre, dur);
        else if (ahora == 2)
        {
            // arranca en el mismo encuadre del templo; AcercarAPuerta lo va acercando cuadro a cuadro
            acercamiento = 0f; velAcercamiento = 0f;
            camara.FijarEncuadre(centroEncuadre, tamanoEncuadre, dur);
        }
        else camara.Liberar(dur);
    }

    // 'objetivo' 0 = encuadre del templo, 1 = encuadre de la puerta. El zoom va hacia ahi sin apuro
    // ('suavidadAcercamiento' segundos), asi aunque la chica camine rapido el paso es sutil y gradual.
    private void AcercarAPuerta(float objetivo)
    {
        acercamiento = Mathf.SmoothDamp(acercamiento, objetivo, ref velAcercamiento, suavidadAcercamiento);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(acercamiento));
        camara.MoverEncuadre(Vector2.Lerp(centroEncuadre, centroPuerta, t), Mathf.Lerp(tamanoEncuadre, tamanoPuerta, t));
    }

    // borde izquierdo o derecho del dibujo de la columna (si no tiene dibujo, su posicion)
    private static float Borde(Transform t, bool izquierdo)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) return t.position.x;
        return izquierdo ? sr.bounds.min.x : sr.bounds.max.x;
    }

    private void OnDisable()
    {
        if (adentro && camara != null) camara.Liberar(duracion);
        adentro = false;
        encuadre = 0;
    }

    private void OnDrawGizmosSelected()
    {
        if (columnaIzquierda == null || columnaDerecha == null) return;
        float x0 = Mathf.Min(Borde(columnaIzquierda, true), Borde(columnaDerecha, true)) - distanciaActivar;
        float x1 = Mathf.Max(Borde(columnaIzquierda, false), Borde(columnaDerecha, false)) + distanciaActivar;
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireCube(new Vector3((x0 + x1) / 2f, (yMinima + yMaxima) / 2f), new Vector3(x1 - x0, yMaxima - yMinima));
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.8f);
        float alto = tamanoEncuadre * 2f, ancho = alto * 16f / 9f;
        Gizmos.DrawWireCube(centroEncuadre, new Vector3(ancho, alto));
    }
}
