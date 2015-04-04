using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using KaiHelper.Properties;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace KaiHelper.Tracker
{
    public enum WardType
    {
        None,
        Green,
        Pink,
        Trap,
    }

    public class Ward
    {
        private Render.Circle _circle;
        private Render.Sprite _minimapSprite;
        private Render.Text _timerText;
        public static int IsWard(string name)
        {
            int range=0;
            switch (name)
            {
                case "YellowTrinket":
                    range = 1100;
                    break;
                case "YellowTrinketUpgrade":
                    range = 1100;
                    break;
                case "SightWard":
                    range = 1100;
                    break;
                case "VisionWard":
                    range = 1100;
                    break;
                case "CaitlynTrap":
                    range = 300;
                    break;
                case "TeemoMushroom":
                    range = 212;
                    break;
                case "Nidalee_Spear":
                    range = 212;
                    break;
                case "ShacoBox":
                    range = 212;
                    break;
            }
            return range;
        }

        public Ward(string skinName, int startTime,Obj_AI_Base wardObject)
        {
            
            switch (skinName)
            {
                case "YellowTrinket":
                    Duration = 60 * 1000;
                    Type = WardType.Green;
                    Color = Color.Lime;
                    Range = 1100;
                    break;
                case "YellowTrinketUpgrade":
                    Duration = 60 * 2 * 1000;
                    Type = WardType.Green;
                    Color = Color.Lime;
                    Range = 1100;
                    break;
                case "SightWard":
                    Duration = 60 * 3 * 1000;
                    Type = WardType.Green;
                    Color = Color.Lime;
                    Range = 1100;
                    break;
                case "VisionWard":
                    Duration = int.MaxValue;
                    Type = WardType.Pink;
                    Color = Color.Magenta;
                    Range = 1100;
                    break;
                case "CaitlynTrap":
                    Duration = 60 * 4 * 1000;
                    Type = WardType.Trap;
                    Color = Color.Red;
                    Range = 300;
                    break;
                case "TeemoMushroom":
                    Duration = 60 * 10 * 1000;
                    Type = WardType.Trap;
                    Color = Color.Red;
                    Range = 212;
                    break;
                case "Nidalee_Spear":
                    Duration = 60 * 2 * 1000;
                    Type = WardType.Trap;
                    Color = Color.Red;
                    Range = 212;
                    break;
                case "ShacoBox":
                    Duration = 60 * 1 * 1000;
                    Type = WardType.Trap;
                    Color = Color.Red;
                    Range = 212;
                    break;
                default:
                    Duration = 0;
                    Type = WardType.None;
                    Color = Color.Red;
                    Range = 1100;
                    break;
            }
            StartTime = startTime;
            EndTime = StartTime + Duration;
            switch (Type)
            {
                case WardType.Green:
                    Bitmap= Resources.ward;
                    break;
                case WardType.Pink:
                    Bitmap = Resources.pink;
                    break;
                default:
                    Bitmap = Resources.ward;
                    break;
            }
            WardObject = wardObject;
            MinimapPosition = Drawing.WorldToMinimap(wardObject.Position) + new Vector2(-Bitmap.Width / 2f * Scale, -Bitmap.Height / 2f * Scale);
            DrawCircle();
        }

        public string SkinName { get; set; }
        public Obj_AI_Base WardObject { get; set; }
        public Bitmap Bitmap { get; set; }
        public Color Color { get; set; }
        public int StartTime { get; set; }
        public int Duration { get; set; }
        public int EndTime { get; set; }
        public  int Range { get; set; }
        public WardType Type { get; set; }
        private Vector2 MinimapPosition { get; set; }

        public float Scale
        {
            get { return WardDetector.MenuWard.Item("WardScale").GetValue<Slider>().Value/100f; }
        }

        public void DrawCircle()
        {
            _circle = new Render.Circle(WardObject.Position, 100, Color, 5, true);
            _circle.VisibleCondition += sender => WardDetector.IsActive() && Render.OnScreen(Drawing.WorldToScreen(WardObject.Position));
            _circle.Add(0);
            if (Type != WardType.Trap)
            {
                _minimapSprite = new Render.Sprite(Bitmap, MinimapPosition) { Scale = new Vector2(Scale, Scale) };
                _minimapSprite.Add(0);
            }
            if (Duration == int.MaxValue)
            {
                return;
            }
            _timerText = new Render.Text(10, 10, "t", 18, new ColorBGRA(255, 255, 255, 255))
            {
                OutLined = true,
                PositionUpdate = () => Drawing.WorldToScreen(WardObject.Position),
                Centered = true
            };
            _timerText.VisibleCondition +=
                sender => WardDetector.IsActive() && Render.OnScreen(Drawing.WorldToScreen(WardObject.Position));
            _timerText.TextUpdate = () => Utils.FormatTime((EndTime - Environment.TickCount) / 1000f);
            _timerText.Add(2);
        }

        public bool RemoveCircle()
        {
            _circle.Remove();
            if (_timerText != null)
            {
                _timerText.Remove();
            }
            if (_minimapSprite != null)
            {
                _minimapSprite.Remove();
            }
            return true;
        }
    }

    public class WardDetector
    {
        public readonly List<Ward> DetectedWards = new List<Ward>();
        public static Menu MenuWard;
        public WardDetector(Menu config)
        {
            MenuWard = config.AddSubMenu(new Menu("Ward Tracker","WardTracker"));
            MenuWard.AddItem(new MenuItem("WardScale", "Scale (F5)")).SetValue(new Slider(70));
            MenuWard.AddItem(new MenuItem("WardActive", "Active")).SetValue(true);
            foreach (GameObject obj in ObjectManager.Get<GameObject>().Where(o => o is Obj_AI_Base))
            {
                Game_OnCreate(obj, null);
            }
            GameObject.OnCreate += Game_OnCreate;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        public static bool IsActive()
        {
            return MenuWard.Item("WardActive").GetValue<bool>();
        }

        private void Game_OnCreate(GameObject sender, EventArgs args)
        {
            if (!IsActive())
            {
                return;
            }
            var wardObject = sender as Obj_AI_Base;
            if (wardObject == null || wardObject.IsAlly || Ward.IsWard(wardObject.SkinName) == 0)
            {
                return;
            }
            int startTime = Environment.TickCount - (int)((wardObject.MaxMana - wardObject.Mana) * 1000);
            DetectedWards.Add(new Ward(wardObject.SkinName, startTime, wardObject));
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive())
            {
                return;
            }
            DetectedWards.RemoveAll(w => w.WardObject.IsDead && w.RemoveCircle());
        }
    }
}