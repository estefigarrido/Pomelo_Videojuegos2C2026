using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

// Para que un menu que arma una escena (Pomelo -> Escenas -> Armar escena Inicio_introducción) no pise los ajustes
// que se hicieron a mano en esa escena.
//
// Como funciona (una "mezcla de tres", como hace git):
//   - "base": como dejo el menu la escena la ultima vez (se guarda en un .json al terminar de armar).
//   - "actual": como esta la escena ahora, con los ajustes a mano.
//   - "nuevo": lo que arma el menu hoy (que puede traer cambios del codigo).
// Para cada valor (posicion, escala, color, orden de dibujo, valores de los scripts...): si en "actual" es distinto
// de "base", es un ajuste a mano y se le pone a "nuevo". Si no se toco, queda el de "nuevo".
// Ademas: los objetos que se agregaron a mano se pasan a la escena nueva, los que se borraron a mano se borran, y
// los componentes agregados a mano se copian. Si no hay "base" (la primera vez), todo lo que difiere cuenta como
// ajuste a mano.
public static class ConservarAjustes
{
    // ---------- foto de una escena ----------

    public class Foto
    {
        public readonly Dictionary<string, string> valores = new Dictionary<string, string>();   // objeto|componente|propiedad -> valor
        public readonly HashSet<string> objetos = new HashSet<string>();                        // rutas de los objetos
        public readonly HashSet<string> componentes = new HashSet<string>();                    // objeto|componente
        public readonly Dictionary<string, GameObject> porRuta = new Dictionary<string, GameObject>();
        public readonly Dictionary<Object, string> rutaDe = new Dictionary<Object, string>();
    }

    [System.Serializable] private class Entrada { public string k; public string v; }
    [System.Serializable] private class FotoJson
    {
        public List<string> objetos = new List<string>();
        public List<string> componentes = new List<string>();
        public List<Entrada> valores = new List<Entrada>();
    }

    private static readonly HashSet<string> Ignorar = new HashSet<string>
    {
        "m_Script", "m_GameObject", "m_Children", "m_Father", "m_PrefabInstance", "m_PrefabAsset",
        "m_CorrespondingSourceObject", "m_ObjectHideFlags", "m_EditorHideFlags", "m_EditorClassIdentifier",
    };

    // foto de todos los objetos que cuelgan de estas raices
    public static Foto Tomar(IEnumerable<GameObject> raices)
    {
        var f = new Foto();
        foreach (var r in raices)
            if (r != null) TomarObjeto(f, r.transform);
        return f;
    }

    private static void TomarObjeto(Foto f, Transform t)
    {
        string ruta = Ruta(t);
        f.objetos.Add(ruta);
        f.porRuta[ruta] = t.gameObject;
        f.rutaDe[t.gameObject] = ruta;
        f.valores[ruta + "|GameObject|activo"] = t.gameObject.activeSelf ? "1" : "0";
        foreach (var (c, clave) in Componentes(t.gameObject))
        {
            string rc = ruta + "|" + clave;
            f.componentes.Add(rc);
            f.rutaDe[c] = rc;
            var so = new SerializedObject(c);
            var it = so.GetIterator();
            bool entrar = true;
            while (it.NextVisible(entrar))
            {
                entrar = !Ignorar.Contains(it.name);
                if (!entrar) continue;
                string v = Leer(it);
                if (v != null) f.valores[rc + "|" + it.propertyPath] = v;
            }
        }
        foreach (Transform h in t) TomarObjeto(f, h);
    }

    // los componentes de un objeto, cada uno con una clave estable: Tipo#n (n = cuantos del mismo tipo hay antes)
    private static IEnumerable<(Component, string)> Componentes(GameObject go)
    {
        var cuenta = new Dictionary<string, int>();
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;   // script que falta
            string tipo = c.GetType().FullName;
            cuenta.TryGetValue(tipo, out int n);
            cuenta[tipo] = n + 1;
            yield return (c, tipo + "#" + n);
        }
    }

    // ruta del objeto en la jerarquia; si hay hermanos con el mismo nombre, se les agrega #1, #2...
    public static string Ruta(Transform t)
    {
        var partes = new List<string>();
        for (var x = t; x != null; x = x.parent)
        {
            int igualesAntes = 0;
            if (x.parent != null)
            {
                for (int i = 0; i < x.GetSiblingIndex(); i++)
                    if (x.parent.GetChild(i).name == x.name) igualesAntes++;
            }
            else
            {
                foreach (var r in x.gameObject.scene.GetRootGameObjects())
                {
                    if (r.transform == x) break;
                    if (r.name == x.name) igualesAntes++;
                }
            }
            partes.Add(igualesAntes > 0 ? x.name + "#" + igualesAntes : x.name);
        }
        partes.Reverse();
        return string.Join("/", partes);
    }

    // ---------- leer y escribir valores ----------

    private static string F(float x) => x.ToString("R", CultureInfo.InvariantCulture);

    private static string Leer(SerializedProperty p)
    {
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: return p.longValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character: return p.intValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Boolean: return p.boolValue ? "1" : "0";
            case SerializedPropertyType.Float: return F(p.floatValue);
            case SerializedPropertyType.String: return "s:" + p.stringValue;
            case SerializedPropertyType.Color: { var c = p.colorValue; return $"{F(c.r)},{F(c.g)},{F(c.b)},{F(c.a)}"; }
            case SerializedPropertyType.Vector2: { var v = p.vector2Value; return $"{F(v.x)},{F(v.y)}"; }
            case SerializedPropertyType.Vector3: { var v = p.vector3Value; return $"{F(v.x)},{F(v.y)},{F(v.z)}"; }
            case SerializedPropertyType.Vector4: { var v = p.vector4Value; return $"{F(v.x)},{F(v.y)},{F(v.z)},{F(v.w)}"; }
            case SerializedPropertyType.Quaternion: { var q = p.quaternionValue; return $"{F(q.x)},{F(q.y)},{F(q.z)},{F(q.w)}"; }
            case SerializedPropertyType.Rect: { var r = p.rectValue; return $"{F(r.x)},{F(r.y)},{F(r.width)},{F(r.height)}"; }
            case SerializedPropertyType.ObjectReference: return Referencia(p.objectReferenceValue);
            default: return null;
        }
    }

    // una referencia: los assets por su id global; los objetos de la escena por su ruta
    private static string Referencia(Object o)
    {
        if (o == null) return "r:null";
        if (EditorUtility.IsPersistent(o)) return "r:asset:" + GlobalObjectId.GetGlobalObjectIdSlow(o);
        if (o is GameObject g) return "r:escena:" + Ruta(g.transform) + "|GameObject";
        if (o is Component c)
        {
            var clave = Componentes(c.gameObject).FirstOrDefault(x => x.Item1 == c).Item2;
            return "r:escena:" + Ruta(c.transform) + "|" + clave;
        }
        return "r:otro";
    }

    private static Object Resolver(string v, Scene escena)
    {
        if (v == "r:null") return null;
        if (v.StartsWith("r:asset:"))
            return GlobalObjectId.TryParse(v.Substring(8), out var id) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) : null;
        if (!v.StartsWith("r:escena:")) return null;
        string resto = v.Substring(9);
        int barra = resto.LastIndexOf('|');
        string ruta = resto.Substring(0, barra), clave = resto.Substring(barra + 1);
        var go = BuscarRuta(escena, ruta);
        if (go == null) return null;
        if (clave == "GameObject") return go;
        return Componentes(go).FirstOrDefault(x => x.Item2 == clave).Item1;
    }

    public static GameObject BuscarRuta(Scene escena, string ruta)
    {
        foreach (var r in escena.GetRootGameObjects())
        {
            var g = Buscar(r.transform, ruta);
            if (g != null) return g;
        }
        return null;
    }

    private static GameObject Buscar(Transform t, string ruta)
    {
        string mia = Ruta(t);
        if (mia == ruta) return t.gameObject;
        if (!ruta.StartsWith(mia + "/")) return null;
        foreach (Transform h in t)
        {
            var g = Buscar(h, ruta);
            if (g != null) return g;
        }
        return null;
    }

    private static float[] Numeros(string v)
    {
        var partes = v.Split(',');
        var r = new float[partes.Length];
        for (int i = 0; i < partes.Length; i++)
            if (!float.TryParse(partes[i], NumberStyles.Float, CultureInfo.InvariantCulture, out r[i])) return null;
        return r;
    }

    // iguales (los numeros con un margen chiquito, por el redondeo al guardar)
    private static bool Iguales(string a, string b)
    {
        if (a == b) return true;
        if (a == null || b == null || a.StartsWith("r:") || a.StartsWith("s:")) return false;
        var x = Numeros(a);
        var y = Numeros(b);
        if (x == null || y == null || x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
            if (Mathf.Abs(x[i] - y[i]) > 1e-4f * Mathf.Max(1f, Mathf.Abs(x[i]))) return false;
        return true;
    }

    private static void Escribir(SerializedProperty p, string v, Scene escena)
    {
        var n = v.StartsWith("r:") || v.StartsWith("s:") ? null : Numeros(v);
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer:
                if (long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) p.longValue = l; break;
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Enum:
            case SerializedPropertyType.ArraySize:
            case SerializedPropertyType.Character:
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int e)) p.intValue = e; break;
            case SerializedPropertyType.Boolean: p.boolValue = v == "1"; break;
            case SerializedPropertyType.Float: if (n != null && n.Length == 1) p.floatValue = n[0]; break;
            case SerializedPropertyType.String: if (v.StartsWith("s:")) p.stringValue = v.Substring(2); break;
            case SerializedPropertyType.Color: if (n != null && n.Length == 4) p.colorValue = new Color(n[0], n[1], n[2], n[3]); break;
            case SerializedPropertyType.Vector2: if (n != null && n.Length == 2) p.vector2Value = new Vector2(n[0], n[1]); break;
            case SerializedPropertyType.Vector3: if (n != null && n.Length == 3) p.vector3Value = new Vector3(n[0], n[1], n[2]); break;
            case SerializedPropertyType.Vector4: if (n != null && n.Length == 4) p.vector4Value = new Vector4(n[0], n[1], n[2], n[3]); break;
            case SerializedPropertyType.Quaternion: if (n != null && n.Length == 4) p.quaternionValue = new Quaternion(n[0], n[1], n[2], n[3]); break;
            case SerializedPropertyType.Rect: if (n != null && n.Length == 4) p.rectValue = new Rect(n[0], n[1], n[2], n[3]); break;
            case SerializedPropertyType.ObjectReference: if (v.StartsWith("r:")) p.objectReferenceValue = Resolver(v, escena); break;
        }
    }

    // ---------- guardar / leer la base ----------

    public static void GuardarBase(Foto f, string ruta)
    {
        var j = new FotoJson();
        j.objetos.AddRange(f.objetos.OrderBy(x => x));
        j.componentes.AddRange(f.componentes.OrderBy(x => x));
        foreach (var kv in f.valores.OrderBy(x => x.Key)) j.valores.Add(new Entrada { k = kv.Key, v = kv.Value });
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllText(ruta, JsonUtility.ToJson(j));
    }

    public static Foto LeerBase(string ruta)
    {
        if (!File.Exists(ruta)) return null;
        var j = JsonUtility.FromJson<FotoJson>(File.ReadAllText(ruta));
        if (j == null) return null;
        var f = new Foto();
        foreach (var o in j.objetos) f.objetos.Add(o);
        foreach (var c in j.componentes) f.componentes.Add(c);
        foreach (var e in j.valores) f.valores[e.k] = e.v;
        return f;
    }

    // ---------- la mezcla ----------

    // actual = la escena antes de rearmar (sus objetos siguen vivos, con las raices renombradas); nuevo = lo recien
    // armado; base = como lo dejo el menu la vez anterior (puede ser null). Devuelve cuantos ajustes conservo.
    public static int Mezclar(Foto actual, Foto nuevo, Foto base_, Scene escena)
    {
        int ajustes = 0;
        var movidos = new List<GameObject>();

        // 1) objetos agregados a mano: se pasan a la escena nueva, bajo el mismo padre
        foreach (var ruta in actual.objetos.OrderBy(r => r.Count(ch => ch == '/')))
        {
            if (nuevo.objetos.Contains(ruta)) continue;
            if (base_ != null && base_.objetos.Contains(ruta)) continue;   // lo armaba el menu y ya no: no va
            int barra = ruta.LastIndexOf('/');
            if (barra < 0) continue;                                      // raiz: ya esta en la escena
            if (movidos.Any(m => ruta.StartsWith(actual.rutaDe[m] + "/"))) continue;   // se mueve con su padre
            if (!nuevo.porRuta.TryGetValue(ruta.Substring(0, barra), out var padre)) continue;
            var go = actual.porRuta[ruta];
            go.transform.SetParent(padre.transform, false);
            movidos.Add(go);
            ajustes++;
        }

        // 2) objetos que se borraron a mano
        if (base_ != null)
            foreach (var ruta in base_.objetos)
                if (!actual.objetos.Contains(ruta) && nuevo.porRuta.TryGetValue(ruta, out var go) && go != null)
                {
                    Object.DestroyImmediate(go);
                    ajustes++;
                }

        // 3) componentes agregados a mano a objetos que arma el menu: se copian
        var pegados = new List<Component>();
        foreach (var rc in actual.componentes)
        {
            if (nuevo.componentes.Contains(rc) || (base_ != null && base_.componentes.Contains(rc))) continue;
            int barra = rc.LastIndexOf('|');
            string ruta = rc.Substring(0, barra);
            if (!nuevo.porRuta.TryGetValue(ruta, out var destino) || destino == null) continue;
            var origen = actual.porRuta[ruta];
            var comp = Componentes(origen).FirstOrDefault(x => ruta + "|" + x.Item2 == rc).Item1;
            if (comp == null || !ComponentUtility.CopyComponent(comp) || !ComponentUtility.PasteComponentAsNew(destino)) continue;
            var c = destino.GetComponents(comp.GetType()).LastOrDefault();
            if (c != null) pegados.Add(c);
            ajustes++;
        }

        // 4) valores cambiados a mano
        var porComponente = new Dictionary<string, List<KeyValuePair<string, string>>>();
        foreach (var kv in actual.valores)
        {
            if (base_ != null && base_.valores.TryGetValue(kv.Key, out var vb) && Iguales(vb, kv.Value)) continue;
            if (nuevo.valores.TryGetValue(kv.Key, out var vn) && Iguales(vn, kv.Value)) continue;
            if (base_ == null && !nuevo.valores.ContainsKey(kv.Key)) continue;
            var partes = kv.Key.Split('|');
            if (partes.Length < 3) continue;
            string clave = partes[0] + "|" + partes[1];
            if (!porComponente.TryGetValue(clave, out var lista)) porComponente[clave] = lista = new List<KeyValuePair<string, string>>();
            lista.Add(new KeyValuePair<string, string>(string.Join("|", partes.Skip(2)), kv.Value));
        }
        foreach (var kv in porComponente)
        {
            var partes = kv.Key.Split('|');
            if (!nuevo.porRuta.TryGetValue(partes[0], out var go) || go == null) continue;
            if (partes[1] == "GameObject")
            {
                foreach (var p in kv.Value) if (p.Key == "activo") { go.SetActive(p.Value == "1"); ajustes++; }
                continue;
            }
            var comp = Componentes(go).FirstOrDefault(x => x.Item2 == partes[1]).Item1;
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            // primero los tamanos de las listas, despues el resto
            foreach (var p in kv.Value.OrderBy(p => p.Key.EndsWith(".Array.size") ? 0 : 1))
            {
                var prop = so.FindProperty(p.Key);
                if (prop == null) continue;
                Escribir(prop, p.Value, escena);
                ajustes++;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 5) lo movido y lo pegado puede apuntar a objetos de la escena vieja: se apunta a los nuevos
        foreach (var go in movidos)
            foreach (var c in go.GetComponentsInChildren<Component>(true)) Reapuntar(c, actual, escena);
        foreach (var c in pegados) Reapuntar(c, actual, escena);

        return ajustes;
    }

    private static void Reapuntar(Component c, Foto actual, Scene escena)
    {
        if (c == null) return;
        var so = new SerializedObject(c);
        var it = so.GetIterator();
        bool cambio = false;
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null) continue;
            if (!actual.rutaDe.TryGetValue(it.objectReferenceValue, out var ruta)) continue;
            // la ruta vieja de ese objeto: se busca el equivalente en la escena nueva
            string clave = it.objectReferenceValue is GameObject ? "GameObject" : ruta.Substring(ruta.LastIndexOf('|') + 1);
            string rutaObjeto = it.objectReferenceValue is GameObject ? ruta : ruta.Substring(0, ruta.LastIndexOf('|'));
            var nuevo = Resolver("r:escena:" + rutaObjeto + "|" + clave, escena);
            if (nuevo != null && nuevo != it.objectReferenceValue) { it.objectReferenceValue = nuevo; cambio = true; }
        }
        if (cambio) so.ApplyModifiedPropertiesWithoutUndo();
    }
}
