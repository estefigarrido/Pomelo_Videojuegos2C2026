using System.Collections.Generic;
using UnityEngine;

// La estatua botanica empieza gris y con enredaderas. Cuando la chica llega con la gema al ultimo
// escalon de la escalera, en 'duracion' segundos la estatua recupera su color dorado y las enredaderas
// se desvanecen; al terminar aparecen destellos titilando sobre la estatua.
// El dibujo dorado original queda abajo; encima se ponen la copia gris y las enredaderas.
[RequireComponent(typeof(SpriteRenderer))]
public class EstatuaRestaurada : MonoBehaviour
{
    [System.Serializable]
    public struct Colocacion
    {
        [Tooltip("Posicion en el mundo.")]
        public Vector2 posicion;
        [Tooltip("Tamano en el mundo respecto del sprite (negativo = espejado).")]
        public Vector2 escala;
    }

    [Header("Estado inicial")]
    [SerializeField] private Sprite estatuaGris;
    [SerializeField] private Sprite enredadera;
    [SerializeField] private Colocacion[] enredaderas;

    [Header("Cuando llega la chica con la gema")]
    [Tooltip("X del ultimo escalon: cuando la chica (con la gema) llega aca o mas a la izquierda, empieza.")]
    [SerializeField] private float xUltimoEscalon = -73.1f;
    [Tooltip("La chica tiene que estar arriba de la plataforma (Y minima).")]
    [SerializeField] private float yMinima = 33.5f;
    [SerializeField] private float duracion = 4f;

    [Header("Destellos al terminar")]
    [SerializeField] private Sprite[] framesDestello;
    [SerializeField] private float fpsDestello = 14f;
    [SerializeField] private Colocacion[] destellos;
    [SerializeField] private Material materialBrillo;

    private SpriteRenderer dibujo;
    private SpriteRenderer gris;
    private readonly List<SpriteRenderer> plantas = new List<SpriteRenderer>();
    private Transform chica;
    private int estado; // 0 gris, 1 recuperando el color, 2 dorada con destellos
    private float inicio;

    private class Destello { public SpriteRenderer sr; public float proximo; public float inicio; }
    private readonly List<Destello> activos = new List<Destello>();

    private void Start()
    {
        dibujo = GetComponent<SpriteRenderer>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) chica = salud.transform;

        // copia gris exactamente encima de la dorada
        if (estatuaGris != null)
        {
            gris = CrearDibujo("Estatua gris", estatuaGris, dibujo.sortingOrder + 1, dibujo.sharedMaterial);
            gris.transform.SetParent(transform, false);
        }
        if (enredadera != null && enredaderas != null)
            foreach (var c in enredaderas)
            {
                var sr = CrearDibujo("Enredadera", enredadera, dibujo.sortingOrder + 2, dibujo.sharedMaterial);
                sr.transform.SetParent(transform, true);
                sr.transform.position = new Vector3(c.posicion.x, c.posicion.y, transform.position.z);
                sr.transform.localScale = new Vector3(c.escala.x / transform.lossyScale.x, c.escala.y / transform.lossyScale.y, 1f);
                plantas.Add(sr);
            }
    }

    private SpriteRenderer CrearDibujo(string nombre, Sprite sprite, int orden, Material material)
    {
        var go = new GameObject(nombre);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = orden;
        if (material != null) sr.sharedMaterial = material;
        return sr;
    }

    private void Update()
    {
        if (estado == 0)
        {
            if (chica != null && GemaEnMano.TieneGema && chica.position.x <= xUltimoEscalon && chica.position.y >= yMinima)
            {
                estado = 1;
                inicio = Time.time;
            }
            return;
        }

        if (estado == 1)
        {
            float k = Mathf.Clamp01((Time.time - inicio) / Mathf.Max(0.01f, duracion));
            SetAlfa(gris, 1f - k);
            foreach (var p in plantas) SetAlfa(p, 1f - k);
            if (k >= 1f)
            {
                if (gris != null) Destroy(gris.gameObject);
                foreach (var p in plantas) Destroy(p.gameObject);
                plantas.Clear();
                CrearDestellos();
                estado = 2;
            }
            return;
        }

        AnimarDestellos();
    }

    private static void SetAlfa(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        var c = sr.color; c.a = a; sr.color = c;
    }

    private void CrearDestellos()
    {
        if (framesDestello == null || framesDestello.Length == 0 || destellos == null) return;
        foreach (var c in destellos)
        {
            var sr = CrearDibujo("Destello", framesDestello[0], dibujo.sortingOrder + 3, materialBrillo != null ? materialBrillo : dibujo.sharedMaterial);
            sr.transform.SetParent(transform, true);
            sr.transform.position = new Vector3(c.posicion.x, c.posicion.y, transform.position.z);
            sr.transform.localScale = new Vector3(c.escala.x / transform.lossyScale.x, c.escala.y / transform.lossyScale.y, 1f);
            sr.enabled = false;
            // cada destello arranca en un momento distinto, asi titilan desparejo
            activos.Add(new Destello { sr = sr, proximo = Time.time + Random.Range(0f, 0.8f) });
        }
    }

    // cada destello crece (001 -> 005), se achica (005 -> 001) y espera un rato antes de volver a brillar
    private void AnimarDestellos()
    {
        int n = framesDestello.Length;
        int largo = n * 2 - 1;
        foreach (var d in activos)
        {
            if (d.inicio <= 0f)
            {
                if (Time.time < d.proximo) continue;
                d.inicio = Time.time;
                d.sr.enabled = true;
            }
            int f = Mathf.FloorToInt((Time.time - d.inicio) * fpsDestello);
            if (f >= largo)
            {
                d.sr.enabled = false;
                d.inicio = 0f;
                d.proximo = Time.time + Random.Range(0.3f, 1.3f);
                continue;
            }
            d.sr.sprite = framesDestello[f < n ? f : largo - 1 - f];
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
        Gizmos.DrawLine(new Vector3(xUltimoEscalon, yMinima - 1f), new Vector3(xUltimoEscalon, yMinima + 4f));
    }
}
