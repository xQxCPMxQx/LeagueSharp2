#region
using System;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
#endregion

namespace Marksman
{
    internal class Jinx : Champion
    {
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public readonly Font PlayerText;

        public Jinx()
        {
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1450f);
            E = new Spell(SpellSlot.E, 835f);
            R = new Spell(SpellSlot.R, 1500f);

            W.SetSkillshot(0.7f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.7f, 120f, 1050f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 140f, 1700f, false, SkillshotType.SkillshotLine);

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;

            Utils.PrintMessage("Jinx loaded.");
            PlayerText = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Courier new",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });
            //PlayerText = new SharpDX.Direct3D9.Font(Drawing.Direct3DDevice, new System.Drawing.Font("Times New Roman", 20));
        }


        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (unit.IsMe)
                if (JinxData.GetPowPowStacks > 2 && spell.SData.Name == "JinxQAttack")
                {
                    JinxData.HassPowPowStack = true;
                }
        }

        #region JinxData

        public class JinxData
        {
            public static bool HassPowPowStack = false;


            public class JinxSpells
            {

                public static bool CanCastQ
                {
                    get
                    {
                        var protectManaForUlt =
                            Program.Config.SubMenu("Misc").Item("ProtectManaForUlt").GetValue<bool>();

                        var xMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                        var xRMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;

                        return !protectManaForUlt || (!R.IsReady() || ObjectManager.Player.Mana >= xRMana + xMana);
                    }
                }

                public static bool CanCastW
                {
                    get
                    {
                        var protectManaForUlt =
                            Program.Config.SubMenu("Misc").Item("ProtectManaForUlt").GetValue<bool>();

                        var xMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                        var xRMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;

                        return !protectManaForUlt || (!R.IsReady() || ObjectManager.Player.Mana >= xRMana + xMana);
                    }
                }

                public static bool CanCastE
                {
                    get
                    {
                        var protectManaForUlt =
                            Program.Config.SubMenu("Misc").Item("ProtectManaForUlt").GetValue<bool>();

                        var xMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                        var xRMana = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).ManaCost;

                        return !protectManaForUlt || (!R.IsReady() || ObjectManager.Player.Mana >= xRMana + xMana);
                    }
                }

            }

            internal enum GunType
            {
                Mini,
                Mega
            };

            public static float QMiniGunRange
            {
                get { return 650; }
            }

            public static float QMegaGunRange
            {
                get { return QMiniGunRange + 50 + 25 * Q.Level; }
            }

            public static bool EnemyHasBuffForCastE
            {
                get
                {
                    var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                    return t.HasBuffOfType(BuffType.Slow) || t.HasBuffOfType(BuffType.Stun) ||
                           t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Charm) ||
                           t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt) ||
                           t.HasBuff("zhonyasringshield") || t.HasBuff("Recall") || t.HasBuff("teleport_target", true);
                }
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
                return ObjectManager.Player.CountEnemiesInRange(range);
            }

            public static float GetRealPowPowRange(GameObject target)
            {
                return 525f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
            }

            public static float GetRealDistance(GameObject target)
            {
                return ObjectManager.Player.Position.Distance(target.Position);
                //+ ObjectManager.Player.BoundingRadius + target.BoundingRadius;
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
                if (JinxData.JinxSpells.CanCastQ)
                    HarassToggleQ();
                else
                {


                }
                if (JinxData.JinxSpells.CanCastW)
                    HarassToggleW();
            }

            public static void AlwaysChooseMiniGun()
            {
                if (Program.Config.SubMenu("Misc").Item("Swap2Mini").GetValue<bool>())
                {
                    if (JinxData.QGunType == JinxData.GunType.Mega &&
                        ObjectManager.Player.CountEnemiesInRange(JinxData.QMegaGunRange) == 0)
                    {
                        Q.Cast();
                    }
                }
            }

            private static void UsePowPowStack()
            {
                if (!Q.IsReady())
                    return;

                if (JinxData.QGunType == JinxData.GunType.Mini && JinxData.GetPowPowStacks > 2 &&
                    JinxData.HassPowPowStack)
                {
                    var t = TargetSelector.GetTarget(JinxData.QMegaGunRange, TargetSelector.DamageType.Physical);
                    if (t.IsValidTarget())
                        Q.Cast();
                }
            }

            public static void ExecutePowPowStack()
            {
                var useQPow = Program.Config.SubMenu("Misc").Item("UseQPowPowStack").GetValue<bool>();
                if (useQPow)
                {
                    UsePowPowStack();
                }
                return;
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

            public static void CastQ(bool checkPerMana = false)
            {
                var existsManaPer = Program.Config.Item("UseQTHM").GetValue<Slider>().Value;

                if (checkPerMana && ObjectManager.Player.ManaPercentage() < existsManaPer)
                    return;

                var t = TargetSelector.GetTarget(
                    JinxData.QMegaGunRange + Q.Width / 2, TargetSelector.DamageType.Physical);

                if (!t.IsValidTarget())
                    return;

                var swapDistance = Program.Config.SubMenu("Misc").Item("SwapDistance").GetValue<bool>();
                if (!swapDistance)
                    return;

                if (JinxData.GetRealDistance(t) > JinxData.QMiniGunRange &&
                    JinxData.GetRealDistance(t) <= JinxData.QMegaGunRange)
                {
                    if (JinxData.QGunType == JinxData.GunType.Mini)
                    {
                        Q.Cast();
                    }
                }
                else
                {
                    if (JinxData.QGunType == JinxData.GunType.Mega)
                        Q.Cast(t);
                }
            }

            public static void CastW(bool checkPerMana = false)
            {
                var existsManaPer = Program.Config.Item("UseQTHM").GetValue<Slider>().Value;

                if (checkPerMana && ObjectManager.Player.ManaPercentage() < existsManaPer)
                    return;

                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                
                if (!t.IsValidTarget())
                    return;

                var minW = Program.Config.Item("MinWRange").GetValue<Slider>().Value;

                if (W.IsReady())// && (JinxData.GetRealDistance(t) >= minW) ||
                    //t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.W))
                {
                    W.CastIfHitchanceEquals(t, JinxData.GetWHitChance);
                }
            }

            private static void HarassToggleQ()
            {
                var toggleActive = Program.Config.Item("UseQTH").GetValue<KeyBind>().Active;

                if (toggleActive && !ObjectManager.Player.HasBuff("Recall"))
                {
                    CastQ(true);
                }
            }

            private static void HarassToggleW()
            {
                var toggleActive = Program.Config.Item("UseWTH").GetValue<KeyBind>().Active;

                if (toggleActive && !ObjectManager.Player.HasBuff("Recall"))
                    CastW(true);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            //Drawing.DrawText(ObjectManager.Player.HPBarPosition.X + 145, ObjectManager.Player.HPBarPosition.Y + 20, System.Drawing.Color.White, time);
            if (JinxData.GetPowPowStacks < 3)
            {
                JinxData.HassPowPowStack = false;
            }

            JinxEvents.ExecuteToggle();
            JinxEvents.ExecutePowPowStack();
            JinxEvents.AlwaysChooseMiniGun();

            if (E.IsReady() && JinxData.JinxSpells.CanCastE)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (GetValue<bool>("AutoEI"))
                {
                    E.CastIfHitchanceEquals(t, HitChance.Immobile);
                }

                if (GetValue<bool>("AutoED"))
                {
                    E.CastIfHitchanceEquals(t, HitChance.Dashing);
                }

                if (GetValue<bool>("UseEC") || (GetValue<bool>("AutoES") && JinxData.EnemyHasBuffForCastE))
                {
                    E.CastIfHitchanceEquals(t, HitChance.High);
                }
            }

            if (GetValue<bool>("UseRC") && R.IsReady())
            {
                var maxRRange = GetValue<Slider>("MaxRRange").Value;
                var t = TargetSelector.GetTarget(40000, TargetSelector.DamageType.Physical);

                var aaDamage = Orbwalking.InAutoAttackRange(t)
                    ? ObjectManager.Player.GetAutoAttackDamage(t, true) * JinxData.GetPowPowStacks
                    : 0;

                if (t.Health > aaDamage && t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.R))
                {
                    if (t.IsValidTarget(maxRRange))
                    {
                        R.Cast(t);
                    }
                    else
                    {
                        var xRKillNotice = String.Format(
                            "Killable Target: {0}, Distance: {1}", t.ChampionName, JinxData.GetRealDistance(t));
                        Drawing.DrawText(
                            Drawing.Width * 0.44f, Drawing.Height * 0.80f, System.Drawing.Color.Red, xRKillNotice);
                    }
                }
            }

            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100))
            {
                var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (useQ && JinxData.JinxSpells.CanCastQ && Q.IsReady())
                    JinxEvents.CastQ();

                if (useW && W.IsReady())
                {
                    JinxEvents.CastW();
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if ((ComboActive || HarassActive) && (unit.IsValid || unit.IsMe) && (target is Obj_AI_Hero))
            {
                var useW = GetValue<bool>("UseW" + (ComboActive ? "C" : "H"));

                if (useW && W.IsReady())
                    JinxEvents.CastW();
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            var xDraw = Program.Config.Item("CustomRange").GetValue<Slider>().Value;
            Render.Circle.DrawCircle(ObjectManager.Player.Position, xDraw, System.Drawing.Color.Aqua);

            Spell[] spellList = { W, E };
            var drawQbound = GetValue<Circle>("DrawQBound");

            if (Program.Config.Item("DrawToggleStatus").GetValue<bool>())
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
                var xText = xHarassStatus.Substring(0, xHarassStatus.Length - 3);
                Vector2 pos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                Utils.DrawText(PlayerText, xText, (int)ObjectManager.Player.HPBarPosition.X + 185, (int)ObjectManager.Player.HPBarPosition.Y + 5, SharpDX.Color.White);
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
                Render.Circle.DrawCircle(
                    ObjectManager.Player.Position,
                    JinxData.QGunType == JinxData.GunType.Mini ? JinxData.QMegaGunRange : JinxData.QMiniGunRange,
                    drawQbound.Color);
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
            Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.82f, System.Drawing.Color.Wheat, xHarassStatus);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseWC" + Id, "Use W").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));

            config.AddItem(
                new MenuItem("UseQPowPowStackH", "Use Mega Q PowPow Stack").SetValue(
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
            var xQMenu = new Menu("Q Settings", "QSettings");
            {
                xQMenu.AddItem(new MenuItem("UseQPowPowStack", "Use Q PowPow Stack").SetValue(true));
                xQMenu.AddItem(new MenuItem("SwapDistance", "Swap Q for Distance").SetValue(true));
                xQMenu.AddItem(new MenuItem("SwapAOE" + Id, "Swap Q for AOE Damage").SetValue(false));
                xQMenu.AddItem(new MenuItem("Swap2Mini", "Always Swap to MiniGun If No Enemy").SetValue(true));
                config.AddSubMenu(xQMenu);
            }

            var xWMenu = new Menu("W Settings", "WSettings");
            {
                xWMenu.AddItem(
                    new MenuItem("WHitChance", "W HitChance").SetValue(
                        new StringList(new[] { "Low", "Medium", "High", "Very High", "Immobile" }, 2)));
                xWMenu.AddItem(new MenuItem("MinWRange", "Min. W range").SetValue(new Slider(525 + 65 * 2, 0, 1200)));
                config.AddSubMenu(xWMenu);
            }

            var xEMenu = new Menu("E Settings", "ESettings");
            {
                xEMenu.AddItem(new MenuItem("AutoEI" + Id, "Auto-E -> Immobile").SetValue(true));
                xEMenu.AddItem(new MenuItem("AutoES" + Id, "Auto-E -> Slowed/Stunned/Teleport/Snare").SetValue(true));
                xEMenu.AddItem(new MenuItem("AutoED" + Id, "Auto-E -> Dashing").SetValue(false));
                config.AddSubMenu(xEMenu);
            }

            var xRMenu = new Menu("R Settings", "RSettings");
            {
//                xRMenu.AddItem(new MenuItem("CastR", "Cast R (2000 Range)").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
//                xRMenu.AddItem(new MenuItem("ROverKill", "Check R Overkill").SetValue(true));
                xRMenu.AddItem(new MenuItem("MaxRRange" + Id, "Max R range").SetValue(new Slider(1700, 0, 4000)));
                xRMenu.AddItem(new MenuItem("ProtectManaForUlt", "Protect Mana for Ultimate").SetValue(true));
                config.AddSubMenu(xRMenu);
            }
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQBound" + Id, "Draw Q bound").SetValue(new Circle(true, System.Drawing.Color.Azure)));
            config.AddItem(
                new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(false, System.Drawing.Color.Azure)));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, System.Drawing.Color.Azure)));

            config.AddItem(new MenuItem("DrawToggleStatus", "Show Toggle Status").SetValue(true));

            return true;
        }

        public override bool ExtrasMenu(Menu config)
        {
            config.AddItem(new MenuItem("CustomRange", "Custom Range").SetValue(new Slider(100, 0, 4000)));
            return true;
        }

    }
}