using UnityEngine;

// Marca: esta es una de las plataformas de piedra flotantes (_entorno/_Plataformas). Al caer encima, la chica
// usa el sonido Salto_caida_plataforma_* en vez de la caida comun.
// No hace falta ponerla a mano: el menu Pomelo -> Sonidos -> Conectar sonidos se la agrega a todas las
// plataformas que usan esos dibujos.
public class PisoPlataformaPiedra : MonoBehaviour
{
    // true si el collider es (o es parte de) una plataforma de piedra
    public static bool Es(Collider2D col)
    {
        if (col == null) return false;
        return col.GetComponentInParent<PisoPlataformaPiedra>() != null;
    }
}
