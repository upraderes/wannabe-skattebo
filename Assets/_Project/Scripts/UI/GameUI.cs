using System.Collections.Generic;
using UnityEngine;

namespace WannabeSkattebo
{
    /// <summary>
    /// All screens for the MVP, drawn with IMGUI in a 1080x1920 portrait reference space.
    /// Intentionally dependency-free; migrate to UI Toolkit once the art direction is settled.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        const float RefW = 1080f, RefH = 1920f;

        enum Panel { Main, Shop, Career }

        struct FloatingText
        {
            public Vector2 Pos;
            public string Text;
            public Color Color;
            public float Age;
        }

        GameManager _gm;
        Panel _panel;
        float _scale;
        Rect _safe;
        string _shopMessage;
        float _shopMessageTime;
        readonly List<FloatingText> _popups = new List<FloatingText>();

        GUIStyle _title, _h1, _h2, _body, _small, _button, _buttonAlt, _hudScore;
        Texture2D _tex;

        static readonly Color Ink = new Color(0.08f, 0.1f, 0.25f);
        static readonly Color Panel_ = new Color(0.05f, 0.08f, 0.2f, 0.82f);
        static readonly Color Gold = ModelFactory.Palette.JerseyTrim;

        void Start()
        {
            _gm = GameManager.Instance;
            _gm.Popup += OnPopup;
        }

        void OnDestroy()
        {
            if (_gm != null) _gm.Popup -= OnPopup;
        }

        void OnPopup(Vector3 world, string text, Color color)
        {
            var cam = Camera.main;
            if (cam == null || _scale <= 0f) return;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return;
            _popups.Add(new FloatingText
            {
                Pos = new Vector2(sp.x / _scale, (Screen.height - sp.y) / _scale),
                Text = text,
                Color = color,
            });
        }

        void Update()
        {
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                p.Age += Time.deltaTime;
                p.Pos.y -= 160f * Time.deltaTime;
                if (p.Age > 0.9f) _popups.RemoveAt(i); else _popups[i] = p;
            }
            if (_shopMessageTime > 0f) _shopMessageTime -= Time.deltaTime;
        }

        // ------------------------------------------------------------------

        void OnGUI()
        {
            if (_gm == null) return;
            EnsureStyles();

            _scale = Mathf.Min(Screen.width / RefW, Screen.height / RefH);
            GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));
            var sa = Screen.safeArea;
            _safe = new Rect(sa.x / _scale, (Screen.height - sa.yMax) / _scale, sa.width / _scale, sa.height / _scale);

            switch (_gm.State)
            {
                case GameState.Menu:
                    if (_panel == Panel.Main) DrawMenu();
                    else DrawPanel();
                    break;
                case GameState.Running:
                case GameState.Caught:
                    DrawHud();
                    break;
                case GameState.Paused:
                    DrawHud();
                    DrawPause();
                    break;
                case GameState.Results:
                    if (_panel == Panel.Main) DrawResults();
                    else DrawPanel();
                    break;
            }
        }

        Rect Column(out float x, out float w)
        {
            w = Mathf.Min(_safe.width - 80f, 960f);
            x = _safe.x + (_safe.width - w) * 0.5f;
            return new Rect(x, _safe.y, w, _safe.height);
        }

        // ---------------- Menu ----------------

        void DrawMenu()
        {
            Column(out float x, out float w);
            float y = _safe.y + 120f;

            Shadowed(new Rect(x, y, w, 150f), "WANNABE", _title, Color.white);
            y += 140f;
            Shadowed(new Rect(x, y, w, 150f), "SKATTEBO", _title, Gold);
            y += 170f;
            Label(new Rect(x, y, w, 70f), $"Road to #44  -  Career: {_gm.CareerStage.Name}", _h2, Color.white);
            y += 110f;

            Fill(new Rect(x, y, w, 250f), Panel_);
            Label(new Rect(x, y + 20f, w, 70f), _gm.Stage.Name, _h1, Gold);
            Label(new Rect(x + 30f, y + 95f, w - 60f, 60f), _gm.Stage.Tagline, _body, Color.white);
            Label(new Rect(x + 30f, y + 165f, w - 60f, 60f), $"Points {_gm.Save.Wallet:N0}     Best {_gm.Save.BestScore:N0}", _body, Color.white);
            y += 300f;

            if (Button(new Rect(x, y, w, 190f), "PLAY", _button)) _gm.StartRun();
            y += 230f;
            float half = (w - 30f) * 0.5f;
            if (Button(new Rect(x, y, half, 140f), "GEAR", _buttonAlt)) _panel = Panel.Shop;
            if (Button(new Rect(x + half + 30f, y, half, 140f), "CAREER", _buttonAlt)) _panel = Panel.Career;

            float bottom = _safe.yMax - 230f;
            Label(new Rect(x, bottom, w, 200f),
                "Swipe LEFT / RIGHT to change lanes, UP to jump, DOWN to slide.\nSmash everything in your way. Don't get caught!",
                _small, Color.white);
        }

        // ---------------- HUD ----------------

        void DrawHud()
        {
            float left = _safe.x + 40f;
            float top = _safe.y + 30f;

            Shadowed(new Rect(left, top, 700f, 120f), _gm.Score.Score.ToString("N0"), _hudScore, Color.white, TextAnchor.UpperLeft);
            Shadowed(new Rect(left, top + 115f, 700f, 70f), $"{Mathf.FloorToInt(_gm.Distance)} m", _h2, Color.white, TextAnchor.UpperLeft);

            if (_gm.State == GameState.Running &&
                Button(new Rect(_safe.xMax - 170f, top, 130f, 130f), "II", _buttonAlt))
            {
                _gm.Pause();
            }

            if (_gm.Score.Combo > 1)
            {
                Shadowed(new Rect(_safe.x, top + 200f, _safe.width, 110f), $"x{_gm.Score.Combo} COMBO!", _h1, Gold);
            }

            // active power-ups with timer bars
            float py = top + 200f + (_gm.Score.Combo > 1 ? 110f : 0f);
            foreach (var kv in _gm.ActivePowerUps)
            {
                float frac = kv.Value / PowerUpInfo.Duration(kv.Key);
                Fill(new Rect(left, py, 420f, 64f), Panel_);
                Fill(new Rect(left, py + 54f, 420f * frac, 10f), ModelFactory.Palette.Cyan);
                Label(new Rect(left + 20f, py, 400f, 54f), PowerUpInfo.Label(kv.Key), _small, Color.white, TextAnchor.MiddleLeft);
                py += 80f;
            }

            if (_gm.State == GameState.Running && _gm.RunTime < 3.5f)
            {
                Shadowed(new Rect(_safe.x, _safe.center.y + 250f, _safe.width, 200f),
                    "SMASH the junk!\nDODGE the grown-ups!", _h1, Color.white);
            }

            if (_gm.State == GameState.Caught)
            {
                Shadowed(new Rect(_safe.x, _safe.center.y - 200f, _safe.width, 160f), "CAUGHT!", _title, ModelFactory.Palette.Red);
            }

            foreach (var p in _popups)
            {
                var c = p.Color;
                c.a = 1f - Mathf.Clamp01((p.Age - 0.5f) / 0.4f);
                Shadowed(new Rect(p.Pos.x - 400f, p.Pos.y - 50f, 800f, 100f), p.Text, _h2, c);
            }
        }

        void DrawPause()
        {
            Fill(new Rect(0, 0, Screen.width / _scale, Screen.height / _scale), new Color(0, 0, 0, 0.55f));
            Column(out float x, out float w);
            float y = _safe.center.y - 300f;
            Shadowed(new Rect(x, y, w, 150f), "PAUSED", _title, Color.white);
            y += 220f;
            if (Button(new Rect(x, y, w, 170f), "RESUME", _button)) _gm.Resume();
            y += 210f;
            if (Button(new Rect(x, y, w, 140f), "END RUN & BANK POINTS", _buttonAlt)) _gm.QuitRun();
        }

        // ---------------- Results ----------------

        void DrawResults()
        {
            var r = _gm.LastRun;
            if (r == null) return;
            Column(out float x, out float w);
            float y = _safe.y + 110f;

            string headline = r.CaughtBy.HasValue ? $"Caught by {Enemy.DisplayName(r.CaughtBy.Value)}!" : "Run over";
            Shadowed(new Rect(x, y, w, 120f), headline, _h1, Color.white);
            y += 150f;

            Fill(new Rect(x, y, w, 560f), Panel_);
            Label(new Rect(x, y + 30f, w, 60f), "SCORE", _h2, Color.white);
            Label(new Rect(x, y + 90f, w, 160f), r.Score.ToString("N0"), _title, Gold);
            if (r.NewBest) Label(new Rect(x, y + 245f, w, 60f), "NEW PERSONAL BEST!", _h2, ModelFactory.Palette.Cyan);
            Label(new Rect(x, y + 330f, w, 60f), $"{r.Distance} m run     {r.Smashes} things smashed", _body, Color.white);
            Label(new Rect(x, y + 400f, w, 60f), $"+{r.BankedPoints:N0} points banked  (wallet {_gm.Save.Wallet:N0})", _body, Color.white);
            Label(new Rect(x, y + 470f, w, 60f), KaizenTip(r), _small, Color.white);
            y += 600f;

            if (r.UnlockedStage)
            {
                var stage = GameCatalog.Stages[_gm.Save.HighestStageUnlocked];
                Fill(new Rect(x, y, w, 120f), Gold);
                Label(new Rect(x, y, w, 120f), $"UNLOCKED: {stage.Name}!", _h1, Ink);
                y += 150f;
            }

            if (Button(new Rect(x, y, w, 180f), "RUN AGAIN", _button)) _gm.StartRun();
            y += 215f;
            float half = (w - 30f) * 0.5f;
            if (Button(new Rect(x, y, half, 140f), "GEAR", _buttonAlt)) _panel = Panel.Shop;
            if (Button(new Rect(x + half + 30f, y, half, 140f), "MENU", _buttonAlt)) _gm.GoToMenu();
        }

        string KaizenTip(RunResult r)
        {
            var next = _gm.Save.HighestStageUnlocked + 1 < GameCatalog.Stages.Count
                ? GameCatalog.Stages[_gm.Save.HighestStageUnlocked + 1] : null;
            if (next == null) return "You wear #44. Keep getting 1% better every run.";
            var current = GameCatalog.Stages[_gm.Save.HighestStageUnlocked];
            int scoreGap = current.GoalScore - _gm.Save.BestScore;
            int gearGap = next.RequiredGearLevels - _gm.Save.TotalGearLevels();
            if (scoreGap > 0) return $"{scoreGap:N0} more best-score points to reach {next.Name}.";
            if (gearGap > 0) return $"Buy {gearGap} more gear level(s) to reach {next.Name}.";
            return "Get 1% better every run.";
        }

        // ---------------- Shop / Career ----------------

        void DrawPanel()
        {
            Fill(new Rect(0, 0, Screen.width / _scale, Screen.height / _scale), new Color(0.05f, 0.08f, 0.2f, 0.9f));
            if (_panel == Panel.Shop) DrawShop(); else DrawCareer();
        }

        void DrawShop()
        {
            Column(out float x, out float w);
            float y = _safe.y + 60f;
            Label(new Rect(x, y, w, 120f), "GEAR SHOP", _title, Gold);
            y += 150f;
            Label(new Rect(x, y, w, 70f), $"Points: {_gm.Save.Wallet:N0}", _h2, Color.white);
            y += 100f;

            foreach (var item in GameCatalog.Gear)
            {
                int level = _gm.Save.GetGearLevel(item.Id);
                int cost = Economy.CostForNextLevel(item, level);

                Fill(new Rect(x, y, w, 250f), new Color(1f, 1f, 1f, 0.08f));
                Label(new Rect(x + 30f, y + 20f, w - 360f, 70f), item.Name, _h2, Color.white, TextAnchor.MiddleLeft);
                Label(new Rect(x + 30f, y + 90f, w - 360f, 60f), item.Description, _small, Color.white, TextAnchor.MiddleLeft);
                for (int i = 0; i < item.MaxLevel; i++)
                    Fill(new Rect(x + 30f + i * 70f, y + 175f, 55f, 40f), i < level ? Gold : new Color(1f, 1f, 1f, 0.2f));

                var btnRect = new Rect(x + w - 310f, y + 55f, 280f, 140f);
                if (cost < 0)
                {
                    Label(btnRect, "MAX", _h2, Gold);
                }
                else
                {
                    GUI.enabled = _gm.Save.Wallet >= cost;
                    if (Button(btnRect, $"{cost:N0}", _button))
                    {
                        var result = _gm.BuyGear(item.Id);
                        _shopMessage = result == PurchaseResult.Ok ? $"{item.Name} upgraded!" : "Not enough points";
                        _shopMessageTime = 1.5f;
                    }
                    GUI.enabled = true;
                }
                y += 280f;
            }

            if (_shopMessageTime > 0f) Label(new Rect(x, y, w, 70f), _shopMessage, _h2, Gold);
            if (Button(new Rect(x, _safe.yMax - 200f, w, 150f), "BACK", _buttonAlt)) _panel = Panel.Main;
        }

        void DrawCareer()
        {
            Column(out float x, out float w);
            float y = _safe.y + 60f;
            Label(new Rect(x, y, w, 120f), "CAREER", _title, Gold);
            y += 130f;
            Label(new Rect(x, y, w, 60f), "Backyard kid to #44. One run at a time.", _body, Color.white);
            y += 100f;

            var stages = GameCatalog.Stages;
            for (int i = 0; i < stages.Count; i++)
            {
                var s = stages[i];
                bool unlocked = i <= _gm.Save.HighestStageUnlocked;
                Fill(new Rect(x, y, w, 210f), unlocked ? new Color(1f, 0.85f, 0.1f, 0.25f) : new Color(1f, 1f, 1f, 0.06f));
                Fill(new Rect(x, y, 16f, 210f), unlocked ? Gold : new Color(1f, 1f, 1f, 0.2f));
                Label(new Rect(x + 50f, y + 15f, w - 80f, 70f), $"{i + 1}. {s.Name}", _h2, unlocked ? Color.white : new Color(1, 1, 1, 0.5f), TextAnchor.MiddleLeft);

                string status;
                if (unlocked && s.Playable) status = $"Goal: best score {s.GoalScore:N0}";
                else if (unlocked) status = "Unlocked! Coming in a future update";
                else
                {
                    var prev = stages[i - 1];
                    status = $"Needs best {prev.GoalScore:N0} and {s.RequiredGearLevels} gear levels";
                }
                Label(new Rect(x + 50f, y + 90f, w - 80f, 60f), status, _small, Color.white, TextAnchor.MiddleLeft);
                Label(new Rect(x + 50f, y + 140f, w - 80f, 60f), s.Tagline, _small, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleLeft);
                y += 235f;
            }

            if (Button(new Rect(x, _safe.yMax - 200f, w, 150f), "BACK", _buttonAlt)) _panel = Panel.Main;
        }

        // ---------------- Helpers ----------------

        void Fill(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _tex);
            GUI.color = prev;
        }

        static void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var prevColor = style.normal.textColor;
            var prevAnchor = style.alignment;
            style.normal.textColor = color;
            style.alignment = anchor;
            GUI.Label(r, text, style);
            style.normal.textColor = prevColor;
            style.alignment = prevAnchor;
        }

        static void Shadowed(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var shadow = new Color(0, 0, 0, 0.45f * color.a);
            Label(new Rect(r.x + 5f, r.y + 6f, r.width, r.height), text, style, shadow, anchor);
            Label(r, text, style, color, anchor);
        }

        static bool Button(Rect r, string text, GUIStyle style) => GUI.Button(r, text, style);

        void EnsureStyles()
        {
            if (_title != null) return;

            _tex = new Texture2D(1, 1);
            _tex.SetPixel(0, 0, Color.white);
            _tex.Apply();

            GUIStyle Make(int size, FontStyle fs = FontStyle.Bold)
            {
                return new GUIStyle(GUI.skin.label)
                {
                    fontSize = size,
                    fontStyle = fs,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    richText = false,
                    clipping = TextClipping.Overflow,
                };
            }

            _title = Make(130);
            _hudScore = Make(110);
            _h1 = Make(70);
            _h2 = Make(52);
            _body = Make(44, FontStyle.Normal);
            _small = Make(36, FontStyle.Normal);

            _button = MakeButton(Gold, new Color(1f, 0.7f, 0.05f), Ink, 76);
            _buttonAlt = MakeButton(Color.white, new Color(0.85f, 0.9f, 1f), Ink, 54);
        }

        GUIStyle MakeButton(Color bg, Color pressed, Color text, int size)
        {
            var normal = Solid(bg);
            var active = Solid(pressed);
            var disabled = Solid(new Color(bg.r, bg.g, bg.b, 0.35f));
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            s.normal.background = normal; s.normal.textColor = text;
            s.hover.background = normal; s.hover.textColor = text;
            s.focused.background = normal; s.focused.textColor = text;
            s.active.background = active; s.active.textColor = text;
            s.onNormal.background = normal;
            s.border = new RectOffset(0, 0, 0, 0);
            return s;
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
