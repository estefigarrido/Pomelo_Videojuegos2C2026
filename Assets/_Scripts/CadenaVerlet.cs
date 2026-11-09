using UnityEngine;

// Fisica de soga (Verlet) que usan SogaBotellita y ColganteFlexible: una cadena de puntos colgada del
// primero (el ancla), con gravedad, un poco de rigidez para que vuelva a su forma y empujones de la chica.
// No es un MonoBehaviour: cada componente tiene la suya y la avanza en su Update.
public class CadenaVerlet
{
    public readonly Vector2[] p;
    private readonly Vector2[] previo;
    private readonly float[] largo;
    private readonly Vector2[] reposo;   // direccion de cada tramo en reposo
    private readonly float[] invMasa;
    public Vector2 ancla;
    public bool extremoSuelto;   // la botellita se rompio: la punta queda libre

    public float rigidez;        // 0 = soga, mas = vuelve a su forma (las plantas)
    public float amortiguacion = 0.985f;

    public int Cantidad => p.Length;
    public Vector2 Punta => p[p.Length - 1];

    public CadenaVerlet(Vector2[] puntos, float rigidez, float masaPunta = 1f)
    {
        int n = puntos.Length;
        p = (Vector2[])puntos.Clone();
        previo = (Vector2[])puntos.Clone();
        largo = new float[n];
        reposo = new Vector2[n];
        invMasa = new float[n];
        ancla = puntos[0];
        this.rigidez = rigidez;
        for (int i = 1; i < n; i++)
        {
            Vector2 d = puntos[i] - puntos[i - 1];
            largo[i] = d.magnitude;
            reposo[i] = largo[i] > 0.0001f ? d / largo[i] : Vector2.down;
            invMasa[i] = 1f;
        }
        invMasa[n - 1] = 1f / Mathf.Max(0.1f, masaPunta);
    }

    public void SoltarPunta() { extremoSuelto = true; invMasa[p.Length - 1] = 1f; }

    public void Paso(float dt, float gravedad, Vector2 fuerzaExtra)
    {
        // pasos chicos y fijos: estable aunque baje el framerate
        int sub = Mathf.Clamp(Mathf.CeilToInt(dt / 0.0083f), 1, 4);
        float h = Mathf.Min(dt, 0.05f) / sub;
        for (int s = 0; s < sub; s++) Subpaso(h, gravedad, fuerzaExtra);
    }

    private void Subpaso(float h, float gravedad, Vector2 fuerzaExtra)
    {
        int n = p.Length;
        for (int i = 1; i < n; i++)
        {
            Vector2 v = (p[i] - previo[i]) * amortiguacion;
            previo[i] = p[i];
            float k = (float)i / (n - 1);   // el viento mueve mas la punta
            p[i] += v + (Vector2.down * gravedad + fuerzaExtra * k) * h * h;
        }
        p[0] = ancla; previo[0] = ancla;

        for (int it = 0; it < 6; it++)
        {
            p[0] = ancla;
            for (int i = 1; i < n; i++)
            {
                // rigidez: tira cada punto hacia donde estaria en reposo respecto del anterior
                if (rigidez > 0f)
                {
                    Vector2 objetivo = p[i - 1] + reposo[i] * largo[i];
                    p[i] = Vector2.Lerp(p[i], objetivo, rigidez * 0.2f);
                }
                Vector2 d = p[i] - p[i - 1];
                float l = d.magnitude;
                if (l < 0.00001f) continue;
                float error = (l - largo[i]) / l;
                float wa = i - 1 == 0 ? 0f : invMasa[i - 1];
                float wb = invMasa[i];
                float suma = wa + wb;
                if (suma <= 0f) continue;
                p[i - 1] += d * error * (wa / suma);
                p[i] -= d * error * (wb / suma);
            }
        }
    }

    // la chica (o un golpe) empuja los puntos que toca: les suma velocidad en su direccion
    public bool Empujar(Bounds caja, Vector2 velocidad, float dt, float fuerza)
    {
        bool toco = false;
        for (int i = 1; i < p.Length; i++)
        {
            if (!caja.Contains(new Vector3(p[i].x, p[i].y, caja.center.z))) continue;
            p[i] += velocidad * dt * fuerza;
            toco = true;
        }
        return toco;
    }

    // angulo (grados) del tramo que termina en el punto i, medido desde la vertical hacia abajo
    public float Angulo(int i)
    {
        Vector2 d = p[i] - p[i - 1];
        return Mathf.Atan2(d.x, -d.y) * Mathf.Rad2Deg;
    }
}
