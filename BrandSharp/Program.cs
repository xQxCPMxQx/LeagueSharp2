using System;
using System.Collections.Generic;
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

            Q = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E, 625);
            R = new Spell(SpellSlot.R, 750);

            Q.SetSkillshot(0.25f, 66f, 1600, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.75f, 250f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetTargetted(0.25f, float.MaxValue);
            R.SetTargetted(0.25f, 1000f);

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu("Hellsing | Brand", "Brand", true);

            // Target selector
            var MenuTargetSelector = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(MenuTargetSelector);
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
            Drawings.AddItem(new MenuItem("DrawRangeAblazed", "Burning Enemy").SetValue(new Circle(false, Color.FromArgb(150, Color.Red))));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
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
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            var drawRangeAblazed = Config.SubMenu("Drawings").Item("DrawRangeAblazed").GetValue<Circle>();
            if (drawRangeAblazed.Active)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => !enemy.IsAlly && enemy.IsVisible && !enemy.IsDead && enemy.HasBuff("brandablaze", true)))
                {
                    Utility.DrawCircle(enemy.Position, 100f, drawRangeAblazed.Color);
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
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
                var target = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
                if (target != null)
                    W.CastIfHitchanceEquals(target, HitChance.High);
            }
        }

        private static void Combo()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.SubMenu("Combo").Item("ComboUseQ").GetValue<bool>() && Q.IsReady();
            var useW = Config.SubMenu("Combo").Item("ComboUseW").GetValue<bool>() && W.IsReady();
            var useE = Config.SubMenu("Combo").Item("ComboUseE").GetValue<bool>() && E.IsReady();
            var useR = Config.SubMenu("Combo").Item("ComboUseR").GetValue<bool>() && R.IsReady();

            // Killable status
            bool inMinimumRange = Vector2.DistanceSquared(eTarget.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range;

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
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.SubMenu("Combo").Item("HarassUseQ").GetValue<bool>() && Q.IsReady();
            var useW = Config.SubMenu("Combo").Item("HarassseW").GetValue<bool>() && W.IsReady();
            var useE = Config.SubMenu("Combo").Item("HarassUseE").GetValue<bool>() && E.IsReady();

            if (useW && wTarget != null)
            {
                W.CastIfHitchanceEquals(wTarget, HitChance.High);
            }

            if (useE && eTarget != null && useQ && qTarget != null)
            {
                E.CastOnUnit(eTarget);
                Q.CastIfHitchanceEquals(eTarget, HitChance.High);
            }
            else if (useE && eTarget != null)
            {
                E.CastOnUnit(eTarget);
            }
            else if (useQ && qTarget != null)
            {
                Q.CastIfHitchanceEquals(eTarget, HitChance.High);
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
                var minionsW = MinionManager.GetBestCircularFarmLocation(minions.Select(minion => minion.ServerPosition.To2D()).ToList(), W.Width, W.Range);

                if (minionsW.MinionsHit >= 2)
                    W.Cast(minionsW.Position);
            }

            if (useE)
            {
                foreach (var minion in minions.Where(minion => Vector2.DistanceSquared(minion.ServerPosition.To2D(), Player.Position.To2D()) < E.Range * E.Range)
                    .Where(minion =>
                        IsAblazed(minion) || minion.Health > Player.GetAutoAttackDamage(minion)))
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
