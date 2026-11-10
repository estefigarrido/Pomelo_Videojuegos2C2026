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

    [Header("Chispas de los fuegos")]
    [SerializeField] private float chispasPorSegundo = 7f;
    [SerializeField] private Vector2 vidaChispas = new Vector2(0.9f, 1.8f);
    [SerializeField] private Vector2 velocidadChispas = new Vector2(0.8f, 1.8f);
    [SerializeField] private Vector2 tamanoChispas = new Vector2(0.06f, 0.16f);
    [SerializeField] private Color colorChispas = new Color(0.83f, 0.68f, 1f, 1f);

    [Header("Saturacion del templo")]
    [Tooltip("Partes del templo con el material 'Pomelo/Sprite Saturacion': desaturadas antes de la pelea.")]
    [SerializeField] private SpriteRenderer[] templo;
    [Range(0f, 1f)] [SerializeField] private float saturacionAntes = 0.3f;
    [Tooltip("Saturacion del templo durante la pelea (0.6 = 40 % menos que los colores originales).")]
    [Range(0f, 1f)] [SerializeField] private float saturacionPelea = 0.6f;
    [Tooltip("Exposicion del templo antes de la pelea (mas oscuro); durante la pelea es 1.")]
    [Range(0f, 1f)] [SerializeField] private float exposicionAntes = 0.55f;
    [Tooltip("Exposicion del templo durante la pelea (0.687 = el oscurecido del mapa triste del Figma).")]
    [Range(0f, 1.5f)] [SerializeField] private float exposicionPelea = 1f;
    [SerializeField] private float segundosSaturar = 1.5f;

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
    private float velocidadGuardar;

    // Umbrae empieza a atacar recien cuando esto es verdadero.
    public bool Terminado => estado == Estado.Lista;

    private void Awake()
    {
        if (zona == null) zona = FindFirstObjectByType<ZonaCamaraFija>();

        // las piezas de la decoracion: cada hijo directo, segun su nombre y su altura.
        // Los hijos sin dibujo son grupos (por ejemplo "pinchos_base_umbrae1"): se miran sus piezas de adentro.
        var piezasDecoracion = new List<Transform>();
        foreach (Transform hijo in transform)
        {
            if (hijo.GetComponent<SpriteRenderer>() != null) piezasDecoracion.Add(hijo);
            else foreach (Transform nieto in hijo) piezasDecoracion.Add(nieto);
        }
        foreach (Transform hijo in piezasDecoracion)
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
            if (llamasFondo != null) foreach (var o in llamasFondo) CopiarFuegoFondo(o, Tipo.Fuego);
            if (halosFondo != null) foreach (var o in halosFondo) CopiarFuegoFondo(o, Tipo.Halo);
            if (romboOjo != null) Agregar(romboOjo, Tipo.Rombo, romboOjo.GetComponent<SpriteRenderer>());
        }

        CalcularHorario();
        if (Application.isPlaying)
        {
            foreach (var p in piezas) Aplicar(p, 0f);
            Saturar(saturacionAntes);
            CrearSpriteChispa();
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

    // los fuegos de las columnas retorcidas del templo se animan tal cual estan en la escena: lo que queda
    // delante o detras de la columna lo decide el orden de capas (sorting layer y order), sin recortes ni copias
    private void CopiarFuegoFondo(SpriteRenderer original, Tipo tipo)
    {
        if (original == null) return;
        Agregar(original.transform, tipo, original);
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

        // el templo se satura cuando empieza la pelea y se desatura cuando termina
        float objetivo = estado == Estado.Armando || estado == Estado.Lista ? saturacionPelea : saturacionAntes;
        float nueva = Mathf.MoveTowards(saturacionActual, objetivo, Time.deltaTime * Mathf.Max(0.05f, saturacionPelea - saturacionAntes) / Mathf.Max(0.05f, segundosSaturar));
        if (!Mathf.Approximately(nueva, saturacionActual)) Saturar(nueva);

        ActualizarChispas();
    }

    // ---------- saturacion del templo ----------

    private float saturacionActual = -1f;
    private MaterialPropertyBlock bloque;

    private void Saturar(float s)
    {
        saturacionActual = s;
        if (templo == null) return;
        if (bloque == null) bloque = new MaterialPropertyBlock();
        foreach (var sr in templo)
        {
            if (sr == null) continue;
            sr.GetPropertyBlock(bloque);
            bloque.SetFloat("_Saturacion", s);
            // se ilumina junto con la saturacion: oscuro antes de la pelea, normal durante
            bloque.SetFloat("_Exposicion", Exposicion(s));
            sr.SetPropertyBlock(bloque);
        }
        // las luces (los resplandores de los fuegos) y las llamas de las columnas retorcidas del fondo
        // (las copias con el material de recorte) se desaturan igual que las columnas
        foreach (var p in piezas)
        {
            if (p.dibujos == null) continue;
            bool llamaFondo = p.tipo == Tipo.Fuego && p.delFondo;
            if (p.tipo != Tipo.Halo && !llamaFondo) continue;
            foreach (var sr in p.dibujos)
            {
                if (sr == null) continue;
                sr.GetPropertyBlock(bloque);
                bloque.SetFloat("_Saturacion", s);
                bloque.SetFloat("_Exposicion", Exposicion(s));
                sr.SetPropertyBlock(bloque);
            }
        }
    }

    // la exposicion acompana a la saturacion: de la de antes de la pelea a la de la pelea
    private float Exposicion(float s) => Mathf.Approximately(saturacionAntes, saturacionPelea)
        ? (estado == Estado.Armando || estado == Estado.Lista ? exposicionPelea : exposicionAntes)
        : Mathf.Lerp(exposicionAntes, exposicionPelea, Mathf.InverseLerp(saturacionAntes, saturacionPelea, s));

    // ---------- chispas que salen de los fuegos ----------

    private class Chispa { public Transform t; public SpriteRenderer sr; public float vida, dur, vel, x0, fase, tam, deriva; }
    private readonly List<Chispa> chispas = new List<Chispa>();
    private readonly Stack<Chispa> chispasLibres = new Stack<Chispa>();
    private Sprite spriteChispa;
    private readonly Dictionary<Pieza, float> acumuladoChispas = new Dictionary<Pieza, float>();

    // rombito blanco suave (las chispas son facetadas, como el resto del arte)
    private void CrearSpriteChispa()
    {
        const int tam = 32;
        var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[tam * tam];
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / tam * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / tam * 2f - 1f);
                float d = dx * 1.6f + dy;                                   // rombo alargado para arriba
                float a = Mathf.Clamp01((1f - d) * 3f);
                px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px); tex.Apply();
        spriteChispa = Sprite.Create(tex, new Rect(0, 0, tam, tam), new Vector2(0.5f, 0.5f), tam);
    }

    private void ActualizarChispas()
    {
        if (spriteChispa == null) return;
        foreach (var p in piezas)
        {
            if (p.tipo != Tipo.Fuego || p.progreso < 0.6f || p.dibujos.Length == 0 || !p.dibujos[0].enabled) continue;
            float acc; acumuladoChispas.TryGetValue(p, out acc);
            acc += chispasPorSegundo * Time.deltaTime;
            while (acc >= 1f) { acc -= 1f; SoltarChispa(p); }
            acumuladoChispas[p] = acc;
        }
        for (int i = chispas.Count - 1; i >= 0; i--)
        {
            var c = chispas[i];
            c.vida += Time.deltaTime;
            float u = c.vida / c.dur;
            if (u >= 1f) { c.t.gameObject.SetActive(false); chispas.RemoveAt(i); chispasLibres.Push(c); continue; }
            var pos = c.t.position;
            pos.y += c.vel * Time.deltaTime * (1f - u * 0.5f);
            pos.x = c.x0 + Mathf.Sin(c.vida * 3f + c.fase) * 0.18f + c.deriva * c.vida;
            c.t.position = pos;
            c.t.localScale = Vector3.one * c.tam * (1f - u * 0.7f);
            c.t.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(c.vida * 4f + c.fase) * 20f);
            var col = colorChispas; col.a *= Mathf.Clamp01(Mathf.Min(u * 8f, (1f - u) * 2.5f)) * (0.7f + 0.3f * Mathf.Sin(c.vida * 20f + c.fase));
            c.sr.color = col;
        }
    }

    private void SoltarChispa(Pieza fuego)
    {
        var c = chispasLibres.Count > 0 ? chispasLibres.Pop() : NuevaChispa();
        var b = fuego.dibujos[0].bounds;
        c.x0 = b.center.x + Random.Range(-0.3f, 0.3f) * b.size.x;
        c.t.position = new Vector3(c.x0, Mathf.Lerp(b.min.y, b.max.y, Random.Range(0.45f, 0.8f)), fuego.t.position.z - 0.01f);
        c.vel = Random.Range(velocidadChispas.x, velocidadChispas.y);
        c.dur = Random.Range(vidaChispas.x, vidaChispas.y);
        c.tam = Random.Range(tamanoChispas.x, tamanoChispas.y) / spriteChispa.bounds.size.x;
        c.fase = Random.value * 10f;
        c.deriva = Random.Range(-0.25f, 0.25f);
        c.vida = 0f;
        c.sr.sortingLayerID = fuego.dibujos[0].sortingLayerID;
        c.sr.sortingOrder = fuego.dibujos[0].sortingOrder + 1;
        c.t.gameObject.SetActive(true);
        chispas.Add(c);
    }

    private Chispa NuevaChispa()
    {
        var go = new GameObject("chispa fuego");
        go.transform.SetParent(transform, true);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteChispa;
        return new Chispa { t = go.transform, sr = sr };
    }

    private void Empezar()
    {
        CalcularHorario();
        estado = Estado.Armando;
        tiempo = 0f;
        CorrerChicaDeLasColumnas();
    }

    private void Guardar()
    {
        estado = Estado.Guardando;
        velocidadGuardar = 1f / Mathf.Max(0.05f, segundosGuardar);
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
                Titilar(p, flickerAlfaHalo, 4.5f);
                break;
        }
    }

    // la llama se mece (gira un poquito y se estira) desde su base, que queda quieta sobre la columna
    private void MoverLlama(Pieza p, float tamano)
    {
        float t = Time.time + p.fase;
        float sx = 1f + wiggleEscala * Mathf.Sin(t * 2.1f);
        float sy = 1f + wiggleEscala * 1.5f * Mathf.Sin(t * 2.7f + 1.3f);
        float ang = wiggleGrados * (Mathf.Sin(t * 1.5f) * 0.7f + Mathf.Sin(t * 3.4f + 0.6f) * 0.3f);
        var giro = Quaternion.Euler(0f, 0f, ang);
        var estirar = new Vector3(sx, sy, 1f);
        p.t.localScale = Vector3.Scale(p.escala * tamano, estirar);
        p.t.rotation = p.giro * giro;
        Vector3 aLaBase = new Vector3(0f, p.baseLlama * tamano, 0f);
        p.t.position = p.pos + aLaBase - giro * Vector3.Scale(aLaBase, estirar);
        Titilar(p, flickerAlfaFuego, 2.5f);
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
