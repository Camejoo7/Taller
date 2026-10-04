using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Herramientas de editor para armar el mapa del Stage 2 por código (las usa
/// Claude vía MCP, pero se pueden llamar desde cualquier script de editor).
///
/// - Capture: renderiza una región del mundo a PNG, sin entrar en Play mode.
/// - Ascii: vuelca una región de un tilemap como texto, un carácter por tile,
///   para estudiar cómo está armado un tramo.
/// - Tile / Set / Fill: pintar por nombre de sprite ("Tiles_12") en vez de
///   por referencia, con Undo.
///
/// Las coordenadas de Set/Fill/Ascii son de CELDA del tilemap (no de mundo).
/// Los tilemaps del Stage 2 están a escala 0,32: una celda = 0,32 unidades.
/// </summary>
public static class MapTools
{
    // ------------------------------------------------------------ captura

    /// <summary>
    /// Renderiza el rectángulo de mundo (x0,y0)-(x1,y1) con una cámara
    /// temporal que copia la principal. ppu = píxeles por unidad del PNG
    /// (50 = la densidad nativa del arte a escala 0,32).
    /// </summary>
    public static string Capture(float x0, float y0, float x1, float y1, int ppu, string path)
    {
        Camera main = Camera.main;
        GameObject go = new GameObject("~MapCaptureCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            Camera cam = go.AddComponent<Camera>();
            if (main != null) cam.CopyFrom(main);
            cam.orthographic = true;
            // CopyFrom no copia los datos de URP: sin esto la captura sale sin
            // post-proceso aunque el juego lo tenga.
            var mainData = main != null ? main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>() : null;
            if (mainData != null)
            {
                var data = go.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (data == null) data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = mainData.renderPostProcessing;
            }

            float w = x1 - x0, h = y1 - y0;
            cam.orthographicSize = h * 0.5f;
            cam.aspect = w / h;
            go.transform.position = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, -10f);
            go.transform.rotation = Quaternion.identity;

            int pw = Mathf.Max(8, Mathf.RoundToInt(w * ppu));
            int ph = Mathf.Max(8, Mathf.RoundToInt(h * ppu));
            RenderTexture rt = new RenderTexture(pw, ph, 24);
            rt.filterMode = FilterMode.Point;
            cam.targetTexture = rt;

            // El fondo con parallax depende de dónde esté la cámara: lo
            // acomodamos como si la del juego estuviera centrada acá.
            ParallaxLayer.ApplyAll(go.transform.position, cam.orthographicSize, cam.aspect);
            cam.Render();
            if (main != null) ParallaxLayer.ApplyAll(main.transform.position, main.orthographicSize, main.aspect);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
            if (!string.IsNullOrEmpty(GridFor)) DrawGrid(tex, x0, y0, ppu);
            tex.Apply();
            RenderTexture.active = prev;

            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            return path + " (" + pw + "x" + ph + ")";
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    /// <summary>
    /// Si tiene el nombre de un tilemap, Capture le dibuja encima la grilla de
    /// celdas de ese tilemap: línea tenue por celda, amarilla cada 5 y roja
    /// cada 10 (en la celda 0, 10, 20...), para poder contar celdas en la foto.
    /// </summary>
    public static string GridFor = null;

    static void DrawGrid(Texture2D tex, float x0, float y0, int ppu)
    {
        Tilemap tm = Map(GridFor);
        if (tm == null) return;
        Vector3 o = tm.transform.position;
        float cs = tm.layoutGrid.cellSize.x * tm.transform.lossyScale.x;
        int cx0 = Mathf.FloorToInt((x0 - o.x) / cs) - 1;
        int cx1 = Mathf.CeilToInt((x0 + tex.width / (float)ppu - o.x) / cs) + 1;
        int cy0 = Mathf.FloorToInt((y0 - o.y) / cs) - 1;
        int cy1 = Mathf.CeilToInt((y0 + tex.height / (float)ppu - o.y) / cs) + 1;
        for (int cx = cx0; cx <= cx1; cx++)
        {
            int px = Mathf.RoundToInt((o.x + cx * cs - x0) * ppu);
            if (px < 0 || px >= tex.width) continue;
            Color c = cx % 10 == 0 ? Color.red : cx % 5 == 0 ? Color.yellow : new Color(1f, 1f, 1f, 0.25f);
            for (int py = 0; py < tex.height; py++) Blend(tex, px, py, c);
        }
        for (int cy = cy0; cy <= cy1; cy++)
        {
            int py = Mathf.RoundToInt((o.y + cy * cs - y0) * ppu);
            if (py < 0 || py >= tex.height) continue;
            Color c = cy % 10 == 0 ? Color.red : cy % 5 == 0 ? Color.yellow : new Color(1f, 1f, 1f, 0.25f);
            for (int px = 0; px < tex.width; px++) Blend(tex, px, py, c);
        }
    }

    static void Blend(Texture2D tex, int x, int y, Color c)
    {
        Color b = tex.GetPixel(x, y);
        tex.SetPixel(x, y, Color.Lerp(b, new Color(c.r, c.g, c.b, 1f), c.a));
    }

    /// <summary>
    /// Arma un mosaico de "lo que ve el jugador": un cuadro del tamaño de la
    /// cámara del juego (orto 1,8, 16:9) centrado en cada celda de Piso dada,
    /// en una grilla de 'cols' columnas. Las celdas son {x, y} del piso donde
    /// estaría parado el jugador.
    /// </summary>
    public static string Views(string path, int cols, int ppu, params int[] cells)
    {
        Tilemap piso = Map("Piso");
        float h = 3.6f, w = 6.4f;
        int n = cells.Length / 2;
        int rows = (n + cols - 1) / cols;
        int fw = Mathf.RoundToInt(w * ppu), fh = Mathf.RoundToInt(h * ppu);
        Texture2D sheet = new Texture2D(cols * (fw + 4), rows * (fh + 4), TextureFormat.RGB24, false);
        Color[] fill = new Color[sheet.width * sheet.height];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
        sheet.SetPixels(fill);
        string tmp = Path.Combine(Path.GetDirectoryName(path), "_view_tmp.png");
        for (int i = 0; i < n; i++)
        {
            Vector3 c = piso.GetCellCenterWorld(new Vector3Int(cells[i * 2], cells[i * 2 + 1], 0));
            // el jugador está parado sobre la celda: la cámara lo centra ~0,3 más arriba
            float cx = c.x, cy = c.y + 0.33f;
            Capture(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, ppu, tmp);
            Texture2D t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(tmp));
            int ox = (i % cols) * (fw + 4) + 2, oy = sheet.height - ((i / cols) + 1) * (fh + 4) + 2;
            sheet.SetPixels(ox, oy, Mathf.Min(fw, t.width), Mathf.Min(fh, t.height), t.GetPixels(0, 0, Mathf.Min(fw, t.width), Mathf.Min(fh, t.height)));
            Object.DestroyImmediate(t);
        }
        sheet.Apply();
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        File.Delete(tmp);
        return path + " (" + sheet.width + "x" + sheet.height + ")";
    }

    /// <summary>
    /// Altura del piso en x: el primer collider sólido (no trigger, no del
    /// jugador ni de un enemigo) que encuentra un rayo que baja desde fromY.
    /// Devuelve float.NaN si abajo no hay nada.
    /// </summary>
    public static float GroundY(float x, float fromY)
    {
        Physics2D.SyncTransforms();
        foreach (RaycastHit2D h in Physics2D.RaycastAll(new Vector2(x, fromY), Vector2.down, 60f))
        {
            if (h.collider.isTrigger) continue;
            Rigidbody2D rb = h.collider.attachedRigidbody;
            if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) continue;
            if (h.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            return h.point.y;
        }
        return float.NaN;
    }

    /// <summary>
    /// Como Views, pero en coordenadas de mundo: xy = {x, y, x, y...} con y =
    /// los pies del jugador (la cámara del juego lo centra ahí). Si y es NaN
    /// se usa el piso más alto en esa x (buscando desde fromY).
    /// </summary>
    public static string ViewsXY(string path, int cols, int ppu, float fromY, params float[] xy)
    {
        float h = 3.6f, w = 6.4f;
        int n = xy.Length / 2;
        int rows = (n + cols - 1) / cols;
        int fw = Mathf.RoundToInt(w * ppu), fh = Mathf.RoundToInt(h * ppu);
        Texture2D sheet = new Texture2D(cols * (fw + 4), rows * (fh + 4), TextureFormat.RGB24, false);
        Color[] fill = new Color[sheet.width * sheet.height];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.white;
        sheet.SetPixels(fill);
        string tmp = Path.Combine(Path.GetDirectoryName(path), "_view_tmp.png");
        StringBuilder log = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            float cx = xy[i * 2], cy = xy[i * 2 + 1];
            if (float.IsNaN(cy)) cy = GroundY(cx, fromY);
            if (float.IsNaN(cy)) cy = 0f;
            log.Append(" (" + cx.ToString("F1") + "," + cy.ToString("F2") + ")");
            Capture(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, ppu, tmp);
            Texture2D t = new Texture2D(2, 2);
            t.LoadImage(File.ReadAllBytes(tmp));
            int ox = (i % cols) * (fw + 4) + 2, oy = sheet.height - ((i / cols) + 1) * (fh + 4) + 2;
            sheet.SetPixels(ox, oy, Mathf.Min(fw, t.width), Mathf.Min(fh, t.height), t.GetPixels(0, 0, Mathf.Min(fw, t.width), Mathf.Min(fh, t.height)));
            Object.DestroyImmediate(t);
        }
        sheet.Apply();
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        File.Delete(tmp);
        Object.DestroyImmediate(sheet);
        return path + " (" + (cols * (fw + 4)) + "x" + (rows * (fh + 4)) + ")" + log;
    }

    // ------------------------------------------------------------ tilemaps

    /// <summary>
    /// Un tilemap por nombre: primero bajo "Grid" (el del Stage 2), si no,
    /// cualquiera de la escena con ese nombre (ej. los de "GridCentral").
    /// </summary>
    public static Tilemap Map(string name)
    {
        GameObject go = GameObject.Find("Grid/" + name);
        if (go != null) return go.GetComponent<Tilemap>();
        foreach (Tilemap t in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name) return t;
        return null;
    }

    static Dictionary<string, TileBase> tileCache;
    static Dictionary<string, int> tileRank;

    /// <summary>
    /// Todas las Tile del proyecto, por clave de sprite. Si dos Tile usan el
    /// mismo sprite, gana la de la paleta del Stage 2 (la que usa el mapa
    /// pintado a mano), así lo nuevo queda con los mismos assets.
    /// </summary>
    static Dictionary<string, TileBase> Tiles()
    {
        if (tileCache != null) return tileCache;
        tileCache = new Dictionary<string, TileBase>();
        tileRank = new Dictionary<string, int>();
        foreach (string guid in AssetDatabase.FindAssets("t:TileBase"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            TileBase t = AssetDatabase.LoadAssetAtPath<TileBase>(p);
            Tile tile = t as Tile;
            if (tile == null || tile.sprite == null) continue;
            string key = SpriteKey(tile.sprite);
            int rank = p.Contains("Central City/Assets/Stage2/") ? 0 : p.Contains("Central City") ? 1 : 2;
            int old;
            if (!tileRank.TryGetValue(key, out old) || rank < old)
            {
                tileCache[key] = t;
                tileRank[key] = rank;
            }
        }
        return tileCache;
    }

    public static void ResetCache() { tileCache = null; }

    /// <summary>
    /// Clave única de un sprite: "Hoja/Sprite", ej. "Tiles/Tiles_12" para el
    /// pack Central City. Los sprites de otros packs llevan el pack adelante,
    /// ej. "INDUSTRIA:Tiles/Tiles_12", porque hay hojas con el mismo nombre.
    /// </summary>
    public static string SpriteKey(Sprite s)
    {
        string path = AssetDatabase.GetAssetPath(s);
        string key = Path.GetFileNameWithoutExtension(path) + "/" + s.name;
        if (path.Contains("Central City")) return key;
        const string assets = "Assets/ASSETS/";
        if (path.StartsWith(assets))
        {
            string pack = path.Substring(assets.Length);
            int slash = pack.IndexOf('/');
            if (slash > 0) pack = pack.Substring(0, slash);
            return pack + ":" + key;
        }
        return Path.GetDirectoryName(path).Replace('\\', '/') + ":" + key;
    }

    public static string Key(TileBase t)
    {
        Tile tile = t as Tile;
        if (tile != null && tile.sprite != null) return SpriteKey(tile.sprite);
        return t != null ? "asset:" + t.name : "";
    }

    /// <summary>La Tile de un sprite. Acepta "Tiles/Tiles_12" o solo "Tiles_12" si no es ambiguo.</summary>
    public static TileBase Tile(string key)
    {
        if (string.IsNullOrEmpty(key) || key == ".") return null;
        Dictionary<string, TileBase> all = Tiles();
        TileBase t;
        if (all.TryGetValue(key, out t)) return t;
        if (!key.Contains("/"))
        {
            TileBase found = null;
            foreach (KeyValuePair<string, TileBase> kv in all)
                if (kv.Key.EndsWith("/" + key)) { if (found != null) throw new System.Exception("Tile ambigua: " + key); found = kv.Value; }
            if (found != null) return found;
        }
        throw new System.Exception("No hay Tile para " + key);
    }

    public static void Set(string map, int x, int y, string key)
    {
        Tilemap tm = Map(map);
        Undo.RegisterCompleteObjectUndo(tm, "MapTools");
        tm.SetTile(new Vector3Int(x, y, 0), Tile(key));
    }

    public static void Fill(string map, int x0, int y0, int x1, int y1, string key)
    {
        Tilemap tm = Map(map);
        Undo.RegisterCompleteObjectUndo(tm, "MapTools");
        TileBase t = Tile(key);
        for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
                tm.SetTile(new Vector3Int(x, y, 0), t);
    }

    /// <summary>
    /// Pinta un bloque a partir de filas de texto (la primera fila es la de
    /// ARRIBA). Cada carácter se traduce con el legend; '.' deja la celda
    /// como está y ' ' la borra.
    /// </summary>
    public static void Stamp(string map, int left, int top, string[] rows, Dictionary<char, string> legend)
    {
        Tilemap tm = Map(map);
        Undo.RegisterCompleteObjectUndo(tm, "MapTools");
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c];
                if (ch == '.') continue;
                Vector3Int cell = new Vector3Int(left + c, top - r, 0);
                tm.SetTile(cell, ch == ' ' ? null : Tile(legend[ch]));
            }
    }

    /// <summary>Copia un bloque de celdas de un lugar a otro (mismo tilemap o no).</summary>
    public static void Copy(string fromMap, int x0, int y0, int x1, int y1, string toMap, int dx, int dy, bool skipEmpty)
    {
        Tilemap a = Map(fromMap), b = Map(toMap);
        Undo.RegisterCompleteObjectUndo(b, "MapTools");
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                TileBase t = a.GetTile(new Vector3Int(x, y, 0));
                if (t == null && skipEmpty) continue;
                Vector3Int to = new Vector3Int(x + dx, y + dy, 0);
                b.SetTile(to, t);
                b.SetTransformMatrix(to, a.GetTransformMatrix(new Vector3Int(x, y, 0)));
            }
    }

    // ------------------------------------------------------------ lectura

    const string Symbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#$%&*+=?@^~<>!";

    /// <summary>
    /// Vuelca una región como texto (fila de arriba primero) con su leyenda.
    /// Las celdas vacías son '.'. Las tiles volteadas/rotadas se marcan en la
    /// leyenda con su matriz.
    /// </summary>
    public static string Ascii(string map, int x0, int y0, int x1, int y1)
    {
        Tilemap tm = Map(map);
        Dictionary<string, char> sym = new Dictionary<string, char>();
        StringBuilder grid = new StringBuilder();
        for (int y = y1; y >= y0; y--)
        {
            grid.Append(y.ToString().PadLeft(4)).Append(' ');
            for (int x = x0; x <= x1; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileBase t = tm.GetTile(cell);
                if (t == null) { grid.Append('.'); continue; }
                string k = Key(t);
                Matrix4x4 m = tm.GetTransformMatrix(cell);
                if (m != Matrix4x4.identity)
                {
                    Vector3 s = m.lossyScale;
                    k += (s.x < 0 ? " flipX" : "") + (s.y < 0 ? " flipY" : "") + (Mathf.Abs(m.rotation.eulerAngles.z) > 0.1f ? " rot" + Mathf.RoundToInt(m.rotation.eulerAngles.z) : "");
                }
                char c;
                if (!sym.TryGetValue(k, out c))
                {
                    c = sym.Count < Symbols.Length ? Symbols[sym.Count] : '?';
                    sym[k] = c;
                }
                grid.Append(c);
            }
            grid.AppendLine();
        }
        StringBuilder legend = new StringBuilder();
        foreach (KeyValuePair<string, char> kv in sym) legend.Append(kv.Value).Append('=').Append(kv.Key).Append("  ");
        string head = "     x " + x0 + ".." + x1 + "\n";
        return head + grid + "\n" + legend;
    }
}
