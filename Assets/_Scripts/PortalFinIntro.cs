using UnityEngine;

// Portal del final de la intro (la puerta dorada): cuando la chica entra, la musica de la intro (Katara) se apaga
// con un fundido. Necesita un Collider2D en modo trigger que cubra la entrada.
// Lo pone el menu  Pomelo -> Escenas -> Armar escena Inicio_introducción.
[RequireComponent(typeof(Collider2D))]
public class PortalFinIntro : MonoBehaviour
{
    private bool usado;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (usado || otro.GetComponentInParent<MovimientoPersonaje>() == null) return;
        usado = true;
        if (MusicaIntro.Instancia != null) MusicaIntro.Instancia.SalirPorElPortal();
    }
}
