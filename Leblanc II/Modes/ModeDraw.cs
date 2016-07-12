using System;
using System.Collections.Generic;
using System.Drawing;
using LeagueSharp.Common;
using Leblanc.Champion;
using Leblanc.Common;
using Leblanc.Properties;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;
using CommonGeometry = Leblanc.Common.CommonGeometry;
using Font = SharpDX.Direct3D9.Font;

namespace Leblanc.Modes
{
    using System.Linq;
    using LeagueSharp;
    internal class LeblancQ
    {
        public GameObject Object { get; set; }
        public float NetworkId { get; set; }
        public Vector3 QPos { get; set; }
        public double ExpireTime { get; set; }
    }

    internal class LeblancViciousStrikes
    {
        public static float StartTime { get; set; }
        public static float EndTime { get; set; }
    }


    internal class LeblancRagnarok
    {
        public static float StartTime { get; set; }
        public static float EndTime { get; set; }

    }

    enum PcMode
    {
        NewComputer,
        NormalComputer,
        OldComputer
    }

    internal class ModeDraw
    {
        public static Menu MenuLocal { get; private set; }
        public static Menu SubMenuSpells { get; private set; }
        public static Menu SubMenuBuffs { get; private set; }
        public static Menu SubMenuTimers { get; private set; }
        public static Menu SubMenuManaBarIndicator { get; private set; }
        private static Spell Q => Champion.PlayerSpells.Q;
        private static Spell W => Champion.PlayerSpells.W;
        private static Spell E => Champion.PlayerSpells.E;
        private static Spell R => Champion.PlayerSpells.R;
        private static Spell Q2 => Champion.PlayerSpells.Q2;
        private static Spell W2 => Champion.PlayerSpells.W2;
        private static Spell E2 => Champion.PlayerSpells.E2;
        public static PcMode PcMode { get; set; }

        private static readonly List<MenuItem> MenuLocalSubMenuItems = new List<MenuItem>();

        private static readonly string[] pcMode = new[] { "newpc.", "oldpc." };

        private static readonly List<LeblancQ> LeblancQ = new List<LeblancQ>();
        public void Init()
        {
            MenuLocal = new Menu("Drawings", "Drawings");
            {
                MenuLocal.AddItem(new MenuItem("Draw.Enable", "Enable/Disable Drawings:").SetValue(true)).SetFontStyle(FontStyle.Bold, SharpDX.Color.GreenYellow);
                MenuLocal.AddItem(new MenuItem("DrawPc.Mode", "Adjust settings to your own computer:").SetValue(new StringList(new[] { "New Computer", "Old Computer" }, 0)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Coral)).ValueChanged +=
                 (sender, args) =>
                 {
                     InitRefreshMenuItems();
                 };

                SubMenuManaBarIndicator = new Menu("Mana Bar Combo Indicator", "ManaBarIndicator");
                {
                    for (int i = 0; i < 2; i++)
                    {
                        SubMenuManaBarIndicator.AddItem(new MenuItem(pcMode[i] + "DrawManaBar.Q", "Q:").SetValue(true).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
                        SubMenuManaBarIndicator.AddItem(new MenuItem(pcMode[i] + "DrawManaBar.W", "W:").SetValue(true).SetFontStyle(FontStyle.Regular, W.MenuColor()));
                        SubMenuManaBarIndicator.AddItem(new MenuItem(pcMode[i] + "DrawManaBar.E", "E:").SetValue(true).SetFontStyle(FontStyle.Regular, E.MenuColor()));
                    }
                    MenuLocal.AddSubMenu(SubMenuManaBarIndicator);
                }


                SubMenuSpells = new Menu("Spell Ranges", "DrawSpellRanges");
                {
                    for (int i = 0; i < 2; i++)
                    {
                        SubMenuSpells.AddItem(new MenuItem(pcMode[i] + "Draw.Q", "Q:").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))).SetFontStyle(FontStyle.Regular, Q.MenuColor()));
                        SubMenuSpells.AddItem(new MenuItem(pcMode[i] + "Draw.W", "W:").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))).SetFontStyle(FontStyle.Regular, W.MenuColor()));
                        SubMenuSpells.AddItem(new MenuItem(pcMode[i] + "Draw.E", "E:").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))).SetFontStyle(FontStyle.Regular, E.MenuColor()));
                        SubMenuSpells.AddItem(new MenuItem(pcMode[i] + "Draw.R", "R:").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))).SetFontStyle(FontStyle.Regular, R.MenuColor()));
                    }
                    MenuLocal.AddSubMenu(SubMenuSpells);
                }

                SubMenuBuffs = new Menu("Buff Times", "DrawBuffTimes");
                {
                    for (int i = 0; i < 2; i++)
                    {
                        SubMenuBuffs.AddItem(new MenuItem(pcMode[i] + "DrawBuffs", "Show Red/Blue/Baron Time Circle").SetValue(true));
                    }
                    MenuLocal.AddSubMenu(SubMenuBuffs);
                }

                SubMenuTimers = new Menu("W-R Objects", "DrawSpellTimes");
                {
                    for (int i = 0; i < 2; i++)
                    {
                        SubMenuTimers.AddItem(new MenuItem(pcMode[i] + "Draw.W.BuffTime", "W: Show Time Circle").SetValue(new StringList(new[] { "Off", "On" }, 1)).SetFontStyle(FontStyle.Regular, R.MenuColor()));
                        SubMenuTimers.AddItem(new MenuItem(pcMode[i] + "Draw.R.BuffTime", "R: Show Time Circle").SetValue(new StringList(new[] { "Off", "On" }, 1)).SetFontStyle(FontStyle.Regular, E.MenuColor()));
                    }
                    MenuLocal.AddSubMenu(SubMenuTimers);
                }

                for (int i = 0; i < 2; i++)
                {
                    MenuLocal.AddItem(new MenuItem(pcMode[i] + "DrawKillableEnemy", "Killable Enemy Notification").SetValue(true));
                    MenuLocal.AddItem(new MenuItem(pcMode[i] + "DrawKillableEnemyMini", "Killable Enemy [Mini Map]").SetValue(new Circle(true, Color.GreenYellow)));
                }

                for (int i = 0; i < 2; i++)
                {
                    MenuLocal.AddItem(new MenuItem(pcMode[i] + "Draw.MinionLastHit", "Draw Minion Last Hit").SetValue(new StringList(new []{ "Off", "Auto Attack", "Q Damage" }, 2)));
                }


                for (int i = 0; i < 2; i++)
                {
                    var dmgAfterComboItem = new MenuItem(pcMode[i] + "DrawDamageAfterCombo", "Combo Damage").SetValue(true);
                    {
                        MenuLocal.AddItem(dmgAfterComboItem);

                        //Utility.HpBarDamageIndicator.DamageToUnit = Common.CommonMath.GetComboDamage;
                        Utility.HpBarDamageIndicator.DamageToUnit = Modes.ModeCombo.GetComboDamage;
                        Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
                        dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
                        {
                            Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                        };
                    }
                }

                CommonManaBar.Init(MenuLocal);
            }
            ModeConfig.MenuConfig.AddSubMenu(MenuLocal);
            InitRefreshMenuItems();


            Game.OnUpdate += GameOnOnUpdate;
       
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += DrawingOnOnEndScene;
        }

        private void GameOnOnUpdate(EventArgs args)
        {
            //if (SubMenuTimers.Item(GetPcModeStringValue + "Draw.W.BuffTime").GetValue<StringList>().SelectedIndex == 1 && CommonBuffs.LeblancHaveFrenziedStrikes)
            //{
            //    BuffInstance b = ObjectManager.Player.Buffs.Find(buff => buff.DisplayName == "LeblancFrenziedStrikes");
            //    if (LeblancViciousStrikes.EndTime < Game.Time || b.EndTime > LeblancViciousStrikes.EndTime)
            //    {
            //        LeblancViciousStrikes.StartTime = b.StartTime;
            //        LeblancViciousStrikes.EndTime = b.EndTime;
            //    }
            //}
            
            //if (SubMenuTimers.Item(GetPcModeStringValue + "Draw.R.BuffTime").GetValue<StringList>().SelectedIndex == 1 & CommonBuffs.LeblancHaveRagnarok)
            //{
            //    BuffInstance b = ObjectManager.Player.Buffs.Find(buff => buff.DisplayName == "LeblancRagnarok");
            //    if (LeblancRagnarok.EndTime < Game.Time || b.EndTime > LeblancRagnarok.EndTime)
            //    {
            //        LeblancRagnarok.StartTime = b.StartTime;
            //        LeblancRagnarok.EndTime = b.EndTime;
            //    }
            //}
        }

        private static MenuItem GetMenuItems(Menu menu)
        {
            foreach (var j in menu.Children.Cast<Menu>().SelectMany(GetMenu).SelectMany(i => i.Items))
            {
                MenuLocalSubMenuItems.Add(j);
            }

            foreach (var j in menu.Items)
            {
                MenuLocalSubMenuItems.Add(j);
            }
            return null;
        }
        private static IEnumerable<Menu> GetMenu(Menu menu)
        {
            yield return menu;

            foreach (var childChild in menu.Children.SelectMany(GetMenu))
                yield return childChild;
        }
        public static PcMode GetPcModeEnum
        {
            get
            {
                if (MenuLocal.Item("DrawPc.Mode").GetValue<StringList>().SelectedIndex == 0)
                {
                    return PcMode.NewComputer;
                }

                if (MenuLocal.Item("DrawPc.Mode").GetValue<StringList>().SelectedIndex == 1)
                {
                    return PcMode.NormalComputer;
                }

                if (MenuLocal.Item("DrawPc.Mode").GetValue<StringList>().SelectedIndex == 2)
                {
                    return PcMode.OldComputer;
                }

                return PcMode.NormalComputer;
            }
        }

        public static string GetPcModeStringValue => pcMode[MenuLocal.Item("DrawPc.Mode").GetValue<StringList>().SelectedIndex];

        private static void InitRefreshMenuItems()
        {
            int argsValue = MenuLocal.Item("DrawPc.Mode").GetValue<StringList>().SelectedIndex;
            MenuLocalSubMenuItems.Clear();
            GetMenuItems(MenuLocal);

            foreach (var item in MenuLocalSubMenuItems)
            {
                item.Show(true);
                switch (argsValue)
                {
                    case 0:
                        if (!item.Name.StartsWith("newpc.") && !item.Name.StartsWith("DrawPc.Mode") && !item.Name.StartsWith("Draw.Enable"))
                        {
                            item.Show(false);
                        }
                        break;
                    case 1:
                        if (!item.Name.StartsWith("oldpc.") && !item.Name.StartsWith("DrawPc.Mode") && !item.Name.StartsWith("Draw.Enable"))
                        {
                            item.Show(false);
                        }
                        break;
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Width, Color.GreenYellow);
            if (!MenuLocal.Item("Draw.Enable").GetValue<bool>())
            {
                return;
            }


            DrawSpells();
            DrawMinionLastHit();
            //KillableEnemy();
            DrawBuffs();

            //Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Red);

            return;
            var t = TargetSelector.GetTarget(W.Range * 3, TargetSelector.DamageType.Magical);

            if (t == null)
            {
                return;
            }

            if (t.IsValidTarget(W.Range))
            {
                return;
            }

            
            List<Vector2> xList = new List<Vector2>();

            var nLocation = ObjectManager.Player.Position.To2D() + Vector2.Normalize(t.Position.To2D() - ObjectManager.Player.Position.To2D()) * W.Range;


            //if (CommonGeometry.IsWallBetween(nEvadePoint.To3D(), location.To3D()))
            //{
            //    Game.PrintChat("Wall");
            //}
            //else
            //{
            //    Game.PrintChat("Not Wall");
            //}


            
            Vector2 wCastPosition = nLocation;

            //Render.Circle.DrawCircle(wCastPosition.To3D(), 105f, System.Drawing.Color.Red);


            if (!wCastPosition.IsWall())
            {
                xList.Add(wCastPosition);
            }

            if (wCastPosition.IsWall())
            {
                for (int j = 20; j < 80; j += 20)
                {
                    Vector2 wcPositive = ObjectManager.Player.Position.To2D() + Vector2.Normalize(t.Position.To2D() - ObjectManager.Player.Position.To2D()).Rotated(j * (float)Math.PI / 180) * W.Range;
                    if (!wcPositive.IsWall())
                    {
                        xList.Add(wcPositive);
                    }

                    Vector2 wcNegative = ObjectManager.Player.Position.To2D() + Vector2.Normalize(t.Position.To2D() - ObjectManager.Player.Position.To2D()) .Rotated(-j*(float) Math.PI/180)*W.Range;
                    if (!wcNegative.IsWall())
                    {
                        xList.Add(wcNegative);
                    }
                }

                float xDiff = ObjectManager.Player.Position.X - t.Position.X;
                float yDiff = ObjectManager.Player.Position.Y - t.Position.Y;
                int angle = (int)(Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI);
            }

            //foreach (var aa in xList)
            //{
            //    Render.Circle.DrawCircle(aa.To3D2(), 105f, System.Drawing.Color.White);
            //}
            var nJumpPoint = xList.OrderBy(al => al.Distance(t.Position)).First();

            var color = System.Drawing.Color.DarkRed;
            var width = 4;

            var startpos = ObjectManager.Player.Position;
            var endpos = nJumpPoint.To3D();

            if (startpos.Distance(endpos) > 100)
            {
                var endpos1 = nJumpPoint.To3D() + (startpos - endpos).To2D().Normalized().Rotated(25 * (float)Math.PI / 180).To3D() * 75;
                var endpos2 = nJumpPoint.To3D() + (startpos - endpos).To2D().Normalized().Rotated(-25 * (float)Math.PI / 180).To3D() * 75;

                var x1 = new LeagueSharp.Common.Geometry.Polygon.Line(startpos, endpos);
                x1.Draw(color, width - 2);
                var y1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos1);
                y1.Draw(color, width - 2);
                var z1 = new LeagueSharp.Common.Geometry.Polygon.Line(endpos, endpos2);
                z1.Draw(color, width - 2);

                Geometry.Polygon.Circle x2 = new LeagueSharp.Common.Geometry.Polygon.Circle(endpos, W.Width / 2);

                if (CommonGeometry.IsWallBetween(ObjectManager.Player.Position, endpos))
                {
                    x2.Draw(Color.Red, width - 2);
                }
                else
                {
                    x2.Draw(Color.Wheat, width - 2);
                }
            }

            if (!t.IsValidTarget(W.Range + Q.Range - 60))
            {
                return;
            }

            if (t.IsValidTarget(W.Range))
            {
                return;
            }

            var canJump = false;
            if (Modes.ModeCombo.ComboMode == ComboMode.Mode2xQ)
            {
                if ((t.Health < ModeCombo.GetComboDamage(t) - W.GetDamage(t) && Q.IsReady() && R.IsReady()) || (t.Health < Q.GetDamage(t) && Q.IsReady()))
                {
                    canJump = true;
                }
            }

            var nPoint = nJumpPoint.Extend(ObjectManager.Player.Position.To2D(), +ObjectManager.Player.BoundingRadius * 3);
            Render.Circle.DrawCircle(nPoint.To3D(), 50f, Color.GreenYellow);

            if (CommonGeometry.IsWallBetween(nPoint.To3D(), nJumpPoint.To3D()))
            {
                canJump = false;
            }

            if (canJump && W.IsReady() && !W.StillJumped())
            {
                if (Modes.ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    W.Cast(nJumpPoint);
                }
                return;
            }
        }


        private static void DrawBuffs()
        {
            if (MenuLocal.Item(GetPcModeStringValue + "DrawBuffs").GetValue<bool>())
            {
                foreach (var hero in HeroManager.AllHeroes)
                {
                    var jungleBuffs =
                        (from b in hero.Buffs
                         join b1 in CommonBuffManager.JungleBuffs on b.DisplayName equals b1.BuffName
                         select new { b, b1 }).Distinct();

                    foreach (var buffName in jungleBuffs.ToList())
                    {
                        var circle1 =
                            new CommonGeometry.Circle2(new Vector2(hero.Position.X + 3, hero.Position.Y - 3),
                                140 + (buffName.b1.Number * 20),
                                Game.Time - buffName.b.StartTime, buffName.b.EndTime - buffName.b.StartTime).ToPolygon();
                        circle1.Draw(Color.Black, 3);

                        var circle =
                            new CommonGeometry.Circle2(hero.Position.To2D(), 140 + (buffName.b1.Number * 20),
                                Game.Time - buffName.b.StartTime, buffName.b.EndTime - buffName.b.StartTime).ToPolygon();
                        circle.Draw(buffName.b1.Color, 3);
                    }
                }
            }
        }

        private static void DrawingOnOnEndScene(EventArgs args)
        {
            var drawKillableEnemyMini = MenuLocal.Item(GetPcModeStringValue + "DrawKillableEnemyMini").GetValue<Circle>();
            if (drawKillableEnemyMini.Active)
            {
                foreach (
                    var e in
                        HeroManager.Enemies.Where(
                            e => e.IsVisible && !e.IsDead && !e.IsZombie && e.Health < Common.CommonMath.GetComboDamage(e)))
                {
                    if ((int) Game.Time%2 == 1)
                    {
                        #pragma warning disable 618
                        Utility.DrawCircle(e.Position, 850, drawKillableEnemyMini.Color, 2, 30, true);
                        #pragma warning restore 618
                    }
                }
            }
        }

        private static void DrawSpells()
        {
            var t = TargetSelector.GetTarget(Q.Range + 500, TargetSelector.DamageType.Physical);

            var drawQ = MenuLocal.Item(GetPcModeStringValue + "Draw.Q").GetValue<Circle>();
            if (drawQ.Active && Q.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? drawQ.Color: Color.LightGray, Q.IsReady() ? 5 : 1);
            }

            var drawW = MenuLocal.Item(GetPcModeStringValue + "Draw.W").GetValue<Circle>();
            if (drawW.Active && W.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? drawW.Color : Color.LightGray, W.IsReady() ? 5 : 1);
            }

            var drawE = MenuLocal.Item(GetPcModeStringValue + "Draw.E").GetValue<Circle>();
            if (drawE.Active && E.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Modes.ModeSettings.MaxERange, E.IsReady() ? drawE.Color: Color.LightGray, E.IsReady() ? 5 : 1);
            }

            var drawR = MenuLocal.Item(GetPcModeStringValue + "Draw.R").GetValue<Circle>();
            if (drawR.Active && R.Level > 0)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, E.IsReady() ? drawR.Color : Color.LightGray, E.IsReady() ? 5 : 1);
            }
        }

        public static Obj_AI_Hero GetKillableEnemy
        {
            get
            {
                if (MenuLocal.Item(GetPcModeStringValue + "DrawKillableEnemy").GetValue<bool>())
                {
                    return HeroManager.Enemies.FirstOrDefault(e => e.IsVisible && !e.IsDead && !e.IsZombie && e.Health < Common.CommonMath.GetComboDamage(e));
                }
                return null;
            }
        }

        private static void KillableEnemy()
        {
            if (MenuLocal.Item(GetPcModeStringValue + "DrawKillableEnemy").GetValue<bool>())
            {
                var t = KillableEnemyAa;
                if (t.Item1 != null && t.Item1.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 800) && t.Item2 > 0)
                {
                    CommonHelper.DrawText(CommonHelper.Text, $"{t.Item1.ChampionName}: {t.Item2} Combo = Kill", (int)t.Item1.HPBarPosition.X + 85, (int)t.Item1.HPBarPosition.Y + 5, SharpDX.Color.GreenYellow);
                    //CommonHelper.DrawText(CommonHelper.Text, $"{t.Item1.ChampionName}: {t.Item2} Combo = Kill", (int)t.Item1.HPBarPosition.X + 7, (int)t.Item1.HPBarPosition.Y + 36, SharpDX.Color.GreenYellow);

                }
            }
        }
        private static void DrawMinionLastHit()
        {
            
            var drawMinionLastHit = MenuLocal.Item(GetPcModeStringValue + "Draw.MinionLastHit").GetValue<StringList>().SelectedIndex;
            if (drawMinionLastHit != 0)
            {

                var minions = MinionManager.GetMinions(ObjectManager.Player.Position, (float) (Q.Range * 1.5), MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);

                switch (drawMinionLastHit)
                {
                    case 1:
                    {
                        foreach (var m in minions.ToList().Where(m => m.Health < ObjectManager.Player.TotalAttackDamage)
                            )
                        {
                            Render.Circle.DrawCircle(m.Position, m.BoundingRadius, Color.Wheat);
                        }
                        break;
                    }
                    case 2:
                    {
                        foreach (var m in minions.ToList().Where(m => m.Health < Q.GetDamage(m)))
                        {
                            Render.Circle.DrawCircle(m.Position, m.BoundingRadius, Color.Wheat);
                        }
                        break;
                    }
                }
            }
        }

        private static Tuple<Obj_AI_Hero, int> KillableEnemyAa
        {
            get
            {
                var x = 0;
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                {
                    if (t.IsValidTarget())
                    {
                            if (t.Health <= Common.CommonMath.GetComboDamage(t))
                            {
                            x = (int)Math.Ceiling(t.Health / ObjectManager.Player.TotalAttackDamage);
                        }
                        return new Tuple<Obj_AI_Hero, int>(t, x);
                    }

                }
                return new Tuple<Obj_AI_Hero, int>(t, x);
            }
        }

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, SharpDX.Color aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor != SharpDX.Color.Black ? SharpDX.Color.Black : SharpDX.Color.White);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);




        }
    }
}
