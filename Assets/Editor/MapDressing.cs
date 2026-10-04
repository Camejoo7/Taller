using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// La pasada visual del Stage 2 (04/10/2026): cielo, fondos por zona,
/// brillos, profundidad. La usa Claude vía MCP, pero se puede llamar desde
/// cualquier script de editor.
///
/// Regla de la casa: todo lo que arma va en objetos y tilemaps NUEVOS (los que
/// empiezan con "Visual_"), sin colliders. Volver a correr una función rehace
/// solo su decorado: no toca lo jugable ni lo pintado a mano.
///
/// Capas de dibujo que usa:
///   Fondo      el cielo y todo lo lejano (detrás de los tilemaps)
///   Default    paredes de fondo y props (los tilemaps del nivel van en -3..5)
///   Personaje  brillos y primer plano (el jugador está en 10)
/// </summary>
public static class MapDressing
{
    public const string Dir = "Assets/ASSETS/Stage2/Generado/Visual/";

    /// <summary>
    /// Rehace toda la pasada visual en orden. Solo toca lo suyo (los objetos
    /// y tilemaps "Visual_"), más tres retoques chicos que también son
    /// visuales y deterministas: los agujeros de las ventanas en el relleno
    /// negro del interior, las piedras en la tierra del parque y el risco del
    /// final. Correrlo dos veces da lo mismo que una.
    /// </summary>
    [MenuItem("CodeBreak/Stage 2: rehacer decorado visual")]
    public static void BuildAll()
    {
        if (Application.isPlaying) { Debug.LogWarning("Salí de Play mode primero."); return; }
        ImportSettings();
        BuildSky();
        BuildInterior();
        BuildInteriorDetails();
        BuildCity();
        BuildPowerStation();
        BuildPort();
        BuildPark();
        BuildLampGlows();
        BuildAtmosphere();
        BuildKiraGlow();
        BuildPostFX(true);
        ClearDiscard();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Decorado del Stage 2 rehecho. Guardá la escena (Ctrl+S).");
    }

    // ------------------------------------------------------------ assets

    /// <summary>
    /// Deja bien importadas las texturas generadas: los brillos y degradés con
    /// filtro suave, el pixel art con Point y sin compresión.
    /// </summary>
    public static string ImportSettings()
    {
        System.Text.StringBuilder log = new System.Text.StringBuilder();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir.TrimEnd('/') }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            string n = System.IO.Path.GetFileNameWithoutExtension(p);
            bool soft = n.StartsWith("Glow") || n == "Cono" || n == "Degrade" || n == "Banda" || n.StartsWith("Luz") || n.StartsWith("Nube");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.filterMode = soft ? FilterMode.Bilinear : FilterMode.Point;
            ti.wrapMode = (n == "Estrellas" || n == "Degrade" || n == "Banda") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;      // hace falta para el modo Tiled
            s.spriteExtrude = 0;
            // Los brillos miden 1 unidad (se escalan al tamaño que haga falta);
            // el pixel art va a 50 px por unidad, como el resto del Stage 2.
            if (soft) s.spritePixelsPerUnit = n == "Cono" ? 64 : (n == "Degrade" || n == "Banda" ? 128 : 64);
            else s.spritePixelsPerUnit = 50;
            if (n == "Cono") { s.spriteAlignment = (int)SpriteAlignment.TopCenter; }
            else if (n.StartsWith("Torre") || n == "Antena" || n.StartsWith("Pared") || n.StartsWith("Base")) s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            else s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);
            ti.SaveAndReimport();
            log.Append(n).Append(' ');
        }
        return log.ToString();
    }

    public static Sprite S(string file)
    {
        string p = file.StartsWith("Assets/") ? file : Dir + file;
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
        if (s == null)
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(p)) { s = o as Sprite; if (s != null) break; }
        if (s == null) throw new System.Exception("MapDressing: no hay sprite en " + p);
        return s;
    }

    /// <summary>Un sprite de una hoja cortada, por nombre ("Buildings_125").</summary>
    public static Sprite Sub(string sheetPath, string spriteName)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
        {
            Sprite s = o as Sprite;
            if (s != null && s.name == spriteName) return s;
        }
        throw new System.Exception("MapDressing: no hay " + spriteName + " en " + sheetPath);
    }

    static Material glowMat;

    /// <summary>Multiplica todos los brillos a la vez (el _Intensity del material).</summary>
    public static float GlowIntensity = 1.2f;

    /// <summary>El material aditivo de los brillos (se crea la primera vez).</summary>
    public static Material GlowMaterial()
    {
        if (glowMat != null) return glowMat;
        string p = Dir + "GlowAditivo.mat";
        glowMat = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (glowMat == null)
        {
            Shader sh = Shader.Find("CodeBreak/Sprite Glow (Aditivo)");
            if (sh == null) throw new System.Exception("No compiló el shader de brillo");
            glowMat = new Material(sh);
            glowMat.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "Glow.png"));
            AssetDatabase.CreateAsset(glowMat, p);
        }
        glowMat.SetFloat("_Intensity", GlowIntensity);
        EditorUtility.SetDirty(glowMat);
        return glowMat;
    }

    static Material unlitMat;

    /// <summary>Sprite-Unlit-Default (para lo que no tiene que oscurecerse nunca).</summary>
    public static Material UnlitMaterial()
    {
        if (unlitMat != null) return unlitMat;
        unlitMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        return unlitMat;
    }

    // ------------------------------------------------------------ objetos

    /// <summary>Borra (si existe) y crea vacío un objeto raíz o hijo con ese nombre.</summary>
    public static Transform Fresh(string name, Transform parent = null)
    {
        if (parent == null)
        {
            foreach (GameObject g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (g.name == name) Object.DestroyImmediate(g);
        }
        else
        {
            Transform old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    public static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>
    /// Un sprite con su pivote en (x, y) de mundo, escalado para que sus
    /// píxeles midan lo mismo que los del nivel (50 por unidad).
    /// </summary>
    public static SpriteRenderer Spr(Transform parent, Sprite s, float x, float y, string layer, int order, Color color, bool flip = false, float scale = 1f)
    {
        GameObject go = new GameObject(s.name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = color;
        float k = s.pixelsPerUnit / 50f * scale;
        go.transform.localScale = new Vector3(flip ? -k : k, k, 1f);
        go.transform.position = new Vector3(x, y, 0f);
        return sr;
    }

    /// <summary>
    /// Como Spr, pero (x, baseY) es el centro de la BASE del dibujo, sea cual
    /// sea su pivote. Así se apoyan los props en el piso sin calcular nada.
    /// </summary>
    public static SpriteRenderer Prop(Transform parent, Sprite s, float x, float baseY, string layer, int order, Color color, bool flip = false)
    {
        SpriteRenderer sr = Spr(parent, s, x, baseY, layer, order, color, flip);
        Bounds b = sr.bounds;
        Vector3 p = sr.transform.position;
        sr.transform.position = new Vector3(p.x + (x - b.center.x), p.y + (baseY - b.min.y), 0f);
        return sr;
    }

    /// <summary>
    /// Interruptor general de los brillos. Kevin pidió sacar todas las luces
    /// (04/10/2026): con esto en false, cada Glow() se arma igual (para que el
    /// código que lo usa no se rompa) pero en un objeto descartable que nunca
    /// se guarda en la escena. Ponerlo en true trae de vuelta todos los brillos.
    /// </summary>
    public static bool Lights = false;

    static Transform discard;

    static Transform Discard()
    {
        if (discard == null)
        {
            GameObject g = new GameObject("~BrillosDescartados");
            g.hideFlags = HideFlags.HideAndDontSave;
            discard = g.transform;
        }
        return discard;
    }

    static void ClearDiscard()
    {
        if (discard != null) Object.DestroyImmediate(discard.gameObject);
        discard = null;
    }

    /// <summary>Un halo de luz aditivo de w x h unidades.</summary>
    public static SpriteRenderer Glow(Transform parent, float x, float y, float w, float h, Color color, string layer, int order, string file = "Glow.png")
    {
        Sprite s = S(file);
        GameObject go = new GameObject("Brillo");
        go.transform.SetParent(Lights ? parent : Discard(), false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sharedMaterial = GlowMaterial();
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = color;
        Vector2 size = s.bounds.size;
        go.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
        go.transform.position = new Vector3(x, y, 0f);
        return sr;
    }

    public static GlowFlicker Flicker(SpriteRenderer sr, GlowFlicker.Mode mode, float depth, float speed, params SpriteRenderer[] linked)
    {
        GlowFlicker f = sr.gameObject.AddComponent<GlowFlicker>();
        f.mode = mode;
        f.depth = depth;
        f.speed = speed;
        f.linked = linked;
        return f;
    }

    /// <summary>Una tira que se repite a lo ancho (modo Tiled), centrada en x.</summary>
    public static SpriteRenderer Strip(Transform parent, Sprite s, float x, float y, float width, string layer, int order, Color color)
    {
        GameObject go = new GameObject(s.name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        float k = s.pixelsPerUnit / 50f;
        go.transform.localScale = new Vector3(k, k, 1f);
        sr.size = new Vector2(width / k, s.bounds.size.y);
        sr.sortingLayerName = layer;
        sr.sortingOrder = order;
        sr.color = color;
        go.transform.position = new Vector3(x, y, 0f);
        return sr;
    }

    public static ParallaxLayer Parallax(Transform t, float fx, float fy, float ox, float oy)
    {
        ParallaxLayer pl = t.gameObject.AddComponent<ParallaxLayer>();
        pl.factor = new Vector2(fx, fy);
        pl.origin = new Vector2(ox, oy);
        return pl;
    }

    public static ZoneBackdrop Zone(Transform t, float fromX, float toX, float fade)
    {
        ZoneBackdrop z = t.gameObject.AddComponent<ZoneBackdrop>();
        z.fromX = fromX;
        z.toX = toX;
        z.fade = fade;
        z.Capture();
        return z;
    }

    /// <summary>Crea (o devuelve) un tilemap nuevo alineado con Piso.</summary>
    public static Tilemap Layer(string name, string sortingLayer, int order, Color tint)
    {
        Tilemap tm = MapTools.Map(name);
        Tilemap piso = MapTools.Map("Piso");
        if (tm == null)
        {
            GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(piso.transform.parent, false);
            go.transform.localPosition = piso.transform.localPosition;
            go.transform.localScale = piso.transform.localScale;
            tm = go.GetComponent<Tilemap>();
            tm.tileAnchor = piso.tileAnchor;
            TilemapRenderer r = go.GetComponent<TilemapRenderer>();
            r.sharedMaterial = piso.GetComponent<TilemapRenderer>().sharedMaterial;
            r.mode = TilemapRenderer.Mode.Chunk;
        }
        TilemapRenderer tr = tm.GetComponent<TilemapRenderer>();
        tr.sortingLayerName = sortingLayer;
        tr.sortingOrder = order;
        tm.color = tint;
        return tm;
    }

    /// <summary>Borde izquierdo de la celda x de Piso / centro / borde de abajo de la fila y.</summary>
    public static float PX(int x) { return -0.01f + 0.32f * x; }
    public static float PY(int y) { return 0.04f + 0.32f * y; }

    // ------------------------------------------------------------ cielo

    /// <summary>
    /// El cielo de todo el mapa: estrellas, la luna, torres lejanas de la
    /// ciudad y autos voladores cruzando. Va detrás de los horizontes por zona
    /// que ya había (FondoParallax), en la capa Fondo.
    /// </summary>
    public static void BuildSky()
    {
        Transform root = Fresh("Visual_Cielo");

        // El degradé del cielo pasa al fondo de todo, y los horizontes que ya
        // había se separan de a 10 para que entren capas nuevas entre medio:
        //   -20 cielo, -19..-13 estrellas/luna/torres/autos, 10 lejos,
        //   15 orilla del puerto, 20 niebla media, 30 medio, 40 cables,
        //   50 cerca, 60 niebla del frente, 65 la ciudad de abajo
        GameObject fondo = GameObject.Find("FondoParallax");
        if (fondo != null)
            foreach (SpriteRenderer sr in fondo.GetComponentsInChildren<SpriteRenderer>(true))
            {
                switch (sr.gameObject.name)
                {
                    case "Cielo": sr.sortingOrder = -20; break;
                    case "EdificiosLejos": sr.sortingOrder = 10; break;
                    case "NieblaMedia": sr.sortingOrder = 20; break;
                    case "EdificiosMedio": sr.sortingOrder = 30; break;
                    case "Cables": sr.sortingOrder = 40; break;
                    case "EdificiosCerca": sr.sortingOrder = 50; break;
                    case "NieblaFrontal": sr.sortingOrder = 60; break;
                }
            }
        // El agua del puerto baja de 7 a 5 para que los reflejos y la niebla
        // entren entre el agua y la cortina de los sectores sin energía (8).
        Tilemap agua = MapTools.Map("Central_Agua");
        if (agua != null) agua.GetComponent<TilemapRenderer>().sortingOrder = 5;

        // --- estrellas: pegadas a la cámara (están infinitamente lejos),
        //     arriba de todo, apagándose hacia el horizonte (ya vienen así).
        Transform stars = Group(root, "Estrellas");
        Parallax(stars, 0.99f, 1f, 0f, 0.35f);
        SpriteRenderer st = Strip(stars, S("Estrellas.png"), 0f, 0f, 24f, "Fondo", -19, new Color(1f, 0.92f, 1f, 0.75f));
        // unas pocas que titilan (brillo encima de una estrella grande)
        float[][] tw = { new[] { -2.6f, 1.2f }, new[] { 1.1f, 1.45f }, new[] { -0.4f, 0.95f }, new[] { 2.9f, 0.7f }, new[] { -1.7f, 0.55f } };
        foreach (float[] p in tw)
        {
            SpriteRenderer g = Glow(stars, p[0], p[1] - 0.35f, 0.14f, 0.14f, new Color(0.9f, 0.8f, 1f, 0.9f), "Fondo", -18, "GlowNucleo.png");
            Flicker(g, GlowFlicker.Mode.Pulse, 0.8f, 0.25f + p[0] * 0.03f);
        }

        // --- la luna, arriba a la derecha, con su halo
        Transform moon = Group(root, "Luna");
        Parallax(moon, 1f, 1f, 1.95f, 1.0f);
        Glow(moon, 0f, 0f, 2.4f, 2.4f, new Color(0.55f, 0.3f, 1f, 0.14f), "Fondo", -18);
        Glow(moon, 0f, 0f, 1.0f, 1.0f, new Color(0.8f, 0.6f, 1f, 0.26f), "Fondo", -18);
        Spr(moon, S("Luna.png"), 0f, 0f, "Fondo", -17, Color.white);

        // --- torres lejanas: casi acompañan a la cámara, así que con pocas
        //     alcanza para todo el recorrido (en 150 unidades de mapa se
        //     corren ~9). Se ven en la ciudad y se apagan al llegar al puerto.
        Transform far = Group(root, "TorresLejanas");
        // Las bases quedan escondidas detrás de los horizontes y la niebla; el
        // mirador de arriba de las torres A tiene que entrar en cuadro, si no
        // se lee como un techo oscuro cortado por el borde de la pantalla.
        Parallax(far, 0.94f, 0.97f, 0f, -2.15f);
        Color farTint = new Color(0.78f, 0.7f, 0.95f, 1f);
        string[] pick = { "TorreA.png", "TorreB.png", "TorreA.png", "TorreB.png", "TorreA.png", "TorreB.png", "TorreA.png" };
        float[] xs = { -6.2f, -3.1f, 0.9f, 3.6f, 6.9f, 9.8f, 13.1f };
        float[] ys = { -0.1f, 0.25f, -0.3f, 0.1f, 0f, 0.35f, -0.2f };
        for (int i = 0; i < xs.Length; i++)
            Prop(far, S(pick[i]), xs[i], ys[i], "Fondo", -15, farTint, i % 3 == 1);
        Zone(far, -1000f, 64f, 8f);

        // --- nubes finas que se arrastran despacio, algunas por delante de la luna
        Transform clouds = Group(root, "Nubes");
        Parallax(clouds, 0.97f, 0.99f, 0f, 0f);
        string[] cl = { "Nube1.png", "Nube2.png", "Nube3.png", "Nube2.png", "Nube1.png" };
        float[][] cpos = { new[] { -6f, 1.05f, 0.09f }, new[] { 1.5f, 1.25f, -0.06f }, new[] { 7f, 0.8f, 0.07f }, new[] { 12f, 1.4f, -0.05f }, new[] { -1f, 0.55f, 0.05f } };
        for (int i = 0; i < cl.Length; i++)
        {
            Sprite cs = S(cl[i]);
            SpriteRenderer c = Spr(clouds, cs, cpos[i][0], cpos[i][1], "Fondo", -16, new Color(0.62f, 0.45f, 0.9f, 0.32f));
            c.transform.localPosition = new Vector3(cpos[i][0], cpos[i][1], 0f);
            float k = cs.pixelsPerUnit / 50f;
            c.transform.localScale = new Vector3(k * 0.5f, k * 0.5f, 1f);   // (64 px por unidad de textura suave → a la mitad)
            Drifter d = c.gameObject.AddComponent<Drifter>();
            d.speed = cpos[i][2];
            d.minX = -14f;
            d.maxX = 22f;
            d.faceDirection = false;
        }

        // --- una nave de carga lejana cruzando el cielo de la ciudad, con balizas
        Transform ship = Group(root, "NaveCarga");
        Parallax(ship, 0.88f, 0.96f, 0f, 1.05f);
        Transform hull = Group(ship, "Nave");
        hull.localPosition = new Vector3(2f, 0f, 0f);
        SpriteRenderer hs = Spr(hull, Sub("Assets/ASSETS/DRONES/1 Drones/2/Drop.png", "Drop_0"), 0f, 0f, "Fondo", -14, new Color(0.32f, 0.24f, 0.48f, 1f));
        hs.transform.localPosition = Vector3.zero;
        SpriteRenderer b1 = Glow(hull, 0f, 0f, 0.18f, 0.18f, A(new Color(1f, 0.2f, 0.25f), 0.9f), "Fondo", -13, "GlowNucleo.png");
        b1.transform.localPosition = new Vector3(-0.72f, 0.12f, 0f);
        Flicker(b1, GlowFlicker.Mode.Pulse, 0.95f, 0.6f);
        SpriteRenderer b2 = Glow(hull, 0f, 0f, 0.14f, 0.14f, A(new Color(0.9f, 0.95f, 1f), 0.9f), "Fondo", -13, "GlowNucleo.png");
        b2.transform.localPosition = new Vector3(0.75f, -0.05f, 0f);
        Flicker(b2, GlowFlicker.Mode.Pulse, 0.95f, 0.9f);
        SpriteRenderer beam = Glow(hull, 0f, 0f, 0.7f, 1.6f, A(new Color(0.8f, 0.9f, 1f), 0.08f), "Fondo", -13, "Cono.png");
        beam.transform.localPosition = new Vector3(0.1f, -0.15f, 0f);
        Drifter dr = hull.gameObject.AddComponent<Drifter>();
        dr.speed = 0.16f;
        dr.minX = -6f;
        dr.maxX = 14f;
        dr.faceDirection = true;
        // (sin ZoneBackdrop: le pisaría el parpadeo a las balizas; una nave
        //  lejana puede pasar por cualquier zona)

        // --- autos voladores: cruzan el cielo a lo lejos, a distintas alturas
        Transform traffic = Group(root, "TraficoAereo");
        Parallax(traffic, 0.9f, 0.97f, 0f, 0f);
        string[] cars = { "Auto1.png", "Auto2.png", "Auto3.png" };
        Color[] lights = { new Color(1f, 0.35f, 0.7f, 0.9f), new Color(0.3f, 0.9f, 1f, 0.9f), new Color(1f, 0.7f, 0.3f, 0.9f) };
        System.Random rnd = new System.Random(11);
        for (int i = 0; i < 14; i++)
        {
            float y = 0.15f + (float)rnd.NextDouble() * 1.25f;
            float x = -20f + (float)rnd.NextDouble() * 40f;
            float speed = (0.25f + (float)rnd.NextDouble() * 0.55f) * (rnd.Next(2) == 0 ? -1f : 1f);
            Transform car = Group(traffic, "Auto " + (i + 1));
            car.position = new Vector3(x, y, 0f);
            SpriteRenderer body = Spr(car, S(cars[i % 3]), x, y, "Fondo", -14, new Color(0.75f, 0.6f, 1f, 1f));
            body.transform.localPosition = Vector3.zero;
            SpriteRenderer lamp = Glow(car, x, y, 0.16f, 0.1f, lights[i % 3], "Fondo", -13, "GlowNucleo.png");
            lamp.transform.localPosition = new Vector3(0.06f, 0f, 0f);
            Drifter d = car.gameObject.AddComponent<Drifter>();
            d.speed = speed;
            d.minX = -20f;
            d.maxX = 20f;
            d.bobAmount = 0.02f;
            d.bobSpeed = 1.3f;
        }
        Zone(traffic, -1000f, 64f, 8f);
    }

    // ------------------------------------------------------------ tiles por hoja

    /// <summary>La Tile de la celda (col, fila) de una hoja del pack Central City (fila 0 = arriba).</summary>
    public static TileBase SheetTile(string sheet, int col, int row)
    {
        int idx = ZoneBuilder.SheetIndex(sheet, col, row);
        if (idx < 0) return null;
        return MapTools.Tile(sheet + "/" + sheet + "_" + idx);
    }

    static void Put(Tilemap tm, int x, int y, TileBase t)
    {
        tm.SetTile(new Vector3Int(x, y, 0), t);
    }

    // ------------------------------------------------------------ zonas 1-2: interior NEXCORP

    // El rectángulo del interior, en celdas de Piso (lo que hoy tapa el relleno
    // negro de FondoVisual).
    public const int IX0 = -112, IX1 = 5, IY0 = -19, IY1 = 7;

    /// <summary>La fila del piso principal (la pasarela de abajo) en cada columna del interior.</summary>
    public static int FloorRow(int x)
    {
        if (x <= -60) return -5;
        if (x <= -38) return -6;
        if (x <= -36) return -7;
        if (x <= -13) return -9;
        if (x <= -11) return -10;
        if (x <= -9) return -11;
        return -12;
    }

    /// <summary>
    /// Un ventanal en la pared del interior: el vidrio deja ver la ciudad de
    /// afuera (el cielo de la capa Fondo), así que además de poner el marco hay
    /// que abrir el agujero en la pared nueva y en el relleno negro de atrás.
    /// left/bottom en celdas de Piso; el tamaño sale del sprite (múltiplo de 16 px).
    /// </summary>
    static SpriteRenderer InteriorWindow(Transform parent, Tilemap wall, string file, int left, int bottom)
    {
        Sprite s = S(file);
        int wCells = Mathf.RoundToInt(s.rect.width / 16f), hCells = Mathf.RoundToInt(s.rect.height / 16f);
        Tilemap fv = MapTools.Map("FondoVisual"), fv2 = MapTools.Map("FondoVisual2");
        for (int x = left; x < left + wCells; x++)
            for (int y = bottom; y < bottom + hCells; y++)
            {
                Put(wall, x, y, null);
                // FondoVisual está en (0,0), corrido 0,01/0,04 de Piso: mismas celdas
                if (fv != null) fv.SetTile(new Vector3Int(x, y, 0), null);
                if (fv2 != null) fv2.SetTile(new Vector3Int(x, y, 0), null);
            }
        SpriteRenderer sr = Spr(parent, s, 0f, 0f, "Default", -3, Color.white);
        Bounds b = sr.bounds;
        sr.transform.position += new Vector3(PX(left) - b.min.x, PY(bottom) - b.min.y, 0f);
        return sr;
    }

    public static void BuildInterior()
    {
        Tilemap fvA = MapTools.Map("FondoVisual"), fvB = MapTools.Map("FondoVisual2");
        // El relleno negro baja dos lugares para hacerle lugar a la pared nueva.
        fvA.GetComponent<TilemapRenderer>().sortingOrder = -6;
        fvB.GetComponent<TilemapRenderer>().sortingOrder = -5;
        // Si se corre de nuevo, primero se tapan los agujeros de las ventanas viejas.
        TileBase black = MapTools.Tile("Tiles/Tiles_16");
        for (int x = IX0; x <= IX1; x++)
            for (int y = IY0; y <= IY1; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                if (fvA.GetTile(c) == null) fvA.SetTile(c, black);
                if (fvB.GetTile(c) == null) fvB.SetTile(c, black);
            }

        Tilemap wall = Layer("Visual_Pared", "Default", -4, new Color(0.40f, 0.38f, 0.55f, 1f));
        Tilemap low = Layer("Visual_ParedBaja", "Default", -4, new Color(0.46f, 0.42f, 0.62f, 1f));
        Tilemap det = Layer("Visual_ParedDetalle", "Default", -3, new Color(0.52f, 0.48f, 0.68f, 1f));
        wall.ClearAllTiles(); low.ClearAllTiles(); det.ClearAllTiles();

        // --- la pared: paneles de metal (el panel 3x3 de Buildings) arriba del
        //     piso, y abajo una estructura de vigas cruzadas en sombra.
        //     Arranca 14 celdas antes del interior: a la izquierda del garage
        //     se veía el cielo violeta (el borde del mundo).
        for (int x = IX0 - 14; x <= IX1; x++)
        {
            int f = FloorRow(x);
            for (int y = IY0; y <= IY1; y++)
            {
                if (y > f)
                {
                    int col = ((x % 3) + 3) % 3, row = 12 + ((((IY1 - y) % 3) + 3) % 3);
                    Put(wall, x, y, SheetTile("Buildings", col, row));
                }
                else
                {
                    // vigas: fila de rejilla + X de 2x2, cada 3 filas
                    int r = (((f - y) % 3) + 3) % 3;
                    int cx = ((x % 2) + 2) % 2;
                    TileBase t = r == 0 ? SheetTile("Tiles", cx, 1) : SheetTile("Tiles", cx, r == 1 ? 2 : 3);
                    Put(low, x, y, t);
                }
            }
        }

        // --- viga del techo (la misma pieza que la pasarela, en sombra) y
        //     zócalo: una franja de rejilla justo arriba del piso.
        for (int x = IX0 - 14; x <= IX1; x++)
        {
            int cx = ((x % 2) + 2) % 2;
            Put(det, x, IY1, SheetTile("Tiles", cx, 0));
            Put(det, x, IY1 - 1, SheetTile("Tiles", cx, 1));
        }

        // --- pilastras: el pilar violeta de la pasarela, de piso a techo
        int[] cols = { -123, -110, -97, -84, -71, -58, -45, -32, -19, -6 };
        foreach (int px in cols)
        {
            int f = Mathf.Min(FloorRow(px), FloorRow(px + 1));
            for (int y = f + 1; y <= IY1 - 2; y++)
            {
                int row = y == IY1 - 2 ? 4 : (y == f + 1 ? 6 : 5);
                Put(det, px, y, SheetTile("Tiles", 0, row));
                Put(det, px + 1, y, SheetTile("Tiles", 1, row));
            }
        }

        Transform root = Fresh("Visual_Interior");

        // --- respaldo negro: las vigas de abajo tienen huecos, y debajo del
        //     relleno negro de FondoVisual (que termina en y −6,08) y a la
        //     izquierda (donde se estiró la pared) se asomaba la ciudad de abajo
        Color black0 = new Color(0.031f, 0.039f, 0.055f, 1f);
        float bx0 = PX(IX0 - 14), bx1 = PX(IX1 + 1);
        BlackRect(root, "RespaldoAbajo", bx0, -12f, bx1, PY(IY0) + 0.04f, black0);
        BlackRect(root, "RespaldoIzquierda", bx0, PY(IY0), PX(IX0) + 0.04f, PY(IY1 + 1), black0);

        // --- ventanales: la ciudad de noche del otro lado del vidrio.
        //     {izquierda, ancho en celdas (archivo), alto sobre el piso}
        Transform wins = Group(root, "Ventanales");
        int[][] w = { new[] { -95, 0 }, new[] { -56, 0 }, new[] { -30, 0 }, new[] { -4, 1 } };
        foreach (int[] wi in w)
        {
            string file = wi[1] == 1 ? "VentanalGigante.png" : "VentanalAncho.png";
            int f = FloorRow(wi[0] + 5);
            InteriorWindow(wins, wall, file, wi[0], f + 2);
        }
    }

    /// <summary>Un rectángulo opaco de color liso (Default/−6, detrás de todo el nivel).</summary>
    static SpriteRenderer BlackRect(Transform parent, string name, float x0, float y0, float x1, float y1, Color c)
    {
        SpriteRenderer r = Spr(parent, S("Punto.png"), (x0 + x1) / 2f, (y0 + y1) / 2f, "Default", -6, c);
        r.name = name;
        Vector2 size = r.sprite.bounds.size;
        r.transform.localScale = new Vector3((x1 - x0) / size.x, (y1 - y0) / size.y, 1f);
        return r;
    }

    /// <summary>Tope del piso principal del interior en la columna x (en unidades de mundo).</summary>
    public static float FloorY(int x) { return PY(FloorRow(x) + 1); }

    static readonly Color Cyan = new Color(0.45f, 0.95f, 1f, 1f);
    static readonly Color Warm = new Color(1f, 0.66f, 0.32f, 1f);

    static Color A(Color c, float a) { return new Color(c.r, c.g, c.b, a); }

    /// <summary>
    /// Lo que cuelga de la pared del interior: lámparas en las pilastras,
    /// pantallas de propaganda, banderas de NEXCORP, paneles de control y los
    /// carteles de neón. Va aparte de BuildInterior para poder retocarlo solo.
    /// </summary>
    public static void BuildInteriorDetails()
    {
        Transform root = Fresh("Visual_InteriorDetalle");

        // (04/10/2026, a pedido de Kevin, se sacaron del interior: las lámparas
        //  de las pilastras con sus conos, la pantalla de propaganda con la
        //  cara, los paneles y monitores de pared, las lucecitas, los
        //  resplandores de abajo y el vapor. Quedan las banderas y los carteles.)

        // --- banderas de NEXCORP colgando del techo sobre la plataforma alta
        Transform flags = Group(root, "Banderas");
        Sprite flag = S("Assets/ASSETS/INDUSTRIA/3 Objects/Flag.png");
        foreach (int fx in new[] { -68, -61 })
            Prop(flags, flag, PX(fx), PY(IY1 - 1) - 1.26f, "Default", -2, new Color(0.9f, 0.85f, 0.95f, 1f));
        Neon(root, "CartelNexcorpChico.png", PX(-64) + 0.16f, PY(4) + 0.1f, Cyan, 0.9f);

        // --- el cartel grande al pie de la rampa (se ve al bajar), y SALIDA
        //     junto a la puerta de afuera
        Neon(root, "CartelNexcorp.png", PX(-7) + 0.1f, FloorY(-7) + 1.12f, Cyan, 1f);
        Neon(root, "CartelSalida.png", PX(4) - 0.1f, FloorY(4) + 1.45f, new Color(0.45f, 1f, 0.55f), 0.9f);
    }

    /// <summary>
    /// Un cartel de neón: el tablero con las letras y, atrás, su halo aditivo
    /// del mismo color, que parpadea junto con las letras.
    /// </summary>
    public static SpriteRenderer Neon(Transform parent, string file, float x, float y, Color color, float strength, int order = -2)
    {
        SpriteRenderer sign = Spr(parent, S(file), x, y, "Default", order, Color.white);
        Sprite halo = S("Luz" + file);
        SpriteRenderer g = Glow(parent, x, y, 1f, 1f, A(color, 0.55f * strength), "Default", order + 1, "Luz" + file);
        // el halo tiene la misma densidad de píxeles que el cartel (50 por unidad)
        g.transform.localScale = new Vector3(halo.pixelsPerUnit / 50f, halo.pixelsPerUnit / 50f, 1f);
        Flicker(g, GlowFlicker.Mode.Neon, 0.7f, 1f, sign);
        return sign;
    }

    /// <summary>Los cuadros de una tira de animación ya cortada, ordenados por nombre (_0, _1...).</summary>
    public static Sprite[] SpriteFrames(string path)
    {
        List<Sprite> frames = new List<Sprite>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path)) { Sprite sp = o as Sprite; if (sp != null) frames.Add(sp); }
        frames.Sort((a, b) => a.name.Length != b.name.Length ? a.name.Length.CompareTo(b.name.Length) : string.CompareOrdinal(a.name, b.name));
        return frames.ToArray();
    }

    /// <summary>Una columna de vapor que sube desde 'at' y se deshace.</summary>
    public static ParticleSystem Steam(Transform parent, Vector2 at)
    {
        ParticleSystem ps = Particles(parent, "Vapor", at, new Vector2(0.25f, 0.05f), ParticleMaterial("ParticulaGlow", "Glow.png"), 40, 5f,
                                      new Vector2(2.2f, 3.2f), new Vector2(0.35f, 0.55f), new Color(0.8f, 0.75f, 1f, 0.14f), new Color(0.7f, 0.8f, 1f, 0.1f),
                                      new Vector2(-0.05f, 0.35f), new Vector2(0.08f, 0.55f), 0.18f, "Default", 9);
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
        return ps;
    }

    /// <summary>Una pantalla con marco (pack Anuncios) mostrando un aviso, con su resplandor.</summary>
    public static void Screen(Transform parent, string adPath, float x, float baseY, bool glow)
    {
        Sprite frame = S("Assets/ASSETS/Anuncios/2 Billboard/128x64.png");
        SpriteRenderer f = Prop(parent, frame, x, baseY, "Default", -2, new Color(0.8f, 0.8f, 0.9f, 1f));
        SpriteRenderer img = Spr(parent, S(adPath), f.bounds.center.x, f.bounds.center.y, "Default", -1, new Color(0.9f, 0.9f, 0.95f, 1f));
        if (glow)
        {
            SpriteRenderer g = Glow(parent, f.bounds.center.x, f.bounds.center.y, f.bounds.size.x * 1.7f, f.bounds.size.y * 1.9f, A(new Color(0.5f, 0.75f, 1f), 0.2f), "Default", -1);
            Flicker(g, GlowFlicker.Mode.Pulse, 0.25f, 0.3f);
        }
    }

    // ------------------------------------------------------------ cables

    static Material lineMat;

    /// <summary>Material de los cables: color del vértice, sin luces (se crea la primera vez).</summary>
    public static Material LineMaterial()
    {
        if (lineMat != null) return lineMat;
        string p = Dir + "Cable.mat";
        lineMat = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (lineMat == null)
        {
            lineMat = new Material(Shader.Find("CodeBreak/Vertex Color Unlit"));
            AssetDatabase.CreateAsset(lineMat, p);
        }
        return lineMat;
    }

    /// <summary>
    /// Un cable colgando entre a y b (catenaria aproximada con una parábola).
    /// Con LineRenderer, de 1 píxel de grosor.
    /// </summary>
    public static LineRenderer Cable(Transform parent, Vector2 a, Vector2 b, float sag, Color color, string layer, int order, float width = 0.025f)
    {
        LineMaterial();
        GameObject go = new GameObject("Cable");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = lineMat;
        lr.useWorldSpace = true;
        lr.widthMultiplier = width;
        lr.numCapVertices = 0;
        lr.startColor = lr.endColor = color;
        lr.sortingLayerName = layer;
        lr.sortingOrder = order;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        int n = 16;
        lr.positionCount = n + 1;
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n;
            Vector2 p = Vector2.Lerp(a, b, t);
            p.y -= sag * 4f * t * (1f - t);
            lr.SetPosition(i, new Vector3(p.x, p.y, 0f));
        }
        return lr;
    }

    // ------------------------------------------------------------ zona 3: la calle

    /// <summary>
    /// Fachada de fondo (más oscura que las de la calle): remate, ladrillo y
    /// ventanas en grilla. Devuelve las celdas (izquierda, arriba) de cada ventana.
    /// </summary>
    static List<Vector2Int> BackFacade(Tilemap tm, int x0, int x1, int bottom, int top, int winEvery, int seed)
    {
        List<Vector2Int> wins = new List<Vector2Int>();
        for (int x = x0; x <= x1; x++)
            for (int y = bottom; y <= top; y++)
            {
                int col = (x == x0 || x == x1) ? 5 : ((x - x0) % 5);
                int row = y == top ? 0 : 1 + ((top - 1 - y) % 3);
                if (y == top && (x == x0 || x == x1)) col = 6;
                Put(tm, x, y, SheetTile("Buildings", col, row));
            }
        System.Random r = new System.Random(seed);
        for (int y = top - 2; y - 2 >= bottom + 1; y -= 4)
            for (int x = x0 + 2; x + 1 <= x1 - 2; x += winEvery)
            {
                if (r.NextDouble() < 0.15) continue;
                for (int dx = 0; dx < 2; dx++)
                    for (int dy = 0; dy < 3; dy++)
                        Put(tm, x + dx, y - dy, SheetTile("Buildings", dx, 5 + dy));
                wins.Add(new Vector2Int(x, y));
            }
        return wins;
    }

    public static void BuildCity()
    {
        int s = ZoneBuilder.Street;   // -15: tope de la calle
        float street = PY(s + 1);     // -4,44

        Tilemap back = Layer("Visual_FachadasFondo", "Default", -3, new Color(0.40f, 0.35f, 0.55f, 1f));
        Tilemap sub = Layer("Visual_Subsuelo", "Default", 0, new Color(0.40f, 0.36f, 0.56f, 1f));
        back.ClearAllTiles(); sub.ClearAllTiles();
        Transform root = Fresh("Visual_Calle");

        // --- edificios de atrás: llenan el cielo vacío detrás de la pasarela
        //     de salida y el hueco entre los dos edificios de la calle.
        Transform lit = Group(root, "VentanasPrendidas");
        System.Random rnd = new System.Random(5);
        Color[] winCols = { new Color(1f, 0.72f, 0.38f), new Color(1f, 0.6f, 0.3f), new Color(0.5f, 0.9f, 1f), new Color(1f, 0.5f, 0.8f) };
        int[][] blocks = { new[] { 8, 26, s + 1, -1, 4 }, new[] { 49, 56, s + 1, -3, 4 }, new[] { 64, 68, s + 1, -4, 3 } };
        for (int bi = 0; bi < blocks.Length; bi++)
        {
            int[] b = blocks[bi];
            foreach (Vector2Int w in BackFacade(back, b[0], b[1], b[2], b[3], b[4], 20 + bi))
            {
                if (rnd.NextDouble() < 0.45) continue;
                Color c = winCols[rnd.Next(winCols.Length)];
                SpriteRenderer g = Glow(lit, PX(w.x) + 0.32f, PY(w.y - 1) + 0.16f, 0.62f, 0.9f, A(c, 0.22f), "Default", -2);
                if (rnd.NextDouble() < 0.25) Flicker(g, GlowFlicker.Mode.Pulse, 0.5f, 0.07f + (float)rnd.NextDouble() * 0.1f);
            }
        }

        // --- vidrieras y puertas prendidas en la planta baja
        Transform shops = Group(root, "Locales");
        Glow(shops, PX(34) + 0.64f, PY(s + 1) + 0.5f, 1.6f, 1.2f, A(Warm, 0.28f), "Default", 3);       // vidriera
        Glow(shops, PX(38) + 0.32f, PY(s + 1) + 0.5f, 0.8f, 1.1f, A(Warm, 0.22f), "Default", 3);
        Glow(shops, PX(31) + 0.32f, PY(s + 1) + 0.45f, 0.75f, 1.0f, A(new Color(1f, 0.45f, 0.75f), 0.2f), "Default", 3);   // puerta
        Glow(shops, PX(57) + 1.28f, PY(s + 1) + 0.35f, 2.8f, 0.9f, A(new Color(0.5f, 0.85f, 1f), 0.12f), "Default", 3);   // portón
        // carteles de neón sobre las puertas (delante de las fachadas, que están en -1)
        Neon(shops, "CartelRamen.png", PX(31) + 0.32f, PY(s + 4) + 0.08f, new Color(1f, 0.6f, 0.25f), 1f, 3);
        Neon(shops, "CartelBar.png", PX(44) + 0.32f, PY(s + 4) + 0.08f, new Color(1f, 0.6f, 0.25f), 1f, 3);
        Neon(shops, "Cartel24h.png", PX(60) + 0.3f, PY(s + 4) + 0.2f, new Color(1f, 0.4f, 0.8f), 1f, 3);
        Neon(shops, "CartelHotel.png", PX(18) + 0.2f, PY(s + 7) + 0.1f, new Color(1f, 0.4f, 0.8f), 0.9f, -2);
        // la pantalla del cartel rosa del edificio de ladrillo ya existe: brillo
        Glow(shops, PX(38) + 0.32f, PY(-8) + 0.16f, 1.1f, 0.6f, A(new Color(1f, 0.45f, 0.85f), 0.35f), "Default", 3);
        // la vereda mojada refleja los carteles
        float[][] puddles = { new[] { PX(31) + 0.32f, 1f, 0.6f, 0.25f }, new[] { PX(44) + 0.32f, 1f, 0.6f, 0.25f }, new[] { PX(60) + 0.3f, 1f, 0.4f, 0.8f } };
        foreach (float[] pd in puddles)
        {
            SpriteRenderer r = Glow(shops, pd[0], street + 0.03f, 1.3f, 0.16f, A(new Color(pd[1], pd[2], pd[3]), 0.35f), "Default", 6);
            Flicker(r, GlowFlicker.Mode.Lamp, 0.3f, 0.7f);
        }
        // la máquina expendedora y el buzón azul dan luz fría
        Glow(shops, PX(46) + 0.32f, street + 0.5f, 1.1f, 1.3f, A(new Color(0.4f, 0.75f, 1f), 0.22f), "Default", 3);

        // --- el subsuelo: debajo de la calle hay estructura, no un negro liso.
        //     Siete filas de vigas que se pierden en lo oscuro; más abajo
        //     sigue el relleno negro de siempre.
        int subBottom = s - 8;
        for (int x = 6; x <= 66; x++)
        {
            if (x >= 52 && x <= 54) continue;           // la zanja sigue siendo un pozo
            for (int y = subBottom; y <= s - 2; y++)
            {
                int r = ((((s - 2) - y) % 3) + 3) % 3;
                int cx = ((x % 2) + 2) % 2;
                Put(sub, x, y, r == 0 ? SheetTile("Tiles", cx, 1) : SheetTile("Tiles", cx, r == 1 ? 2 : 3));
            }
        }
        // un caño rojo corriendo debajo de la calle
        for (int x = 8; x <= 51; x++) Put(sub, x, s - 3, SheetTile("Tiles", 4 + (x & 1), 0));
        for (int x = 55; x <= 66; x++) Put(sub, x, s - 3, SheetTile("Tiles", 4 + (x & 1), 0));
        // sombra: del color del relleno negro, transparente arriba y opaca
        // abajo, así las vigas se funden con el negro sin un corte
        float shTop = PY(s - 1) - 0.2f, shBottom = PY(subBottom);
        Color black = new Color(0.031f, 0.039f, 0.055f, 1f);
        SpriteRenderer shade = Strip(root, S("Degrade.png"), (PX(6) + PX(67)) / 2f, (shTop + shBottom) / 2f, PX(67) - PX(6), "Default", 1, black);
        shade.transform.localScale = new Vector3(shade.transform.localScale.x, (shTop - shBottom) / 1f, 1f);

        // --- sobre el vacío: un cartel gigante con su poste que baja hasta perderse
        // ⚠️ El borde de arriba del cartel parece una cornisa: tiene que quedar
        // FUERA del alcance de un salto (el jugador sube ~0,76 y la plataforma
        // gris más alta está en −2,52), si no alguien intenta pararse ahí y se
        // cae al vacío. Con la base en −3,0 el borde queda en −0,9.
        Transform bb = Group(root, "CartelGigante");
        float bx = PX(76) + 0.1f, by = street + 1.44f;
        Sprite pillar = S("Assets/ASSETS/Anuncios/2 Billboard/Pillar.png");
        for (int i = 0; i < 11; i++)
            Prop(bb, pillar, bx, by - 0.64f * (i + 1), "Default", -3, new Color(0.62f, 0.58f, 0.75f, 1f));
        SpriteRenderer board = Prop(bb, S("Assets/ASSETS/Anuncios/2 Billboard/128x64_2.png"), bx, by, "Default", -3, new Color(0.75f, 0.72f, 0.88f, 1f));
        // (el aviso de la bebida "DUG": el de la pizza trae texto de relleno en latín)
        SpriteRenderer adv = Spr(bb, S("Assets/ASSETS/Anuncios/1 Ad/128x64/11.png"), board.bounds.center.x, board.bounds.max.y - 0.04f - 0.62f - 0.12f, "Default", -2, new Color(0.88f, 0.86f, 0.92f, 1f));
        SpriteRenderer advGlow = Glow(bb, adv.bounds.center.x, adv.bounds.center.y, 4.4f, 2.4f, A(new Color(0.4f, 0.9f, 1f), 0.2f), "Default", -2);
        Flicker(advGlow, GlowFlicker.Mode.Neon, 0.5f, 1f, adv);
        // los focos de arriba del cartel
        for (int i = 0; i < 4; i++)
        {
            float fx = board.bounds.min.x + 0.42f + i * 0.62f;
            Glow(bb, fx, board.bounds.max.y - 0.12f, 0.5f, 0.3f, A(new Color(1f, 0.95f, 0.8f), 0.45f), "Default", -2, "GlowNucleo.png");
            Glow(bb, fx, board.bounds.max.y - 0.14f, 0.9f, 1.4f, A(new Color(1f, 0.95f, 0.8f), 0.08f), "Default", -2, "Cono.png");
        }

        // --- cables entre los faroles y los pilares de las pasarelas
        Transform cables = Group(root, "Cables");
        Color cc = new Color(0.07f, 0.05f, 0.1f, 1f);
        Cable(cables, new Vector2(PX(6) + 0.3f, PY(4)), new Vector2(PX(27), PY(-4)), 0.35f, cc, "Default", -2);
        Cable(cables, new Vector2(PX(6) + 0.3f, PY(2)), new Vector2(PX(27), PY(-5)), 0.5f, cc, "Default", -2);
        Cable(cables, new Vector2(PX(65), PY(-6)), new Vector2(PX(87), PY(-4)), 0.6f, cc, "Default", -4);
        Cable(cables, new Vector2(PX(87), PY(-4)), new Vector2(PX(99), PY(-4)), 0.35f, cc, "Default", -4);
        Cable(cables, new Vector2(PX(99), PY(-4)), new Vector2(PX(112), PY(-2)), 0.4f, cc, "Default", -4);
        // banderines de colores en un cable sobre la calle
        LineRenderer flagLine = Cable(cables, new Vector2(PX(27), PY(-6)), new Vector2(PX(50), PY(-5)), 0.45f, cc, "Default", 1);
        Color[] fcols = { new Color(1f, 0.35f, 0.6f), new Color(0.3f, 0.9f, 1f), new Color(1f, 0.8f, 0.3f) };
        for (int i = 1; i < flagLine.positionCount - 1; i++)
        {
            Vector3 p = flagLine.GetPosition(i);
            SpriteRenderer f = Glow(cables, p.x, p.y - 0.04f, 0.09f, 0.12f, A(fcols[i % 3], 0.9f), "Default", 1, "GlowNucleo.png");
            Flicker(f, GlowFlicker.Mode.Pulse, 0.6f, 0.4f + i * 0.05f);
        }

        // --- la ciudad de abajo: se ve por el vacío, lejos y en la niebla
        Transform low = Group(root, "CiudadBaja");
        Parallax(low, 0.82f, 0.92f, 0f, -2.6f);
        Strip(low, S("CiudadBaja.png"), 10f, 0f, 60f, "Fondo", 65, new Color(0.85f, 0.8f, 1f, 1f));
        Zone(low, -1000f, 62f, 6f);
    }

    // ------------------------------------------------------------ zona 4: central eléctrica

    static float CX(int i) { return ZoneBuilder.CX(i); }
    static float CTop(int row) { return ZoneBuilder.CTop(row); }

    public static void BuildPowerStation()
    {
        Transform root = Fresh("Visual_Central");
        string tube = "Assets/ASSETS/POWER STATION/3 Objects/1 Tube/";
        string deco = "Assets/ASSETS/POWER STATION/3 Objects/2 Decoration/";
        Color wallTint = new Color(0.85f, 0.85f, 0.95f, 1f);
        Color elec = new Color(0.45f, 0.85f, 1f, 1f);

        // --- la muralla: caños y una lámpara sobre la pared de ladrillo
        Transform wall = Group(root, "Muralla");
        float top = CTop(-5);                                   // -2,52: donde camina el jugador
        Prop(wall, S(tube + "4.png"), CX(55) + 0.2f, top - 1.35f, "Default", 6, wallTint);
        Prop(wall, S(tube + "3.png"), CX(53) + 0.42f, top - 1.95f, "Default", 6, wallTint);
        Prop(wall, S(tube + "1.png"), CX(58) + 0.45f, top - 1.05f, "Default", 6, wallTint);
        Prop(wall, S(tube + "10.png"), CX(59) + 0.5f, top - 1.9f, "Default", 6, wallTint);

        // --- el patio de nodos: cables de la red entre las torres-nodo, y
        //     un resplandor en cada nodo que se prende recién cuando el for
        //     los reinicia (lo maneja PowerNode: se mira su animación)
        Transform yard = Group(root, "PatioNodos");
        Color cable = new Color(0.08f, 0.06f, 0.12f, 1f);
        Transform deco4 = GameObject.Find("Zona4_Decoracion") != null ? GameObject.Find("Zona4_Decoracion").transform : null;
        Vector2 prev = new Vector2(CX(64) + 0.5f, CTop(-7) + 0.7f);
        for (int i = 0; i < ZoneBuilder.Nodes.Length; i++)
        {
            int[] n = ZoneBuilder.Nodes[i];
            Vector2 a = new Vector2(CX(n[0]) + 0.64f, CTop(n[1]) + 0.32f);
            Cable(yard, prev, a, 0.28f, cable, "Default", 2);
            prev = a;
            if (deco4 == null) continue;
            Transform node = deco4.Find("Nodo " + (i + 1));
            if (node == null) continue;
            SpriteRenderer g = Glow(yard, a.x, CTop(n[1]) + 0.62f, 1.6f, 1.6f, A(elec, 0f), "Default", 4);
            GlowWhenActive gw = g.gameObject.AddComponent<GlowWhenActive>();
            gw.watch = node.GetComponent<SpriteFlipbook>();
            gw.onAlpha = 0.45f;
            SpriteRenderer g2 = Glow(yard, a.x, CTop(n[1]) + 0.1f, 3.2f, 2.6f, A(elec, 0f), "Default", 4);
            GlowWhenActive gw2 = g2.gameObject.AddComponent<GlowWhenActive>();
            gw2.watch = gw.watch;
            gw2.onAlpha = 0.14f;
        }
        Cable(yard, prev, new Vector2(CX(78) + 0.4f, CTop(-7) + 1.6f), 0.3f, cable, "Default", 2);
        // las torres de alta tensión del fondo, unidas entre sí y con el borde
        Color farCable = new Color(0.2f, 0.16f, 0.32f, 1f);
        Cable(yard, new Vector2(CX(60), -1.0f), new Vector2(CX(68) + 0.05f, -2.78f), 0.3f, farCable, "Default", -4);
        Cable(yard, new Vector2(CX(68) + 0.6f, -2.78f), new Vector2(CX(74) - 0.05f, -3.42f), 0.35f, farCable, "Default", -4);
        Cable(yard, new Vector2(CX(74) + 0.7f, -3.42f), new Vector2(CX(80), -1.4f), 0.4f, farCable, "Default", -4);
        Cable(yard, new Vector2(CX(60), -1.3f), new Vector2(CX(68) + 0.05f, -3.05f), 0.3f, farCable, "Default", -4);
        Cable(yard, new Vector2(CX(68) + 0.6f, -3.05f), new Vector2(CX(74) - 0.05f, -3.7f), 0.35f, farCable, "Default", -4);

        // (04/10/2026: las lámparas y tubos de luz de la sala de control se
        //  sacaron junto con todas las luces)
        Transform room = Group(root, "SalaControl");
        float floor = CTop(-7);                                 // -3,80

        // --- la base de ladrillo de la sala (lo que se ve debajo del piso
        //     cuando estás adentro): caños
        Transform baseW = Group(room, "BaseSala");
        Prop(baseW, S(tube + "4.png"), CX(84) + 0.4f, floor - 1.35f, "Default", 6, wallTint, true);
        Prop(baseW, S(tube + "2.png"), CX(89) + 0.3f, floor - 0.95f, "Default", 6, wallTint);
        Prop(baseW, S(tube + "3.png"), CX(92) + 0.2f, floor - 1.75f, "Default", 6, wallTint);
        Prop(baseW, S(tube + "1.png"), CX(80) + 0.5f, floor - 0.85f, "Default", 6, wallTint);
        SpriteRenderer warn = Glow(baseW, CX(86) + 0.3f, floor - 0.55f, 0.16f, 0.16f, A(new Color(1f, 0.6f, 0.15f), 0.9f), "Default", 7, "GlowNucleo.png");
        Flicker(warn, GlowFlicker.Mode.Pulse, 0.95f, 0.7f);

        // --- el resplandor de la ciudad de abajo entre las torres, y niebla
        //     baja sobre el vacío del patio
        SpriteRenderer fog = Strip(root, S("Banda.png"), (CX(64) + CX(78)) / 2f, -4.9f, CX(78) - CX(64), "Default", 1, new Color(0.45f, 0.25f, 0.85f, 0.5f));
        fog.transform.localScale = new Vector3(fog.transform.localScale.x, 1.8f, 1f);
    }

    // ------------------------------------------------------------ zona 5: el puerto

    /// <summary>
    /// Un reflejo vertical que tiembla sobre el agua. Personaje/6: arriba del
    /// agua (5) y debajo de la cortina de los sectores sin energía (8).
    /// </summary>
    static SpriteRenderer Reflection(Transform parent, float x, float waterY, float w, float h, Color c)
    {
        SpriteRenderer r = Glow(parent, x, waterY - h * 0.45f, w, h, c, "Personaje", 6);
        Flicker(r, GlowFlicker.Mode.Lamp, 0.35f, 0.9f);
        Wobble wb = r.gameObject.AddComponent<Wobble>();
        wb.amount = 0.03f;
        wb.stretch = 0.25f;
        return r;
    }

    public static void BuildPort()
    {
        Transform root = Fresh("Visual_Puerto");
        float dock = CTop(ZoneBuilder.DockTop);                 // -3,80
        float water = CTop(ZoneBuilder.WaterTop);               // -4,44
        float x0 = CX(ZoneBuilder.Z5Start), x1 = CX(ZoneBuilder.Z5End + 1);

        // --- la luna reflejada: sigue a la cámara en x (como la luna) y
        //     queda fija sobre el agua en y
        // (el brillo lo maneja ZoneBackdrop: aparece solo sobre el agua del
        //  puerto; el temblor es de posición, con Wobble, para no pelearse)
        Transform moonRef = Group(root, "ReflejoLuna");
        Parallax(moonRef, 1f, 0f, 1.95f, water - 0.05f);
        for (int i = 0; i < 6; i++)
        {
            float w = 0.75f - i * 0.08f;
            SpriteRenderer r = Glow(moonRef, 0f, 0f, w, 0.09f, A(new Color(0.85f, 0.7f, 1f), 0.75f - i * 0.1f), "Personaje", 6, "GlowNucleo.png");
            r.transform.localPosition = new Vector3((i % 2 == 0 ? 0.03f : -0.04f), -0.08f - i * 0.17f, 0f);
            Wobble wb = r.gameObject.AddComponent<Wobble>();
            wb.amount = 0.05f;
            wb.stretch = 0.3f;
            wb.speed = 1.2f + i * 0.2f;
        }
        // (el reflejo queda 1,95 a la derecha de la cámara: la zona es el
        //  agua del puerto corrida esa distancia)
        Zone(moonRef, x0 - 1f, x1 - 2.6f, 2f);

        // --- reflejos de los faroles del muelle y del brillo de la grúa
        Transform refl = Group(root, "Reflejos");
        foreach (float lx in new[] { PX(2 * 98) + 0.5f, PX(2 * 130) + 0.5f })
            Reflection(refl, lx, water, 0.45f, 1.1f, A(Warm, 0.35f));
        // chispitas sueltas en la superficie
        System.Random rnd = new System.Random(9);
        for (float x = x0 + 0.4f; x < x1; x += 0.7f + (float)rnd.NextDouble() * 1.1f)
        {
            SpriteRenderer g = Glow(refl, x, water - 0.06f - (float)rnd.NextDouble() * 0.5f, 0.16f, 0.04f, A(new Color(0.7f, 0.8f, 1f), 0.6f), "Personaje", 6, "GlowNucleo.png");
            Flicker(g, GlowFlicker.Mode.Pulse, 0.95f, 0.3f + (float)rnd.NextDouble() * 0.5f);
        }

        // --- niebla baja sobre el agua: manchas suaves que se arrastran despacio
        //     (bordes difusos, así no se nota dónde termina sobre el muelle)
        Transform mist = Group(root, "Niebla");
        for (int i = 0; i < 9; i++)
        {
            float mx = x0 + 1.5f + i * (x1 - x0 - 3f) / 8f;
            SpriteRenderer m = Glow(mist, mx, water + 0.05f - (i % 2) * 0.18f, 4.5f, 0.7f, A(new Color(0.55f, 0.4f, 1f), 0.16f), "Personaje", 7);
            Drifter d = m.gameObject.AddComponent<Drifter>();
            d.speed = (i % 2 == 0 ? 0.1f : -0.07f);
            d.minX = x0 + 1.5f;
            d.maxX = x1 - 1.5f;
            d.faceDirection = false;
        }

        // --- pilas de contenedores al fondo del muelle: un patio de carga de
        //     verdad, no cuatro cajas sueltas (oscuras, detrás de todo lo jugable)
        Transform stacks = Group(root, "PilasFondo");
        string cargo = "Assets/ASSETS/Seaport/3 Objects/1 Cargos/";
        Color farBox = new Color(0.36f, 0.32f, 0.52f, 1f);
        // {x izquierda del muelle, archivos de abajo hacia arriba...}
        object[][] piles = {
            new object[] { CX(101) + 0.3f, "21", "23" },
            new object[] { CX(106) + 0.2f, "26", "22", "25" },
            new object[] { CX(116) + 0.1f, "23" },
            new object[] { CX(127) + 0.2f, "22", "21" },
            new object[] { CX(133) + 0.4f, "25", "26" } };
        foreach (object[] pile in piles)
        {
            float px = (float)pile[0];
            float y = dock;
            for (int i = 1; i < pile.Length; i++)
            {
                SpriteRenderer c = Prop(stacks, S(cargo + (string)pile[i] + ".png"), px + (i % 2) * 0.12f, y, "Default", -4, farBox, i % 2 == 0);
                y = c.bounds.max.y - 0.02f;
            }
        }
        // una luz de patio sobre las pilas
        Glow(stacks, CX(106) + 1.2f, dock + 2.4f, 3.2f, 2.6f, A(new Color(1f, 0.75f, 0.45f), 0.08f), "Default", -3);

        // --- la grúa: balizas rojas arriba y un reflector que alumbra la carga
        Transform crane = Group(root, "Grua");
        GameObject g0 = GameObject.Find("Zona5_Puerto/Grua");
        if (g0 != null)
        {
            Bounds b = g0.GetComponent<SpriteRenderer>().bounds;
            foreach (float bx in new[] { b.min.x + 0.12f, b.max.x - 0.12f })
            {
                SpriteRenderer red = Glow(crane, bx, b.max.y + 0.02f, 0.35f, 0.35f, A(new Color(1f, 0.15f, 0.2f), 0.85f), "Default", 6, "GlowNucleo.png");
                Flicker(red, GlowFlicker.Mode.Pulse, 0.95f, 0.55f);
            }
            Glow(crane, b.center.x, b.max.y - 0.5f, 2.6f, 2.8f, A(new Color(1f, 0.85f, 0.55f), 0.1f), "Default", 6, "Cono.png");
        }

        // --- la ciudad al otro lado del agua: un horizonte de luces lejanas
        //     justo sobre la línea del agua (detrás de todo)
        // (el agua la tapa de la línea de flotación para abajo: se ven solo
        //  las terrazas de los edificios y sus ventanas)
        Transform far = Group(root, "OrillaLejana");
        Parallax(far, 0.86f, 0.9f, 0f, -0.9f);
        // (la capa se corre con la cámara: posición local ≈ 0,14 × x de la cámara)
        Strip(far, S("CiudadBaja.png"), 10.5f, 0f, 30f, "Fondo", 15, new Color(0.6f, 0.55f, 0.85f, 1f));
        Zone(far, x0 - 2f, CX(ZoneBuilder.Z6Start) + 4f, 5f);
    }

    // ------------------------------------------------------------ zona 6: la zona verde

    public static void BuildPark()
    {
        Transform root = Fresh("Visual_Parque");
        Tilemap solid = MapTools.Map(ZoneBuilder.CPiso);
        string ob = "Assets/ASSETS/Green Zone/3 Objects/";
        Color night = new Color(0.78f, 0.78f, 0.92f, 1f);

        // --- la tierra: el relleno liso (Tile 04) se salpica con las variantes
        //     de piedras del pack. Son tiles enteras y opacas, así que el
        //     collider no cambia.
        TileBase plain = MapTools.Tile(ZoneBuilder.GZ(4));
        TileBase[] stones = { MapTools.Tile(ZoneBuilder.GZ(40)), MapTools.Tile(ZoneBuilder.GZ(39)), MapTools.Tile(ZoneBuilder.GZ(38)), MapTools.Tile(ZoneBuilder.GZ(37)) };
        int swapped = 0;
        for (int x = ZoneBuilder.Z6Start; x <= ZoneBuilder.Z6End + 3; x++)
            for (int y = -14; y <= 2; y++)
            {
                Vector3Int c = new Vector3Int(x, y, 0);
                if (solid.GetTile(c) != plain) continue;
                int h = Mathf.Abs((x * 73856093) ^ (y * 19349663)) % 100;
                TileBase t = h < 18 ? stones[0] : h < 30 ? stones[1] : h < 38 ? stones[2] : h < 42 ? stones[3] : null;
                if (t == null) continue;
                solid.SetTile(c, t);
                swapped++;
            }

        // --- el estanque: chispas, niebla y el reflejo de la luna (se suma a
        //     la zona del reflejo del puerto)
        float water = CTop(ZoneBuilder.WaterTop);
        float p0 = CX(154), p1 = CX(164);
        Transform pond = Group(root, "Estanque");
        System.Random rnd = new System.Random(4);
        for (float x = p0 + 0.3f; x < p1 - 0.2f; x += 0.6f + (float)rnd.NextDouble() * 0.8f)
        {
            SpriteRenderer g = Glow(pond, x, water - 0.06f - (float)rnd.NextDouble() * 0.4f, 0.16f, 0.04f, A(new Color(0.7f, 0.9f, 1f), 0.6f), "Personaje", 6, "GlowNucleo.png");
            Flicker(g, GlowFlicker.Mode.Pulse, 0.95f, 0.3f + (float)rnd.NextDouble() * 0.5f);
        }
        for (int i = 0; i < 3; i++)
        {
            SpriteRenderer m = Glow(pond, p0 + 1.2f + i * 2.2f, water + 0.05f, 3.2f, 0.6f, A(new Color(0.5f, 0.45f, 1f), 0.15f), "Personaje", 7);
            Drifter d = m.gameObject.AddComponent<Drifter>();
            d.speed = i % 2 == 0 ? 0.08f : -0.06f;
            d.minX = p0 + 1f;
            d.maxX = p1 - 1f;
            d.faceDirection = false;
        }
        Transform moonRef = Group(pond, "ReflejoLuna");
        Parallax(moonRef, 1f, 0f, 1.95f, water - 0.05f);
        for (int i = 0; i < 5; i++)
        {
            SpriteRenderer r = Glow(moonRef, 0f, 0f, 0.7f - i * 0.09f, 0.09f, A(new Color(0.85f, 0.7f, 1f), 0.7f - i * 0.12f), "Personaje", 6, "GlowNucleo.png");
            r.transform.localPosition = new Vector3((i % 2 == 0 ? 0.03f : -0.04f), -0.08f - i * 0.17f, 0f);
            Wobble wb = r.gameObject.AddComponent<Wobble>();
            wb.amount = 0.05f; wb.stretch = 0.3f; wb.speed = 1.3f + i * 0.2f;
        }
        Zone(moonRef, p0 - 1.2f, p1 - 0.6f, 1.6f);

        // --- la fuente: cuando Kira queda libre (PowerNode prende su
        //     animación), el agua brilla
        GameObject fuente = GameObject.Find("Zona6_Parque/Fuente");
        if (fuente != null)
        {
            Bounds fb = fuente.GetComponent<SpriteRenderer>().bounds;
            SpriteRenderer fg = Glow(root, fb.center.x, fb.center.y + 0.15f, 2.6f, 2.2f, A(new Color(0.4f, 0.85f, 1f), 0f), "Default", 6);
            GlowWhenActive gw = fg.gameObject.AddComponent<GlowWhenActive>();
            gw.watch = fuente.GetComponent<SpriteFlipbook>();
            gw.onAlpha = 0.35f;
            gw.fadeSpeed = 1.2f;
            SpriteRenderer fg2 = Glow(root, fb.center.x, fb.min.y + 0.05f, 3.4f, 0.4f, A(new Color(0.4f, 0.85f, 1f), 0f), "Default", 6);
            GlowWhenActive gw2 = fg2.gameObject.AddComponent<GlowWhenActive>();
            gw2.watch = gw.watch;
            gw2.onAlpha = 0.25f;
            gw2.fadeSpeed = 1.2f;
        }

        // (04/10/2026: las guirnaldas de luces de la plaza se sacaron junto
        //  con todas las luces)

        // --- el final del mapa: la pared del borde del mundo deja de ser una
        //     columna lisa. Árboles grandes y arbustos delante la tapan, y la
        //     cerca con el portón marca la salida.
        Transform end = Group(root, "Final");
        float ground = CTop(-6);                                 // -3,16: la plaza
        float wallX = CX(ZoneBuilder.Z6End + 1);                 // 120,95
        // la columna pasa a ser un risco de piedra (mismas celdas, mismas
        // tiles opacas: el collider queda igual)
        for (int y = -13; y <= 1; y++)
        {
            solid.SetTile(new Vector3Int(ZoneBuilder.Z6End + 1, y, 0), MapTools.Tile(ZoneBuilder.GZ(63)));
            solid.SetTile(new Vector3Int(ZoneBuilder.Z6End + 2, y, 0), MapTools.Tile(ZoneBuilder.GZ(y % 2 == 0 ? 65 : 64)));
            solid.SetTile(new Vector3Int(ZoneBuilder.Z6End + 3, y, 0), MapTools.Tile(ZoneBuilder.GZ(66)));
        }
        Prop(end, S(ob + "Other/Tree4.png"), wallX + 0.3f, ground, "Default", 6, new Color(0.62f, 0.62f, 0.8f, 1f), true);
        Prop(end, S(ob + "Other/Tree3.png"), wallX + 1.6f, ground + 0.5f, "Default", 6, new Color(0.5f, 0.5f, 0.7f, 1f));
        Prop(end, S(ob + "Bushes/17.png"), wallX - 0.25f, ground, "Default", 7, night);
        Prop(end, S(ob + "Bushes/19.png"), wallX + 0.6f, ground, "Default", 7, night, true);
        Prop(end, S(ob + "Bushes/18.png"), wallX - 0.9f, ground, "Default", 7, night);
        // la salida: un portón abierto en la cerca, justo donde está el trigger
        float exitX = CX(185) + 0.32f;
        Prop(end, S(ob + "Fence/6.png"), exitX - 0.95f, ground, "Default", -1, night);
        Prop(end, S(ob + "Fence/2.png"), exitX + 0.2f, ground, "Default", -1, night);
        Prop(end, S(ob + "Fence/7.png"), exitX + 1.25f, ground, "Default", -1, night);

        // --- la ciudad a lo lejos, detrás de los árboles del parque: se ve
        //     de dónde viene el jugador
        Transform city = Group(root, "CiudadLejana");
        // (baja y oscura: solo asoman las terrazas sobre los árboles del fondo;
        //  el parque es la zona libre, la ciudad tiene que quedar atrás)
        Parallax(city, 0.9f, 0.95f, 0f, -0.45f);
        // (la capa se corre con la cámara: posición local ≈ 0,1 × x de la cámara)
        // (detrás de la niebla media, así queda lejos y brumosa)
        Strip(city, S("CiudadBaja.png"), 11f, 0f, 30f, "Fondo", 15, new Color(0.42f, 0.36f, 0.62f, 1f));
        Zone(city, CX(ZoneBuilder.Z6Start) - 2f, 1000f, 6f);

        // --- más arbustos y piedras sobre el camino, para que el pasto no
        //     quede pelado
        Transform bush = Group(root, "Arbustos");
        float[][] bs = {
            new[] { CX(137) + 0.1f, CTop(-7), 3 }, new[] { CX(143) + 0.5f, CTop(-7), 9 }, new[] { CX(147) + 0.6f, CTop(-6), 14 },
            new[] { CX(150) + 0.2f, CTop(-5), 16 }, new[] { CX(164) + 0.6f, CTop(-6), 11 }, new[] { CX(168) + 0.1f, CTop(-6), 13 },
            new[] { CX(173) + 0.5f, CTop(-6), 15 }, new[] { CX(176) - 0.6f, CTop(-6), 20 }, new[] { CX(180) + 0.2f, CTop(-6), 12 },
            new[] { CX(183) + 0.4f, CTop(-6), 10 } };
        foreach (float[] b in bs)
            Prop(bush, S(ob + "Bushes/" + (int)b[2] + ".png"), b[0], b[1], "Default", 2, night, ((int)b[2] % 2) == 0);
        // pastitos sueltos
        for (int i = 0; i < 26; i++)
        {
            int cx = ZoneBuilder.Z6Start + 1 + (int)(rnd.NextDouble() * (ZoneBuilder.Z6End - ZoneBuilder.Z6Start - 2));
            if (cx >= 154 && cx <= 163) continue;                // el estanque
            int row = cx < 146 ? -7 : cx < 150 ? -6 : cx < 154 ? -5 : -6;
            float gx = CX(cx) + (float)rnd.NextDouble() * 0.6f;
            Prop(bush, S(ob + "Grass/" + (1 + rnd.Next(15)) + ".png"), gx, CTop(row), "Default", 3, night, rnd.Next(2) == 0);
        }
        Debug.Log("BuildPark: " + swapped + " tiles de tierra con piedras");
    }

    // ------------------------------------------------------------ ambiente: abismo y partículas

    static Material ParticleMaterial(string name, string texture)
    {
        string p = Dir + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("CodeBreak/Particle Glow (Aditivo)"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + texture));
        m.SetFloat("_Intensity", 1.2f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static ParticleSystem.MinMaxCurve Range(float a, float b) { return new ParticleSystem.MinMaxCurve(a, b); }

    /// <summary>Un sistema de partículas que llena una caja (centro, tamaño) y deja flotar lo que suelta.</summary>
    static ParticleSystem Particles(Transform parent, string name, Vector2 center, Vector2 box, Material mat, int max, float rate,
                                    Vector2 life, Vector2 size, Color c0, Color c1, Vector2 velMin, Vector2 velMax, float noise, string layer, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startLifetime = Range(life.x, life.y);
        main.startSpeed = 0f;
        main.startSize = Range(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(c0, c1);
        main.maxParticles = max;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = rate;
        ParticleSystem.ShapeModule sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = new Vector3(box.x, box.y, 0f);
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = Range(velMin.x, velMax.x);
        vel.y = Range(velMin.y, velMax.y);
        vel.z = Range(0f, 0f);
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        if (noise > 0f)
        {
            ParticleSystem.NoiseModule nz = ps.noise;
            nz.enabled = true;
            nz.strength = noise;
            nz.frequency = 0.35f;
            nz.scrollSpeed = 0.15f;
            nz.quality = ParticleSystemNoiseQuality.Low;
        }
        ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingLayerName = layer;
        r.sortingOrder = order;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    /// <summary>
    /// Lo que va en todo el mapa: la oscuridad del fondo del vacío (abajo de
    /// todo se pierde en negro, en vez de terminar en un corte), polvo adentro
    /// del complejo, lluvia fina en la calle y luciérnagas en el parque.
    /// </summary>
    public static void BuildAtmosphere()
    {
        Transform root = Fresh("Visual_Ambiente");

        // --- el abismo: delante del agua (5) y sus reflejos (6), debajo de la
        //     cortina de los sectores sin energía (8), así en un sector a
        //     oscuras no se nota una costura entre los dos negros
        float left = -37f, right = 125f;
        Color abyss = new Color(0.035f, 0.02f, 0.06f, 1f);
        float top = -5.1f, bottom = -7.3f;
        SpriteRenderer grad = Strip(root, S("Degrade.png"), (left + right) / 2f, (top + bottom) / 2f, right - left, "Personaje", 7, abyss);
        grad.name = "Abismo";
        grad.transform.localScale = new Vector3(grad.transform.localScale.x, top - bottom, 1f);
        SpriteRenderer solidBelow = Spr(root, S("Punto.png"), (left + right) / 2f, bottom - 3f, "Personaje", 7, abyss);
        solidBelow.name = "AbismoFondo";
        Vector2 ps = solidBelow.sprite.bounds.size;
        solidBelow.transform.localScale = new Vector3((right - left) / ps.x, 6.02f / ps.y, 1f);

        // --- partículas: solo la lluvia. (04/10/2026, a pedido de Kevin, se
        //     sacaron el polvo flotando, las luciérnagas, las chispas y el vapor.)
        Material drop = ParticleMaterial("ParticulaGota", "Gota.png");

        // lluvia fina en toda la ciudad (calle, central, puerto). Arranca en la
        // pared del complejo, no entra en la sala de control (tiene techo) y
        // se va apagando en tandas al llegar al parque, para que no haya una
        // "pared de lluvia" donde termina.
        //   {desde, hasta, densidad}
        float[][] rainZones = {
            new[] { 2.6f, 52.4f, 1f }, new[] { 61.5f, 84.5f, 1f },
            new[] { 84.5f, 86.5f, 0.6f }, new[] { 86.5f, 88.5f, 0.3f }, new[] { 88.5f, 90.5f, 0.12f } };
        Transform rains = Group(root, "Lluvia");
        foreach (float[] rz in rainZones)
        {
            float w = rz[1] - rz[0];
            ParticleSystem rain = Particles(rains, "Lluvia " + rz[0].ToString("F0"), new Vector2((rz[0] + rz[1]) / 2f, -2.5f), new Vector2(w, 9f), drop,
                      Mathf.CeilToInt(18f * w * rz[2]), 5.8f * w * rz[2],
                      new Vector2(0.7f, 0.9f), new Vector2(0.05f, 0.07f), new Color(0.75f, 0.75f, 1f, 0.24f), new Color(0.6f, 0.8f, 1f, 0.16f),
                      new Vector2(-1.4f, -9.5f), new Vector2(-1.1f, -8.5f), 0f, "Default", 10);
            ParticleSystemRenderer rr = rain.GetComponent<ParticleSystemRenderer>();
            rr.renderMode = ParticleSystemRenderMode.Stretch;
            rr.velocityScale = 0.025f;
            rr.lengthScale = 1f;
            ParticleSystem.ColorOverLifetimeModule rc = rain.colorOverLifetime;
            rc.enabled = false;
        }
    }

    // ------------------------------------------------------------ post-proceso

    /// <summary>
    /// Bloom suave (solo lo que pasa de blanco: neones, lámparas, la fuente) y
    /// una viñeta leve que oscurece las esquinas. Va en un Volume global de la
    /// escena y se prende en la cámara del Stage 2. La interfaz (corazones,
    /// terminal) no se ve afectada: son canvas superpuestos.
    /// </summary>
    public static void BuildPostFX(bool enable)
    {
        string path = "Assets/Settings/Stage2_PostFX.asset";
        UnityEngine.Rendering.VolumeProfile profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        UnityEngine.Rendering.Universal.Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        // (04/10/2026: el bloom hace brillar lo claro, y Kevin pidió sacar las
        //  luces: va apagado. La viñeta queda.)
        bloom.active = Lights;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.62f);
        bloom.tint.Override(new Color(1f, 0.92f, 1f, 1f));
        bloom.highQualityFiltering.Override(true);

        UnityEngine.Rendering.Universal.Vignette vig;
        if (!profile.TryGet(out vig))
        {
            vig = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vig.name = "Vignette";
            AssetDatabase.AddObjectToAsset(vig, profile);
        }
        vig.active = true;
        vig.intensity.Override(0.26f);
        vig.smoothness.Override(0.5f);
        vig.color.Override(new Color(0.05f, 0f, 0.09f, 1f));
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Transform root = Fresh("Visual_PostProceso");
        UnityEngine.Rendering.Volume vol = root.gameObject.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = profile;

        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data = Camera.main.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        data.renderPostProcessing = enable;
        EditorUtility.SetDirty(data);
    }

    // ------------------------------------------------------------ Kira

    /// <summary>
    /// Un halo cian suave que acompaña a Kira (hijo del objeto KIRA, sin
    /// tocar sus componentes): en los pasillos oscuros se lee como la luz del
    /// dron. Personaje/1, detrás del sprite de Kira (2).
    /// </summary>
    public static void BuildKiraGlow()
    {
        GameObject kira = GameObject.Find("KIRA");
        if (kira == null) return;
        Transform old = kira.transform.Find("BrilloKira");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        // (04/10/2026: a Kevin no le gustó la luz de Kira; con Lights en
        //  false solo se borra la que había)
        if (!Lights) return;
        Transform g = Group(kira.transform, "BrilloKira");
        g.localPosition = Vector3.zero;
        SpriteRenderer outer = Glow(g, 0f, 0f, 1.5f, 1.5f, A(new Color(0.35f, 0.9f, 1f), 0.16f), "Personaje", 1);
        outer.transform.localPosition = Vector3.zero;
        Flicker(outer, GlowFlicker.Mode.Pulse, 0.3f, 0.35f);
        SpriteRenderer inner = Glow(g, 0f, 0f, 0.5f, 0.5f, A(new Color(0.5f, 0.95f, 1f), 0.22f), "Personaje", 1);
        inner.transform.localPosition = new Vector3(0f, -0.02f, 0f);
    }

    // ------------------------------------------------------------ faroles

    /// <summary>
    /// Luz para todos los faroles de calle del mapa: busca la punta del farol
    /// (Props-01_69, o Props-01_76 en el farol largo) en todos los tilemaps y
    /// le pone halo, cono hacia abajo y la mancha de luz en el piso.
    /// </summary>
    public static int BuildLampGlows()
    {
        Transform root = Fresh("Visual_Faroles");
        int n = 0;
        foreach (Tilemap tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tm.name.StartsWith("Visual_")) continue;
            foreach (Vector3Int c in tm.cellBounds.allPositionsWithin)
            {
                TileBase t = tm.GetTile(c);
                if (t == null) continue;
                string k = MapTools.Key(t);
                float back;
                if (k == "Props-01/Props-01_69") back = 0.16f;
                else if (k == "Props-01/Props-01_76") back = 0.48f;
                else continue;
                Vector3 w = tm.GetCellCenterWorld(c);
                float cell = tm.cellSize.x * tm.transform.lossyScale.x;
                float x = w.x - back * cell / 0.32f, y = w.y - cell * 0.2f;
                Transform l = Group(root, "Farol " + (++n));
                SpriteRenderer halo = Glow(l, x, y, 1.4f, 1.0f, A(Warm, 0.32f), "Default", 10);
                Glow(l, x, y, 0.6f, 0.2f, A(new Color(1f, 0.88f, 0.6f), 0.6f), "Default", 11, "GlowNucleo.png");
                Glow(l, x, y - 0.02f, 1.7f, 1.45f, A(Warm, 0.15f), "Default", 10, "Cono.png");
                float floor = y - 1.3f;
                Glow(l, x, floor, 1.5f, 0.24f, A(Warm, 0.22f), "Default", 10);
                Flicker(halo, GlowFlicker.Mode.Lamp, 0.12f, 0.4f);
            }
        }
        return n;
    }
}
