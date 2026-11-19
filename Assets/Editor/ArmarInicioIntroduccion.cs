using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Arma la escena de la pantalla de inicio (Assets/Scenes/Inicio_introducción.unity) con el frame "intro juego ·
// escena" del Figma de Caro. Cada capa del frame esta exportada como PNG en Assets/_Entorno/_Inicio_introduccion,
// y capas.json dice, para cada una, donde va (px de la escena: 1 px = 0.36 unidades de Figma, como antes), su
// tamano y su orden de dibujo (de atras hacia adelante, como en Figma). El menu pone todas las capas y les agrega
// las animaciones segun su nombre:
//   - agua (agua, reflejos, 09 agua...): se mece y corre como un rio (shader AguaOndulante)
//   - agua cayendo (los chorritos de la cueva) y 07 plataformas de agua (derecha): el agua cae (CascaditasAgua)
//   - inicio_13_planta: se mece con la brisa (PlantaViento)
//   - niebla (inicio_02_niebla, 01 cielo y niebla, niebla al pie): se mueve un poquito (Neblina)
//   - las dos columnas sueltas y el Jarron: flotan, con agua delante de la base (Flotante); el Jarron con brillo
//     (Resplandor) y el Lore que sale de el (LoreDeLaVasija)
//   - pilares, 10 plataformas, bloques de agua y 07 plataformas: tienen piso invisible (la chica camina encima)
//   - los puntitos de luz de la cueva (Destellos), la chica, los carteles, el dialogo y la pantalla del titulo
// Tambien pone la escena primera en la lista del build, asi el juego arranca por la pantalla de inicio.
// Menu:  Pomelo -> Escenas -> Armar escena Inicio_introducción
public static class ArmarInicioIntroduccion
{
    // Todo lo de la intro esta en Assets/_Intro, en una carpeta por capa (las mismas que las sorting layers):
    // 01_Fondo, 02_Edificios (murales, murales/hoja_botanica), 03_FondoDetalles, 04_Decorado,
    // 05_Escenario (techo, rocas_de_la_cueva, agua, piedras_y_cascadas, piedras_y_cascadas/piezas) y 06_Frente.
    // Cada archivo de capas.json se busca por su nombre en cualquiera de esas carpetas.
    private const string Carpeta = "Assets/_Intro";
    private const string RutaManifiesto = Carpeta + "/capas.json";
    private static Dictionary<string, string> archivos;
    private static string Buscar(string archivo)
    {
        if (archivos == null)
        {
            archivos = new Dictionary<string, string>();
            if (Directory.Exists(Carpeta))
                foreach (var p in Directory.GetFiles(Carpeta, "*.png", SearchOption.AllDirectories))
                    archivos[Path.GetFileName(p)] = p.Replace('\\', '/');
        }
        return archivos.TryGetValue(archivo, out var r) ? r : Carpeta + "/" + archivo;
    }
    private const string RutaEscena = "Assets/Scenes/Inicio_introducción.unity";
    // px de la escena por unidad de Unity. Estefi (19/11): la chica mide lo mismo que en el nivel (escala 1: su dibujo
    // visible mide 2.4 unidades) y todo lo demas se escala junto, en la proporcion del Figma: la chica de referencia
    // del Figma ("001@2x 1") mide 459 px de alto visible = 165.24 px de la escena (x 0.36) -> 165.24 / 2.4 = 68.85.
    private const float PixelesPorUnidad = 68.85f;
    private const float AnchoCuevaPx = 3612f;   // la parte de la cueva de inicio (lo que muestra la pantalla de inicio)
    // medidas de la escena (las pone el capas.json: el frame de Figma por 0.36)
    private static float AltoEscenaPx = 2347.2f;
    private static float AnchoEscenaPx = 7839.36f;
    // altura del agua (px de la escena, desde arriba): el borde de arriba de la capa "agua" del fondo
    private static float LineaDelAguaPx = 1990.8f;

    // Si cambia el diseno (otro frame de Figma), los ajustes a mano de la escena anterior ya no sirven: con otra
    // version, el menu arma la escena de cero (sin mezclar). Con la misma version, conserva los ajustes a mano.
    private const string VersionDisenio = "figma: intro juego · escena (18/11/2026)";
    private const string RutaVersion = "Assets/Editor/ArmarInicioIntroduccion_base.version.txt";

    [System.Serializable] private class Capa
    {
        public int i, sub; public string archivo, nombre, ruta, tipo, blend;
        public float x, y, ancho, alto, opacidad; public int imgW, imgH;
        public float F => ancho > 0f ? imgW / ancho : 1f;   // pixeles de la imagen por pixel de la escena
        public string Ruta => Buscar(archivo);
    }
    [System.Serializable] private class Cuadro { public string nombre; public float ancho, alto, escala; }
    [System.Serializable] private class Manifiesto { public Cuadro frame; public Capa[] capas; }

    [MenuItem("Pomelo/Escenas/Armar escena Inicio_introducción")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Para el Play antes de armar la escena."); return; }
        if (!File.Exists(RutaManifiesto)) { Debug.LogError("Falta " + RutaManifiesto + " (las capas exportadas de Figma)."); return; }
        var man = JsonUtility.FromJson<Manifiesto>(File.ReadAllText(RutaManifiesto));
        if (man == null || man.capas == null || man.capas.Length == 0) { Debug.LogError("No pude leer " + RutaManifiesto); return; }
        AltoEscenaPx = man.frame.alto;
        AnchoEscenaPx = man.frame.ancho;
        archivos = null;
        capaDeI.Clear();
        // el agua de adelante (los rectangulos de agua y los reflejos) va en Frente: pasa por delante de la chica
        // y las piedras (con sus cascaditas) van en Frente despues del agua, enteras por encima de ella como en el
        // Figma: de atras para adelante queda la chica, el agua y las piedras
        foreach (var c in man.capas)
            capaDeI[c.i] = (c.ruta.Contains("Escenario/agua") && (c.nombre == "agua" || c.nombre.StartsWith("reflejo"))) ||
                           c.ruta.Contains("piedras y cascadas") || c.ruta == ""
                ? Capas.Frente : CapaDeRuta(c.ruta);
        var aguaFondo = man.capas.FirstOrDefault(c => c.nombre == "agua" && c.ruta == "1 · Fondo");
        if (aguaFondo != null) LineaDelAguaPx = aguaFondo.y;

        // Si la escena ya existe, se rearma ENCIMA de ella y se conservan los ajustes hechos a mano (ver
        // ConservarAjustes): se saca una foto de como esta, se arma todo de nuevo y se le vuelven a poner los cambios
        // a mano. Si esta abierta, se usa tal cual (con lo que no se haya guardado todavia).
        Scene escena;
        ConservarAjustes.Foto actual = null, anterior = null;
        var viejas = new List<GameObject>();
        bool mismoDisenio = File.Exists(RutaVersion) && File.ReadAllText(RutaVersion).Trim() == VersionDisenio;
        if (File.Exists(RutaEscena))
        {
            var activa = SceneManager.GetActiveScene();
            if (activa.path == RutaEscena) escena = activa;
            else
            {
                // si la escena abierta (Nivel1_Templo) tiene cambios sin guardar, pregunta antes de cerrarla
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            }
            // el Canvas (cuadro de dialogo y pantalla de entrada) se llama "UI": si en la escena quedo con el nombre
            // viejo ("Canvas"), se lo renombra, asi la mezcla no cree que se borro a mano
            var raices = escena.GetRootGameObjects();
            if (!raices.Any(g => g.name == "UI"))
            {
                var renombrado = raices.FirstOrDefault(g => g.GetComponent<Canvas>() != null &&
                                                            g.GetComponentInChildren<CuadroDialogo>(true) != null);
                if (renombrado != null) renombrado.name = "UI";
            }
            viejas = escena.GetRootGameObjects().Where(g => RaicesDelMenu.Contains(g.name)).ToList();
            if (mismoDisenio)
            {
                actual = ConservarAjustes.Tomar(viejas);
                anterior = ConservarAjustes.LeerBase(RutaBase);
                // el dibujo del escenario queda siempre como en el Figma: de el no se conservan ajustes a mano
                // (si, los de la chica, los carteles, el lore, el dialogo y la pantalla de inicio)
                if (anterior != null) SinAjustesDelDibujo(actual, anterior);
            }
            foreach (var g in viejas) g.name = "__viejo " + g.name;   // asi no se confunden con las nuevas
        }
        else
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // camara: ve la cueva de inicio entera (PantallaInicio la reencuadra segun la pantalla)
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = AnchoCuevaPx / PixelesPorUnidad / (16f / 9f) / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(0x14, 0x1a, 0x18, 0xff);
        camGo.transform.position = new Vector3(AnchoCuevaPx / PixelesPorUnidad / 2f, (AltoEscenaPx - CentroDesdeArribaPx) / PixelesPorUnidad, -10f);
        camGo.AddComponent<AudioListener>();

        // luz global, como en el nivel (los sprites usan el material iluminado del 2D)
        var luzGo = new GameObject("Luz global");
        var luz = luzGo.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Global;
        luz.intensity = 1f;

        var raiz = new GameObject("Escenario").transform;
        var inicio = new GameObject("Inicio (cueva)").transform;
        var cascada = new GameObject("Cascada (templo)").transform;
        inicio.SetParent(raiz, false);
        cascada.SetParent(raiz, false);

        colocadas.Clear();
        Colocada pisoChica = null;
        int faltan = 0, ordenMax = 0, nNiebla = 0, nFlota = 0;
        Material cascadasCueva = null;
        piedras.Clear();
        decorados.Clear();
        bloquesDeLaCueva = null;
        foreach (var c in man.capas.OrderBy(c => c.i).ThenBy(c => c.sub))
        {
            if (!File.Exists(c.Ruta)) { Debug.LogWarning("Falta la capa " + c.Ruta); faltan++; continue; }
            // de a 10: entre una capa y la siguiente entran cosas (agua de adelante, brillo); 'sub' = partes de una capa
            int orden = c.i * 10 + c.sub - 3000;
            ordenMax = Mathf.Max(ordenMax, orden);
            var padre = c.x + c.ancho / 2f < AnchoCuevaPx ? inicio : cascada;
            var sprite = ImportarPieza(c.Ruta, PixelesPorUnidad * c.F);
            if (sprite == null) { faltan++; continue; }

            // las dos columnas sueltas: flotan, con la base metida en el agua
            if (c.ruta.EndsWith("08 columnas sueltas"))
            {
                ArmarFlotante(c, sprite, padre, "Columna flotante " + (++nFlota), orden, nFlota * 1.7f);
                continue;
            }
            // el Jarron: flota, con un brillo suave, y de el sale el Lore cuando la chica se acerca
            if (c.nombre.StartsWith("Jarr"))
            {
                var raizVasija = ArmarFlotante(c, sprite, padre, "Vasija flotante", orden, 0.9f);
                // Estefi (19/11): el jarron es solido, con la forma del dibujo: se puede pasar por encima y pararse arriba
                var dibujoVasija = raizVasija.Find("Dibujo");
                if (dibujoVasija != null && dibujoVasija.GetComponent<PolygonCollider2D>() == null)
                {
                    dibujoVasija.gameObject.AddComponent<PolygonCollider2D>();
                    dibujoVasija.gameObject.AddComponent<PisoPlataformaPiedra>();   // suena como piedra al caer encima
                }
                PonerBrillo(raizVasija, c.Ruta, c.F, 1f, orden - 1);
                PonerLore(raizVasija, sprite, orden + 6);
                continue;
            }

            // las piedras de "10 plataformas" vienen en una sola imagen: se separan en piezas (cada bloque) para poder
            // agrandarlas y pisarlas una por una
            if (c.ruta.EndsWith("10 plataformas"))
            {
                foreach (var pz in SepararEnPiezas(c))
                {
                    var gp = new GameObject($"{c.i:000} {c.nombre} pieza {pz.n}");
                    gp.transform.SetParent(padre, false);
                    gp.transform.localPosition = new Vector3(pz.x / PixelesPorUnidad, (AltoEscenaPx - pz.y) / PixelesPorUnidad, 0f);
                    var srp = gp.AddComponent<SpriteRenderer>();
                    srp.sprite = pz.sprite;
                    srp.sortingOrder = orden;
                    var mp = MaterialPiedrasConAgua();   // sus tiras de agua claras corren para abajo
                    if (mp != null) srp.sharedMaterial = mp;
                    var colp = new Colocada { ruta = pz.ruta, zona = new RectInt(0, 0, pz.imgW, pz.imgH), x = pz.x, y = pz.y,
                                              escala = 1f, f = c.F, go = gp };
                    colocadas.Add(colp);
                    piedras.Add(new Piedra { go = gp, x = pz.x, y = pz.y, ancho = pz.ancho, alto = pz.alto, col = colp });
                    if (pisoChica == null) pisoChica = colp;   // la chica arranca sobre el primer bloque
                }
                continue;
            }

            // los bloques con chorritos de agua de la cueva van todos juntos en un objeto, para moverlos de una vez
            if (EsBloqueDeLaCueva(c)) padre = BloquesDeLaCueva(inicio, man);

            var go = new GameObject($"{c.i:000} {c.nombre}");
            go.transform.SetParent(padre, false);
            // el punto de cada imagen esta arriba a la izquierda; el piso de la escena queda en y = 0
            go.transform.position = new Vector3(c.x / PixelesPorUnidad, (AltoEscenaPx - c.y) / PixelesPorUnidad, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;

            // agua que se mece y corre como un rio
            if (EsAgua(c)) sr.sharedMaterial = MaterialAgua(Path.GetFileNameWithoutExtension(c.archivo), sprite);
            // los chorritos de agua de la cueva caen
            if (c.nombre == "agua cayendo")
            {
                if (cascadasCueva == null)
                {
                    cascadasCueva = MaterialAnimado("Pomelo/CascaditasAgua", "Cascadas_cueva");
                    if (cascadasCueva != null)
                    {
                        // todo el dibujo es agua: sin tenir la piedra ni fundir la parte de abajo
                        cascadasCueva.SetFloat("_AltoPx", c.imgH);
                        cascadasCueva.SetFloat("_Umbral", 0f);
                        cascadasCueva.SetFloat("_TonoPiedra", 0f);
                        cascadasCueva.SetFloat("_FundidoAbajo", 0f);
                    }
                }
                if (cascadasCueva != null) sr.sharedMaterial = cascadasCueva;
            }
            // las plataformas de agua de la derecha: la piedra gris oscura y el agua que cae
            if (c.nombre.StartsWith("07 plataformas") && AgregadosQueNoEstanEnElFigma)
            {
                var m = MaterialAnimado("Pomelo/CascaditasAgua", "Cascaditas_agua");
                if (m != null) { m.SetFloat("_AltoPx", c.imgH); sr.sharedMaterial = m; }
            }
            // la plantita de la cueva se mece con la brisa
            if (c.nombre == "inicio_13_planta") sr.sharedMaterial = MaterialAnimado("Pomelo/PlantaViento", "Planta_viento");
            // la neblina se mueve un poquito
            // (solo la neblina sola: "01 cielo y niebla" tiene tambien sombras de piedras y agua, que no se mueven)
            if (c.nombre.Contains("niebla") && c.nombre != "01 cielo y niebla")
            {
                var so = new SerializedObject(go.AddComponent<Neblina>());
                so.FindProperty("desfase").floatValue = (nNiebla++) * 5.3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // lo que se puede pisar: piso invisible pegado al dibujo
            Colocada col = null;
            if (EsPiso(c))
            {
                col = new Colocada { ruta = c.Ruta, zona = new RectInt(0, 0, c.imgW, c.imgH), x = c.x, y = c.y,
                                     escala = 1f, f = c.F, go = go };
                colocadas.Add(col);
            }
            // las piedras de la parte 2 se agrandan para la chica (ver AgrandarPiedras); lo que esta parado encima
            // (la puerta dorada) sube con ellas
            if (EsPiedraAgrandable(c))
            {
                piedras.Add(new Piedra { go = go, x = c.x, y = c.y, ancho = c.ancho, alto = c.alto, col = col });
                // las tiras de agua claras que tienen dibujadas corren para abajo
                var mp = MaterialPiedrasConAgua();
                if (mp != null) sr.sharedMaterial = mp;
            }
            else if (c.ruta == "4 · Decorado") decorados.Add(new Piedra { go = go, x = c.x, y = c.y, ancho = c.ancho, alto = c.alto });
            // la columna caida de la cueva (Group 296) es una pared: la chica no puede pasar al otro lado
            // (con la forma del dibujo: una caja tomaria todo el rectangulo de la columna inclinada y empujaria a la chica)
            if (c.nombre == "Group 296") go.AddComponent<PolygonCollider2D>();
            // los puntitos de luz de la cueva, adelante del musgo
            if (c.nombre == "inicio_15_musgo") ArmarDestellos(inicio, orden + 5);
        }

        // Estefi (19/11): las piedras quedan como en Figma, bajas sobre el agua y con la puerta dorada apoyada abajo.
        // Para volver a agrandarlas (pedido anterior de Caro) poner AgrandarPiedrasParaLaChica en true.
        if (AgrandarPiedrasParaLaChica) AgrandarPiedras();
        var arranque = colocadas.FirstOrDefault(c => c.go != null && c.go.name == ChicaArrancaEn) ?? pisoChica;
        var chica = PonerChica(cascada, ordenMax + 5, arranque);
        ArmarPisosParte2(cascada);
        ArmarMusica(cascada);
        var dialogo = ArmarDialogoAlLlegar(cascada);
        var (cartel, cartelCorrer, cartelSaltar) = PonerCarteles(cascada, chica, actual);
        var posicionCartelArmado = cartel != null ? cartel.transform.position : Vector3.zero;
        var posicionCorrerArmado = cartelCorrer != null ? cartelCorrer.transform.position : Vector3.zero;
        var posicionSaltarArmado = cartelSaltar != null ? cartelSaltar.transform.position : Vector3.zero;
        // el dialogo de Jaya lo abre el Lore de la vasija (el cartel de Saltar ya no lo muestra el Lore: va despues del
        // de Correr)
        foreach (var lore in cascada.GetComponentsInChildren<LoreDeLaVasija>(true))
        {
            var sol = new SerializedObject(lore);
            sol.FindProperty("dialogo").objectReferenceValue = dialogo;
            sol.FindProperty("cartelAlTerminar").objectReferenceValue = null;
            sol.FindProperty("frenarEnX").floatValue = LineaJaya;
            sol.ApplyModifiedPropertiesWithoutUndo();
        }
        ArmarPantalla(cam, chica, dialogo, cartel);
        // la pantalla "Ingresaste al Templo de botanica" (Assets/_Pantallas_info/Entrada_templo_botánica), en el Canvas
        var canvasNuevo = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "UI");
        if (canvasNuevo != null) ArmarEntradaTemplo.Armar(canvasNuevo.transform);
        // la maestra (Lady Li Hua) antes de la cascadita: su pantalla y sus dos lineas
        if (canvasNuevo != null) ArmarMaestra(cascada, canvasNuevo.transform);

        // cada capa en su sorting layer (Fondo, Edificios, FondoDetalles, Decorado, Escenario, Frente; la chica en
        // Personajes) con ordenes chicos dentro de cada una
        PonerSortingLayers(raiz);
        LuzEnTodasLasCapas(luz);

        // la "base" para la proxima vez: lo que arma el menu, sin los ajustes a mano
        var nuevas = escena.GetRootGameObjects().Where(g => RaicesDelMenu.Contains(g.name)).ToList();
        var nuevo = ConservarAjustes.Tomar(nuevas);
        ConservarAjustes.GuardarBase(nuevo, RutaBase);
        File.WriteAllText(RutaVersion, VersionDisenio);

        int ajustes = 0;
        if (actual != null) ajustes = ConservarAjustes.Mezclar(actual, nuevo, anterior, escena);
        foreach (var g in viejas) if (g != null) Object.DestroyImmediate(g);
        // los ajustes a mano pueden traer ordenes de antes (todo en Default, de -3000 a 0): se vuelven a repartir por
        // capa respetando ese orden
        var raizFinal = GameObject.Find("Escenario");
        if (raizFinal != null) PonerSortingLayers(raizFinal.transform, primera: false);
        // la puerta dorada que se abre y deja entrar a la chica (despues de la mezcla, sobre la escena ya armada)
        ArmarEntradaTemplo.PonerPuerta();

        // la chica parada sobre la piedra que tenga abajo (aunque se haya movido a mano o se hayan movido las piedras)
        var chicaFinal = GameObject.Find("Escenario/Cascada (templo)/Chica");
        if (chicaFinal != null) PantallaInicio.ApoyarEnElPiso(chicaFinal);

        // los carteles de Caminar y Saltar, al lado de la chica ya ubicada (salvo que se los haya movido a mano)
        if (chicaFinal != null)
        {
            var src = chicaFinal.GetComponent<SpriteRenderer>();
            float pies = src != null ? src.bounds.min.y : chicaFinal.transform.position.y;
            float cx = chicaFinal.transform.position.x;
            if (cartel != null && (cartel.transform.position - posicionCartelArmado).sqrMagnitude < 1e-6f)
                cartel.transform.position = new Vector3(cx + CartelDesdeLosPies.x, pies + CartelDesdeLosPies.y, cartel.transform.position.z);
            if (cartelSaltar != null && (cartelSaltar.transform.position - posicionSaltarArmado).sqrMagnitude < 1e-6f)
                cartelSaltar.transform.position = new Vector3(cx + SaltarDesdeLosPies.x, pies + SaltarDesdeLosPies.y, cartelSaltar.transform.position.z);
            // (Caro, 19/11, "Ajuste de dialogos y tiempos") el de Saltar mas adelante, en x 55.68
            if (cartelSaltar != null && cartelSaltar.transform is RectTransform rtSaltar)
                rtSaltar.anchoredPosition = new Vector2(SaltarEnXDeCaro, rtSaltar.anchoredPosition.y);
            // Correr: a la derecha del de Caminar (donde este), con un espacio entre los dos (Caro, recuadro violeta)
            if (cartelCorrer != null && cartel != null && (cartelCorrer.transform.position - posicionCorrerArmado).sqrMagnitude < 1e-6f)
                cartelCorrer.transform.position = new Vector3(cartel.transform.position.x + AnchoEnElMundo(cartel) * CorrerDespuesDeCaminar,
                                                              cartel.transform.position.y, cartelCorrer.transform.position.z);
            // la altura siempre es la del nivel (aunque se los haya corrido a mano de costado)
            foreach (var ct in new[] { cartel, cartelCorrer, cartelSaltar })
                if (ct != null)
                    ct.transform.position = new Vector3(ct.transform.position.x, pies + AlturaCartelSobrePies, ct.transform.position.z);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(RutaEscena));
        EditorSceneManager.SaveScene(escena, RutaEscena);
        PonerPrimeraEnElBuild();
        Debug.Log($"Escena {RutaEscena} armada con {man.capas.Length - faltan} capas del Figma. Quedo primera en el build." +
                  (actual != null ? $" Se conservaron {ajustes} ajustes hechos a mano." :
                   (viejas.Count > 0 ? " (Diseno nuevo: se armo de cero, sin los ajustes a mano de la escena anterior.)" : "")));
    }

    // ---------- musica ----------
    // Titulo: "Leaves from the Vine". Al Comenzar: "Katara" en loop hasta el portal del final (la puerta dorada).
    // Mismo volumen y fundido que la musica del nivel (MusicaDeFondo de Nivel1_Templo: 0.072 y 3 s).
    private const string RutaTemaTitulo = "Assets/_Musica/Leaves-from-the-Vine-_Orchestral-Version_.mp3";
    private const string RutaTemaIntro = "Assets/_Musica/Katara.mp3";

    private static void ArmarMusica(Transform cascada)
    {
        var go = new GameObject("Musica");
        var m = go.AddComponent<MusicaIntro>();
        var so = new SerializedObject(m);
        so.FindProperty("temaTitulo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaTemaTitulo);
        so.FindProperty("temaIntro").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaTemaIntro);
        so.FindProperty("volumen").floatValue = 0.072f;
        so.FindProperty("duracionFade").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // el portal: la entrada de la puerta dorada ("Group 290"), un trigger en el medio del dibujo
        var puerta = cascada.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s => s.name.EndsWith(" Group 290"));
        if (puerta == null) { Debug.LogWarning("[Intro] No encontre la puerta dorada (Group 290) para el portal."); return; }
        var b = puerta.bounds;
        var portal = new GameObject("Portal (fin de la intro)");
        portal.transform.SetParent(cascada, false);
        portal.transform.position = new Vector3(b.center.x, b.min.y + b.size.y * 0.35f, 0f);
        var caja = portal.AddComponent<BoxCollider2D>();
        caja.isTrigger = true;
        caja.size = new Vector2(b.size.x * 0.4f, b.size.y * 0.6f);
        portal.AddComponent<PortalFinIntro>();
    }

    // ---------- ajustes a mano que se conservan ----------
    // Lo que cuelga de "Escenario" es el dibujo del Figma, salvo la chica, los carteles, el lore y el dialogo.
    private static bool EsDelDibujo(string ruta)
    {
        if (!ruta.StartsWith("Escenario/")) return false;
        foreach (var p in new[] { "/Chica", "/Cartel", "Lore", "Dialogo", "/Donde", "/Boca" })
            if (ruta.Contains(p)) return false;
        return true;
    }

    // Para el dibujo: los valores cambiados a mano se igualan a la base (asi gana lo que arma el menu), lo agregado a
    // mano no se pasa y lo que se borro (o se movio a otro grupo) a mano vuelve a estar.
    private static void SinAjustesDelDibujo(ConservarAjustes.Foto actual, ConservarAjustes.Foto anterior)
    {
        foreach (var k in actual.valores.Keys.ToList())
        {
            // la chica y los carteles: su lugar depende del dibujo (y de su escala), asi que lo pone el menu; el resto
            // de sus ajustes (textos, estilos...) se conserva
            string obj = k.Split('|')[0];
            bool lugarDeChicaOCartel = (obj.EndsWith("/Chica") || obj.Contains("/Cartel")) &&
                                       (k.Contains("m_LocalPosition") || k.Contains("m_AnchoredPosition"));
            if (!EsDelDibujo(obj) && !lugarDeChicaOCartel) continue;
            if (anterior.valores.TryGetValue(k, out var v)) actual.valores[k] = v; else actual.valores.Remove(k);
        }
        foreach (var o in actual.objetos.ToList()) if (EsDelDibujo(o) && !anterior.objetos.Contains(o)) actual.objetos.Remove(o);
        foreach (var o in anterior.objetos) if (EsDelDibujo(o)) actual.objetos.Add(o);
        foreach (var c in actual.componentes.ToList())
            if (EsDelDibujo(c.Split('|')[0]) && !anterior.componentes.Contains(c)) actual.componentes.Remove(c);
    }

    // ---------- sorting layers ----------
    // La capa de cada objeto sale del grupo de Figma de su capa (el numero del principio de su nombre, "012 ...").
    // Lo que no tiene numero (columnas y vasija flotantes, brillos, destellos) toma la capa de la que esta mas cerca
    // en el orden de dibujo. Dentro de cada sorting layer, los ordenes van 1, 2, 3... en el mismo orden que tenian.
    private static readonly Dictionary<int, string> capaDeI = new Dictionary<int, string>();

    private static string CapaDeRuta(string ruta)
    {
        string r = ruta ?? "";
        if (r.StartsWith("1 ")) return Capas.Fondo;
        if (r.StartsWith("2 ")) return Capas.Edificios;
        if (r.StartsWith("3 ")) return Capas.FondoDetalles;
        if (r.StartsWith("4 ")) return Capas.Decorado;
        if (r.StartsWith("6 ")) return Capas.Frente;
        return Capas.Escenario;   // 5 · Escenario y lo suelto (el pilar de afuera)
    }

    private static string CapaDe(Renderer r)
    {
        if (r.GetComponentInParent<MovimientoPersonaje>() != null || r.name == "Chica") return Capas.Personajes;
        for (var t = r.transform; t != null; t = t.parent)
        {
            var m = System.Text.RegularExpressions.Regex.Match(t.name, @"^(\d{3}) ");
            if (m.Success && capaDeI.TryGetValue(int.Parse(m.Groups[1].Value), out var capa)) return capa;
        }
        // sin numero y ya repartido (segunda pasada): se queda en su capa
        if (r.sortingLayerName != "Default") return r.sortingLayerName;
        // sin numero: por el orden de dibujo (orden = i * 10 + sub - 3000; los agregados van de -1 a +6 de su capa)
        int i =Mathf.FloorToInt((r.sortingOrder + 3003) / 10f);
        var cerca = capaDeI.Keys.Where(k => k <= i).DefaultIfEmpty(capaDeI.Keys.Min()).Max();
        return capaDeI.TryGetValue(cerca, out var c2) ? c2 : Capas.Escenario;
    }

    // orden global de antes (todo en Default) -> orden nuevo dentro de su capa, de la primera pasada
    private static readonly Dictionary<string, SortedList<int, int>> globalARango = new Dictionary<string, SortedList<int, int>>();

    private static void PonerSortingLayers(Transform raiz, bool primera = true)
    {
        if (primera) globalARango.Clear();
        var todos = raiz.GetComponentsInChildren<Renderer>(true).Select((r, n) => (r, n, capa: CapaDe(r))).ToList();
        foreach (var grupo in todos.GroupBy(x => x.capa))
        {
            // clave de orden: en la primera pasada todo esta en la escala de antes (global). En la segunda, los
            // ordenes ya repartidos (1, 2, 3...) se usan tal cual, y si un ajuste a mano trajo un orden de antes
            // (-3000 a 0), se ubica entre los que tenian ordenes de antes parecidos.
            float Clave((Renderer r, int n, string capa) x)
            {
                int o = x.r.sortingOrder;
                if (primera || o > 0 || !globalARango.TryGetValue(x.capa, out var mapa) || mapa.Count == 0) return o;
                float rango = 0.5f;
                foreach (var kv in mapa) { if (kv.Key <= o) rango = kv.Value + 0.5f; else break; }
                return rango;
            }
            int rangoActual = 0; float anterior = float.MinValue;
            foreach (var x in grupo.Select(x => (x, clave: Clave(x))).OrderBy(y => y.clave).ThenBy(y => y.x.n))
            {
                if (x.clave != anterior) { rangoActual++; anterior = x.clave; }
                if (primera)
                {
                    if (!globalARango.TryGetValue(x.x.capa, out var mapa)) globalARango[x.x.capa] = mapa = new SortedList<int, int>();
                    mapa[x.x.r.sortingOrder] = rangoActual;
                }
                x.x.r.sortingLayerName = x.x.capa;
                x.x.r.sortingOrder = rangoActual;
            }
        }
    }

    // la luz global ilumina todas las sorting layers (si no, lo que no esta en Default se veria oscuro)
    private static void LuzEnTodasLasCapas(Light2D luz)
    {
        if (luz == null) return;
        var so = new SerializedObject(luz);
        var p = so.FindProperty("m_ApplyToSortingLayers");
        if (p == null) return;
        var capas = SortingLayer.layers;
        p.arraySize = capas.Length;
        for (int k = 0; k < capas.Length; k++) p.GetArrayElementAtIndex(k).intValue = capas[k].id;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // los objetos de primer nivel que arma este menu (los demas que haya en la escena no se tocan)
    private static readonly HashSet<string> RaicesDelMenu = new HashSet<string>
    {
        "Main Camera", "Luz global", "Escenario", "Pantalla de inicio", "UI",   // "UI": antes "Canvas" (Franco lo renombro)
        "Musica",
    };

    // el centro de la camara en la cueva, medido desde arriba (px de la escena): el encuadre de antes
    private const float CentroDesdeArribaPx = 1080f;

    // ---------- que hace cada capa (por su nombre en Figma) ----------

    // solo el agua y sus reflejos se mecen; las sombras oscuras de abajo (Vector..., Group 284 de "09 agua",
    // sombra bajo piedra, Rectangle 7) quedan quietas, pegadas a sus piedras
    private static bool EsAgua(Capa c) => c.nombre == "agua" || c.nombre.StartsWith("reflejo");

    private static bool EsPiso(Capa c) =>
        (c.nombre == "pilar" && c.ruta.Contains("piedras y cascadas")) || c.ruta.EndsWith("10 plataformas") ||
        c.nombre.StartsWith("bloque agua") || c.nombre.StartsWith("07 plataformas");

    // ---------- los bloques con chorritos de agua de la cueva, juntos ----------
    // Los pilares negros, los chorritos de "agua cayendo", sus brillos y las rayitas de arriba (de "piedras y
    // cascadas (encima del agua)", en la cueva) van de hijos de un solo objeto: moviendolo se mueve toda la fila.
    // El objeto queda en el medio de la fila, a la altura de la base.
    private static Transform bloquesDeLaCueva;

    private static bool EsBloqueDeLaCueva(Capa c) =>
        c.ruta == "5 · Escenario/piedras y cascadas (encima del agua)" && c.x + c.ancho / 2f < AnchoCuevaPx;

    private static Transform BloquesDeLaCueva(Transform padre, Manifiesto man)
    {
        if (bloquesDeLaCueva != null) return bloquesDeLaCueva;
        var todos = man.capas.Where(EsBloqueDeLaCueva).ToList();
        float minX = todos.Min(c => c.x), maxX = todos.Max(c => c.x + c.ancho), abajo = todos.Max(c => c.y + c.alto);
        var go = new GameObject("Bloques con agua (cueva)");
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3((minX + maxX) / 2f / PixelesPorUnidad, (AltoEscenaPx - abajo) / PixelesPorUnidad, 0f);
        bloquesDeLaCueva = go.transform;
        return bloquesDeLaCueva;
    }

    // el material de las piedras con tiras de agua dibujadas (bloques de la parte 2): solo lo claro (el agua) se
    // anima y cae; la piedra queda como esta (sin tenirla ni fundir la parte de abajo). Se ajusta solo la primera vez,
    // asi se respetan los cambios a mano en el material.
    private static Material MaterialPiedrasConAgua()
    {
        // Estefi (19/11): las piedras quedan como en el Figma (el shader animaba sus tiras de agua y debajo del agua
        // se veian pedazos claros ondulados que no estan en el Figma)
        if (!AgregadosQueNoEstanEnElFigma) return null;
        string ruta = $"{CarpetaAnimaciones}/Cascaditas_piedras.mat";
        bool nuevo = AssetDatabase.LoadAssetAtPath<Material>(ruta) == null;
        var m = MaterialAnimado("Pomelo/CascaditasAgua", "Cascaditas_piedras");
        if (m != null && nuevo)
        {
            m.SetFloat("_TonoPiedra", 0f);
            m.SetFloat("_FundidoAbajo", 0f);
            m.SetFloat("_Umbral", 0.6f);
            EditorUtility.SetDirty(m);
        }
        return m;
    }

    // ---------- piedras agrandadas para la chica ----------
    // Caro: las estructuras de piedra de la parte 2 tienen que ser proporcionales a la chica, con lugar para caminar.
    // Las piedras que se tocan (o casi) forman un grupo, y cada grupo se agranda desde su base (abajo, en el agua):
    //   - grupos angostos: EscalaPiedras para arriba y para los costados, y si aun asi quedan mas angostos que
    //     AnchoMinimoPiedra, se ensanchan hasta ese ancho
    //   - grupos anchos (AnchoYaComodo o mas: ya hay lugar para caminar): solo mas altos, asi no se comen el resto
    // Lo que esta parado arriba de un grupo (la puerta dorada) sube lo mismo que crecio la piedra.
    // Las piedras de la cueva no se tocan (la chica no llega ahi).
    private const bool AgrandarPiedrasParaLaChica = false;
    // agua delante de lo que flota y tiras de agua animadas en las piedras y en las plataformas de agua de la derecha.
    // Estefi (19/11) los habia apagado porque no estan en el Figma; Caro (19/11) pidio volver a aplicar la animacion
    // al agua y a las cascadas.
    private const bool AgregadosQueNoEstanEnElFigma = true;
    // hasta donde se hunde la chica en el agua (px de la escena, desde arriba): la guia roja del Figma
    private const float FondoDelAguaPx = 2134.8f;
    private const float EscalaPiedras = 1.4f;
    private const float AnchoMinimoPiedra = 200f;   // px de la escena (la chica mide unos 110 de ancho)
    private const float AnchoYaComodo = 450f;
    private const float JuntasHasta = 4f;            // px: piedras a esta distancia o menos van juntas

    private class Piedra { public GameObject go; public float x, y, ancho, alto, subio; public Colocada col; }
    private static readonly List<Piedra> piedras = new List<Piedra>();
    private static readonly List<Piedra> decorados = new List<Piedra>();

    private static bool EsPiedraAgrandable(Capa c) =>
        c.x >= AnchoCuevaPx &&
        (c.nombre.StartsWith("bloque agua") || c.ruta.EndsWith("12 plantas de fondo"));

    private static void AgrandarPiedras()
    {
        var orden = piedras.OrderBy(p => p.x).ToList();
        var grupos = new List<List<Piedra>>();
        foreach (var p in orden)
        {
            var g = grupos.LastOrDefault();
            if (g != null && p.x <= g.Max(q => q.x + q.ancho) + JuntasHasta) g.Add(p);
            else grupos.Add(new List<Piedra> { p });
        }
        foreach (var g in grupos)
        {
            float minX = g.Min(p => p.x), maxX = g.Max(p => p.x + p.ancho);
            float abajo = g.Max(p => p.y + p.alto);
            float ancho = maxX - minX;
            float sy = EscalaPiedras;
            float sx = ancho >= AnchoYaComodo ? 1f : Mathf.Max(EscalaPiedras, AnchoMinimoPiedra / Mathf.Max(1f, ancho));
            float cx = (minX + maxX) / 2f;
            // que no se salga por la derecha del dibujo
            float corrido = Mathf.Max(0f, cx + (maxX - cx) * sx - (AnchoEscenaPx - 5f));
            foreach (var p in g)
            {
                float nx = cx + (p.x - cx) * sx - corrido;
                float ny = abajo + (p.y - abajo) * sy;
                // cuanto subio el borde de arriba de esta piedra (para lo que este parado encima)
                p.subio = p.y - ny;
                p.go.transform.localPosition = new Vector3(nx / PixelesPorUnidad, (AltoEscenaPx - ny) / PixelesPorUnidad, 0f);
                p.go.transform.localScale = new Vector3(sx, sy, 1f);
                if (p.col != null) { p.col.x = nx; p.col.y = ny; p.col.escala = sy; }
            }
        }
        // lo que esta parado arriba de una piedra (la puerta dorada) sube lo mismo que subio esa piedra
        foreach (var d in decorados)
        {
            float base_ = d.y + d.alto, centro = d.x + d.ancho / 2f;
            var debajo = piedras.Where(p => centro >= p.x && centro <= p.x + p.ancho && Mathf.Abs(p.y - base_) < 40f)
                                .OrderBy(p => Mathf.Abs(p.y - base_)).FirstOrDefault();
            if (debajo == null || debajo.subio <= 0f) continue;
            d.go.transform.localPosition += new Vector3(0f, debajo.subio / PixelesPorUnidad, 0f);
        }
    }

    // una pieza recortada de una capa: su sprite (en la carpeta piezas), su lugar y tamano (px de la escena)
    private class Pieza { public int n; public Sprite sprite; public string ruta; public float x, y, ancho, alto; public int imgW, imgH; }

    // separa una capa en piezas: cada tramo de columnas con algo dibujado
    private static List<Pieza> SepararEnPiezas(Capa c)
    {
        var res = new List<Pieza>();
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(c.Ruta));
        int W = tex.width, H = tex.height;
        var hay = new bool[W];
        for (int x = 0; x < W; x++)
            for (int y = 0; y < H && !hay[x]; y++)
                hay[x] = tex.GetPixel(x, y).a > 0.15f;
        var tramos = new List<(int desde, int hasta)>();
        for (int x = 0, desde = -1; x <= W; x++)
        {
            bool on = x < W && hay[x];
            if (on && desde < 0) desde = x;
            if (!on && desde >= 0) { tramos.Add((desde, x)); desde = -1; }
        }
        string carpetaPiezas = Carpeta + "/05_Escenario/piedras_y_cascadas/piezas";
        Directory.CreateDirectory(carpetaPiezas);
        var rutas = new List<string>();
        for (int i = 0; i < tramos.Count; i++)
        {
            int w = tramos[i].hasta - tramos[i].desde;
            var p = new Texture2D(w, H, TextureFormat.RGBA32, false);
            p.SetPixels(tex.GetPixels(tramos[i].desde, 0, w, H));
            p.Apply();
            string rp = $"{carpetaPiezas}/{Path.GetFileNameWithoutExtension(c.archivo)}_pieza_{i + 1}.png";
            File.WriteAllBytes(rp, p.EncodeToPNG());
            Object.DestroyImmediate(p);
            rutas.Add(rp);
        }
        Object.DestroyImmediate(tex);
        AssetDatabase.Refresh();
        for (int i = 0; i < tramos.Count; i++)
        {
            int w = tramos[i].hasta - tramos[i].desde;
            var sprite = ImportarPieza(rutas[i], PixelesPorUnidad * c.F);
            if (sprite == null) continue;
            res.Add(new Pieza { n = i + 1, sprite = sprite, ruta = rutas[i], x = c.x + tramos[i].desde / c.F, y = c.y,
                                ancho = w / c.F, alto = c.alto, imgW = w, imgH = H });
        }
        return res;
    }

    // algo que flota (columna o jarron): la base en el agua, el dibujo encima, y agua translucida delante de la base
    private static Transform ArmarFlotante(Capa c, Sprite sprite, Transform padre, string nombre, int orden, float desfase)
    {
        var (desde, hasta) = BajoElAgua(c);
        float centro = (desde + hasta) / 2f;
        var raiz = Flotar(padre, nombre, sprite, 1f, centro, LineaDelAguaPx, c.x, c.y, orden, desfase);
        AguaDelante(raiz, centro, hasta - desde, orden + 3);
        return raiz;
    }

    // de donde a donde (x, px de la escena) llega lo que una capa tiene debajo de la linea del agua
    private static (float desde, float hasta) BajoElAgua(Capa c)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(c.Ruta));
        int linea = Mathf.Clamp(Mathf.CeilToInt((LineaDelAguaPx - c.y) * c.F), 0, tex.height - 1);
        int min = int.MaxValue, max = int.MinValue;
        for (int yArriba = linea; yArriba < tex.height; yArriba++)
            for (int x = 0; x < tex.width; x++)
                if (tex.GetPixel(x, tex.height - 1 - yArriba).a > 0.08f) { if (x < min) min = x; if (x > max) max = x; }
        Object.DestroyImmediate(tex);
        if (max < min) return (c.x, c.x + c.ancho);   // no llega al agua: toda su base
        return (c.x + min / c.F, c.x + (max + 1) / c.F);
    }

    // ---------- la chica ----------

    // el dibujo de la chica quieta (el de ChicaIdle): se busca por su GUID, asi no importa si se mueve de carpeta
    private const string GuidChicaQuieta = "f246479d5783035438e7d2f1468faa88";
    // donde pisa (Estefi, 19/11): al principio de la fila de piedras de la cueva, junto a la columna caida, sobre el
    // pilar "070", a 100 px de su borde izquierdo. Desde ahi recorre la cueva y la parte 2 (la camara la sigue).
    private const string ChicaArrancaEn = "070 pilar";
    private const float ChicaEnElBloquePx = 140f;
    // donde empieza lo que se puede recorrer (px de la escena): la columna caida de la cueva (que tiene collider)
    private const float InicioRecorridoPx = 1700f;
    // tamano: en el nivel mide 2.4 unidades; aca el dibujo de la cueva esta a otra escala (1.4)
    private const float EscalaChica = 1f;   // la chica mide lo mismo que en el nivel (antes 1.4)
    // cuanto se pasa el agua de adelante a cada lado de la base de lo que flota
    private const float MargenAguaColumna = 110f;

    // lo que se puede pisar: la imagen, la zona que se usa, donde quedo (arriba a la izquierda, px de la escena),
    // cuanto se agrando y f = pixeles de la imagen por pixel de la escena. go = el objeto (los pisos van como
    // componentes de el, asi lo siguen si se mueve o se agranda a mano).
    private class Colocada { public string ruta; public RectInt zona; public float x, y, escala, f = 1f; public GameObject go; }
    private static readonly List<Colocada> colocadas = new List<Colocada>();

    // La chica jugable: el prefab Assets/_Prefabs/Chica.prefab es una copia de la del nivel (con todos sus
    // componentes: moverse, correr, saltar, dash, atacar, pasos, vida). Arranca quieta y PantallaInicio le da los
    // controles cuando la camara llega a la parte 2.
    private const string RutaPrefabChica = "Assets/_Prefabs/Chica.prefab";

    private static GameObject PonerChica(Transform padre, int orden, Colocada piso)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabChica);
        GameObject go;
        if (prefab != null) go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        else
        {
            // sin el prefab, al menos el dibujo quieto
            Debug.LogWarning("No encontre " + RutaPrefabChica + ": la chica queda quieta, sin controles.");
            var sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(GuidChicaQuieta))
                .OfType<Sprite>().FirstOrDefault();
            if (sprite == null) return null;
            go = new GameObject("Chica");
            go.AddComponent<SpriteRenderer>().sprite = sprite;
        }
        go.name = "Chica";
        go.transform.SetParent(padre, false);
        go.transform.localScale = Vector3.one * EscalaChica;
        var sr = go.GetComponent<SpriteRenderer>();
        sr.sortingLayerID = 0;   // la capa Default, como todo el dibujo de la escena
        sr.sortingOrder = orden;
        // sobre el primer bloque: x a ChicaEnElBloquePx del borde, y en el borde de arriba ahi
        float xPx = AnchoCuevaPx + 300f, pisoPx = LineaDelAguaPx;
        if (piso != null)
        {
            var arriba = BordeDeArriba(piso);
            int col = Mathf.Clamp(Mathf.RoundToInt(ChicaEnElBloquePx * piso.f), 0, arriba.Length - 1);
            xPx = piso.x + ChicaEnElBloquePx * piso.escala;
            pisoPx = piso.y + Mathf.Max(0, arriba[col]) / piso.f * piso.escala;
        }
        // los pies DIBUJADOS quedan sobre la piedra. El colisionador de la chica termina un poquito mas arriba que
        // sus pies dibujados: los pisos se suben esa diferencia (pisoSobreLaPiedra), asi parada se ve apoyada.
        float pies = sr.sprite != null ? sr.sprite.bounds.min.y : 0f;
        pisoSobreLaPiedra = 0f;
        if (go.TryGetComponent<CapsuleCollider2D>(out var capsula))
            pisoSobreLaPiedra = Mathf.Max(0f, (capsula.offset.y - capsula.size.y / 2f - pies) * EscalaChica);
        go.transform.position = new Vector3(xPx / PixelesPorUnidad,
                                            (AltoEscenaPx - pisoPx) / PixelesPorUnidad - pies * EscalaChica + 0.02f, 0f);
        return go;
    }

    // cuanto (unidades) quedan los pisos invisibles por encima del borde dibujado de las piedras (ver PonerChica)
    private static float pisoSobreLaPiedra;

    private static void ArmarPisosParte2(Transform padre)
    {
        var pisos = new GameObject("Pisos (invisibles)").transform;
        pisos.SetParent(padre, false);
        foreach (var c in colocadas) PisosDe(c, true);
        // el agua: la chica cae adentro hasta la guia roja del Figma (frame "intro juego · escena", guia Y = 5930,
        // 2134.8 px de la escena), y el agua de adelante (capa Frente) le pasa por delante
        // (desde la columna caida de la cueva: la chica arranca ahi y recorre la cueva y la parte 2)
        float inicioX = InicioRecorridoPx, finX = AnchoEscenaPx;
        Caja(pisos, "agua", inicioX, FondoDelAguaPx, finX - inicioX, 200f, false);
        // paredes en los bordes de la parte 2
        Caja(pisos, "pared izquierda", inicioX - 100f, -2000f, 100f, 4500f, false);
        Caja(pisos, "pared derecha", finX, -2000f, 100f, 4500f, false);
    }

    // las importa como sprite, con el punto arriba a la izquierda (como las coordenadas de Figma), sin achicarlas.
    // Si ya estan bien importadas no se tocan (reimportar todo cada vez tarda mucho y llena la memoria).
    private static Sprite ImportarPieza(string ruta, float pixelesPorUnidad = PixelesPorUnidad)
    {
        if (AssetImporter.GetAtPath(ruta) == null) AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
        {
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            bool listo = imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single &&
                         imp.maxTextureSize == 4096 && !imp.mipmapEnabled &&
                         ajustes.spriteAlignment == (int)SpriteAlignment.TopLeft &&
                         Mathf.Abs(ajustes.spritePixelsPerUnit - pixelesPorUnidad) < 0.01f;
            if (!listo)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.ReadTextureSettings(ajustes);
                ajustes.spriteAlignment = (int)SpriteAlignment.TopLeft;
                ajustes.spritePivot = new Vector2(0f, 1f);
                ajustes.spritePixelsPerUnit = pixelesPorUnidad;
                ajustes.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(ajustes);
                imp.maxTextureSize = 4096;
                imp.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ---------- dialogo al llegar a la parte 2 ----------
    // Usa los dialogos de Franco (Dialogo + CuadroDialogo, el cuadro lo arma Pomelo -> Dialogos -> Armar cuadro de
    // dialogo). Cuando la camara llega a la parte 2, Jaya comenta que sale a explorar; recien al cerrar el dialogo
    // la chica se puede mover (lo maneja PantallaInicio). Las lineas se pueden cambiar en el Inspector del objeto
    // "Dialogo al llegar (Jaya)".
    private static readonly (string personaje, string texto)[] LineasAlLlegar =
    {
        // el texto de Caro (lo habia escrito en el cuadro de dialogo de la escena)
        ("Jaya", "“Parece que la crecida del río trajo objetos que nunca había visto. Que raro...”"),
        ("Jaya", "“¿Eso de allá es un portal? ¿A dónde llevará?”"),
    };

    private static Dialogo ArmarDialogoAlLlegar(Transform padre)
    {
        // el cuadro de dialogo va en un Canvas como el del HUD del nivel (1920 x 1080), arriba de la pantalla de inicio
        // se crea llamado "Canvas" porque ArmarCuadroDialogo pone el cuadro en el Canvas que se llama asi (si no, lo
        // ponia en el "UI" viejo, que despues se borra, y la escena quedaba sin cuadro de dialogo); despues pasa a "UI"
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var escalador = canvasGo.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        ArmarCuadroDialogo.Armar();
        canvasGo.name = "UI";
        if (canvasGo.transform.Find("Cuadro de dialogo") == null)
            Debug.LogError("[Inicio] El cuadro de dialogo no quedo en el Canvas de la intro.");
        Selection.activeGameObject = null;
        // igual que el cuadro del nivel (Nivel1_Templo, que Franco ajusto a mano): 248 de alto y el nombre 2 px mas
        // arriba, asi queda a la misma altura que el resto de los cuadros de dialogo
        var cuadro = canvasGo.transform.Find("Cuadro de dialogo/Cuadro") as RectTransform;
        if (cuadro != null)
        {
            // (Franco, "Tamano cuadro de dialogo": mas ancho y mas abajo)
            cuadro.anchoredPosition = new Vector2(10.5f, 70f);
            cuadro.sizeDelta = new Vector2(1800f, 248f);
            if (cuadro.Find("Nombre") is RectTransform nombreRt)
                nombreRt.anchoredPosition = new Vector2(nombreRt.anchoredPosition.x, -44f);
            // el texto, como en el nivel: centrado en alto (no arriba) y con un margen de abajo de 16.9
            var textoDialogo = cuadro.Find("Texto")?.GetComponent<TextMeshProUGUI>();
            if (textoDialogo != null)
            {
                textoDialogo.verticalAlignment = VerticalAlignmentOptions.Middle;
                textoDialogo.margin = new Vector4(0f, 0f, 0f, 16.907791f);
            }
        }

        var go = new GameObject("Dialogo al llegar (Jaya)");
        go.transform.SetParent(padre, false);
        var so = new SerializedObject(go.AddComponent<Dialogo>());
        var lineas = so.FindProperty("lineas");
        lineas.arraySize = LineasAlLlegar.Length;
        for (int i = 0; i < LineasAlLlegar.Length; i++)
        {
            var l = lineas.GetArrayElementAtIndex(i);
            l.FindPropertyRelative("sinNombre").boolValue = false;
            l.FindPropertyRelative("personaje").stringValue = LineasAlLlegar[i].personaje;
            l.FindPropertyRelative("texto").stringValue = LineasAlLlegar[i].texto;
        }
        // Lo usa el Lore de la vasija: arranca apagado; cuando aparece el Lore, el dialogo se pone sobre el y se
        // prende: aparece el cartel 'Interactuar' (la chica esta cerca) y con E (o L1) se abre el cuadro.
        so.FindProperty("distancia").vector2Value = new Vector2(3.5f, 3f);
        so.FindProperty("cartelInteractuar").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_UI/Carteles/Cartel_interactuar.prefab");
        so.FindProperty("soloUnaVez").boolValue = true;
        so.FindProperty("abrirAlEntrar").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        var d = go.GetComponent<Dialogo>();
        d.enabled = false;
        return d;
    }
    // ---------- la maestra (Lady Li Hua), antes de la cascadita ----------
    // Estefi (19/11): pasando el jarron, parada en la piedra de antes de la cascadita (bloque agua 6), aparece la
    // pantalla de la maestra (2 s, easing suave), a los 2 s el dialogo (dos lineas, con las flechas como el resto) y al
    // cerrarlo la pantalla se va igual. Lo maneja EscenaMaestra.
    private const string RutaPantallaMaestra = "Assets/_Pantallas_info/dialogo_maestrabotanica.png";
    // Las tres lineas rojas que marco Estefi en el Figma (pagina "intro juego", Line 3, 4 y 5 sobre la captura
    // "image 331"), pasadas a x del mundo: al cruzarlas pasa cada interaccion.
    private const float LineaJaya = 70.85f;     // Line 3: sobre la piedra de antes del jarron -> dialogo de Jaya (Lore)
    private const float LineaAlejar = 77.9f;    // Line 4: sobre la piedra que sigue al jarron -> la camara se aleja
    private const float LineaLiHua = 87.2f;     // Line 5: sobre el bloque de antes de la cascadita -> Lady Li Hua
    private static readonly (string personaje, string texto)[] LineasMaestra =
    {
        ("Lady Li Hua", "“Es admirable tu valor para entrar aquí y querer conocer los saberes olvidados. Los demás maestros no tuvieron la misma suerte que yo, verás...”"),
        ("Lady Li Hua", "“como mi estatua no fue completamente destruida, puedes ayudarme y no todo está perdido. Las artes todavía se pueden recuperar. Yo sola no puedo hacerlo... te necesito!”"),
    };

    private static void ArmarMaestra(Transform cascada, Transform canvas)
    {
        // la imagen, como sprite de una sola pieza
        if (AssetImporter.GetAtPath(RutaPantallaMaestra) is TextureImporter imp &&
            (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPantallaMaestra);
        if (sprite == null) Debug.LogWarning("[Inicio] No encontre " + RutaPantallaMaestra + ": la maestra queda sin pantalla.");

        // la pantalla: a pantalla completa, detras del cuadro de dialogo
        var pantalla = new GameObject("Pantalla maestra (Lady Li Hua)", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rt = (RectTransform)pantalla.transform;
        rt.SetParent(canvas, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var cuadroDialogo = canvas.Find("Cuadro de dialogo");
        if (cuadroDialogo != null) rt.SetSiblingIndex(cuadroDialogo.GetSiblingIndex());
        var img = pantalla.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        var grupo = pantalla.GetComponent<CanvasGroup>();
        grupo.alpha = 0f; grupo.interactable = false; grupo.blocksRaycasts = false;

        // las lineas (Dialogo de Franco, apagado: lo abre EscenaMaestra)
        var go = new GameObject("Dialogo maestra (Lady Li Hua)");
        go.transform.SetParent(cascada, false);
        var d = go.AddComponent<Dialogo>();
        var sod = new SerializedObject(d);
        var lineas = sod.FindProperty("lineas");
        lineas.arraySize = LineasMaestra.Length;
        for (int i = 0; i < LineasMaestra.Length; i++)
        {
            var l = lineas.GetArrayElementAtIndex(i);
            l.FindPropertyRelative("sinNombre").boolValue = false;
            l.FindPropertyRelative("personaje").stringValue = LineasMaestra[i].personaje;
            l.FindPropertyRelative("texto").stringValue = LineasMaestra[i].texto;
        }
        sod.FindProperty("soloUnaVez").boolValue = true;
        sod.ApplyModifiedPropertiesWithoutUndo();
        d.enabled = false;

        // donde: sobre el bloque agua 6 (el bloque negro de adelante de la columna inclinada), desde su borde
        // izquierdo hasta la cascadita (Vector 195, la primera tira de agua)
        var escenaMaestra = go.AddComponent<EscenaMaestra>();
        var so = new SerializedObject(escenaMaestra);
        so.FindProperty("pantalla").objectReferenceValue = grupo;
        so.FindProperty("dialogo").objectReferenceValue = d;
        var srs = cascada.GetComponentsInChildren<SpriteRenderer>(true);
        var bloque = srs.FirstOrDefault(s => s.name.Contains("bloque agua 6"));
        if (bloque != null)
        {
            var b = bloque.bounds;
            var tira = srs.FirstOrDefault(s => s.name.Contains("Vector 195"));
            float hasta = tira != null && tira.bounds.min.x > b.min.x ? tira.bounds.min.x : b.min.x + 2.5f;
            // arranca al cruzar la linea roja del Figma (sobre este bloque)
            so.FindProperty("desdeX").floatValue = Mathf.Clamp(LineaLiHua, b.min.x, hasta);
            so.FindProperty("hastaX").floatValue = hasta;
            go.transform.position = new Vector3((b.min.x + hasta) / 2f, b.max.y, 0f);
        }
        else Debug.LogWarning("[Inicio] No encontre la cascadita (bloque agua 6): revisar la zona de EscenaMaestra.");
        // la hojita del mural de botanica: brilla verde desde que la camara termina de alejarse hasta que termina el
        // dialogo
        var hoja = srs.FirstOrDefault(s => s.name.Contains("hoja_mural_botanica"));
        so.FindProperty("hoja").objectReferenceValue = hoja;
        // glow verde clarito y, encima, el destello de la gema al 60 % (Estefi, 19/11)
        so.FindProperty("colorBrillo").colorValue = new Color(0.78f, 1f, 0.74f, 1f);
        so.FindProperty("destello").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Entorno/_Items/destello_gema.png");
        so.FindProperty("opacidadDestello").floatValue = 0.6f;
        if (hoja == null) Debug.LogWarning("[Inicio] No encontre la hojita del mural de botanica (hoja_mural_botanica).");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // como dejo el menu la escena la ultima vez (para saber que se cambio a mano)
    private const string RutaBase = "Assets/Editor/ArmarInicioIntroduccion_base.json";

    // un material por capa de agua (cada imagen tiene otro tamano), con el shader Pomelo/AguaOndulante
    private static Material MaterialAgua(string capa, Sprite sprite)
    {
        var shader = Shader.Find("Pomelo/AguaOndulante");
        if (shader == null) { Debug.LogWarning("No encontre el shader Pomelo/AguaOndulante: el agua queda quieta."); return null; }
        // todo lo que se anima va en Assets/_Animaciones/_inicio_introduccion (pedido de Caro)
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/Agua_{capa}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        mat.SetFloat("_AnchoPx", sprite.rect.width);
        mat.SetFloat("_AltoPx", sprite.rect.height);
        // (Caro, 19/11: volver a la animacion del agua de antes) el movimiento (velocidad, rayitas, vaiven, brillo) no
        // se pisa aca: queda el que tiene cada material, asi se puede retocar a mano
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // un material con uno de los shaders de animacion de la pantalla (se guarda en CarpetaAnimaciones)
    private static Material MaterialAnimado(string nombreShader, string nombre)
    {
        var shader = Shader.Find(nombreShader);
        if (shader == null) { Debug.LogWarning("No encontre el shader " + nombreShader + ": queda sin animar."); return null; }
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/{nombre}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------- brillo de la vasija ----------

    private static readonly Color ColorBrillo = new Color32(0xee, 0xf4, 0xe2, 255);   // la luz clara de la cueva
    // (Caro: mucho mas fino -> 5 y 4; "mucho mas difuso, esta muy duro" -> 2 y 14; "sigue muy duro, mas difuso":
    // sin agrandar la forma, muy difuminado y sin reforzarlo, asi es una luz que se apaga de a poco, sin borde)
    private const float MargenBrillo = 0f;      // cuanto se abre el brillo alrededor del dibujo (px de la escena)
    private const float DesenfoqueBrillo = 26f; // que tan difuminado (px de la escena)

    // el brillo de la vasija: un halo claro con su forma, detras del dibujo y de hijo de el (si el dibujo se mueve o
    // se agranda a mano, el brillo lo sigue). Late suave con Resplandor.
    private static void PonerBrillo(Transform raiz, string rutaPieza, float f, float escala, int orden)
    {
        var dibujo = raiz.Find("Dibujo");
        if (dibujo == null) return;
        var sr = Halo(dibujo, rutaPieza, f, "_brillo", "Brillo", MargenBrillo, DesenfoqueBrillo, ColorBrillo, 1f,
                      Vector2.zero, orden);
        if (sr == null) return;
        sr.color = new Color(1f, 1f, 1f, 0.45f);
        sr.gameObject.AddComponent<Resplandor>();
    }

    // ---------- cartel de Caminar ----------
    // El mismo prefab del nivel (Assets/_UI/Carteles/Cartel_caminar, lo arma Pomelo -> Carteles -> Armar carteles),
    // a la derecha de la chica y a la altura de su pecho (el recuadro rojo de Caro). Del mismo tamano con respecto a
    // la chica que en el nivel: alla mide 0.013 con la chica a 1; aca la chica esta a 1.4. Aparece cuando la chica
    // recibe los controles (lo muestra PantallaInicio) y se borra cuando la chica le pasa por delante, como en el
    // nivel. Si se mueve a mano, se conserva.
    // El de Saltar (y doble salto) va despues, sobre el hueco entre el primer bloque y el siguiente (el otro
    // recuadro rojo): aparece cuando el de Caminar se termina de borrar, como en el nivel.
    private const string RutaCartelCaminar = "Assets/_UI/Carteles/Cartel_caminar.prefab";
    private const string RutaCartelSaltar = "Assets/_UI/Carteles/Cartel_saltoysupersalto.prefab";
    // Altura: la misma que en el nivel con respecto a la chica (Caro). En Nivel1_Templo todos los carteles estan en
    // y -28.33 y la chica sale del portal con los pies en -30.26 (centro -29.06, medio alto 1.2): 1.93 por encima de
    // los pies con la chica a 1. Aca la chica esta a 1.4: 2.7 por encima de los pies.
    private const float AlturaCartelSobrePies = (-28.33f - (-29.063f - 1.2f)) * EscalaChica;
    private static readonly Vector2 CartelDesdeLosPies = new Vector2(4.06f, AlturaCartelSobrePies);   // unidades, desde los pies de la chica
    private static readonly Vector2 SaltarDesdeLosPies = new Vector2(5.62f, AlturaCartelSobrePies);
    private const float EscalaCartelEnElNivel = 0.013f;

    // los dos carteles, encadenados: Caminar lo muestra PantallaInicio; Saltar lo muestra Caminar al borrarse
    // (Caro, 19/11) Caminar -> Correr -> Saltar: cuando termina de borrarse uno aparece el siguiente.
    // Los tres se borran al pasarles por delante, como los del nivel.
    private const string RutaCartelCorrer = "Assets/_UI/Carteles/Cartel_correr.prefab";
    private const float SaltarEnXDeCaro = 55.68f;   // x del cartel de Saltar que dejo Caro a mano
    private const float CorrerDespuesDeCaminar = 1.35f;   // anchos del cartel de Caminar, desde el de Caminar

    private static float AnchoEnElMundo(CartelTutorial c)
    {
        var rt = c.transform as RectTransform;
        return rt != null ? rt.rect.width * Mathf.Abs(rt.lossyScale.x) : 4f;
    }

    private static (CartelTutorial caminar, CartelTutorial correr, CartelTutorial saltar) PonerCarteles(Transform padre,
        GameObject chica, ConservarAjustes.Foto actual)
    {
        var caminar = PonerCartel(RutaCartelCaminar, "Cartel_caminar", CartelDesdeLosPies, padre, chica, actual);
        var correr = PonerCartel(RutaCartelCorrer, "Cartel_correr", CartelDesdeLosPies, padre, chica, actual);
        var saltar = PonerCartel(RutaCartelSaltar, "Cartel_saltoysupersalto", SaltarDesdeLosPies, padre, chica, actual);
        if (caminar != null && correr != null)
            correr.transform.position = caminar.transform.position + Vector3.right * AnchoEnElMundo(caminar) * CorrerDespuesDeCaminar;
        // (Caro: todos se borran al pasarles por delante, y la chica camina apenas llega la camara; ninguno la frena.
        //  CerrarConTecla queda por si se quiere volver a usar)
        Encadenar(caminar, correr);
        Encadenar(correr, saltar);
        return (caminar, correr, saltar);
    }

    private static void CerrarConTecla(CartelTutorial c, CartelTutorial.BotonMando boton, params UnityEngine.InputSystem.Key[] teclas)
    {
        if (c == null) return;
        var so = new SerializedObject(c);
        so.FindProperty("cerrarConTecla").boolValue = true;
        var lista = so.FindProperty("teclasParaCerrar");
        lista.arraySize = teclas.Length;
        for (int i = 0; i < teclas.Length; i++) lista.GetArrayElementAtIndex(i).intValue = (int)teclas[i];
        so.FindProperty("botonParaCerrar").enumValueIndex = (int)boton;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // cuando 'antes' termina de borrarse, aparece 'despues'
    private static void Encadenar(CartelTutorial antes, CartelTutorial despues)
    {
        if (antes == null || despues == null) return;
        var so = new SerializedObject(antes);
        var lista = so.FindProperty("mostrarAlTerminarDeBorrarme");
        lista.arraySize = 1;
        lista.GetArrayElementAtIndex(0).objectReferenceValue = despues;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CartelTutorial PonerCartel(string ruta, string nombre, Vector2 desdeLosPies, Transform padre, GameObject chica,
                                              ConservarAjustes.Foto actual)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null || chica == null) { Debug.LogWarning("No encontre " + ruta); return null; }
        // la chica de referencia: la que habia en la escena (si se la movio a mano), si no la recien puesta
        var referencia = chica.transform;
        if (actual != null && actual.porRuta.TryGetValue("Escenario/Cascada (templo)/Chica", out var vieja) && vieja != null)
            referencia = vieja.transform;
        var sr = chica.GetComponent<SpriteRenderer>();
        float pies = chica.transform.position.y + (sr != null && sr.sprite != null ? sr.sprite.bounds.min.y * EscalaChica : 0f);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = nombre;
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(referencia.position.x + desdeLosPies.x, pies + desdeLosPies.y, -0.05f);
        go.transform.localScale = Vector3.one * EscalaCartelEnElNivel * EscalaChica;
        var cartel = go.GetComponent<CartelTutorial>();
        if (cartel != null)
        {
            var so = new SerializedObject(cartel);
            so.FindProperty("esperarAOtroCartel").boolValue = true;   // espera a que lo muestre PantallaInicio / Caminar
            // los del nivel traen enlaces a otros carteles de alla: aca no van (Caminar -> Saltar se enlaza aparte)
            so.FindProperty("desvanecerAlEmpezarABorrarme").arraySize = 0;
            so.FindProperty("mostrarAlTerminarDeBorrarme").arraySize = 0;
            so.FindProperty("desvanecerAlPisarHoja").objectReferenceValue = null;
            so.FindProperty("esperarSalidaDelPortal").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return cartel;
    }

    // ---------- el Lore que sale de la vasija ----------
    // Puntos relativos a la base de la vasija (unidades), medidos sobre la captura de Caro: la linea celeste (donde
    // se activa, a la izquierda de la vasija) y la cruz naranja (donde aparece el Lore, arriba y un poco a la
    // izquierda). Se pueden mover despues en la escena (son objetos hijos).
    // (Caro: que se active cuando la chica llega justo delante del arco, a unas 2.5 unidades de la vasija)
    // (Caro, 19/11: que frene en el borde derecho de la piedra que esta antes de la vasija, x 72.04)
    private static readonly Vector2 LoreSeActiva = new Vector2(-2.55f, 0f);
    private static readonly Vector2 LoreDestino = new Vector2(-0.5f, 3f);
    private const string RutaLore = "Assets/_Entorno/_Items/Lore.png";

    private static void PonerLore(Transform raiz, Sprite dibujoVasija, int orden)
    {
        var lore = AssetDatabase.LoadAllAssetsAtPath(RutaLore).OfType<Sprite>().FirstOrDefault();
        if (lore == null) { Debug.LogWarning("No encontre el Lore en " + RutaLore); return; }
        var go = new GameObject("Lore (sale de la vasija)");
        go.transform.SetParent(raiz, false);

        var activa = new GameObject("Donde se activa").transform;
        activa.SetParent(go.transform, false);
        activa.localPosition = LoreSeActiva;
        var destino = new GameObject("Donde aparece el Lore").transform;
        destino.SetParent(go.transform, false);
        destino.localPosition = LoreDestino;
        // la boca de la vasija: arriba del dibujo (va dentro del dibujo, asi flota con la vasija)
        Transform boca = null;
        var dibujo = raiz.Find("Dibujo");
        if (dibujo != null && dibujoVasija != null)
        {
            boca = new GameObject("Boca de la vasija").transform;
            boca.SetParent(dibujo, false);
            var b = dibujoVasija.bounds;
            boca.localPosition = new Vector3(b.min.x + b.size.x * 0.6f, b.max.y - b.size.y * 0.15f, 0f);
        }

        var so = new SerializedObject(go.AddComponent<LoreDeLaVasija>());
        so.FindProperty("dondeSeActiva").objectReferenceValue = activa;
        so.FindProperty("destino").objectReferenceValue = destino;
        so.FindProperty("boca").objectReferenceValue = boca;
        so.FindProperty("lore").objectReferenceValue = lore;
        so.FindProperty("ordenDibujo").intValue = orden;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Un halo con la forma de una imagen: la forma agrandada 'margen' px y difuminada 'desenfoque' px (px de la
    // escena), del color dado, con 'fuerza' de opacidad. Se calcula a la resolucion de la escena (aunque la imagen
    // este en alta), se guarda al lado de la imagen y se pone de hijo de 'padre', detras ('orden'), corrido 'corrido'
    // px. 'f' = pixeles de la imagen por pixel de la escena. El padre tiene que tener el punto arriba a la izquierda
    // y 1 unidad = 100 px de la escena (como las capas y las piezas).
    private static SpriteRenderer Halo(Transform padre, string rutaImagen, float f, string sufijo, string nombre,
                                       float margen, float desenfoque, Color color, float fuerza, Vector2 corrido, int orden)
    {
        if (!File.Exists(rutaImagen)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(rutaImagen));
        // a la resolucion de la escena: promedio de cada bloque de f x f pixeles
        int paso = Mathf.Max(1, Mathf.RoundToInt(f));
        int w = Mathf.CeilToInt(tex.width / (float)paso), h = Mathf.CeilToInt(tex.height / (float)paso);
        int pad = Mathf.CeilToInt(margen + desenfoque * 2.5f) + 2;
        int W = w + 2 * pad, H = h + 2 * pad;
        var a = new float[W * H];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float suma = 0f; int n = 0;
                for (int yy = y * paso; yy < Mathf.Min(tex.height, (y + 1) * paso); yy++)
                    for (int xx = x * paso; xx < Mathf.Min(tex.width, (x + 1) * paso); xx++) { suma += tex.GetPixel(xx, yy).a; n++; }
                a[(y + pad) * W + x + pad] = n > 0 ? suma / n : 0f;
            }
        Object.DestroyImmediate(tex);

        if (margen >= 1f) a = Promedio(a, W, H, Mathf.RoundToInt(margen), true);
        // tres pasadas de promedio = un desenfoque parejo, sin bordes
        int r = Mathf.Max(1, Mathf.RoundToInt(desenfoque / 1.7f));
        for (int i = 0; i < 3; i++) a = Promedio(a, W, H, r, false);

        var px = new Color[W * H];
        for (int i = 0; i < px.Length; i++)
        {
            var c = color;
            c.a = Mathf.Clamp01(a[i] * fuerza);
            px[i] = c;
        }
        var salida = new Texture2D(W, H, TextureFormat.RGBA32, false);
        salida.SetPixels(px);
        salida.Apply();
        string ruta = Path.Combine(Path.GetDirectoryName(rutaImagen), Path.GetFileNameWithoutExtension(rutaImagen) + sufijo + ".png").Replace('\\', '/');
        File.WriteAllBytes(ruta, salida.EncodeToPNG());
        Object.DestroyImmediate(salida);
        AssetDatabase.Refresh();
        var sprite = ImportarPieza(ruta, PixelesPorUnidad);
        if (sprite == null) return null;

        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = new Vector3((corrido.x - pad) / PixelesPorUnidad, (corrido.y + pad) / PixelesPorUnidad, 0f);
        go.transform.localScale = Vector3.one;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        return sr;
    }

    // promedio en una caja de radio r, primero horizontal y despues vertical. 'maximo' = en vez de promediar, toma
    // el valor mas alto (para agrandar la forma)
    private static float[] Promedio(float[] a, int W, int H, int r, bool maximo)
    {
        var tmp = new float[a.Length];
        var res = new float[a.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float acc = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int xx = Mathf.Clamp(x + k, 0, W - 1);
                    float v = a[y * W + xx];
                    acc = maximo ? Mathf.Max(acc, v) : acc + v;
                }
                tmp[y * W + x] = maximo ? acc : acc / (2 * r + 1);
            }
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float acc = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int yy = Mathf.Clamp(y + k, 0, H - 1);
                    float v = tmp[yy * W + x];
                    acc = maximo ? Mathf.Max(acc, v) : acc + v;
                }
                res[y * W + x] = maximo ? acc : acc / (2 * r + 1);
            }
        return res;
    }

    // para cada columna de la zona: la fila (desde arriba) donde empieza lo dibujado, -1 si no hay nada
    private static int[] BordeDeArriba(Colocada c)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(c.ruta));
        int H = tex.height;
        var arriba = new int[c.zona.width];
        for (int i = 0; i < arriba.Length; i++)
        {
            arriba[i] = -1;
            for (int yArriba = c.zona.y; yArriba < c.zona.y + c.zona.height; yArriba++)
                if (tex.GetPixel(c.zona.x + i, H - 1 - yArriba).a > 0.6f) { arriba[i] = yArriba - c.zona.y; break; }
        }
        Object.DestroyImmediate(tex);
        return arriba;
    }

    // ---------- pisos de la parte 2 (para que la chica pueda caminar y saltar) ----------

    // Pisos invisibles sobre algo agrandado: lo recorre de izquierda a derecha y, donde el borde de arriba de lo
    // dibujado se mantiene parejo, le agrega una caja (BoxCollider2D) AL MISMO OBJETO, en su espacio: asi, si se
    // mueve o se agranda a mano, el piso lo acompana. 'piedra' = suena como plataforma de piedra al caer.
    private static void PisosDe(Colocada c, bool piedra)
    {
        if (c.go == null) return;
        var arriba = BordeDeArriba(c);
        int W = arriba.Length, H = c.zona.height;
        float ppu = PixelesPorUnidad * c.f;                       // pixeles de la imagen por unidad (sin agrandar)
        float subir = pisoSobreLaPiedra / Mathf.Max(0.01f, c.escala);   // en unidades del objeto
        int margen = Mathf.RoundToInt(3 * c.f), minimo = Mathf.RoundToInt(8 * c.f);
        int desde = -1, tope = 0;
        for (int x = 0; x <= W; x++)
        {
            bool sigue = x < W && arriba[x] >= 0 && desde >= 0 && Mathf.Abs(arriba[x] - tope) <= margen;
            if (sigue) { tope = Mathf.Min(tope, arriba[x]); continue; }
            if (desde >= 0 && x - desde >= minimo)
            {
                var caja = c.go.AddComponent<BoxCollider2D>();
                float ancho = (x - desde) / ppu, alto = (H - tope) / ppu + subir;
                caja.size = new Vector2(ancho, alto);
                // el objeto tiene el punto arriba a la izquierda: y para abajo es negativo
                caja.offset = new Vector2(desde / ppu + ancho / 2f, -tope / ppu + subir - alto / 2f);
            }
            desde = x < W && arriba[x] >= 0 ? x : -1;
            tope = desde >= 0 ? arriba[x] : 0;
        }
        if (piedra && c.go.GetComponent<PisoPlataformaPiedra>() == null) c.go.AddComponent<PisoPlataformaPiedra>();
    }

    // caja invisible: x, y = esquina de arriba a la izquierda en px de la escena
    private static void Caja(Transform padre, string nombre, float xPx, float yPx, float anchoPx, float altoPx, bool piedra)
    {
        var go = new GameObject("Piso " + nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3((xPx + anchoPx / 2f) / PixelesPorUnidad,
                                            (AltoEscenaPx - yPx - altoPx / 2f) / PixelesPorUnidad, 0f);
        var caja = go.AddComponent<BoxCollider2D>();
        caja.size = new Vector2(anchoPx / PixelesPorUnidad, altoPx / PixelesPorUnidad);
        if (piedra) go.AddComponent<PisoPlataformaPiedra>();
    }

    // algo que flota: un objeto en el punto de la base (a la altura del agua) con el componente Flotante, y el
    // dibujo de hijo, en su lugar. pivote / esquina en px de la escena (esquina = arriba a la izquierda del dibujo).
    private static Transform Flotar(Transform padre, string nombre, Sprite sprite, float escala, float pivoteX, float pivoteY,
                                    float esquinaX, float esquinaY, int orden, float desfase)
    {
        var raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre, false);
        raiz.transform.localPosition = new Vector3(pivoteX / PixelesPorUnidad, (AltoEscenaPx - pivoteY) / PixelesPorUnidad, 0f);
        var dibujo = new GameObject("Dibujo");
        dibujo.transform.SetParent(raiz.transform, false);
        dibujo.transform.localPosition = new Vector3((esquinaX - pivoteX) / PixelesPorUnidad, (pivoteY - esquinaY) / PixelesPorUnidad, 0f);
        dibujo.transform.localScale = Vector3.one * escala;
        var sr = dibujo.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = orden;
        var so = new SerializedObject(raiz.AddComponent<Flotante>());
        so.FindProperty("desfase").floatValue = desfase;
        // Estefi (19/11): que no floten: quedan quietas, apoyadas en el agua (el agua de adelante sigue tapando la base)
        so.FindProperty("subeYBaja").floatValue = 0f;
        so.FindProperty("seMece").floatValue = 0f;
        // flota el dibujo; este objeto (la base) queda quieto con el agua de adelante de hijo
        so.FindProperty("queFlota").objectReferenceValue = dibujo.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        return raiz.transform;
    }

    // agua translucida delante de la base de algo (como la de la columna de roca de la cueva): tapa la base y hace
    // que se vea metido en el agua. centroX y ancho en px de la escena (el ancho sin los margenes).
    private static void AguaDelante(Transform padre, float centroX, float anchoPx, int orden)
    {
        // Estefi (19/11): solo los elementos del Figma; esta agua agregada no va (el agua del Figma ya pasa por delante)
        if (!AgregadosQueNoEstanEnElFigma) return;
        var agua = AguaDeAdelante();
        if (agua == null) return;
        var mat = MaterialAgua("frente_columnas", agua);
        float altoPx = AltoEscenaPx - LineaDelAguaPx + 2f;
        float ancho = anchoPx + 2f * MargenAguaColumna;
        var a = new GameObject("Agua adelante");
        a.transform.SetParent(padre, false);
        a.transform.position = new Vector3(centroX / PixelesPorUnidad, (AltoEscenaPx - LineaDelAguaPx + 2f) / PixelesPorUnidad, 0f);
        // el tamano es en el mundo: si el padre esta agrandado, se compensa
        Vector3 lp = padre.lossyScale;
        a.transform.localScale = new Vector3(ancho / agua.rect.width / Mathf.Max(0.0001f, Mathf.Abs(lp.x)),
                                             altoPx / agua.rect.height / Mathf.Max(0.0001f, Mathf.Abs(lp.y)), 1f);
        var sr = a.AddComponent<SpriteRenderer>();
        sr.sprite = agua;
        sr.sortingOrder = orden;
        if (mat != null) sr.sharedMaterial = mat;
    }


    // la imagen del agua de adelante: los colores del agua del dibujo (#7b988d arriba -> #374a43 abajo) al 60 %,
    // con la linea clara (#d5decf) en la superficie, y que se desvanece a los costados
    private static Sprite AguaDeAdelante()
    {
        Directory.CreateDirectory(CarpetaAnimaciones);
        string ruta = $"{CarpetaAnimaciones}/Agua_frente_columnas.png";
        const int W = 256, H = 168;
        Color arriba = new Color32(0x7b, 0x98, 0x8d, 255), abajo = new Color32(0x37, 0x4a, 0x43, 255);
        Color linea = new Color32(0xd5, 0xde, 0xcf, 255);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, desdeArriba = (H - 1 - y) / (float)(H - 1);
                float costados = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.22f)) *
                                 Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - u) / 0.22f));
                Color c = Color.Lerp(arriba, abajo, desdeArriba);
                float alfa = 0.6f;
                if (H - 1 - y < 4) { c = linea; alfa = 0.9f; }   // la linea de la superficie
                c.a = alfa * costados;
                px[y * W + x] = c;
            }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            var ajustes = new TextureImporterSettings();
            imp.ReadTextureSettings(ajustes);
            ajustes.spriteAlignment = (int)SpriteAlignment.TopCenter;   // se ubica por el medio de la linea del agua
            ajustes.spritePivot = new Vector2(0.5f, 1f);
            ajustes.spritePixelsPerUnit = PixelesPorUnidad;
            imp.SetTextureSettings(ajustes);
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ---------- puntitos de luz (inicio_17_particulas) ----------

    // los 40 puntitos del SVG de Caro: x, y en px del cuadro de 1234 x 738; z = radio del halo
    private static readonly Vector3[] PuntitosSvg =
    {
        new Vector3(388f, 655f, 4.7f), new Vector3(888f, 585f, 3.5f), new Vector3(936f, 442f, 3.4f), new Vector3(530f, 413f, 4.6f),
        new Vector3(829f, 591f, 4.6f), new Vector3(773f, 517f, 2.7f), new Vector3(864f, 445f, 3.0f), new Vector3(847f, 648f, 2.9f),
        new Vector3(556f, 594f, 3.7f), new Vector3(626f, 446f, 4.0f), new Vector3(383f, 398f, 2.5f), new Vector3(592f, 386f, 3.9f),
        new Vector3(689f, 561f, 2.7f), new Vector3(870f, 433f, 3.4f), new Vector3(956f, 442f, 4.4f), new Vector3(616f, 481f, 4.0f),
        new Vector3(807f, 391f, 3.1f), new Vector3(783f, 651f, 3.7f), new Vector3(911f, 484f, 4.4f), new Vector3(906f, 491f, 3.5f),
        new Vector3(915f, 680f, 4.4f), new Vector3(598f, 557f, 4.5f), new Vector3(683f, 541f, 4.5f), new Vector3(559f, 491f, 4.2f),
        new Vector3(559f, 429f, 2.4f), new Vector3(605f, 447f, 4.0f), new Vector3(625f, 578f, 2.3f), new Vector3(791f, 658f, 4.2f),
        new Vector3(489f, 482f, 3.1f), new Vector3(378f, 429f, 2.6f), new Vector3(726f, 564f, 2.3f), new Vector3(877f, 555f, 3.4f),
        new Vector3(553f, 416f, 3.3f), new Vector3(384f, 399f, 4.1f), new Vector3(887f, 671f, 3.3f), new Vector3(554f, 474f, 2.3f),
        new Vector3(536f, 460f, 4.8f), new Vector3(457f, 676f, 2.2f), new Vector3(833f, 419f, 4.1f), new Vector3(694f, 580f, 3.3f),
    };

    private static void ArmarDestellos(Transform padre, int orden)
    {
        // cada puntito se dibuja solo, como en el SVG (Destellos lo arma con su radio): ya no se recorta de la capa
        const float k = 3612f / 1234f;   // px del SVG -> px de la escena
        int n = PuntitosSvg.Length;
        var go = new GameObject("Destellos (puntitos de luz)");
        go.transform.SetParent(padre, false);
        var so = new SerializedObject(go.AddComponent<Destellos>());
        var pp = so.FindProperty("puntos");
        pp.arraySize = n;
        for (int i = 0; i < n; i++)
        {
            var p = PuntitosSvg[i];
            pp.GetArrayElementAtIndex(i).vector3Value =
                new Vector3(p.x * k / PixelesPorUnidad, (AltoEscenaPx - p.y * k) / PixelesPorUnidad, p.z * k);
        }
        so.FindProperty("desenfoque").floatValue = 1.6f * k;
        so.FindProperty("pixelesPorUnidad").floatValue = PixelesPorUnidad;
        so.FindProperty("ordenDibujo").intValue = orden;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- titulo y "Comenzar" ----------
    // Medidas en px del diseno de Caro: el cuadro de la cueva mide 1234 x 738 (el SVG pantalla_inicio) y las
    // posiciones van desde el centro de ese cuadro (y para arriba). El lienzo escala con el ancho, asi coincide
    // con la cueva que encuadra la camara.
    private const string CarpetaUI = "Assets/_UI/Inicio/";
    // los assets de las animaciones de la pantalla (el shader del agua y sus materiales). Los scripts van en _Scripts.
    private const string CarpetaAnimaciones = "Assets/_Animaciones/_inicio_introduccion";
    private const string CarpetaIosevka = "Assets/_Fuentes/Iosevka";
    private const int PaddingIosevka = 48;   // margen de cada letra en el atlas (a 90 px): da lugar al brillo ancho
    private const string RutaAlegreya = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string Teclado = "Assets/_UI/Carteles/Teclas_y_botones/Teclado/";
    private const string Joystick = "Assets/_UI/Carteles/Teclas_y_botones/Joystick/";
    private static readonly Vector2 Diseno = new Vector2(1234f, 738f);

    private const float TamanoTitulo = 190f;                 // Iosevka Charon Mono Regular, 190 px
    private static readonly Vector2 LugarTitulo = new Vector2(-13f, -36f);   // centro en x, linea de base en y
    // fila "Comenzar  [espacio] o [X]": el mismo formato que los carteles (Alegreya 32 en versalitas, "o" a 15,
    // iconos a su tamano, todo un 4 % mas grande, y la franja oscura difuminada detras)
    private const float EscalaCartel = 78f / 75f;
    private static readonly Vector2 LugarComenzar = new Vector2(-126.5f, -110f);   // borde izquierdo, linea de base
    private static readonly Vector2 LugarEspacio = new Vector2(57f, -99f);
    private static readonly Vector2 LugarO = new Vector2(87f, -100f);
    private static readonly Vector2 LugarEquis = new Vector2(119.5f, -99f);
    private static readonly Vector2 CentroFranja = new Vector2(3f, -100f);
    private static readonly Vector2 TamanoFranja = new Vector2(348f, 64f);   // la fila + 44 px a los costados y 16 arriba/abajo
    private static readonly Color ColorFranja = new Color(0f, 0f, 0f, 0.55f);  // igual que los carteles

    private static void ArmarPantalla(Camera cam, GameObject chica, Dialogo dialogo, CartelTutorial cartel)
    {
        Directory.CreateDirectory(CarpetaUI);
        var alegreya = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaAlegreya);
        var iosevka = FuenteIosevka();

        var go = new GameObject("Pantalla de inicio", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                typeof(PantallaInicio));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = go.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = Diseno;
        escalador.matchWidthOrHeight = 0f;   // con el ancho, como la camara
        var raizLienzo = (RectTransform)go.transform;

        // el titulo y "Comenzar" van juntos para poder desvanecerlos al comenzar
        var lienzo = new GameObject("Textos", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        lienzo.SetParent(raizLienzo, false);
        lienzo.anchorMin = Vector2.zero;
        lienzo.anchorMax = Vector2.one;
        lienzo.offsetMin = lienzo.offsetMax = Vector2.zero;
        var grupoTextos = lienzo.GetComponent<CanvasGroup>();
        grupoTextos.interactable = false;
        grupoTextos.blocksRaycasts = false;

        // titulo, con brillo
        var fuenteTitulo = iosevka != null ? iosevka : alegreya;
        // centrado en Y (Caro): la linea de base va a media altura de las mayusculas por debajo del centro de la
        // pantalla, asi las mayusculas quedan justo en el medio (el acento de la E no cuenta). Se mide con la fuente.
        float bajar = 0f;
        if (fuenteTitulo != null && fuenteTitulo.faceInfo.pointSize > 0 && fuenteTitulo.faceInfo.capLine > 0f)
        {
            var fi = fuenteTitulo.faceInfo;
            float alturaMayusculas = fi.capLine / fi.pointSize * fi.scale * TamanoTitulo;
            bajar = -alturaMayusculas / 2f - LugarTitulo.y;
        }
        // el titulo ahora es la imagen del SVG de Caro (letras + brillo + sombra), ver TituloImagen.cs
        TituloImagen.Crear(lienzo);

        // boton "Comenzar juego" (Figma "intro juego"), ver BotonesInicioEditor.cs (ahi se suman mas botones).
        // La fila vieja "Comenzar [espacio] o [X]" se sigue armando pero queda apagada.
        BotonesInicioEditor.Crear(lienzo, go.GetComponent<PantallaInicio>());
        var fila = new GameObject("Comenzar", typeof(RectTransform)).GetComponent<RectTransform>();
        fila.SetParent(lienzo, false);
        fila.anchorMin = fila.anchorMax = fila.pivot = new Vector2(0.5f, 0.5f);
        fila.sizeDelta = Vector2.zero;
        fila.anchoredPosition = new Vector2(0f, bajar);
        var franja = Franja();
        if (franja != null)
        {
            var img = Imagen("Sombra", fila, franja, CentroFranja, TamanoFranja);
            img.color = ColorFranja;
        }
        var comenzar = Texto("Comenzar", fila, alegreya, 32f * EscalaCartel, FontStyles.SmallCaps,
                             TextAlignmentOptions.BaselineLeft, LugarComenzar + new Vector2(150f, 0f), new Vector2(300f, 60f));
        // sin kerning: con las versalitas, TextMeshPro le aplicaba a la "m" y la "e" el ajuste de las minusculas
        // y la M quedaba metida adentro de la E. Sin kerning todas las letras quedan parejas.
        comenzar.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>();
        Icono(Teclado + "Tecla_barraespaciadora.png", fila, LugarEspacio);
        Texto("o", fila, alegreya, 15f * EscalaCartel, FontStyles.Normal, TextAlignmentOptions.Midline, LugarO,
              new Vector2(30f, 30f));
        Icono(Joystick + "Boton_Equis.png", fila, LugarEquis);
        fila.gameObject.SetActive(false);

        // fundido a negro al comenzar (arriba de todo)
        var negro = new GameObject("Fundido", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        negro.SetParent(raizLienzo, false);
        negro.anchorMin = Vector2.zero;
        negro.anchorMax = Vector2.one;
        negro.offsetMin = negro.offsetMax = Vector2.zero;
        var imgNegro = negro.GetComponent<Image>();
        imgNegro.color = new Color(0f, 0f, 0f, 0f);
        imgNegro.raycastTarget = false;

        var so = new SerializedObject(go.GetComponent<PantallaInicio>());
        so.FindProperty("camara").objectReferenceValue = cam;
        so.FindProperty("fundido").objectReferenceValue = imgNegro;
        so.FindProperty("textos").objectReferenceValue = grupoTextos;
        so.FindProperty("chica").objectReferenceValue = chica;
        // el dialogo de Jaya ya no se abre al llegar: lo abre el Lore de la vasija (Caro)
        so.FindProperty("dialogoAlLlegar").objectReferenceValue = null;
        so.FindProperty("cartelAlLlegar").objectReferenceValue = cartel;
        // el encuadre, con las medidas del dibujo de Figma (unidades: px / 100)
        so.FindProperty("anchoCueva").floatValue = AnchoCuevaPx / PixelesPorUnidad;
        so.FindProperty("inicioOtraParte").floatValue = AnchoCuevaPx / PixelesPorUnidad;
        so.FindProperty("finOtraParte").floatValue = AnchoEscenaPx / PixelesPorUnidad;
        so.FindProperty("altoDibujo").floatValue = AltoEscenaPx / PixelesPorUnidad;
        so.FindProperty("centroDesdeArriba").floatValue = CentroDesdeArribaPx / PixelesPorUnidad;
        // la piedra que sigue al jarron (la de adelante del mural verde): apenas llega (pisandola o saltandola) la camara
        // se aleja en 2 s (Estefi, 19/11; antes era la de antes del jarron, donde ahora frena el dialogo de Jaya)
        so.FindProperty("esperaAlejar").floatValue = 0f;
        so.FindProperty("duracionAlejar").floatValue = 2f;
        var vasija = GameObject.Find("Escenario/Cascada (templo)/Vasija flotante");
        if (vasija != null)
        {
            float xv = vasija.transform.position.x;
            var piedra = colocadas.Where(c => c.go != null && c.go.name.Contains("pieza") && c.go.GetComponent<SpriteRenderer>() != null)
                                  .Select(c => c.go.GetComponent<SpriteRenderer>().bounds)
                                  .Where(b => b.min.x >= xv - 0.5f)
                                  .OrderBy(b => b.min.x).FirstOrDefault();
            if (piedra.size.x > 0f)
            {
                // se aleja al cruzar la linea roja del Figma (sobre esta piedra)
                so.FindProperty("alejarDesdeX").floatValue = Mathf.Clamp(LineaAlejar, piedra.min.x, piedra.max.x);
                so.FindProperty("alejarHastaX").floatValue = piedra.max.x;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        if (iosevka == null)
            Debug.LogWarning("No encontre la fuente Iosevka Charon Mono (un .ttf con 'Charon' y 'Mono' en el nombre, " +
                             "Regular) dentro de " + CarpetaIosevka + ". El titulo quedo con Alegreya: pone el .ttf ahi y " +
                             "volve a armar la escena.");
    }

    // el TTF de Iosevka Charon Mono Regular que este en Assets/_Fuentes/Iosevka -> su fuente de TextMeshPro (estatica)
    private static TMP_FontAsset FuenteIosevka()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaIosevka)) return null;
        string ttf = AssetDatabase.FindAssets("t:Font", new[] { CarpetaIosevka })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(r => { string n = Path.GetFileNameWithoutExtension(r).ToLowerInvariant();
                          return n.Contains("charon") && n.Contains("mono") &&
                                 (n.EndsWith("-regular") || n.EndsWith("regular")) && !n.Contains("italic"); })
            .FirstOrDefault();
        if (ttf == null) return null;

        string ruta = CarpetaIosevka + "/Iosevka Charon Mono SDF.asset";
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (existente != null && existente.atlasPadding >= PaddingIosevka) return existente;
        if (existente != null) AssetDatabase.DeleteAsset(ruta);   // la vieja tenia poco margen para el brillo

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
        // margen MUY grande alrededor de cada letra: el brillo de TextMeshPro solo llega hasta donde llega ese margen,
        // y el del titulo es ancho (como la referencia de Caro)
        var fa = TMP_FontAsset.CreateFontAsset(font, 90, PaddingIosevka, GlyphRenderMode.SDFAA, 2048, 2048,
                                               AtlasPopulationMode.Dynamic, false);
        if (fa == null) { Debug.LogError("No pude crear la fuente de TextMeshPro a partir de " + ttf); return null; }
        fa.name = "Iosevka Charon Mono SDF";
        AssetDatabase.CreateAsset(fa, ruta);
        fa.atlasTextures[0].name = "Iosevka Charon Mono SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = "Iosevka Charon Mono SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        // solo las letras de titulos (con tanto margen no entran todas): mayusculas, minusculas, numeros y acentos.
        // Despues queda estatica, asi el archivo no cambia solo.
        fa.TryAddCharacters(" ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚÑÜabcdefghijklmnopqrstuvwxyzáéíóúñü0123456789.,:;!¡?¿-'\"",
                            out string faltan);
        if (!string.IsNullOrEmpty(faltan)) Debug.Log("Iosevka: no entraron estas letras: " + faltan);
        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        var so = new SerializedObject(fa);
        var limpiar = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (limpiar != null) { limpiar.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
        EditorUtility.SetDirty(fa);
        EditorUtility.SetDirty(fa.atlasTextures[0]);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // material del titulo: sombra oscura y difuminada alrededor de las letras (underlay de TextMeshPro)
    private static Material MaterialConSombra(TMP_FontAsset fuente)
    {
        string ruta = CarpetaUI + "Titulo_TECHNI_sombra.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (mat == null)
        {
            mat = new Material(fuente.material);
            AssetDatabase.CreateAsset(mat, ruta);
        }
        else
        {
            // ya existe: se respetan los ajustes a mano (brillo, sombra...). Solo se asegura que use el atlas de la
            // fuente actual (por si la fuente se rearmo).
            if (fuente.material != null && fuente.material.HasProperty("_MainTex") && mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", fuente.material.GetTexture("_MainTex"));
            EditorUtility.SetDirty(mat);
            return mat;
        }
        // el shader completo de TextMeshPro: es el que tiene sombra (underlay) y brillo (glow) juntos
        var completo = Shader.Find("TextMeshPro/Distance Field");
        if (completo != null) mat.shader = completo;

        // brillo como la referencia de Caro: blanco, fuerte pegado a las letras y que se va apagando lejos
        // (ancho: llega hasta el margen de la fuente, PaddingIosevka)
        mat.EnableKeyword("GLOW_ON");
        // (Caro: "demasiado brillo, bajale un monton" -> mucho mas transparente y mas cerca de las letras)
        // (y despues "todavia mas": casi transparente y mas cerca, asi tampoco se nota el borde cuadrado del halo)
        mat.SetColor("_GlowColor", new Color(1f, 1f, 1f, 0.14f));
        mat.SetFloat("_GlowOffset", 0f);
        mat.SetFloat("_GlowInner", 0f);
        mat.SetFloat("_GlowOuter", 0.3f);
        mat.SetFloat("_GlowPower", 0.45f);   // menos de 1: cae rapido cerca de la letra y deja una cola suave

        // sombra apenas marcada, muy difuminada, solo para que el brillo no se pierda sobre lo claro del fondo
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor("_UnderlayColor", new Color32(0x14, 0x1a, 0x18, 70));
        mat.SetFloat("_UnderlayOffsetX", 0f);
        mat.SetFloat("_UnderlayOffsetY", 0f);
        mat.SetFloat("_UnderlayDilate", 0.6f);
        mat.SetFloat("_UnderlaySoftness", 1f);

        ShaderUtilities.UpdateShaderRatios(mat);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static TextMeshProUGUI Texto(string texto, RectTransform padre, TMP_FontAsset fuente, float tamano,
                                         FontStyles estilo, TextAlignmentOptions alineacion, Vector2 centro, Vector2 caja)
    {
        var go = new GameObject(texto, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = caja;
        rt.anchoredPosition = centro;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) { t.font = fuente; t.fontSharedMaterial = fuente.material; }
        t.text = texto;
        t.fontSize = tamano;
        t.fontStyle = estilo;
        t.color = Color.white;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static Image Imagen(string nombre, RectTransform padre, Sprite sprite, Vector2 centro, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = tamano;
        rt.anchoredPosition = centro;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static void Icono(string ruta, RectTransform padre, Vector2 centro)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (sprite == null) { Debug.LogError("No encontre el icono " + ruta); return; }
        Imagen(Path.GetFileNameWithoutExtension(ruta), padre, sprite, centro, sprite.rect.size * EscalaCartel);
    }

    // la misma franja que los carteles: pareja en el medio, difuminada a los costados y apenas arriba y abajo
    private static Sprite Franja()
    {
        string ruta = CarpetaUI + "Comenzar_sombra.png";
        int W = Mathf.CeilToInt(TamanoFranja.x), H = Mathf.CeilToInt(TamanoFranja.y);
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                float horizontal = Mathf.SmoothStep(0f, 1f, u / 0.15f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.15f);
                float vertical = Mathf.SmoothStep(0f, 1f, v / 0.12f) * Mathf.SmoothStep(0f, 1f, (1f - v) / 0.12f);
                px[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(horizontal * vertical)));
            }
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(ruta) is TextureImporter imp)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void PonerPrimeraEnElBuild()
    {
        var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        lista.RemoveAll(s => s.path == RutaEscena);
        lista.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }
}

