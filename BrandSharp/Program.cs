using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace BrandSharp
{
    class Program
    {
        private const string ChampionName = "Brand";
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        private static Spell Q, W, E, R;
        private static readonly List<Spell> SpellList = new List<Spell>();

        private static readonly SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        public static Orbwalking.Orbwalker Orbwalker;
        private static Menu Config;

        static void Main(string[] args)
        {
            // Register load event
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != ChampionName)
                return;

            Q = new Spell(SpellSlot.Q, 1080);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            Q.SetSkillshot(0.25f, 66f, 1400, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.75f, 250f, 800, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, 1000f);

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu("Hellsing | Brand", "Brand", true);

            // Target selector
            var MenuTargetSelector = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(MenuTargetSelector);
            Config.AddSubMenu(MenuTargetSelector);

            // Orbwalker
            var MenuOrbwalker = new Menu("Orbwalking", "Orbwalking");
            Orbwalker = new Orbwalking.Orbwalker(MenuOrbwalker);
            Config.AddSubMenu(MenuOrbwalker);

            var Combo = new Menu("Combo", "Combo");
            Config.AddSubMenu(Combo);
            Combo.AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
            Combo.AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
            Combo.AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
            Combo.AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));
            Combo.AddItem(new MenuItem("ComboActive", "Combo Active!").SetValue<KeyBind>(new KeyBind(32, KeyBindType.Press)));

            var Harass = new Menu("Harass", "Harass");
            Config.AddSubMenu(Harass);
            Harass.AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
            Harass.AddItem(new MenuItem("HarassUseQT", "Use Q (Toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Harass.AddItem(new MenuItem("HarassUseW", "Use W").SetValue(true));
            Harass.AddItem(new MenuItem("HarassUseWT", "Use W (Toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));
            Harass.AddItem(new MenuItem("HarassUseE", "Use E").SetValue(true));
            Harass.AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Harass.AddItem(new MenuItem("HarassActive", "Harass Active!").SetValue<KeyBind>(new KeyBind('C', KeyBindType.Press)));

            var LaneClear = new Menu("LaneClear", "LaneClear");
            Config.AddSubMenu(LaneClear);
            LaneClear.AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(true));
            LaneClear.AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(true));
            LaneClear.AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(true));
            LaneClear.AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            LaneClear.AddItem(new MenuItem("LaneClearActive", "LaneClear Active!").SetValue<KeyBind>(new KeyBind('V', KeyBindType.Press)));

            var JungleFarm = new Menu("JungleFarm", "JungleFarm");
            Config.AddSubMenu(JungleFarm);
            JungleFarm.AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
            JungleFarm.AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(true));
            JungleFarm.AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(true));
            JungleFarm.AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            JungleFarm.AddItem(new MenuItem("JungleFarmActive", "JungleFarm Active!").SetValue<KeyBind>(new KeyBind('V', KeyBindType.Press)));

            var Drawings = new Menu("Drawings", "Drawings");
            Config.AddSubMenu(Drawings);
            Drawings.AddItem(new MenuItem("DrawRangeQ", "Q Range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            Drawings.AddItem(new MenuItem("DrawRangeW", "W Range").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            Drawings.AddItem(new MenuItem("DrawRangeE", "E Range").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            Drawings.AddItem(new MenuItem("DrawRangeR", "R Range").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            Drawings.AddItem(new MenuItem("DrawBurningEnemy", "Burning Enemy").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));
            Drawings.AddItem(new MenuItem("DrawBurningMinions", "Burning Minions").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat(String.Format("<font color='#70DBDB'>Hellsing / xQx</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded! Visit our forum http://www.joduska.me</font>", ChampionName));
        }

        private static bool IsAblazed(Obj_AI_Base target)
        {
            return target.HasBuff("brandablaze", true);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.SubMenu("Drawings").Item("DrawRange" + spell.Slot).GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var drawBurningEnemy = Config.SubMenu("Drawings").Item("DrawBurningEnemy").GetValue<Circle>();
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => !enemy.IsDead && enemy.IsEnemy && IsAblazed(enemy) && enemy.IsVisible))
            {
                Render.Circle.DrawCircle(enemy.Position, 90f, Color.White);
                Render.Circle.DrawCircle(enemy.Position, 95f, drawBurningEnemy.Color);
                Render.Circle.DrawCircle(enemy.Position, 100f, Color.Wheat);
            }

            var drawBurningMinions = Config.SubMenu("Drawings").Item("DrawBurningMinions").GetValue<Circle>();
            foreach (var enemy in ObjectManager.Get<Obj_AI_Minion>().Where(enemy => !enemy.IsDead && enemy.IsEnemy && IsAblazed(enemy) && Player.Distance(enemy) < E.Range && E.IsReady()))
            {
                Render.Circle.DrawCircle(enemy.Position, 50f, Color.White);
                Render.Circle.DrawCircle(enemy.Position, 55f, drawBurningMinions.Color);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {

            var qTarget = TargetSelector.GetTarget(1500, TargetSelector.DamageType.Magical);
            if (qTarget != null)
            {
             //   Game.PrintChat(qTarget.ChampionName + " : " + qTarget.MoveSpeed.ToString());

            }
            if (Config.SubMenu("Combo").Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();

            if (Config.SubMenu("Harass").Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    Harass();
            }

            if (Config.SubMenu("LaneClear").Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.SubMenu("JungleFarm").Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    JungleFarm();
            }

            if (Config.SubMenu("harass").Item("harassToggleW").GetValue<bool>() && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (target != null)
                    W.CastIfHitchanceEquals(target, HitChance.High);
            }
        }

        private static void Combo()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.SubMenu("Combo").Item("ComboUseQ").GetValue<bool>();
            var useW = Config.SubMenu("Combo").Item("ComboUseW").GetValue<bool>();
            var useE = Config.SubMenu("Combo").Item("ComboUseE").GetValue<bool>();
            var useR = Config.SubMenu("Combo").Item("ComboUseR").GetValue<bool>();

            var cdQEx = Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
            var cdEEx = Player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;
            var cdE = Game.Time < cdEEx ? cdEEx - Game.Time : 0;
            
            // var cdW = Player.Spellbook.GetSpell(SpellSlot.W).Cooldown;
            // var cdE = Player.Spellbook.GetSpell(SpellSlot.E).Cooldown;

            //Game.PrintChat("Q Cd: " + cdQ + " | W Cd: " + cdW + " | E Cd: " + cdE);

            if (IsAblazed(qTarget))
            {
                if (qTarget != null && Q.IsReady() && useQ)
                    Q.Cast(qTarget, true);
                if (!Q.IsReady() && cdQ > 4)
                {
                    if (eTarget != null && E.IsReady() && useE)
                        E.CastOnUnit(eTarget, true);
                    if (useW && wTarget != null && W.IsReady())
                        W.Cast(wTarget);
                }
            }
            else
            {
                if (eTarget != null && E.IsReady() && useE)
                    E.CastOnUnit(eTarget, true);
                if (useW && wTarget != null && W.IsReady())
                    W.Cast(wTarget);
                if (Q.IsReady() && !E.IsReady() && !W.IsReady() && cdW > 4 && cdE > 4)
                    Q.Cast(qTarget, true);
            }

            if (rTarget != null && R.IsReady() && useR)
            {
                if (rTarget.Health < Player.GetSpellDamage(eTarget, SpellSlot.R))
                    R.CastOnUnit(rTarget);
            }

            if (rTarget != null && IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(rTarget, Damage.SummonerSpell.Ignite) >= rTarget.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, rTarget);
                }
            }
            return;
            if (qTarget != null)
            {
                if (eTarget != null && useE && E.IsReady())
                    E.CastOnUnit(eTarget);
                if (wTarget != null && useW && W.IsReady())
                    W.Cast(wTarget);
            }
            if (eTarget != null && E.IsReady() && useE)
                E.CastOnUnit(eTarget);

            if (useW && wTarget != null && W.IsReady())
                W.Cast(wTarget);

            if (qTarget != null && Q.IsReady() && useQ)
            {
                if (Player.Spellbook.GetSpell(SpellSlot.W).Cooldown > 2 && Player.Spellbook.GetSpell(SpellSlot.E).Cooldown > 2)
                    Q.Cast(qTarget);
                if (IsAblazed(qTarget))
                    Q.Cast(qTarget);
            }

            return;
            if (IsAblazed(qTarget) && useQ && qTarget != null && Q.IsReady())
                Q.CastIfHitchanceEquals(qTarget, HitChance.High);
            else
            {
                if (useW && wTarget != null && W.IsReady())
                    W.CastIfHitchanceEquals(wTarget, HitChance.High);

                if (useE && eTarget != null && E.IsReady())
                    E.CastOnUnit(eTarget);

                if (useQ && qTarget != null && Q.IsReady() && (!E.IsReady() || !useE))
                    Q.CastIfHitchanceEquals(qTarget, HitChance.High);
            }

            bool inMinimumRange = Vector2.DistanceSquared(eTarget.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range;

            if (Vector2.DistanceSquared(rTarget.ServerPosition.To2D(), Player.Position.To2D()) < R.Range * R.Range)
            {
                // Logic prechecks
                if ((useQ && Q.IsReady() && Q.GetPrediction(rTarget).Hitchance == HitChance.High || useW && W.IsReady()) && Player.Health / Player.MaxHealth > 0.4f)
                    

                // Single hit
                if (inMinimumRange)
                    R.CastOnUnit(rTarget);
            }
            
            return;
            
            // Killable status
            inMinimumRange = Vector2.DistanceSquared(eTarget.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range;

            foreach (var spell in SpellList.Where(spell => spell.IsReady()))
            {
                // Q
                if (spell.Slot == SpellSlot.Q && useQ)
                {
                    if ((inMinimumRange) || (!useW && !useE) || (IsAblazed(qTarget)) ||
                        (useW && !useE && !W.IsReady() && W.IsReady((int)(Player.Spellbook.GetSpell(SpellSlot.Q).Cooldown * 1000))) ||
                        ((useE && !useW || useW && useE) && !E.IsReady() && E.IsReady((int)(Player.Spellbook.GetSpell(SpellSlot.Q).Cooldown * 1000))))
                    {
                        Q.CastIfHitchanceEquals(qTarget, HitChance.High);
                    }
                }
                // W
                else if (spell.Slot == SpellSlot.W && useW)
                {
                    if ((inMinimumRange) || (!useE) || (IsAblazed(wTarget)) || (Vector2.DistanceSquared(wTarget.ServerPosition.To2D(), Player.Position.To2D()) > E.Range * E.Range) ||
                        (!E.IsReady() && E.IsReady((int)(Player.Spellbook.GetSpell(SpellSlot.W).Cooldown * 1000))))
                    {
                        W.CastIfHitchanceEquals(wTarget, HitChance.High);
                    }
                }
                // E
                else if (useE)
                {
                    if (Vector2.DistanceSquared(eTarget.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range)
                    {
                        if ((!useQ && !useW) || (E.Level >= 4) || (useQ && (Q.IsReady() || Player.Spellbook.GetSpell(SpellSlot.Q).Cooldown < 5)) || (useW && W.IsReady()))
                        {
                            E.CastOnUnit(eTarget);
                        }
                    }
                }
                // R
                else if (R.IsReady() && useR)
                {
                    if (Vector2.DistanceSquared(rTarget.ServerPosition.To2D(), Player.Position.To2D()) < R.Range * R.Range)
                    {
                        // Logic prechecks
                        if ((useQ && Q.IsReady() && Q.GetPrediction(rTarget).Hitchance == HitChance.High || useW && W.IsReady()) && Player.Health / Player.MaxHealth > 0.4f)
                            continue;

                        // Single hit
                        if (inMinimumRange)
                            R.CastOnUnit(rTarget);
                    }
                }
            }
        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.SubMenu("Harass").Item("HarassUseQ").GetValue<bool>();
            var useW = Config.SubMenu("Harass").Item("HarassUseW").GetValue<bool>();
            var useE = Config.SubMenu("Harass").Item("HarassUseE").GetValue<bool>();

            if (wTarget != null && useW && W.IsReady())
            {
                W.Cast(wTarget);
            }

            if (eTarget != null && useE && E.IsReady())
            {
                E.CastOnUnit(eTarget);
            }

            if (qTarget != null && useQ && Q.IsReady())
            {
                Q.Cast(eTarget);
            }
        }

        private static void LaneClear()
        {
            // Minions around
            var minions = MinionManager.GetMinions(Player.Position, W.Range + W.Width / 2);

            // Spell usage
            var useQ = Q.IsReady() && Config.SubMenu("LaneClear").Item("LaneClearUseQ").GetValue<bool>() && Q.IsReady();
            var useW = W.IsReady() && Config.SubMenu("LaneClear").Item("LaneClearUseW").GetValue<bool>() && W.IsReady();
            var useE = E.IsReady() && Config.SubMenu("LaneClear").Item("LaneClearUseE").GetValue<bool>() && E.IsReady();

            if (useQ)
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All,
                    MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);

                foreach (var vMinion in
                                       from vMinion in minionsQ
                                       let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                                       where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                                       select vMinion)
                {
                    Q.Cast(vMinion);
                }
            }

            if (useW)
            {
                var minionsW = MinionManager.GetBestCircularFarmLocation(minions
                    .Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                if (minionsW.MinionsHit >= 2)
                    W.Cast(minionsW.Position);
            }

            if (useE)
            {
                foreach (var minion in minions
                    .Where(minion => Vector2.DistanceSquared(minion.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range && minions.Count >= 3 && IsAblazed(minion)))
                {
                    E.CastOnUnit(minion);
                }
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>() && Q.IsReady();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>() && W.IsReady();
            var useE = Config.Item("JungleFarmUseE").GetValue<bool>() && E.IsReady();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];

            if (useW && mobs.Count >= 2)
                W.Cast(mob);

            if (useE && mob.HasBuff("brandablaze", true))
                E.CastOnUnit(mob);

            if (useQ && mob.HasBuff("brandablaze", true))
                Q.Cast(mob);

        }
    }
}
