using System.Collections;
using UnityEngine;

// La reproduce MuerteUmbrae en un objeto aparte (Umbrae ya se desactivo). Une las dos animaciones:
//  1) "umbrae-muerte": Umbrae se deshace; en el frame 0007 se ve algo verde en su cabeza.
//  2) Eso verde sigue cayendo derecho hasta donde esta la gema en "umb-muerte-cae-gema"
//     (mismo lienzo, asi que coinciden las ubicaciones) y ahi se transforma en la gema.
//  3) Queda la gema suelta (GemaUmbrae), que se puede agarrar.
public class SecuenciaMuerteUmbrae : MonoBehaviour
{
    public struct Datos
    {
        public Sprite[] framesMuerte;
        public float fps;
        public int indiceFrameGema;
        public Vector2 gemaEnMuerte;
        public Sprite[] framesCaida;
        public Vector2[] gemaEnCaida;
        public float duracionCaida;
        public float fpsTransformacion;
        public Sprite gema;
        public Vector2 gemaFinal;
        public Sprite destello;
        public Material materialBrillo;
        public Vector3 punta;
        public float lado;
        public Material material;
        public int capa;
        public int orden;
    }

    private Datos d;
    private bool gemaLista;

    public void Iniciar(Datos datos)
    {
        d = datos;
        StartCoroutine(Reproducir());
    }

    private SpriteRenderer CrearCapa(string nombre, int ordenExtra)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.position = d.punta;
        var sr = go.AddComponent<SpriteRenderer>();
        if (d.material != null) sr.sharedMaterial = d.material;
        sr.sortingLayerID = d.capa;
        sr.sortingOrder = d.orden + ordenExtra;
        sr.flipX = d.lado < 0f;
        return sr;
    }

    private IEnumerator Reproducir()
    {
        var capaMuerte = CrearCapa("Umbrae", 0);
        float paso = 1f / Mathf.Max(1f, d.fps);

        for (int i = 0; i < d.framesMuerte.Length; i++)
        {
            capaMuerte.sprite = d.framesMuerte[i];
            yield return new WaitForSeconds(paso);
            // despues del frame con lo verde, eso verde sigue en su propia capa
            if (i == d.indiceFrameGema) StartCoroutine(Gema());
        }
        capaMuerte.enabled = false;

        while (!gemaLista) yield return null;
        Destroy(gameObject);
    }

    private IEnumerator Gema()
    {
        int n = d.framesCaida != null ? d.framesCaida.Length : 0;
        if (n > 0)
        {
            var capa = CrearCapa("Gema", 1);
            capa.sprite = d.framesCaida[0];

            // cae derecho: arranca con la gema donde estaba lo verde del frame 0007 y baja
            // (acelerando) hasta su lugar en el dibujo de la caida
            Vector2 desfase = d.gemaEnMuerte - (d.gemaEnCaida != null && d.gemaEnCaida.Length > 0 ? d.gemaEnCaida[0] : Vector2.zero);
            for (float t = 0f; t < d.duracionCaida; t += Time.deltaTime)
            {
                float k = t / Mathf.Max(0.01f, d.duracionCaida);
                Vector2 off = desfase * (1f - k * k);
                capa.transform.position = d.punta + new Vector3(d.lado * off.x, off.y, 0f);
                yield return null;
            }
            capa.transform.position = d.punta;

            // se transforma en la gema, en su lugar
            float paso = 1f / Mathf.Max(1f, d.fpsTransformacion);
            for (int i = 0; i < n; i++)
            {
                capa.sprite = d.framesCaida[i];
                yield return new WaitForSeconds(paso);
            }
            Destroy(capa.gameObject);
        }

        // la gema suelta queda en el mismo lugar que la del ultimo frame (no es hija de esta secuencia)
        if (d.gema != null)
        {
            var go = new GameObject("Gema verde");
            go.transform.position = d.punta + new Vector3(d.lado * d.gemaFinal.x, d.gemaFinal.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = d.gema;
            if (d.material != null) sr.sharedMaterial = d.material;
            sr.sortingLayerID = d.capa;
            sr.sortingOrder = d.orden + 2;
            go.AddComponent<GemaUmbrae>().Iniciar(d.destello, d.materialBrillo);
        }
        gemaLista = true;
    }
}
