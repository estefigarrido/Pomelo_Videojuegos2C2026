using UnityEngine;
using UnityEngine.UI;

// Animacion de cuando se pierde una vida en el HUD (prefab Explosion_vida, en _UI/Barravida_y_sabiduria):
//  1. un destello chiquito en el centro del icono, que crece rapido y se apaga (la "explosion")
//  2. de ese destello salen unas pocas particulas medianas que se abren, frenan, se achican y se apagan
// Todo queda adentro del contenedor de la vida: el prefab ocupa justo el icono y tiene una RectMask2D,
// asi que nada se dibuja afuera. Se destruye solo al terminar.
// Lo pone HUDVidaSabiduria como hijo del icono que se apaga.
[RequireComponent(typeof(RectTransform))]
public class ExplosionVida : MonoBehaviour
{
    [Header("Dibujos")]
    [SerializeField] private Sprite destello;
    [SerializeField] private Sprite particula;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.8f, 1f);

    [Header("Explosion")]
    [Tooltip("Tamano maximo del destello, en proporcion al icono (1 = todo el icono).")]
    [SerializeField] private float tamanoDestello = 0.75f;
    [SerializeField] private float duracionDestello = 0.3f;

    [Header("Particulas")]
    [SerializeField] private int cantidad = 6;
    [Tooltip("Tamano de cada particula, en proporcion al ancho del icono (minimo y maximo).")]
    [SerializeField] private Vector2 tamanoParticula = new Vector2(0.14f, 0.22f);
    [Tooltip("Velocidad con la que salen, en proporcion al ancho del icono por segundo (minimo y maximo).")]
    [SerializeField] private Vector2 velocidad = new Vector2(0.9f, 1.5f);
    [Tooltip("Cuanto frenan (mas = frenan antes).")]
    [SerializeField] private float frenado = 3.5f;
    [Tooltip("Segundos que dura cada particula (minimo y maximo).")]
    [SerializeField] private Vector2 vida = new Vector2(0.55f, 0.85f);

    private struct Particula
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float tamano, dura, giro;
    }

    private RectTransform rtDestello;
    private Image imgDestello;
    private Particula[] particulas;
    private float t;
    private float fin;

    private void Start()
    {
        var rt = (RectTransform)transform;
        Vector2 caja = rt.rect.size;
        float ancho = Mathf.Max(1f, Mathf.Min(caja.x, caja.y));

        // que nada salga del icono
        if (GetComponent<RectMask2D>() == null) gameObject.AddComponent<RectMask2D>();

        // 1. destello
        imgDestello = NuevaImagen("Destello", destello, out rtDestello);
        rtDestello.sizeDelta = Vector2.one * ancho * tamanoDestello;
        fin = duracionDestello;

        // 2. particulas, saliendo parejas para todos lados (con un poco de azar)
        particulas = new Particula[Mathf.Max(0, cantidad)];
        float giroInicial = Random.value * 360f;
        for (int i = 0; i < particulas.Length; i++)
        {
            var p = new Particula();
            p.img = NuevaImagen("Particula " + (i + 1), particula, out p.rt);
            float ang = (giroInicial + i * 360f / particulas.Length + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
            p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(velocidad.x, velocidad.y) * ancho;
            p.tamano = Random.Range(tamanoParticula.x, tamanoParticula.y) * ancho;
            p.dura = Random.Range(vida.x, vida.y);
            p.giro = Random.Range(-180f, 180f);
            p.rt.anchoredPosition = p.vel.normalized * ancho * 0.05f;
            p.rt.sizeDelta = Vector2.one * p.tamano;
            particulas[i] = p;
            fin = Mathf.Max(fin, p.dura);
        }
        Actualizar(0f);
    }

    private Image NuevaImagen(string nombre, Sprite sprite, out RectTransform rt)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private void Update()
    {
        // sin escala de tiempo: se ve igual aunque el juego frene al recibir el golpe
        float dt = Time.unscaledDeltaTime;
        t += dt;
        Actualizar(dt);
        if (t >= fin) Destroy(gameObject);
    }

    private void Actualizar(float dt)
    {
        // destello: crece rapido desde chiquito y se apaga
        float d = Mathf.Clamp01(t / duracionDestello);
        float crece = 1f - (1f - d) * (1f - d) * (1f - d);   // arranca rapido y frena
        rtDestello.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, crece);
        imgDestello.color = ConAlfa(color, (1f - d) * (1f - d));

        // particulas: se abren, frenan, se achican y se apagan
        float freno = Mathf.Exp(-frenado * dt);
        for (int i = 0; i < particulas.Length; i++)
        {
            var p = particulas[i];
            float k = Mathf.Clamp01(t / p.dura);
            p.vel *= freno;
            p.rt.anchoredPosition += p.vel * dt;
            p.rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
            p.rt.localRotation = Quaternion.Euler(0f, 0f, p.giro * t);
            // aparecen enseguida (justo despues del destello) y se van apagando
            float alfa = Mathf.Clamp01(t / 0.06f) * (1f - k * k);
            p.img.color = ConAlfa(color, alfa);
            particulas[i] = p;
        }
    }

    private static Color ConAlfa(Color c, float a) { c.a *= a; return c; }
}
