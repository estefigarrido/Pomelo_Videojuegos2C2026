using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraSeguidora : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
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
    // paso de un encuadre fijo a otro (0 = el anterior, 1 = el nuevo)
    private Vector2 centroDesde;
    private float tamanoDesde;
    private float pasoFijo = 1f;
    private float duracionActual;
    private float inicioTemblor, finTemblor, intensidadTemblor;
    private Vector2 ultimaSacudida;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        tamanoNormal = cam.orthographicSize;
        duracionActual = duracionTransicion;
    }

    // La camara deja de seguir y se queda quieta mostrando ese lugar (con un zoom suave hasta 'tamano').
    // 'duracion' cambia lo que tarda la transicion (si no se pasa, usa duracionTransicion).
    public void FijarEncuadre(Vector2 centro, float tamano, float duracion = -1f)
    {
        duracionActual = duracion > 0f ? duracion : duracionTransicion;
        // si ya estaba en otro encuadre fijo, pasa de a poco desde ese (antes saltaba de golpe)
        if (fijo && mezcla > 0f)
        {
            centroDesde = CentroFijoActual();
            tamanoDesde = TamanoFijoActual();
            pasoFijo = 0f;
        }
        else pasoFijo = 1f;
        fijo = true;
        centroFijo = centro;
        tamanoFijo = tamano;
    }

    // Cambia el encuadre fijo al instante, sin transicion: para moverlo cuadro a cuadro desde otro script
    // (por ejemplo, el acercamiento a la puerta que va siguiendo los pasos de la chica).
    public void MoverEncuadre(Vector2 centro, float tamano)
    {
        if (!fijo) { FijarEncuadre(centro, tamano); return; }
        if (pasoFijo < 1f) { centroDesde = CentroFijoActual(); tamanoDesde = TamanoFijoActual(); pasoFijo = 1f; }
        centroFijo = centro;
        tamanoFijo = tamano;
    }

    private Vector2 CentroFijoActual() => Vector2.Lerp(centroDesde, centroFijo, Mathf.SmoothStep(0f, 1f, pasoFijo));
    private float TamanoFijoActual() => Mathf.Lerp(tamanoDesde, tamanoFijo, Mathf.SmoothStep(0f, 1f, pasoFijo));

    // Vuelve a seguir a la chica con el zoom normal.
    public void Liberar(float duracion = -1f)
    {
        fijo = false;
        duracionActual = duracion > 0f ? duracion : duracionTransicion;
        CambiarTamanoSeguir(tamanoNormal, duracionActual);
        usarAlturaFija = false;
    }

    // Sigue a la chica pero con otro zoom ('tamano'), pasando suave en 'duracion' segundos
    // (por ejemplo, despues de reparar la estatua sigue con el zoom alejado del templo).
    // 'alturaFija': si se pasa, la camara sigue a la chica solo de costado y se queda a esa altura (asi con el
    // zoom alejado no muestra tanto piso abajo).
    public void SeguirConTamano(float tamano, float duracion = -1f, float alturaFija = float.NaN)
    {
        fijo = false;
        duracionActual = duracion > 0f ? duracion : duracionTransicion;
        CambiarTamanoSeguir(tamano, duracionActual);
        if (!float.IsNaN(alturaFija)) yFijaSeguir = alturaFija;
        usarAlturaFija = !float.IsNaN(alturaFija);
    }

    private float yFijaSeguir; private bool usarAlturaFija; private float pesoAltura;

    private float tamanoSeguir = -1f, tamanoSeguirObjetivo, velocidadTamano;
    private void CambiarTamanoSeguir(float objetivo, float duracion)
    {
        if (tamanoSeguir < 0f) tamanoSeguir = tamanoNormal;
        tamanoSeguirObjetivo = objetivo;
        velocidadTamano = Mathf.Abs(objetivo - tamanoSeguir) / Mathf.Max(0.01f, duracion);
    }

    // Tiembla 'duracion' segundos; el temblor se va apagando hasta terminar.
    public void Temblar(float duracion, float intensidad)
    {
        inicioTemblor = Time.time;
        finTemblor = Time.time + duracion;
        intensidadTemblor = intensidad;
    }

    public bool Fijo => fijo;

    private void LateUpdate()
    {
        if (objetivo == null) return;
        Posicionar();

        if (Time.time < finTemblor)
        {
            // en pausa (el tiempo no avanza) se queda quieta donde estaba, en vez de seguir sacudiendose
            if (Time.deltaTime > 0f)
            {
                float queda = 1f - (Time.time - inicioTemblor) / Mathf.Max(0.01f, finTemblor - inicioTemblor);
                ultimaSacudida = Random.insideUnitCircle * intensidadTemblor * queda;
            }
            transform.position += new Vector3(ultimaSacudida.x, ultimaSacudida.y, 0f);
        }
    }

    private void Posicionar()
    {
        mezcla = Mathf.MoveTowards(mezcla, fijo ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, duracionActual));
        float k = Mathf.SmoothStep(0f, 1f, mezcla);
        // el zoom con el que sigue a la chica (normal, o el alejado que pidio SeguirConTamano)
        if (tamanoSeguir < 0f) { tamanoSeguir = tamanoNormal; tamanoSeguirObjetivo = tamanoNormal; }
        tamanoSeguir = Mathf.MoveTowards(tamanoSeguir, tamanoSeguirObjetivo, velocidadTamano * Time.deltaTime);
        float tamanoBase = tamanoSeguir;

        // donde estaria siguiendo a la chica, con el zoom normal: siempre con ella en el centro
        float x = objetivo.position.x;
        float y = seguirVertical ? objetivo.position.y + offsetVertical : transform.position.y;
        // con la altura fija (SeguirConTamano) la camara pasa suave a esa altura y solo sigue a la chica de costado
        pesoAltura = Mathf.MoveTowards(pesoAltura, usarAlturaFija ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, duracionActual));
        if (pesoAltura > 0f) y = Mathf.Lerp(y, yFijaSeguir, Mathf.SmoothStep(0f, 1f, pesoAltura));

        if (k <= 0f)
        {
            cam.orthographicSize = tamanoBase;
            transform.position = new Vector3(x, y, transform.position.z);
            return;
        }

        pasoFijo = Mathf.MoveTowards(pasoFijo, 1f, Time.deltaTime / Mathf.Max(0.01f, duracionActual));
        Vector2 c = CentroFijoActual();
        cam.orthographicSize = Mathf.Lerp(tamanoBase, TamanoFijoActual(), k);
        transform.position = new Vector3(Mathf.Lerp(x, c.x, k), Mathf.Lerp(y, c.y, k), transform.position.z);
    }
}
