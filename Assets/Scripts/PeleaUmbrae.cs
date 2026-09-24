using UnityEngine;

// Muertes de la chica durante la pelea con Umbrae:
//  - las primeras veces reaparece en el medio de la arena ('reaparicion') y la pelea sigue.
//  - a la tercera, reaparece en el ultimo spawnpoint que agarro y la pelea se reinicia
//    (Umbrae vuelve a su lugar con la vida llena).
[RequireComponent(typeof(ZonaCamaraFija))]
public class PeleaUmbrae : MonoBehaviour
{
    [SerializeField] private UmbraeBoss jefe;
    [Tooltip("Donde reaparece la chica si muere peleando (el medio de la arena).")]
    [SerializeField] private Transform reaparicion;
    [Tooltip("A esta cantidad de muertes vuelve al ultimo spawnpoint y se reinicia la pelea.")]
    [SerializeField] private int muertesParaReiniciar = 3;

    private ZonaCamaraFija zona;
    private SaludPersonaje salud;
    private int muertes;

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

        // tercera muerte: vuelve al ultimo spawnpoint (lo hace SaludPersonaje solo) y se reinicia todo
        muertes = 0;
        jefe.Reiniciar();
        var vida = jefe.GetComponent<VidaEnemigo>();
        if (vida != null) vida.Reiniciar();
        Debug.Log("[Pelea Umbrae] " + muertesParaReiniciar + " muertes: vuelve al spawnpoint y se reinicia la pelea");
    }

    private void OnDrawGizmos()
    {
        if (reaparicion == null) return;
        Gizmos.color = new Color(1f, 0.35f, 0.35f, 0.9f);
        Gizmos.DrawWireCube(reaparicion.position, new Vector3(0.8f, 2.4f, 0f));
    }
}
