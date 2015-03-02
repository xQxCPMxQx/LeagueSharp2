#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace Akali
{
    internal class Program
    {
        public const string ChampionName = "Akali";

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static List<Spell> SpellList = new List<Spell>();

        public static SpellSlot IgniteSlot;
        public static Items.Item Hex;
        public static Items.Item Cutlass;

        public static Orbwalking.Orbwalker Orbwalker;

        public static Menu Config;

        private static Obj_AI_Hero Player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName)
                return;

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 290f);
            R = new Spell(SpellSlot.R, 800f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            Hex = new Items.Item(3146, 700);
            Cutlass = new Items.Item(3144, 450);

            Config = new Menu("xQx | " + ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            new AssassinManager();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboActive", "Combo!").SetValue(
                            new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassUseQT", "Use Q (toggle)!").SetValue(
                            new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(
                            new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            {
                Config.SubMenu("Farm")
                    .AddItem(
                        new MenuItem("UseQFarm", "Use Q").SetValue(
                            new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 2)));
                Config.SubMenu("Farm")
                    .AddItem(
                        new MenuItem("UseEFarm", "Use E").SetValue(
                            new StringList(new[] { "Freeze", "LaneClear", "Both", "No" }, 1)));
                Config.SubMenu("Farm")
                    .AddItem(
                        new MenuItem("FreezeActive", "Freeze!").SetValue(
                            new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
                Config.SubMenu("Farm")
                    .AddItem(
                        new MenuItem("LaneClearActive", "LaneClear!").SetValue(
                            new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(
                            new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("KillstealR", "Killsteal R").SetValue(false));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("QRange", "Q Range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("RRange", "R Range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));

            Config.AddToMainMenu();

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;

            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx |</font> <font color='#FFFFFF'>{0} Loaded!</font>", ChampionName));
        }

        private static Obj_AI_Hero enemyHaveMota
        {
            get
            {
                return
                    (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget(R.Range))
                        from buff in enemy.Buffs
                        where buff.DisplayName == "AkaliMota"
                        select enemy).FirstOrDefault();
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(vTarget, SpellSlot.Q) +
                                ObjectManager.Player.GetSpellDamage(vTarget, SpellSlot.Q, 1);

            if (E.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(vTarget, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(vTarget, SpellSlot.R) * R.Instance.Ammo;
            ;

            if (Items.CanUseItem(3146))
                fComboDamage += ObjectManager.Player.GetItemDamage(vTarget, Damage.DamageItems.Hexgun);

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            return (float) fComboDamage;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.CountEnemiesInRange(350) >= 2)
            {
                if (W.IsReady())
                {
                    W.Cast(Player.Position);
                }
            }

            if (ObjectManager.Player.HasBuff("zedulttargetmark", true))
            {
                if (W.IsReady())
                {
                    W.Cast(Player.Position);
                }
            }

            /*
            foreach (var t1 in ObjectManager.Player.Buffs)
            {
                if (t1.Name.ToLower().Contains("zedulttargetmark"))
                {
                    if (W.IsReady())
                    {
                        W.Cast(Player.Position);
                        Game.PrintChat("Zed Ulti Used");
                    }
                }
            }
            */
            Orbwalker.SetAttack(true);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                     Config.Item("HarassUseQT").GetValue<KeyBind>().Active)
                Harass();

            var lc = Config.Item("LaneClearActive").GetValue<KeyBind>().Active;
            if (lc || Config.Item("FreezeActive").GetValue<KeyBind>().Active)
                Farm(lc);

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                JungleFarm();
            if (Config.Item("KillstealR").GetValue<bool>())
            {
                Killsteal();
            }
        }

        private static void Combo()
        {
            var t = GetTarget(R.Range, TargetSelector.DamageType.Magical);

            Orbwalker.SetAttack(!R.IsReady() && !Q.IsReady() && !E.IsReady() && Geometry.Distance(Player, t) < 800f);

            var motaEnemy = enemyHaveMota;

            if (GetComboDamage(t) > t.Health && IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
            }

            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(t);
            }

            if (Hex.IsReady() && t.IsValidTarget(Hex.Range))
            {
                Hex.Cast(t);
            }

            if (Cutlass.IsReady() && t.IsValidTarget(Cutlass.Range))
            {
                Cutlass.Cast(t);
            }

            if (motaEnemy != null && motaEnemy.IsValidTarget(Orbwalking.GetRealAutoAttackRange(t)))
                return;

            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.Cast();
            }

            if (R.IsReady() && t.IsValidTarget(R.Range))
            {
                R.CastOnUnit(t);
            }
        }

        private static void Harass()
        {
            var t = GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (Q.IsReady() && t.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(t);
            }

            if (E.IsReady() && t.IsValidTarget(E.Range))
            {
                E.Cast();
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalking.CanMove(40))
                return;

            var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range);
            var useQi = Config.Item("UseQFarm").GetValue<StringList>().SelectedIndex;
            var useEi = Config.Item("UseEFarm").GetValue<StringList>().SelectedIndex;
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            if (useQ && Q.IsReady())
            {
                foreach (var minion in allMinions)
                {
                    if (minion.IsValidTarget() &&
                        HealthPrediction.GetHealthPrediction(minion, (int) (Player.Distance(minion) * 1000 / 1400)) <
                        0.75 * Player.GetSpellDamage(minion, SpellSlot.Q))
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }
            }

            else if (useE && E.IsReady())
            {
                if (
                    allMinions.Any(
                        minion =>
                            minion.IsValidTarget(E.Range) &&
                            minion.Health < 0.75 * Player.GetSpellDamage(minion, SpellSlot.E)))
                {
                    E.Cast();
                    return;
                }
            }

            if (laneClear)
            {
                foreach (var minion in allMinions)
                {
                    if (useQ)
                        Q.CastOnUnit(minion);

                    if (useE)
                        E.Cast();
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(
                Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (Q.IsReady())
                    Q.CastOnUnit(mob);

                if (E.IsReady())
                    E.Cast();
            }
        }

        private static void Killsteal()
        {
            var useR = Config.Item("KillstealR").GetValue<bool>() && R.IsReady();
            if (useR)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(R.Range)))
                {
                    if (hero.Distance(ObjectManager.Player) <= R.Range &&
                        Player.GetSpellDamage(hero, SpellSlot.R) >= hero.Health)
                        R.CastOnUnit(hero, true);
                }
            }
        }

        private static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = Q.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1);
            }
        }
    }
}