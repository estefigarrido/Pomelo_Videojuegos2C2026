using System.Collections.Generic;
using UnityEngine;

// Vuelve a armar el nivel desde el principio: la chica reaparece en el primer spawnpoint (que pasa a ser
// su punto de reaparicion), todos los mobs vuelven a su lugar con la vida llena y los spawnpoints que ya
// agarro vuelven a aparecer. Lo usa PeleaUmbrae cuando se pierde la pelea demasiadas veces.
// Al empezar guarda una copia apagada de cada mob y de cada spawnpoint; al reiniciar los cambia por copias
// nuevas, asi arrancan exactamente como al principio sin tocar sus scripts. Umbrae no se copia: se reinicia aparte.
public class ReinicioNivel : MonoBehaviour
{
    [Tooltip("Donde reaparece la chica al volver al inicio (el primer spawnpoint).")]
    [SerializeField] private Transform primerSpawnpoint;

    private class Guardado
    {
        public GameObject actual, copia;
        public Transform padre;
        public int orden;
        public Vector3 posicion, escala;
        public Quaternion rotacion;
    }

    private readonly List<Guardado> guardados = new List<Guardado>();
    private Transform deposito;
    private Vector3 posicionInicio;

    // Start corre antes del primer Update de todos, asi que los mobs todavia estan en su lugar inicial
    private void Start()
    {
        posicionInicio = primerSpawnpoint != null ? primerSpawnpoint.position : transform.position;

        var go = new GameObject("Copias para reiniciar");
        go.SetActive(false);
        deposito = go.transform;
        deposito.SetParent(transform, false);

        foreach (var mob in FindObjectsByType<VidaEnemigo>(FindObjectsSortMode.None))
            if (mob.GetComponent<UmbraeBoss>() == null) Guardar(mob.gameObject, null);
        // el halo "Brillo" lo crea el checkpoint solo al aparecer, asi que no se copia
        foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            Guardar(cp.gameObject, "Brillo");
    }

    private void Guardar(GameObject original, string hijoCreadoEnJuego)
    {
        var t = original.transform;
        // adentro de un objeto apagado: la copia no arranca (no corre su Awake) hasta que se use
        var copia = Instantiate(original, t.position, t.rotation, deposito);
        copia.name = original.name;
        if (hijoCreadoEnJuego != null)
        {
            var hijo = copia.transform.Find(hijoCreadoEnJuego);
            if (hijo != null) Destroy(hijo.gameObject);
        }
        guardados.Add(new Guardado
        {
            actual = original, copia = copia, padre = t.parent, orden = t.GetSiblingIndex(),
            posicion = t.position, rotacion = t.rotation, escala = t.localScale
        });
    }

    // La chica vuelve al primer spawnpoint y el nivel queda como al principio.
    public void VolverAlInicio(SaludPersonaje salud)
    {
        foreach (var g in guardados)
        {
            if (g.actual != null) Destroy(g.actual);
            var nuevo = Instantiate(g.copia, g.posicion, g.rotacion, g.padre);
            nuevo.name = g.copia.name;
            nuevo.transform.localScale = g.escala;
            nuevo.transform.SetSiblingIndex(g.orden);
            g.actual = nuevo;
        }
        if (salud != null) salud.DefinirPuntoDeReaparicion(posicionInicio);
        Debug.Log("[Reinicio] vuelve al primer spawnpoint; reaparecen " + guardados.Count + " mobs y spawnpoints");
    }
}
