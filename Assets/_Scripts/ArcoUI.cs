using UnityEngine;
using UnityEngine.UI;

// Un pedazo de anillo para el HUD (UI, en el Canvas): el relleno amarillo de la barra curva del templo.
// Es la traduccion del "Ellipse 33" del Figma (Barra 4): una elipse con arco y radio interno.
// Se llena desde 'anguloInicio' hacia 'anguloFin' segun 'relleno' (0 a 1). Angulos en grados, 0 = derecha,
// y crecen en sentido antihorario.
public class ArcoUI : MaskableGraphic
{
    [SerializeField] private Vector2 centro;
    [SerializeField] private float radioInterno = 186f;
    [SerializeField] private float radioExterno = 199f;
    [SerializeField] private float anguloInicio = 213.6f;
    [SerializeField] private float anguloFin = 283.7f;
    [Range(0f, 1f)] [SerializeField] private float relleno = 0.5f;
    [SerializeField] private int segmentos = 48;

    public float Relleno
    {
        get => relleno;
        set { value = Mathf.Clamp01(value); if (Mathf.Approximately(value, relleno)) return; relleno = value; SetVerticesDirty(); }
    }

    public void Configurar(Vector2 c, float rIn, float rOut, float a0, float a1)
    {
        centro = c; radioInterno = rIn; radioExterno = rOut; anguloInicio = a0; anguloFin = a1;
        SetVerticesDirty();
    }

    // punto de la mitad del anillo donde termina el relleno (ahi llega el polen)
    public Vector2 PuntaLocal(float r01)
    {
        float a = Mathf.Lerp(anguloInicio, anguloFin, Mathf.Clamp01(r01)) * Mathf.Deg2Rad;
        float r = (radioInterno + radioExterno) * 0.5f;
        return centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (relleno <= 0.0001f) return;
        int n = Mathf.Max(2, Mathf.CeilToInt(segmentos * relleno));
        float fin = Mathf.Lerp(anguloInicio, anguloFin, relleno);
        UIVertex v = UIVertex.simpleVert;
        v.color = color;
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.Lerp(anguloInicio, fin, i / (float)n) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            v.position = centro + dir * radioInterno; vh.AddVert(v);
            v.position = centro + dir * radioExterno; vh.AddVert(v);
            if (i > 0)
            {
                int b = i * 2;
                vh.AddTriangle(b - 2, b - 1, b + 1);
                vh.AddTriangle(b - 2, b + 1, b);
            }
        }
    }
}
