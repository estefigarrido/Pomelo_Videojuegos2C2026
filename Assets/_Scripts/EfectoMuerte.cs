using System.Collections.Generic;
using UnityEngine;

// Pasa los frames una vez y se borra.
// Con pedacitos (muerte de los mobs): al llegar al ultimo frame, la animacion se corta y sus pedacitos
// vuelan haciendo una curva hasta el circulo de sabiduria del HUD. Cuando llegan todos, se suma la
// sabiduria del golpe que lo mato (asi el liquido sube justo despues de que entran).
public class EfectoMuerte : MonoBehaviour
{
    private const float SegundosVuelo = 0.85f;
    private const float RetrasoMaximo = 0.18f;

    private Sprite[] frames;
    private float fps;
    private float inicio;
    private SpriteRenderer sr;

    private Sprite[] piezas;
    private float sabiduria;
    private bool volando;
    private float inicioVuelo;
    private Camera camara;

    private class Pieza { public Transform t; public SpriteRenderer sr; public Vector3 desde, control; public float retraso, escala; }
    private readonly List<Pieza> enVuelo = new List<Pieza>();

    public void Iniciar(Sprite[] frames, float fps)
    {
        this.frames = frames;
        this.fps = Mathf.Max(1f, fps);
        inicio = Time.time;
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
    }

    public void Iniciar(Sprite[] frames, float fps, Sprite[] piezas, float sabiduria)
    {
        Iniciar(frames, fps);
        this.piezas = piezas;
        this.sabiduria = sabiduria;
        var seguidora = FindFirstObjectByType<CamaraSeguidora>();
        camara = seguidora != null ? seguidora.GetComponent<Camera>() : Camera.main;
    }

    private void Update()
    {
        if (volando) { Volar(); return; }

        int i = Mathf.FloorToInt((Time.time - inicio) * fps);
        if (i >= frames.Length)
        {
            if (piezas != null && piezas.Length > 0 && camara != null && HUDVidaSabiduria.Instancia != null) { Soltar(); return; }
            if (piezas != null) VidaEnemigo.SumarSabiduriaAlLlegar(sabiduria);
            Destroy(gameObject);
            return;
        }
        sr.sprite = frames[i];
    }

    // el ultimo frame se reemplaza por sus pedacitos sueltos, en el mismo lugar
    private void Soltar()
    {
        volando = true;
        inicioVuelo = Time.time;
        sr.enabled = false;
        foreach (var p in piezas)
        {
            var go = new GameObject("pedacito");
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<SpriteRenderer>();
            s.sprite = p;
            s.sharedMaterial = sr.sharedMaterial;
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = 300;   // por encima de todo mientras vuela
            s.flipX = sr.flipX;
            // el centro del pedacito (el sprite es del tamano del frame entero)
            Vector3 centro = transform.TransformPoint(new Vector3(sr.flipX ? -p.bounds.center.x : p.bounds.center.x, p.bounds.center.y, 0f));
            var pieza = new Pieza { t = go.transform, sr = s, desde = centro, retraso = Random.Range(0f, RetrasoMaximo), escala = transform.localScale.x };
            // la curva sale hacia arriba y un poco para afuera antes de ir al circulo
            pieza.control = centro + new Vector3((centro.x - transform.position.x) * 1.5f + Random.Range(-0.6f, 0.6f), Random.Range(1.2f, 2.2f), 0f);
            enVuelo.Add(pieza);
        }
    }

    private void Volar()
    {
        Vector3 destino = PuntoDelCirculo();
        bool quedan = false;
        foreach (var p in enVuelo)
        {
            float u = Mathf.Clamp01((Time.time - inicioVuelo - p.retraso) / SegundosVuelo);
            if (u < 1f) quedan = true;
            float k = u * u * (3f - 2f * u);
            // Bezier cuadratica: sale hacia afuera y entra al circulo
            Vector3 a = Vector3.Lerp(p.desde, p.control, k), b = Vector3.Lerp(p.control, destino, k);
            Vector3 centro = Vector3.Lerp(a, b, k);
            float esc = p.escala * Mathf.Lerp(1f, 0.25f, k);
            p.t.localScale = Vector3.one * (esc / Mathf.Max(0.0001f, transform.localScale.x));
            // el pedacito se dibuja alrededor de su centro (el sprite trae el desplazamiento del frame)
            var desp = new Vector3(p.sr.flipX ? -p.sr.sprite.bounds.center.x : p.sr.sprite.bounds.center.x, p.sr.sprite.bounds.center.y, 0f) * esc;
            p.t.position = centro - desp;
            p.sr.color = new Color(1f, 1f, 1f, u < 0.85f ? 1f : 1f - (u - 0.85f) / 0.15f);
        }
        if (quedan) return;
        VidaEnemigo.SumarSabiduriaAlLlegar(sabiduria);
        Destroy(gameObject);
    }

    // el circulo del HUD esta en pantalla: se pasa a coordenadas del mundo con la camara del juego
    private Vector3 PuntoDelCirculo()
    {
        Vector2 pantalla = HUDVidaSabiduria.Instancia.PuntoDelCirculoEnPantalla();
        var p = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, Mathf.Abs(camara.transform.position.z - transform.position.z)));
        p.z = transform.position.z;
        return p;
    }
}
