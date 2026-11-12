using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Arma el menu de pausa con los PNG de Assets/_UI/Pausa (Frame 62 del Figma, en 1920 x 1080):
//   - el juego de fondo, bien desenfocado y oscurecido (shader de vidrio del HUD), para que no se mezcle con el menu
//   - Marco_pausa encima, con "PAUSA" en la placa de arriba (Alegreya Bold, 55 px, #56653F)
//   - 4 botones: CONTINUAR, NUEVO JUEGO, CONFIGURACION, SALIR DEL JUEGO (Alegreya, 32 px)
//     El elegido se funde a Boton_hover, crece un poquito, tiene una sombra detras y lleva el icono de
//     como aceptar (mouse / X, cambian cada 2.5 s). Texto + icono quedan centrados en el boton, con el
//     mismo espacio entre los dos que en el Figma de CONTINUAR (lo calcula MenuPausa para cada boton).
//   - a la derecha del panel, a la altura de SALIR DEL JUEGO: DESPLAZARSE [rueda] o [stick R] (iconos de 40 px)
//   - sonido al cambiar de boton: Assets/_Sonidos/Pausa_cambio_boton.wav
//   - jugando, abajo a la derecha: aviso "PAUSA [Esc] o [Options]" en una placa redondeada con borde oliva
//     (distinta de los carteles), que aparece al salir la chica del portal y se va la primera vez que se
//     pausa (o sola, a los 10 s)
// Queda guardado como prefab en Assets/_UI/Pausa/Menu_pausa.prefab y en la escena queda una copia conectada.
// Menu:  Pomelo -> Pausa -> Armar menu de pausa
public static class ArmarMenuPausa
{
    private const string NombreObjeto = "Menu pausa";
    private const string Carpeta = "Assets/_UI/Pausa/";
    private const string RutaPrefab = Carpeta + "Menu_pausa.prefab";
    private const string RutaSombraBoton = Carpeta + "sombra_boton.png";
    private const string RutaMatFondo = Carpeta + "Pausa_fondo_desenfocado.mat";
    private const string RutaShaderVidrio = "Assets/_UI/Barravida_y_sabiduria/Vidrio/UI_Vidrio.shader";
    private const string Iconos = "Assets/_UI/Carteles/Teclas_y_botones/";
    private const string RutaFuente = "Assets/_Fuentes/Alegreya/Alegreya SDF.asset";
    private const string RutaTtfBold = "Assets/_Fuentes/Alegreya/Alegreya-Bold.ttf";
    private const string RutaFuenteBold = "Assets/_Fuentes/Alegreya/Alegreya Bold SDF.asset";
    private const string RutaRenderer = "Assets/Settings/Renderer2D.asset";
    private const string RutaSonidoCambio = "Assets/_Sonidos/Pausa_cambio_boton.wav";

    // centro de cada boton, medido desde arriba de la pantalla de 1920 x 1080 (Figma)
    private static readonly float[] AlturaBotones = { 316f, 463f, 610f, 756f };
    private static readonly string[] Textos = { "CONTINUAR", "NUEVO JUEGO", "CONFIGURACIÓN", "SALIR DEL JUEGO" };
    private static readonly MenuPausa.Accion[] Acciones =
        { MenuPausa.Accion.Continuar, MenuPausa.Accion.NuevoJuego, MenuPausa.Accion.Configuracion, MenuPausa.Accion.Salir };

    private const float TamanoTexto = 32f;
    private const float TamanoTitulo = 55f;
    private static readonly Color ColorTitulo = new Color32(0x56, 0x65, 0x3F, 0xFF);   // #56653F

    // ayuda "DESPLAZARSE": tamano del texto, alto (visible) de los iconos y donde termina, a la derecha del panel
    private const float TamanoAyuda = 24f;
    private const float AltoIconosAyuda = 40f;   // antes 28: no se llegaban a leer
    // centrada entre el borde derecho de los botones y el borde de adentro del marco (x = 1477, lineas celestes
    // de la captura), con la parte de abajo alineada con el borde de abajo del boton SALIR DEL JUEGO
    // (el boton termina en y = 802: centro 756 + 46 px de dibujo; la ayuda mide 40 px de alto -> centro en 782)
    // (despues se corrio 10 px mas a la izquierda: 1467)
    private static readonly Vector2 CentroAyuda = new Vector2(1467f, 782f);   // px del Figma (desde arriba a la izquierda)

    [MenuItem("Pomelo/Pausa/Armar menu de pausa")]
    public static void Armar()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Para el Play antes de armar el menu de pausa.");
            return;
        }

        var fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuente);
        if (fuente == null) { Debug.LogError("No encontre la fuente " + RutaFuente + " (se crea con Pomelo -> Carteles -> Armar carteles)."); return; }
        var fuenteBold = FuenteBold();
        if (fuenteBold == null) return;

        // los PNG de Pausa estaban cortados en pedazos: cada uno pasa a ser una sola imagen
        var marco = UnSoloSprite(Carpeta + "Marco_pausa.png");
        var normal = UnSoloSprite(Carpeta + "Boton_normal.png");
        var hover = UnSoloSprite(Carpeta + "Boton_hover.png");
        var rueda = UnSoloSprite(Carpeta + "Desplazarse_mouse.png");
        var stickR = UnSoloSprite(Carpeta + "Desplazarse_joystick.png");
        var mouse = PrimerSprite(Iconos + "Teclado/Click_izquierdo.png");
        var equis = PrimerSprite(Iconos + "Joystick/Boton_Equis.png");
        if (marco == null || normal == null || hover == null || rueda == null || stickR == null || mouse == null || equis == null) return;
        var sombraBoton = SombraDeBoton(Carpeta + "Boton_normal.png");
        var matFondo = MaterialFondo();

        var viejo = GameObject.Find(NombreObjeto);
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        // raiz: Canvas propio, por encima del HUD, que se adapta a la pantalla como el HUD (1920 x 1080)
        var raiz = new GameObject(NombreObjeto, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(MenuPausa));
        Undo.RegisterCreatedObjectUndo(raiz, "Armar menu de pausa");
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
        var escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        var panel = Estirado("Panel", (RectTransform)raiz.transform);

        // 1. el juego de fondo, desenfocado y oscurecido (tapa tambien el HUD)
        var fondo = Estirado("Fondo desenfocado", panel).gameObject.AddComponent<Image>();
        fondo.material = matFondo;
        fondo.raycastTarget = false;

        // 2. marco y titulo
        Imagen("Marco", panel, marco, Vector2.zero, new Vector2(1920f, 1080f));
        var titulo = Texto("Titulo", panel, "PAUSA", TamanoTitulo, DesdeArriba(150f), new Vector2(520f, 90f), fuenteBold, TextAlignmentOptions.Center);
        titulo.color = ColorTitulo;

        // 3. botones (cada uno con su sombra detras)
        var botones = new List<MenuPausa.Boton>();
        for (int i = 0; i < Textos.Length; i++)
        {
            Vector2 pos = DesdeArriba(AlturaBotones[i]);
            var sombra = Imagen("Sombra " + Textos[i], panel, sombraBoton, pos, sombraBoton.rect.size);
            sombra.color = new Color(0f, 0f, 0f, 0f);

            var rtBoton = Hijo("Boton " + Textos[i], panel, pos, new Vector2(600f, 110f));
            var fondoBoton = rtBoton.gameObject.AddComponent<Image>();
            fondoBoton.sprite = normal;
            fondoBoton.raycastTarget = false;

            // el hover se funde encima del normal
            var imgHover = Imagen("Hover", rtBoton, hover, Vector2.zero, new Vector2(600f, 110f));
            imgHover.color = new Color(1f, 1f, 1f, 0f);

            var texto = Texto("Texto", rtBoton, Textos[i], TamanoTexto, Vector2.zero, new Vector2(500f, 60f), fuente, TextAlignmentOptions.Center);

            // icono de como aceptar: mouse y X en el mismo lugar, se turnan (AlternarTeclas)
            var rtIcono = Hijo("Icono", rtBoton, Vector2.zero, new Vector2(40f, 40f));
            var grupo = rtIcono.gameObject.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            var imgMouse = Imagen("Mouse", rtIcono, mouse, Vector2.zero, mouse.rect.size);
            var imgX = Imagen("X", rtIcono, equis, Vector2.zero, equis.rect.size);
            imgX.color = new Color(1f, 1f, 1f, 0f);
            var alternar = new SerializedObject(rtIcono.gameObject.AddComponent<AlternarTeclas>());
            alternar.FindProperty("primero").arraySize = 1;
            alternar.FindProperty("primero").GetArrayElementAtIndex(0).objectReferenceValue = imgMouse;
            alternar.FindProperty("segundo").arraySize = 1;
            alternar.FindProperty("segundo").GetArrayElementAtIndex(0).objectReferenceValue = imgX;
            alternar.FindProperty("intervalo").floatValue = 2.5f;
            alternar.ApplyModifiedPropertiesWithoutUndo();
            rtIcono.gameObject.SetActive(false);

            botones.Add(new MenuPausa.Boton
            {
                accion = Acciones[i], fondo = fondoBoton, hover = imgHover, sombra = sombra,
                texto = texto, icono = rtIcono.gameObject
            });
        }

        // 4. ayuda de abajo a la derecha
        ArmarAyuda(panel, fuente, rueda, stickR);

        // 5. aviso de como pausar, en la esquina de abajo a la derecha (fuera del Panel: se ve jugando)
        // versiones nitidas (3x, bordes afilados) de Tecla_ESC y Boton_options, hechas a partir de las
        // originales, que no se tocan. Sin compresion: los iconos chiquitos se ensuciaban al comprimirlos.
        var esc = SpriteNitido(Carpeta + "Tecla_ESC_hd.png");
        var options = SpriteNitido(Carpeta + "Boton_options_hd.png");
        CanvasGroup aviso = null;
        if (esc != null && options != null) aviso = ArmarAviso((RectTransform)raiz.transform, fuente, esc, options);

        // conectar el script
        var so = new SerializedObject(raiz.GetComponent<MenuPausa>());
        so.FindProperty("panel").objectReferenceValue = panel.gameObject;
        so.FindProperty("aviso").objectReferenceValue = aviso;
        var sonido = AssetDatabase.LoadAssetAtPath<AudioClip>(RutaSonidoCambio);
        if (sonido == null) Debug.LogWarning("No encontre el sonido " + RutaSonidoCambio + ": el menu va a andar sin sonido al cambiar de boton.");
        so.FindProperty("sonidoCambio").objectReferenceValue = sonido;
        var lista = so.FindProperty("botones");
        lista.arraySize = botones.Count;
        for (int i = 0; i < botones.Count; i++)
        {
            var e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("accion").enumValueIndex = (int)botones[i].accion;
            e.FindPropertyRelative("fondo").objectReferenceValue = botones[i].fondo;
            e.FindPropertyRelative("hover").objectReferenceValue = botones[i].hover;
            e.FindPropertyRelative("sombra").objectReferenceValue = botones[i].sombra;
            e.FindPropertyRelative("texto").objectReferenceValue = botones[i].texto;
            e.FindPropertyRelative("icono").objectReferenceValue = botones[i].icono;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // queda escondido hasta que se aprieta Esc / Options
        panel.gameObject.SetActive(false);

        // se guarda como prefab en _UI/Pausa y en la escena queda una copia conectada
        var instancia = PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, RutaPrefab, InteractionMode.AutomatedAction);

        EditorSceneManager.MarkSceneDirty(instancia.scene);
        Selection.activeGameObject = instancia;
        Debug.Log("Menu de pausa armado y guardado en " + RutaPrefab + ". En Play se abre con Esc o con Options del joystick. " +
                  "Guarda la escena con Ctrl+S.");
    }

    // DESPLAZARSE  [rueda]  o  [stick R], centrado en CentroAyuda; los espacios se miden con lo que
    // se ve de cada icono (los PNG tienen aire alrededor)
    private static void ArmarAyuda(RectTransform panel, TMP_FontAsset fuente, Sprite rueda, Sprite stickR)
    {
        var ayuda = Hijo("Ayuda desplazarse", panel, Vector2.zero, new Vector2(10f, 40f));
        ayuda.pivot = new Vector2(1f, 0.5f);

        // alto visible de cada icono -> escala del rect
        float escRueda = AltoIconosAyuda / 45f;   // en Desplazarse_mouse se ven 19 x 45 px de 55 x 55
        float escStick = AltoIconosAyuda / 32f;   // en Desplazarse_joystick se ven 28 x 32 px de 38 x 38
        float anchoRueda = 19f * escRueda, anchoStick = 28f * escStick;

        var tituloAyuda = Texto("Texto", ayuda, "DESPLAZARSE", TamanoAyuda, Vector2.zero, new Vector2(240f, 40f), fuente,
                                TextAlignmentOptions.Right, pivot: new Vector2(1f, 0.5f));
        float anchoTexto = tituloAyuda.GetPreferredValues("DESPLAZARSE").x;
        var o = Texto("o", ayuda, "o", 15f, Vector2.zero, new Vector2(20f, 24f), fuente, TextAlignmentOptions.Center);
        float anchoO = o.GetPreferredValues("o").x;

        // de derecha a izquierda: stick, o, rueda, texto (todo relativo al borde derecho del grupo)
        const float EntreIconos = 12f, EntreTextoEIcono = 18f;
        float x = 0f;
        float cStick = x - anchoStick / 2f;            x -= anchoStick + EntreIconos;
        float cO = x - anchoO / 2f;                    x -= anchoO + EntreIconos;
        float cRueda = x - anchoRueda / 2f;            x -= anchoRueda + EntreTextoEIcono;
        float finTexto = x;

        // el grupo entero (texto + iconos) queda centrado en CentroAyuda: su borde derecho va medio ancho a la derecha
        float anchoGrupo = anchoTexto - finTexto;
        ayuda.anchoredPosition = new Vector2(CentroAyuda.x - 960f + anchoGrupo / 2f, 540f - CentroAyuda.y);

        // los hijos se ubican desde el centro del grupo (ancho 10, pivot a la derecha): el borde derecho esta en +5
        const float Borde = 5f;
        Imagen("Rueda del mouse", ayuda, rueda, new Vector2(Borde + cRueda, 0f), rueda.rect.size * escRueda);
        Imagen("Stick R", ayuda, stickR, new Vector2(Borde + cStick, 0f), stickR.rect.size * escStick);
        o.rectTransform.anchoredPosition = new Vector2(Borde + cO, -2f);
        tituloAyuda.rectTransform.anchoredPosition = new Vector2(Borde + finTexto, 0f);
        Debug.Log("Ayuda DESPLAZARSE: texto de " + anchoTexto.ToString("0") + " px, iconos de " + AltoIconosAyuda + " px de alto.");
    }

    // ---------- fuente bold ----------

    private static TMP_FontAsset FuenteBold()
    {
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaFuenteBold);
        if (existente != null) return existente;
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(RutaTtfBold);
        if (ttf == null) { Debug.LogError("No encontre " + RutaTtfBold); return null; }
        var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 12, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Debug.LogError("No pude crear la fuente de TextMeshPro a partir de " + RutaTtfBold); return null; }
        fa.name = "Alegreya Bold SDF";
        AssetDatabase.CreateAsset(fa, RutaFuenteBold);
        fa.atlasTextures[0].name = "Alegreya Bold SDF Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = "Alegreya Bold SDF Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters("AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZzÁáÉéÍíÓóÚúÑñ0123456789 .,:;!¡?¿-");
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ---------- fondo desenfocado ----------

    private static Material MaterialFondo()
    {
        // el vidrio lee la copia de la escena que hace el Renderer 2D: que este prendida
        var renderer = AssetDatabase.LoadMainAssetAtPath(RutaRenderer);
        if (renderer != null)
        {
            var so = new SerializedObject(renderer);
            var usar = so.FindProperty("m_UseCameraSortingLayersTexture");
            if (usar != null && !usar.boolValue)
            {
                usar.boolValue = true;
                so.FindProperty("m_CameraSortingLayersTextureBound").intValue = SortingLayer.NameToID("Efectos");
                so.FindProperty("m_CameraSortingLayerDownsamplingMethod").intValue = 3;
                so.ApplyModifiedProperties();
            }
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMatFondo);
        if (mat != null) return mat;   // ya existe: se respetan los ajustes que tenga
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(RutaShaderVidrio);
        if (shader == null) { Debug.LogWarning("No encontre el shader del vidrio en " + RutaShaderVidrio + ": el fondo no se va a desenfocar."); return null; }
        mat = new Material(shader);
        mat.SetFloat("_Desenfoque", 32f);
        mat.SetColor("_ColorVidrio", new Color(0.04f, 0.08f, 0.06f, 0.5f));
        mat.SetFloat("_Brillo", 0f);
        mat.SetFloat("_Grano", 0.5f);
        mat.SetFloat("_UmbralForma", 0.2f);
        AssetDatabase.CreateAsset(mat, RutaMatFondo);
        return mat;
    }

    // ---------- aviso de como pausar ----------

    // margenes seguros: el aviso "encastra" en la esquina que forman el borde derecho del HUD (69 px del borde
    // de la pantalla) y la linea de abajo (71 px), en px de la pantalla de 1920 x 1080
    private const float MargenSeguroX = 69f, MargenSeguroY = 71f;

    // contenedor propio (contenedor_aviso_pausa.png, 1000 x 240), inspirado en el HUD y en la pausa:
    // esquinas redondeadas, contorno verde oscuro con filete oliva, y un segundo contorno POR FUERA (por
    // detras, separado por un hueco) como el doble reborde de los contenedores del HUD, con pinchitos
    // arriba y abajo como los de las vidas. Sin rombos.
    // Sus extremos (que no se estiran) miden 170 px; el cuerpo recto empieza a ~92 px de cada borde.
    private const string RutaContenedorAviso = Carpeta + "contenedor_aviso_pausa.png";
    private const float AltoImagenContenedor = 240f, ExtremosContenedor = 170f, InicioCuerpo = 92f;

    // PAUSA  [Esc]  o  [Options], abajo a la derecha, dentro de los margenes seguros.
    //  - el contenedor propio de arriba. Se estira solo por el medio (9 cortes), asi las esquinas, los
    //    pinchitos y los rombos no se deforman.
    //  - "Pausa" en versalitas a 32 px como los carteles, sin el ajuste de pares de letras (kerning) de la
    //    fuente, que separaba de mas la U de la S
    //  - la "o" a 20 px (antes 15: quedaba muy chica al lado de los iconos de 38 px)
    //  - espacios parejos: lo mismo entre la palabra y el primer icono que entre cada pieza, medido con lo
    //    que se ve de cada icono
    private static CanvasGroup ArmarAviso(RectTransform raiz, TMP_FontAsset fuente, Sprite esc, Sprite options)
    {
        const float AltoIcono = 38f;                         // alto visible de los iconos
        const float EntreTituloEIconos = 22f, EntreIconos = 14f;
        const float AltoContenedor = 104f;   // antes 84: mas aire arriba y abajo del contenido
        const float AireExtra = 20f;         // aire de mas a cada costado, despues de la curva de la esquina
        float escalaContenedor = AltoContenedor / AltoImagenContenedor;
        float Puntas = InicioCuerpo * escalaContenedor + AireExtra;

        var contenedor = SpriteNitido(RutaContenedorAviso);   // sin compresion: el reborde claro es finito
        if (contenedor != null)
        {
            var impC = (TextureImporter)AssetImporter.GetAtPath(RutaContenedorAviso);
            var borde = new Vector4(ExtremosContenedor, 0f, ExtremosContenedor, 0f);
            if (impC.spriteBorder != borde) { impC.spriteBorder = borde; impC.SaveAndReimport(); }
            contenedor = AssetDatabase.LoadAssetAtPath<Sprite>(RutaContenedorAviso);
        }
        var rt = Hijo("Aviso pausa", raiz, Vector2.zero, new Vector2(10f, AltoContenedor));
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        var grupo = rt.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        // medir: de izquierda a derecha
        var titulo = Texto("Titulo", rt, "Pausa", 32f, Vector2.zero, new Vector2(200f, 44f), fuente,
                           TextAlignmentOptions.Left, pivot: new Vector2(0f, 0.5f));
        titulo.fontStyle = FontStyles.SmallCaps;
        titulo.enableKerning = false;   // sin kerning: espacio parejo entre letras
        float anchoTitulo = titulo.GetPreferredValues("Pausa").x;
        var o = Texto("o", rt, "o", 20f, Vector2.zero, new Vector2(24f, 30f), fuente, TextAlignmentOptions.Center);
        float anchoO = o.GetPreferredValues("o").x;
        // versiones 3x: en Tecla_ESC_hd se ven 78 px de 108; en Boton_options_hd, 72 px de 108
        Vector2 tamEsc = esc.rect.size * (AltoIcono / 78f);
        Vector2 tamOptions = options.rect.size * (AltoIcono / 72f);
        float anchoEsc = AltoIcono, anchoOptions = AltoIcono;   // para los espacios, lo que se ve de cada uno

        float x = 0f;
        float xTitulo = x;                              x += anchoTitulo + EntreTituloEIconos;
        float cEsc = x + anchoEsc / 2f;                 x += anchoEsc + EntreIconos;
        float cO = x + anchoO / 2f;                     x += anchoO + EntreIconos;
        float cOptions = x + anchoOptions / 2f;         x += anchoOptions;
        float anchoContenido = x;
        float anchoContenedor = anchoContenido + 2f * Puntas;
        rt.sizeDelta = new Vector2(anchoContenedor, AltoContenedor);
        rt.anchoredPosition = new Vector2(-MargenSeguroX, MargenSeguroY);

        // contenedor: la barra del HUD, estirada solo por el medio
        if (contenedor != null)
        {
            var fondo = Imagen("Contenedor", rt, contenedor, Vector2.zero, new Vector2(anchoContenedor, AltoContenedor));
            fondo.type = Image.Type.Sliced;
            fondo.pixelsPerUnitMultiplier = 1f / escalaContenedor;   // los extremos se achican en proporcion al alto
            fondo.rectTransform.SetAsFirstSibling();
        }

        // contenido centrado en el contenedor
        float izq = -anchoContenido / 2f;
        titulo.rectTransform.anchoredPosition = new Vector2(izq + xTitulo, 0f);
        Imagen("Esc", rt, esc, new Vector2(izq + cEsc, 0f), tamEsc);
        o.rectTransform.anchoredPosition = new Vector2(izq + cO, -1f);
        Imagen("Options", rt, options, new Vector2(izq + cOptions, 0f), tamOptions);
        return grupo;
    }

    // ---------- sombra de los botones ----------

    // la forma del boton (Boton_normal), engordada y desenfocada: una sombra suave que sobresale ~24 px
    private static Sprite SombraDeBoton(string rutaBoton)
    {
        const int Margen = 24;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(rutaBoton));
        int w = tex.width, h = tex.height, W = w + 2 * Margen, H = h + 2 * Margen;
        var a = new float[W * H];
        var px = tex.GetPixels32();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                a[(y + Margen) * W + x + Margen] = px[y * w + x].a / 255f;
        Object.DestroyImmediate(tex);

        a = Engordar(a, W, H, 6);
        for (int pasada = 0; pasada < 3; pasada++) { a = Desenfocar(a, W, H, 5, true); a = Desenfocar(a, W, H, 5, false); }

        var salida = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var cols = new Color32[W * H];
        for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a[i]) * 255f));
        salida.SetPixels32(cols);
        salida.Apply();
        File.WriteAllBytes(RutaSombraBoton, salida.EncodeToPNG());
        Object.DestroyImmediate(salida);

        AssetDatabase.ImportAsset(RutaSombraBoton, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaSombraBoton);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaSombraBoton);
    }

    private static float[] Engordar(float[] a, int W, int H, int r)
    {
        var b = new float[a.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float m = 0f;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy > r * r) continue;
                        int xx = x + dx, yy = y + dy;
                        if (xx >= 0 && xx < W && yy >= 0 && yy < H && a[yy * W + xx] > m) m = a[yy * W + xx];
                    }
                b[y * W + x] = m;
            }
        return b;
    }

    private static float[] Desenfocar(float[] a, int W, int H, int r, bool horizontal)
    {
        var b = new float[a.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float s = 0f;
                for (int k = -r; k <= r; k++)
                {
                    int xx = horizontal ? x + k : x, yy = horizontal ? y : y + k;
                    if (xx >= 0 && xx < W && yy >= 0 && yy < H) s += a[yy * W + xx];
                }
                b[y * W + x] = s / (2 * r + 1);
            }
        return b;
    }

    // ---------- piezas ----------

    // posicion desde el centro de la pantalla, a partir de "px desde arriba" del Figma (1080 de alto)
    private static Vector2 DesdeArriba(float y) => new Vector2(0f, 540f - y);

    private static RectTransform Hijo(string nombre, RectTransform padre, Vector2 posicion, Vector2 tamano)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
        return rt;
    }

    private static RectTransform Estirado(string nombre, RectTransform padre)
    {
        var rt = Hijo(nombre, padre, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image Imagen(string nombre, RectTransform padre, Sprite sprite, Vector2 posicion, Vector2 tamano)
    {
        var rt = Hijo(nombre, padre, posicion, tamano);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI Texto(string nombre, RectTransform padre, string texto, float tamano, Vector2 posicion,
                                         Vector2 caja, TMP_FontAsset fuente, TextAlignmentOptions alineacion, Vector2? pivot = null)
    {
        var rt = Hijo(nombre, padre, posicion, caja);
        if (pivot != null) rt.pivot = pivot.Value;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = fuente;
        t.fontSharedMaterial = fuente.material;
        t.text = texto;
        t.fontSize = tamano;
        t.characterSpacing = 0f;
        t.color = Color.white;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    // ---------- sprites ----------

    private static Sprite UnSoloSprite(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("No encontre " + ruta); return null; }
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // una sola imagen, sin compresion y sin mipmaps: para iconos que tienen que verse nitidos
    private static Sprite SpriteNitido(string ruta)
    {
        var imp = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (imp == null) { Debug.LogError("No encontre " + ruta); return null; }
        if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single ||
            imp.textureCompression != TextureImporterCompression.Uncompressed || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Sprite PrimerSprite(string ruta)
    {
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(ruta))
            if (o is Sprite s) return s;
        Debug.LogError("No encontre el icono " + ruta);
        return null;
    }
}
