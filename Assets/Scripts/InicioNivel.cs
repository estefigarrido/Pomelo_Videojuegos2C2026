using System.Collections;
using UnityEngine;

// Comienzo del nivel en el portal de entrada:
//  1) la chica esta adentro del portal, detras de la puerta cerrada, y no se puede controlar.
//  2) a los 5 segundos de empezar, la puerta sube (sin asomarse por arriba del portal) y desaparece;
//     a medida que sube se ve a la chica de frente (chica_salidaportal 000).
//  3) 2 segundos despues de que termina de abrirse, la chica se gira de perfil (chica_salidaportal)
//     y recien ahi se le devuelven los controles: empieza el juego.
public class InicioNivel : MonoBehaviour
{
    [Tooltip("Apagalo para probar el nivel desde otro lado sin la intro.")]
    [SerializeField] private bool activa = true;

    [Header("Portal y puerta")]
    [SerializeField] private SpriteRenderer portal;
    [SerializeField] private SpriteRenderer puerta;

    [Header("La chica")]
    [Tooltip("Donde esta parada la chica adentro del portal.")]
    [SerializeField] private Vector2 posicionChica = new Vector2(-32f, -29f);
    [Tooltip("chica_salidaportal: 000 de frente ... el ultimo de perfil.")]
    [SerializeField] private Sprite[] framesSalida;
    [Tooltip("Cuanto tarda en girarse de perfil (los frames se funden entre si).")]
    [SerializeField] private float duracionGiro = 0.35f;

    [Header("Tiempos")]
    [SerializeField] private float esperaInicial = 5f;
    [SerializeField] private float duracionSubidaPuerta = 2.5f;
    [SerializeField] private float esperaDespuesDeAbrir = 2f;

    private Transform chica;
    private SpriteRenderer dibujoChica;
    private SpriteMask mascaraChica;

    private void Start()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (!activa || salud == null || puerta == null) return;
        chica = salud.transform;
        dibujoChica = salud.GetComponent<SpriteRenderer>();
        StartCoroutine(Secuencia());
    }

    private IEnumerator Secuencia()
    {
        // sin controles hasta que termine la intro
        var mov = chica.GetComponent<MovimientoPersonaje>();
        var latigo = chica.GetComponent<AtaqueLatigo>();
        var sabiduria = chica.GetComponent<AtaqueSabiduria>();
        var animator = chica.GetComponent<Animator>();
        var rb = chica.GetComponent<Rigidbody2D>();
        if (mov != null) mov.enabled = false;
        if (latigo != null) latigo.enabled = false;
        if (sabiduria != null) sabiduria.enabled = false;
        if (animator != null) animator.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        // parada adentro del portal, de frente, mirando hacia la derecha al terminar
        chica.position = new Vector3(posicionChica.x, posicionChica.y, chica.position.z);
        var escala = chica.localScale; escala.x = Mathf.Abs(escala.x); chica.localScale = escala;
        MostrarChica(framesSalida != null && framesSalida.Length > 0 ? framesSalida[0] : null);

        // la puerta solo se ve por debajo del borde de arriba del portal; la chica solo por fuera de la puerta
        CrearMascaraPuerta();
        CrearMascaraChica();

        yield return new WaitForSeconds(esperaInicial);

        // la puerta sube y desaparece
        Vector3 cerrada = puerta.transform.position;
        Vector3 abierta = cerrada + Vector3.up * puerta.bounds.size.y * 1.05f;
        for (float t = 0f; t < duracionSubidaPuerta; t += Time.deltaTime)
        {
            puerta.transform.position = Vector3.Lerp(cerrada, abierta, Mathf.SmoothStep(0f, 1f, t / duracionSubidaPuerta));
            AcomodarMascaraChica();
            yield return null;
        }
        puerta.transform.position = abierta;
        puerta.enabled = false;
        if (dibujoChica != null) dibujoChica.maskInteraction = SpriteMaskInteraction.None;
        if (mascaraChica != null) Destroy(mascaraChica.gameObject);

        yield return new WaitForSeconds(esperaDespuesDeAbrir);

        // se gira de perfil
        yield return GiroSuave.Reproducir(dibujoChica, framesSalida, duracionGiro);

        // empieza el juego
        if (rb != null) { rb.simulated = true; rb.linearVelocity = Vector2.zero; }
        if (animator != null) animator.enabled = true;
        if (mov != null) mov.enabled = true;
        if (latigo != null) latigo.enabled = true;
        if (sabiduria != null) sabiduria.enabled = true;
    }

    private void MostrarChica(Sprite s)
    {
        if (s != null && dibujoChica != null) dibujoChica.sprite = s;
    }

    private static SpriteMask NuevaMascara(string nombre, int atras, int adelante)
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color32[16];
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        var go = new GameObject(nombre);
        var m = go.AddComponent<SpriteMask>();
        m.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f); // 1x1 unidad
        // solo afecta a los dibujos con orden entre "atras" (sin incluir) y "adelante"
        m.isCustomRangeActive = true;
        m.backSortingOrder = atras;
        m.frontSortingOrder = adelante;
        return m;
    }

    private void CrearMascaraPuerta()
    {
        if (portal == null) return;
        var m = NuevaMascara("Mascara puerta entrada", puerta.sortingOrder - 1, puerta.sortingOrder);
        m.backSortingLayerID = m.frontSortingLayerID = puerta.sortingLayerID;
        Bounds b = portal.bounds;
        float arriba = b.max.y, abajo = b.min.y - 6f;
        m.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        m.transform.localScale = new Vector3(b.size.x + 2f, arriba - abajo, 1f);
        puerta.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    private void CrearMascaraChica()
    {
        if (dibujoChica == null) return;
        mascaraChica = NuevaMascara("Mascara chica", dibujoChica.sortingOrder - 1, dibujoChica.sortingOrder);
        mascaraChica.backSortingLayerID = mascaraChica.frontSortingLayerID = dibujoChica.sortingLayerID;
        dibujoChica.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        AcomodarMascaraChica();
    }

    // tapa a la chica en el hueco de la puerta (y un poco mas abajo, hasta el piso)
    private void AcomodarMascaraChica()
    {
        if (mascaraChica == null) return;
        Bounds b = puerta.bounds;
        float arriba = b.max.y, abajo = b.min.y - 0.6f;
        mascaraChica.transform.position = new Vector3(b.center.x, (arriba + abajo) / 2f, 0f);
        mascaraChica.transform.localScale = new Vector3(b.size.x, arriba - abajo, 1f);
    }
}
