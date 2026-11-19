using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Menu:  Pomelo -> Sonidos -> Mejorar splash (alta calidad)
// El splash que eligio Caro (Assets/_Sonidos/Agua_splash.mp3, de freesound) viene en baja resolucion (MP3 MPEG-2 a
// 24 kHz: no tiene agudos arriba de 12 kHz y suena opaco). Este menu lo pasa a la calidad de los demas sonidos:
//  1) lo remuestrea a 44.1 kHz con interpolacion de alta calidad (Lanczos), con el tono ya bajado ('Tono', mas grave);
//  2) le recupera el brillo: genera suave los agudos que se perdieron a partir de los que tiene (exciter);
//  3) lo guarda sin compresion (Assets/_Sonidos/Agua_splash_HQ.wav, PCM) y lo conecta en el SonidoAgua de la escena
//     abierta (con el tono en 1, porque ya viene incluido). El eco (sala de piedra) lo sigue poniendo SonidoAgua.
public static class MejorarSplash
{
    private const string Origen = "Assets/_Sonidos/Agua_splash.mp3";
    public const string Destino = "Assets/_Sonidos/Agua_splash_HQ.wav";
    private const float Tono = 0.74f;          // mas grave (Caro: 0.82 y despues un poco mas)
    private const int Frecuencia = 44100;
    private const float Brillo = 0.18f;        // cuanto del agudo recuperado se suma
    private const float DuracionSplash = 1.8f; // segundos que se toman de la grabacion (si es mas larga)

    [MenuItem("Pomelo/Sonidos/Mejorar splash (alta calidad)")]
    public static void Mejorar()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Splash] Para el Play antes."); return; }
        if (!(AssetImporter.GetAtPath(Origen) is AudioImporter imp)) { Debug.LogError("[Splash] No encontre " + Origen); return; }

        // para leer las muestras tiene que estar descomprimido al cargar
        var ajustes = imp.defaultSampleSettings;
        if (ajustes.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            imp.defaultSampleSettings = ajustes;
            imp.SaveAndReimport();
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Origen);
        if (clip == null || !clip.LoadAudioData()) { Debug.LogError("[Splash] No pude leer el sonido."); return; }
        int canales = clip.channels, entrada = clip.samples;
        var datos = new float[entrada * canales];
        if (!clip.GetData(datos, 0)) { Debug.LogError("[Splash] No pude leer las muestras."); return; }

        // 0) (Caro: "sacalo de aca") si la grabacion es larga (agua de pileta), se busca el splash: el golpe mas fuerte
        //    (ventanas de 20 ms), desde un poquito antes de que empiece, 'DuracionSplash' segundos, con entrada y salida
        //    suaves
        float largoSeg = (float)entrada / clip.frequency;
        string recorte = "";
        if (largoSeg > DuracionSplash + 0.3f)
        {
            int ventana = Math.Max(1, clip.frequency / 50);
            int nv = entrada / ventana;
            var energia = new double[nv];
            for (int v = 0; v < nv; v++)
            {
                double s = 0;
                for (int i = v * ventana; i < (v + 1) * ventana; i++)
                    for (int c = 0; c < canales; c++) { double x = datos[i * canales + c]; s += x * x; }
                energia[v] = Math.Sqrt(s / (ventana * canales));
            }
            // el golpe: la ventana que mas sube respecto del fondo de antes (no solo la mas fuerte)
            int mejor = 0; double mejorSalto = -1;
            for (int v = 5; v < nv; v++)
            {
                double antes = 0; for (int k = v - 5; k < v; k++) antes += energia[k]; antes /= 5;
                double salto = energia[v] - antes * 1.2;
                if (salto > mejorSalto) { mejorSalto = salto; mejor = v; }
            }
            // donde empieza: hacia atras, hasta que baja al fondo
            int inicioV = mejor;
            double umbral = energia[mejor] * 0.25;
            while (inicioV > 0 && energia[inicioV - 1] > umbral) inicioV--;
            int desde = Math.Max(0, inicioV * ventana - clip.frequency * 3 / 100);   // 30 ms antes
            int largo = Math.Min(entrada - desde, (int)(DuracionSplash * clip.frequency));
            var corte = new float[largo * canales];
            int entradaSuave = clip.frequency * 4 / 1000, salidaSuave = (int)(0.35f * clip.frequency);
            for (int i = 0; i < largo; i++)
            {
                float g = 1f;
                if (i < entradaSuave) g = (float)i / entradaSuave;
                if (i > largo - salidaSuave) g *= Mathf.SmoothStep(0f, 1f, (float)(largo - i) / salidaSuave);
                for (int c = 0; c < canales; c++) corte[i * canales + c] = datos[(desde + i) * canales + c] * g;
            }
            datos = corte; entrada = largo;
            recorte = $" Recortado de una grabacion de {largoSeg:0.0} s: el splash en {(float)desde / clip.frequency:0.00} s.";
        }

        // 1) remuestreo Lanczos (a = 6) a 44.1 kHz, con el tono bajado: cada muestra nueva avanza 'paso' en el original
        double paso = (double)clip.frequency / Frecuencia * Tono;
        int salida = (int)Math.Floor((entrada - 1) / paso);
        var canalesSalida = new double[canales][];
        for (int c = 0; c < canales; c++)
        {
            var o = new double[salida];
            for (int k = 0; k < salida; k++)
            {
                double pos = k * paso; int centro = (int)Math.Floor(pos);
                double suma = 0, pesos = 0;
                for (int j = centro - 5; j <= centro + 6; j++)
                {
                    if (j < 0 || j >= entrada) continue;
                    double w = Lanczos(pos - j, 6);
                    suma += datos[j * canales + c] * w; pesos += w;
                }
                o[k] = pesos != 0 ? suma / pesos : 0;
            }
            canalesSalida[c] = o;
        }

        // 2) exciter: los agudos que tiene (2.5 - 10 kHz) saturados suave generan armonicos arriba, que se suman
        double maximo = 1e-9;
        for (int c = 0; c < canales; c++)
        {
            var x = canalesSalida[c];
            var hp1 = Filtro.PasaAltos(2500, Frecuencia); var lp1 = Filtro.PasaBajos(10000, Frecuencia);
            var hp2 = Filtro.PasaAltos(7000, Frecuencia); var lp2 = Filtro.PasaBajos(17000, Frecuencia);
            for (int i = 0; i < x.Length; i++)
            {
                double medio = lp1.P(hp1.P(x[i]));
                double armonicos = lp2.P(hp2.P(Math.Tanh(medio * 4.0)));
                x[i] += armonicos * Brillo;
                maximo = Math.Max(maximo, Math.Abs(x[i]));
            }
        }
        // ecualizacion (Caro: sonaba "metido en un tubo"): baja los medios nasales/de caja (300 y 900 Hz) y levanta un
        // poco los agudos
        maximo = 1e-9;
        for (int c = 0; c < canales; c++)
        {
            var x = canalesSalida[c];
            var caja = Filtro.Campana(320, 1.0, -5.0, Frecuencia);
            var nasal = Filtro.Campana(950, 1.4, -3.5, Frecuencia);
            var aire = Filtro.EstanteAlto(5000, 4.0, Frecuencia);
            for (int i = 0; i < x.Length; i++)
            {
                x[i] = aire.P(nasal.P(caja.P(x[i])));
                maximo = Math.Max(maximo, Math.Abs(x[i]));
            }
        }
        // normalizado a -1 dB
        double ganancia = 0.89 / maximo;

        // 3) WAV PCM 16 bits, 44.1 kHz
        string ruta = Path.Combine(Directory.GetCurrentDirectory(), Destino);
        using (var bw = new BinaryWriter(File.Create(ruta)))
        {
            int bytes = salida * canales * 2;
            bw.Write(new[] { 'R', 'I', 'F', 'F' }); bw.Write(36 + bytes); bw.Write(new[] { 'W', 'A', 'V', 'E' });
            bw.Write(new[] { 'f', 'm', 't', ' ' }); bw.Write(16); bw.Write((short)1); bw.Write((short)canales);
            bw.Write(Frecuencia); bw.Write(Frecuencia * canales * 2); bw.Write((short)(canales * 2)); bw.Write((short)16);
            bw.Write(new[] { 'd', 'a', 't', 'a' }); bw.Write(bytes);
            for (int i = 0; i < salida; i++)
                for (int c = 0; c < canales; c++)
                    bw.Write((short)Math.Round(Math.Max(-1, Math.Min(1, canalesSalida[c][i] * ganancia)) * 32767));
        }
        AssetDatabase.ImportAsset(Destino);
        if (AssetImporter.GetAtPath(Destino) is AudioImporter hq)
        {
            var a = hq.defaultSampleSettings;
            a.compressionFormat = AudioCompressionFormat.PCM;      // sin compresion: la misma calidad que el archivo
            a.loadType = AudioClipLoadType.DecompressOnLoad;
            a.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            hq.defaultSampleSettings = a;
            hq.forceToMono = false;
            hq.SaveAndReimport();
        }
        var nuevo = AssetDatabase.LoadAssetAtPath<AudioClip>(Destino);

        // conectarlo en la escena abierta (el tono ya viene bajado: queda en 1)
        int conectados = 0;
        foreach (var s in Object.FindObjectsByType<SonidoAgua>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(s);
            so.FindProperty("splash").objectReferenceValue = nuevo;
            so.FindProperty("tono").floatValue = 1f;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(s.gameObject.scene);
            conectados++;
        }
        Debug.Log($"[Splash] Listo: {Destino} ({canales} canal/es, {Frecuencia} Hz, {salida / (float)Frecuencia:0.00} s), " +
                  $"desde {clip.frequency} Hz.{recorte} Conectado en {conectados} SonidoAgua de la escena abierta: guarda la escena.");
    }

    private static double Lanczos(double x, int a)
    {
        if (Math.Abs(x) < 1e-9) return 1;
        if (Math.Abs(x) >= a) return 0;
        double px = Math.PI * x;
        return a * Math.Sin(px) * Math.Sin(px / a) / (px * px);
    }

    private class Filtro
    {
        double b0, b1, b2, a1, a2, x1, x2, y1, y2;
        public static Filtro PasaAltos(double f, double sr)
        {
            var b = new Filtro(); double w = 2 * Math.PI * f / sr, al = Math.Sin(w) / (2 * 0.707), c = Math.Cos(w), a0 = 1 + al;
            b.b0 = (1 + c) / 2 / a0; b.b1 = -(1 + c) / a0; b.b2 = (1 + c) / 2 / a0; b.a1 = -2 * c / a0; b.a2 = (1 - al) / a0; return b;
        }
        public static Filtro PasaBajos(double f, double sr)
        {
            var b = new Filtro(); double w = 2 * Math.PI * f / sr, al = Math.Sin(w) / (2 * 0.707), c = Math.Cos(w), a0 = 1 + al;
            b.b0 = (1 - c) / 2 / a0; b.b1 = (1 - c) / a0; b.b2 = (1 - c) / 2 / a0; b.a1 = -2 * c / a0; b.a2 = (1 - al) / a0; return b;
        }
        // campana (RBJ): sube o baja 'db' alrededor de 'f'
        public static Filtro Campana(double f, double q, double db, double sr)
        {
            var b = new Filtro(); double A = Math.Pow(10, db / 40), w = 2 * Math.PI * f / sr, al = Math.Sin(w) / (2 * q), c = Math.Cos(w);
            double a0 = 1 + al / A;
            b.b0 = (1 + al * A) / a0; b.b1 = -2 * c / a0; b.b2 = (1 - al * A) / a0; b.a1 = -2 * c / a0; b.a2 = (1 - al / A) / a0; return b;
        }
        // estante alto (RBJ): sube 'db' todo lo que esta arriba de 'f'
        public static Filtro EstanteAlto(double f, double db, double sr)
        {
            var b = new Filtro(); double A = Math.Pow(10, db / 40), w = 2 * Math.PI * f / sr, c = Math.Cos(w), s = Math.Sin(w);
            double al = s / 2 * Math.Sqrt(2), raiz = 2 * Math.Sqrt(A) * al;
            double a0 = (A + 1) - (A - 1) * c + raiz;
            b.b0 = A * ((A + 1) + (A - 1) * c + raiz) / a0; b.b1 = -2 * A * ((A - 1) + (A + 1) * c) / a0; b.b2 = A * ((A + 1) + (A - 1) * c - raiz) / a0;
            b.a1 = 2 * ((A - 1) - (A + 1) * c) / a0; b.a2 = ((A + 1) - (A - 1) * c - raiz) / a0; return b;
        }

        public double P(double x)
        {
            double y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = x; y2 = y1; y1 = y; return y;
        }
    }
}
