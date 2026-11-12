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

    private CamaraSeguidora camara;
    private Transform chica;
    private bool adentro;

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
    }

    private void Update()
    {
        if (camara == null || chica == null || columnaIzquierda == null || columnaDerecha == null) return;
        Vector2 p = chica.position;
        float x0 = Mathf.Min(Borde(columnaIzquierda, true), Borde(columnaDerecha, true)) - distanciaActivar;
        float x1 = Mathf.Max(Borde(columnaIzquierda, false), Borde(columnaDerecha, false)) + distanciaActivar;
        float margen = adentro ? margenSalida : 0f;
        bool ahora = p.x > x0 - margen && p.x < x1 + margen && p.y > yMinima && p.y < yMaxima;
        if (ahora == adentro) return;
        adentro = ahora;
        if (adentro) camara.FijarEncuadre(centroEncuadre, tamanoEncuadre, duracion);
        else camara.Liberar(duracion);
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
