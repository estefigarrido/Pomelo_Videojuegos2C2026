using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraSeguidora : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField] private float margenIzquierdoPx = 500f;
    [SerializeField] private bool seguirVertical = true;
    [SerializeField] private float offsetVertical = 1f;

    [Header("Encuadre fijo (por ejemplo, la pelea con Umbrae)")]
    [Tooltip("Segundos que tarda en pasar de seguir a la chica al encuadre fijo (y al volver).")]
    [SerializeField] private float duracionTransicion = 0.9f;

    private Camera cam;
    private float tamanoNormal;
    private bool fijo;
    private Vector2 centroFijo;
    private float tamanoFijo;
    private float mezcla; // 0 = sigue a la chica, 1 = encuadre fijo

    private void Awake()
    {
        cam = GetComponent<Camera>();
        tamanoNormal = cam.orthographicSize;
    }

    // La camara deja de seguir y se queda quieta mostrando ese lugar (con un zoom suave hasta 'tamano').
    public void FijarEncuadre(Vector2 centro, float tamano)
    {
        fijo = true;
        centroFijo = centro;
        tamanoFijo = tamano;
    }

    // Vuelve a seguir a la chica con el zoom normal.
    public void Liberar()
    {
        fijo = false;
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        mezcla = Mathf.MoveTowards(mezcla, fijo ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, duracionTransicion));
        float k = Mathf.SmoothStep(0f, 1f, mezcla);

        // donde estaria siguiendo a la chica, con el zoom normal
        float unidadesPorPixel = (tamanoNormal * 2f) / Screen.height;
        float margenUnidades = margenIzquierdoPx * unidadesPorPixel;
        float mitadAncho = tamanoNormal * cam.aspect;

        float x = objetivo.position.x + mitadAncho - margenUnidades;
        float y = seguirVertical ? objetivo.position.y + offsetVertical : transform.position.y;

        if (k <= 0f)
        {
            cam.orthographicSize = tamanoNormal;
            transform.position = new Vector3(x, y, transform.position.z);
            return;
        }

        cam.orthographicSize = Mathf.Lerp(tamanoNormal, tamanoFijo, k);
        transform.position = new Vector3(Mathf.Lerp(x, centroFijo.x, k), Mathf.Lerp(y, centroFijo.y, k), transform.position.z);
    }
}
