using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Cartelito de tutorial (por ejemplo "Atacar" con el mouse o el boton circulo) puesto en el mundo,
// al lado del portal de entrada. Es un Canvas en World Space (prefab en Assets/_UI/Carteles):
//  - se dibuja justo detras de la chica (orden de ella - 1), asi queda adelante del fondo,
//    el portal y las columnas, pero la chica pasa por delante.
//  - aparece suavemente apenas la chica termina de salir por la puerta (InicioNivel) y se queda ahi.
//  - no se va solo con el tiempo: a medida que la chica le pasa por delante, se va borrando de
//    izquierda a derecha (lo que ella ya paso no vuelve). Cuando lo paso entero, desaparece.
//    Usa la RectMask2D "Mascara": le va subiendo el padding izquierdo.
// Los arma el menu  Pomelo -> Carteles -> Armar carteles  (Assets/Editor/ArmarCarteles.cs).
[RequireComponent(typeof(Canvas), typeof(CanvasGroup))]
public class CartelTutorial : MonoBehaviour
{
    [Tooltip("Segundos que tarda en aparecer (suave).")]
    [SerializeField] private float duracionAparicion = 1.2f;
    [Tooltip("Si esta prendido, espera a que la chica termine de salir por la puerta del portal.")]
    [SerializeField] private bool esperarSalidaDelPortal = true;

    [Header("Borrado al pasar la chica")]
    [Tooltip("Mascara que tapa lo que la chica ya paso.")]
    [SerializeField] private RectMask2D mascara;
    [Tooltip("Ancho (en px del cartel) del borde difuminado del borrado.")]
    [SerializeField] private int bordeBorrado = 40;

    private Canvas canvas;
    private CanvasGroup grupo;
    private SpriteRenderer dibujoChica;
    private float borrado;
    private bool salio;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (mascara != null) { mascara.padding = Vector4.zero; mascara.softness = Vector2Int.zero; }
    }

    private void OnEnable() { InicioNivel.SalidaTerminada += AlSalir; }
    private void OnDisable() { InicioNivel.SalidaTerminada -= AlSalir; }
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

        if (esperarSalidaDelPortal && FindFirstObjectByType<InicioNivel>() != null)
            while (!salio && !InicioNivel.YaSalio) yield return null;

        // aparece suave y se queda: solo se va cuando la chica le pasa por delante (LateUpdate)
        yield return Fundido(0f, 1f, duracionAparicion);
    }

    private void LateUpdate()
    {
        if (mascara == null || dibujoChica == null) return;

        // hasta donde llego la chica, medido desde el borde izquierdo de la mascara (en px del cartel)
        var rt = mascara.rectTransform;
        float x = rt.InverseTransformPoint(dibujoChica.bounds.center).x - rt.rect.xMin;
        if (x <= borrado) return;
        borrado = x;

        if (borrado >= rt.rect.width) { gameObject.SetActive(false); return; }
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
