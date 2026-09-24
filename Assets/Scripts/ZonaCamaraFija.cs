using UnityEngine;

// Cuando la chica entra en 'zona', la camara deja de seguirla y se queda fija mostrando el encuadre
// (con un de-zoom suave) y suena el tema de la pelea. Al salir, vuelve todo a la normalidad.
// Si el jefe muere, suena el tema de victoria (subiendo de a poco) y la camara sigue fija
// hasta que la chica agarra la gema que dejo.
public class ZonaCamaraFija : MonoBehaviour
{
    [Tooltip("Donde tiene que estar la chica para que la camara se fije (la misma zona de pelea de Umbrae).")]
    [SerializeField] private Rect zona = new Rect(-20.5f, 22.8f, 43f, 10f);
    [Tooltip("Margen extra para salir, asi no parpadea si camina justo por el borde.")]
    [SerializeField] private float margenSalida = 0.6f;
    [Tooltip("El jefe de la pelea. Cuando se desactiva (muere) se termina la pelea.")]
    [SerializeField] private GameObject jefe;

    [Header("Encuadre")]
    [Tooltip("Centro de lo que muestra la camara mientras esta fija.")]
    [SerializeField] private Vector2 centroEncuadre = new Vector2(1.24f, 32.19f);
    [Tooltip("Mitad del alto de lo que muestra (tamano de la camara ortografica). El normal es 5.4.")]
    [SerializeField] private float tamanoEncuadre = 12.26f;

    [Header("Musica")]
    [SerializeField] private AudioClip temaPelea;
    [Tooltip("Volumen del tema de la pelea respecto de la musica del mapa (1.2 = 20% mas fuerte).")]
    [SerializeField] private float volumenPelea = 1.2f;
    [Tooltip("Tema que suena desde que muere el jefe.")]
    [SerializeField] private AudioClip temaVictoria;
    [Tooltip("Volumen al que llega el tema de victoria respecto de la musica del mapa (1.2 = 20% mas fuerte).")]
    [SerializeField] private float volumenVictoria = 1.2f;
    [Tooltip("Segundos que tarda el tema de victoria en subir hasta su volumen.")]
    [SerializeField] private float subidaVictoria = 12f;
    [Tooltip("Segundos del cruce entre el tema de la pelea y el de victoria (al terminar, la victoria suena como la musica del mapa y sigue subiendo).")]
    [SerializeField] private float cruceVictoria = 4f;
    [Tooltip("Segundo de la cancion de victoria desde el que arranca (y al que vuelve cuando da la vuelta).")]
    [SerializeField] private float inicioVictoria = 33f;

    private CamaraSeguidora camara;
    private MusicaDeFondo musica;
    private Transform chica;
    private bool adentro;
    private bool jefeMuerto;
    private bool gemaRecolectada;

    // true mientras la chica esta adentro peleando y el jefe sigue vivo
    public bool PeleaEnCurso => adentro && !jefeMuerto;

    private void OnEnable() => GemaUmbrae.Recolectada += AlRecolectarGema;
    private void OnDisable() => GemaUmbrae.Recolectada -= AlRecolectarGema;
    private void AlRecolectarGema() => gemaRecolectada = true;

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        musica = FindFirstObjectByType<MusicaDeFondo>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;
    }

    private void Update()
    {
        if (camara == null || chica == null) return;

        if (!jefeMuerto && jefe != null && !jefe.activeInHierarchy)
        {
            jefeMuerto = true;
            if (musica != null && temaVictoria != null)
                musica.PonerTema(temaVictoria, musica.Volumen * volumenVictoria, cruceVictoria, subidaVictoria, musica.Volumen, inicioVictoria);
        }

        // la pelea (camara fija) dura hasta que la chica agarra la gema de Umbrae
        Vector2 p = chica.position;
        bool peleaActiva = !jefeMuerto || !gemaRecolectada;
        bool ahora = peleaActiva && (adentro
            ? new Rect(zona.x - margenSalida, zona.y - margenSalida, zona.width + margenSalida * 2f, zona.height + margenSalida * 2f).Contains(p)
            : zona.Contains(p));

        if (ahora == adentro) return;
        adentro = ahora;
        if (adentro)
        {
            camara.FijarEncuadre(centroEncuadre, tamanoEncuadre);
            if (musica != null && temaPelea != null && !jefeMuerto) musica.PonerTema(temaPelea, musica.Volumen * volumenPelea);
        }
        else
        {
            camara.Liberar();
            // si se va sin terminar la pelea vuelve la musica del mapa; si gano, queda el tema de victoria
            if (musica != null && !jefeMuerto) musica.QuitarTema();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        float alto = tamanoEncuadre * 2f, ancho = alto * 16f / 9f;
        Gizmos.DrawWireCube(centroEncuadre, new Vector3(ancho, alto, 0f));
        Gizmos.color = new Color(1f, 0.6f, 0.6f, 0.6f);
        Gizmos.DrawWireCube(zona.center, zona.size);
    }
}
