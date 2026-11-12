using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Comportamiento de Umbrae (boss), armado como el ciclo de las Mantis Lords de Hollow Knight:
//   teletransporte (el lugar donde aparece avisa que ataque viene) -> ataque -> pausa (hueco para pegarle)
//   -> elige el proximo ataque. Hace una sola cosa a la vez.
// Ataques:
//  - Disparo: aparece en un costado (el punto izquierdo o derecho, el mas lejos de la chica) y tira una bola
//    de ignorancia que la persigue.
//  - Rayos: aparece en el medio de la arena y se eleva 'segundosSubida' (1,5 s): los primeros 'segundosSinBrazos'
//    (1 s) sube dos tercios sin abrir los brazos, y el resto abre los brazos (con estela y temblor de pantalla) y
//    termina de subir 'alturaSubida' (100 px del Figma) sobre su altura normal. Mientras sube brilla con el
//    degradado violeta, no se le puede pegar y recupera el 10 % de la vida. En el piso se anticipan los rayos
//    y al terminar de subir caen. Hay 4 variantes de lugares; 1 de cada 3 veces cae un rayo justo debajo de Umbrae
//    (garantizado: sale de una bolsa de 3), asi pararse abajo suyo no siempre es seguro.
// Eleccion del ataque: al azar, nunca mas de 'maximoRepetidos' iguales seguidos, y si los rayos no salieron
// en los ultimos 'rayosComoMaximoCada' ataques, salen si o si.
// Durante las pausas reacciona: si queda de espaldas a la chica se teletransporta en el lugar para mirarla, y
// si ella se le acerca retrocede (o se va al otro punto si no tiene lugar).
// Fase 2 (vida <= 'vidaFase2'): las pausas, el teletransporte y la espera entre escapes se acortan.
// La flotacion y la sombra las maneja LevitacionBoss.
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(LevitacionBoss))]
public class UmbraeBoss : MonoBehaviour
{
    [System.Serializable]
    public class VarianteRayos
    {
        public string nombre;
        [Tooltip("Donde cae cada rayo: fraccion del ancho de la arena (0 = borde izquierdo, 1 = borde derecho).")]
        public float[] posiciones;
    }

    private enum Ataque { Disparo, Rayos }

    [Header("Zona de combate (solo actua si la chica esta adentro; el borde de abajo es el piso)")]
    [SerializeField] private Rect zonaCombate = new Rect(-20.5f, 22.8f, 43f, 10f);
    [Tooltip("Limites en X hasta donde puede moverse (escapar o aparecer).")]
    [SerializeField] private float xMinimo = -18.6f;
    [SerializeField] private float xMaximo = 20.7f;

    [Header("Quieto")]
    [SerializeField] private Sprite quieto;

    [Header("Ciclo de ataques")]
    [Tooltip("La animacion de inicio de la pelea: Umbrae no ataca hasta que termina.")]
    [SerializeField] private InicioPeleaUmbrae inicioPelea;
    [Tooltip("Segundos desde que termina el inicio de la pelea hasta el primer ataque.")]
    [SerializeField] private float pausaInicial = 0.6f;
    [Tooltip("Segundos entre un ataque y el siguiente: es el hueco para pegarle.")]
    [SerializeField] private float pausaEntreAtaques = 0.9f;
    [Tooltip("Chance de elegir rayos (si no lo fuerzan las reglas).")]
    [Range(0f, 1f)] [SerializeField] private float probabilidadRayos = 0.45f;
    [Tooltip("Nunca hace el mismo ataque mas de esta cantidad de veces seguidas.")]
    [SerializeField] private int maximoRepetidos = 2;
    [Tooltip("Si los rayos no salieron en esta cantidad de ataques, salen si o si.")]
    [SerializeField] private int rayosComoMaximoCada = 3;

    [Header("Fase 2")]
    [Tooltip("Fraccion de la vida en la que empieza la fase 2.")]
    [Range(0f, 1f)] [SerializeField] private float vidaFase2 = 0.5f;
    [Tooltip("Las pausas, el teletransporte y la espera entre escapes se multiplican por esto en la fase 2.")]
    [Range(0.2f, 1f)] [SerializeField] private float factorTiemposFase2 = 0.6f;

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
    [SerializeField] private float fpsTeleport = 28f;
    [Tooltip("Segundos que queda invisible entre desaparecer y reaparecer.")]
    [SerializeField] private float pausaInvisible = 0.125f;
    [SerializeField] private Vector2 puntoIzquierdo = new Vector2(-14.6f, 23.9f);
    [SerializeField] private Vector2 puntoDerecho = new Vector2(15.7f, 23.9f);

    [Header("Disparo (umbrae-dispara)")]
    [SerializeField] private Sprite[] framesDisparo;
    [SerializeField] private float fpsDisparo = 24f;
    [Tooltip("Frame (empieza en 1) en el que sale la bola.")]
    [SerializeField] private int frameSalida = 4;
    [Tooltip("De donde sale la bola, relativo a Umbrae mirando a la derecha (la mano), a escala 1.")]
    [SerializeField] private Vector2 salida = new Vector2(1.75f, 3.35f);

    [Header("Bola de ignorancia")]
    [SerializeField] private Sprite[] framesBola;
    [SerializeField] private float fpsBola = 14f;
    [Tooltip("Estela negra desenfocada (como la del dash).")]
    [SerializeField] private Sprite estelaBola;
    [SerializeField] private float velocidadInicial = 6f;
    [SerializeField] private float velocidadMaxima = 33f;
    [SerializeField] private float aceleracion = 66f;
    [SerializeField] private float alcance = 30f;
    [Tooltip("Vida que le saca a la chica (su vida maxima es 100).")]
    [SerializeField] private float danio = 40f;
    [SerializeField] private float giroMaximo = 360f;
    [SerializeField] private float radio = 0.3f;
    [Tooltip("Tamano de la bola (y de su estela y su radio de choque). 1.7 = 70% mas grande que el dibujo original.")]
    [SerializeField] private float escalaBola = 1.7f;

    [Header("Rayos: Umbrae se eleva (umbrae-levita)")]
    [SerializeField] private Sprite[] framesLevita;
    [Tooltip("Segundos desde que empieza a subir hasta que caen los rayos.")]
    [SerializeField] private float segundosSubida = 1.5f;
    [Tooltip("Segundos que sube sin abrir los brazos (el resto abre los brazos).")]
    [SerializeField] private float segundosSinBrazos = 1f;
    [Tooltip("Parte de la altura que sube sin abrir los brazos.")]
    [Range(0f, 1f)] [SerializeField] private float fraccionSinBrazos = 2f / 3f;
    [Tooltip("Cuanto sube por encima de su altura normal hasta que caen los rayos (1 u = 100 px del Figma).")]
    [SerializeField] private float alturaSubida = 1f;
    [Tooltip("Parte de la vida maxima que recupera mientras sube.")]
    [Range(0f, 1f)] [SerializeField] private float curacion = 0.1f;
    [Tooltip("Segundos que tarda en bajar despues de los rayos.")]
    [SerializeField] private float segundosBajada = 0.25f;
    [Tooltip("Temblor de la pantalla mientras abre los brazos.")]
    [SerializeField] private float intensidadTemblor = 0.12f;
    [Tooltip("Temblor corto cuando caen los rayos.")]
    [SerializeField] private float intensidadTemblorImpacto = 0.22f;

    [Header("Rayos: brillo y estela")]
    [Tooltip("Material con el shader 'Pomelo/Sprite Glow Gradiente' (degradado 8B5CC7 -> transparente -> B48CF0).")]
    [SerializeField] private Material materialGlow;
    [Tooltip("Mismo shader con radio chico: la estela de los brazos.")]
    [SerializeField] private Material materialEstela;
    [SerializeField] private float intervaloEstela = 0.025f;
    [SerializeField] private float vidaEstela = 0.15f;
    [Range(0f, 1f)] [SerializeField] private float alfaEstela = 0.6f;

    [Header("Rayos: anticipacion en el piso (umbrae-rayo-anticipacion)")]
    [SerializeField] private Sprite[] framesAnticipacion;
    [SerializeField] private float fpsAnticipacion = 24f;
    [SerializeField] private int ordenAnticipacion = -1;

    [Header("Rayos: el rayo (umbrae-rayo)")]
    [SerializeField] private Sprite[] framesRayo;
    [SerializeField] private float fpsRayo = 30f;
    [Tooltip("Frame (empieza en 1) en el que el rayo toca el piso: coincide con el final de la subida.")]
    [SerializeField] private int frameImpacto = 7;
    [Tooltip("Ultimo frame (empieza en 1) en el que el rayo lastima.")]
    [SerializeField] private int frameFinDanio = 11;
    [Tooltip("Zona que lastima de cada rayo: ancho y alto desde el piso.")]
    [SerializeField] private Vector2 tamanoDanioRayo = new Vector2(1.6f, 8.5f);
    [Tooltip("Vida que le saca a la chica (una vez por ataque).")]
    [SerializeField] private float danioRayo = 40f;
    [SerializeField] private int ordenRayos = 5;
    [SerializeField] private VarianteRayos[] variantesRayos =
    {
        new VarianteRayos { nombre = "A (la del Figma)", posiciones = new[] { 0.13f, 0.37f, 0.66f, 0.91f } },
        new VarianteRayos { nombre = "B centro cerrado", posiciones = new[] { 0.25f, 0.42f, 0.58f, 0.75f } },
        new VarianteRayos { nombre = "C extremos y centro", posiciones = new[] { 0.08f, 0.50f, 0.70f, 0.92f } },
        new VarianteRayos { nombre = "D corrida a un lado", posiciones = new[] { 0.05f, 0.28f, 0.52f, 0.76f } },
    };

    private SpriteRenderer dibujo;
    private LevitacionBoss levitacion;
    private VidaEnemigo vida;
    private Collider2D hitbox;
    private SaludPersonaje victima;
    private Transform objetivo;
    private CamaraSeguidora camara;
    private Sprite spriteActual;
    private SpriteRenderer glow;
    private float alfaGlow;
    private bool ocupado;
    private bool enCombate;
    private Coroutine ciclo;
    private float proximoEscape;
    private float cercaDesde = -1f;
    private Vector3 posicionInicial;
    private bool mirabaIzquierda;

    private readonly List<Ataque> historial = new List<Ataque>();
    private readonly List<bool> bolsaDebajo = new List<bool>();
    private int ultimaVariante = -1;
    private readonly List<GameObject> temporales = new List<GameObject>();

    public bool Ocupado => ocupado;
    // mientras sube a tirar los rayos no se le puede pegar (la barra de vida brilla)
    public bool Protegido => protegido;
    private bool protegido;

    private float Escala => Mathf.Abs(transform.lossyScale.x);
    private bool Fase2 => vida != null && vida.Vida <= vida.VidaMaxima * vidaFase2;
    private float Factor => Fase2 ? factorTiemposFase2 : 1f;
    private float Piso => zonaCombate.y;

    private void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        levitacion = GetComponent<LevitacionBoss>();
        vida = GetComponent<VidaEnemigo>();
        hitbox = GetComponent<Collider2D>();
        spriteActual = quieto != null ? quieto : dibujo.sprite;
    }

    private void Start()
    {
        victima = FindFirstObjectByType<SaludPersonaje>();
        if (victima != null) objetivo = victima.transform;
        camara = FindFirstObjectByType<CamaraSeguidora>();
        posicionInicial = levitacion.PosicionBase;
        mirabaIzquierda = dibujo.flipX;
        CrearGlow();
    }

    // Deja a Umbrae como al principio de la pelea (lugar, mirada, ciclo) y borra sus bolas y rayos.
    public void Reiniciar()
    {
        protegido = false;
        StopAllCoroutines();
        ciclo = null;
        ocupado = false;
        enCombate = false;
        cercaDesde = -1f;
        proximoEscape = 0f;
        historial.Clear();
        bolsaDebajo.Clear();
        levitacion.PosicionBase = posicionInicial;
        levitacion.Elevacion = 0f;
        levitacion.FactorOnda = 1f;
        levitacion.VisibilidadSombra = 1f;
        dibujo.enabled = true;
        dibujo.flipX = mirabaIzquierda;
        spriteActual = quieto;
        alfaGlow = 0f;
        if (hitbox != null) hitbox.enabled = true;
        BorrarTemporales();
        foreach (var bola in FindObjectsByType<ProyectilIgnorancia>(FindObjectsSortMode.None)) Destroy(bola.gameObject);
    }

    private void Update()
    {
        // mientras se arma la arena y se carga su barra de vida no se le puede hacer dano
        if (vida != null) vida.Invulnerable = inicioPelea != null && inicioPelea.Armando;
        if (objetivo == null) return;
        enCombate = zonaCombate.Contains((Vector2)objetivo.position);
        // recien entra (y termino el inicio de la pelea): arranca el ciclo de ataques (sigue mientras dure la pelea)
        bool listo = inicioPelea == null || inicioPelea.Terminado;
        if (enCombate && listo && ciclo == null) ciclo = StartCoroutine(Ciclo());
    }

    private void LateUpdate()
    {
        // se pone al final del frame por si algo mas (un Animator) quisiera cambiar el dibujo
        if (spriteActual != null) dibujo.sprite = spriteActual;
        if (glow != null)
        {
            glow.sprite = dibujo.sprite;
            glow.flipX = dibujo.flipX;
            glow.enabled = dibujo.enabled && alfaGlow > 0.001f;
            glow.color = new Color(1f, 1f, 1f, alfaGlow);
        }
    }

    // 1 = mira a la derecha (como el dibujo original), -1 = a la izquierda
    private float Mirando() => dibujo.flipX ? -1f : 1f;

    private void MirarHacia(float lado) => dibujo.flipX = lado < 0f;

    private void MirarALaChica()
    {
        if (objetivo != null) MirarHacia(objetivo.position.x >= levitacion.PosicionBase.x ? 1f : -1f);
    }

    private Vector3 EnElPiso(float x) => new Vector3(Mathf.Clamp(x, xMinimo, xMaximo), puntoIzquierdo.y, levitacion.PosicionBase.z);

    // el punto (izquierdo o derecho) mas lejos de donde esta ahora
    private Vector3 PuntoLejano()
    {
        float x = levitacion.PosicionBase.x;
        return EnElPiso(Mathf.Abs(x - puntoIzquierdo.x) > Mathf.Abs(x - puntoDerecho.x) ? puntoIzquierdo.x : puntoDerecho.x);
    }

    // el punto (izquierdo o derecho) mas lejos de la chica
    private Vector3 PuntoLejosDeLaChica()
    {
        float x = objetivo != null ? objetivo.position.x : levitacion.PosicionBase.x;
        return EnElPiso(Mathf.Abs(x - puntoIzquierdo.x) > Mathf.Abs(x - puntoDerecho.x) ? puntoIzquierdo.x : puntoDerecho.x);
    }

    // ---------------- ciclo ----------------

    private IEnumerator Ciclo()
    {
        yield return Pausa(pausaInicial);
        while (true)
        {
            while (!enCombate) yield return null;
            if (ElegirAtaque() == Ataque.Rayos) yield return AtaqueRayos();
            else yield return AtaqueDisparo();
            yield return Pausa(pausaEntreAtaques * Factor);
        }
    }

    private Ataque ElegirAtaque()
    {
        Ataque elegido = Random.value < probabilidadRayos ? Ataque.Rayos : Ataque.Disparo;

        // nunca mas de 'maximoRepetidos' iguales seguidos
        int n = historial.Count;
        if (n >= maximoRepetidos)
        {
            bool repetido = true;
            for (int i = n - maximoRepetidos; i < n; i++) if (historial[i] != elegido) repetido = false;
            if (repetido) elegido = elegido == Ataque.Rayos ? Ataque.Disparo : Ataque.Rayos;
        }
        // si los rayos no salieron en los ultimos 'rayosComoMaximoCada' ataques, salen si o si
        if (n >= rayosComoMaximoCada)
        {
            bool sinRayos = true;
            for (int i = n - rayosComoMaximoCada; i < n; i++) if (historial[i] == Ataque.Rayos) sinRayos = false;
            if (sinRayos) elegido = Ataque.Rayos;
        }

        historial.Add(elegido);
        if (historial.Count > 8) historial.RemoveAt(0);
        return elegido;
    }

    // Espera entre ataques; mientras tanto reacciona a la chica (se da vuelta o retrocede).
    private IEnumerator Pausa(float segundos)
    {
        float fin = Time.time + segundos;
        while (Time.time < fin)
        {
            if (enCombate && objetivo != null)
            {
                float dx = objetivo.position.x - levitacion.PosicionBase.x;
                float ladoChica = dx >= 0f ? 1f : -1f;

                // de espaldas a la chica: se teletransporta en el lugar para reaparecer mirandola
                if (Mirando() != ladoChica && Mathf.Abs(dx) > 0.3f)
                {
                    yield return Teletransportar(levitacion.PosicionBase);
                    continue;
                }

                // la chica se acerco (y se quedo cerca 'demoraEscape' segundos): retrocede lo que le deje
                // la plataforma (si esta contra el borde, se va al otro punto)
                bool cerca = Mathf.Abs(dx) < radioEscape && Mathf.Abs(objetivo.position.y - (levitacion.PosicionBase.y + 1.2f * Escala)) < 4f * Escala;
                if (!cerca) cercaDesde = -1f;
                else if (cercaDesde < 0f) cercaDesde = Time.time;
                if (cerca && Time.time >= proximoEscape && Time.time - cercaDesde >= demoraEscape)
                {
                    cercaDesde = -1f;
                    float x = levitacion.PosicionBase.x;
                    float lugar = ladoChica > 0f ? x - xMinimo : xMaximo - x;
                    float distancia = Mathf.Min(distanciaEscape, lugar);
                    if (distancia >= escapeMinimo) yield return Escapar(-ladoChica, distancia);
                    else yield return Teletransportar(PuntoLejano());
                    continue;
                }
            }
            yield return null;
        }
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
        proximoEscape = Time.time + esperaEntreEscapes * Factor;
        ocupado = false;
    }

    private IEnumerator Teletransportar(Vector3 destino)
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
        yield return new WaitForSeconds(pausaInvisible * Factor);

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
        ocupado = false;
    }

    // ---------------- ataque: disparo ----------------

    private IEnumerator AtaqueDisparo()
    {
        // aparece en un costado: el punto mas lejos de la chica (si ya esta ahi, dispara desde donde esta)
        Vector3 punto = PuntoLejosDeLaChica();
        if (Mathf.Abs(punto.x - levitacion.PosicionBase.x) > 0.5f) yield return Teletransportar(punto);
        yield return Disparar();
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
        ocupado = false;
    }

    private void LanzarBola()
    {
        if (framesBola == null || framesBola.Length == 0 || objetivo == null) return;
        float lado = Mirando();
        Vector3 origen = transform.position + new Vector3(salida.x * lado, salida.y, 0f) * Escala;

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

    // ---------------- ataque: rayos ----------------

    private IEnumerator AtaqueRayos()
    {
        if (framesLevita == null || framesLevita.Length == 0 || framesRayo == null || framesRayo.Length == 0
            || variantesRayos == null || variantesRayos.Length == 0)
        {
            yield return AtaqueDisparo();
            yield break;
        }

        // variante distinta a la anterior, a veces espejada
        int nv = variantesRayos.Length;
        int elegida = Random.Range(0, nv);
        if (nv > 1 && elegida == ultimaVariante) elegida = (elegida + 1 + Random.Range(0, nv - 1)) % nv;
        ultimaVariante = elegida;
        bool espejo = Random.value < 0.5f;
        var xs = new List<float>();
        foreach (float f in variantesRayos[elegida].posiciones)
            xs.Add(zonaCombate.x + zonaCombate.width * (espejo ? 1f - f : f));
        xs.Sort();

        // 1 de cada 3 ataques de rayos, Umbrae aparece justo arriba de un rayo
        bool debajo = SacarDeLaBolsa();
        float centro = zonaCombate.center.x;
        float xUmbrae = debajo ? MasCercaDe(xs, centro) : HuecoMasCercaDe(xs, centro);
        yield return Teletransportar(EnElPiso(xUmbrae));

        ocupado = true;
        if (hitbox != null) hitbox.enabled = false;   // mientras se eleva no se le puede pegar
        protegido = true;
        MirarALaChica();

        // anticipacion en el piso, donde van a caer
        var anticipaciones = new List<SpriteRenderer>();
        if (framesAnticipacion != null && framesAnticipacion.Length > 0)
            foreach (float x in xs) anticipaciones.Add(CrearDibujo("anticipacion rayo", new Vector3(x, Piso, 0f), framesAnticipacion[0], ordenAnticipacion));

        // sube 'alturaSubida' por encima de donde flota normalmente
        float alturaTotal = Mathf.Max(0f, alturaSubida);
        float vidaACurar = vida != null ? vida.VidaMaxima * curacion : 0f;
        float curado = 0f;
        float inicioRayo = segundosSubida - (frameImpacto - 1) / Mathf.Max(1f, fpsRayo);
        bool rayosLanzados = false, temblando = false;
        float proximaEstela = 0f;
        int nl = framesLevita.Length;

        for (float t = 0f; t < segundosSubida; t += Time.deltaTime)
        {
            levitacion.FactorOnda = 1f - Mathf.SmoothStep(0f, 1f, t / 0.4f);
            alfaGlow = Mathf.SmoothStep(0f, 1f, t / (segundosSubida * 0.5f));

            if (t < segundosSinBrazos)
            {
                // sube dos tercios sin abrir los brazos
                levitacion.Elevacion = alturaTotal * fraccionSinBrazos * Mathf.SmoothStep(0f, 1f, t / segundosSinBrazos);
                spriteActual = framesLevita[0];
            }
            else
            {
                // final de la subida: abre los brazos y termina de subir; tiembla la pantalla y deja estela
                float u = Mathf.Clamp01((t - segundosSinBrazos) / Mathf.Max(0.01f, segundosSubida - segundosSinBrazos));
                levitacion.Elevacion = alturaTotal * Mathf.Lerp(fraccionSinBrazos, 1f, Mathf.SmoothStep(0f, 1f, u));
                spriteActual = framesLevita[Mathf.Min(nl - 1, Mathf.FloorToInt(u * nl))];
                if (!temblando)
                {
                    temblando = true;
                    if (camara != null) camara.Temblar(segundosSubida - segundosSinBrazos, intensidadTemblor);
                }
                if (Time.time >= proximaEstela) { proximaEstela = Time.time + intervaloEstela; StartCoroutine(Estela()); }
            }

            // recupera vida de a poco
            float objetivoCura = vidaACurar * t / segundosSubida;
            if (vida != null) vida.Curar(objetivoCura - curado);
            curado = objetivoCura;

            int ia = Mathf.FloorToInt(t * fpsAnticipacion);
            foreach (var a in anticipaciones) if (a != null) a.sprite = framesAnticipacion[ia % framesAnticipacion.Length];

            // los rayos arrancan antes para que toquen el piso justo al terminar de subir
            if (!rayosLanzados && t >= inicioRayo) { rayosLanzados = true; StartCoroutine(Rayos(xs, anticipaciones)); }
            yield return null;
        }
        if (vida != null) vida.Curar(vidaACurar - curado);
        levitacion.Elevacion = alturaTotal;
        spriteActual = framesLevita[nl - 1];
        if (!rayosLanzados) { rayosLanzados = true; StartCoroutine(Rayos(xs, anticipaciones)); }

        // queda arriba con los brazos abiertos mientras caen los rayos
        float restoRayos = (framesRayo.Length - frameImpacto + 1) / Mathf.Max(1f, fpsRayo);
        yield return new WaitForSeconds(restoRayos);

        // baja cerrando los brazos
        for (float t = 0f; t < segundosBajada; t += Time.deltaTime)
        {
            float u = t / segundosBajada;
            levitacion.Elevacion = alturaTotal * (1f - Mathf.SmoothStep(0f, 1f, u));
            levitacion.FactorOnda = u;
            alfaGlow = 1f - u;
            spriteActual = framesLevita[Mathf.Clamp(nl - 1 - Mathf.FloorToInt(u * nl), 0, nl - 1)];
            yield return null;
        }
        levitacion.Elevacion = 0f;
        levitacion.FactorOnda = 1f;
        alfaGlow = 0f;
        spriteActual = quieto;
        if (hitbox != null) hitbox.enabled = true;
        protegido = false;
        ocupado = false;
    }

    // los rayos caen en todos los lugares a la vez; lastiman entre 'frameImpacto' y 'frameFinDanio'
    private IEnumerator Rayos(List<float> xs, List<SpriteRenderer> anticipaciones)
    {
        var rayos = new List<SpriteRenderer>();
        foreach (float x in xs) rayos.Add(CrearDibujo("rayo umbrae", new Vector3(x, Piso, 0f), framesRayo[0], ordenRayos));
        var colChica = victima != null ? victima.GetComponent<Collider2D>() : null;
        bool lastimo = false, impacto = false;
        int ultimo = -1;
        float inicio = Time.time;

        // va por reloj (no por cuadros), asi el rayo toca el piso justo a tiempo aunque el juego vaya lento
        while (true)
        {
            int i = Mathf.FloorToInt((Time.time - inicio) * fpsRayo);
            if (i >= framesRayo.Length) break;
            if (i != ultimo)
            {
                ultimo = i;
                foreach (var r in rayos) if (r != null) r.sprite = framesRayo[i];
            }
            int frame = i + 1;
            if (!impacto && frame >= frameImpacto)
            {
                impacto = true;
                foreach (var a in anticipaciones) if (a != null) { temporales.Remove(a.gameObject); Destroy(a.gameObject); }
                if (camara != null) camara.Temblar(0.3f, intensidadTemblorImpacto);
            }
            // lastima durante los frames del golpe (una sola vez por ataque)
            if (!lastimo && frame >= frameImpacto && frame <= frameFinDanio && colChica != null && colChica.enabled)
            {
                Bounds b = colChica.bounds;
                foreach (float x in xs)
                {
                    if (b.max.x > x - tamanoDanioRayo.x / 2f && b.min.x < x + tamanoDanioRayo.x / 2f
                        && b.min.y < Piso + tamanoDanioRayo.y && b.max.y > Piso)
                    {
                        lastimo = true;
                        victima.RecibirDanio(danioRayo);
                        break;
                    }
                }
            }
            yield return null;
        }
        foreach (var r in rayos) if (r != null) { temporales.Remove(r.gameObject); Destroy(r.gameObject); }
    }

    // copia de Umbrae en violeta que se desvanece: la estela mientras abre los brazos
    private IEnumerator Estela()
    {
        if (materialEstela == null || !dibujo.enabled) yield break;
        var go = new GameObject("estela umbrae");
        go.transform.position = transform.position;
        go.transform.localScale = transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = dibujo.sprite;
        sr.flipX = dibujo.flipX;
        sr.sharedMaterial = materialEstela;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = dibujo.sortingOrder - 1;
        temporales.Add(go);
        for (float t = 0f; t < vidaEstela; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(1f, 1f, 1f, alfaEstela * (1f - t / vidaEstela));
            yield return null;
        }
        temporales.Remove(go);
        Destroy(go);
    }

    private SpriteRenderer CrearDibujo(string nombre, Vector3 posicion, Sprite sprite, int orden)
    {
        var go = new GameObject(nombre);
        go.transform.position = posicion;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = dibujo.sharedMaterial;
        sr.sortingLayerID = dibujo.sortingLayerID;
        sr.sortingOrder = orden;
        temporales.Add(go);
        return sr;
    }

    // bolsa de 3: una vez "debajo de un rayo" y dos veces "en un hueco", en orden al azar
    private bool SacarDeLaBolsa()
    {
        if (bolsaDebajo.Count == 0)
        {
            bolsaDebajo.Add(true); bolsaDebajo.Add(false); bolsaDebajo.Add(false);
            for (int i = bolsaDebajo.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bolsaDebajo[i], bolsaDebajo[j]) = (bolsaDebajo[j], bolsaDebajo[i]);
            }
        }
        bool v = bolsaDebajo[0];
        bolsaDebajo.RemoveAt(0);
        return v;
    }

    // el rayo mas cerca de x (dentro de donde puede estar Umbrae)
    private float MasCercaDe(List<float> xs, float x)
    {
        float mejor = xs[0];
        foreach (float r in xs)
            if (r >= xMinimo && r <= xMaximo && (Mathf.Abs(r - x) < Mathf.Abs(mejor - x) || mejor < xMinimo || mejor > xMaximo)) mejor = r;
        return mejor;
    }

    // el medio del hueco entre rayos mas cerca de x
    private float HuecoMasCercaDe(List<float> xs, float x)
    {
        var huecos = new List<float>();
        for (int i = 0; i + 1 < xs.Count; i++) huecos.Add((xs[i] + xs[i + 1]) / 2f);
        if (huecos.Count == 0) return Mathf.Clamp(x, xMinimo, xMaximo);
        return MasCercaDe(huecos, x);
    }

    private void CrearGlow()
    {
        if (materialGlow == null) return;
        var go = new GameObject("glow umbrae");
        go.transform.SetParent(transform, false);
        glow = go.AddComponent<SpriteRenderer>();
        glow.sharedMaterial = materialGlow;
        glow.sortingLayerID = dibujo.sortingLayerID;
        glow.sortingOrder = dibujo.sortingOrder - 1;
        glow.enabled = false;
    }

    private void BorrarTemporales()
    {
        foreach (var go in temporales) if (go != null) Destroy(go);
        temporales.Clear();
    }

    private void OnDisable()
    {
        BorrarTemporales();
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
        Gizmos.DrawWireSphere(transform.position + (Vector3)salida * Mathf.Abs(transform.lossyScale.x), 0.2f);

        // la primera variante de rayos con su zona de dano
        if (variantesRayos != null && variantesRayos.Length > 0 && variantesRayos[0].posiciones != null)
        {
            Gizmos.color = new Color(0.8f, 0.6f, 1f, 0.9f);
            foreach (float f in variantesRayos[0].posiciones)
            {
                float x = zonaCombate.x + zonaCombate.width * f;
                Gizmos.DrawWireCube(new Vector3(x, zonaCombate.y + tamanoDanioRayo.y / 2f, 0f), tamanoDanioRayo);
            }
        }
    }
}
