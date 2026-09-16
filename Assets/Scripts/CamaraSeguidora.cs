using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraSeguidora : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField] private float margenIzquierdoPx = 500f;
    [SerializeField] private bool seguirVertical = true;
    [SerializeField] private float offsetVertical = 1f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        float unidadesPorPixel = (cam.orthographicSize * 2f) / Screen.height;
        float margenUnidades = margenIzquierdoPx * unidadesPorPixel;
        float mitadAncho = cam.orthographicSize * cam.aspect;

        float x = objetivo.position.x + mitadAncho - margenUnidades;
        float y = seguirVertical ? objetivo.position.y + offsetVertical : transform.position.y;

        transform.position = new Vector3(x, y, transform.position.z);
    }
}
