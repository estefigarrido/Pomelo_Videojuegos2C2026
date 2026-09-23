using UnityEngine;

// Revolotea en linea recta dentro de una zona eliptica y rebota en el borde,
// como el logo de DVD, con un desvio al azar en cada rebote para no repetir el recorrido.
public class PajaroRevoloteo : MonoBehaviour
{
    [Header("Zona")]
    [Tooltip("Centro de la zona donde revolotea, en coordenadas del mundo.")]
    [SerializeField] private Vector2 centroZona;
    [Tooltip("Radio horizontal y vertical de la zona (es una elipse).")]
    [SerializeField] private Vector2 radiosZona = new Vector2(2.5f, 2.5f);

    [Header("Vuelo")]
    [Tooltip("Unidades por segundo (1 unidad = 100 px).")]
    [SerializeField] private float velocidad = 2.5f;
    [Tooltip("Cuantos grados se desvia al azar cada vez que rebota.")]
    [SerializeField] private float desvioAlRebotar = 25f;
    [Tooltip("Angulo maximo de salida respecto de la perpendicular al borde. Menos = cruza mas por el medio de la zona.")]
    [SerializeField] private float anguloMaximoSalida = 60f;
    [Tooltip("El dibujo original mira hacia la derecha.")]
    [SerializeField] private bool dibujoMiraDerecha = true;

    private SpriteRenderer dibujo;
    private Vector2 direccion;
    private Vector2 radiosUtiles;

    public void ConfigurarZona(Vector2 centro, Vector2 radios)
    {
        centroZona = centro;
        radiosZona = radios;
    }

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();

        // que todo el dibujo quede dentro de la zona, no solo el centro
        Vector2 medio = dibujo != null && dibujo.sprite != null
            ? Vector2.Scale(ContenidoVisible(dibujo.sprite), transform.lossyScale) * 0.5f
            : Vector2.zero;
        radiosUtiles = new Vector2(Mathf.Max(0.3f, radiosZona.x - medio.x), Mathf.Max(0.3f, radiosZona.y - medio.y));

        float angulo = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        direccion = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));

        var animator = GetComponent<Animator>();
        if (animator != null) animator.Play(0, -1, Random.value);
    }

    private void Update()
    {
        Vector2 pos = (Vector2)transform.position + direccion * velocidad * Time.deltaTime;
        Vector2 d = pos - centroZona;
        float a = radiosUtiles.x, b = radiosUtiles.y;
        float k = (d.x * d.x) / (a * a) + (d.y * d.y) / (b * b);

        if (k > 1f)
        {
            Vector2 normal = new Vector2(d.x / (a * a), d.y / (b * b)).normalized;
            direccion = Vector2.Reflect(direccion, normal);
            direccion = Rotar(direccion, Random.Range(-desvioAlRebotar, desvioAlRebotar));

            // en una elipse, un rebote casi paralelo al borde lo deja girando pegado a la orilla;
            // limitar el angulo de salida lo obliga a cruzar la zona
            float angulo = Vector2.SignedAngle(-normal, direccion);
            direccion = Rotar(-normal, Mathf.Clamp(angulo, -anguloMaximoSalida, anguloMaximoSalida));

            pos = centroZona + d / Mathf.Sqrt(k);
        }

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);

        if (dibujo != null && Mathf.Abs(direccion.x) > 0.05f)
            dibujo.flipX = dibujoMiraDerecha ? direccion.x < 0f : direccion.x > 0f;
    }

    private static Vector2 Rotar(Vector2 v, float grados)
    {
        float r = grados * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c).normalized;
    }

    // el lienzo del frame tiene aire alrededor; esto mide solo el dibujo
    private static Vector2 ContenidoVisible(Sprite sprite)
    {
        var verts = sprite.vertices;
        if (verts.Length == 0) return sprite.bounds.size;
        Vector2 min = verts[0], max = verts[0];
        foreach (var v in verts) { min = Vector2.Min(min, v); max = Vector2.Max(max, v); }
        return max - min;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        const int segmentos = 48;
        Vector3 previo = centroZona + new Vector2(radiosZona.x, 0f);
        for (int i = 1; i <= segmentos; i++)
        {
            float t = i / (float)segmentos * Mathf.PI * 2f;
            Vector3 punto = centroZona + new Vector2(Mathf.Cos(t) * radiosZona.x, Mathf.Sin(t) * radiosZona.y);
            Gizmos.DrawLine(previo, punto);
            previo = punto;
        }
    }
}
