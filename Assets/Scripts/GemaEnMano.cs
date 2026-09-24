using System.Collections.Generic;
using UnityEngine;

// Cuando la chica agarra la gema de Umbrae, la lleva en la mano de atras (con su brillo).
// Para cada dibujo de la chica hay guardada la posicion de esa mano (medida sobre los PNG).
// En los frames donde esa mano queda tapada por el cuerpo, la gema va detras de la chica.
// Corre despues de todo lo demas (los golpes cambian el dibujo en LateUpdate), por eso el orden alto.
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(SpriteRenderer))]
public class GemaEnMano : MonoBehaviour
{
    public static bool TieneGema { get; private set; }

    [SerializeField] private Sprite gema;
    [SerializeField] private Sprite destello;
    [SerializeField] private Material materialBrillo;
    [Tooltip("Tamano de la gema en la mano respecto de la gema suelta.")]
    [SerializeField] private float escala = 0.4f;
    [Tooltip("Donde queda el centro de la gema respecto de la mano (la mano la agarra de arriba).")]
    [SerializeField] private Vector2 ajuste = new Vector2(-0.03f, -0.16f);
    [SerializeField] private float periodoBrillo = 1.6f;
    [SerializeField] private float brilloMinimo = 0.75f;
    [SerializeField] private float brilloMaximo = 1f;

    [Header("Mano de atras en cada dibujo (unidades locales del sprite)")]
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Vector2[] manos;
    [SerializeField] private bool[] ocultas;

    private SpriteRenderer dibujo;
    private Dictionary<Sprite, int> indice;
    private Transform objetoGema;
    private SpriteRenderer srGema;
    private SpriteRenderer srBrillo;
    private Vector2 ultimaMano;
    private bool ultimaOculta;

    private void Awake()
    {
        TieneGema = false;
        dibujo = GetComponent<SpriteRenderer>();
        indice = new Dictionary<Sprite, int>();
        if (sprites != null)
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null && !indice.ContainsKey(sprites[i])) indice.Add(sprites[i], i);
    }

    private void OnEnable() => GemaUmbrae.Recolectada += Agarrar;
    private void OnDisable() => GemaUmbrae.Recolectada -= Agarrar;

    private void Agarrar()
    {
        TieneGema = true;
        if (objetoGema != null || gema == null) return;

        var go = new GameObject("Gema en la mano");
        objetoGema = go.transform;
        objetoGema.localScale = Vector3.one * escala;
        srGema = go.AddComponent<SpriteRenderer>();
        srGema.sprite = gema;
        srGema.sharedMaterial = dibujo.sharedMaterial;
        srGema.sortingLayerID = dibujo.sortingLayerID;

        if (destello != null)
        {
            var b = new GameObject("Destello");
            b.transform.SetParent(objetoGema, false);
            srBrillo = b.AddComponent<SpriteRenderer>();
            srBrillo.sprite = destello;
            if (materialBrillo != null) srBrillo.sharedMaterial = materialBrillo;
            srBrillo.sortingLayerID = dibujo.sortingLayerID;
        }
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (!TieneGema || objetoGema == null) return;

        if (dibujo.sprite != null && indice.TryGetValue(dibujo.sprite, out int i) && i < manos.Length)
        {
            ultimaMano = manos[i];
            ultimaOculta = ocultas != null && i < ocultas.Length && ocultas[i];
        }
        // si el dibujo no esta en la tabla, se queda donde estaba la ultima vez
        Vector3 p = transform.TransformPoint(ultimaMano + ajuste);
        objetoGema.position = new Vector3(p.x, p.y, transform.position.z);

        // delante de la chica si la mano se ve; detras si la mano queda tapada por el cuerpo
        int orden = dibujo.sortingOrder;
        srGema.sortingOrder = ultimaOculta ? orden - 1 : orden + 2;
        if (srBrillo != null)
        {
            srBrillo.sortingOrder = ultimaOculta ? orden - 2 : orden + 1;
            float onda = Mathf.Sin(Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, periodoBrillo));
            srBrillo.color = new Color(1f, 1f, 1f, Mathf.Lerp(brilloMinimo, brilloMaximo, (onda + 1f) * 0.5f));
        }
    }
}
