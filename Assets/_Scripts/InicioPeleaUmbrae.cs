using System.Collections.Generic;
using UnityEngine;

// "Inicio de pelea Umbrae". Va en el objeto con la decoracion de la pelea. Antes de la pelea no hay nada de esto
// en la arena; cuando la chica entra (ZonaCamaraFija.PeleaEnCurso, que tambien prende la musica) se arma asi:
//  1) las dos columnas suben desde el piso (con un collider que no se puede atravesar) y los capiteles con la
//     cabeza de Umbrae bajan desde arriba del mapa.
//  2) se prenden los fuegos de las columnas (upscale rapido) y medio segundo despues crece el circulo violeta.
//  3) los pinchos del techo bajan y los del piso salen; a la vez se prenden los fuegos de las torres del fondo.
//  4) aparece el rombo del ojo con un upscale.
//  Entre cada parte hay 'pausaEntrePartes' (2 s). La camara tiembla fuerte (0,8) mientras pasa algo y suave (0,3)
//  en las pausas; deja de temblar cuando termina todo y Umbrae empieza a atacar (UmbraeBoss espera a 'Terminado').
// Cuando la pelea termina (se gana y se agarra la gema, o la chica pierde y vuelve al spawnpoint) se guarda
// todo al reves, asi la proxima vez se vuelve a armar.
public class InicioPeleaUmbrae : MonoBehaviour
{
    private enum Tipo { Columna, Capitel, Techo, PinchoPiso, Fuego, Halo, Rombo }

    private class Pieza
    {
        public Transform t;
        public Tipo tipo;
        public Vector3 pos;
        public Vector3 escala;
        public Quaternion giro;
        public float fase;           // para que cada fuego se mueva a su ritmo
        public float baseLlama;      // donde esta la base de la llama respecto de su pivote (mundo)
        public Color[] colores;
        public float distancia;      // cuanto viaja al entrar (las que se mueven)
        public float inicio, duracion;
        public float progreso;       // 0 = guardada, 1 = en su lugar
        public bool delFondo;        // copia de un fuego de las torres del fondo
        public SpriteRenderer[] dibujos;
        public Collider2D[] cols;
    }

    [SerializeField] private ZonaCamaraFija zona;

    [Header("De donde entran")]
    [Tooltip("Altura del piso de la arena (los pinchos y columnas salen desde el piso que tienen debajo).")]
    [SerializeField] private float piso = 38.59f;
    [Tooltip("Las piezas de arriba bajan desde esta altura (arriba del borde de la camara de la pelea).")]
    [SerializeField] private float alturaArriba = 68.5f;

    [Header("Ritmo y temblor")]
    [Tooltip("Segundos entre una parte de la animacion y la siguiente.")]
    [SerializeField] private float pausaEntrePartes = 2f;
    [Tooltip("Temblor de la camara mientras pasa alguna animacion.")]
    [SerializeField] private float temblorAnimacion = 0.8f;
    [Tooltip("Temblor de la camara en las pausas entre animaciones.")]
    [SerializeField] private float temblorPausa = 0.3f;

    [Header("Columnas y capiteles")]
    [SerializeField] private float segundosColumnas = 1.5f;

    [Header("Fuegos")]
    [Tooltip("Upscale de la llama (menos de un segundo).")]
    [SerializeField] private float segundosFuego = 0.45f;
    [Tooltip("El circulo violeta empieza a crecer este tiempo despues que la llama.")]
    [SerializeField] private float retrasoHalo = 0.5f;
    [SerializeField] private float segundosHalo = 0.5f;
    [Tooltip("Cuanto gira la llama al mecerse (grados).")]
    [SerializeField] private float wiggleGrados = 3f;
    [Tooltip("Cuanto se estira y encoge la llama al mecerse (0,05 = 5 %).")]
    [SerializeField] private float wiggleEscala = 0.05f;
    [Tooltip("Cuanto baja la opacidad de la llama al titilar.")]
    [Range(0f, 1f)] [SerializeField] private float flickerAlfaFuego = 0.15f;
    [Tooltip("Cuanto se agranda y achica el circulo violeta al titilar (0,05 = 5 %).")]
    [SerializeField] private float flickerEscalaHalo = 0.05f;
    [Tooltip("Cuanto baja la opacidad del circulo violeta al titilar.")]
    [Range(0f, 1f)] [SerializeField] private float flickerAlfaHalo = 0.25f;

    [Header("Pinchos")]
    [SerializeField] private float segundosPinchos = 0.8f;
    [Tooltip("Cada pincho arranca con un desfase al azar de hasta esto.")]
    [SerializeField] private float desfasePinchos = 0.25f;

    [Header("Fuegos de las torres del fondo")]
    [Tooltip("Llamas de las torres del templo (se copian por encima del fondo desenfocado).")]
    [SerializeField] private SpriteRenderer[] llamasFondo;
    [Tooltip("Circulos violetas de las torres del templo.")]
    [SerializeField] private SpriteRenderer[] halosFondo;
    [Tooltip("El fondo desenfocado del templo: sin los fuegos antes de la pelea, con los fuegos despues.")]
    [SerializeField] private SpriteRenderer fondoBlur;
    [SerializeField] private Sprite blurApagado;
    [SerializeField] private Sprite blurPrendido;
    [Tooltip("Tono de las copias (el blur oscurece un poco lo que tapa).")]
    [Range(0.5f, 1f)] [SerializeField] private float tonoFondo = 0.94f;
    [Tooltip("Llamas de las torres recortadas donde las tapa el cuenco (izquierda, derecha): se mueven encima del fondo.")]
    [SerializeField] private Sprite[] llamasFondoRecortadas;
    [Tooltip("Opacidad de los circulos copiados cuando el fondo ya tiene los circulos (solo agregan el titileo).")]
    [Range(0f, 1f)] [SerializeField] private float alfaHaloSobreFondo = 0.3f;
    [SerializeField] private int ordenHalosFondo = -95;
    [SerializeField] private int ordenLlamasFondo = -94;

    [Header("Ojo")]
    [Tooltip("El rombo del ojo de Umbrae (aparece con un upscale al final).")]
    [SerializeField] private Transform romboOjo;
    [SerializeField] private float segundosOjo = 0.5f;
    [Tooltip("Pausa despues de que aparece el ojo, antes de que Umbrae ataque.")]
    [SerializeField] private float pausaFinal = 0.3f;

    [Header("Guardar al terminar la pelea")]
    [SerializeField] private float segundosGuardar = 0.8f;

    [Tooltip("Si esta activado, la decoracion queda a la vista en el editor (para acomodarla).")]
    [SerializeField] private bool verEnElEditor = true;

    private readonly List<Pieza> piezas = new List<Pieza>();
    private CamaraSeguidora camara;
    private Collider2D colChica;
    private enum Estado { Guardada, Armando, Lista, Guardando }
    private Estado estado = Estado.Guardada;
    private float tiempo;
    private float finArmado;
    private float finHalosFondo;
    private bool blurPrendidoPuesto;
    private float velocidadGuardar;

    // Umbrae empieza a atacar recien cuando esto es verdadero.
    public bool Terminado => estado == Estado.Lista;

    private void Awake()
    {
        if (zona == null) zona = FindFirstObjectByType<ZonaCamaraFija>();

        // las piezas de la decoracion: cada hijo directo, segun su nombre y su altura
        foreach (Transform hijo in transform)
        {
            var sr = hijo.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            string n = hijo.name;
            bool arriba = hijo.position.y > 55f;
            Tipo tipo;
            if (n.StartsWith("pelea_columna")) tipo = Tipo.Columna;
            else if (n.StartsWith("pelea_capitel") || (n.StartsWith("pelea_cristales") && arriba)) tipo = Tipo.Capitel;
            else if (n.StartsWith("pelea_banda_techo") || (n.StartsWith("pincho") && arriba)) tipo = Tipo.Techo;
            else if (n.StartsWith("pincho") || n.StartsWith("pelea_cristales")) tipo = Tipo.PinchoPiso;
            else if (n.StartsWith("Fuego_llama")) tipo = Tipo.Fuego;
            else if (n.StartsWith("Fuego_bright")) tipo = Tipo.Halo;
            else continue;
            Agregar(hijo, tipo, sr);
        }

        // copias de los fuegos de las torres del fondo, por encima del fondo desenfocado
        if (Application.isPlaying)
        {
            if (llamasFondo != null)
                for (int i = 0; i < llamasFondo.Length; i++)
                    CopiarFuegoFondo(llamasFondo[i], Tipo.Fuego, ordenLlamasFondo,
                        llamasFondoRecortadas != null && i < llamasFondoRecortadas.Length ? llamasFondoRecortadas[i] : null);
            if (halosFondo != null) foreach (var o in halosFondo) CopiarFuegoFondo(o, Tipo.Halo, ordenHalosFondo, null);
            if (romboOjo != null) Agregar(romboOjo, Tipo.Rombo, romboOjo.GetComponent<SpriteRenderer>());
        }

        CalcularHorario();
        if (Application.isPlaying)
        {
            foreach (var p in piezas) Aplicar(p, 0f);
            if (fondoBlur != null && blurApagado != null) fondoBlur.sprite = blurApagado;
        }
    }

    private void Start()
    {
        camara = FindFirstObjectByType<CamaraSeguidora>();
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) colChica = salud.GetComponent<Collider2D>();
    }

    private void Agregar(Transform t, Tipo tipo, SpriteRenderer propio)
    {
        var p = new Pieza
        {
            t = t,
            tipo = tipo,
            pos = t.position,
            escala = t.localScale,
            dibujos = t.GetComponentsInChildren<SpriteRenderer>(true),
            cols = t.GetComponentsInChildren<Collider2D>(true),
            giro = t.rotation,
            fase = Random.Range(0f, 100f),
        };
        p.colores = new Color[p.dibujos.Length];
        for (int i = 0; i < p.dibujos.Length; i++) p.colores[i] = p.dibujos[i].color;
        Bounds b = propio != null ? propio.bounds : new Bounds(t.position, Vector3.zero);
        p.baseLlama = b.min.y - t.position.y;
        if (tipo == Tipo.Columna || tipo == Tipo.PinchoPiso)
            p.distancia = Mathf.Max(0f, b.max.y - PisoDebajo(b.center.x) + 0.15f);      // sale de abajo del piso
        else if (tipo == Tipo.Capitel || tipo == Tipo.Techo)
            p.distancia = Mathf.Max(0f, alturaArriba - b.min.y);                          // baja desde arriba
        piezas.Add(p);
    }

    private void CopiarFuegoFondo(SpriteRenderer original, Tipo tipo, int orden, Sprite otroSprite)
    {
        if (original == null) return;
        var go = new GameObject(original.name + " (pelea)");
        go.transform.SetParent(original.transform.parent, false);
        go.transform.position = original.transform.position;
        go.transform.rotation = original.transform.rotation;
        go.transform.localScale = original.transform.localScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = otroSprite != null ? otroSprite : original.sprite;
        sr.sharedMaterial = original.sharedMaterial;
        sr.maskInteraction = original.maskInteraction;
        sr.sortingLayerID = original.sortingLayerID;
        sr.sortingOrder = orden;
        var c = original.color;
        sr.color = new Color(c.r * tonoFondo, c.g * tonoFondo, c.b * tonoFondo, c.a);
        Agregar(go.transform, tipo, sr);
        piezas[piezas.Count - 1].delFondo = true;
    }

    // el piso solido debajo de x (cerca del piso de la arena); si no encuentra, el piso de la arena
    private float PisoDebajo(float x)
    {
        foreach (var h in Physics2D.RaycastAll(new Vector2(x, piso + 2f), Vector2.down, 6f))
        {
            var c = h.collider;
            if (c.isTrigger || c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<SaludPersonaje>() != null || c.GetComponentInParent<VidaEnemigo>() != null) continue;
            return h.point.y;
        }
        return piso;
    }

    // cuando arranca y cuanto dura cada pieza
    // cuando arranca y cuanto dura cada pieza: 4 partes con 'pausaEntrePartes' entre una y otra
    private void CalcularHorario()
    {
        // parte 1: columnas y capiteles
        float finParte1 = segundosColumnas;
        // parte 2: fuegos de las columnas (la llama y despues su circulo)
        float inicioFuegos = finParte1 + pausaEntrePartes;
        float finParte2 = inicioFuegos + Mathf.Max(segundosFuego, retrasoHalo + segundosHalo);
        // parte 3: pinchos y fuegos de las torres del fondo
        float inicioPinchos = finParte2 + pausaEntrePartes;
        float finParte3 = inicioPinchos + Mathf.Max(desfasePinchos + segundosPinchos, retrasoHalo + segundosHalo);
        // parte 4: el rombo del ojo
        float inicioOjo = finParte3 + pausaEntrePartes;

        finHalosFondo = 0f;
        foreach (var p in piezas)
        {
            bool delFondo = p.delFondo;
            float inicioFuego = delFondo ? inicioPinchos : inicioFuegos;
            switch (p.tipo)
            {
                case Tipo.Columna:
                case Tipo.Capitel:
                    p.inicio = 0f; p.duracion = segundosColumnas; break;
                case Tipo.Fuego:
                    p.inicio = inicioFuego; p.duracion = segundosFuego; break;
                case Tipo.Halo:
                    p.inicio = inicioFuego + retrasoHalo; p.duracion = segundosHalo;
                    if (delFondo) finHalosFondo = Mathf.Max(finHalosFondo, p.inicio + p.duracion);
                    break;
                case Tipo.Techo:
                case Tipo.PinchoPiso:
                    p.inicio = inicioPinchos + Random.Range(0f, desfasePinchos); p.duracion = segundosPinchos; break;
                case Tipo.Rombo:
                    p.inicio = inicioOjo; p.duracion = segundosOjo; break;
            }
        }
        finArmado = 0f;
        foreach (var p in piezas) finArmado = Mathf.Max(finArmado, p.inicio + p.duracion);
        finArmado += pausaFinal;
    }

    // si alguna pieza se esta animando en este momento
    private bool AlgoSeAnima()
    {
        foreach (var p in piezas)
            if (tiempo >= p.inicio && tiempo <= p.inicio + p.duracion) return true;
        return false;
    }

    private void Update()
    {
        bool pelea = zona != null && zona.PeleaEnCurso;

        switch (estado)
        {
            case Estado.Guardada:
                if (pelea) Empezar();
                break;

            case Estado.Armando:
                if (!pelea) { Guardar(); break; }
                tiempo += Time.deltaTime;
                foreach (var p in piezas)
                {
                    float u = p.duracion > 0f ? Mathf.Clamp01((tiempo - p.inicio) / p.duracion) : (tiempo >= p.inicio ? 1f : 0f);
                    // antes de su turno no esta en la arena
                    if (tiempo < p.inicio) { Aplicar(p, 0f); continue; }
                    Aplicar(p, Mathf.Max(u, 0.0001f));
                }
                if (!blurPrendidoPuesto && finHalosFondo > 0f && tiempo >= finHalosFondo) PrenderFondo(true);
                // tiembla fuerte mientras algo se anima y suave en las pausas (se renueva cada cuadro, asi no se apaga)
                if (camara != null) camara.Temblar(0.12f, AlgoSeAnima() ? temblorAnimacion : temblorPausa);
                if (tiempo >= finArmado) estado = Estado.Lista;   // termina el temblor y empieza la pelea
                break;

            case Estado.Lista:
                if (!pelea) { Guardar(); break; }
                // los fuegos siguen moviendose y titilando durante la pelea
                foreach (var p in piezas) if (p.tipo == Tipo.Fuego || p.tipo == Tipo.Halo) Aplicar(p, 1f);
                break;

            case Estado.Guardando:
                bool queda = false;
                foreach (var p in piezas)
                {
                    float u = Mathf.MoveTowards(p.progreso, 0f, Time.deltaTime * velocidadGuardar);
                    Aplicar(p, u);
                    if (u > 0f) queda = true;
                }
                if (!queda) estado = Estado.Guardada;
                break;
        }
    }

    private void Empezar()
    {
        CalcularHorario();
        estado = Estado.Armando;
        tiempo = 0f;
        blurPrendidoPuesto = false;
        CorrerChicaDeLasColumnas();
    }

    private void Guardar()
    {
        estado = Estado.Guardando;
        velocidadGuardar = 1f / Mathf.Max(0.05f, segundosGuardar);
        // las copias de los fuegos del fondo vuelven a verse para achicarse; el fondo vuelve a estar apagado
        PrenderFondo(false);
    }

    private void PrenderFondo(bool prendido)
    {
        blurPrendidoPuesto = prendido;
        if (fondoBlur != null)
        {
            var s = prendido ? blurPrendido : blurApagado;
            if (s != null) fondoBlur.sprite = s;
        }
        // con el fondo prendido (que ya tiene los circulos) las llamas copiadas siguen moviendose encima, y los
        // circulos copiados quedan suaves, solo para que titilen
    }

    // u: 0 = guardada (fuera de la arena), 1 = en su lugar
    private void Aplicar(Pieza p, float u)
    {
        if (p.t == null) return;
        p.progreso = u;
        bool visible = u > 0f;
        foreach (var d in p.dibujos) if (d != null) d.enabled = visible;
        foreach (var c in p.cols) if (c != null) c.enabled = u > 0f;

        switch (p.tipo)
        {
            case Tipo.Columna:
            case Tipo.PinchoPiso:
                p.t.position = p.pos + Vector3.down * p.distancia * (1f - SalidaSuave(u));
                break;
            case Tipo.Capitel:
            case Tipo.Techo:
                p.t.position = p.pos + Vector3.up * p.distancia * (1f - SalidaSuave(u));
                break;
            case Tipo.Rombo:
                p.t.localScale = p.escala * ConRebote(u);
                break;
            case Tipo.Fuego:
                MoverLlama(p, ConRebote(u));
                break;
            case Tipo.Halo:
                // el circulo de luz se agranda y achica un poco, como una luz que titila
                float t = Time.time + p.fase;
                float latido = 1f + flickerEscalaHalo * (2f * Mathf.PerlinNoise(t * 2.2f, p.fase) - 1f) + flickerEscalaHalo * 0.35f * Mathf.Sin(t * 13f);
                p.t.localScale = p.escala * SalidaSuave(u) * latido;
                Titilar(p, flickerAlfaHalo, 4.5f, p.delFondo && blurPrendidoPuesto ? alfaHaloSobreFondo : 1f);
                break;
        }
    }

    // la llama se mece (gira un poquito y se estira) desde su base, que queda quieta sobre la columna
    private void MoverLlama(Pieza p, float tamano)
    {
        float t = Time.time + p.fase;
        float sx = 1f + wiggleEscala * Mathf.Sin(t * 7.3f);
        float sy = 1f + wiggleEscala * 1.5f * Mathf.Sin(t * 9.1f + 1.3f);
        float ang = wiggleGrados * (Mathf.Sin(t * 5.2f) * 0.7f + Mathf.Sin(t * 11.7f + 0.6f) * 0.3f);
        var giro = Quaternion.Euler(0f, 0f, ang);
        var estirar = new Vector3(sx, sy, 1f);
        p.t.localScale = Vector3.Scale(p.escala * tamano, estirar);
        p.t.rotation = p.giro * giro;
        Vector3 aLaBase = new Vector3(0f, p.baseLlama * tamano, 0f);
        p.t.position = p.pos + aLaBase - giro * Vector3.Scale(aLaBase, estirar);
        Titilar(p, flickerAlfaFuego, 6f);
    }

    // baja y sube un poco la opacidad al azar, como una luz que titila
    private void Titilar(Pieza p, float cuanto, float velocidad, float extra = 1f)
    {
        float k = (1f - cuanto * Mathf.PerlinNoise((Time.time + p.fase) * velocidad, p.fase * 0.37f)) * extra;
        for (int i = 0; i < p.dibujos.Length; i++)
        {
            if (p.dibujos[i] == null) continue;
            var c = p.colores[i];
            p.dibujos[i].color = new Color(c.r, c.g, c.b, c.a * k);
        }
    }

    private static float SalidaSuave(float u) => 1f - (1f - u) * (1f - u) * (1f - u);

    // crece pasandose un poquito y vuelve (upscale con rebote)
    private static float ConRebote(float u)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float x = u - 1f;
        return Mathf.Max(0f, 1f + c3 * x * x * x + c1 * x * x);
    }

    // si la chica quedo justo donde sube una columna, la corre hacia el centro de la arena
    private void CorrerChicaDeLasColumnas()
    {
        if (colChica == null) return;
        foreach (var p in piezas)
        {
            if (p.tipo != Tipo.Columna) continue;
            foreach (var c in p.cols)
            {
                if (!(c is BoxCollider2D box)) continue;
                Vector2 centro = (Vector2)p.pos + Vector2.Scale(box.offset, (Vector2)box.transform.lossyScale) + ((Vector2)box.transform.position - (Vector2)p.t.position);
                Vector2 tam = Vector2.Scale(box.size, (Vector2)box.transform.lossyScale);
                Bounds b = colChica.bounds;
                float x0 = centro.x - tam.x / 2f, x1 = centro.x + tam.x / 2f;
                if (b.max.x <= x0 || b.min.x >= x1 || b.min.y > centro.y + tam.y / 2f) continue;
                var t = colChica.attachedRigidbody != null ? colChica.attachedRigidbody.transform : colChica.transform;
                bool aLaIzquierda = centro.x > MedioDeLasColumnas();
                float mover = aLaIzquierda ? x0 - b.max.x - 0.05f : x1 - b.min.x + 0.05f;
                t.position += new Vector3(mover, 0f, 0f);
            }
        }
    }

    private float MedioDeLasColumnas()
    {
        float suma = 0f; int n = 0;
        foreach (var p in piezas) if (p.tipo == Tipo.Columna && !p.t.name.Contains("linea")) { suma += p.pos.x; n++; }
        return n > 0 ? suma / n : transform.position.x;
    }

    private void OnValidate()
    {
        // en el editor (sin jugar) se puede ver u ocultar para acomodarla
        if (Application.isPlaying) return;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = verEnElEditor;
    }
}
