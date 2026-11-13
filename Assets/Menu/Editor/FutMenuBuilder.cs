using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using FutMenu;

// Menu: Menu > Build FUT27 Menu Scene  (crea Assets/Scenes/MainMenu.unity)
public static class FutMenuBuilder
{
    const string Gen = "Assets/Menu/Generated";
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    static Font font;

    [MenuItem("Menu/Build FUT27 Menu Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Menu] Sal de Play Mode primero."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(Gen);
        FixBackgroundImport();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Sprite logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Menu/Images/logo_fut27.png");
        Texture2D bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Menu/Images/fondo_estadio.jpg");
        if (logo == null || bg == null) { Debug.LogError("[Menu] Faltan el logo o el fondo en Assets/Menu/Images."); return; }

        // ---------- Texturas generadas ----------
        Sprite radial = MakeRadial();
        Sprite flare = MakeFlare();
        Sprite vigWorld = MakeVignetteWorld();
        Sprite vigIntro = MakeVignetteIntro();
        Sprite shadeTop = MakeShadeTop();
        Sprite tintBottom = MakeTintBottom();
        Sprite noise = MakeNoise();
        Sprite pill = MakePill();
        Sprite iconPencil = MakeIcon("icon_pencil", (x, y) =>
            InPoly(x, y, new float[] { 17f, 21f, 7.5f, 2f, 3.5f }, new float[] { 3f, 7f, 20.5f, 22f, 16.5f })
            || Dist(x, y, 19f, 5f) <= 2.83f);
        Sprite iconFriends = MakeIcon("icon_friends", (x, y) => FriendsShape(x, y));
        Sprite iconMusic = MakeIcon("icon_music", (x, y) =>
            DistSeg(x, y, 9f, 18f, 9f, 5f) <= 1.2f || DistSeg(x, y, 9f, 5f, 21f, 3f) <= 1.2f || DistSeg(x, y, 21f, 3f, 21f, 16f) <= 1.2f
            || Dist(x, y, 6f, 18f) <= 3.2f || Dist(x, y, 18f, 16f) <= 3.2f);

        // ---------- Escena ----------
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) Object.DestroyImmediate(l.gameObject);
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        var canvasGo = new GameObject("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasGo.transform;

        var menuGo = new GameObject("FutMenuController");
        FutMainMenu m = menuGo.AddComponent<FutMainMenu>();
        m.backgroundTexture = bg;

        // ---------- Mundo (fondo animado) ----------
        RectTransform world = NewRT("World", root);
        Stretch(world, 0.03f);
        world.pivot = new Vector2(0.6f, 0.4f);
        m.world = world;

        RectTransform stageRt = NewRT("Stage", world);
        Stretch(stageRt, 0f);
        RawImage stage = stageRt.gameObject.AddComponent<RawImage>();
        stage.texture = bg;
        stage.raycastTarget = false;
        m.stage = stage;

        Image noiseImg = NewImage("Noise", world, noise, new Color(1f, 1f, 1f, 0.07f));
        Stretch(noiseImg.rectTransform, 0f);
        noiseImg.type = Image.Type.Tiled;
        Stretch(NewImage("BottomTint", world, tintBottom, new Color(1f, 1f, 1f, 0.5f)).rectTransform, 0f);
        Stretch(NewImage("TopShade", world, shadeTop, Color.white).rectTransform, 0f);
        Stretch(NewImage("Vignette", world, vigWorld, Color.white).rectTransform, 0f);

        // Niebla de colores: contenedor + manchas (porcentajes del CSS original)
        RectTransform fogA = NewFog("FogA", world, -0.25f, 1.25f, -0.15f, 0.5f);
        Blob(fogA, radial, new Color(0.133f, 0.212f, 0.722f, 0.4f), 0.18f, 0.25f, 1532f, 540f);
        Blob(fogA, radial, new Color(0.110f, 0.498f, 0.659f, 0.4f), 0.52f, 0.12f, 1694f, 590f);
        Blob(fogA, radial, new Color(0.137f, 0.702f, 0.518f, 0.4f), 0.85f, 0.20f, 1613f, 590f);
        m.fogA = fogA;
        RectTransform fogB = NewFog("FogB", world, -0.25f, 1.25f, -0.20f, 0.35f);
        Blob(fogB, radial, new Color(0.122f, 0.624f, 0.690f, 0.3f), 0.30f, 0.15f, 1411f, 457f);
        Blob(fogB, radial, new Color(0.165f, 0.275f, 0.788f, 0.3f), 0.70f, 0.20f, 1613f, 499f);
        m.fogB = fogB;

        // ---------- Interfaz del menu ----------
        RectTransform ui = NewRT("UI", root);
        Stretch(ui, 0f);

        // Logo (arriba a la izquierda)
        RectTransform logoRt = NewRT("Logo", ui);
        SetAnchored(logoRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -14f), new Vector2(211f, 211f));
        Image logoImg = logoRt.gameObject.AddComponent<Image>();
        logoImg.sprite = logo;
        logoImg.preserveAspect = true;
        logoImg.color = Color.white;
        m.logoGroup = logoRt.gameObject.AddComponent<CanvasGroup>();
        AddButton(logoRt.gameObject, m, 200);

        // Iconos (arriba a la derecha)
        RectTransform iconsRt = NewRT("Icons", ui);
        SetAnchored(iconsRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-77f, -67f), new Vector2(404f, 96f));
        m.iconsGroup = iconsRt.gameObject.AddComponent<CanvasGroup>();
        Sprite[] icons = { iconPencil, iconFriends, iconMusic };
        string[] iconNames = { "IconEdit", "IconFriends", "IconMusic" };
        for (int i = 0; i < 3; i++)
        {
            RectTransform irt = NewRT(iconNames[i], iconsRt);
            SetAnchored(irt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 154f, 0f), new Vector2(96f, 96f));
            Image img = irt.gameObject.AddComponent<Image>();
            img.sprite = icons[i];
            img.color = new Color(0.925f, 0.929f, 0.929f, 1f);
            AddButton(irt.gameObject, m, 100 + i);
            if (i == 2) m.musicIcon = img;
        }

        // Opciones (3 columnas x 2 filas, abajo)
        string[] names = { "Modo carrera", "Amistosos", "Crear club", "Copa general", "Mercado de fichaje", "Entrenamiento" };
        m.itemNames = names;
        m.itemGroups = new CanvasGroup[6];
        m.itemRects = new RectTransform[6];
        m.itemTexts = new Text[6];
        m.underlines = new RectTransform[6];
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3;
            int row = i / 3;
            RectTransform item = NewRT("Item_" + i, ui);
            SetAnchored(item, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(77f + 589f * (col + 0.5f), row == 0 ? 298f : 153f), new Vector2(589f, 114f));
            Image hit = item.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            m.itemGroups[i] = item.gameObject.AddComponent<CanvasGroup>();
            AddButton(item.gameObject, m, i);

            Text label = NewText("Label", item, names[i].ToUpper(), 52, FontStyle.Bold, Color.white);
            Stretch(label.rectTransform, 0f);
            Shadow sh = label.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.65f);
            sh.effectDistance = new Vector2(0f, -3f);

            Image ul = NewImage("Underline", item, null, Color.white);
            SetAnchored(ul.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(0f, 6f));

            m.itemRects[i] = item;
            m.itemTexts[i] = label;
            m.underlines[i] = ul.rectTransform;
        }

        // Aviso (toast)
        RectTransform toastRt = NewRT("Toast", ui);
        SetAnchored(toastRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 260f), new Vector2(100f, 76f));
        Image toastBg = toastRt.gameObject.AddComponent<Image>();
        toastBg.sprite = pill;
        toastBg.type = Image.Type.Sliced;
        toastBg.color = new Color(0f, 0f, 0f, 0.62f);
        toastBg.raycastTarget = false;
        HorizontalLayoutGroup hlg = toastRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(44, 44, 16, 16);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        ContentSizeFitter csf = toastRt.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        m.toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
        m.toastGroup.alpha = 0f;
        m.toastGroup.blocksRaycasts = false;
        m.toastText = NewText("Text", toastRt, "Pantalla pendiente", 36, FontStyle.Normal, Color.white);

        // ---------- Intro (encima de todo) ----------
        RectTransform intro = NewRT("Intro", root);
        Stretch(intro, 0f);
        Image introBg = intro.gameObject.AddComponent<Image>();
        introBg.color = Color.black;
        introBg.raycastTarget = true;
        m.introGroup = intro.gameObject.AddComponent<CanvasGroup>();

        RectTransform picRt = NewRT("Pic", intro);
        Stretch(picRt, 0.03f);
        RawImage pic = picRt.gameObject.AddComponent<RawImage>();
        pic.texture = bg;
        pic.color = Color.black;
        pic.raycastTarget = false;
        m.introPic = pic;

        float[] fx = { 0.12f, 0.88f, 0.32f, 0.68f };
        float[] fy = { 0.64f, 0.64f, 0.70f, 0.70f };
        m.flares = new Image[4];
        for (int i = 0; i < 4; i++)
        {
            Image f = NewImage("Flare" + i, intro, flare, new Color(1f, 0.98f, 0.91f, 0f));
            SetAnchored(f.rectTransform, new Vector2(fx[i], fy[i]), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(883f, 883f));
            m.flares[i] = f;
        }

        Image vig = NewImage("IntroVignette", intro, vigIntro, new Color(1f, 1f, 1f, 0f));
        Stretch(vig.rectTransform, 0f);
        m.introVignette = vig;

        RectTransform introLogo = NewRT("IntroLogo", intro);
        SetAnchored(introLogo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(583f, 583f));
        Image il = introLogo.gameObject.AddComponent<Image>();
        il.sprite = logo;
        il.preserveAspect = true;
        il.raycastTarget = false;
        m.introLogoGroup = introLogo.gameObject.AddComponent<CanvasGroup>();
        m.introLogo = introLogo;

        Image line = NewImage("FlareLine", intro, flare, new Color(1f, 1f, 1f, 0f));
        SetAnchored(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600f, 8f));
        m.flareLine = line;
        m.flareLineRect = line.rectTransform;

        RectTransform gate = NewRT("Gate", intro);
        SetAnchored(gate, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 200f));
        m.gateGroup = gate.gameObject.AddComponent<CanvasGroup>();
        Text gt = NewText("Title", gate, "PULSA PARA EMPEZAR", 56, FontStyle.Bold, new Color(0.914f, 0.945f, 0.925f));
        SetAnchored(gt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 25f), new Vector2(1400f, 80f));
        Text gs = NewText("Subtitle", gate, "Toca la pantalla, pulsa una tecla o un botón del mando", 26, FontStyle.Normal, new Color(0.624f, 0.702f, 0.659f));
        SetAnchored(gs.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1400f, 50f));

        Text hint = NewText("Hint", intro, "Toca o pulsa cualquier tecla para saltar", 24, FontStyle.Normal, new Color(0.765f, 0.816f, 0.784f));
        SetAnchored(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(1400f, 40f));
        m.hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

        // ---------- Audio, EventSystem y guardado ----------
        m.clipIntro = LoadClip("intro_focos_y_logo");
        m.clipMenuAppears = LoadClip("menu_aparece");
        m.clipMove = LoadClip("ui_mover");
        m.clipConfirm = LoadClip("ui_confirmar");
        m.clipAmbient = LoadClip("ambiente_estadio_bucle");

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[Menu] Escena creada: " + ScenePath + " (es la unica escena del Build). Pulsa Play.");
    }

    // ---------------- Ayudas de UI ----------------

    static AudioClip LoadClip(string n)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Menu/Audio/" + n + ".wav");
    }

    static void AddButton(GameObject go, FutMainMenu owner, int id)
    {
        FutMenuButton b = go.AddComponent<FutMenuButton>();
        b.owner = owner;
        b.id = id;
        Graphic g = go.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;
    }

    static RectTransform NewRT(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = new Vector2(-inset, -inset);
        rt.anchorMax = new Vector2(1f + inset, 1f + inset);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Image NewImage(string name, Transform parent, Sprite sprite, Color color)
    {
        RectTransform rt = NewRT(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Text NewText(string name, Transform parent, string text, int size, FontStyle style, Color color)
    {
        RectTransform rt = NewRT(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static RectTransform NewFog(string name, Transform parent, float x0, float x1, float y0, float y1)
    {
        RectTransform rt = NewRT(name, parent);
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    static void Blob(RectTransform parent, Sprite sprite, Color color, float ax, float ay, float w, float h)
    {
        Image img = NewImage("Blob", parent, sprite, color);
        SetAnchored(img.rectTransform, new Vector2(ax, ay), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
    }

    // ---------------- Importacion ----------------

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static void FixBackgroundImport()
    {
        var ti = AssetImporter.GetAtPath("Assets/Menu/Images/fondo_estadio.jpg") as TextureImporter;
        if (ti == null) return;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.maxTextureSize = 2048;
        ti.SaveAndReimport();
    }

    static Sprite SaveSprite(string name, Color32[] px, int w, int h, bool repeat, float border)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        string path = Gen + "/" + name + ".png";
        File.WriteAllBytes(Path.Combine(Application.dataPath, path.Substring(7)), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsPerUnit = 100f;
        if (border > 0f) ti.spriteBorder = new Vector4(border, border, border, border);
        var ts = new TextureImporterSettings();
        ti.ReadTextureSettings(ts);
        ts.spriteMeshType = SpriteMeshType.FullRect;
        ti.SetTextureSettings(ts);
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------------- Texturas generadas ----------------

    static Color32 C(float r, float g, float b, float a)
    {
        return new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(r) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(g) * 255f),
                           (byte)Mathf.RoundToInt(Mathf.Clamp01(b) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
    }

    static Sprite MakeRadial()
    {
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f;
                float dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = r >= 1f ? 0f : Mathf.Pow(1f - r, 1.6f);
                px[y * S + x] = C(1f, 1f, 1f, a);
            }
        return SaveSprite("radial", px, S, S, false, 0f);
    }

    // radial-gradient(closest-side, .95 -> .25 al 40% -> 0)
    static Sprite MakeFlare()
    {
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f;
                float dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = r >= 1f ? 0f : (r < 0.4f ? Mathf.Lerp(0.95f, 0.25f, r / 0.4f) : Mathf.Lerp(0.25f, 0f, (r - 0.4f) / 0.6f));
                px[y * S + x] = C(1f, 1f, 1f, a);
            }
        return SaveSprite("flare", px, S, S, false, 0f);
    }

    // ellipse at 50% 55%: transparente hasta 55%, negro .4 al 100%
    static Sprite MakeVignetteWorld()
    {
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = ((x + 0.5f) / S - 0.5f) * 2f;
                float dy = ((y + 0.5f) / S - 0.45f) * 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                float a = 0.4f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - 0.55f) / 0.45f));
                px[y * S + x] = C(0f, 0f, 0f, a);
            }
        return SaveSprite("vignette_world", px, S, S, false, 0f);
    }

    // radial: .6 en el centro, .25 al 38%, .7 en el borde
    static Sprite MakeVignetteIntro()
    {
        const int S = 256;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = ((x + 0.5f) / S - 0.5f) * 2f;
                float dy = ((y + 0.5f) / S - 0.5f) * 2f;
                float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 1.414f);
                float a = r < 0.38f ? Mathf.Lerp(0.6f, 0.25f, r / 0.38f) : Mathf.Lerp(0.25f, 0.7f, (r - 0.38f) / 0.62f);
                px[y * S + x] = C(0f, 0f, 0f, a);
            }
        return SaveSprite("vignette_intro", px, S, S, false, 0f);
    }

    // linear-gradient(to bottom, rgba(4,10,18,.6), transparent 30%)
    static Sprite MakeShadeTop()
    {
        const int W = 4, H = 128;
        var px = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            float fromTop = 1f - (y + 0.5f) / H;
            float a = 0.6f * (1f - Mathf.Clamp01(fromTop / 0.3f));
            for (int x = 0; x < W; x++) px[y * W + x] = C(4f / 255f, 10f / 255f, 18f / 255f, a);
        }
        return SaveSprite("shade_top", px, W, H, false, 0f);
    }

    // degradado azul -> turquesa -> verde, visible solo en el 42% inferior
    static Sprite MakeTintBottom()
    {
        const int W = 128, H = 128;
        var px = new Color32[W * H];
        Color c0 = new Color(0.102f, 0.165f, 0.471f);
        Color c1 = new Color(0.102f, 0.314f, 0.471f);
        Color c2 = new Color(0.122f, 0.549f, 0.416f);
        for (int y = 0; y < H; y++)
        {
            float v = (y + 0.5f) / H;
            float a = 1f - Mathf.Clamp01(v / 0.42f);
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                Color c = u < 0.48f ? Color.Lerp(c0, c1, u / 0.48f) : Color.Lerp(c1, c2, (u - 0.48f) / 0.52f);
                px[y * W + x] = C(c.r, c.g, c.b, a);
            }
        }
        return SaveSprite("tint_bottom", px, W, H, false, 0f);
    }

    static Sprite MakeNoise()
    {
        const int S = 128;
        var px = new Color32[S * S];
        Random.InitState(27);
        for (int i = 0; i < px.Length; i++)
        {
            float g = Random.value;
            px[i] = C(g, g, g, 1f);
        }
        return SaveSprite("noise", px, S, S, true, 0f);
    }

    // capsula redondeada para el aviso (9-slice)
    static Sprite MakePill()
    {
        const int S = 64;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - S / 2f;
                float dy = y + 0.5f - S / 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * S + x] = C(1f, 1f, 1f, Mathf.Clamp01(S / 2f - d));
            }
        return SaveSprite("pill", px, S, S, false, 30f);
    }

    // ---------------- Iconos (formas del SVG original, viewBox 24x24) ----------------

    static Sprite MakeIcon(string name, System.Func<float, float, bool> inside)
    {
        const int S = 128;
        const int SS = 3;
        const float V = 24f;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                    {
                        float ux = (x + (sx + 0.5f) / SS) / S * V;
                        float uy = (1f - (y + (sy + 0.5f) / SS) / S) * V;
                        if (inside(ux, uy)) hit++;
                    }
                px[y * S + x] = C(1f, 1f, 1f, hit / (float)(SS * SS));
            }
        return SaveSprite(name, px, S, S, false, 0f);
    }

    static bool FriendsShape(float x, float y)
    {
        if (Dist(x, y, 9f, 7.5f) <= 4.2f) return true;
        if (x >= 1.5f && x <= 16.5f && y >= 14f && y <= 20.5f)
        {
            if (y < 18.8f)
            {
                if (x < 6.3f) return Dist(x, y, 6.3f, 18.8f) <= 4.8f;
                if (x > 11.7f) return Dist(x, y, 11.7f, 18.8f) <= 4.8f;
            }
            return true;
        }
        return DistSeg(x, y, 19f, 7.5f, 19f, 13.5f) <= 1.1f || DistSeg(x, y, 16f, 10.5f, 22f, 10.5f) <= 1.1f;
    }

    static float Dist(float x, float y, float cx, float cy)
    {
        float dx = x - cx;
        float dy = y - cy;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    static float DistSeg(float x, float y, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
        float cx = ax + t * dx - x;
        float cy = ay + t * dy - y;
        return Mathf.Sqrt(cx * cx + cy * cy);
    }

    static bool InPoly(float x, float y, float[] px, float[] py)
    {
        bool c = false;
        for (int i = 0, j = px.Length - 1; i < px.Length; j = i++)
        {
            if (((py[i] > y) != (py[j] > y)) && (x < (px[j] - px[i]) * (y - py[i]) / (py[j] - py[i]) + px[i])) c = !c;
        }
        return c;
    }
}
