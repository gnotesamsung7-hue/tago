using UnityEngine;

// All graphics are painted pixel by pixel in code, so the project needs no image files.
// Side-view sprites face RIGHT; the game flips them to face left.
public static class Art
{
    public const float PPU = 20f;
    public static Sprite px1, drum, crates, bin, stall, barrier, dog, food, cash, jeep, ejeep, car, moto, bus, pole, wire, ring, pebble;
    public static Sprite[] houses, trikes;
    public static Sprite markHouse, store, terminal, skyline;
    public static Sprite[] player, goon, boss;
    public static Sprite[][] npcs;
    public static Texture2D vignette, white;

    public static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString(hex, out c); return c; }
    public static Color A(string hex, float a) { var c = C(hex); c.a = a; return c; }
    static System.Random rng = new System.Random(11);
    static float R() { return (float)rng.NextDouble(); }

    class Pix
    {
        public int w, h; public Color[] px;
        public Pix(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0); }
        public void Set(int x, int y, Color c) // y counted from the TOP
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = (h - 1 - y) * w + x; px[i] = Blend(px[i], c);
        }
        public void Rect(int x, int y, int rw, int rh, Color c) { for (int j = y; j < y + rh; j++) for (int i = x; i < x + rw; i++) Set(i, j, c); }
        public void Fill(Color c) { Rect(0, 0, w, h, c); }
        public void Circle(float cx, float cy, float r, Color c)
        {
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            { float dx = x + .5f - cx, dy = y + .5f - cy; if (dx * dx + dy * dy <= r * r) Set(x, y, c); }
        }
        public void RingShape(float cx, float cy, float r, float t, Color c)
        {
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            { float d = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy)); if (d <= r && d >= r - t) Set(x, y, c); }
        }
        public Texture2D Tex(bool smooth = false)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
            t.SetPixels(px); t.Apply(); return t;
        }
        // default pivot = bottom centre (feet / wheels on the ground)
        public Sprite Spr(float ppu = PPU, float pvx = .5f, float pvy = 0f, bool smooth = false)
        { return Sprite.Create(Tex(smooth), new Rect(0, 0, w, h), new Vector2(pvx, pvy), ppu); }
    }

    static Color Blend(Color a, Color b)
    {
        float ao = b.a + a.a * (1 - b.a);
        if (ao <= 0f) return new Color(0, 0, 0, 0);
        return new Color((b.r * b.a + a.r * a.a * (1 - b.a)) / ao, (b.g * b.a + a.g * a.a * (1 - b.a)) / ao, (b.b * b.a + a.b * a.a * (1 - b.a)) / ao, ao);
    }

    public static void Init()
    {
        var wp = new Pix(2, 2); wp.Fill(Color.white); white = wp.Tex();
        var p1 = new Pix(1, 1); p1.Fill(Color.white); px1 = p1.Spr(1f, 0f, 0f);

        // ---- people ----
        Color skin = C("#8B5E3C"), skin2 = C("#A8754B"), hairC = C("#1B1612");
        player = Person(C("#ECECEC"), C("#2F5DA8"), hairC, skin, 3, false);
        goon = Person(C("#2A2A2A"), C("#3B3B46"), hairC, skin, 1, false);
        boss = Person(C("#E8E1C8"), C("#1E1E1E"), hairC, skin2, 2, false);
        string[] shirts = { "#C9473F", "#E0A33A", "#3FB8AF", "#7E57C2", "#F0ECE0", "#4F9A5B", "#E8467C", "#5C9AD6" };
        npcs = new Sprite[8][];
        for (int i = 0; i < 8; i++) npcs[i] = Person(C(shirts[i]), R() > .5f ? C("#2F3B52") : C("#5A4A3A"), hairC, R() > .5f ? skin : skin2, 0, i % 3 == 1);

        // ---- cover objects ----
        var d = new Pix(14, 20); d.Rect(1, 2, 12, 18, C("#2F6FB0")); d.Rect(1, 1, 12, 2, C("#5C9AD6")); d.Rect(1, 7, 12, 1, C("#5C9AD6")); d.Rect(1, 14, 12, 1, C("#5C9AD6")); d.Rect(3, 3, 2, 15, A("#FFFFFF", .15f)); drum = d.Spr();
        var cr = new Pix(20, 18); cr.Rect(1, 9, 18, 9, C("#C9473F")); cr.Rect(2, 1, 16, 8, C("#E0A33A"));
        for (int i = 0; i < 4; i++) { cr.Rect(3 + i * 4, 11, 2, 2, C("#6E1F1A")); cr.Rect(4 + i * 4, 3, 2, 2, C("#8A5A12")); }
        crates = cr.Spr();
        var b = new Pix(14, 18); b.Rect(1, 3, 12, 15, C("#2E7D4F")); b.Rect(0, 1, 14, 3, C("#3E9A63")); b.Rect(4, 7, 6, 1, A("#000000", .3f)); bin = b.Spr();
        var st = new Pix(40, 36);
        for (int i = 0; i < 10; i++) st.Rect(i * 4, 0, 4, 4, i % 2 == 0 ? C("#C9473F") : C("#F0ECE0"));
        st.Rect(2, 4, 2, 18, C("#5E4630")); st.Rect(36, 4, 2, 18, C("#5E4630"));
        string[] goods = { "#E0A33A", "#3FB8AF", "#E8467C", "#F2C14E", "#4F9A5B", "#5C9AD6" };
        for (int i = 0; i < 8; i++) { st.Rect(5 + i * 4, 6, 3, 5, C(goods[i % 6])); st.Rect(5 + i * 4, 13, 3, 4, C(goods[(i + 3) % 6])); }
        st.Rect(1, 20, 38, 15, C("#7A5634")); st.Rect(1, 20, 38, 2, C("#9A6E44")); st.Rect(4, 34, 4, 2, C("#2A2A2A")); st.Rect(32, 34, 4, 2, C("#2A2A2A"));
        stall = st.Spr();
        var br = new Pix(40, 18); br.Rect(3, 4, 2, 14, C("#5A5A5A")); br.Rect(35, 4, 2, 14, C("#5A5A5A"));
        for (int i = 0; i < 10; i++) { br.Rect(i * 4, 3, 4, 5, i % 2 == 0 ? C("#F2C14E") : C("#1E1E1E")); br.Rect(i * 4, 10, 4, 3, i % 2 == 1 ? C("#F2C14E") : C("#1E1E1E")); }
        barrier = br.Spr();

        var dg = new Pix(20, 14); dg.Rect(4, 4, 12, 6, C("#8A6A45")); dg.Rect(14, 1, 5, 5, C("#8A6A45")); dg.Rect(15, 0, 2, 2, C("#5E4630"));
        dg.Rect(5, 10, 2, 4, C("#8A6A45")); dg.Rect(13, 10, 2, 4, C("#8A6A45")); dg.Rect(1, 3, 3, 2, C("#8A6A45")); dg.Set(17, 2, C("#1B1612")); dog = dg.Spr();
        var f = new Pix(8, 9); f.Rect(1, 1, 6, 8, C("#C8A16B")); f.Rect(1, 1, 6, 1, C("#A67F4E")); f.Rect(3, 4, 2, 2, C("#E0913A")); food = f.Spr();
        var cs = new Pix(10, 6); cs.Rect(0, 0, 10, 6, C("#4F9A5B")); cs.Rect(4, 1, 2, 4, C("#B9E2A8")); cash = cs.Spr();

        // ---- vehicles ----
        jeep = Jeepney(); ejeep = EJeep(); car = Car(C("#7A1F2B")); moto = Moto(); bus = Bus();
        trikes = new[] { Trike(C("#2F5DA8")), Trike(C("#C9473F")), Trike(C("#4F9A5B")) };

        // ---- buildings & sky ----
        houses = new Sprite[6];
        string[][] hc = {
            new[]{"#C9B79C","#E3D3B4","#6E7B85"}, new[]{"#8FB3A8","#B5D1C6","#7A6E6A"}, new[]{"#D9A07A","#E8C2A2","#6E7B85"},
            new[]{"#A9A0C8","#C9C2E0","#5E6670"}, new[]{"#D8C66E","#E9DC9C","#7A6E6A"}, new[]{"#B0B6BC","#CCD1D6","#5E6670"} };
        for (int i = 0; i < 6; i++) houses[i] = House(C(hc[i][0]), C(hc[i][1]), C(hc[i][2]), i, false, false);
        markHouse = House(C("#5C86B8"), C("#86A8D2"), C("#5E6670"), 9, false, true);
        store = House(C("#E0D2B0"), C("#EEE2C4"), C("#6E7B85"), 4, true, false);
        terminal = Terminal(); skyline = Skyline();

        var po = new Pix(6, 110); po.Rect(2, 0, 3, 110, C("#4A4038")); po.Rect(0, 6, 6, 2, C("#3A3230")); po.Rect(0, 14, 6, 10, C("#6A6E70")); po.Rect(1, 16, 4, 1, C("#8A8E90")); pole = po.Spr();
        var wi = new Pix(100, 26);
        float[] base0 = { 3, 7, 11 };
        for (int k = 0; k < 3; k++)
            for (int x = 0; x < 100; x++) { float t = (x - 50f) / 50f; int y = (int)(base0[k] + (9 - k * 2) * (1 - t * t)); wi.Set(x, y, A("#0C0B12", .9f)); }
        wi.RingShape(30, 14, 3, 1, A("#0C0B12", .85f)); wi.RingShape(70, 16, 2.5f, 1, A("#0C0B12", .85f)); wire = wi.Spr();

        var rg = new Pix(64, 64); rg.RingShape(32, 32, 32, 2.5f, Color.white); ring = rg.Spr(32f, .5f, .5f, true);
        var pb = new Pix(6, 6); pb.Circle(3, 3, 2.5f, C("#D8D8D8")); pebble = pb.Spr(PPU, .5f, .5f);

        int V = 128; var vg = new Pix(V, V);
        for (int y = 0; y < V; y++) for (int x = 0; x < V; x++)
        {
            float r = Mathf.Sqrt((x - V / 2f) * (x - V / 2f) + (y - V / 2f) * (y - V / 2f)) / (V / 2f);
            vg.Set(x, y, new Color(.04f, .03f, .1f, Mathf.Clamp01((r - .5f) / .7f) * .75f));
        }
        vignette = vg.Tex(true);
    }

    // frames: 0 stand, 1 walk, 2 crouch.  style 0 civilian, 1 goon (cap), 2 boss (shades + chain), 3 player (sando + backpack)
    static Sprite[] Person(Color shirt, Color pants, Color hair, Color skin, int style, bool longHair)
    {
        var fr = new Sprite[3];
        for (int f = 0; f < 3; f++)
        {
            var p = new Pix(16, 32); bool cr = f == 2; int oy = cr ? 9 : 0, ox = cr ? 1 : 0;
            Color dark = C("#1B1612");
            // head
            p.Rect(5 + ox, 2 + oy, 6, 7, skin); p.Rect(5 + ox, 1 + oy, 6, 3, hair); p.Rect(5 + ox, 3 + oy, 2, longHair ? 7 : 3, hair);
            p.Set(9 + ox, 5 + oy, dark);
            if (style == 1) { p.Rect(4 + ox, 0 + oy, 8, 3, C("#8E2B2B")); p.Rect(11 + ox, 2 + oy, 3, 1, C("#8E2B2B")); }
            if (style == 2) { p.Rect(8 + ox, 5 + oy, 4, 1, dark); p.Rect(5 + ox, 1 + oy, 6, 2, C("#3A3A3A")); }
            p.Rect(7 + ox, 9 + oy, 2, 1, skin);
            // torso
            int th = cr ? 7 : 10;
            if (style == 2) p.Rect(3 + ox, 10 + oy, 10, th, shirt); else p.Rect(4 + ox, 10 + oy, 8, th, shirt);
            if (style == 3) { p.Rect(2 + ox - 1, 11 + oy, 3, cr ? 6 : 8, C("#6B4E2E")); }
            if (style == 2) p.Rect(6 + ox, 10 + oy, 5, 1, C("#E2B93B"));
            Color arm = style == 3 ? skin : shirt;
            p.Rect(8 + ox + (cr ? 1 : 0), 11 + oy, 2, cr ? 5 : 7, arm); p.Rect(8 + ox + (cr ? 1 : 0), (cr ? 16 : 18) + oy, 2, 2, skin);
            // legs
            if (!cr)
            {
                p.Rect(4, 20, 8, 3, pants);
                if (f == 0) { p.Rect(5, 23, 3, 7, pants); p.Rect(8, 23, 3, 7, pants); p.Rect(5, 30, 4, 2, dark); p.Rect(8, 30, 4, 2, dark); }
                else { p.Rect(3, 23, 3, 7, pants); p.Rect(10, 23, 3, 7, pants); p.Rect(2, 30, 4, 2, dark); p.Rect(10, 30, 4, 2, dark); }
            }
            else
            {
                p.Rect(4, 26, 9, 2, pants); p.Rect(4, 28, 4, 2, pants); p.Rect(9, 27, 3, 3, pants);
                p.Rect(3, 30, 5, 2, dark); p.Rect(10, 30, 4, 2, dark);
            }
            fr[f] = p.Spr();
        }
        return fr;
    }

    static void Wheel(Pix p, float cx, float cy, float r)
    { p.Circle(cx, cy, r, C("#1A1A1E")); p.Circle(cx, cy, r * .45f, C("#9A9EA6")); }

    static Sprite Jeepney()
    {
        var p = new Pix(64, 30);
        p.Rect(4, 4, 52, 4, C("#8E2B2B")); p.Rect(2, 8, 54, 16, C("#C9CDD3"));
        p.Rect(8, 9, 42, 5, C("#1C1A24")); for (int i = 0; i < 6; i++) p.Rect(8 + i * 8, 9, 1, 5, C("#C9CDD3"));
        p.Rect(2, 15, 54, 2, C("#C9473F")); p.Rect(2, 18, 54, 1, C("#F2C14E")); p.Rect(2, 20, 54, 1, C("#2F6FB0"));
        p.Rect(54, 11, 9, 13, C("#B8BCC4")); p.Rect(52, 5, 5, 7, C("#7FB8D6")); p.Rect(58, 2, 2, 3, C("#E0E0E0")); p.Rect(62, 17, 1, 2, C("#FFF2B0"));
        p.Rect(2, 7, 2, 16, C("#3A3A40"));
        Wheel(p, 13, 25, 4.5f); Wheel(p, 50, 25, 4.5f);
        return p.Spr();
    }
    static Sprite EJeep()
    {
        var p = new Pix(64, 30);
        p.Rect(2, 3, 60, 21, C("#E8E8E0")); p.Rect(6, 6, 46, 8, C("#2A3340")); for (int i = 0; i < 5; i++) p.Rect(6 + i * 10, 6, 1, 8, C("#E8E8E0"));
        p.Rect(2, 16, 60, 3, C("#2E8B57")); p.Rect(54, 5, 8, 11, C("#7FB8D6")); p.Rect(61, 19, 1, 2, C("#FFF2B0"));
        Wheel(p, 13, 25, 4.5f); Wheel(p, 51, 25, 4.5f);
        return p.Spr();
    }
    static Sprite Car(Color body)
    {
        var p = new Pix(46, 22);
        p.Rect(1, 9, 44, 8, body); p.Rect(10, 3, 24, 7, body); p.Rect(12, 4, 9, 5, C("#3A4A5C")); p.Rect(23, 4, 9, 5, C("#3A4A5C"));
        p.Rect(44, 11, 1, 2, C("#FFF2B0")); p.Rect(1, 11, 1, 2, C("#E8467C"));
        Wheel(p, 10, 18, 3.8f); Wheel(p, 36, 18, 3.8f);
        return p.Spr();
    }
    static Sprite Moto()
    {
        var p = new Pix(24, 24);
        p.Rect(5, 14, 14, 4, C("#C9473F")); p.Rect(9, 4, 5, 9, C("#3FB8AF")); p.Rect(9, 0, 5, 4, C("#E8E1C8")); p.Rect(12, 9, 6, 2, C("#3FB8AF"));
        p.Rect(18, 10, 2, 5, C("#5A5A5A"));
        Wheel(p, 5, 19, 4); Wheel(p, 19, 19, 4);
        return p.Spr();
    }
    static Sprite Trike(Color side)
    {
        var p = new Pix(34, 28);
        p.Rect(0, 4, 21, 3, C("#3A3A40")); p.Rect(1, 7, 19, 14, side); p.Rect(4, 9, 11, 6, C("#1C1A24")); p.Rect(1, 17, 19, 1, C("#F2C14E"));
        p.Rect(20, 15, 10, 5, C("#2A2A2A")); p.Rect(22, 6, 5, 9, C("#E0A33A")); p.Rect(22, 2, 5, 4, skinOf());
        Wheel(p, 8, 23, 4); Wheel(p, 28, 23, 4);
        return p.Spr();
    }
    static Color skinOf() { return C("#8B5E3C"); }
    static Sprite Bus()
    {
        var p = new Pix(100, 44);
        p.Rect(1, 3, 98, 33, C("#E9E6DD")); p.Rect(5, 7, 82, 13, C("#24303F")); for (int i = 0; i < 8; i++) p.Rect(5 + i * 10, 7, 1, 13, C("#E9E6DD"));
        p.Rect(1, 24, 98, 3, C("#2A5DA8")); p.Rect(1, 28, 98, 2, C("#D6A12B"));
        p.Rect(90, 5, 8, 18, C("#7FB8D6")); p.Rect(80, 12, 8, 22, C("#BDB8AC")); p.Rect(98, 30, 1, 3, C("#FFF2B0"));
        Wheel(p, 18, 37, 6); Wheel(p, 78, 37, 6);
        return p.Spr();
    }

    static Sprite House(Color wall, Color wall2, Color roof, int variant, bool isStore, bool gate)
    {
        var p = new Pix(100, 80);
        p.Rect(0, 3, 100, 37, wall2); p.Rect(0, 40, 100, 40, wall);
        for (int x = 0; x < 100; x += 3) p.Rect(x, 0, 2, 4, roof);
        p.Rect(0, 38, 100, 2, A("#000000", .2f));
        for (int k = 0; k < 2; k++)
        {
            int wx = k == 0 ? 12 : 64; bool lit = R() > .45f;
            p.Rect(wx, 12, 24, 17, lit ? C("#F5C76B") : C("#2A2838"));
            if (lit && R() > .6f) p.Rect(wx, 12, 24, 17, A("#3FB8AF", .5f));
            for (int i = 0; i <= 24; i += 4) p.Rect(wx + i, 12, 1, 17, C("#2A2A2A"));
            p.Rect(wx, 20, 24, 1, C("#2A2A2A"));
        }
        if (R() > .5f) p.Rect(42, 20, 12, 8, C("#CFCFCF"));
        if (!isStore && variant % 3 == 0)
        { Color[] tc = { C("#E8467C"), C("#3FB8AF"), C("#F2C14E") }; p.Rect(10, 30, 32, 7, tc[variant % 3 == 0 ? (variant / 3) % 3 : 0]); p.Rect(12, 32, 28, 1, A("#FFFFFF", .7f)); p.Rect(12, 34, 18, 1, A("#FFFFFF", .7f)); }
        if (isStore)
        {
            p.Rect(0, 40, 100, 8, C("#F2C14E")); for (int i = 0; i < 12; i++) p.Rect(6 + i * 7, 42, 4, 4, C("#C9473F"));
            p.Rect(8, 49, 84, 18, C("#2A2433"));
            string[] g = { "#E0A33A", "#3FB8AF", "#E8467C", "#F2C14E", "#4F9A5B", "#5C9AD6" };
            for (int i = 0; i < 20; i++) { p.Rect(10 + i * 4, 51, 3, 5, C(g[i % 6])); p.Rect(10 + i * 4, 58, 3, 5, C(g[(i + 2) % 6])); }
            for (int x = 8; x < 92; x += 4) p.Rect(x, 49, 1, 18, C("#3A3A3A"));
            p.Rect(8, 66, 84, 14, C("#7A5634"));
        }
        else
        {
            p.Rect(42, 50, 16, 30, C("#5B3A29")); p.Rect(55, 64, 2, 2, C("#E2B93B")); p.Rect(42, 50, 16, 2, A("#000000", .3f));
            p.Rect(10, 52, 22, 14, C("#2A2838")); p.Rect(68, 52, 22, 14, R() > .5f ? C("#F5C76B") : C("#2A2838"));
            for (int i = 0; i <= 22; i += 4) { p.Rect(10 + i, 52, 1, 14, C("#2A2A2A")); p.Rect(68 + i, 52, 1, 14, C("#2A2A2A")); }
            if (gate)
            {
                p.Rect(30, 44, 40, 2, C("#3A3A3A"));
                for (int x = 30; x <= 70; x += 3) p.Rect(x, 44, 1, 36, C("#3A3A3A"));
                p.Rect(30, 60, 40, 1, C("#3A3A3A"));
            }
        }
        return p.Spr();
    }

    static Sprite Terminal()
    {
        var p = new Pix(100, 80);
        p.Rect(0, 0, 100, 80, C("#8A8E94")); p.Rect(0, 0, 100, 4, C("#6A6E74"));
        p.Rect(0, 8, 100, 10, C("#B8323C")); for (int i = 0; i < 14; i++) p.Rect(4 + i * 7, 11, 4, 4, C("#F0ECE0"));
        for (int k = 0; k < 4; k++) { p.Rect(6 + k * 24, 24, 18, 20, R() > .3f ? C("#F5C76B") : C("#2A2838")); p.Rect(6 + k * 24, 33, 18, 1, C("#3A3A3A")); }
        p.Rect(0, 50, 100, 30, C("#6A6E74")); p.Rect(0, 50, 100, 3, C("#4A4E54"));
        for (int x = 4; x < 100; x += 16) p.Rect(x, 53, 2, 27, C("#4A4E54"));
        return p.Spr();
    }

    static Sprite Skyline()
    {
        var p = new Pix(200, 120);
        int x = 0;
        while (x < 200)
        {
            int bw = 14 + rng.Next(22), bh = 30 + rng.Next(85);
            Color c = Color.Lerp(C("#1E1C36"), C("#2C2A48"), R());
            p.Rect(x, 120 - bh, bw, bh, c);
            for (int wy = 120 - bh + 4; wy < 116; wy += 5) for (int wx = x + 2; wx < x + bw - 2; wx += 4)
                if (R() > .62f) p.Rect(wx, wy, 2, 2, R() > .85f ? A("#3FB8AF", .8f) : A("#F5C76B", .75f));
            if (bh > 90) p.Set(x + bw / 2, 120 - bh - 1, C("#E8467C"));
            x += bw + rng.Next(3);
        }
        p.Rect(120, 30, 34, 16, C("#E8467C")); p.Rect(122, 32, 30, 12, C("#2C1A36")); p.Rect(124, 35, 20, 2, C("#F5C76B")); p.Rect(124, 39, 14, 2, C("#3FB8AF"));
        return p.Spr(PPU, .5f, 0f);
    }
}
