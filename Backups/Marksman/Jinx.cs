#region

using System;
using System.Drawing;
using System.Linq;

using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Marksman
{
    internal class Jinx : Champion
    {
        public static Spell Q;
        public static Spell W;
        public Spell E;
        public Spell R;
        public static bool PowPowStackUsed = true;


        public Jinx()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 25000f);

            W.SetSkillshot(0.7f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.7f, 120f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;

            Utils.PrintMessage("Jinx loaded.");
        }

        #region JinxData

        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
                if (JinxData.GetPowPowStacks > 2 && spell.SData.Name == "JinxQAttack")
                {
                    PowPowStackUsed = true;
                }
            //JinxQAttack
        }

        public class JinxData
        {
            public static bool UsedPowPowStack;
            #region JinxMenu

            public class JinxMenu
            {
                public static int MenuItemMinWRange
                {
                    get { return Program.Config.Item("MinWRange").GetValue<Slider>().Value; }
                }
            }

            #endregion

            internal enum GunType
            {
                Mini,
                Mega
            };

            public static float QMiniGunRange
            {
                get { return 575; }
            }

            public static float QMegaGunRange
            {
                get { return QMiniGunRange + 50 + 25 * Q.Level; }
            }

            public static int GetPowPowStacks
            {
                get
                {
                    return
                        ObjectManager.Player.Buffs.Where(buff => buff.DisplayName.ToLower() == "jinxqramp")
                            .Select(buff => buff.Count)
                            .FirstOrDefault();
                }
            }

            public static GunType QGunType
            {
                get { return ObjectManager.Player.HasBuff("JinxQ", true) ? GunType.Mega : GunType.Mini; }
            }

            public static int CountEnemies(float range)
            {
                return ObjectManager.Player.CountEnemysInRange(range);
            }

            public static float GetRealPowPowRange(GameObject target)
            {
                return 525f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
            }

            public static float GetRealDistance(GameObject target)
            {
                return ObjectManager.Player.Position.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                       target.BoundingRadius;
            }

            public static float GetSlowEndTime(Obj_AI_Base target)
            {
                return
                    target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                        .Where(buff => buff.Type == BuffType.Slow)
                        .Select(buff => buff.EndTime)
                        .FirstOrDefault();
            }

            public static HitChance GetWHitChance
            {
                get
                {
                    HitChance hitChance;
                    var wHitChance = Program.Config.Item("WHitChance").GetValue<StringList>().SelectedIndex;
                    switch (wHitChance)
                    {
                        case 0:
                        {
                            hitChance = HitChance.Low;
                            break;
                        }
                        case 1:
                        {
                            hitChance = HitChance.Medium;
                            break;
                        }
                        case 2:
                        {
                            hitChance = HitChance.High;
                            break;
                        }
                        case 3:
                        {
                            hitChance = HitChance.VeryHigh;
                            break;
                        }
                        case 4:
                        {
                            hitChance = HitChance.Dashing;
                            break;
                        }
                        default:
                        {
                            hitChance = HitChance.High;
                            break;
                        }
                    }
                    return hitChance;
                }
            }
        }

        #endregion

        public class JinxEvents
        {
            public static void ExecuteToggle()
            {
                HarassToggleQ();
                HarassToggleW();
            }

            private static void UsePowPowStack()
            {
                if (!Q.IsReady())
                    return;

                if (JinxData.QGunType == JinxData.GunType.Mini && JinxData.GetPowPowStacks > 2)
                {
                    PowPowStackUsed = false;

                    var t = TargetSelector.GetTarget(JinxData.QMegaGunRange, TargetSelector.DamageType.Physical);
                    //Game.PrintChat(JinxData.GetPowPowStacks.ToString());
                    if (t.IsValidTarget())
                        Q.Cast();
                }
            }

            public static void ExecutePowPowStack()
            {
                var useQPowPowStack = Program.Config.Item("UseQPowPowStackH").GetValue<StringList>().SelectedIndex;
                switch (useQPowPowStack)
                {
                    case 1:
                    {
                        UsePowPowStack();
                        break;
                    }

                    case 2:
                    {
                        if (Program.CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                            UsePowPowStack();
                        break;
                    }

                    case 3:
                    {
                        if (Program.CClass.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                            UsePowPowStack();
                        break;
                    }
                }
            }

            private static void CastQ(bool checkPerMana = false)
            {
                var existsManaPer = Program.Config.Item("UseQTHM").GetValue<Slider>().Value;

                if (checkPerMana && ObjectManager.Player.ManaPercentage() < existsManaPer)
                    return;

                var t = TargetSelector.GetTarget(
                    JinxData.QMegaGunRange + Q.Width / 2, TargetSelector.DamageType.Physical);

                if (!t.IsValidTarget())
                    return;

                if (ObjectManager.Player.Distance(t) > JinxData.QMiniGunRange &&
                    ObjectManager.Player.Distance(t) < JinxData.QMegaGunRange)
                {
                    if (JinxData.QGunType == JinxData.GunType.Mini)
                        Q.Cast();
                }
                else
                {
                    if (JinxData.QGunType == JinxData.GunType.Mega)
                        Q.Cast();
                }
            }

            private static void CastW(bool checkPerMana = false)
            {
                var existsManaPer = Program.Config.Item("UseQTHM").GetValue<Slider>().Value;

                if (checkPerMana && ObjectManager.Player.ManaPercentage() < existsManaPer)
                    return;

                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (!t.IsValidTarget())
                    return;

                if (W.IsReady())
                    W.CastIfHitchanceEquals(t, JinxData.GetWHitChance);
            }

            private static void HarassToggleQ()
            {
                var toggleActive = Program.Config.Item("UseQTH").GetValue<KeyBind>().Active;

                if (toggleActive || !ObjectManager.Player.HasBuff("Recall"))
                    CastQ(true);
            }

            private static void HarassToggleW()
            {
                var toggleActive = Program.Config.Item("UseQTH").GetValue<KeyBind>().Active;

                if (toggleActive || !ObjectManager.Player.HasBuff("Recall"))
                    CastW(true);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            var autoEi = GetValue<bool>("AutoEI");
            var autoEs = GetValue<bool>("AutoES");
            var autoEd = GetValue<bool>("AutoED");

            JinxEvents.ExecuteToggle();
            
            JinxEvents.ExecutePowPowStack();

            if (autoEs || autoEi || autoEd)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                if (E.IsReady())
                {
                    if ((t.HasBuffOfType(BuffType.Slow) || t.HasBuffOfType(BuffType.Stun) ||
                         t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Charm) ||
                         t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt) ||
                         t.HasBuff("zhonyasringshield") || t.HasBuff("Recall")))
                    {
                        E.CastIfHitchanceEquals(t, HitChance.High);
                    }
                    else
                    {
                        if (E.CastIfHitchanceEquals(t, HitChance.High))
                        {
                        }
                    }
                }
            }

            if (GetValue<KeyBind>("CastR").Active && R.IsReady())
            {
                var target = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);

                if (target.IsValidTarget())
                {
                    if (ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        R.Cast(target);
                    }
                }
            }

           

            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100))
            {


                return;
            }

          
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if ((ComboActive || HarassActive) && unit.IsMe && (target is Obj_AI_Hero))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (useW && W.IsReady())
                {
                    var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                    var minW = GetValue<Slider>("MinWRange").Value;

                    if (t.IsValidTarget() && JinxData.GetRealDistance(t) >= minW)
                    {
                        if (W.CastIfHitchanceEquals(t, JinxData.GetWHitChance))
                        {
                            return;
                        }
                    }
                }

                if (useQ)
                {

                    foreach (var t in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(t => t.IsValidTarget(JinxData.GetRealPowPowRange(t) + JinxData.QMegaGunRange + 20f)))
                    {
                        var swapDistance = GetValue<bool>("SwapDistance");
                        var swapAoe = GetValue<bool>("SwapAOE");
                        var distance = JinxData.GetRealDistance(t);
                        var powPowRange = JinxData.GetRealPowPowRange(t);

                        if (swapDistance && Q.IsReady())
                        {
                            if (distance > powPowRange && JinxData.QGunType == JinxData.GunType.Mini)
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                            else if (distance < powPowRange && JinxData.QGunType == JinxData.GunType.Mega)
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                        }

                        if (swapAoe && Q.IsReady())
                        {
                            if (distance > powPowRange && JinxData.GetPowPowStacks > 2 &&
                                JinxData.QGunType == JinxData.GunType.Mini && JinxData.CountEnemies(150) > 1)
                            {
                                if (Q.Cast())
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { W, E };
            var drawQbound = GetValue<Circle>("DrawQBound");

            if (Program.Config.Item("HarassShowStatus").GetValue<bool>())
            {
                ShowToggleStatus();
            }

            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }

            if (drawQbound.Active)
            {
                if (JinxData.QGunType == JinxData.GunType.Mega)
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position, 525f + ObjectManager.Player.BoundingRadius + 65f,
                        drawQbound.Color);
                }
                else
                {
                    Render.Circle.DrawCircle(
                        ObjectManager.Player.Position,
                        525f + ObjectManager.Player.BoundingRadius + 65f + JinxData.QMegaGunRange + 20f,
                        drawQbound.Color);
                }
            }
        }

        private static void ShowToggleStatus()
        {
            var xHarassStatus = "";
            if (Program.Config.Item("UseQTH").GetValue<KeyBind>().Active)
                xHarassStatus += "Q - ";

            if (Program.Config.Item("UseWTH").GetValue<KeyBind>().Active)
                xHarassStatus += "W - ";

            if (xHarassStatus.Length < 1)
            {
                xHarassStatus = "Toggle: Off   ";
            }
            else
            {
                xHarassStatus = "Toggle: " + xHarassStatus;
            }
            xHarassStatus = xHarassStatus.Substring(0, xHarassStatus.Length - 3);
            Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.82f, Color.Wheat, xHarassStatus);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));

            config.AddItem(
                new MenuItem("UseQPowPowStackH", "Use Q PowPow Stack").SetValue(
                    new StringList(new[] { "Off", "Both", "Combo", "Harass" }, 1)));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWH" + Id, "Use W").SetValue(false));

            config.AddItem(
                new MenuItem("UseQTH", "Q (Toggle!)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));
            config.AddItem(new MenuItem("UseQTHM", "Q Mana Per.").SetValue(new Slider(50, 100, 0)));

            config.AddItem(
                new MenuItem("UseWTH", "W (Toggle!)").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));
            config.AddItem(new MenuItem("UseWTHM", "W Mana Per.").SetValue(new Slider(50, 100, 0)));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            config.AddItem(new MenuItem("SwapQ" + Id, "Always swap to Minigun").SetValue(false));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQPowPowStack" + Id, "Use Q PowPow Stack").SetValue(true));
            config.AddItem(
                new MenuItem("WHitChance", "W HitChance").SetValue(
                    new StringList(new[] { "Low", "Medium", "High", "Very High", "Immobile" }, 2)));

            config.AddItem(new MenuItem("SwapDistance" + Id, "Swap Q for distance").SetValue(true));
            config.AddItem(new MenuItem("SwapAOE" + Id, "Swap Q for AOE").SetValue(false));
            config.AddItem(new MenuItem("MinWRange" + Id, "Min W range").SetValue(new Slider(525 + 65 * 2, 0, 1200)));

            config.AddItem(new MenuItem("AutoEI" + Id, "Auto-E on immobile").SetValue(true));
            config.AddItem(new MenuItem("AutoES" + Id, "Auto-E on slowed").SetValue(true));
            config.AddItem(new MenuItem("AutoED" + Id, "Auto-E on dashing").SetValue(false));

            config.AddItem(
                new MenuItem("CastR" + Id, "Cast R (2000 Range)").SetValue(
                    new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("ROverKill" + Id, "Check R Overkill").SetValue(true));
            config.AddItem(new MenuItem("MinRRange" + Id, "Min R range").SetValue(new Slider(300, 0, 1500)));
            config.AddItem(new MenuItem("MaxRRange" + Id, "Max R range").SetValue(new Slider(1700, 0, 4000)));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQBound" + Id, "Draw Q bound").SetValue(
                    new Circle(true, Color.FromArgb(100, 255, 0, 0))));
            config.AddItem(new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, Color.CornflowerBlue)));
            config.AddItem(new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.CornflowerBlue)));
            config.AddItem(new MenuItem("HarassShowStatus", "Show Toggle Status").SetValue(true));
            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {

            return true;
        }

    }
}