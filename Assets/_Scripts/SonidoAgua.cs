using UnityEngine;

// El splash de la intro (Inicio_introduccion): cuando la chica cae al agua (sus pies cruzan la superficie bajando)
// suena un splash (Assets/_Sonidos/Agua_splash.mp3, de freesound, elegido por Caro), un poco mas grave y con eco, mas fuerte
// cuanto mas rapido cae. (Caro: el sonido del agua corriendo se saco.)
// Lo arma el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción  (o  Poner sonido del agua (intro)).
public class SonidoAgua : MonoBehaviour
{
    [Header("Splash")]
    [SerializeField] private AudioClip splash;
    [Tooltip("Se multiplica por el volumen general de los efectos (Sonido), como la puerta.")]
    [Range(0f, 1f)] [SerializeField] private float volumenSplash = 0.8f;
    [Tooltip("Tono del splash (Caro: un poco mas grave). 1 = el original; menos de 1 = mas grave.")]
    [Range(0.5f, 1.5f)] [SerializeField] private float tono = 0.74f;
    [Tooltip("Cuanto varia el tono de un splash a otro (0.04 = 4 %), asi no suena siempre igual.")]
    [SerializeField] private float variacionTono = 0.04f;
    [Tooltip("Eco de una sala de piedra (AudioReverbFilter).")]
    [SerializeField] private bool conEco = true;
    [Tooltip("Eco abierto y suave (sin el sonido de caja/tubo de los presets). Apagado: usa 'presetEco'.")]
    [SerializeField] private bool ecoAbierto = true;
    [Tooltip("Cuanto eco (mB): -600 bastante, -1400 suave, -3000 casi nada.")]
    [SerializeField] private float nivelEco = -1400f;
    [Tooltip("Segundos que dura la cola del eco.")]
    [SerializeField] private float duracionEco = 1.5f;
    [SerializeField] private AudioReverbPreset presetEco = AudioReverbPreset.Stoneroom;
    [Tooltip("Segundos que se deja sonar la cola del eco despues del sonido.")]
    [SerializeField] private float colaEco = 2f;
    [Tooltip("Altura (y del mundo) de la superficie del agua: cuando los pies de la chica la cruzan bajando, suena.")]
    [SerializeField] private float superficie = 4.81f;   // (2347.2 - 2016) px / 68.85 px por unidad
    [Tooltip("Velocidad de caida (unidades/s) desde la que suena; mas rapido = mas fuerte.")]
    [SerializeField] private float velocidadMinima = 1.5f;
    [SerializeField] private float velocidadFuerte = 9f;
    [Tooltip("Segundos minimos entre un splash y el siguiente.")]
    [SerializeField] private float pausaEntreSplash = 0.35f;

    private Transform chica;
    private Collider2D colChica;
    private Rigidbody2D rbChica;
    private float piesAntes = float.NaN;
    private float ultimoSplash = -10f;

    private void Start() => BuscarChica();

    private void BuscarChica()
    {
        var salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud == null) return;
        chica = salud.transform;
        colChica = salud.GetComponent<Collider2D>();
        rbChica = salud.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (chica == null) { BuscarChica(); return; }
        if (!chica.gameObject.activeInHierarchy) { piesAntes = float.NaN; return; }
        float pies = colChica != null ? colChica.bounds.min.y : chica.position.y;
        if (!float.IsNaN(piesAntes) && piesAntes > superficie && pies <= superficie && Time.time - ultimoSplash > pausaEntreSplash)
        {
            float caida = rbChica != null ? -rbChica.linearVelocity.y : (piesAntes - pies) / Mathf.Max(Time.deltaTime, 0.001f);
            if (caida >= velocidadMinima && splash != null)
            {
                float fuerza = Mathf.InverseLerp(velocidadMinima, velocidadFuerte, caida);
                Sonar(volumenSplash * Mathf.Lerp(0.45f, 1f, fuerza), tono * Random.Range(1f - variacionTono, 1f + variacionTono));
                ultimoSplash = Time.time;
            }
        }
        piesAntes = pies;
    }

    // suena con el tono bajado (mas grave) y con el eco de una sala de piedra (AudioReverbFilter)
    private void Sonar(float volumen, float tonoFinal)
    {
        var go = new GameObject("Sonido: splash");
        var fuente = go.AddComponent<AudioSource>();
        fuente.clip = splash;
        fuente.volume = volumen * Sonido.VolumenEfectos;
        fuente.pitch = tonoFinal;
        fuente.spatialBlend = 0f;
        fuente.playOnAwake = false;
        if (conEco)
        {
            var eco = go.AddComponent<AudioReverbFilter>();
            if (ecoAbierto)
            {
                // (Caro: sonaba "metido en un tubo") eco abierto y suave: casi sin reflejos tempranos (que son los que
                // suenan a caja o cano), una cola difusa que se apaga sola y los agudos del eco un poco mas apagados
                eco.reverbPreset = AudioReverbPreset.User;
                eco.dryLevel = 0f;
                eco.room = nivelEco;           // mB: cuanto eco (0 = mucho, -10000 = nada)
                eco.roomHF = -700f;
                eco.roomLF = -300f;
                eco.decayTime = duracionEco;
                eco.decayHFRatio = 0.55f;
                eco.reflectionsLevel = -2600f; // casi sin reflejos tempranos
                eco.reflectionsDelay = 0.012f;
                eco.reverbLevel = -200f;
                eco.reverbDelay = 0.03f;
                eco.hfReference = 5000f;
                eco.lfReference = 250f;
                eco.diffusion = 100f;
                eco.density = 100f;
            }
            else eco.reverbPreset = presetEco;
        }
        fuente.Play();
        Destroy(go, splash.length / Mathf.Max(0.01f, tonoFinal) + colaEco);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.9f);
        Gizmos.DrawLine(new Vector3(0f, superficie, 0f), new Vector3(120f, superficie, 0f));
    }
}
