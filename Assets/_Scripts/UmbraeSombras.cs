using UnityEngine;

// Va en Umbrae. Le pone los "glows" del Figma ("umbrae_quieto 1"):
// - Las 3 sombras internas van en su propio material (shader "Pomelo/Sprite Lit Sombras Internas").
// - Las 2 drop shadows van en una copia detras de el (shader "Pomelo/Sprite Sombra Exterior") que sigue
//   cada cuadro de la animacion, hacia donde mira y si esta visible o no.
// Los PNG de Umbrae necesitan mipmaps (el desenfoque sale de ahi) y malla FullRect.
[RequireComponent(typeof(SpriteRenderer))]
public class UmbraeSombras : MonoBehaviour
{
    [SerializeField] private Material materialSombraExterior;
    [Tooltip("Cuanto mas atras se dibuja la sombra exterior.")]
    [SerializeField] private int ordenAtras = 1;

    private SpriteRenderer dibujo, sombra;
    private MaterialPropertyBlock mpb;
    private static readonly int IdAlfa = Shader.PropertyToID("_Alfa");

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        if (materialSombraExterior == null) return;
        var go = new GameObject("sombra exterior (Figma)");
        go.transform.SetParent(transform, false);
        sombra = go.AddComponent<SpriteRenderer>();
        sombra.sharedMaterial = materialSombraExterior;
        sombra.sortingLayerID = dibujo.sortingLayerID;
        sombra.sortingOrder = dibujo.sortingOrder - ordenAtras;
        mpb = new MaterialPropertyBlock();
    }

    // despues de que UmbraeBoss cambio el cuadro
    private void LateUpdate()
    {
        if (sombra == null) return;
        sombra.enabled = dibujo.enabled && dibujo.sprite != null;
        if (!sombra.enabled) return;
        sombra.sprite = dibujo.sprite;
        // el flipX se copia como escala (el shader agranda el quad y no lee el flip del sprite)
        sombra.transform.localScale = new Vector3(dibujo.flipX ? -1f : 1f, dibujo.flipY ? -1f : 1f, 1f);
        sombra.sortingOrder = dibujo.sortingOrder - ordenAtras;
        sombra.GetPropertyBlock(mpb);
        mpb.SetFloat(IdAlfa, dibujo.color.a);
        sombra.SetPropertyBlock(mpb);
    }
}
