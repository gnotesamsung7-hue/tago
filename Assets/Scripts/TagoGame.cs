using System.Collections.Generic;
using UnityEngine;

// MARK DAVE'S TAGO - side-scrolling stealth with depth, set in modern Tondo.
// Everything is built at runtime, so the scene can be empty.
// Each character has x (along the street) and d (depth: 0 front, 1 back by the houses).
public class TagoGame : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (FindObjectOfType<TagoGame>() == null) new GameObject("Tago").AddComponent<TagoGame>(); }

    const float Y0 = -4.3f, DH = 3.2f;
    static float Yd(float d) { return Y0 + d * DH; }
    static float Sc(float d) { return 1f - .25f * d; }
    static int Ord(float d) { return 100 + (int)((1f - d) * 800f); }

    enum St { Title, Cutscene, Intro, Play, Caught, Clear, Win }

    class Actor { public Transform tr; public SpriteRenderer sr; public Sprite[] fr; public float x, d, anim; public int face = 1; public bool crouch, moving; }
    class Goon
    {
        public Actor a; public GoonDef def; public string st = "patrol"; public float alert, wait, search, sayT, lostT, flipT;
        public string say = ""; public float tx, td, lx, ld; public HouseDef checkHouse; public Mesh cone; public MeshRenderer coneR; public bool seeing;
    }
    class Cover { public CoverDef def; public float halfW; public bool tall; public Transform tr; }
    class Crowd { public CrowdDef def; public float x; public float dir = 1; public List<Actor> people = new List<Actor>(); public List<float> ox = new List<float>(), od = new List<float>(); }
    class Vehicle { public float laneD, x, speed, halfW; public int dir; public bool tall; public Transform tr; }
    class Dog { public float x, d, cd, bark; public SpriteRenderer sr; }
    class Item { public char k; public float x, d; public string n; public Transform tr; }
    class Fx { public float x, d, r, max, t; public Color c; public string txt; public SpriteRenderer sr; }
    class Proj { public float sx, ex, d, t; public bool done; public Transform tr; }

    static readonly string[] FOODS = { "Pandesal", "Fishball", "Kwek-kwek", "Taho", "Banana cue", "Siopao" };
    static readonly string[] L_CHASE = { "Hoy! Mark!", "Nandiyan siya!", "Huli ka!", "Tigil!", "Bayad mo!" };
    static readonly string[] L_SUS = { "Ano 'yon?", "May tao ba diyan?", "Hmm?" };
    static readonly string[] L_LOST = { "Nasaan na 'yon...", "Nawala...", "Tsk." };
    static readonly string[] L_BOSS = { "Akin ka na, Mark!", "Saan ka pupunta?", "Interes pa lang 'yan!" };
    static readonly string[] L_CAUGHT = { "Saan ka pupunta, ha?", "Kala mo makakatakas ka?", "Bayaran mo na!" };
    static readonly string[][] CUT = {
        new[]{"BOSSING TIBURON","MARK! Alam kong nandiyan ka sa loob!"},
        new[]{"BOSSING TIBURON","Tatlong buwan ka nang hindi nagbabayad. ₱48,000 na 'yan, kasama ang interes!"},
        new[]{"BOSSING TIBURON","Kulot, bantayan mo 'tong bahay. Paglabas niya, dalhin mo sa akin."},
        new[]{"KULOT","Opo, Bossing."},
        new[]{"MARK (pabulong)","Hindi ko na kaya 'to. Kailangan kong makarating sa bus terminal at umuwi sa probinsya."},
        new[]{"MARK (pabulong)","Tahimik lang... Tago."} };

    St state = St.Title; int si; StageDef stg; float L;
    readonly List<Goon> goons = new List<Goon>(); readonly List<Cover> covers = new List<Cover>(); readonly List<Crowd> crowds = new List<Crowd>();
    readonly List<Vehicle> cars = new List<Vehicle>(); readonly List<Dog> dogs = new List<Dog>(); readonly List<Item> items = new List<Item>();
    readonly List<Fx> fx = new List<Fx>(); readonly List<Proj> projs = new List<Proj>(); readonly List<Transform> sky = new List<Transform>(); readonly List<float> skyX = new List<float>();
    float[] laneT; int debt = 48000, cash, throws; float time, shake;

    Actor pl, tib; bool run, hidden, peeking, blended, sawCut; float stam, hunger, noiseT, stun, insideT, hintT; string hint = "";
    HouseDef inside, nearHouse; int cutLine; bool tipCover, tipCrowd, tipDoor, tipRoad;

    Camera cam; Transform root; Material meshMat;
    string ovTitle = "", ovBody = "", ovBtn = "";

    Vector2 moveIn, joyO, joyV; int joyId = -1; bool runHeld, actHeld, actDown; Rect rTago, rAct, rThrow, rRun, rSkip; float S = 1f;

    // ================= setup =================
    void Start()
    {
        Application.targetFrameRate = 60; Input.simulateMouseWithTouches = false; Screen.sleepTimeout = SleepTimeout.NeverSleep;
        var co = new GameObject("Camera"); cam = co.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 5f; cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Art.C("#121022"); co.transform.position = new Vector3(0, 0, -10);
        Art.Init();
        var tmp = new GameObject("tmp").AddComponent<SpriteRenderer>(); meshMat = tmp.sharedMaterial; Destroy(tmp.gameObject);
        if (meshMat == null) meshMat = new Material(Shader.Find("Sprites/Default"));
        LoadStage(0, true); TitleScreen();
    }

    static T Pick<T>(T[] a) { return a[Random.Range(0, a.Length)]; }

    SpriteRenderer Put(Sprite s, float x, float y, int order, float scale = 1f)
    {
        var go = new GameObject("o"); go.transform.SetParent(root, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.sortingOrder = order;
        go.transform.position = new Vector3(x, y, 0); go.transform.localScale = Vector3.one * scale; return sr;
    }
    void Quad(float x0, float x1, float y0, float y1, Color c, int order)
    {
        var sr = Put(Art.px1, x0, y0, order); sr.color = c; sr.transform.localScale = new Vector3(x1 - x0, y1 - y0, 1);
    }
    Actor MakeActor(Sprite[] fr, float x, float d, int face)
    {
        var a = new Actor { fr = fr, x = x, d = d, face = face };
        a.sr = Put(fr[0], x, Yd(d), Ord(d), Sc(d)); a.tr = a.sr.transform; return a;
    }

    void LoadStage(int i, bool withCut)
    {
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("Stage").transform;
        si = i; stg = Stages.Build(i); L = stg.length;
        goons.Clear(); covers.Clear(); crowds.Clear(); cars.Clear(); dogs.Clear(); items.Clear(); fx.Clear(); projs.Clear(); sky.Clear(); skyX.Clear();
        tib = null; inside = null;

        // sky & ground
        for (float x = -12; x < L + 24; x += 10) { var s = Put(Art.skyline, x, -1.6f, -200); sky.Add(s.transform); skyX.Add(x); }
        if (stg.road)
        {
            Quad(-12, L + 12, -5.5f, Yd(.2f), Art.C("#5A5560"), -90);
            Quad(-12, L + 12, Yd(.2f) - .06f, Yd(.2f), Art.C("#8A8590"), -89);
            Quad(-12, L + 12, Yd(.2f), Yd(.72f), Art.C("#26252E"), -90);
            for (int k = 0; k < stg.lanes.Length - 1; k++)
            {
                float ld = (stg.lanes[k] + stg.lanes[k + 1]) / 2f;
                for (float x = -10; x < L + 10; x += 2.2f) Quad(x, x + 1.1f, Yd(ld) - .03f, Yd(ld) + .03f, Art.A("#D8D0A0", .55f), -88);
            }
            Quad(-12, L + 12, Yd(.72f), Yd(.72f) + .07f, Art.C("#8A8590"), -89);
            Quad(-12, L + 12, Yd(.72f) + .07f, Yd(1f) + .1f, Art.C("#4E4A55"), -90);
        }
        else
        {
            Quad(-12, L + 12, -5.5f, Yd(1f) + .1f, Art.C("#3A3640"), -90);
            Quad(-12, L + 12, Yd(.02f) - .05f, Yd(.02f) + .05f, Art.C("#24222A"), -89);
            for (float x = -10; x < L + 10; x += 3.7f) Quad(x, x + .9f, Yd(.5f) + Mathf.Sin(x) * .6f, Yd(.5f) + Mathf.Sin(x) * .6f + .03f, Art.A("#000000", .25f), -88);
        }
        // houses, poles, wires
        foreach (var h in stg.houses)
        {
            Sprite s = h.terminal ? Art.terminal : h.mark ? Art.markHouse : h.store ? Art.store : Art.houses[h.variant];
            Put(s, h.x, Yd(1f) - .02f, -60);
        }
        for (float x = 0; x < L + 15; x += Stages.HouseW) Put(Art.wire, x, 1.25f, -40);
        for (float x = 7.5f; x < L + 15; x += 15) Put(Art.pole, x, Yd(1f) - .05f, -45);
        foreach (var b in stg.blocks) for (float x = b.x0 + 1; x < b.x1; x += 2f) Put(Art.barrier, x, Yd(b.d0 + .12f), Ord(b.d0 + .12f), Sc(b.d0 + .12f));
        if (stg.terminal) Put(Art.bus, L - 2.6f, Yd(.88f), Ord(.88f), Sc(.88f));

        foreach (var c in stg.covers)
        {
            Sprite s; bool tall = false;
            switch (c.k)
            {
                case CoverKind.Drum: s = Art.drum; break;
                case CoverKind.Crates: s = Art.crates; break;
                case CoverKind.Bin: s = Art.bin; break;
                case CoverKind.Stall: s = Art.stall; tall = true; break;
                case CoverKind.Trike: s = Art.trikes[Random.Range(0, 3)]; tall = true; break;
                case CoverKind.Jeep: s = Art.jeep; tall = true; break;
                default: s = Art.bus; tall = true; break;
            }
            var sr = Put(s, c.x, Yd(c.d), Ord(c.d) + 1, Sc(c.d));
            if (Random.value < .5f && c.k != CoverKind.Bus) sr.flipX = true;
            covers.Add(new Cover { def = c, tall = tall, tr = sr.transform, halfW = s.rect.width / Art.PPU / 2f * Sc(c.d) });
        }
        foreach (var it in stg.items)
        {
            var item = new Item { k = it.k, x = it.x, d = it.d, n = Pick(FOODS) };
            item.tr = Put(it.k == 'F' ? Art.food : Art.cash, it.x, Yd(it.d), Ord(it.d), Sc(it.d)).transform; items.Add(item);
        }
        foreach (var dd in stg.dogs) { var dg = new Dog { x = dd[0], d = dd[1] }; dg.sr = Put(Art.dog, dg.x, Yd(dg.d), Ord(dg.d), Sc(dg.d)); dogs.Add(dg); }
        foreach (var cd in stg.crowds)
        {
            var cr = new Crowd { def = cd, x = cd.x, dir = cd.speed < 0 ? -1 : 1 };
            for (int k = 0; k < cd.n; k++)
            {
                float ox = -cd.halfW + 2 * cd.halfW * (k + .5f) / cd.n + Random.Range(-.2f, .2f), od = Random.Range(-.08f, .08f);
                int face = cd.speed == 0 ? (Random.value < .5f ? 1 : -1) : (int)cr.dir;
                cr.people.Add(MakeActor(Art.npcs[Random.Range(0, Art.npcs.Length)], cd.x + ox, Mathf.Clamp01(cd.d + od), face));
                cr.ox.Add(ox); cr.od.Add(od);
            }
            crowds.Add(cr);
        }
        foreach (var gd in stg.goons)
        {
            var g = new Goon { def = gd, a = MakeActor(gd.boss ? Art.boss : Art.goon, gd.x, gd.d, 1) };
            if (gd.boss) g.a.tr.localScale *= 1.1f;
            var cgo = new GameObject("cone"); cgo.transform.SetParent(root, false);
            g.cone = new Mesh(); cgo.AddComponent<MeshFilter>().sharedMesh = g.cone;
            g.coneR = cgo.AddComponent<MeshRenderer>(); g.coneR.sharedMaterial = meshMat; g.coneR.sortingOrder = 60;
            goons.Add(g);
        }
        laneT = new float[stg.lanes.Length]; for (int k = 0; k < laneT.Length; k++) laneT[k] = Random.Range(0f, 2f);

        pl = MakeActor(Art.player, stg.startX, stg.startD, 1);
        stam = 100; hunger = 100; run = false; hidden = false; peeking = false; blended = false; stun = 0; throws = 3; noiseT = 0; pl.crouch = false;
        if (i == 0)
        {
            inside = stg.houses[0]; pl.sr.enabled = false; insideT = 0;
            if (withCut)
            {
                tib = MakeActor(Art.boss, 4.6f, .55f, -1); tib.tr.localScale *= 1.1f;
                goons[0].a.x = 6.4f; goons[0].a.face = -1; goons[0].wait = 1.5f;
            }
        }
        cam.transform.position = new Vector3(CamTarget(), 0, -10);
        // pre-fill traffic so the road isn't empty
        for (int k = 0; k < stg.lanes.Length; k++) for (int n = 0; n < 2; n++) SpawnCar(k, cam.transform.position.x + Random.Range(-9f, 9f));
    }

    // ================= flow =================
    void TitleScreen()
    {
        state = St.Title; ovTitle = "Mark Dave's\nTAGO";
        ovBody = "You borrowed ₱20,000 from Bossing Tiburon. Three months later you owe ₱48,000.\nTonight he's at your gate. Get to the bus terminal and go home to the province.\n\n" +
                 "Left side: move (up/down to change depth)    Tago: crouch    Hold to run: fast but loud\n" +
                 "Crouch behind cover to hide, hold Silip to peek over it. Walk with a crowd to blend in.\nPasok: hide inside a house - but not for long, and not if they saw you go in.";
        ovBtn = "Simulan";
    }
    void StartStage()
    {
        if (si == 0 && !sawCut) { LoadStage(0, true); state = St.Cutscene; cutLine = 0; return; }
        LoadStage(si, false); state = St.Intro;
        ovTitle = (si + 1) + "/3  " + stg.name;
        ovBody = stg.sub + "\n\nUtang: ₱" + debt.ToString("N0") + "     Pera: ₱" + cash.ToString("N0");
        ovBtn = "Tara";
    }
    void Advance()
    {
        switch (state)
        {
            case St.Title: si = 0; debt = 48000; cash = 0; sawCut = false; StartStage(); break;
            case St.Cutscene: NextCut(); break;
            case St.Intro: state = St.Play; break;
            case St.Caught: StartStage(); break;
            case St.Clear: si++; StartStage(); break;
            case St.Win: si = 0; LoadStage(0, true); TitleScreen(); break;
        }
    }
    void NextCut()
    {
        cutLine++;
        if (cutLine == 3 && tib != null) tib.face = -1;
        if (cutLine >= CUT.Length) EndCut();
    }
    void EndCut()
    {
        sawCut = true; state = St.Play;
        goons[0].a.face = 1; goons[0].wait = 0;
        ShowHint("You're inside your house. Tap LABAS when Kulot looks away.");
    }
    void Caught(Goon g, string why = null)
    {
        state = St.Caught; shake = .4f;
        debt += Mathf.RoundToInt(debt * .2f / 100f) * 100; cash = cash / 2 / 100 * 100;
        ovTitle = g.def.boss ? "Nahuli ka ni Bossing" : "Nahuli ka";
        ovBody = "\"" + (g.def.boss ? Pick(L_BOSS) : Pick(L_CAUGHT)) + "\"\n\n" + (why ?? "") + (why != null ? "\n" : "") +
                 "They took half your cash and added 20% interest.\nUtang: ₱" + debt.ToString("N0");
        ovBtn = "Subukan ulit";
    }
    void Cleared()
    {
        if (si >= Stages.Count - 1)
        {
            state = St.Win; ovTitle = "Nakatakas ka";
            ovBody = "The bus pulls out of the terminal. Through the window you see Bossing Tiburon shouting at nobody.\nNext stop: the province.\n\nEscaped with ₱" + cash.ToString("N0") + " in your pocket.";
            ovBtn = "Laro ulit";
        }
        else { state = St.Clear; ovTitle = "Nakalusot"; ovBody = stg.name + " is behind you. Keep moving.\n\nPera: ₱" + cash.ToString("N0"); ovBtn = "Tuloy"; }
    }
    void ShowHint(string h) { hint = h; hintT = 4.5f; }

    // ================= input =================
    void Layout()
    {
        S = Screen.height / 400f; Rect sa = Screen.safeArea;
        float right = Screen.width - sa.xMax, bottom = sa.yMin, top = Screen.height - sa.yMax;
        float bw = 100 * S, bh = 50 * S, m = 14 * S, g = 10 * S;
        float x0 = Screen.width - right - m - bw * 2 - g, y1 = Screen.height - bottom - m - bh;
        rThrow = new Rect(x0, y1, bw, bh); rRun = new Rect(x0 + bw + g, y1, bw, bh);
        rTago = new Rect(x0, y1 - bh - g, bw, bh); rAct = new Rect(x0 + bw + g, y1 - bh - g, bw, bh);
        rSkip = new Rect(Screen.width - right - m - 90 * S, top + m, 90 * S, 36 * S);
    }

    void ReadInput()
    {
        Layout();
        Vector2 k = Vector2.zero;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x -= 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y += 1;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y -= 1;
        actDown = Input.GetKeyDown(KeyCode.E); actHeld = Input.GetKey(KeyCode.E);
        bool touchRun = false, joyAlive = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            Vector2 gp = new Vector2(t.position.x, Screen.height - t.position.y);
            bool ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            if (t.phase == TouchPhase.Began)
            {
                if (state == St.Cutscene && rSkip.Contains(gp)) { EndCut(); continue; }
                if (state != St.Play) { Advance(); continue; }
                if (rTago.Contains(gp)) { pl.crouch = !pl.crouch; continue; }
                if (rThrow.Contains(gp)) { DoThrow(); continue; }
                if (rAct.Contains(gp)) { actDown = true; continue; }
                if (!rRun.Contains(gp) && gp.x < Screen.width * .55f && joyId < 0) { joyId = t.fingerId; joyO = gp; joyV = Vector2.zero; }
            }
            if (!ended && t.fingerId != joyId) { if (rRun.Contains(gp)) touchRun = true; if (rAct.Contains(gp)) actHeld = true; }
            if (t.fingerId == joyId)
            {
                if (ended) { joyId = -1; joyV = Vector2.zero; }
                else { joyAlive = true; Vector2 d = gp - joyO; float R = 60 * S; if (d.magnitude > R) d = d.normalized * R; joyV = d / R; }
            }
        }
        if (!joyAlive) { joyId = -1; joyV = Vector2.zero; }

        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (Input.GetMouseButtonDown(0))
        {
            if (state == St.Cutscene && rSkip.Contains(mp)) EndCut();
            else if (state != St.Play) Advance();
            else if (rTago.Contains(mp)) pl.crouch = !pl.crouch;
            else if (rThrow.Contains(mp)) DoThrow();
            else if (rAct.Contains(mp)) actDown = true;
        }
        if (Input.GetMouseButton(0) && rAct.Contains(mp)) actHeld = true;
        bool mouseRun = Input.GetMouseButton(0) && rRun.Contains(mp);

        moveIn = k + joyV;
        runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || touchRun || mouseRun;
        if (state != St.Play) { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Advance(); }
        else
        {
            if (Input.GetKeyDown(KeyCode.Space)) DoThrow();
            if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) pl.crouch = !pl.crouch;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) { if (state == St.Title) Application.Quit(); else { si = 0; LoadStage(0, true); TitleScreen(); } }
    }

    void DoThrow()
    {
        if (state != St.Play || throws <= 0 || inside != null) return;
        throws--;
        float ex = Mathf.Clamp(pl.x + pl.face * 5f, .5f, L - .5f);
        var pr = new Proj { sx = pl.x, ex = ex, d = pl.d }; pr.tr = Put(Art.pebble, pl.x, Yd(pl.d) + 1f, 950).transform; projs.Add(pr);
    }

    // ================= queries =================
    float Dist(float x1, float d1, float x2, float d2) { float dx = x2 - x1, dd = (d2 - d1) * 5f; return Mathf.Sqrt(dx * dx + dd * dd); }
    float Range(Goon g) { return g.def.boss ? 8.5f : 7f; }
    bool InBlock(float x, float d) { foreach (var b in stg.blocks) if (x > b.x0 && x < b.x1 && d > b.d0 && d < b.d1) return true; return false; }

    bool Blocked(float gx, float gd, float px, float pd)
    {
        float lo = Mathf.Min(gd, pd) + .04f, hi = Mathf.Max(gd, pd) - .04f;
        if (hi <= lo) return false;
        foreach (var c in covers)
        {
            if (!c.tall || c.def.d < lo || c.def.d > hi) continue;
            float t = (c.def.d - gd) / (pd - gd), xa = gx + (px - gx) * t;
            if (Mathf.Abs(xa - c.def.x) < c.halfW) return true;
        }
        foreach (var v in cars)
        {
            if (!v.tall || v.laneD < lo || v.laneD > hi) continue;
            float t = (v.laneD - gd) / (pd - gd), xa = gx + (px - gx) * t;
            if (Mathf.Abs(xa - v.x) < v.halfW) return true;
        }
        return false;
    }

    bool CanSee(Goon g, out float dist)
    {
        dist = 99f; if (inside != null || state != St.Play) return false;
        float dx = pl.x - g.a.x, dd = pl.d - g.a.d; dist = Dist(g.a.x, g.a.d, pl.x, pl.d);
        if (hidden && !peeking) return g.st == "chase" && dist < 1.2f;
        float range = Range(g);
        if (blended) range = Mathf.Min(range, 2.6f);
        if (peeking) range = Mathf.Min(range, 4f);
        if (pl.crouch) range *= .8f;
        if (dist > range || Mathf.Abs(dd) > .45f) return false;
        if (g.st != "chase" && Mathf.Abs(dx) > .5f && Mathf.Sign(dx) != g.a.face) return false;
        return !Blocked(g.a.x, g.a.d, pl.x, pl.d);
    }

    void Say(Goon g, string[] lines) { g.say = Pick(lines); g.sayT = 1.8f; }
    void AddText(float x, float d, string s) { fx.Add(new Fx { x = x, d = d, txt = s, t = 1.4f }); }

    void Noise(float x, float d, float r, string src)
    {
        foreach (var g in goons)
        {
            if (g.st == "chase" || g.st == "checkhouse") continue;
            if (Dist(g.a.x, g.a.d, x, d) < r)
            {
                g.st = "investigate"; g.tx = x; g.td = d; g.alert = Mathf.Max(g.alert, .3f);
                if (src != "quiet") Say(g, L_SUS);
            }
        }
        var f = new Fx { x = x, d = d, max = r, t = .6f, c = src == "dog" || src == "honk" ? Art.C("#E8467C") : Art.C("#F2A541") };
        f.sr = Put(Art.ring, x, Yd(d), 70); fx.Add(f);
    }

    // returns true when arrived
    bool MoveTo(Actor a, float x, float d, float sp, float dt)
    {
        float dx = x - a.x, dd = d - a.d;
        if (Mathf.Abs(dx) > .05f) a.face = dx > 0 ? 1 : -1;
        float stepX = sp * dt, stepD = .5f * dt * Mathf.Clamp(sp / 2f, .6f, 1.6f);
        a.x += Mathf.Clamp(dx, -stepX, stepX); a.d += Mathf.Clamp(dd, -stepD, stepD);
        a.moving = Mathf.Abs(dx) > .05f || Mathf.Abs(dd) > .01f;
        return Mathf.Abs(dx) < .08f && Mathf.Abs(dd) < .02f;
    }

    // ================= update =================
    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .05f); time += dt;
        ReadInput(); UpdateFx(dt); if (shake > 0) shake -= dt; if (hintT > 0) hintT -= dt;
        if (tib != null && ((state == St.Cutscene && cutLine >= 3) || state == St.Play))
        {
            MoveTo(tib, -6f, .45f, 2.2f, dt);
            if (tib.x < -5f) { Destroy(tib.tr.gameObject); tib = null; }
        }
        UpdateCrowds(dt);
        if (state == St.Play || state == St.Cutscene) UpdateTraffic(dt);
        if (state == St.Play) Tick(dt);
        Render(dt);
    }

    void UpdateFx(float dt)
    {
        for (int i = fx.Count - 1; i >= 0; i--)
        {
            var f = fx[i]; f.t -= dt;
            if (f.sr != null)
            {
                f.r += f.max * dt / .6f;
                f.sr.transform.localScale = new Vector3(Mathf.Max(.01f, f.r), Mathf.Max(.01f, f.r) * .3f, 1);
                var c = f.c; c.a = Mathf.Clamp01(f.t / .6f); f.sr.color = c;
            }
            if (f.t <= 0) { if (f.sr != null) Destroy(f.sr.gameObject); fx.RemoveAt(i); }
        }
    }

    void UpdateCrowds(float dt)
    {
        foreach (var c in crowds)
        {
            if (c.def.speed != 0)
            {
                c.x += c.dir * Mathf.Abs(c.def.speed) * dt;
                if (c.x > L - 6) c.dir = -1; if (c.x < 6) c.dir = 1;
            }
            for (int k = 0; k < c.people.Count; k++)
            {
                var a = c.people[k]; a.x = c.x + c.ox[k]; a.d = Mathf.Clamp01(c.def.d + c.od[k]);
                if (c.def.speed != 0) { a.face = (int)c.dir; a.moving = true; }
            }
        }
    }

    void SpawnCar(int lane, float x)
    {
        int dir = lane % 2 == 0 ? 1 : -1; Sprite s; float sp; bool tall = true;
        if (stg.busLane) { s = Random.value < .7f ? Art.bus : Art.jeep; sp = 2.6f; }
        else
        {
            float r = Random.value;
            if (r < .3f) { s = Art.jeep; sp = 4.6f; } else if (r < .45f) { s = Art.ejeep; sp = 4.8f; }
            else if (r < .65f) { s = Art.trikes[Random.Range(0, 3)]; sp = 3.8f; } else if (r < .82f) { s = Art.moto; sp = 6.2f; tall = false; }
            else { s = Art.car; sp = 5.6f; }
        }
        float ld = stg.lanes[lane];
        var sr = Put(s, x, Yd(ld), Ord(ld), Sc(ld)); sr.flipX = dir < 0;
        cars.Add(new Vehicle { laneD = ld, x = x, speed = sp * Random.Range(.85f, 1.15f), dir = dir, tall = tall, tr = sr.transform, halfW = s.rect.width / Art.PPU / 2f * Sc(ld) });
    }

    void UpdateTraffic(float dt)
    {
        if (stg.lanes.Length == 0) return;
        float cx = cam.transform.position.x;
        for (int k = 0; k < laneT.Length; k++)
        {
            laneT[k] -= dt;
            if (laneT[k] <= 0) { int dir = k % 2 == 0 ? 1 : -1; SpawnCar(k, cx - dir * 14f); laneT[k] = stg.busLane ? Random.Range(6f, 10f) : Random.Range(1.6f, 4.2f); }
        }
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            var v = cars[i];
            // keep a gap behind the vehicle ahead in the same lane
            float sp = v.speed;
            foreach (var o in cars) if (o != v && o.laneD == v.laneD) { float gap = (o.x - v.x) * v.dir; if (gap > 0 && gap < v.halfW + o.halfW + .6f) sp = Mathf.Min(sp, o.speed * .9f); }
            v.x += v.dir * sp * dt;
            if ((v.x - cx) * v.dir > 16f) { Destroy(v.tr.gameObject); cars.RemoveAt(i); }
        }
    }

    void Tick(float dt)
    {
        // ---- inside a house ----
        if (inside != null)
        {
            insideT += dt; stam = Mathf.Min(100, stam + 20 * dt); hunger = Mathf.Max(0, hunger - dt * .4f);
            if (actDown) ExitHouse(false);
            else if (!inside.mark && insideT > 8f) ExitHouse(true);
            foreach (var g in goons) { UpdGoon(g, dt); if (state != St.Play) return; }
            return;
        }

        // ---- movement ----
        float m = moveIn.magnitude; pl.moving = m > .15f && stun <= 0;
        Vector2 mv = m > 1f ? moveIn / m : moveIn;
        run = runHeld && stam > 5 && pl.moving;
        if (run) pl.crouch = false;
        float sp = pl.crouch ? 1.2f : 2.4f; if (run) sp = 4.5f; if (hunger <= 0) sp *= .6f;
        stam = run ? Mathf.Max(0, stam - 26 * dt) : Mathf.Min(100, stam + 13 * dt);
        hunger = Mathf.Max(0, hunger - dt * (run ? 1.3f : .55f));
        if (stun > 0) stun -= dt;
        else if (pl.moving)
        {
            if (Mathf.Abs(mv.x) > .2f) pl.face = mv.x > 0 ? 1 : -1;
            float nx = Mathf.Clamp(pl.x + mv.x * sp * dt, .4f, L - .2f);
            float nd = Mathf.Clamp01(pl.d - mv.y * .55f * dt * (sp / 2.4f));
            if (!InBlock(nx, pl.d)) pl.x = nx;
            if (!InBlock(pl.x, nd)) pl.d = nd;
        }

        // ---- concealment ----
        hidden = false;
        if (pl.crouch && !run)
            foreach (var c in covers) if (Mathf.Abs(c.def.x - pl.x) < c.halfW + .15f && pl.d >= c.def.d - .03f && pl.d - c.def.d < .18f) { hidden = true; break; }
        peeking = hidden && actHeld;
        blended = false;
        if (!run && !hidden)
            foreach (var c in crowds) if (Mathf.Abs(c.x - pl.x) < c.def.halfW + .3f && Mathf.Abs(c.def.d - pl.d) < .16f) { blended = true; break; }

        nearHouse = null;
        if (pl.d > .82f) foreach (var h in stg.houses) if (!h.store && !h.terminal && Mathf.Abs(h.x - pl.x) < .7f) { nearHouse = h; break; }
        if (actDown && nearHouse != null)
        {
            if (nearHouse.locked) AddText(pl.x, pl.d, "Sarado!");
            else { EnterHouse(nearHouse); return; }
        }

        // tips
        if (!tipCover && pl.x > 6) foreach (var c in covers) if (Mathf.Abs(c.def.x - pl.x) < 1.2f && Mathf.Abs(c.def.d - pl.d) < .2f) { tipCover = true; ShowHint("Tap TAGO next to cover to hide. Hold SILIP to peek over it."); break; }
        if (!tipCrowd) foreach (var c in crowds) if (Mathf.Abs(c.x - pl.x) < c.def.halfW + 1f && Mathf.Abs(c.def.d - pl.d) < .25f) { tipCrowd = true; ShowHint("Walk inside a crowd to blend in. Don't run."); break; }
        if (!tipDoor && nearHouse != null && si == 0 && pl.x > 6) { tipDoor = true; ShowHint("PASOK to hide in a house. The owner will kick you out soon."); }
        if (!tipRoad && stg.road && !stg.terminal && pl.d < .7f && pl.d > .2f) { tipRoad = true; ShowHint("Watch the traffic! Jeepneys passing between you and a goon block his view."); }

        noiseT -= dt;
        if (run && noiseT <= 0) { noiseT = .35f; Noise(pl.x, pl.d, 4f, "quiet"); }

        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            if (Mathf.Abs(it.x - pl.x) > .5f || Mathf.Abs(it.d - pl.d) > .12f) continue;
            if (it.k == 'F') { hunger = Mathf.Min(100, hunger + 40); AddText(pl.x, pl.d, it.n + " +40"); }
            else { int v = Pick(new[] { 200, 300, 500 }); cash += v; AddText(pl.x, pl.d, "+₱" + v); }
            Destroy(it.tr.gameObject); items.RemoveAt(i);
        }

        for (int i = projs.Count - 1; i >= 0; i--)
        {
            var o = projs[i]; o.t += dt / .5f;
            if (o.t >= 1 && !o.done) { o.done = true; Noise(o.ex, o.d, 6f, "tansan"); }
            if (o.t >= 1.5f) { Destroy(o.tr.gameObject); projs.RemoveAt(i); }
        }

        foreach (var dg in dogs)
        {
            dg.cd -= dt; dg.bark -= dt;
            float trig = hidden ? 0 : (pl.crouch ? 1.2f : 2.6f);
            if (Dist(dg.x, dg.d, pl.x, pl.d) < trig && dg.cd <= 0) { dg.cd = 4; dg.bark = 1; Noise(dg.x, dg.d, 7f, "dog"); }
        }

        if (stun <= 0)
            foreach (var v in cars)
                if (Mathf.Abs(v.laneD - pl.d) < .07f && Mathf.Abs(v.x - pl.x) < v.halfW + .15f)
                {
                    stun = 1f; shake = .3f; hunger = Mathf.Max(0, hunger - 5);
                    pl.d = v.laneD < .46f ? .1f : .8f; pl.crouch = false;
                    AddText(pl.x, pl.d, "BEEEP! Ingat!"); Noise(pl.x, pl.d, 7f, "honk"); break;
                }

        foreach (var g in goons) { UpdGoon(g, dt); if (state != St.Play) return; }
        if (pl.x >= L - 1.6f) Cleared();
    }

    void EnterHouse(HouseDef h)
    {
        inside = h; insideT = 0; pl.crouch = false; pl.sr.enabled = false; hidden = false; peeking = false; blended = false;
        foreach (var g in goons)
            if (g.seeing && (g.st == "chase" || g.alert > .5f)) { g.st = "checkhouse"; g.checkHouse = h; g.wait = 0; Say(g, new[] { "Pumasok siya diyan!", "Nasa loob 'yon!" }); }
    }
    void ExitHouse(bool kicked)
    {
        pl.x = inside.x; pl.d = .93f; pl.sr.enabled = true; pl.face = 1;
        if (kicked) { AddText(pl.x, pl.d, inside.owner + ": \"Labas! Sino ka ba?!\""); Noise(pl.x, pl.d, 4f, "quiet"); }
        inside = null;
    }

    void UpdGoon(Goon g, float dt)
    {
        g.sayT -= dt; float dist; bool seen = CanSee(g, out dist); g.seeing = seen; var a = g.a; a.moving = false;
        if (seen)
        {
            float rate = (g.def.boss ? 2f : 1.4f) * (1.3f - dist / Range(g));
            if (run) rate *= 1.6f; if (pl.crouch && !hidden) rate *= .55f; if (blended) rate *= .25f; if (peeking) rate *= .6f;
            g.alert = Mathf.Min(1.2f, g.alert + rate * dt); g.lx = pl.x; g.ld = pl.d; g.lostT = 0;
            if (g.st != "chase" && g.alert >= 1)
            {
                g.st = "chase"; Say(g, g.def.boss ? L_BOSS : L_CHASE);
                foreach (var o in goons) if (o != g && o.st != "chase" && Mathf.Abs(o.a.x - a.x) < 9) { o.st = "investigate"; o.tx = pl.x; o.td = pl.d; }
            }
            else if ((g.st == "patrol" || g.st == "search" || g.st == "return") && g.alert > .35f) { g.st = "investigate"; g.tx = pl.x; g.td = pl.d; Say(g, L_SUS); }
        }
        else if (g.st != "chase") g.alert = Mathf.Max(0, g.alert - dt * .3f);

        float walk = g.def.boss ? 1.6f : 1.4f;
        switch (g.st)
        {
            case "chase":
                {
                    float sp = g.def.boss ? 4.4f : 4.0f;
                    if (seen) MoveTo(a, pl.x, pl.d, sp, dt);
                    else
                    {
                        g.lostT += dt;
                        if (MoveTo(a, g.lx, g.ld, sp * .9f, dt) || g.lostT > 3f) { g.st = "search"; g.search = 3; g.alert = .5f; Say(g, L_LOST); }
                    }
                    if (inside == null && Mathf.Abs(pl.x - a.x) < .5f && Mathf.Abs(pl.d - a.d) < .14f) { Caught(g); return; }
                    break;
                }
            case "investigate":
                if (MoveTo(a, g.tx, Mathf.Clamp01(g.td), walk * 1.5f, dt)) { g.st = "search"; g.search = 2.5f; }
                break;
            case "checkhouse":
                if (MoveTo(a, g.checkHouse.x, .9f, 3.2f, dt))
                {
                    g.wait += dt;
                    if (g.wait > 1f)
                    {
                        if (inside == g.checkHouse) { Caught(g, "They saw you go in, and searched the house."); return; }
                        g.st = "search"; g.search = 2.5f; g.wait = 0;
                    }
                }
                break;
            case "search":
                g.search -= dt; g.flipT -= dt;
                if (g.flipT <= 0) { a.face = -a.face; g.flipT = .8f; }
                if (g.search <= 0) g.st = "return";
                break;
            case "return":
                if (MoveTo(a, Mathf.Clamp(a.x, g.def.min, g.def.max), g.def.d, walk, dt)) { g.st = "patrol"; g.wait = .5f; }
                break;
            default: // patrol
                if (g.wait > 0) { g.wait -= dt; if (g.wait <= 0) a.face = -a.face; }
                else if (MoveTo(a, a.face > 0 ? g.def.max : g.def.min, g.def.d, walk, dt)) g.wait = 1.6f;
                break;
        }
    }

    // ================= rendering =================
    float CamTarget()
    {
        float half = cam.orthographicSize * cam.aspect, x = pl != null ? pl.x + pl.face * 1.5f : 0;
        if (pl != null && inside != null) x = inside.x;
        return Mathf.Clamp(x, half - .5f, Mathf.Max(half - .5f, L - half + .5f));
    }

    void Place(Actor a, float dt)
    {
        if (a.moving) a.anim += dt; 
        int f = a.crouch ? 2 : (a.moving && ((int)(a.anim / .18f)) % 2 == 1 ? 1 : 0);
        a.sr.sprite = a.fr[f]; a.sr.flipX = a.face < 0;
        float sc = Sc(a.d) * (a.fr == Art.boss ? 1.1f : 1f);
        a.tr.position = new Vector3(a.x, Yd(a.d), 0); a.tr.localScale = new Vector3(sc, sc, 1); a.sr.sortingOrder = Ord(a.d);
    }

    void Render(float dt)
    {
        var cp = cam.transform.position; cp.x = Mathf.Lerp(cp.x, CamTarget(), .1f);
        Vector3 p = new Vector3(cp.x, 0, -10); if (shake > 0) p += (Vector3)(Random.insideUnitCircle * .12f);
        cam.transform.position = p;
        for (int i = 0; i < sky.Count; i++) sky[i].position = new Vector3(skyX[i] + cp.x * .6f, -1.6f, 0);

        Place(pl, dt);
        pl.sr.color = new Color(1, 1, 1, hidden && !peeking ? .55f : 1f);
        if (peeking) pl.sr.sprite = pl.fr[0];
        if (tib != null) Place(tib, dt);
        foreach (var c in crowds) foreach (var a in c.people) Place(a, dt);
        foreach (var v in cars) { v.tr.position = new Vector3(v.x, Yd(v.laneD), 0); }
        foreach (var d in dogs) d.sr.flipX = pl.x < d.x;
        foreach (var it in items) it.tr.position = new Vector3(it.x, Yd(it.d) + .05f + Mathf.Abs(Mathf.Sin(time * 3 + it.x)) * .08f, 0);
        foreach (var o in projs) { float t = Mathf.Min(1, o.t); o.tr.position = new Vector3(Mathf.Lerp(o.sx, o.ex, t), Yd(o.d) + .9f + Mathf.Sin(t * Mathf.PI) * 1.2f - t * .9f, 0); }

        bool blind = (hidden && !peeking) || inside != null;
        foreach (var g in goons)
        {
            Place(g.a, dt);
            float dist = Dist(g.a.x, g.a.d, pl.x, pl.d);
            g.a.sr.color = new Color(1, 1, 1, blind && dist > 3.5f && state == St.Play ? .15f : 1f);
            g.coneR.enabled = !blind && state != St.Cutscene;
            if (g.coneR.enabled) BuildCone(g);
        }
    }

    void BuildCone(Goon g)
    {
        float r = Range(g), x0 = g.a.x, x1 = g.a.x + g.a.face * r;
        float dlo = Mathf.Max(0, g.a.d - .45f), dhi = Mathf.Min(1, g.a.d + .45f);
        Color col = g.st == "chase" ? new Color(.91f, .27f, .49f, .3f) : (g.alert > .3f || g.st == "investigate" || g.st == "search" || g.st == "checkhouse") ? new Color(.95f, .65f, .25f, .25f) : new Color(1f, .94f, .78f, .12f);
        Color far = col; far.a *= .15f;
        var v = new[] { new Vector3(x0, Yd(g.a.d), 0), new Vector3(x1, Yd(dlo), 0), new Vector3(x1, Yd(dhi), 0) };
        g.cone.Clear(); g.cone.vertices = v; g.cone.colors = new[] { col, far, far }; g.cone.triangles = new[] { 0, 1, 2 }; g.cone.RecalculateBounds();
    }

    // ================= HUD (IMGUI) =================
    GUIStyle sLabel, sCenter, sTitle, sBody, sBtn; float styleS = -1;
    void Styles()
    {
        if (Mathf.Approximately(styleS, S) && sLabel != null) return; styleS = S;
        sLabel = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(13 * S), richText = true }; sLabel.normal.textColor = Art.C("#F0ECE0");
        sCenter = new GUIStyle(sLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        sTitle = new GUIStyle(sLabel) { fontSize = Mathf.RoundToInt(42 * S), fontStyle = FontStyle.Bold, wordWrap = true }; sTitle.normal.textColor = Art.C("#F2A541");
        sBody = new GUIStyle(sLabel) { fontSize = Mathf.RoundToInt(14 * S), wordWrap = true };
        sBtn = new GUIStyle(sCenter) { fontSize = Mathf.RoundToInt(15 * S) };
    }
    void Box(Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, Art.white); GUI.color = o; }
    Vector2 WS(float x, float y) { var v = cam.WorldToScreenPoint(new Vector3(x, y, 0)); return new Vector2(v.x, Screen.height - v.y); }
    void Txt(Vector2 at, string s, Color c, int size)
    {
        sCenter.fontSize = Mathf.RoundToInt(size * S); sCenter.normal.textColor = c;
        GUI.Label(new Rect(at.x - 160 * S, at.y - 12 * S, 320 * S, 24 * S), s, sCenter);
    }
    void Bubble(Vector2 at, string s, Color bg, Color fg)
    {
        sCenter.fontSize = Mathf.RoundToInt(11 * S);
        float w = Mathf.Min(sCenter.CalcSize(new GUIContent(s)).x + 12 * S, 260 * S);
        var r = new Rect(at.x - w / 2, at.y - 20 * S, w, 18 * S); Box(r, bg); sCenter.normal.textColor = fg; GUI.Label(r, s, sCenter);
    }
    float HeadY(Actor a) { return Yd(a.d) + 1.75f * Sc(a.d); }

    void OnGUI()
    {
        if (cam == null || pl == null) return;
        Layout(); Styles();
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Art.vignette);
        bool blind = (hidden && !peeking) || inside != null;

        // world labels
        if (stg.exitSign != "") Bubble(WS(L - 1.2f, Yd(1f) + 1.2f), stg.exitSign, Art.C("#2E8B57"), Art.C("#F0ECE0"));
        if (stg.terminal) { Bubble(WS(L - 2.6f, Yd(.88f) + 2.4f), "PROBINSYA", Art.C("#2A5DA8"), Art.C("#F0ECE0")); if (((int)time) % 4 == 0) Txt(WS(L - 4.5f, Yd(.9f) + 2.2f), "\"Probinsya! Aalis na!\"", Art.C("#F5E6B8"), 11); }
        foreach (var d in dogs) if (d.bark > 0) Txt(WS(d.x, Yd(d.d) + 1f), "AW AW!", Art.C("#E8467C"), 11);
        if (state == St.Play && inside == null)
        {
            string tag = hidden ? (peeking ? "sumisilip" : "nakatago") : blended ? "halo sa tao" : null;
            if (tag != null) Txt(WS(pl.x, HeadY(pl) + .35f), tag, Art.C("#3FB8AF"), 10);
            if (nearHouse != null) Txt(WS(nearHouse.x, Yd(1f) + 1.7f), nearHouse.locked ? "sarado" : "PASOK", nearHouse.locked ? Art.C("#9A96A0") : Art.C("#F2A541"), 10);
        }
        foreach (var g in goons)
        {
            float dist = Dist(g.a.x, g.a.d, pl.x, pl.d);
            if (blind && dist > 3.5f && state == St.Play) continue;
            Vector2 s = WS(g.a.x, HeadY(g.a) + .3f);
            if (g.st == "chase" || g.alert > .05f)
            {
                Txt(s, g.st == "chase" ? "!" : "?", g.st == "chase" ? Art.C("#E8467C") : Art.C("#F2A541"), 18);
                if (g.st != "chase") { var br = new Rect(s.x - 18 * S, s.y - 16 * S, 36 * S, 3 * S); Box(br, new Color(0, 0, 0, .5f)); br.width *= Mathf.Min(1, g.alert); Box(br, Art.C("#F2A541")); }
            }
            if (g.sayT > 0) Bubble(WS(g.a.x, HeadY(g.a) + 1.1f), g.say, new Color(.94f, .93f, .88f, .95f), Art.C("#16142A"));
        }
        foreach (var f in fx) if (f.txt != null) Txt(WS(f.x, HeadY(pl) + .6f + (1.4f - f.t) * .5f), f.txt, new Color(.96f, .9f, .72f, Mathf.Clamp01(f.t)), 12);

        if (state == St.Cutscene) { Cutscene(); return; }

        // inside-house tint
        if (state == St.Play && inside != null)
        {
            Box(new Rect(0, 0, Screen.width, Screen.height), new Color(.05f, .04f, .1f, .6f));
            string who = inside.mark ? "Nasa loob ka ng bahay mo." : "Nasa loob ka ng bahay ni " + inside.owner + ".";
            Txt(new Vector2(Screen.width / 2, Screen.height * .38f), who, Art.C("#F0ECE0"), 15);
            if (!inside.mark)
            {
                Txt(new Vector2(Screen.width / 2, Screen.height * .38f + 24 * S), inside.owner + ": \"Ay! Sino ka?! Lumabas ka!\"", Art.C("#F2A541"), 12);
                var br = new Rect(Screen.width / 2 - 90 * S, Screen.height * .38f + 44 * S, 180 * S, 5 * S); Box(br, new Color(0, 0, 0, .5f));
                br.width *= Mathf.Clamp01(1 - insideT / 8f); Box(br, Art.C("#E8467C"));
            }
        }
        if (hintT > 0 && state == St.Play)
        {
            sBody.alignment = TextAnchor.MiddleCenter;
            var hr = new Rect(Screen.width * .2f, Screen.height * .2f, Screen.width * .6f, 30 * S); Box(hr, new Color(.09f, .08f, .16f, .8f));
            GUI.Label(hr, hint, sBody); sBody.alignment = TextAnchor.UpperLeft;
        }

        // HUD
        Rect sa = Screen.safeArea; float left = sa.xMin + 10 * S, top = (Screen.height - sa.yMax) + 8 * S;
        var b1 = new Rect(left, top, 170 * S, 44 * S); Box(b1, new Color(.09f, .08f, .16f, .78f)); Box(new Rect(b1.x, b1.y, 3 * S, b1.height), Art.C("#F2A541"));
        GUI.Label(new Rect(b1.x + 10 * S, b1.y + 3 * S, 160 * S, 20 * S), "<b>" + stg.name + "</b>", sLabel);
        string mode = inside != null ? "Nasa loob" : stun > 0 ? "Nabangga!" : hidden ? (peeking ? "Sumisilip" : "Nakatago") : blended ? "Halo sa tao" : run ? "Tumatakbo - maingay" : pl.crouch ? "Nakayuko" : "Naglalakad";
        GUI.Label(new Rect(b1.x + 10 * S, b1.y + 21 * S, 160 * S, 20 * S), "<color=#F2A541>" + mode + "</color>", sLabel);
        var b2 = new Rect(b1.xMax + 8 * S, top, 130 * S, 44 * S); Box(b2, new Color(.09f, .08f, .16f, .78f));
        GUI.Label(new Rect(b2.x + 8 * S, b2.y + 3 * S, 130 * S, 20 * S), "Utang <color=#E8467C><b>₱" + debt.ToString("N0") + "</b></color>", sLabel);
        GUI.Label(new Rect(b2.x + 8 * S, b2.y + 21 * S, 130 * S, 20 * S), "Pera <color=#8FD49A><b>₱" + cash.ToString("N0") + "</b></color>", sLabel);
        var b3 = new Rect(b2.xMax + 8 * S, top, 120 * S, 44 * S); Box(b3, new Color(.09f, .08f, .16f, .78f));
        Bar(new Rect(b3.x + 8 * S, b3.y + 4 * S, 104 * S, 18 * S), "Stamina", stam / 100f, Art.C("#3FB8AF"));
        Bar(new Rect(b3.x + 8 * S, b3.y + 22 * S, 104 * S, 18 * S), "Gutom", hunger / 100f, hunger < 25 ? Art.C("#E8467C") : Art.C("#E0913A"));

        if (state == St.Play)
        {
            string act = inside != null ? "Labas" : nearHouse != null ? (nearHouse.locked ? "Sarado" : "Pasok") : hidden ? "Silip" : "-";
            Button(rTago, "Tago", pl.crouch); Button(rAct, act, peeking || inside != null);
            Button(rThrow, "Tansan x" + throws, false); Button(rRun, "Takbo", run);
            if (joyId >= 0)
            {
                float R = 60 * S;
                Box(new Rect(joyO.x - R, joyO.y - 1, R * 2, 2), new Color(1, 1, 1, .25f)); Box(new Rect(joyO.x - 1, joyO.y - R, 2, R * 2), new Color(1, 1, 1, .25f));
                Vector2 k = joyO + joyV * R; Box(new Rect(k.x - 18 * S, k.y - 18 * S, 36 * S, 36 * S), new Color(1, 1, 1, .3f));
            }
        }
        else Overlay();
    }

    void Cutscene()
    {
        var line = CUT[Mathf.Min(cutLine, CUT.Length - 1)];
        Actor speaker = line[0].StartsWith("BOSS") ? tib : line[0] == "KULOT" ? goons[0].a : null;
        if (speaker != null) Bubble(WS(speaker.x, HeadY(speaker) + .7f), "!", Art.C("#E8467C"), Art.C("#F0ECE0"));
        else Txt(WS(stg.houses[0].x, Yd(1f) + 2.4f), "( ... )", Art.C("#3FB8AF"), 14);
        if (cutLine <= 1) shake = Mathf.Max(shake, .1f);
        float h = 96 * S; var r = new Rect(20 * S, Screen.height - h - 16 * S, Screen.width - 40 * S, h);
        Box(r, new Color(.06f, .05f, .12f, .92f)); Box(new Rect(r.x, r.y, 4 * S, r.height), Art.C("#E8467C"));
        sLabel.fontSize = Mathf.RoundToInt(13 * S);
        GUI.Label(new Rect(r.x + 16 * S, r.y + 8 * S, r.width, 20 * S), "<color=#F2A541><b>" + line[0] + "</b></color>", sLabel);
        sBody.fontSize = Mathf.RoundToInt(16 * S);
        GUI.Label(new Rect(r.x + 16 * S, r.y + 30 * S, r.width - 32 * S, h - 36 * S), line[1], sBody);
        sBody.fontSize = Mathf.RoundToInt(14 * S);
        Txt(new Vector2(r.xMax - 70 * S, r.yMax - 14 * S), "tap ▸", new Color(1, 1, 1, .5f), 11);
        Button(rSkip, "Skip", false);
    }

    void Bar(Rect r, string label, float v, Color c)
    {
        sLabel.fontSize = Mathf.RoundToInt(10 * S); GUI.Label(new Rect(r.x, r.y - 2 * S, r.width, 14 * S), label, sLabel); sLabel.fontSize = Mathf.RoundToInt(13 * S);
        var bg = new Rect(r.x, r.y + 12 * S, r.width, 4 * S); Box(bg, new Color(0, 0, 0, .45f)); bg.width *= Mathf.Clamp01(v); Box(bg, c);
    }
    void Button(Rect r, string label, bool on)
    {
        Box(r, new Color(.09f, .08f, .16f, .8f));
        Color e = on ? Art.C("#F2A541") : new Color(1, 1, 1, .3f);
        Box(new Rect(r.x, r.y, r.width, 2 * S), e); Box(new Rect(r.x, r.yMax - 2 * S, r.width, 2 * S), e);
        Box(new Rect(r.x, r.y, 2 * S, r.height), e); Box(new Rect(r.xMax - 2 * S, r.y, 2 * S, r.height), e);
        sBtn.normal.textColor = on ? Art.C("#F2A541") : Art.C("#F0ECE0"); GUI.Label(r, label, sBtn);
    }
    void Overlay()
    {
        Box(new Rect(0, 0, Screen.width, Screen.height), new Color(.055f, .047f, .12f, .86f));
        float w = Mathf.Min(620 * S, Screen.width - 60 * S), x = (Screen.width - w) / 2;
        float th = sTitle.CalcHeight(new GUIContent(ovTitle), w), bh = sBody.CalcHeight(new GUIContent(ovBody), w);
        float y = (Screen.height - (th + 12 * S + bh + 20 * S + 46 * S)) / 2;
        var shadow = new GUIStyle(sTitle); shadow.normal.textColor = Art.C("#E8467C");
        GUI.Label(new Rect(x + 3 * S, y + 3 * S, w, th), ovTitle, shadow); GUI.Label(new Rect(x, y, w, th), ovTitle, sTitle); y += th + 12 * S;
        GUI.Label(new Rect(x, y, w, bh), ovBody, sBody); y += bh + 20 * S;
        var br = new Rect(x, y, Mathf.Max(150 * S, sBtn.CalcSize(new GUIContent(ovBtn)).x + 40 * S), 46 * S);
        Box(br, Art.C("#E8467C")); sBtn.normal.textColor = Art.C("#F0ECE0"); GUI.Label(br, ovBtn, sBtn);
    }
}
