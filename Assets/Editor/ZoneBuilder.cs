using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Piezas para armar zonas del Stage 2 por código, con la misma gramática que
/// usa el tramo pintado a mano (pasarela violeta, pilares, rampas, bloques).
///
/// Todo en coordenadas de CELDA de "Piso" (0,32 unidades por celda). Las capas
/// nuevas (Fachadas, Deco, Frente) se crean con la MISMA posición que Piso,
/// así lo decorativo queda alineado al píxel con lo sólido. (La capa "Visual"
/// original está corrida 2 px respecto de Piso.)
///
///   Piso      Default/5   sólido (colisiona)
///   Frente    Personaje/15 delante del jugador, sin colisión
///   Deco      Default/2   props y estructuras, sin colisión
///   Fachadas  Default/-1  edificios del fondo, sin colisión
///
/// Pensado para generar una zona y después retocarla a mano en el Tile
/// Palette. OJO: volver a correr el armado de una zona BORRA lo que se haya
/// retocado a mano en ese rango.
/// </summary>
public static class ZoneBuilder
{
    public const string Piso = "Piso", Deco = "Deco", Fachadas = "Fachadas", Frente = "Frente";

    // --------------------------------------------------------------- capas

    /// <summary>Crea (si no existen) las capas nuevas alineadas con Piso.</summary>
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

    // --------------------------------------------------------------- básicos

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

    /// <summary>Bloque de filas de texto (la primera es la de ARRIBA). '.' = no tocar.</summary>
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

    /// <summary>Copia un bloque de una sprite sheet: (col,fila) de la hoja, fila 0 = arriba.</summary>
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

    /// <summary>Índice del sprite que está en (col,fila) de la hoja, o -1 si ahí no hay.</summary>
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

    // --------------------------------------------------------------- estructuras

    /// <summary>
    /// Pasarela violeta en la fila g, de x0 a x1. Los pilares (2 celdas de
    /// ancho, en Deco, sin colisión) bajan hasta baseRow. pillars = x izquierda
    /// de cada pilar.
    /// </summary>
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

    /// <summary>
    /// Rampa que BAJA hacia la derecha: arranca en (x0, g) y baja 'steps'
    /// filas, 2 celdas por fila. Devuelve la x donde sigue lo plano (en g-steps).
    /// Debajo de cada tramo va el relleno de la rampa y debajo de eso, fill.
    /// </summary>
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

    /// <summary>Rampa que SUBE hacia la derecha: termina en la fila g+steps.</summary>
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

    /// <summary>
    /// Bloque violeta macizo con borde (el panel 3x3 de Tiles: 26-28 / 32-34 /
    /// 38-40). Su borde de arriba es la misma pieza que la pasarela.
    /// </summary>
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

    /// <summary>Piso de hormigón gris: fila de arriba 46/47 alternadas, abajo 52/53.</summary>
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

    /// <summary>Relleno oscuro de fondo (el "abajo" del nivel), sin colisión.</summary>
    public static void DarkFill(int x0, int x1, int y0, int y1)
    {
        Rect(Fachadas, x0, y0, x1, y1, "Tiles/Tiles_16");
    }

    /// <summary>
    /// Farol: poste parado sobre la fila g (la base va en g+1), brazo hacia la
    /// derecha. Tal cual los del tramo hecho a mano.
    /// </summary>
    public static void Lamp(int x, int g)
    {
        T(Deco, x, g + 1, "Props-01/Props-01_93");
        T(Deco, x, g + 2, "Props-01/Props-01_85");
        T(Deco, x, g + 3, "Props-01/Props-01_79");
        T(Deco, x, g + 4, "Props-01/Props-01_66");
        T(Deco, x + 1, g + 4, "Props-01/Props-01_67");
        T(Deco, x + 2, g + 4, "Props-01/Props-01_69");
    }

    /// <summary>
    /// Plataforma gris (la del tramo a mano): tope sólido en la fila 'row',
    /// de x0 a x1, con soportes de 2 celdas en 'supports' que bajan hasta
    /// baseRow (la fila de la base, apoyada sobre el piso de abajo).
    /// </summary>
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

    /// <summary>Baranda naranja sobre una plataforma (fila row+1), de x0 a x1.</summary>
    public static void Railing(int x0, int x1, int row)
    {
        for (int x = x0; x <= x1; x++)
            T(Deco, x, row + 1, x == x0 ? "Props-01/Props-01_0" : x == x1 ? "Props-01/Props-01_2" : "Props-01/Props-01_1");
    }

    // --------------------------------------------------------------- zona 3

    // Zona 3 (condicionales), "Distrito Neón": la salida del complejo a la
    // calle. Va de la celda 6 (justo después del final del tramo a mano) a la 104.
    public const int Z3Start = 6, Z3End = 104;
    public const int Street = -15;     // fila del tope de la calle
    public const int Bottom = -30;     // hasta dónde llega el relleno oscuro

    public static void BuildZone3Terrain()
    {
        EnsureLayers();

        // 0. Limpiar el rango (capas nuevas + el relleno negro que sobraba a
        //    la derecha del tramo a mano).
        foreach (string l in new[] { Piso, Deco, Fachadas, Frente })
            Clear(l, Z3Start, Bottom, Z3End + 6, 12);
        Clear("FondoVisual", Z3Start, -40, 30, 20);
        Clear("FondoVisual2", Z3Start, -40, 30, 20);

        // El caño del final del tramo a mano era pared (estaba en Piso):
        // lo mismo, pero decorativo, para poder pasar.
        T(Deco, 6, -8, null);
        Tilemap piso = MapTools.Map(Piso), deco = MapTools.Map(Deco);
        // (ya se limpió Piso en x=6: se vuelve a pintar en Deco tal cual)
        T(Deco, 6, -11, "Buildings/Buildings_154");
        T(Deco, 6, -10, "Buildings/Buildings_131");
        T(Deco, 6, -9, "Buildings/Buildings_131");
        T(Deco, 6, -8, "Buildings/Buildings_149");
        deco.SetTransformMatrix(new Vector3Int(6, -8, 0), Matrix4x4.Rotate(Quaternion.Euler(0, 0, 270)));

        // 1. El "abajo". Debajo de suelo firme, oscuro desde la calle (se lee
        //    como tierra). Debajo del VACÍO, oscuro recién muy abajo: si
        //    arrancara a la altura de la calle se leería como un piso negro
        //    caminable, y es un pozo. Así se ve la ciudad de abajo y la niebla.
        DarkFill(6, 66, Bottom, Street - 1);
        DarkFill(67, Z3End + 6, Bottom, Bottom + 4);

        // 2. La calle (2 filas de hormigón) de 8 a 51 y de 55 a 66: el hueco
        //    52..54 es la zanja de obra, el primer pozo (ahí el oscuro sí se
        //    lee como agujero en el piso). Va ANTES que la rampa, que apoya su
        //    último escalón sobre la calle.
        Concrete(8, 51, Street, Street - 1);
        Concrete(55, 66, Street, Street - 1);
        DarkFill(52, 54, Bottom, Street);   // la boca de la zanja, oscura

        // 3. A — salida: la pasarela sigue a la misma altura (fila -12),
        //    elevada sobre la calle, con pilares cortos apoyados en ella.
        Walkway(Z3Start, 19, -12, Street + 1, 10, 16);

        // 4. B — rampa a la calle (3 escalones: -12 → -15).
        SlopeDown(20, -12, 3, Street + 1, "Tiles/Tiles_33");

        // 5. C3 — escalón de hormigón de 2 al final de la calle.
        Concrete(63, 66, Street + 2, Street + 1);

        // 6. D — subida por plataformas grises sobre el vacío. Cada una 2
        //    filas más alta y con 2 celdas de hueco: se ve clarito que hay
        //    que saltar, y caerse cuesta un corazón.
        GrayPlatform(69, 74, -11, Bottom, 71);   // +2 sobre el escalón (-13)
        GrayPlatform(77, 82, -9, Bottom, 79);    // +2

        // 7. E — pasarela alta (fila -9) sobre el vacío: hueco de 2 al
        //    entrar y uno de 3 en el medio.
        Walkway(85, 93, -9, Bottom, 88);
        Walkway(97, Z3End, -9, Bottom, 100);
    }

    // --------------------------------------------------------------- fachadas

    const string B = "Buildings";

    /// <summary>
    /// Edificio de ladrillo en Fachadas: remate gris arriba (fila 0 de la
    /// hoja), ladrillo (filas 1-3, repetidas) y bordes oscuros a los costados.
    /// </summary>
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
        // Bordes: la columna oscura de la hoja (col 5) hace de esquina.
        for (int y = bottom; y < top; y++)
        {
            Sheet(Fachadas, x0, y, B, 25, 5, 1 + ((top - 1 - y) % 3), 1, 1);
            Sheet(Fachadas, x1, y, B, 25, 5, 1 + ((top - 1 - y) % 3), 1, 1);
        }
    }

    /// <summary>Ventanal 4x3 (filas 5-7, columnas 0-3 de la hoja).</summary>
    public static void Window4(int left, int top) { Sheet(Fachadas, left, top, B, 25, 0, 5, 4, 3); }

    /// <summary>Ventana angosta 2x3 (columnas 0-1).</summary>
    public static void Window2(int left, int top) { Sheet(Fachadas, left, top, B, 25, 0, 5, 2, 3); }

    /// <summary>Columna de hormigón (col 6 de la hoja) de 'bottom' a 'top'.</summary>
    public static void ConcreteColumn(string layer, int x, int bottom, int top)
    {
        for (int y = bottom; y <= top; y++) Sheet(layer, x, y, B, 25, 6, 1 + ((top - y) % 3), 1, 1);
    }

    /// <summary>Muro gris oscuro (columnas 7-8 de la hoja), como el lado de un edificio.</summary>
    public static void DarkWall(string layer, int x0, int x1, int bottom, int top)
    {
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
                Sheet(layer, x, y, B, 25, 7 + ((x - x0) & 1), 1 + ((top - y) % 3), 1, 1);
    }

    /// <summary>Prop de varias celdas desde la hoja Props-01.</summary>
    public static void Prop(string layer, int left, int top, int col, int row, int w, int h)
    {
        Sheet(layer, left, top, "Props-01", 8, col, row, w, h);
    }

    public static void BuildZone3Dressing()
    {
        int s = Street;   // -15: la calle; lo que se apoya va en s+1

        // --- La pared exterior del complejo NEXCORP: enmarca la salida y
        //     tapa el corte entre el negro de adentro y el cielo de afuera.
        ConcreteColumn(Fachadas, 6, s - 1, 10);
        DarkWall(Fachadas, 7, 7, s - 1, 10);

        // --- Edificio 1 (ladrillo) detrás de la calle: x 27..50.
        BrickBuilding(27, 50, s + 1, -3);
        // planta baja: local con puerta, vidriera y toldo
        Sheet(Fachadas, 31, s + 3, B, 25, 9, 0, 2, 3);       // puerta con marco
        Window4(34, s + 3);                                  // vidriera
        Window2(38, s + 3);
        for (int x = 33; x <= 40; x++)                       // toldo violeta
        {
            Prop(Deco, x, s + 5, 4, 7, 1, 2);
        }
        Prop(Deco, 35, s + 7, 1, 11, 2, 1);                  // cartel "ANNO"
        Sheet(Fachadas, 44, s + 3, B, 25, 11, 0, 2, 3);      // puerta de servicio
        // pisos de arriba: ventanales y aires acondicionados
        foreach (int wx in new[] { 29, 35, 41, 46 }) Window2(wx, -5);
        foreach (int wx in new[] { 29, 41 }) Window4(wx, -9);
        Prop(Deco, 38, -8, 3, 6, 2, 1);                      // neón rosa
        Sheet(Deco, 46, -9, B, 25, 9, 3, 1, 2);              // aire acondicionado
        Sheet(Deco, 47, -9, B, 25, 10, 3, 2, 2);

        // --- Edificio 2 (hormigón) al fondo del escalón: x 55..65, con portón.
        DarkWall(Fachadas, 55, 65, s + 1, -6);
        for (int x = 55; x <= 65; x++) Sheet(Fachadas, x, -6, B, 25, x % 5, 0, 1, 1);   // remate
        Sheet(Fachadas, 57, s + 3, B, 25, 13, 0, 8, 3);      // portón de garage

        // --- Props de la calle (Deco, sin colisión).
        Lamp(28, s);
        Lamp(48, s);
        Prop(Deco, 26, s + 2, 2, 2, 1, 2);                   // bolardo
        Prop(Deco, 29, s + 2, 3, 0, 2, 2);                   // bolsa de basura grande
        Prop(Deco, 30, s + 1, 2, 4, 1, 1);                   // bolsa chica
        Prop(Deco, 41, s + 4, 0, 5, 3, 4);                   // contenedor verde
        Prop(Deco, 46, s + 3, 0, 2, 2, 3);                   // máquina expendedora
        Prop(Deco, 49, s + 2, 5, 11, 3, 2);                  // valla de obra (antes de la zanja)
        Prop(Deco, 55, s + 2, 5, 11, 3, 2);                  // valla de obra (después)
        Prop(Deco, 61, s + 2, 3, 0, 2, 2);                   // bolsa al pie del escalón

        // --- Salida: un farol al lado de la puerta, del lado de afuera.
        Lamp(13, -12);

        // --- Edificio 2: esquinas de hormigón.
        ConcreteColumn(Fachadas, 55, s + 1, -7);
        ConcreteColumn(Fachadas, 65, s + 1, -7);

        // --- Subida y pasarelas altas: barandas, faroles y una pantalla.
        Railing(70, 73, -11);
        Railing(78, 81, -9);
        Lamp(86, -9);
        Lamp(98, -9);
        // pantalla publicitaria colgada del pilar de la primera pasarela
        Prop(Deco, 90, -10, 3, 2, 4, 2);
    }

    // --------------------------------------------------------------- vista previa

    /// <summary>Pinta todas las piezas en un rincón vacío para mirarlas.</summary>
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
