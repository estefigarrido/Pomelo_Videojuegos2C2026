using System.Collections;
using UnityEngine;

// Comportamiento de Umbrae (boss). Mientras la chica esta en la zona de combate:
//  - siempre la mira; si queda de espaldas, se teletransporta en el lugar para reaparecer mirandola.
//  - si ella se acerca (radioEscape), retrocede con "umbrae_movimiento1" unos 5 u sin dejar de mirarla.
//  - cada 'intervaloTeleport' segundos se teletransporta al otro punto (izquierdo <-> derecho).
//  - cada 'intervaloDisparo' segundos dispara una bola de ignorancia hacia ella.
// Hace una sola accion a la vez. La flotacion y la sombra las maneja LevitacionBoss.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(LevitacionBoss))]
public class UmbraeBoss : MonoBehaviour
{
    [Header("Zona de combate (solo actua si la chica esta adentro)")]
    [SerializeField] private Rect zonaCombate = new Rect(-20.5f, 22.8f, 43f, 10f);
    [Tooltip("Limites en X hasta donde puede retroceder al escapar.")]
    [SerializeField] private float xMinimo = -18.6f;
    [SerializeField] private float xMaximo = 20.7f;

    [Header("Quieto")]
    [SerializeField] private Sprite quieto;

    [Header("Escape (umbrae_movimiento1)")]
    [SerializeField] private Sprite[] framesEscape;
    [SerializeField] private float fpsEscape = 16f;
    [Tooltip("Cuanto avanzo la figura en cada frame (0 = inicio, 1 = final). Sale de medir los dibujos.")]
    [SerializeField] private float[] avanceEscape;
    [Tooltip("Distancia a la que la chica lo hace escapar (5 u = 500 px).")]
    [SerializeField] private float radioEscape = 5f;
    [Tooltip("Cuanto retrocede en cada escape (5 u = 500 px).")]
    [SerializeField] private float distanciaEscape = 5f;
    [Tooltip("Si no hay lugar para retroceder ni esto, se teletransporta al otro punto en vez de escapar.")]
    [SerializeField] private float escapeMinimo = 1.5f;
    [Tooltip("Segundos que la chica tiene que estar cerca antes de que escape (le da tiempo al latigo).")]
    [SerializeField] private float demoraEscape = 0.6f;
    [Tooltip("Segundos despues de un escape en los que no vuelve a escapar.")]
    [SerializeField] private float esperaEntreEscapes = 3f;

    [Header("Teleport (umbrae-teleport)")]
    [SerializeField] private Sprite[] framesTeleport;
    [SerializeField] private float fpsTeleport = 14f;
    [Tooltip("Segundos que queda invisible entre desaparecer y reaparecer.")]
    [SerializeField] private float pausaInvisible = 0.25f;
    [SerializeField] private float intervaloTeleport = 5f;
    [SerializeField] private Vector2 puntoIzquierdo = new Vector2(-14.6f, 23.9f);
    [SerializeField] private Vector2 puntoDerecho = new Vector2(15.7f, 23.9f);

    [Header("Disparo (umbrae-dispara)")]
    [SerializeField] private Sprite[] framesDisparo;
    [SerializeField] private float fpsDisparo = 12f;
    [Tooltip("Frame (empieza en 1) en el que sale la bola.")]
    [SerializeField] private int frameSalida = 4;
    [Tooltip("De donde sale la bola, relativo a Umbrae mirando a la derecha (la mano).")]
    [SerializeField] private Vector2 salida = new Vector2(1.75f, 3.35f);
    [SerializeField] private float intervaloDisparo = 5f;

    [Header("Bola de ignorancia")]
    [SerializeField] private Sprite[] framesBola;
    [SerializeField] private float fpsBola = 14f;
    [Tooltip("Estela negra desenfocada (como la del dash).")]
    [SerializeField] private Sprite estelaBola;
    [SerializeField] private float velocidadInicial = 2f;
    [SerializeField] private float velocidadMaxima = 11f;
    [SerializeField] private float aceleracion = 22f;
    [SerializeField] private float alcance = 30f;
    [Tooltip("Vida que le saca a la chica (su vida maxima es 100).")]
    [SerializeField] private float danio = 40f;
    [SerializeField] private float giroMaximo = 360f;
    [SerializeField] private float radio = 0.3f;
    [Tooltip("Tamano de la bola (y de su estela y su radio de choque). 1.7 = 70% mas grande que el dibujo original.")]
    [SerializeField] private float escalaBola = 1.7f;

    private SpriteRenderer dibujo;
    private LevitacionBoss levitacion;
    private Collider2D hitbox;
    private SaludPersonaje victima;
    private Transform objetivo;
    private Sprite spriteActual;
    private bool ocupado;
    private bool enCombate;
    private float proximoTeleport;
    private float proximoDisparo;
    private float proximoEscape;
    private float cercaDesde = -1f;
    private Vector3 posicionInicial;
    private bool mirabaIzquierda;

    public bool Ocupado => ocupado;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        levitacion = GetComponent<LevitacionBoss>();
        hitbox = GetComponent<Collider2D>();
        spriteActual = quieto != null ? quieto : dibujo.sprite;
    }

    private void Start()
    {
        victima = FindFirstObjectByType<SaludPersonaje>();
        if (victima != null) objetivo = victima.transform;
        posicionInicial = levitacion.PosicionBase;
        mirabaIzquierda = dibujo.flipX;
    }

    // Deja a Umbrae como al principio de la pelea (lugar, mirada, timers) y borra sus bolas en vuelo.
    public void Reiniciar()
    {
        StopAllCoroutines();
        ocupado = false;
        enCombate = false;
        cercaDesde = -1f;
        proximoEscape = 0f;
        levitacion.PosicionBase = posicionInicial;
        levitacion.VisibilidadSombra = 1f;
        dibujo.enabled = true;
        dibujo.flipX = mirabaIzquierda;
        spriteActual = quieto;
        if (hitbox != null) hitbox.enabled = true;
        foreach (var bola in FindObjectsByType<ProyectilIgnorancia>(FindObjectsSortMode.None)) Destroy(bola.gameObject);
    }

    private void Update()
    {
        if (objetivo == null) return;

        bool adentro = zonaCombate.Contains((Vector2)objetivo.position);
        if (adentro && !enCombate)
        {
            // recien entra: arranca la cuenta de los ataques
            proximoTeleport = Time.time + intervaloTeleport;
            proximoDisparo = Time.time + intervaloDisparo;
        }
        enCombate = adentro;
        if (!enCombate || ocupado) return;

        float dx = objetivo.position.x - levitacion.PosicionBase.x;
        float ladoChica = dx >= 0f ? 1f : -1f;

        // 1) de espaldas a la chica: se teletransporta en el lugar para reaparecer mirandola
        if (Mirando() != ladoChica && Mathf.Abs(dx) > 0.3f)
        {
            StartCoroutine(Teletransportar(levitacion.PosicionBase, false));
            return;
        }

        // 2) la chica se acerco (y se quedo cerca 'demoraEscape' segundos): retrocede lo que le deje
        //    la plataforma (si esta contra el borde, se va al otro punto)
        bool cerca = Mathf.Abs(dx) < radioEscape && Mathf.Abs(objetivo.position.y - (levitacion.PosicionBase.y + 1.2f)) < 4f;
        if (!cerca) cercaDesde = -1f;
        else if (cercaDesde < 0f) cercaDesde = Time.time;
        if (cerca && Time.time >= proximoEscape && Time.time - cercaDesde >= demoraEscape)
        {
            cercaDesde = -1f;
            float x = levitacion.PosicionBase.x;
            float lugar = ladoChica > 0f ? x - xMinimo : xMaximo - x;
            float distancia = Mathf.Min(distanciaEscape, lugar);
            if (distancia >= escapeMinimo) StartCoroutine(Escapar(-ladoChica, distancia));
            else StartCoroutine(Teletransportar(PuntoLejano(), true));
            return;
        }

        // 3) cada 'intervaloTeleport' segundos cambia de punto
        if (Time.time >= proximoTeleport)
        {
            StartCoroutine(Teletransportar(PuntoLejano(), true));
            return;
        }

        // 4) cada 5 segundos dispara
        if (Time.time >= proximoDisparo) StartCoroutine(Disparar());
    }

    private void LateUpdate()
    {
        // se pone al final del frame por si algo mas (un Animator) quisiera cambiar el dibujo
        if (spriteActual != null) dibujo.sprite = spriteActual;
    }

    // 1 = mira a la derecha (como el dibujo original), -1 = a la izquierda
    private float Mirando() => dibujo.flipX ? -1f : 1f;

    private void MirarHacia(float lado) => dibujo.flipX = lado < 0f;

    private void MirarALaChica()
    {
        if (objetivo != null) MirarHacia(objetivo.position.x >= levitacion.PosicionBase.x ? 1f : -1f);
    }

    // el punto (izquierdo o derecho) mas lejos de donde esta ahora
    private Vector3 PuntoLejano()
    {
        float x = levitacion.PosicionBase.x;
        Vector2 p = Mathf.Abs(x - puntoIzquierdo.x) > Mathf.Abs(x - puntoDerecho.x) ? puntoIzquierdo : puntoDerecho;
        return new Vector3(p.x, p.y, levitacion.PosicionBase.z);
    }

    // ---------------- acciones ----------------

    private IEnumerator Escapar(float direccion, float distancia)
    {
        ocupado = true;
        Vector3 inicio = levitacion.PosicionBase;
        float duracion = framesEscape.Length / Mathf.Max(1f, fpsEscape);
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            int i = Mathf.Min(framesEscape.Length - 1, Mathf.FloorToInt(t * fpsEscape));
            spriteActual = framesEscape[i];
            // la figura se dibuja centrada en su pivote y el cuerpo se mueve con el avance del dibujo
            // (retrocede mirando a la chica: el dibujo va para atras)
            float avance = avanceEscape != null && i < avanceEscape.Length ? avanceEscape[i] : (float)i / (framesEscape.Length - 1);
            levitacion.PosicionBase = inicio + new Vector3(direccion * distancia * avance, 0f, 0f);
            yield return null;
        }
        levitacion.PosicionBase = inicio + new Vector3(direccion * distancia, 0f, 0f);
        spriteActual = quieto;
        proximoEscape = Time.time + esperaEntreEscapes;
        ocupado = false;
    }

    private IEnumerator Teletransportar(Vector3 destino, bool cuentaComoCambioDePunto)
    {
        ocupado = true;
        float paso = 1f / Mathf.Max(1f, fpsTeleport);
        int n = framesTeleport.Length;

        // desaparece
        for (int i = 0; i < n; i++)
        {
            spriteActual = framesTeleport[i];
            levitacion.VisibilidadSombra = 1f - (i + 1f) / n;
            yield return new WaitForSeconds(paso);
        }
        dibujo.enabled = false;
        if (hitbox != null) hitbox.enabled = false;
        yield return new WaitForSeconds(pausaInvisible);

        // aparece en el destino, ya mirando a la chica, con la misma animacion al reves
        levitacion.PosicionBase = destino;
        MirarALaChica();
        dibujo.enabled = true;
        for (int i = n - 1; i >= 0; i--)
        {
            spriteActual = framesTeleport[i];
            levitacion.VisibilidadSombra = 1f - (float)i / n;
            yield return new WaitForSeconds(paso);
        }
        levitacion.VisibilidadSombra = 1f;
        if (hitbox != null) hitbox.enabled = true;
        spriteActual = quieto;
        if (cuentaComoCambioDePunto) proximoTeleport = Time.time + intervaloTeleport;
        ocupado = false;
    }

    private IEnumerator Disparar()
    {
        ocupado = true;
        MirarALaChica();
        float paso = 1f / Mathf.Max(1f, fpsDisparo);
        for (int i = 0; i < framesDisparo.Length; i++)
        {
            spriteActual = framesDisparo[i];
            if (i + 1 == frameSalida) LanzarBola();
            yield return new WaitForSeconds(paso);
        }
        spriteActual = quieto;
        proximoDisparo = Time.time + intervaloDisparo;
        ocupado = false;
    }

    private void LanzarBola()
    {
        if (framesBola == null || framesBola.Length == 0 || objetivo == null) return;
        float lado = Mirando();
        Vector3 origen = transform.position + new Vector3(salida.x * lado, salida.y, 0f);

        // apunta al centro de la chica
        var colChica = victima.GetComponent<Collider2D>();
        Vector2 blanco = colChica != null ? (Vector2)colChica.bounds.center : (Vector2)objetivo.position;
        Vector2 direccion = blanco - (Vector2)origen;
        if (direccion.sqrMagnitude < 0.0001f) direccion = new Vector2(lado, 0f);

        var raiz = new GameObject("BolaDeIgnorancia");
        raiz.transform.position = origen;
        raiz.transform.localScale = Vector3.one * escalaBola;

        var bolaGo = new GameObject("Bola");
        bolaGo.transform.SetParent(raiz.transform, false);
        var srBola = bolaGo.AddComponent<SpriteRenderer>();
        srBola.sprite = framesBola[0];
        srBola.sortingLayerID = dibujo.sortingLayerID;
        srBola.sortingOrder = dibujo.sortingOrder + 3;
        srBola.sharedMaterial = dibujo.sharedMaterial;

        SpriteRenderer srEstela = null;
        if (estelaBola != null)
        {
            var estelaGo = new GameObject("Estela");
            estelaGo.transform.SetParent(raiz.transform, false);
            srEstela = estelaGo.AddComponent<SpriteRenderer>();
            srEstela.sprite = estelaBola;
            srEstela.sortingLayerID = dibujo.sortingLayerID;
            srEstela.sortingOrder = dibujo.sortingOrder + 2;
            srEstela.sharedMaterial = dibujo.sharedMaterial;
        }

        raiz.AddComponent<ProyectilIgnorancia>().Iniciar(direccion, velocidadInicial, velocidadMaxima, aceleracion, alcance, danio,
            giroMaximo, radio * escalaBola, hitbox, srBola, framesBola, fpsBola, srEstela);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireCube(zonaCombate.center, zonaCombate.size);
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(puntoIzquierdo, 0.4f);
        Gizmos.DrawWireSphere(puntoDerecho, 0.4f);
        Gizmos.color = new Color(0.6f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f, radioEscape);
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position + (Vector3)salida, 0.2f);
    }
}
