using UnityEngine;

// Muerte especial de Umbrae: cuando VidaEnemigo avisa que murio, arma la secuencia
// "umbrae-muerte" -> lo verde de su cabeza cae y se transforma en la gema (umb-muerte-cae-gema)
// -> queda la gema suelta que la chica puede agarrar.
// Las posiciones estan medidas sobre los dibujos (mismo lienzo y escala que el teleport, pivote en la punta).
[RequireComponent(typeof(VidaEnemigo))]
public class MuerteUmbrae : MonoBehaviour
{
    [Header("Umbrae se deshace (umbrae-muerte)")]
    [SerializeField] private Sprite[] framesMuerte;
    [SerializeField] private float fps = 12f;
    [Tooltip("Indice (empieza en 0) del frame donde se ve lo verde en la cabeza (0007).")]
    [SerializeField] private int indiceFrameGema = 7;
    [Tooltip("Centro de lo verde en ese frame, relativo a la punta de Umbrae mirando a la derecha.")]
    [SerializeField] private Vector2 gemaEnMuerte = new Vector2(0.104f, 4.832f);

    [Header("Cae y se transforma en la gema (umb-muerte-cae-gema)")]
    [SerializeField] private Sprite[] framesCaida;
    [Tooltip("Centro de la gema en cada frame, relativo a la punta (en el primero es donde termina de caer).")]
    [SerializeField] private Vector2[] gemaEnCaida;
    [Tooltip("Segundos que tarda en caer desde la cabeza hasta su lugar.")]
    [SerializeField] private float duracionCaida = 0.25f;
    [SerializeField] private float fpsTransformacion = 10f;

    [Header("Gema suelta")]
    [SerializeField] private Sprite gema;
    [Tooltip("Centro de la gema del ultimo frame, relativo a la punta: ahi queda la gema suelta.")]
    [SerializeField] private Vector2 gemaFinal = new Vector2(-0.001f, 3.6455f);
    [Tooltip("Brillo fuerte de la gema (destello_gema).")]
    [SerializeField] private Sprite destello;
    [SerializeField] private Material materialBrillo;

    private SpriteRenderer dibujo;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        GetComponent<VidaEnemigo>().AlMorir += Morir;
    }

    private void Morir()
    {
        if (framesMuerte == null || framesMuerte.Length == 0) return;

        var go = new GameObject("Muerte Umbrae");
        go.transform.position = transform.position;
        go.AddComponent<SecuenciaMuerteUmbrae>().Iniciar(new SecuenciaMuerteUmbrae.Datos
        {
            framesMuerte = framesMuerte,
            fps = fps,
            indiceFrameGema = indiceFrameGema,
            gemaEnMuerte = gemaEnMuerte,
            framesCaida = framesCaida,
            gemaEnCaida = gemaEnCaida,
            duracionCaida = duracionCaida,
            fpsTransformacion = fpsTransformacion,
            gema = gema,
            gemaFinal = gemaFinal,
            destello = destello,
            materialBrillo = materialBrillo,
            punta = transform.position,
            lado = dibujo != null && dibujo.flipX ? -1f : 1f,
            material = dibujo != null ? dibujo.sharedMaterial : null,
            capa = dibujo != null ? dibujo.sortingLayerID : 0,
            orden = dibujo != null ? dibujo.sortingOrder : 0
        });
    }
}
