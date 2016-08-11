#region

using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


#endregion

namespace Marksman.Champions
{
    using System.Linq;

    using Utils = LeagueSharp.Common.Utils;

    internal class Caitlyn : Champion
    {
        public static Spell R;

        public Spell E;

        public Spell Q;

        public bool ShowUlt;

        public string UltTarget;

        public Spell W;

        private bool canCastR = true;

        public Caitlyn()
        {
            Q = new Spell(SpellSlot.Q, 1240);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.25f, 60f, 2000f, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.25f, 80f, 1600f, true, SkillshotType.SkillshotLine);

            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnEndScene += DrawingOnOnEndScene;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            Obj_AI_Base.OnBuffAdd += (sender, args) =>
                {
                    if (W.IsReady())
                    {
                        BuffInstance aBuff =
                            (from fBuffs in
                                 sender.Buffs.Where(
                                     s =>
                                     sender.Team != ObjectManager.Player.Team
                                     && sender.Distance(ObjectManager.Player.Position) < W.Range)
                             from b in new[]
                                           {
                                               "teleport", /* Teleport */ "pantheon_grandskyfall_jump", /* Pantheon */ 
                                               "crowstorm", /* FiddleScitck */
                                               "zhonya", "katarinar", /* Katarita */
                                               "MissFortuneBulletTime", /* MissFortune */
                                               "gate", /* Twisted Fate */
                                               "chronorevive" /* Zilean */
                                           }
                             where args.Buff.Name.ToLower().Contains(b)
                             select fBuffs).FirstOrDefault();

                        if (aBuff != null)
                        {
                            W.Cast(sender.Position);
                        }
                    }
                };

            Marksman.Utils.Utils.PrintMessage("Caitlyn loaded.");
        }

        public void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

            if (Program.Config.Item("Misc.AntiGapCloser").GetValue<bool>())
            {
                return;
            }

            

            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range))
            {
                E.CastOnUnit(gapcloser.Sender);
            }
        }

        public void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender is Obj_AI_Turret && args.Target.IsMe)
            {
                canCastR = false;
            }
            else
            {
                canCastR = true;
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            Spell[] spellList = { Q, E, R };
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active) Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var drawUlt = GetValue<Circle>("DrawUlt");
            if (drawUlt.Active && ShowUlt)
            {
                //var playerPos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                //Drawing.DrawText(playerPos.X - 65, playerPos.Y + 20, drawUlt.Color, "Hit R To kill " + UltTarget + "!");
            }
        }

        private static void DrawingOnOnEndScene(EventArgs args)
        {
            var rCircle2 = Program.Config.Item("Draw.UltiMiniMap").GetValue<Circle>();
            if (rCircle2.Active)
            {
#pragma warning disable 618
                Utility.DrawCircle(ObjectManager.Player.Position, R.Range, rCircle2.Color, 1, 23, true);
#pragma warning restore 618
            }
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            R.Range = 500 * (R.Level == 0 ? 1 : R.Level) + 1500;

            Obj_AI_Hero t;

            if (W.IsReady() && GetValue<bool>("AutoWI"))
            {
                t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(W.Range)
                    && (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare)
                        || t.HasBuffOfType(BuffType.Taunt) || t.HasBuff("zhonyasringshield") || t.HasBuff("Recall")))
                {
                    W.Cast(t.Position);
                }
            }

            if (Q.IsReady() && GetValue<bool>("AutoQI"))
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(Q.Range)
                    && (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare)
                        || t.HasBuffOfType(BuffType.Taunt)
                        && (t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)
                            || !Orbwalking.InAutoAttackRange(t))))
                {
                    Q.Cast(t, false, true);
                }
            }

            if (R.IsReady())
            {
                t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(R.Range) && t.Health <= R.GetDamage(t))
                {
                    if (GetValue<KeyBind>("UltHelp").Active && canCastR) R.Cast(t);

                    UltTarget = t.ChampionName;
                    ShowUlt = true;
                }
                else
                {
                    ShowUlt = false;
                }
            }
            else
            {
                ShowUlt = false;
            }

            if (GetValue<KeyBind>("Dash").Active && E.IsReady())
            {
                var pos = ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), -300).To3D();
                E.Cast(pos, true);
            }

            if (GetValue<KeyBind>("UseEQC").Active && E.IsReady() && Q.IsReady())
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(E.Range)
                    && t.Health
                    < ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)
                    + ObjectManager.Player.GetSpellDamage(t, SpellSlot.E) + 20 && E.CanCast(t))
                {
                    E.Cast(t);
                    Q.Cast(t, false, true);
                }
            }

            // PQ you broke it D:
            if ((!ComboActive && !HarassActive) || !Orbwalking.CanMove(100)) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            var useE = GetValue<bool>("UseEC");
            var useR = GetValue<bool>("UseRC");

            if (Q.IsReady() && useQ)
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t != null)
                {
                    Q.Cast(t, false, true);
                }
            }
            else if (E.IsReady() && useE)
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t != null && t.Health <= E.GetDamage(t))
                {
                    E.Cast(t);
                }
            }

            if (R.IsReady() && useR)
            {
                t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (t != null && t.Health <= R.GetDamage(t) && !Orbwalking.InAutoAttackRange(t) && canCastR)
                {
                    R.CastOnUnit(t);
                }
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as Obj_AI_Hero;
            if (t == null || (!ComboActive && !HarassActive) || unit.IsMe) return;

            var useQ = GetValue<bool>("UseQ" + (ComboActive ? "C" : "H"));
            if (useQ) Q.Cast(t, false, true);

            base.Orbwalking_AfterAttack(unit, target);
        }

        public override bool MainMenu(Menu config)
        {
            return base.MainMenu(config);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQC" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("UseEC" + Id, "Use E").SetValue(true));
            config.AddItem(new MenuItem("UseRC" + Id, "Use R").SetValue(true));

            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(new MenuItem("Champion.Drawings", ObjectManager.Player.ChampionName + " Draw Options"));
            config.AddItem(
                new MenuItem("DrawQ" + Id, Marksman.Utils.Utils.Tab + "Q range").SetValue(
                    new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, Marksman.Utils.Utils.Tab + "E range").SetValue(
                    new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, Marksman.Utils.Utils.Tab + "R range").SetValue(
                    new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawUlt" + Id, Marksman.Utils.Utils.Tab + "Ult Text").SetValue(
                    new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            config.AddItem(
                new MenuItem("Draw.UltiMiniMap", Marksman.Utils.Utils.Tab + "Draw Ulti Minimap").SetValue(
                    new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Misc.AntiGapCloser" + Id, "E Anti Gap Closer").SetValue(true));
            config.AddItem(new MenuItem("UltHelp" + Id, "Ult Target on R").SetValue(new KeyBind("R".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("UseEQC" + Id, "Use E-Q Combo").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("Dash" + Id, "Dash to Mouse").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));
            config.AddItem(new MenuItem("AutoQI" + Id, "Auto Q (Stun/Snare/Taunt/Slow)").SetValue(true));
            config.AddItem(new MenuItem("AutoWI" + Id, "Auto W (Stun/Snare/Taunt)").SetValue(true));

            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {
            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            return true;
        }

        public override void ExecuteFlee()
        {
            if (E.IsReady())
            {
                var pos = Vector3.Zero;
                var enemy =
                    HeroManager.Enemies.FirstOrDefault(
                        e =>
                            e.IsValidTarget(E.Range +
                                            (ObjectManager.Player.MoveSpeed > e.MoveSpeed
                                                ? ObjectManager.Player.MoveSpeed - e.MoveSpeed
                                                : e.MoveSpeed - ObjectManager.Player.MoveSpeed)) && E.CanCast(e));

                pos = enemy?.Position ??
                      ObjectManager.Player.ServerPosition.To2D().Extend(Game.CursorPos.To2D(), -300).To3D();
                E.Cast(pos);
            }

            base.PermaActive();
        }
    }
}
