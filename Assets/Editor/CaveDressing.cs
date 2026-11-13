using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// El decorado de la cueva del Stage 1 (SampleScene): cajas, afiches viejos,
/// una máquina expendedora, un banco, antenas. Todo con el pack Assets/Cueva,
/// que es el mismo con el que está hecho el nivel.
///
/// Es solo dibujo: nada de esto tiene collider ni script. Vive todo adentro
/// del objeto "Visual_Cueva" y se rehace con el menú
///   CodeBreak → Stage 1: rehacer decorado de la cueva
/// (borra lo que haya y lo arma de nuevo). Para mover, sacar o agregar un
/// adorno, se cambia la lista de Build() y se vuelve a correr: NO retocar a
/// mano adentro de "Visual_Cueva", rehacer lo pisa.
///
/// Sin luces, brillos ni partículas, igual que en el Stage 2.
/// </summary>
public static class CaveDressing
{
    public const string Root = "Visual_Cueva";
    public const string Dir = "Assets/ASSETS/Stage1/Generado/";

    const string Deco = "Assets/Cueva/tileset/decorations.png";
    const string Main = "Assets/Cueva/tileset/main_tileset.png";

    const float Scale = 2f;     // el mismo de los tilemaps del nivel
    const float Pixel = 0.02f;  // un píxel del arte, en el mundo (100 ppu x 2)

    // La altura del piso de cada tramo (donde se apoyan las cosas).
    const float FloorStart = -0.64f;  // sala del ascensor y primer pasillo
    const float FloorUpper = 0f;      // pasillo de arriba
    const float FloorLower = -1.6f;   // pasillo de abajo (el del dron)
    const float FloorExit = -1.92f;   // sala de la salida

    // Un poco apagados: son fondo, no tienen que competir con el jugador.
    static readonly Color PropTint = new Color(0.80f, 0.84f, 0.88f, 1f);
    static readonly Color PosterTint = new Color(0.62f, 0.66f, 0.70f, 1f);

    /// <summary>El color del "vacío" de la cueva: el del relleno oscuro del tileset.</summary>
    static readonly Color32 VoidColor = new Color32(15, 36, 43, 255);

    [MenuItem("CodeBreak/Stage 1: rehacer decorado de la cueva")]
    public static void BuildMenu()
    {
        Debug.Log(Build());
    }

    public static string Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "SampleScene") return "CaveDressing: hay que tener abierta SampleScene.";
        if (Application.isPlaying) return "CaveDressing: no en Play.";

        GameObject old = GameObject.Find(Root);
        if (old != null) Object.DestroyImmediate(old);

        Transform root = new GameObject(Root).transform;

        // Los afiches vienen de a dos en la hoja y las antenas pegadas: se
        // recortan sueltos.  {x, y, ancho, alto} en píxeles, desde abajo.
        Sprite posterFood = Cut(Deco, "Afiche_Comida", 83, 80, 26, 32);
        Sprite posterCoffee = Cut(Deco, "Afiche_Cafe", 115, 112, 26, 32);
        Sprite posterBored = Cut(Deco, "Afiche_Aburrido", 115, 80, 26, 32);
        Sprite antennaLow = Cut(Main, "Antena_Baja", 48, 16, 16, 32);
        Sprite antennaTall = Cut(Main, "Antena_Alta", 64, 16, 16, 44);

        Sprite box1 = MapDressing.Sub(Deco, "decorations_30");
        Sprite box2 = MapDressing.Sub(Deco, "decorations_31");
        Sprite box3 = MapDressing.Sub(Deco, "decorations_32");
        Sprite vending = MapDressing.Sub(Deco, "decorations_10");
        Sprite bench = MapDressing.Sub(Deco, "decorations_14");
        Sprite table = MapDressing.Sub(Deco, "decorations_13");
        Sprite exitSign = MapDressing.Sub(Deco, "decorations_28");

        // ---- Sala del ascensor (donde arranca el jugador) ----
        Transform a = Group(root, "1 Sala del ascensor");
        Put(a, "Caja", box1, 0.36f, FloorStart, PropTint);
        Put(a, "Caja", box2, 0.70f, FloorStart, PropTint);
        Put(a, "Caja arriba", box3, 0.52f, FloorStart + 0.22f, PropTint);

        // ---- Primer pasillo (entre el pozo de Kira y la escalera) ----
        Transform b = Group(root, "2 Primer pasillo");
        Put(b, "Antena", antennaTall, -6.56f, FloorStart, PropTint);
        Put(b, "Afiche comida", posterFood, -5.74f, FloorStart + 0.52f, PosterTint);

        // ---- Pasillo de arriba (donde Kira entrega el libro) ----
        Transform c = Group(root, "3 Pasillo de arriba");
        Put(c, "Expendedora", vending, -8.86f, FloorUpper, PropTint);
        Put(c, "Banco", bench, -10.90f, FloorUpper, PropTint);
        Put(c, "Afiche cafe", posterCoffee, -10.90f, FloorUpper + 0.56f, PosterTint);
        Put(c, "Caja", box3, -12.56f, FloorUpper, PropTint);
        Put(c, "Caja", box1, -12.90f, FloorUpper, PropTint);

        // ---- Pasillo de abajo (el del dron) ----
        Transform d = Group(root, "4 Pasillo de abajo");
        Put(d, "Antena", antennaLow, -15.72f, FloorLower, PropTint);
        Put(d, "Mesa", table, -21.44f, FloorLower, PropTint);
        Put(d, "Afiche aburrido", posterBored, -22.40f, FloorLower + 0.52f, PosterTint);

        // ---- Sala de la salida ----
        Transform e = Group(root, "5 Sala de la salida");
        Put(e, "Cartel de salida", exitSign, -32.29f, FloorExit + 0.68f, Color.white);
        Put(e, "Caja", box2, -31.62f, FloorExit, PropTint);
        Put(e, "Caja", box1, -31.28f, FloorExit, PropTint);
        Put(e, "Caja arriba", box3, -31.46f, FloorExit + 0.22f, PropTint);

        // Más allá del relleno del nivel se veía el celeste de la cámara
        // (una franja a la derecha de la sala del ascensor). Con el mismo
        // color del relleno, la cueva sigue oscura hasta donde se mire.
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = VoidColor;
            EditorUtility.SetDirty(cam);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        return "CaveDressing: " + root.GetComponentsInChildren<SpriteRenderer>().Length + " adornos en " + Root + ".";
    }

    static Transform Group(Transform root, string name)
    {
        Transform t = new GameObject(name).transform;
        t.SetParent(root, false);
        return t;
    }

    /// <summary>
    /// Pone un sprite centrado en x, con la base apoyada en 'bottom'. Lo calza
    /// en la grilla de píxeles del arte para que no quede medio píxel corrido.
    /// </summary>
    static void Put(Transform parent, string name, Sprite sprite, float x, float bottom, Color tint)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 1;   // delante de la pared, detrás del jugador (capa Personaje)

        // El mismo material que los sprites que ya estaban en la escena.
        GameObject reference = GameObject.Find("Decoo");
        SpriteRenderer refRenderer = reference != null ? reference.GetComponent<SpriteRenderer>() : null;
        if (refRenderer != null) sr.sharedMaterial = refRenderer.sharedMaterial;

        float unitsPerPixel = Scale / sprite.pixelsPerUnit;
        float width = sprite.rect.width * unitsPerPixel;
        float left = Mathf.Round((x - width * 0.5f) / Pixel) * Pixel;

        go.transform.position = new Vector3(
            left + sprite.pivot.x * unitsPerPixel,
            bottom + sprite.pivot.y * unitsPerPixel,
            0f);
        go.transform.localScale = new Vector3(Scale, Scale, 1f);
    }

    /// <summary>
    /// Un recorte de una hoja, guardado como sprite suelto en Dir (se crea la
    /// primera vez). No toca la hoja original ni cómo está cortada.
    /// </summary>
    static Sprite Cut(string sheet, string name, int x, int y, int w, int h)
    {
        string path = Dir + name + ".asset";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null) return s;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(sheet);
        if (tex == null) throw new System.Exception("CaveDressing: no está " + sheet);

        s = Sprite.Create(tex, new Rect(x, y, w, h), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect);
        s.name = name;

        if (!Directory.Exists(Dir))
        {
            Directory.CreateDirectory(Dir);
            AssetDatabase.Refresh();
        }
        AssetDatabase.CreateAsset(s, path);
        return s;
    }
}
