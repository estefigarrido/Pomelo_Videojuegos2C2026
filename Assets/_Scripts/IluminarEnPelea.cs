using UnityEngine;

// Version iluminada de un sprite del templo (la ventana-ojo de Umbrae y su contenedor).
// Antes de la pelea se ve el sprite normal (apagado). Al final de la animacion de inicio de la pelea,
// cuando aparece el rombo del ojo, aparece encima la version iluminada con un fundido, y cuando la
// pelea termina se vuelve a apagar.
[RequireComponent(typeof(SpriteRenderer))]
public class IluminarEnPelea : MonoBehaviour
{
    [Tooltip("La animacion de inicio de la pelea: se enciende cuando aparece el rombo del ojo.")]
    [SerializeField] private InicioPeleaUmbrae inicioPelea;
    [Tooltip("Solo si no hay inicio de pelea: se enciende apenas la chica entra a la zona.")]
    [SerializeField] private ZonaCamaraFija zona;
    [Tooltip("El mismo dibujo, iluminado (mismo tamano y pivote que el apagado).")]
    [SerializeField] private Sprite iluminado;
    [Tooltip("Segundos que tarda en iluminarse al empezar la pelea.")]
    [SerializeField] private float segundosEncender = 0.8f;
    [Tooltip("Segundos que tarda en apagarse cuando termina.")]
    [SerializeField] private float segundosApagar = 0.8f;
    [Tooltip("Cuanto mas adelante (z) va la copia iluminada, para quedar encima del dibujo apagado.")]
    [SerializeField] private float adelante = 0.002f;

    private SpriteRenderer encima;
    private float nivel;

    private void Awake()
    {
        if (inicioPelea == null) inicioPelea = FindFirstObjectByType<InicioPeleaUmbrae>();
        if (zona == null) zona = FindFirstObjectByType<ZonaCamaraFija>();
        var propio = GetComponent<SpriteRenderer>();
        if (iluminado == null) return;

        var go = new GameObject("iluminado");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -adelante / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.z)));
        encima = go.AddComponent<SpriteRenderer>();
        encima.sprite = iluminado;
        encima.sharedMaterial = propio.sharedMaterial;
        encima.sortingLayerID = propio.sortingLayerID;
        encima.sortingOrder = propio.sortingOrder;
        encima.maskInteraction = propio.maskInteraction;
        encima.flipX = propio.flipX;
        encima.flipY = propio.flipY;
        Aplicar(0f);
    }

    private void Update()
    {
        if (encima == null) return;
        bool pelea = inicioPelea != null ? inicioPelea.OjoEncendido : zona != null && zona.PeleaEnCurso;
        float dur = pelea ? segundosEncender : segundosApagar;
        float nuevo = Mathf.MoveTowards(nivel, pelea ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, dur));
        if (!Mathf.Approximately(nuevo, nivel)) Aplicar(nuevo);
    }

    private void Aplicar(float n)
    {
        nivel = n;
        var c = encima.color;
        c.a = Mathf.SmoothStep(0f, 1f, n);
        encima.color = c;
        encima.enabled = n > 0f;
    }
}
