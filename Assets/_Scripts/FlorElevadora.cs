using System.Collections.Generic;
using UnityEngine;

// Flor elevadora (como las columnas de burbujas de soul sand de Minecraft).
// Arriba de la flor hay una zona (el rectangulo turquesa del Figma): mientras la chica esta adentro,
// un viento leve la empuja hacia arriba hasta el borde de arriba de la zona.
// Por la zona suben cristalitos rojos revoloteando (los del Figma), para que se vea el viento.
// Va en la flor, junto con dos BoxCollider2D: uno solido (para pararse) y otro trigger (la zona del viento).
public class FlorElevadora : MonoBehaviour
{
    [Header("Viento")]
    [Tooltip("La zona del viento (el BoxCollider2D marcado como trigger).")]
    [SerializeField] private BoxCollider2D zona;
    [Tooltip("Velocidad a la que la propulsa hacia arriba adentro de la zona (unidades por segundo).")]
    [SerializeField] private float velocidadPropulsion = 6f;
    [Tooltip("Que tan rapido llega a esa velocidad (unidades por segundo, por segundo), ademas de anular la gravedad.")]
    [SerializeField] private float aceleracionPropulsion = 30f;

    [Header("Cristalitos")]
    [SerializeField] private Sprite[] cristales;
    [Tooltip("Cristalitos por segundo.")]
    [SerializeField] private float porSegundo = 5f;
    [Tooltip("Velocidad con la que suben (unidades por segundo): minima y maxima.")]
    [SerializeField] private Vector2 velocidadCristales = new Vector2(1.2f, 2.4f);
    [Tooltip("Revoloteo de costado: cuanto se mueven (unidades) y cuantas veces por segundo.")]
    [SerializeField] private float revoloteo = 0.35f;
    [SerializeField] private float frecuenciaRevoloteo = 1.1f;
    [Tooltip("Cuanto giran (grados por segundo, al azar entre -x y x).")]
    [SerializeField] private float giro = 140f;
    [Tooltip("Tamano al azar (1 = el del Figma).")]
    [SerializeField] private Vector2 tamano = new Vector2(0.7f, 1.1f);

    private class Cristal
    {
        public Transform t; public SpriteRenderer sr;
        public float x, y, vel, fase, frec, amp, giro, vida, duracion;
    }

    private readonly List<Cristal> activos = new List<Cristal>();
    private readonly Stack<Cristal> libres = new Stack<Cristal>();
    private float acumulado;
    private SpriteRenderer dibujo;

    private void Awake() => dibujo = GetComponent<SpriteRenderer>();

    private void OnTriggerStay2D(Collider2D otro)
    {
        if (zona == null || !otro.IsTouching(zona)) return;
        var rb = otro.attachedRigidbody;
        if (rb == null || otro.GetComponentInParent<SaludPersonaje>() == null) return;
        var v = rb.linearVelocity;
        // sube hasta el borde de arriba de la zona; ahi deja de empujar
        if (otro.bounds.min.y > zona.bounds.max.y) return;
        // anula la gravedad de la chica (esta en x2) y la propulsa hacia arriba
        float gravedad = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        v.y += gravedad * Time.fixedDeltaTime;
        v.y = Mathf.MoveTowards(v.y, velocidadPropulsion, aceleracionPropulsion * Time.fixedDeltaTime);
        rb.linearVelocity = v;
    }

    private void Update()
    {
        if (zona == null || cristales == null || cristales.Length == 0) return;
        Bounds b = zona.bounds;

        acumulado += porSegundo * Time.deltaTime;
        while (acumulado >= 1f) { acumulado -= 1f; Soltar(b); }

        for (int i = activos.Count - 1; i >= 0; i--)
        {
            var c = activos[i];
            c.vida += Time.deltaTime;
            c.y += c.vel * Time.deltaTime;
            float u = c.vida / c.duracion;
            if (u >= 1f || c.y > b.max.y) { c.t.gameObject.SetActive(false); activos.RemoveAt(i); libres.Push(c); continue; }
            float lado = Mathf.Sin((c.vida * c.frec + c.fase) * Mathf.PI * 2f) * c.amp;
            c.t.position = new Vector3(c.x + lado, c.y, transform.position.z);
            c.t.Rotate(0f, 0f, c.giro * Time.deltaTime);
            // aparece y desaparece de a poco
            var col = c.sr.color; col.a = Mathf.Clamp01(Mathf.Min(u * 5f, (1f - u) * 4f)); c.sr.color = col;
        }
    }

    private void Soltar(Bounds b)
    {
        var c = libres.Count > 0 ? libres.Pop() : Nuevo();
        c.sr.sprite = cristales[Random.Range(0, cristales.Length)];
        c.x = Random.Range(b.min.x + 0.2f, b.max.x - 0.2f);
        c.y = b.min.y + Random.Range(0f, 0.6f);
        c.vel = Random.Range(velocidadCristales.x, velocidadCristales.y);
        c.duracion = (b.max.y - c.y) / c.vel;
        c.vida = 0f;
        c.fase = Random.value;
        c.frec = frecuenciaRevoloteo * Random.Range(0.7f, 1.3f);
        c.amp = revoloteo * Random.Range(0.5f, 1f);
        c.giro = Random.Range(-giro, giro);
        float s = Random.Range(tamano.x, tamano.y);
        c.t.localScale = new Vector3(s, s, 1f);
        c.t.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        c.t.position = new Vector3(c.x, c.y, transform.position.z);
        c.t.gameObject.SetActive(true);
        activos.Add(c);
    }

    private Cristal Nuevo()
    {
        var go = new GameObject("cristal");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        if (dibujo != null)
        {
            sr.sharedMaterial = dibujo.sharedMaterial;
            sr.sortingLayerID = dibujo.sortingLayerID;
            sr.sortingOrder = dibujo.sortingOrder + 1;
        }
        return new Cristal { t = go.transform, sr = sr };
    }

    private void OnDrawGizmos()
    {
        if (zona == null) return;
        Gizmos.color = new Color(0.39f, 0.62f, 0.58f, 0.6f);
        Gizmos.DrawWireCube(zona.bounds.center, zona.bounds.size);
    }
}
