// Nombres de las Sorting Layers de la escena, de atras hacia adelante. Adentro de cada capa el orden
// (sortingOrder) va de 1 en adelante, con numeros chicos. Nunca se usan mascaras ni recortes para que
// algo parezca detras de otra cosa: eso lo decide la capa y su orden.
public static class Capas
{
    public const string Fondo = "Fondo";                  // fondo del mapa (degradado, siluetas)
    public const string Edificios = "Edificios";          // arquitectura del mapa y el fondo del salon de Umbrae
    public const string FondoDetalles = "FondoDetalles";  // grietas, plantas, luces y lianas del mapa; columnas del templo
    public const string Decorado = "Decorado";            // templo y arena de Umbrae
    public const string Escenario = "Escenario";          // portales, plataformas y lo que va detras del piso
    public const string Personajes = "Personajes";        // la chica, el piso, Umbrae, troncos, agua
    public const string Frente = "Frente";                // plantas, botellitas, enemigos y pinchos delante
    public const string Efectos = "Efectos";              // polen, particulas y cosas que vuelan por encima
    public const string Interfaz = "Interfaz";            // iconos y la barra de Umbrae en el mundo
}
