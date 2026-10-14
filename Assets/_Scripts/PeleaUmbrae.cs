using UnityEngine;

// Muertes de la chica durante la pelea con Umbrae:
//  - las primeras veces reaparece en el medio de la arena ('reaparicion') y la pelea sigue.
//  - a la tercera, reaparece en el ultimo spawnpoint que agarro y la pelea se reinicia
//    (Umbrae vuelve a su lugar con la vida llena).
//  - despues de 'reiniciosAntesDeVolverAlInicio' reinicios, la vez siguiente que pierde vuelve al primer
//    spawnpoint del nivel y reaparecen todos los mobs (lo hace ReinicioNivel).
[RequireComponent(typeof(ZonaCamaraFija))]
public class PeleaUmbrae : MonoBehaviour
{
    [SerializeField] private UmbraeBoss jefe;
    [Tooltip("Donde reaparece la chica si muere peleando (el medio de la arena).")]
    [SerializeField] private Transform reaparicion;
    [Tooltip("A esta cantidad de muertes vuelve al ultimo spawnpoint y se reinicia la pelea.")]
    [SerializeField] private int muertesParaReiniciar = 3;
    [Tooltip("Cuantas veces se puede reiniciar la pelea. La siguiente vez que la pierde, vuelve al primer spawnpoint y reaparecen todos los mobs.")]
    [SerializeField] private int reiniciosAntesDeVolverAlInicio = 3;
    [SerializeField] private ReinicioNivel reinicioNivel;

    private ZonaCamaraFija zona;
    private SaludPersonaje salud;
    private int muertes;
    private int reinicios;

    private void Start()
    {
        zona = GetComponent<ZonaCamaraFija>();
        salud = FindFirstObjectByType<SaludPersonaje>();
        if (salud != null) salud.AlMorir += AlMorirChica;
    }

    private void OnDestroy()
    {
        if (salud != null) salud.AlMorir -= AlMorirChica;
    }

    private void AlMorirChica()
    {
        if (zona == null || !zona.PeleaEnCurso || jefe == null || !jefe.gameObject.activeInHierarchy) return;

        muertes++;
        if (muertes < muertesParaReiniciar)
        {
            if (reaparicion != null) salud.ReaparecerUnaVezEn(reaparicion.position);
            Debug.Log("[Pelea Umbrae] muerte " + muertes + " de " + muertesParaReiniciar + ": reaparece en la arena");
            return;
        }

        // tercera muerte: se reinicia la pelea (Umbrae a su lugar con la vida llena)
        muertes = 0;
        reinicios++;
        jefe.Reiniciar();
        var vida = jefe.GetComponent<VidaEnemigo>();
        if (vida != null) vida.Reiniciar();

        if (reinicioNivel != null && reinicios > reiniciosAntesDeVolverAlInicio)
        {
            // perdio la pelea una vez mas despues de los reinicios: vuelve al primer spawnpoint y reaparecen los mobs
            reinicios = 0;
            reinicioNivel.VolverAlInicio(salud);
            Debug.Log("[Pelea Umbrae] se perdio la pelea " + (reiniciosAntesDeVolverAlInicio + 1) + " veces: vuelve al inicio del nivel");
            return;
        }
        // vuelve al ultimo spawnpoint (lo hace SaludPersonaje solo)
        Debug.Log("[Pelea Umbrae] " + muertesParaReiniciar + " muertes: vuelve al spawnpoint y se reinicia la pelea (reinicio " + reinicios + " de " + reiniciosAntesDeVolverAlInicio + ")");
    }

    private void OnDrawGizmos()
    {
        if (reaparicion == null) return;
        Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.9f);
        Gizmos.DrawWireCube(reaparicion.position, new Vector3(0.8f, 2.4f, 0f));
    }
}
