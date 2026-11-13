using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class ZoneBuilder
{
    public const string Piso = "Piso", Deco = "Deco", Fachadas = "Fachadas", Frente = "Frente";

    public static void EnsureLayers()
    {
        Tilemap piso = MapTools.Map(Piso);
        EnsureLayer(Fachadas, piso, "Default", -1);
        EnsureLayer(Deco, piso, "Default", 2);
        EnsureLayer(Frente, piso, "Personaje", 15);
    }

    static void EnsureLayer(string name, Tilemap like, string sortingLayer, int order)
    {
        if (MapTools.Map(name) != null) return;
        GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        Undo.RegisterCreatedObjectUndo(go, "Capa " + name);
        go.transform.SetParent(like.transform.parent, false);
        go.transform.localPosition = like.transform.localPosition;
        go.transform.localScale = like.transform.localScale;
        Tilemap tm = go.GetComponent<Tilemap>();
        tm.tileAnchor = like.tileAnchor;
        TilemapRenderer r = go.GetComponent<TilemapRenderer>();
        TilemapRenderer lr = like.GetComponent<TilemapRenderer>();
        r.sharedMaterial = lr.sharedMaterial;
        r.sortingLayerName = sortingLayer;
        r.sortingOrder = order;
        r.mode = TilemapRenderer.Mode.Chunk;
    }

    public static void RefreshColliders()
    {
        foreach (string n in new[] { Piso, CPiso })
        {
            Tilemap tm = MapTools.Map(n);
            if (tm == null) continue;
            TilemapCollider2D tc = tm.GetComponent<TilemapCollider2D>();
            CompositeCollider2D cc = tm.GetComponent<CompositeCollider2D>();
            if (tc == null) continue;
            tc.enabled = false;
            tc.enabled = true;
            tc.ProcessTilemapChanges();
            if (cc != null) cc.GenerateGeometry();
        }
        Physics2D.SyncTransforms();
    }

    public static void T(string layer, int x, int y, string key)
    {
        MapTools.Map(layer).SetTile(new Vector3Int(x, y, 0), key == null ? null : MapTools.Tile(key));
    }

    public static void TFlip(string layer, int x, int y, string key)
    {
        Tilemap tm = MapTools.Map(layer);
        Vector3Int c = new Vector3Int(x, y, 0);
        tm.SetTile(c, MapTools.Tile(key));
        tm.SetTransformMatrix(c, Matrix4x4.Scale(new Vector3(-1f, 1f, 1f)));
    }

    public static void Rect(string layer, int x0, int y0, int x1, int y1, string key)
    {
        for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
                T(layer, x, y, key);
    }

    public static void Clear(string layer, int x0, int y0, int x1, int y1)
    {
        Tilemap tm = MapTools.Map(layer);
        if (tm == null) return;
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
                tm.SetTile(new Vector3Int(x, y, 0), null);
    }

    public static void Stamp(string layer, int left, int top, Dictionary<char, string> legend, params string[] rows)
    {
        for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c];
                if (ch == '.') continue;
                T(layer, left + c, top - r, ch == ' ' ? null : legend[ch]);
            }
    }

    public static void Sheet(string layer, int left, int top, string sheet, int sheetCols, int col, int row, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int idx = SheetIndex(sheet, col + dx, row + dy);
                if (idx < 0) continue;
                T(layer, left + dx, top - dy, sheet + "/" + sheet + "_" + idx);
            }
    }

    static Dictionary<string, Dictionary<Vector2Int, int>> sheetIdx = new Dictionary<string, Dictionary<Vector2Int, int>>();

    public static int SheetIndex(string sheet, int col, int row)
    {
        Dictionary<Vector2Int, int> map;
        if (!sheetIdx.TryGetValue(sheet, out map))
        {
            map = new Dictionary<Vector2Int, int>();
            foreach (string guid in AssetDatabase.FindAssets(sheet + " t:Texture2D"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) != sheet || !p.EndsWith(".png")) continue;
                if (!p.Contains("Central City")) continue;
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                int rows = tex.height / 16;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    Sprite s = o as Sprite;
                    if (s == null || s.rect.width != 16 || s.rect.height != 16) continue;
                    int c = (int)(s.rect.x / 16), r = rows - 1 - (int)(s.rect.y / 16);
                    int n;
                    if (int.TryParse(s.name.Substring(s.name.LastIndexOf('_') + 1), out n)) map[new Vector2Int(c, r)] = n;
                }
            }
            sheetIdx[sheet] = map;
        }
        int idx;
        return map.TryGetValue(new Vector2Int(col, row), out idx) ? idx : -1;
    }

    public static void Walkway(int x0, int x1, int g, int baseRow, params int[] pillars)
    {
        Rect(Piso, x0, g, x1, g, "Tiles/Tiles_27");
        foreach (int px in pillars) Pillar(px, g, baseRow);
    }

    public static void Pillar(int px, int g, int baseRow)
    {
        T(Piso, px, g, "Tiles/Tiles_2");
        T(Piso, px + 1, g, "Tiles/Tiles_3");
        T(Deco, px, g - 1, "Tiles/Tiles_24");
        T(Deco, px + 1, g - 1, "Tiles/Tiles_25");
        for (int r = g - 2; r > baseRow; r--)
        {
            T(Deco, px, r, "Tiles/Tiles_30");
            T(Deco, px + 1, r, "Tiles/Tiles_31");
        }
        if (baseRow < g - 1)
        {
            T(Deco, px, baseRow, "Tiles/Tiles_36");
            T(Deco, px + 1, baseRow, "Tiles/Tiles_37");
        }
    }

    public static int SlopeDown(int x0, int g, int steps, int fillTo, string fill)
    {
        for (int i = 0; i < steps; i++)
        {
            int x = x0 + i * 2, y = g - i;
            T(Piso, x, y, "Tiles/Tiles_42");
            T(Piso, x + 1, y, "Tiles/Tiles_43");
            T(Piso, x, y - 1, "Tiles/Tiles_48");
            T(Piso, x + 1, y - 1, "Tiles/Tiles_49");
            if (fill != null)
                for (int r = y - 2; r >= fillTo; r--) { T(Piso, x, r, fill); T(Piso, x + 1, r, fill); }
        }
        return x0 + steps * 2;
    }

    public static int SlopeUp(int x0, int g, int steps, int fillTo, string fill)
    {
        for (int i = 0; i < steps; i++)
        {
            int x = x0 + i * 2, y = g + 1 + i;
            T(Piso, x, y, "Tiles/Tiles_44");
            T(Piso, x + 1, y, "Tiles/Tiles_45");
            T(Piso, x, y - 1, "Tiles/Tiles_50");
            T(Piso, x + 1, y - 1, "Tiles/Tiles_51");
            if (fill != null)
                for (int r = y - 2; r >= fillTo; r--) { T(Piso, x, r, fill); T(Piso, x + 1, r, fill); }
        }
        return x0 + steps * 2;
    }

    public static void Block(string layer, int x0, int top, int x1, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                int col = x == x0 ? 0 : x == x1 ? 2 : 1;
                int row = y == top ? 0 : y == bottom ? 2 : 1;
                int idx = 26 + col + row * 6;
                T(layer, x, y, "Tiles/Tiles_" + idx);
            }
    }

    public static void Concrete(int x0, int x1, int top, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                bool odd = ((x - x0) & 1) == 1;
                string k = y == top ? (odd ? "Tiles/Tiles_47" : "Tiles/Tiles_46") : (odd ? "Tiles/Tiles_53" : "Tiles/Tiles_52");
                T(Piso, x, y, k);
            }
    }

    public static void DarkFill(int x0, int x1, int y0, int y1)
    {
        Rect(Fachadas, x0, y0, x1, y1, "Tiles/Tiles_16");
    }

    public static void Lamp(int x, int g)
    {
        T(Deco, x, g + 1, "Props-01/Props-01_93");
        T(Deco, x, g + 2, "Props-01/Props-01_85");
        T(Deco, x, g + 3, "Props-01/Props-01_79");
        T(Deco, x, g + 4, "Props-01/Props-01_66");
        T(Deco, x + 1, g + 4, "Props-01/Props-01_67");
        T(Deco, x + 2, g + 4, "Props-01/Props-01_69");
    }

    public static void GrayPlatform(int x0, int x1, int row, int baseRow, params int[] supports)
    {
        for (int x = x0; x <= x1; x++)
        {
            string k = x == x0 ? "Buildings/Buildings_0" : x == x1 ? "Buildings/Buildings_3"
                     : ((x - x0) % 3 == 0 ? "Buildings/Buildings_2" : "Buildings/Buildings_1");
            T(Piso, x, row, k);
        }
        foreach (int sx in supports)
        {
            TFlip(Deco, sx - 1, row - 1, "Buildings/Buildings_147");
            T(Deco, sx, row - 1, "Buildings/Buildings_200");
            T(Deco, sx + 1, row - 1, "Buildings/Buildings_200");
            T(Deco, sx + 2, row - 1, "Buildings/Buildings_147");
            for (int r = row - 2; r > baseRow; r--)
            {
                T(Deco, sx, r, "Buildings/Buildings_200");
                T(Deco, sx + 1, r, "Buildings/Buildings_200");
            }
            T(Deco, sx, baseRow, "Buildings/Buildings_225");
            T(Deco, sx + 1, baseRow, "Buildings/Buildings_225");
        }
    }

    public static void Railing(int x0, int x1, int row)
    {
        for (int x = x0; x <= x1; x++)
            T(Deco, x, row + 1, x == x0 ? "Props-01/Props-01_0" : x == x1 ? "Props-01/Props-01_2" : "Props-01/Props-01_1");
    }

    public const int Z3Start = 6, Z3End = 104;
    public const int Street = -15;
    public const int Bottom = -30;

    public static void BuildZone3Terrain()
    {
        EnsureLayers();

        foreach (string l in new[] { Piso, Deco, Fachadas, Frente })
            Clear(l, Z3Start, Bottom, Z3End + 6, 12);
        Clear("FondoVisual", Z3Start, -40, 30, 20);
        Clear("FondoVisual2", Z3Start, -40, 30, 20);

        T(Deco, 6, -8, null);
        Tilemap piso = MapTools.Map(Piso), deco = MapTools.Map(Deco);

        T(Deco, 6, -11, "Buildings/Buildings_154");
        T(Deco, 6, -10, "Buildings/Buildings_131");
        T(Deco, 6, -9, "Buildings/Buildings_131");
        T(Deco, 6, -8, "Buildings/Buildings_149");
        deco.SetTransformMatrix(new Vector3Int(6, -8, 0), Matrix4x4.Rotate(Quaternion.Euler(0, 0, 270)));

        DarkFill(6, 66, Bottom, Street - 1);
        DarkFill(67, Z3End + 6, Bottom, Bottom + 4);

        Concrete(8, 51, Street, Street - 1);
        Concrete(55, 66, Street, Street - 1);
        DarkFill(52, 54, Bottom, Street);

        Walkway(Z3Start, 19, -12, Street + 1, 10, 16);

        SlopeDown(20, -12, 3, Street + 1, "Tiles/Tiles_33");

        Concrete(63, 66, Street + 2, Street + 1);

        GrayPlatform(69, 74, -11, Bottom, 71);
        GrayPlatform(77, 82, -9, Bottom, 79);

        Walkway(85, 93, -9, Bottom, 88);
        Walkway(97, Z3End, -9, Bottom, 100);
        RefreshColliders();
    }

    const string B = "Buildings";

    public static void BrickBuilding(int x0, int x1, int bottom, int top)
    {
        for (int x = x0; x <= x1; x++)
        {
            int col = (x - x0) % 5;
            for (int y = bottom; y <= top; y++)
            {
                int row = y == top ? 0 : 1 + ((top - 1 - y) % 3);
                Sheet(Fachadas, x, y, B, 25, col, row, 1, 1);
            }
        }

        for (int y = bottom; y < top; y++)
        {
            Sheet(Fachadas, x0, y, B, 25, 5, 1 + ((top - 1 - y) % 3), 1, 1);
            Sheet(Fachadas, x1, y, B, 25, 5, 1 + ((top - 1 - y) % 3), 1, 1);
        }
    }

    public static void Window4(int left, int top) { Sheet(Fachadas, left, top, B, 25, 0, 5, 4, 3); }

    public static void Window2(int left, int top) { Sheet(Fachadas, left, top, B, 25, 0, 5, 2, 3); }

    public static void ConcreteColumn(string layer, int x, int bottom, int top)
    {
        for (int y = bottom; y <= top; y++) Sheet(layer, x, y, B, 25, 6, 1 + ((top - y) % 3), 1, 1);
    }

    public static void DarkWall(string layer, int x0, int x1, int bottom, int top)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
                Sheet(layer, x, y, B, 25, 7 + ((x - x0) & 1), 1 + ((top - y) % 3), 1, 1);
    }

    public static void Prop(string layer, int left, int top, int col, int row, int w, int h)
    {
        Sheet(layer, left, top, "Props-01", 8, col, row, w, h);
    }

    public static void BuildZone3Dressing()
    {
        int s = Street;

        ConcreteColumn(Fachadas, 6, s - 1, 10);
        DarkWall(Fachadas, 7, 7, s - 1, 10);

        BrickBuilding(27, 50, s + 1, -3);

        Sheet(Fachadas, 31, s + 3, B, 25, 9, 0, 2, 3);
        Window4(34, s + 3);
        Window2(38, s + 3);
        for (int x = 33; x <= 40; x++)
        {
            Prop(Deco, x, s + 5, 4, 7, 1, 2);
        }
        Prop(Deco, 35, s + 7, 1, 11, 2, 1);
        Sheet(Fachadas, 44, s + 3, B, 25, 11, 0, 2, 3);

        foreach (int wx in new[] { 29, 35, 41, 46 }) Window2(wx, -5);
        foreach (int wx in new[] { 29, 41 }) Window4(wx, -9);
        Prop(Deco, 38, -8, 3, 6, 2, 1);
        Sheet(Deco, 46, -9, B, 25, 9, 3, 1, 2);
        Sheet(Deco, 47, -9, B, 25, 10, 3, 2, 2);

        DarkWall(Fachadas, 55, 65, s + 1, -6);
        for (int x = 55; x <= 65; x++) Sheet(Fachadas, x, -6, B, 25, x % 5, 0, 1, 1);
        Sheet(Fachadas, 57, s + 3, B, 25, 13, 0, 8, 3);

        Lamp(28, s);
        Lamp(48, s);
        Prop(Deco, 26, s + 2, 2, 2, 1, 2);
        Prop(Deco, 29, s + 2, 3, 0, 2, 2);
        Prop(Deco, 30, s + 1, 2, 4, 1, 1);
        Prop(Deco, 41, s + 4, 0, 5, 3, 4);
        Prop(Deco, 46, s + 3, 0, 2, 2, 3);
        Prop(Deco, 49, s + 2, 5, 11, 3, 2);
        Prop(Deco, 55, s + 2, 5, 11, 3, 2);
        Prop(Deco, 61, s + 2, 3, 0, 2, 2);

        Lamp(13, -12);

        ConcreteColumn(Fachadas, 55, s + 1, -7);
        ConcreteColumn(Fachadas, 65, s + 1, -7);

        Railing(70, 73, -11);
        Railing(78, 81, -9);
        Lamp(86, -9);
        Lamp(98, -9);

        Prop(Deco, 90, -10, 3, 2, 4, 2);
    }

    public const string CPiso = "Central_Piso", CFondo = "Central_Fondo";

    public static void EnsureCentralGrid()
    {
        if (MapTools.Map(CPiso) != null) return;
        Tilemap piso = MapTools.Map(Piso);
        GameObject g = new GameObject("GridCentral", typeof(Grid));
        Undo.RegisterCreatedObjectUndo(g, "GridCentral");
        g.GetComponent<Grid>().cellSize = new Vector3(0.32f, 0.32f, 0f);
        g.transform.position = piso.transform.position;

        Tilemap solid = MakeMap(g.transform, CPiso, piso, "Default", 5);
        solid.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        TilemapCollider2D tc = solid.gameObject.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        CompositeCollider2D cc = solid.gameObject.AddComponent<CompositeCollider2D>();
        cc.geometryType = CompositeCollider2D.GeometryType.Outlines;

        Collider2D pc = piso.GetComponent<CompositeCollider2D>();
        if (pc != null && pc.sharedMaterial != null) cc.sharedMaterial = pc.sharedMaterial;

        MakeMap(g.transform, CFondo, piso, "Default", -1);
    }

    static Tilemap MakeMap(Transform parent, string name, Tilemap like, string layer, int order)
    {
        GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(2f, 2f, 1f);
        TilemapRenderer r = go.GetComponent<TilemapRenderer>();
        r.sharedMaterial = like.GetComponent<TilemapRenderer>().sharedMaterial;
        r.sortingLayerName = layer;
        r.sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }

    public static string PS(int n) { return "POWER STATION:Tile_" + n.ToString("00") + "/Tile_" + n.ToString("00"); }

    public static void PSBlock(string layer, int x0, int top, int x1, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                bool l = x == x0, r = x == x1, t = y == top, b = y == bottom;
                int n;
                if (x0 == x1) n = t ? 4 : b ? 20 : 12;
                else if (t) n = l ? 1 : r ? 3 : ((x - x0) % 4 == 2 ? 7 : 2);
                else if (b) n = l ? 17 : r ? 19 : 18;
                else n = l ? 9 : r ? 11 : (((x * 7 + y * 3) % 5) == 0 ? 37 : 10);
                T(layer, x, y, PS(n));
            }
    }

    public static void PSPlatform(string layer, int x0, int x1, int row)
    {
        for (int x = x0; x <= x1; x++)
            T(layer, x, row, PS(x0 == x1 ? 28 : x == x0 ? 25 : x == x1 ? 27 : 26));
    }

    public static void PSPanels(int x0, int x1, int y0, int y1)
    {
        int[] wall = { 50, 51, 52, 54, 61, 62, 55, 53, 63, 64, 57, 58 };
        for (int x = x0; x <= x1; x++)
            for (int y = y0; y <= y1; y++)
            {
                int h = Mathf.Abs(x * 13 + y * 7) % wall.Length;
                T(CFondo, x, y, PS(wall[h]));
            }
    }

    public static GameObject SpriteProp(Transform parent, string path, float x, float baseY, string layer, int order, bool flip, Color tint)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) { s = o as Sprite; if (s != null) break; }
        }
        if (s == null) throw new System.Exception("SpriteProp: no hay sprite en " + path);
        GameObject go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = tint;
        float k = s.pixelsPerUnit / 50f;
        go.transform.localScale = new Vector3(flip ? -k : k, k, 1f);
        float pivotFromBottom = s.pivot.y / s.pixelsPerUnit * k;
        go.transform.position = new Vector3(x, baseY + pivotFromBottom, 0f);
        return go;
    }

    public const int Z4Start = 53, Z4End = 95;

    public static readonly int[][] Nodes = { new[] { 66, -7 }, new[] { 69, -6 }, new[] { 72, -5 }, new[] { 75, -6 } };

    public static float CX(int i) { return -0.01f + 0.64f * i; }
    public static float CTop(int row) { return 0.04f + 0.64f * (row + 1); }

    public static void BuildZone4Terrain()
    {
        EnsureLayers();
        EnsureCentralGrid();
        Clear(CPiso, Z4Start - 2, -20, Z4End + 4, 8);
        Clear(CFondo, Z4Start - 2, -20, Z4End + 4, 8);

        Rect(Piso, 104, -9, 105, -9, "Tiles/Tiles_27");

        PSBlock(CPiso, 53, -5, 60, -12);

        PSBlock(CPiso, 61, -6, 62, -12);
        PSBlock(CPiso, 63, -7, 64, -12);

        foreach (int[] n in Nodes) PSBlock(CPiso, n[0], n[1], n[0] + 1, -16);

        PSBlock(CPiso, 78, -7, Z4End, -12);
        PSBlock(CPiso, 82, -1, Z4End, -2);
        PSPanels(82, Z4End, -6, -3);
        RefreshColliders();
    }

    public static void BuildZone4Dressing()
    {
        GameObject old = GameObject.Find("Zona4_Decoracion");
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject("Zona4_Decoracion").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Zona4");

        string obj = "Assets/ASSETS/POWER STATION/3 Objects/";
        Color dim = new Color(0.55f, 0.5f, 0.75f, 1f);

        SpriteProp(root, obj + "3 Power lines/3.png", CX(68) + 0.32f, CTop(-11), "Default", -3, false, dim);
        SpriteProp(root, obj + "3 Power lines/4.png", CX(74) + 0.32f, CTop(-12), "Default", -3, true, dim);

        System.Type nodeType = null, flipType = null;
        foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
            if (a.GetName().Name == "Assembly-CSharp") { nodeType = a.GetType("PowerNode"); flipType = a.GetType("SpriteFlipbook"); }
        string trap = "Assets/ASSETS/POWER STATION/4 Animated objects/Trap.png";
        List<Sprite> frames = new List<Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(trap)) { Sprite sp = o as Sprite; if (sp != null) frames.Add(sp); }
        frames.Sort((a, b) => a.name.CompareTo(b.name));
        int[][] towers = Nodes;
        for (int i = 0; i < towers.Length; i++)
        {
            GameObject node = SpriteProp(root, trap, CX(towers[i][0]) + 0.64f, CTop(towers[i][1]), "Default", 3, false, Color.white);
            node.name = "Nodo " + (i + 1);
            node.GetComponent<SpriteRenderer>().sprite = frames[0];
            Component fb = node.AddComponent(flipType);
            SerializedObject fso = new SerializedObject(fb);
            SerializedProperty fr = fso.FindProperty("frames");
            fr.arraySize = frames.Count;
            for (int f = 0; f < frames.Count; f++) fr.GetArrayElementAtIndex(f).objectReferenceValue = frames[f];
            fso.FindProperty("frameTime").floatValue = 0.08f;
            fso.ApplyModifiedPropertiesWithoutUndo();
            Component pn = node.AddComponent(nodeType);
            SerializedObject pso = new SerializedObject(pn);

            pso.FindProperty("delay").floatValue = 1.0f + i * 0.45f;
            pso.ApplyModifiedPropertiesWithoutUndo();
        }

        SpriteProp(root, obj + "2 Decoration/24.png", CX(55) + 0.68f, CTop(-5), "Default", 1, false, Color.white);
        SpriteProp(root, obj + "2 Decoration/26.png", CX(58) + 0.7f, CTop(-5), "Default", 1, false, Color.white);
        SpriteProp(root, obj + "2 Decoration/19.png", CX(54) + 0.2f, CTop(-5), "Default", 2, false, Color.white);

        SpriteProp(root, obj + "2 Decoration/20.png", CX(64) + 0.3f, CTop(-7), "Default", 2, false, Color.white);

        SpriteProp(root, obj + "2 Decoration/13.png", CX(84) + 0.6f, CTop(-5) - 0.1f, "Default", 1, false, Color.white);
        SpriteProp(root, obj + "2 Decoration/15.png", CX(87) + 0.6f, CTop(-5) - 0.1f, "Default", 1, false, Color.white);
        SpriteProp(root, obj + "2 Decoration/25.png", CX(90) + 0.68f, CTop(-7), "Default", 1, false, Color.white);
        SpriteProp(root, obj + "2 Decoration/12.png", CX(86) + 0.3f, CTop(-7), "Default", 2, false, Color.white);
        SpriteProp(root, obj + "1 Tube/3.png", CX(82) + 0.2f, CTop(-7), "Default", 1, false, Color.white);
        SpriteProp(root, obj + "1 Tube/4.png", CX(88), CTop(-3) - 0.75f, "Default", 1, false, Color.white);
    }

    public static void BuildZone4Gameplay()
    {
        GameObject t4 = GameObject.Find("TerminalPuerta_Bucles");
        CodeTerminal ct = t4.GetComponentInChildren<CodeTerminal>();
        UnityEngine.Events.UnityEvent ev = ct.onSolved;

        for (int i = ev.GetPersistentEventCount() - 1; i >= 2; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(ev, i);

        GameObject z = GameObject.Find("Zona4");
        if (z != null) Object.DestroyImmediate(z);
        z = new GameObject("Zona4");
        Undo.RegisterCreatedObjectUndo(z, "Zona4");

        GameObject show = new GameObject("Vista Patio de Nodos");
        show.transform.SetParent(z.transform, false);
        show.transform.position = new Vector3((CX(Nodes[0][0]) + CX(Nodes[3][0] + 2)) / 2f, CTop(-6) + 0.2f, 0f);
        CameraShowcase cs = show.AddComponent<CameraShowcase>();
        cs.zoom = 2.7f; cs.weight = 1f; cs.duration = 3.8f;
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ev, cs.Show);

        Transform deco = GameObject.Find("Zona4_Decoracion").transform;
        for (int i = 1; i <= Nodes.Length; i++)
        {
            PowerNode pn = deco.Find("Nodo " + i).GetComponent<PowerNode>();
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ev, pn.PowerOn);
        }
        EditorUtility.SetDirty(ct);

        int[][] cps = { new[] { 54, -5 }, new[] { 63, -7 }, new[] { 79, -7 } };
        string[] names = { "Checkpoint Muralla", "Checkpoint Patio", "Checkpoint Control" };
        for (int i = 0; i < cps.Length; i++)
        {
            GameObject g = new GameObject(names[i]);
            g.transform.SetParent(z.transform, false);
            g.transform.position = new Vector3(CX(cps[i][0]) + 0.32f, CTop(cps[i][1]) + 0.3f, 0f);
            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.3f, 8f);
            g.AddComponent<Checkpoint>();
        }

        GameObject dron = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Dron.prefab");
        GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(dron, z.transform);
        d.name = "Dron Patio";

        d.transform.position = new Vector3(CX(59), CTop(-5) + 1.0f, 0f);
    }

    public const string CAgua = "Central_Agua";
    public const int Z5Start = 96, Z5End = 135;
    public const int DockTop = -7;
    public const int WaterTop = -8;

    public static string SP(int n) { return "Seaport:Tile_" + n.ToString("00") + "/Tile_" + n.ToString("00") + "_0"; }
    public static string SW(int n) { return "Seaport:WaterTile_" + n.ToString("00") + "/WaterTile_" + n.ToString("00") + "_0"; }

    public static void EnsureWaterLayer()
    {
        if (MapTools.Map(CAgua) != null) return;
        EnsureCentralGrid();
        Tilemap like = MapTools.Map(CPiso);
        Tilemap w = MakeMap(like.transform.parent, CAgua, MapTools.Map(Piso), "Personaje", 7);

        w.color = new Color(0.34f, 0.32f, 0.66f, 1f);
    }

    public static void Dock(int x0, int x1, int top, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                bool l = x == x0, r = x == x1, t = y == top, b = y == bottom;
                int n;
                if (x0 == x1) n = t ? 14 : b ? 19 : 15;
                else if (t) n = l ? 1 : r ? 3 : 2;
                else if (b) n = l ? 11 : r ? 13 : 12;
                else n = l ? 6 : r ? 8 : ((x * 5 + y * 3) % 7 == 0 ? 4 : 7);
                T(CPiso, x, y, SP(n));
            }
    }

    public static void Water(int x0, int x1, int top, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                int depth = Mathf.Min(9, top - y);
                T(CAgua, x, y, SW(depth * 4 + 1 + ((x % 4) + 4) % 4));
            }
    }

    public static GameObject SolidProp(Transform parent, string path, float left, float baseY, int order, Color tint)
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        GameObject go = SpriteProp(parent, path, 0f, baseY, "Default", order, false, tint);
        float w = s.bounds.size.x * go.transform.localScale.x;
        Vector3 p = go.transform.position;
        go.transform.position = new Vector3(left + w / 2f, p.y, 0f);
        BoxCollider2D bc = go.AddComponent<BoxCollider2D>();
        Collider2D pisoCol = MapTools.Map(Piso).GetComponent<CompositeCollider2D>();
        if (pisoCol != null) bc.sharedMaterial = pisoCol.sharedMaterial;
        return go;
    }

    public static void BuildZone5Terrain()
    {
        EnsureWaterLayer();
        Clear(CPiso, Z5Start, -20, Z5End + 2, 8);
        Clear(CFondo, Z5Start, -20, Z5End + 2, 8);
        Clear(CAgua, Z5Start, -20, Z5End + 2, 8);

        Dock(96, 103, DockTop, -14);
        Dock(105, 110, DockTop, -14);
        Dock(113, 122, DockTop, -14);
        Dock(126, Z5End, DockTop, -14);
        Water(96, Z5End, WaterTop, -16);
        RefreshColliders();
    }

    public static void BuildZone5Dressing()
    {
        GameObject old = GameObject.Find("Zona5_Puerto");
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject("Zona5_Puerto").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Zona5");

        string cargo = "Assets/ASSETS/Seaport/3 Objects/1 Cargos/";
        string box = "Assets/ASSETS/Seaport/3 Objects/3 Box/";
        string fence = "Assets/ASSETS/Seaport/3 Objects/2 Fencing/";
        string craneDir = "Assets/ASSETS/Seaport/3 Objects/4 Overhead crane/";
        float dock = CTop(DockTop);
        Color night = new Color(0.82f, 0.78f, 0.95f, 1f);
        Color back = new Color(0.45f, 0.4f, 0.6f, 1f);

        SpriteProp(root, cargo + "22.png", CX(97) + 0.9f, dock, "Default", -2, false, back);
        SpriteProp(root, cargo + "11.png", CX(100) + 1.0f, dock, "Default", -2, true, back);
        SpriteProp(root, cargo + "13.png", CX(100) + 0.5f, dock + 0.96f, "Default", -2, false, back);
        SpriteProp(root, fence + "1.png", CX(103) + 0.1f, dock, "Default", 3, false, night);

        SolidProp(root, box + "2.png", CX(109) - 0.6f, dock, 3, night);
        SolidProp(root, cargo + "1.png", CX(109) + 0.15f, dock, 2, night);
        SpriteProp(root, fence + "5.png", CX(106) + 0.3f, dock, "Default", 3, false, night);

        SolidProp(root, box + "3.png", CX(113) + 0.3f, dock, 3, night);
        GameObject verde = SolidProp(root, cargo + "2.png", CX(113) + 0.94f, dock, 2, night);
        GameObject rojo = SolidProp(root, cargo + "5.png", verde.GetComponent<SpriteRenderer>().bounds.max.x + 0.08f, dock, 2, night);
        GameObject gris = SolidProp(root, cargo + "14.png", rojo.GetComponent<SpriteRenderer>().bounds.max.x + 0.08f, dock, 2, night);
        verde.name = "Contenedor 0 verde"; rojo.name = "Contenedor 1 rojo"; gris.name = "Contenedor 2 gris";

        float gapCenter = (CX(123) + CX(126)) / 2f;
        GameObject gantry = SpriteProp(root, craneDir + "Overhead-crane.png", gapCenter, dock, "Default", -1, false, night);
        gantry.name = "Grua";
        float beamBottom = dock + 129f / 50f;
        Sprite cartBody = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ASSETS/Stage2/Generado/Grua_Carro.png");
        GameObject cart = new GameObject("Carro");
        cart.transform.SetParent(root, false);
        SpriteRenderer csr = cart.AddComponent<SpriteRenderer>();
        csr.sprite = cartBody; csr.sortingLayerName = "Default"; csr.sortingOrder = 0; csr.color = night;
        cart.transform.localScale = new Vector3(2f, 2f, 1f);
        cart.transform.position = new Vector3(gapCenter, beamBottom - 0.35f, 0f);

        GameObject cableGo = new GameObject("Cable");
        cableGo.transform.SetParent(root, false);
        SpriteRenderer cab = cableGo.AddComponent<SpriteRenderer>();
        cab.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ASSETS/Stage2/Generado/Pixel.png");
        cab.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        cab.sortingLayerName = "Default"; cab.sortingOrder = -1;
        cableGo.transform.localScale = new Vector3(0.05f, 1f, 1f);

        float hangTop = beamBottom - 0.35f - 0.35f - 0.4f;
        GameObject naranja = SolidProp(root, cargo + "7.png", 0f, hangTop - 0.96f, 4, night);
        float w = naranja.GetComponent<SpriteRenderer>().bounds.size.x;
        naranja.transform.position = new Vector3(gapCenter, naranja.transform.position.y, 0f);
        naranja.name = "Contenedor 3 naranja (grua)";
        CraneLoad load = naranja.AddComponent<CraneLoad>();
        load.cart = cart.transform;
        load.cable = cab;
        load.hookOffset = -0.35f;
        load.lowerOffset = new Vector2(0f, dock - hangTop);
        naranja.GetComponent<BoxCollider2D>().enabled = false;
        load.UpdateCable();

        SpriteProp(root, cargo + "16.png", CX(131) + 0.6f, dock, "Default", -2, false, back);
        SpriteProp(root, cargo + "24.png", CX(134) + 0.2f, dock, "Default", -2, false, back);
        SpriteProp(root, cargo + "4.png", CX(131) + 0.5f, dock + 0.96f, "Default", -2, true, back);
        SpriteProp(root, fence + "2.png", CX(126) + 0.4f, dock, "Default", 3, false, night);

        Lamp(2 * 98, -13);
        Lamp(2 * 130, -13);
    }

    public static void BuildZone5Gameplay(TerminalChallenge challenge, float doorOffset)
    {
        Transform port = GameObject.Find("Zona5_Puerto").transform;
        SpriteRenderer gris = port.Find("Contenedor 2 gris").GetComponent<SpriteRenderer>();
        CraneLoad load = port.Find("Contenedor 3 naranja (grua)").GetComponent<CraneLoad>();

        GameObject old = GameObject.Find("TerminalPuerta_Listas");
        if (old != null) Object.DestroyImmediate(old);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terminal/TerminalPuerta.prefab");
        GameObject t5 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        t5.name = "TerminalPuerta_Listas";
        Undo.RegisterCreatedObjectUndo(t5, "t5");

        float consoleX = gris.bounds.max.x - 0.3f;
        Vector3 root = new Vector3(consoleX + 0.8f, gris.bounds.max.y + doorOffset, 0f);
        t5.transform.position = root;

        GameObject door = t5.transform.Find("PuertaBlindada").gameObject;
        CodeTerminal ct = t5.GetComponentInChildren<CodeTerminal>();
        ct.challenge = challenge;
        UnityEngine.Events.UnityEvent ev = ct.onSolved;
        for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
            if (ev.GetPersistentTarget(i) is PoweredDoor) UnityEditor.Events.UnityEventTools.RemovePersistentListener(ev, i);
        door.SetActive(false);
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ev, load.Lower);
        EditorUtility.SetDirty(ct);

        PowerCurtain pc = t5.GetComponentInChildren<PowerCurtain>();
        pc.leftEdge = CX(123) - root.x;
        pc.rightEdge = CX(Z5End + 1) + 30f - root.x;
        pc.bottom = -9f - root.y;
        pc.top = 4f - root.y;
        EditorUtility.SetDirty(pc);

        GameObject t4 = GameObject.Find("TerminalPuerta_Bucles");
        if (t4 != null)
        {
            PowerCurtain pc4 = t4.GetComponentInChildren<PowerCurtain>();
            pc4.rightEdge = CX(123) - t4.transform.position.x;
            EditorUtility.SetDirty(pc4);
        }

        GameObject z = GameObject.Find("Zona5");
        if (z != null) Object.DestroyImmediate(z);
        z = new GameObject("Zona5");
        Undo.RegisterCreatedObjectUndo(z, "Zona5");
        int[] cps = { 97, 114, 127 };
        string[] names = { "Checkpoint Muelle", "Checkpoint Contenedores", "Checkpoint Puente" };
        for (int i = 0; i < cps.Length; i++)
        {
            GameObject g = new GameObject(names[i]);
            g.transform.SetParent(z.transform, false);
            g.transform.position = new Vector3(CX(cps[i]) + 0.32f, CTop(DockTop) + 0.3f, 0f);
            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.3f, 8f);
            g.AddComponent<Checkpoint>();
        }
        GameObject dron = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Dron.prefab");
        GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(dron, z.transform);
        d.name = "Dron Muelle";

        d.transform.position = new Vector3(CX(101), CTop(DockTop) + 1.2f, 0f);
    }

    public const int Z6Start = 136, Z6End = 210;

    public static string GZ(int n) { return "Green Zone:Tile_" + n.ToString("00") + "/Tile_" + n.ToString("00") + "_0"; }

    public static void GZBlock(int x0, int top, int x1, int bottom)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                bool l = x == x0, r = x == x1, single = x0 == x1;
                int n;
                if (top == bottom) n = single ? 18 : l ? 6 : r ? 8 : 7;
                else if (y == top) n = single ? 5 : l ? 1 : r ? 3 : 2;
                else if (y == bottom) n = single ? 29 : l ? 25 : r ? 28 : 26;
                else if (y == top - 1) n = single ? 17 : l ? 13 : r ? 16 : ((x & 1) == 0 ? 14 : 15);
                else n = single ? 60 : l ? 61 : r ? 62 : 4;
                T(CPiso, x, y, GZ(n));
            }
    }

    public static void GZFloat(int x0, int x1, int row)
    {
        for (int x = x0; x <= x1; x++)
            T(CPiso, x, row, GZ(x0 == x1 ? 52 : x == x0 ? 49 : x == x1 ? 51 : 50));
    }

    public static void BuildZone6Terrain()
    {
        EnsureWaterLayer();
        Clear(CPiso, Z6Start, -20, Z6End + 4, 8);
        Clear(CFondo, Z6Start, -20, Z6End + 4, 8);
        Clear(CAgua, Z6Start, -20, Z6End + 4, 8);

        GZBlock(136, -7, 145, -14);
        GZBlock(146, -6, 149, -14);
        GZBlock(150, -5, 153, -14);

        GZFloat(155, 156, -5);
        GZFloat(158, 159, -4);
        GZFloat(161, 162, -5);
        GZBlock(164, -6, Z6End, -14);
        Water(154, 163, WaterTop, -16);

        GZBlock(Z6End + 1, 2, Z6End + 3, -14);
        RefreshColliders();
    }

    public static void BuildZone6Dressing()
    {
        GameObject old = GameObject.Find("Zona6_Parque");
        if (old != null) Object.DestroyImmediate(old);
        Transform root = new GameObject("Zona6_Parque").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Zona6");

        string ob = "Assets/ASSETS/Green Zone/3 Objects/";
        Color night = new Color(0.78f, 0.78f, 0.92f, 1f);
        Color far = new Color(0.5f, 0.48f, 0.7f, 1f);

        SpriteProp(root, ob + "Other/Tree3.png", CX(139) + 0.4f, CTop(-7), "Default", -3, false, far);
        SpriteProp(root, ob + "Other/Tree4.png", CX(151) + 0.3f, CTop(-5), "Default", -3, false, far);
        SpriteProp(root, ob + "Other/Tree2.png", CX(170), CTop(-6), "Default", -3, true, far);
        SpriteProp(root, ob + "Other/Tree1.png", CX(182), CTop(-6), "Default", -3, false, far);

        SpriteProp(root, ob + "Fence/2.png", CX(137) + 0.3f, CTop(-7), "Default", -1, false, night);
        SpriteProp(root, ob + "Fence/1.png", CX(138) + 0.4f, CTop(-7), "Default", -1, false, night);
        SpriteProp(root, ob + "Bushes/17.png", CX(141) + 0.2f, CTop(-7), "Default", 2, false, night);
        SpriteProp(root, ob + "Other/Garbage_Can1.png", CX(143) + 0.3f, CTop(-7), "Default", 2, false, night);
        SpriteProp(root, ob + "Benches/1.png", CX(144) + 0.2f, CTop(-7), "Default", 1, false, night);

        SpriteProp(root, ob + "Bushes/19.png", CX(147) + 0.3f, CTop(-6), "Default", 2, false, night);
        SpriteProp(root, ob + "Stones/5.png", CX(152) + 0.3f, CTop(-5), "Default", 2, false, night);
        SpriteProp(root, ob + "Bushes/20.png", CX(165) + 0.2f, CTop(-6), "Default", 2, false, night);

        GameObject fuente = SpriteProp(root, "Assets/ASSETS/Stage2/Generado/Fuente_Seca.png", CX(169) + 0.64f, CTop(-6), "Default", 1, false, Color.white);
        fuente.name = "Fuente";
        SpriteFlipbook fb = fuente.AddComponent<SpriteFlipbook>();
        fb.frames = new Sprite[4];
        for (int i = 0; i < 4; i++) fb.frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ASSETS/Stage2/Generado/Fuente_" + i + ".png");
        fb.frameTime = 0.12f;
        PowerNode pn = fuente.AddComponent<PowerNode>();
        pn.offColor = Color.white;
        pn.delay = 0.6f;
        SpriteProp(root, ob + "Benches/2.png", CX(166) + 0.3f, CTop(-6), "Default", 1, false, night);
        SpriteProp(root, ob + "Benches/1.png", CX(172) + 0.2f, CTop(-6), "Default", 1, true, night);
        SpriteProp(root, ob + "Bushes/13.png", CX(178) + 0.2f, CTop(-6), "Default", 2, false, night);
        SpriteProp(root, ob + "Stones/4.png", CX(185) + 0.3f, CTop(-6), "Default", 2, false, night);
        Lamp(2 * 141, -13);
        Lamp(2 * 167, -11);
        Lamp(2 * 181, -11);
    }

    public static void BuildZone6Gameplay(TerminalChallenge challenge, float doorOffset)
    {
        GameObject old = GameObject.Find("TerminalPuerta_Funciones");
        if (old != null) Object.DestroyImmediate(old);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Terminal/TerminalPuerta.prefab");
        GameObject t6 = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        t6.name = "TerminalPuerta_Funciones";
        Undo.RegisterCreatedObjectUndo(t6, "t6");
        Vector3 p = new Vector3(CX(176) + 0.32f, CTop(-6) + doorOffset, 0f);
        t6.transform.position = p;
        CodeTerminal ct = t6.GetComponentInChildren<CodeTerminal>();
        ct.challenge = challenge;
        PowerNode fuente = GameObject.Find("Zona6_Parque/Fuente").GetComponent<PowerNode>();
        UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(ct.onSolved, fuente.PowerOn);
        EditorUtility.SetDirty(ct);
        PowerCurtain pc = t6.GetComponentInChildren<PowerCurtain>();
        pc.leftEdge = 0.3f;
        pc.rightEdge = CX(Z6End + 1) - p.x;
        pc.bottom = -9f - p.y;
        pc.top = 4f - p.y;
        EditorUtility.SetDirty(pc);

        GameObject t5 = GameObject.Find("TerminalPuerta_Listas");
        if (t5 != null)
        {
            PowerCurtain pc5 = t5.GetComponentInChildren<PowerCurtain>();
            pc5.rightEdge = p.x + 0.1f - t5.transform.position.x;
            EditorUtility.SetDirty(pc5);
        }

        GameObject z = GameObject.Find("Zona6");
        if (z != null) Object.DestroyImmediate(z);
        z = new GameObject("Zona6");
        Undo.RegisterCreatedObjectUndo(z, "Zona6");

        int[][] cps = { new[] { 137, -7 }, new[] { 152, -5 }, new[] { 165, -6 } };
        string[] names = { "Checkpoint Parque", "Checkpoint Estanque", "Checkpoint Plaza" };
        for (int i = 0; i < cps.Length; i++)
        {
            GameObject g = new GameObject(names[i]);
            g.transform.SetParent(z.transform, false);
            g.transform.position = new Vector3(CX(cps[i][0]) + 0.32f, CTop(cps[i][1]) + 0.3f, 0f);
            BoxCollider2D bc = g.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.3f, 8f);
            g.AddComponent<Checkpoint>();
        }

        GameObject outro = new GameObject("Kira Cierre");
        outro.transform.SetParent(z.transform, false);
        outro.transform.position = new Vector3(CX(179) + 0.32f, CTop(-6) + 0.6f, 0f);
        BoxCollider2D ob = outro.AddComponent<BoxCollider2D>();
        ob.isTrigger = true;
        ob.size = new Vector2(0.3f, 3f);
        KiraTriggerZone kz = outro.AddComponent<KiraTriggerZone>();
        kz.dialogueManager = Object.FindFirstObjectByType<DialogueManager>();
        kz.segment = DialogueSegment.Outro;

        GameObject exit = new GameObject("Salida Final");
        exit.transform.SetParent(z.transform, false);
        exit.transform.position = new Vector3(CX(Z6End - 3) + 0.32f, CTop(-6) + 0.6f, 0f);
        BoxCollider2D eb = exit.AddComponent<BoxCollider2D>();
        eb.isTrigger = true;
        eb.size = new Vector2(0.3f, 3f);
        StageExit se = exit.AddComponent<StageExit>();
        se.nextScene = "MainMenu";

        GameObject dron = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Dron.prefab");
        GameObject d = (GameObject)PrefabUtility.InstantiatePrefab(dron, z.transform);
        d.name = "Dron Entrada";

        d.transform.position = new Vector3(CX(141) + 0.3f, CTop(-7) + 1.2f, 0f);
    }

    public static void PreviewPieces(int ox, int oy)
    {
        EnsureLayers();
        Walkway(ox, ox + 9, oy, oy - 6, ox + 3);
        SlopeDown(ox + 10, oy, 3, oy - 6, "Tiles/Tiles_33");
        Concrete(ox + 16, ox + 25, oy - 3, oy - 6);
        SlopeUp(ox + 26, oy - 3, 3, oy - 6, "Tiles/Tiles_33");
        Block(Piso, ox + 32, oy, ox + 38, oy - 6);
    }
}
