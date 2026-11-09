using UnityEngine;

// Va en cada planta que cuelga (Colgante_1 / _2, Musgo_colgante). Al empezar corta el dibujo en franjas
// horizontales que siguen una soga (CadenaVerlet): la planta se dobla con la gravedad, se mece apenas con
// el aire y, cuando la chica pasa por adentro, se hamaca hacia donde ella camina y vuelve despacio.
// 'anclaX' (0 a 1) es de donde cuelga el dibujo, arriba; lo mide el menu de configuracion.
[RequireComponent(typeof(SpriteRenderer))]
public class ColganteFlexible : MonoBehaviour
{
    [Tooltip("De donde cuelga el dibujo: 0 = borde izquierdo, 1 = borde derecho (arriba de todo).")]
    [Range(0f, 1f)] [SerializeField] private float anclaX = 0.5f;
    [Tooltip("En cuantas franjas se corta el dibujo (mas = se dobla mas parejo).")]
    [SerializeField] private int franjas = 10;
    [Tooltip("0 = soga floja; mas = vuelve mas rapido a colgar derecha.")]
    [Range(0f, 1f)] [SerializeField] private float rigidez = 0.18f;
    [SerializeField] private float gravedad = 9f;
    [Tooltip("Cuanto la empuja la chica al pasar.")]
    [SerializeField] private float empuje = 0.9f;
    [Tooltip("Vaiven del aire (aceleracion, unidades/s2).")]
    [SerializeField] private float viento = 0.35f;

    private CadenaVerlet cadena;
    private Transform[] partes;
    private Collider2D cuerpoChica;
    private Rigidbody2D rbChica;
    private float fase;

    private void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        Sprite sp = sr.sprite;
        if (sp == null) { enabled = false; return; }
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) { cuerpoChica = salud.GetComponent<Collider2D>(); rbChica = salud.GetComponent<Rigidbody2D>(); }
        fase = Random.value * 10f;

        Rect r = sp.textureRect;
        float ppu = sp.pixelsPerUnit;
        int n = Mathf.Max(2, franjas);
        float alto = r.height / n;
        float xAncla = (anclaX * sp.rect.width - sp.pivot.x) / ppu;
        float yArriba = (sp.rect.height - sp.pivot.y) / ppu;

        partes = new Transform[n];
        var puntos = new Vector2[n + 1];
        for (int k = 0; k <= n; k++)
            puntos[k] = transform.TransformPoint(new Vector3(xAncla, yArriba - k * alto / ppu, 0f));

        for (int k = 0; k < n; k++)
        {
            // cada franja se superpone 2 px con la de abajo para que no se vea la union al doblarse
            float arriba = r.yMax - k * alto;
            float abajo = Mathf.Max(r.yMin, arriba - alto - 2f);
            var rect = new Rect(r.x, abajo, r.width, arriba - abajo);
            var franja = Sprite.Create(sp.texture, rect, new Vector2(anclaX, 1f), ppu, 0, SpriteMeshType.FullRect);
            franja.name = sp.name + "_franja" + k;

            var go = new GameObject("franja " + k);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite = franja;
            s.sharedMaterial = sr.sharedMaterial;
            s.color = sr.color;
            s.flipX = sr.flipX; s.flipY = sr.flipY;
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = sr.sortingOrder;
            s.maskInteraction = sr.maskInteraction;
            partes[k] = go.transform;
        }
        sr.enabled = false;
        cadena = new CadenaVerlet(puntos, rigidez);
        Acomodar();
    }

    private void Update()
    {
        if (cadena == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float t = Time.time;
        var aire = new Vector2(viento * (Mathf.Sin(t * 1.1f + fase) + 0.4f * Mathf.Sin(t * 2.7f + fase * 2f)), 0f);
        if (cuerpoChica != null && rbChica != null)
        {
            Bounds b = cuerpoChica.bounds; b.Expand(0.3f);
            cadena.Empujar(b, rbChica.linearVelocity, dt, empuje);
        }
        cadena.Paso(dt, gravedad, aire);
        Acomodar();
    }

    // cada franja va en su punto de la soga, girada como su tramo
    private void Acomodar()
    {
        for (int k = 0; k < partes.Length; k++)
        {
            Vector3 local = transform.InverseTransformPoint(cadena.p[k]);
            Vector3 dir = transform.InverseTransformVector(cadena.p[k + 1] - cadena.p[k]);
            partes[k].localPosition = new Vector3(local.x, local.y, 0f);
            partes[k].localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg);
        }
    }
}
