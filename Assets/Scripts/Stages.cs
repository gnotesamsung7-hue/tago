using System.Collections.Generic;

// Depth "d" runs from 0 (front, nearest the camera) to 1 (back, against the houses).
// With a road:  0-0.2 front sidewalk, 0.2-0.72 road, 0.72-1 back sidewalk.
public enum CoverKind { Drum, Crates, Bin, Stall, Trike, Jeep, Bus }

public class CoverDef { public CoverKind k; public float x, d; public CoverDef(CoverKind k, float x, float d) { this.k = k; this.x = x; this.d = d; } }
public class GoonDef { public float x, d, min, max; public bool boss; public GoonDef(float x, float d, float min, float max, bool boss = false) { this.x = x; this.d = d; this.min = min; this.max = max; this.boss = boss; } }
public class CrowdDef { public float x, d, speed, halfW; public int n; public CrowdDef(float x, float d, int n, float speed, float halfW = 1.1f) { this.x = x; this.d = d; this.n = n; this.speed = speed; this.halfW = halfW; } }
public class HouseDef { public float x; public int variant; public bool store, mark, terminal, locked; public string owner; }
public class ItemDef { public char k; public float x, d; public ItemDef(char k, float x, float d) { this.k = k; this.x = x; this.d = d; } }
public class BlockDef { public float x0, x1, d0, d1; public BlockDef(float a, float b, float c, float e) { x0 = a; x1 = b; d0 = c; d1 = e; } }

public class StageDef
{
    public string name, sub, exitSign;
    public float length, startX, startD;
    public bool road, terminal;
    public float[] lanes = new float[0];
    public bool busLane;
    public List<HouseDef> houses = new List<HouseDef>();
    public List<CoverDef> covers = new List<CoverDef>();
    public List<GoonDef> goons = new List<GoonDef>();
    public List<CrowdDef> crowds = new List<CrowdDef>();
    public List<ItemDef> items = new List<ItemDef>();
    public List<float[]> dogs = new List<float[]>();
    public List<BlockDef> blocks = new List<BlockDef>();
}

public static class Stages
{
    public const int Count = 3;
    public const float HouseW = 5f;
    static readonly string[] Owners = { "Aling Nena", "Mang Tonyo", "Aling Baby", "Kuya Jun", "Lola Carmen", "Aling Rosa", "Mang Boy", "Ate Joy", "Tita Luz", "Mang Ramon" };

    static void Row(StageDef s, System.Random r, int storeEvery, bool terminal, bool markFirst)
    {
        int i = 0;
        for (float x = HouseW / 2; x < s.length + HouseW * 3; x += HouseW, i++)
        {
            var h = new HouseDef { x = x, variant = r.Next(6) };
            if (terminal) h.terminal = true;
            else if (markFirst && i == 0) h.mark = true;
            else if (storeEvery > 0 && i % storeEvery == storeEvery - 1) h.store = true;
            h.locked = !h.mark && r.NextDouble() < .3;
            h.owner = Owners[r.Next(Owners.Length)];
            s.houses.Add(h);
        }
    }

    public static StageDef Build(int i)
    {
        var r = new System.Random(500 + i * 37);
        var s = new StageDef();
        if (i == 0)
        {
            s.name = "Eskinita, Tondo"; s.exitSign = "JUAN LUNA ->";
            s.sub = "Kulot is guarding your gate. Slip through the alley without being seen.";
            s.length = 72; s.startX = 2.5f; s.startD = .95f;
            Row(s, r, 5, false, true);
            CoverKind[] ks = { CoverKind.Drum, CoverKind.Crates, CoverKind.Bin, CoverKind.Drum, CoverKind.Stall };
            for (float x = 8; x < s.length - 5; x += 3.5f + (float)r.NextDouble() * 3f)
                s.covers.Add(new CoverDef(ks[r.Next(ks.Length)], x, r.NextDouble() < .5 ? .08f + (float)r.NextDouble() * .25f : .58f + (float)r.NextDouble() * .17f));
            s.crowds.Add(new CrowdDef(22, .45f, 3, 0)); s.crowds.Add(new CrowdDef(47, .3f, 4, 0)); s.crowds.Add(new CrowdDef(36, .7f, 3, 1.1f));
            s.goons.Add(new GoonDef(6.2f, .5f, 5, 16)); s.goons.Add(new GoonDef(31, .35f, 26, 39)); s.goons.Add(new GoonDef(55, .6f, 49, 63));
            s.dogs.Add(new[] { 26f, .15f }); s.dogs.Add(new[] { 43f, .8f });
            s.items.Add(new ItemDef('F', 14, .2f)); s.items.Add(new ItemDef('F', 38, .7f)); s.items.Add(new ItemDef('F', 58, .3f));
            s.items.Add(new ItemDef('P', 19, .75f)); s.items.Add(new ItemDef('P', 33, .1f)); s.items.Add(new ItemDef('P', 50, .45f)); s.items.Add(new ItemDef('P', 66, .2f));
        }
        else if (i == 1)
        {
            s.name = "Juan Luna, Tondo"; s.exitSign = "TERMINAL ->"; s.road = true;
            s.sub = "The main road. Cross through traffic, blend in with the crowd at the jeepney stop.";
            s.length = 96; s.startX = 1.5f; s.startD = .85f;
            s.lanes = new[] { .3f, .46f, .62f };
            Row(s, r, 3, false, false);
            for (float x = 6; x < s.length - 4; x += 4f + (float)r.NextDouble() * 4f)
            {
                bool front = r.NextDouble() < .5;
                CoverKind k = front ? (r.NextDouble() < .35 ? CoverKind.Stall : (r.NextDouble() < .5 ? CoverKind.Crates : CoverKind.Bin)) : (r.NextDouble() < .4 ? CoverKind.Trike : CoverKind.Drum);
                float d = front ? .06f + (float)r.NextDouble() * .1f : .76f + (float)r.NextDouble() * .1f;
                if (front && x > 38 && x < 49) continue;
                s.covers.Add(new CoverDef(k, x, d));
            }
            s.blocks.Add(new BlockDef(40, 47, 0, .2f));
            s.crowds.Add(new CrowdDef(30, .82f, 6, 0, 1.6f)); s.crowds.Add(new CrowdDef(15, .1f, 4, 1.3f)); s.crowds.Add(new CrowdDef(62, .84f, 4, -1.2f)); s.crowds.Add(new CrowdDef(72, .1f, 3, 1.0f));
            s.goons.Add(new GoonDef(12, .1f, 8, 20)); s.goons.Add(new GoonDef(27, .86f, 23, 35)); s.goons.Add(new GoonDef(52, .86f, 46, 58));
            s.goons.Add(new GoonDef(62, .12f, 55, 71)); s.goons.Add(new GoonDef(82, .85f, 75, 89));
            s.dogs.Add(new[] { 21f, .86f });
            s.items.Add(new ItemDef('F', 18, .12f)); s.items.Add(new ItemDef('F', 57, .82f)); s.items.Add(new ItemDef('F', 84, .1f));
            s.items.Add(new ItemDef('P', 9, .85f)); s.items.Add(new ItemDef('P', 36, .12f)); s.items.Add(new ItemDef('P', 46, .46f)); s.items.Add(new ItemDef('P', 77, .15f));
        }
        else
        {
            s.name = "Bus Terminal"; s.exitSign = ""; s.road = true; s.terminal = true; s.busLane = true;
            s.sub = "The last bus to the province is boarding. Bossing Tiburon is here himself.";
            s.length = 86; s.startX = 1.5f; s.startD = .85f;
            s.lanes = new[] { .4f };
            Row(s, r, 0, true, false);
            s.covers.Add(new CoverDef(CoverKind.Bus, 18, .58f)); s.covers.Add(new CoverDef(CoverKind.Bus, 35, .58f)); s.covers.Add(new CoverDef(CoverKind.Bus, 53, .58f)); s.covers.Add(new CoverDef(CoverKind.Jeep, 68, .58f));
            for (float x = 7; x < s.length - 8; x += 5f + (float)r.NextDouble() * 3f)
            {
                bool front = r.NextDouble() < .5;
                s.covers.Add(new CoverDef(front ? (r.NextDouble() < .5 ? CoverKind.Crates : CoverKind.Stall) : (r.NextDouble() < .5 ? CoverKind.Bin : CoverKind.Drum), x, front ? .07f + (float)r.NextDouble() * .1f : .78f + (float)r.NextDouble() * .1f));
            }
            s.crowds.Add(new CrowdDef(21, .8f, 5, 0, 1.4f)); s.crowds.Add(new CrowdDef(40, .12f, 5, 0, 1.4f)); s.crowds.Add(new CrowdDef(60, .82f, 6, 0, 1.6f)); s.crowds.Add(new CrowdDef(30, .15f, 3, .9f));
            s.goons.Add(new GoonDef(14, .15f, 9, 22)); s.goons.Add(new GoonDef(28, .84f, 24, 34)); s.goons.Add(new GoonDef(45, .15f, 40, 52));
            s.goons.Add(new GoonDef(58, .84f, 52, 64)); s.goons.Add(new GoonDef(70, .3f, 64, 76)); s.goons.Add(new GoonDef(79, .7f, 74, 83, true));
            s.items.Add(new ItemDef('F', 24, .14f)); s.items.Add(new ItemDef('F', 63, .14f));
            s.items.Add(new ItemDef('P', 11, .82f)); s.items.Add(new ItemDef('P', 43, .84f)); s.items.Add(new ItemDef('P', 72, .12f));
        }
        return s;
    }
}
