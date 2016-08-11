#region

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

#endregion

namespace Marksman.Champions
{
    internal class Jhin : Champion
    {
        public static Spell Q;

        public static Spell W;

        public static Spell E;

        public static Spell R;

        public Jhin()
        {
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 2400);
            E = new Spell(SpellSlot.E, 750);
            R = new Spell(SpellSlot.R, 3500);

            W.SetSkillshot(250f, (float)(45f * Math.PI / 180), 900f, true, SkillshotType.SkillshotCone);
            E.SetSkillshot(377f, 299f, 1400f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(250f, 130f, 1600f, false, SkillshotType.SkillshotLine);

            Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpell;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Drawing.OnEndScene += DrawingOnOnEndScene;
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Obj_AI_Base.OnBuffAdd += (sender, args) =>
            {
                BuffInstance aBuff =
                    (from fBuffs in
                         sender.Buffs.Where(
                             s =>
                             sender.Team != ObjectManager.Player.Team
                             && sender.Distance(ObjectManager.Player.Position) < 2500)
                     from b in new[] { "katarinar", "MissFortuneBulletTime", "crowstorm" }

                     where b.Contains(args.Buff.Name.ToLower())
                     select fBuffs).FirstOrDefault();

                if (aBuff != null && E.IsReady())
                {
                    R.Cast(sender.Position);
                }
            };

            Utils.Utils.PrintMessage("Jhin loaded.");
        }

        private static bool JhinQCastReady
        {
            get
            {
                return ObjectManager.Player.HasBuff("JhinQCastReady", true);
            }
        }

        public bool IsQActive
        {
            get
            {
                return ObjectManager.Player.HasBuff("FrostShot");
            }
        }

        private void Interrupter2_OnInterruptableTarget(
            Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (R.IsReady() && Config.Item("RInterruptable" + Id).GetValue<bool>() && unit.IsValidTarget(1500))
            {
                R.Cast(unit);
            }
        }

        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (W.IsReady()) fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (R.IsReady()) fComboDamage += (float)ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);

            if (ObjectManager.Player.GetSpellSlot("summonerdot") != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("summonerdot"))
                == SpellState.Ready && ObjectManager.Player.Distance(t) < 550) fComboDamage += (float)ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3144) && ObjectManager.Player.Distance(t) < 550) fComboDamage += (float)ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Bilgewater);

            if (Items.CanUseItem(3153) && ObjectManager.Player.Distance(t) < 550) fComboDamage += (float)ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return fComboDamage;
        }

        public void Game_OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
        {
            if (!Config.Item("EFlash" + Id).GetValue<bool>() || unit.Team == ObjectManager.Player.Team)
            {
                return;
            }

            if (spell.SData.Name.ToLower() == "summonerflash" && unit.Distance(ObjectManager.Player.Position) < 2000)
            {
                E.Cast(spell.End);
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            if (!ComboActive)
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (!t.IsValidTarget() || !W.IsReady()) return;

                if (Program.Config.Item("UseWTH").GetValue<KeyBind>().Active)
                {
                    if (ObjectManager.Player.HasBuff("Recall")) return;
                    W.Cast(t);
                }

                if (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) || t.HasBuffOfType(BuffType.Charm)
                    || t.HasBuffOfType(BuffType.Fear) || t.HasBuffOfType(BuffType.Taunt)
                    || t.HasBuff("zhonyasringshield") || t.HasBuff("Recall"))
                {
                    W.Cast(t.Position);
                }
            }

            /* [ Combo ] */
            if (ComboActive)
            {
                var useW = Config.Item("UseWC" + Id).GetValue<bool>();

                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);

                if (Q.IsReady() && JhinQCastReady)
                {
                    if (t.IsValidTarget(Marksman.Utils.Orbwalking.GetRealAutoAttackRange(null) + 90))
                    {
                        Q.Cast();
                    }
                }

                if (useW && W.IsReady() && t.IsValidTarget())
                {
                    W.Cast(t);
                }

                var useR = Program.Config.SubMenu("Combo").Item("UseRC").GetValue<bool>();
                if (useR && R.IsReady())
                {
                    var minRRange = Program.Config.SubMenu("Combo").Item("UseRCMinRange").GetValue<Slider>().Value;
                    var maxRRange = Program.Config.SubMenu("Combo").Item("UseRCMaxRange").GetValue<Slider>().Value;

                    t = TargetSelector.GetTarget(maxRRange, TargetSelector.DamageType.Physical);
                    if (!t.IsValidTarget()) return;

                    var aaDamage = Marksman.Utils.Orbwalking.InAutoAttackRange(t)
                                       ? ObjectManager.Player.GetAutoAttackDamage(t, true)
                                       : 0;

                    if (t.Health > aaDamage && t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.R)
                        && ObjectManager.Player.Distance(t) >= minRRange)
                    {
                        R.Cast(t);
                    }
                }
            }

            //Harass
            if (HarassActive)
            {
                var target = TargetSelector.GetTarget(1200, TargetSelector.DamageType.Physical);
                if (target == null) return;

                if (Config.Item("UseWH" + Id).GetValue<bool>() && W.IsReady()) W.Cast(target);
            }

            //Manual cast R
            if (Config.Item("RManualCast" + Id).GetValue<KeyBind>().Active)
            {
                var rTarget = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
                R.Cast(rTarget);
            }
        }

        public override void ExecuteJungleClear()
        {
            if (Q.IsReady() && JhinQCastReady)
            {
                var jE = GetValue<StringList>("UseQJ").SelectedIndex;
                if (jE != 0)
                {
                    if (jE == 1)
                    {
                        var jungleMobs = Utils.Utils.GetMobs(
                            Marksman.Utils.Orbwalking.GetRealAutoAttackRange(null) + 65,
                            Utils.Utils.MobTypes.BigBoys);
                        if (jungleMobs != null)
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        var totalAa =
                            MinionManager.GetMinions(
                                ObjectManager.Player.Position,
                                Marksman.Utils.Orbwalking.GetRealAutoAttackRange(null) + 165,
                                MinionTypes.All,
                                MinionTeam.Neutral).Sum(mob => (int)mob.Health);
                        totalAa = (int)(totalAa / ObjectManager.Player.TotalAttackDamage());
                        if (totalAa > jE)
                        {
                            Q.Cast();
                        }
                    }
                }
            }

            if (W.IsReady())
            {
                var jungleMobs = Marksman.Utils.Utils.GetMobs(W.Range, Marksman.Utils.Utils.MobTypes.All);
                if (jungleMobs != null)
                {
                    var jW = GetValue<StringList>("UseWJ").SelectedIndex;
                    switch (jW)
                    {
                        case 1:
                            {
                                jungleMobs = Marksman.Utils.Utils.GetMobs(
                                    W.Range,
                                    Marksman.Utils.Utils.MobTypes.All,
                                    jW);
                                W.CastOnUnit(jungleMobs);
                                break;
                            }
                        case 2:
                            {
                                jungleMobs = Utils.Utils.GetMobs(W.Range, Utils.Utils.MobTypes.BigBoys);
                                if (jungleMobs != null)
                                {
                                    W.CastOnUnit(jungleMobs);
                                }
                                break;
                            }
                    }
                }
            }
        }

        public override void ExecuteLaneClear()
        {
            if (Q.IsReady() && JhinQCastReady)
            {
                var jQ = GetValue<StringList>("UseQ.Lane").SelectedIndex;
                if (jQ != 0)
                {
                    var totalAa =
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m => m.IsEnemy && !m.IsDead && m.IsValidTarget(Marksman.Utils.Orbwalking.GetRealAutoAttackRange(null)))
                            .Sum(mob => (int)mob.Health);

                    totalAa = (int)(totalAa / ObjectManager.Player.TotalAttackDamage());
                    if (totalAa > jQ)
                    {
                        Q.Cast();
                    }

                }
            }

            if (E.IsReady())
            {
                var minions = MinionManager.GetMinions(
                    ObjectManager.Player.Position,
                    E.Range,
                    MinionTypes.All,
                    MinionTeam.Enemy);

                if (minions != null)
                {
                    var jE = GetValue<StringList>("UseW.Lane").SelectedIndex;
                    if (jE != 0)
                    {
                        var mE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range, MinionTypes.All);
                        if (mE.Count >= jE)
                        {
                            W.Cast(mE[0].Position);
                        }
                    }
                }
            }
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseWC" + Id, "W").SetValue(true));

            var xRMenu = new Menu("R", "ComboR");
            {
                xRMenu.AddItem(new MenuItem("UseRC", "Use").SetValue(true));
                xRMenu.AddItem(new MenuItem("UseRCMinRange", "Min. Range").SetValue(new Slider(200, 200, 1000)));
                xRMenu.AddItem(new MenuItem("UseRCMaxRange", "Max. Range").SetValue(new Slider(500, 500, 2000)));
                xRMenu.AddItem(
                    new MenuItem("DrawRMin", "Draw Min. R Range").SetValue(
                        new Circle(true, System.Drawing.Color.DarkRed)));
                xRMenu.AddItem(
                    new MenuItem("DrawRMax", "Draw Max. R Range").SetValue(
                        new Circle(true, System.Drawing.Color.DarkMagenta)));

                config.AddSubMenu(xRMenu);
            }
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseWH" + Id, "W").SetValue(true));
            config.AddItem(
                new MenuItem("UseWTH", "Use W (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            string[] strQ = new string[7];
            strQ[0] = "Off";

            for (var i = 1; i < 7; i++)
            {
                strQ[i] = "If need to AA more than >= " + i;
            }

            config.AddItem(new MenuItem("UseQ.Lane" + Id, Utils.Utils.Tab + "Use Q:").SetValue(new StringList(strQ, 0)));

            string[] strW = new string[5];
            strW[0] = "Off";

            for (var i = 1; i < 5; i++)
            {
                strW[i] = "If W it'll Hit >= " + i;
            }

            config.AddItem(new MenuItem("UseW.Lane" + Id, Utils.Utils.Tab + "Use W:").SetValue(new StringList(strW, 0)));

            config.AddItem(
                new MenuItem("UseQ.Lane.UnderTurret" + Id, Utils.Utils.Tab + "Always Use Q Under Ally Turrent:")
                    .SetValue(true));
            config.AddItem(
                new MenuItem("UseW.Lane.UnderTurret" + Id, Utils.Utils.Tab + "Always Use W Under Ally Turrent:")
                    .SetValue(true));
            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            string[] strQ = new string[8];
            {
                strQ[0] = "Off";
                strQ[1] = "Just for big Monsters";

                for (var i = 2; i < 8; i++)
                {
                    strQ[i] = "If need to AA more than >= " + i;
                }

                config.AddItem(new MenuItem("UseQJ" + Id, "Use Q").SetValue(new StringList(strQ, 4)));
            }

            string[] strW = new string[4];
            {
                strW[0] = "Off";
                strW[1] = "Just for big Monsters";

                for (var i = 2; i < 4; i++)
                {
                    strW[i] = "If Mobs Count >= " + i;
                }

                config.AddItem(new MenuItem("UseWJ" + Id, "Use W").SetValue(new StringList(strW, 1)));
            }
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("Draw.X", "W range").SetValue(new Slider(500,0, 4000)));

            //config.AddItem(new MenuItem("DrawW" + Id, "W range").SetValue(new Circle(true, System.Drawing.Color.CornflowerBlue)));
            return true;
        }

        private static void DrawingOnOnEndScene(EventArgs args)
        {
            Utility.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White, 1, 23, true);
            Utility.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Aqua, 1, 23, true);
            var rCircle2 = Program.Config.Item("Draw.X").GetValue<Slider>().Value;
                Utility.DrawCircle(ObjectManager.Player.Position, rCircle2, System.Drawing.Color.Red, 1, 23, true);
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("RInterruptable" + Id, "Auto R Interruptable Spells").SetValue(true));
            config.AddItem(new MenuItem("EFlash" + Id, "Use E against FlJhins").SetValue(true));
            config.AddItem(new MenuItem("RManualCast" + Id, "Cast R Manually(2000 range)"))
                .SetValue(new KeyBind('T', KeyBindType.Press));
            return true;
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            var drawx = Config.Item("Draw.X").GetValue<Slider>().Value;
            Render.Circle.DrawCircle(ObjectManager.Player.Position, drawx, System.Drawing.Color.Red);
            return;
            //foreach (var e in HeroManager.Enemies.Where(e => e.IsValidTarget(3500)))
            //{
            //    var x = new Geometry.Polygon.Line(e.Position, e.Path[0]);

            //    x.Draw(System.Drawing.Color.Red, 3);

            //}
            //return;
            var drawW = Config.Item("DrawW" + Id).GetValue<Circle>();
            if (drawW.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, drawW.Color);
            }

            var drawRMin = Program.Config.SubMenu("Combo").Item("DrawRMin").GetValue<Circle>();
            if (drawRMin.Active)
            {
                var minRRange = Program.Config.SubMenu("Combo").Item("UseRCMinRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, minRRange, drawRMin.Color, 2);
            }

            var drawRMax = Program.Config.SubMenu("Combo").Item("DrawRMax").GetValue<Circle>();
            if (drawRMax.Active)
            {
                var maxRRange = Program.Config.SubMenu("Combo").Item("UseRCMaxRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(ObjectManager.Player.Position, maxRRange, drawRMax.Color, 2);
            }
        }
    }
}
