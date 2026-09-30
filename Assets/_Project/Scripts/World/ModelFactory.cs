using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>
    /// Builds bright low-poly placeholder models from Unity primitives so the game is playable
    /// without imported art. Replace individual builders with real prefabs later.
    /// </summary>
    public static class ModelFactory
    {
        public static class Palette
        {
            public static readonly Color Jersey = new Color(0.10f, 0.25f, 0.75f);
            public static readonly Color JerseyTrim = new Color(1f, 0.85f, 0.1f);
            public static readonly Color Skin = new Color(0.93f, 0.72f, 0.55f);
            public static readonly Color Pants = new Color(0.95f, 0.95f, 0.95f);
            public static readonly Color Football = new Color(0.55f, 0.28f, 0.12f);
            public static readonly Color Lane = new Color(0.45f, 0.85f, 0.40f);
            public static readonly Color Fence = new Color(1f, 1f, 1f);
            public static readonly Color Wood = new Color(0.55f, 0.36f, 0.2f);
            public static readonly Color Leaf = new Color(0.2f, 0.62f, 0.25f);
            public static readonly Color Metal = new Color(0.65f, 0.68f, 0.72f);
            public static readonly Color Orange = new Color(1f, 0.5f, 0.1f);
            public static readonly Color Red = new Color(0.9f, 0.2f, 0.2f);
            public static readonly Color Pink = new Color(1f, 0.45f, 0.7f);
            public static readonly Color Yellow = new Color(1f, 0.9f, 0.2f);
            public static readonly Color Purple = new Color(0.55f, 0.3f, 0.85f);
            public static readonly Color Cyan = new Color(0.2f, 0.85f, 0.95f);
        }

        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        static Material _base;

        public static Material Mat(Color c)
        {
            if (Materials.TryGetValue(c, out var m) && m != null) return m;
            if (_base == null)
            {
                // Clone the built-in primitive material so the shader is guaranteed to be in builds.
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _base = tmp.GetComponent<Renderer>().sharedMaterial;
                Object.DestroyImmediate(tmp);
            }
            m = new Material(_base) { color = c, enableInstancing = true };
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            Materials[c] = m;
            return m;
        }

        public static GameObject Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.transform.localEulerAngles = euler;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat(color);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        public static GameObject Root(string name)
        {
            return new GameObject(name);
        }

        public static BoxCollider AddTrigger(GameObject go, Vector3 center, Vector3 size)
        {
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = center;
            col.size = size;
            return col;
        }

        // ---------- Player ----------

        public class KidRig
        {
            public Transform Root, Body, Head, LegL, LegR, ArmL, ArmR, Ball, Helmet;
        }

        /// <summary>What the kid wears: career-stage uniform + every piece of gear he has bought.</summary>
        public struct KidLook
        {
            public float Scale;
            public Color Jersey, Trim, Pants;
            public bool FootballHelmet;
            public int Pads, Cleats, Gloves, Juke;

            public static KidLook From(CareerStage stage, SaveData save)
            {
                return new KidLook
                {
                    Scale = stage.PlayerScale,
                    Jersey = stage.Jersey,
                    Trim = stage.Trim,
                    Pants = stage.Pants,
                    FootballHelmet = stage.FootballHelmet,
                    Pads = save.GetGearLevel("pads"),
                    Cleats = save.GetGearLevel("cleats"),
                    Gloves = save.GetGearLevel("gloves"),
                    Juke = save.GetGearLevel("juke"),
                };
            }
        }

        public static KidRig BuildKid(Transform parent, KidLook look)
        {
            var rig = new KidRig();
            var root = Root("KidVisual").transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * look.Scale;
            rig.Root = root;

            // --- torso + "44" on the back ---
            rig.Body = Part(PrimitiveType.Cube, root, new Vector3(0, 0.95f, 0), new Vector3(0.62f, 0.62f, 0.38f), look.Jersey).transform;
            Part(PrimitiveType.Cube, rig.Body, new Vector3(-0.18f, 0.05f, -0.51f), new Vector3(0.12f, 0.55f, 0.02f), look.Trim);
            Part(PrimitiveType.Cube, rig.Body, new Vector3(0.18f, 0.05f, -0.51f), new Vector3(0.12f, 0.55f, 0.02f), look.Trim);

            // --- head: backwards cap in the backyard, real football helmet from youth league on ---
            rig.Head = Part(PrimitiveType.Sphere, root, new Vector3(0, 1.48f, 0), Vector3.one * 0.42f, Palette.Skin).transform;
            if (look.FootballHelmet)
            {
                Part(PrimitiveType.Sphere, rig.Head, new Vector3(0, 0.08f, -0.02f), Vector3.one * 1.14f, look.Jersey);
                Part(PrimitiveType.Sphere, rig.Head, new Vector3(0, 0.1f, -0.02f), new Vector3(0.16f, 1.17f, 1.17f), look.Trim);         // centre stripe
                Part(PrimitiveType.Cube, rig.Head, new Vector3(0, -0.12f, 0.55f), new Vector3(0.8f, 0.07f, 0.07f), Palette.Metal);         // face mask bars
                Part(PrimitiveType.Cube, rig.Head, new Vector3(0, -0.3f, 0.5f), new Vector3(0.7f, 0.07f, 0.07f), Palette.Metal);
            }
            else
            {
                Part(PrimitiveType.Sphere, rig.Head, new Vector3(0, 0.25f, -0.05f), new Vector3(1.05f, 0.55f, 1.05f), Palette.Wood);       // hair
                Part(PrimitiveType.Sphere, rig.Head, new Vector3(0, 0.33f, 0f), new Vector3(1.04f, 0.5f, 1.04f), look.Jersey);            // cap
                Part(PrimitiveType.Cube, rig.Head, new Vector3(0, 0.32f, -0.6f), new Vector3(0.6f, 0.06f, 0.4f), look.Jersey);             // brim, worn backwards
            }

            // --- shoulder pads: bulkier with every level ---
            if (look.Pads > 0)
            {
                float lvl = look.Pads;
                var padSize = new Vector3(0.3f + 0.03f * lvl, 0.14f + 0.02f * lvl, 0.46f + 0.03f * lvl);
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (0.28f + 0.015f * lvl);
                    Part(PrimitiveType.Cube, root, new Vector3(x, 1.3f, 0), padSize, look.Jersey, new Vector3(0, 0, -side * 12f));
                    Part(PrimitiveType.Cube, root, new Vector3(x, 1.3f + padSize.y * 0.5f, 0), new Vector3(padSize.x * 0.8f, 0.03f, padSize.z * 1.02f), look.Trim, new Vector3(0, 0, -side * 12f));
                }
                if (look.Pads >= 3) // chest + back plates
                {
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.1f, 0.2f), new Vector3(0.6f, 0.3f, 0.08f), look.Jersey * 0.9f);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.1f, -0.2f), new Vector3(0.6f, 0.3f, 0.08f), look.Jersey * 0.9f);
                }
                if (look.Pads >= 5) // neck roll
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 1.32f, 0), new Vector3(0.42f, 0.05f, 0.34f), look.Trim);
            }

            // --- legs + shoes / cleats ---
            rig.LegL = Pivot(root, "LegL", new Vector3(-0.15f, 0.65f, 0));
            rig.LegR = Pivot(root, "LegR", new Vector3(0.15f, 0.65f, 0));
            Color shoe = look.Cleats == 0 ? new Color(0.9f, 0.9f, 0.92f) : (look.Cleats >= 3 ? look.Trim : new Color(0.1f, 0.1f, 0.1f));
            foreach (var leg in new[] { rig.LegL, rig.LegR })
            {
                Part(PrimitiveType.Cube, leg, new Vector3(0, -0.3f, 0), new Vector3(0.2f, 0.6f, 0.22f), look.Pants);
                Part(PrimitiveType.Cube, leg, new Vector3(0, -0.6f, 0.05f), new Vector3(0.22f, 0.1f, 0.34f), shoe);
                if (look.Cleats > 0)
                {
                    Part(PrimitiveType.Cube, leg, new Vector3(0.112f, -0.59f, 0.05f), new Vector3(0.01f, 0.04f, 0.26f), look.Cleats >= 3 ? Color.white : look.Trim); // stripe
                    for (int i = 0; i < 3; i++)
                        Part(PrimitiveType.Cube, leg, new Vector3(0, -0.665f, -0.07f + i * 0.1f), new Vector3(0.14f, 0.04f, 0.04f), Palette.Metal); // studs
                }
            }

            // --- arms, gloves, wristbands ---
            rig.ArmL = Pivot(root, "ArmL", new Vector3(-0.4f, 1.2f, 0));
            rig.ArmR = Pivot(root, "ArmR", new Vector3(0.4f, 1.2f, 0));
            Color hand = look.Gloves > 0 ? (look.Gloves >= 3 ? look.Trim : look.Jersey * 0.8f) : Palette.Skin;
            float handSize = look.Gloves >= 3 ? 0.19f : 0.15f;
            foreach (var arm in new[] { rig.ArmL, rig.ArmR })
            {
                Part(PrimitiveType.Cube, arm, new Vector3(0, -0.25f, 0), new Vector3(0.16f, 0.5f, 0.16f), look.Jersey);
                Part(PrimitiveType.Sphere, arm, new Vector3(0, -0.52f, 0), Vector3.one * handSize, hand);
                if (look.Juke > 0)
                    Part(PrimitiveType.Cube, arm, new Vector3(0, -0.42f, 0), new Vector3(0.18f, 0.06f + 0.01f * look.Juke, 0.18f), look.Trim);
            }

            // football tucked under the right arm
            rig.Ball = Part(PrimitiveType.Sphere, rig.ArmR, new Vector3(0.05f, -0.4f, 0.12f), new Vector3(0.2f, 0.2f, 0.34f), Palette.Football).transform;

            // Helmet power-up: golden shell over whatever is on the head
            rig.Helmet = Part(PrimitiveType.Sphere, rig.Head, new Vector3(0, 0.08f, 0), Vector3.one * 1.3f, Palette.JerseyTrim).transform;
            rig.Helmet.gameObject.SetActive(false);
            return rig;
        }

        static Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        // ---------- Obstacles ----------

        public static void BuildObstacleVisual(ObstacleKind kind, Transform root)
        {
            switch (kind)
            {
                case ObstacleKind.TrashCan:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.5f, 0.8f), Palette.Metal);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 1.03f, 0), new Vector3(0.9f, 0.04f, 0.9f), Palette.Metal * 0.8f);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.1f, 0), new Vector3(0.3f, 0.06f, 0.08f), Palette.Metal * 0.7f);
                    break;
                case ObstacleKind.Cone:
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.03f, 0), new Vector3(0.6f, 0.06f, 0.6f), Palette.Orange);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.35f, 0), new Vector3(0.35f, 0.3f, 0.35f), Palette.Orange);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.45f, 0), new Vector3(0.36f, 0.06f, 0.36f), Palette.Fence);
                    break;
                case ObstacleKind.Gnome:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.3f, 0), new Vector3(0.45f, 0.3f, 0.45f), Palette.Cyan);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.7f, 0), Vector3.one * 0.35f, Palette.Skin);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.6f, 0.12f), new Vector3(0.3f, 0.25f, 0.2f), Palette.Fence); // beard
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.98f, 0), new Vector3(0.25f, 0.35f, 0.25f), Palette.Red, new Vector3(0, 45, 0));
                    break;
                case ObstacleKind.Wheelbarrow:
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.55f, 0), new Vector3(0.9f, 0.35f, 1.1f), Palette.Red);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.25f, 0.55f), new Vector3(0.45f, 0.06f, 0.45f), Palette.Metal * 0.4f, new Vector3(0, 0, 90));
                    Part(PrimitiveType.Cube, root, new Vector3(-0.35f, 0.5f, -0.8f), new Vector3(0.06f, 0.06f, 0.7f), Palette.Wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0.35f, 0.5f, -0.8f), new Vector3(0.06f, 0.06f, 0.7f), Palette.Wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0.2f, 0.95f, 0.1f), new Vector3(0.05f, 0.9f, 0.05f), Palette.Wood, new Vector3(15, 0, 10)); // rake handle
                    break;
                case ObstacleKind.LawnMower:
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.4f, 1f), Palette.Yellow);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 0.6f, 0.1f), new Vector3(0.4f, 0.12f, 0.4f), Palette.Metal * 0.5f);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.85f, -0.6f), new Vector3(0.8f, 0.06f, 0.06f), Palette.Metal * 0.5f);
                    Part(PrimitiveType.Cube, root, new Vector3(-0.38f, 0.6f, -0.45f), new Vector3(0.05f, 0.5f, 0.05f), Palette.Metal * 0.5f, new Vector3(-30, 0, 0));
                    Part(PrimitiveType.Cube, root, new Vector3(0.38f, 0.6f, -0.45f), new Vector3(0.05f, 0.5f, 0.05f), Palette.Metal * 0.5f, new Vector3(-30, 0, 0));
                    break;
                case ObstacleKind.TelephonePost:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0, 2.5f, 0), new Vector3(0.35f, 2.5f, 0.35f), Palette.Wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 4.4f, 0), new Vector3(1.8f, 0.15f, 0.15f), Palette.Wood);
                    Part(PrimitiveType.Sphere, root, new Vector3(-0.7f, 4.55f, 0), Vector3.one * 0.15f, Palette.Cyan);
                    Part(PrimitiveType.Sphere, root, new Vector3(0.7f, 4.55f, 0), Vector3.one * 0.15f, Palette.Cyan);
                    break;
                case ObstacleKind.Flamingo:
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.9f, 0), new Vector3(0.45f, 0.35f, 0.6f), Palette.Pink);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 1.25f, 0.2f), new Vector3(0.07f, 0.55f, 0.07f), Palette.Pink, new Vector3(-10, 0, 0));
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 1.55f, 0.28f), Vector3.one * 0.18f, Palette.Pink);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.4f, 0), new Vector3(0.04f, 0.8f, 0.04f), Palette.Metal * 0.5f);
                    break;
                case ObstacleKind.Chicken:
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.35f, 0), new Vector3(0.4f, 0.38f, 0.5f), Palette.Fence);
                    Part(PrimitiveType.Sphere, root, new Vector3(0, 0.62f, 0.2f), Vector3.one * 0.22f, Palette.Fence);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.62f, 0.33f), new Vector3(0.06f, 0.05f, 0.1f), Palette.Orange);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.76f, 0.2f), new Vector3(0.04f, 0.1f, 0.12f), Palette.Red);
                    break;
                case ObstacleKind.Dog:
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.45f, 0), new Vector3(0.4f, 0.35f, 0.8f), Palette.Wood * 1.3f);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.7f, 0.45f), new Vector3(0.32f, 0.3f, 0.32f), Palette.Wood * 1.3f);
                    Part(PrimitiveType.Cube, root, new Vector3(-0.14f, 0.9f, 0.4f), new Vector3(0.08f, 0.14f, 0.06f), Palette.Wood);
                    Part(PrimitiveType.Cube, root, new Vector3(0.14f, 0.9f, 0.4f), new Vector3(0.08f, 0.14f, 0.06f), Palette.Wood);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = i % 2 == 0 ? -0.14f : 0.14f;
                        float z = i < 2 ? 0.28f : -0.28f;
                        Part(PrimitiveType.Cube, root, new Vector3(x, 0.14f, z), new Vector3(0.09f, 0.28f, 0.09f), Palette.Wood * 1.3f);
                    }
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.6f, -0.45f), new Vector3(0.06f, 0.06f, 0.25f), Palette.Wood * 1.3f, new Vector3(-35, 0, 0));
                    break;
            }
        }

        // ---------- Enemies ----------

        public static Transform[] BuildPerson(EnemyKind kind, Transform root)
        {
            Color shirt, pants, hat;
            bool hasHat = true;
            switch (kind)
            {
                case EnemyKind.Neighbor: shirt = new Color(0.95f, 0.55f, 0.15f); pants = new Color(0.3f, 0.35f, 0.5f); hat = new Color(0.95f, 0.95f, 0.8f); break; // bathrobe + curlers
                case EnemyKind.Teacher: shirt = Palette.Purple; pants = new Color(0.25f, 0.25f, 0.3f); hat = Palette.Purple; hasHat = false; break;
                case EnemyKind.OpposingPlayer: shirt = Palette.Red; pants = Palette.Fence; hat = Palette.Red; break;
                case EnemyKind.Police: shirt = new Color(0.1f, 0.15f, 0.4f); pants = new Color(0.1f, 0.12f, 0.3f); hat = new Color(0.1f, 0.12f, 0.3f); break;
                case EnemyKind.IceAgent: shirt = new Color(0.2f, 0.25f, 0.2f); pants = new Color(0.15f, 0.15f, 0.15f); hat = new Color(0.15f, 0.15f, 0.15f); break;
                default: shirt = new Color(0.12f, 0.12f, 0.12f); pants = new Color(0.12f, 0.12f, 0.12f); hat = new Color(0.12f, 0.12f, 0.12f); hasHat = false; break; // suits + shades
            }

            float scale = kind == EnemyKind.OpposingPlayer ? 1.05f : 1.15f; // adults are big
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            body.localScale = Vector3.one * scale;
            body.localEulerAngles = new Vector3(0, 180, 0); // face the player

            Part(PrimitiveType.Cube, body, new Vector3(0, 1.05f, 0), new Vector3(0.7f, 0.75f, 0.42f), shirt);
            Part(PrimitiveType.Sphere, body, new Vector3(0, 1.68f, 0), Vector3.one * 0.44f, Palette.Skin);
            if (hasHat) Part(PrimitiveType.Cylinder, body, new Vector3(0, 1.9f, 0), new Vector3(0.42f, 0.08f, 0.42f), hat);
            if (kind == EnemyKind.FedAgent || kind == EnemyKind.Teacher)
                Part(PrimitiveType.Cube, body, new Vector3(0, 1.72f, 0.2f), new Vector3(0.36f, 0.08f, 0.04f), Color.black); // glasses/shades

            var legL = Pivot(body, "LegL", new Vector3(-0.17f, 0.68f, 0));
            Part(PrimitiveType.Cube, legL, new Vector3(0, -0.34f, 0), new Vector3(0.22f, 0.68f, 0.24f), pants);
            var legR = Pivot(body, "LegR", new Vector3(0.17f, 0.68f, 0));
            Part(PrimitiveType.Cube, legR, new Vector3(0, -0.34f, 0), new Vector3(0.22f, 0.68f, 0.24f), pants);
            // arms reaching out to grab
            var armL = Pivot(body, "ArmL", new Vector3(-0.45f, 1.35f, 0));
            Part(PrimitiveType.Cube, armL, new Vector3(0, 0, 0.3f), new Vector3(0.18f, 0.18f, 0.6f), shirt);
            var armR = Pivot(body, "ArmR", new Vector3(0.45f, 1.35f, 0));
            Part(PrimitiveType.Cube, armR, new Vector3(0, 0, 0.3f), new Vector3(0.18f, 0.18f, 0.6f), shirt);
            return new[] { legL, legR, armL, armR };
        }

        // ---------- Pickups ----------

        public static void BuildPickupVisual(PickupKind kind, Transform root)
        {
            switch (kind)
            {
                case PickupKind.Football:
                    Part(PrimitiveType.Sphere, root, Vector3.zero, new Vector3(0.3f, 0.3f, 0.5f), Palette.Football);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.15f, 0), new Vector3(0.04f, 0.02f, 0.2f), Palette.Fence);
                    break;
                case PickupKind.Magnet:
                    Part(PrimitiveType.Cube, root, new Vector3(-0.2f, 0, 0), new Vector3(0.15f, 0.6f, 0.15f), Palette.Red);
                    Part(PrimitiveType.Cube, root, new Vector3(0.2f, 0, 0), new Vector3(0.15f, 0.6f, 0.15f), Palette.Red);
                    Part(PrimitiveType.Cube, root, new Vector3(0, 0.3f, 0), new Vector3(0.55f, 0.15f, 0.15f), Palette.Red);
                    Part(PrimitiveType.Cube, root, new Vector3(-0.2f, -0.3f, 0), new Vector3(0.16f, 0.12f, 0.16f), Palette.Metal);
                    Part(PrimitiveType.Cube, root, new Vector3(0.2f, -0.3f, 0), new Vector3(0.16f, 0.12f, 0.16f), Palette.Metal);
                    break;
                case PickupKind.Helmet:
                    Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 0.6f, Palette.JerseyTrim);
                    Part(PrimitiveType.Cube, root, new Vector3(0, -0.05f, 0.3f), new Vector3(0.4f, 0.06f, 0.06f), Palette.Metal);
                    break;
                case PickupKind.BeastMode:
                    Part(PrimitiveType.Cube, root, Vector3.zero, new Vector3(0.2f, 0.7f, 0.2f), Palette.Yellow, new Vector3(0, 0, 25));
                    Part(PrimitiveType.Cube, root, new Vector3(0.1f, 0.05f, 0), new Vector3(0.4f, 0.12f, 0.2f), Palette.Yellow);
                    break;
                case PickupKind.DoublePoints:
                    Part(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.6f, 0.06f, 0.6f), Palette.Cyan, new Vector3(90, 0, 0));
                    Part(PrimitiveType.Cube, root, new Vector3(-0.1f, 0, -0.05f), new Vector3(0.1f, 0.3f, 0.05f), Palette.Fence, new Vector3(0, 0, 30));
                    Part(PrimitiveType.Cube, root, new Vector3(-0.1f, 0, -0.05f), new Vector3(0.1f, 0.3f, 0.05f), Palette.Fence, new Vector3(0, 0, -30));
                    Part(PrimitiveType.Cube, root, new Vector3(0.14f, 0, -0.05f), new Vector3(0.08f, 0.25f, 0.05f), Palette.Fence);
                    break;
            }
        }

        // ---------- Scenery ----------

        public static void BuildGroundChunk(Transform root, float length, int variant, Color ground)
        {
            float lanesWidth = TrackSpawner.LaneWidth * 3f + 1f;
            Part(PrimitiveType.Cube, root, new Vector3(0, -0.1f, length * 0.5f), new Vector3(40f, 0.2f, length), ground * 0.9f);
            Part(PrimitiveType.Cube, root, new Vector3(0, -0.05f, length * 0.5f), new Vector3(lanesWidth, 0.2f, length), ground);
            // mowed stripes between lanes
            for (int i = -1; i <= 1; i += 2)
                Part(PrimitiveType.Cube, root, new Vector3(i * TrackSpawner.LaneWidth * 0.5f, 0.01f, length * 0.5f), new Vector3(0.15f, 0.1f, length), Palette.Lane);

            var rng = new System.Random(variant * 7919 + 13);
            // picket fences
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (lanesWidth * 0.5f + 1.2f);
                Part(PrimitiveType.Cube, root, new Vector3(x, 0.55f, length * 0.5f), new Vector3(0.08f, 0.1f, length), Palette.Fence);
                Part(PrimitiveType.Cube, root, new Vector3(x, 0.25f, length * 0.5f), new Vector3(0.08f, 0.1f, length), Palette.Fence);
                for (float z = 0.5f; z < length; z += 1.5f)
                    Part(PrimitiveType.Cube, root, new Vector3(x, 0.4f, z), new Vector3(0.1f, 0.8f, 0.3f), Palette.Fence);
            }

            // decor beyond the fences
            for (int i = 0; i < 6; i++)
            {
                int side = rng.Next(2) == 0 ? -1 : 1;
                float x = side * (lanesWidth * 0.5f + 3f + (float)rng.NextDouble() * 8f);
                float z = (float)rng.NextDouble() * length;
                switch (rng.Next(5))
                {
                    case 0: case 1: // tree
                        Part(PrimitiveType.Cylinder, root, new Vector3(x, 1f, z), new Vector3(0.4f, 1f, 0.4f), Palette.Wood);
                        Part(PrimitiveType.Sphere, root, new Vector3(x, 2.7f, z), Vector3.one * (2f + (float)rng.NextDouble()), Palette.Leaf);
                        break;
                    case 2: // bush with flowers
                        Part(PrimitiveType.Sphere, root, new Vector3(x, 0.5f, z), new Vector3(1.6f, 1f, 1.2f), Palette.Leaf * 1.1f);
                        Part(PrimitiveType.Sphere, root, new Vector3(x + 0.3f, 0.95f, z), Vector3.one * 0.25f, rng.Next(2) == 0 ? Palette.Pink : Palette.Yellow);
                        break;
                    case 3: // house
                        var houseColor = new[] { Palette.Cyan, Palette.Yellow, Palette.Pink, Palette.Purple }[rng.Next(4)];
                        float hx = side * (lanesWidth * 0.5f + 11f);
                        Part(PrimitiveType.Cube, root, new Vector3(hx, 2f, z), new Vector3(6f, 4f, 6f), houseColor);
                        Part(PrimitiveType.Cube, root, new Vector3(hx, 4.6f, z), new Vector3(4.5f, 4.5f, 6.2f), Palette.Red * 0.8f, new Vector3(0, 0, 45));
                        break;
                    case 4: // kiddie pool / swing set
                        if (rng.Next(2) == 0)
                        {
                            Part(PrimitiveType.Cylinder, root, new Vector3(x, 0.15f, z), new Vector3(2.4f, 0.15f, 2.4f), Palette.Cyan);
                        }
                        else
                        {
                            Part(PrimitiveType.Cube, root, new Vector3(x, 2.2f, z), new Vector3(0.12f, 0.12f, 3f), Palette.Red);
                            Part(PrimitiveType.Cube, root, new Vector3(x, 1.1f, z - 1.4f), new Vector3(0.12f, 2.2f, 0.12f), Palette.Red);
                            Part(PrimitiveType.Cube, root, new Vector3(x, 1.1f, z + 1.4f), new Vector3(0.12f, 2.2f, 0.12f), Palette.Red);
                            Part(PrimitiveType.Cube, root, new Vector3(x, 0.6f, z), new Vector3(0.4f, 0.05f, 0.5f), Palette.Yellow);
                        }
                        break;
                }
            }
        }
    }
}
