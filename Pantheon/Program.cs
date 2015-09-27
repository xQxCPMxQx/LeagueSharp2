#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Pantheon
{
    internal class Program
    {
        public const string ChampionName = "Pantheon";

        public static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        public static string Tab
        {
            get { return "       "; }
        } //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        private static bool usedSpell, shennBuffActive;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Spell R;

        private static Vector2 PingLocation;
        private static int LastPingT = 0;

        public static AssassinManager AssassinManager;
        public static PotionManager PotionManager;
        public static Utils Utils;
        public static Items Items;
        //Menu
        public static Menu Config;
        public static Menu menuMisc;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != "Pantheon")
                return;

            Q = new Spell(SpellSlot.Q, 620f);
            W = new Spell(SpellSlot.W, 620f);
            E = new Spell(SpellSlot.E, 640f);
            R = new Spell(SpellSlot.R, 5500f);

            Q.SetTargetted(0.2f, 1700f);
            W.SetTargetted(0.2f, 1700f);
            E.SetSkillshot(0.25f, 15f*2*(float) Math.PI/180, 2000f, false, SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu("xQx | Pantheon", "Pantheon", true);

            var targetSelectorMenu = new Menu("Target Selector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Utils = new Utils();
            Sprite.Load();
            Items = new Items();

            AssassinManager = new AssassinManager();
            AssassinManager.Load();


            // Combo
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
                    .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActiveT", "Harass (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                            KeyBindType.Toggle)))
                    .Permashow(true);
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            var menuLane = new Menu("Lane Mode", "Lane Mode");
            {
                menuLane.AddItem(
                    new MenuItem("Lane.UseQ", "Use Q").SetValue(
                        new StringList(new[] {"Off", "On", "Just out of AA Range"}, 1)));
                menuLane.AddItem(new MenuItem("Lane.UseE", "Use E").SetValue(false));

                menuLane.AddItem(
                    new MenuItem("Lane.Mana.Option", "Min. Mana Options").SetValue(
                        new StringList(new[] {"Don't check", "Min. Mana Percent", "Protect my mana for"}))
                        .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        menuLane.Item("Lane.Mana.MinMana").Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 1);
                        menuLane.Item("Lane.Mana.KeepQ").Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuLane.Item("Lane.Mana.KeepW").Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuLane.Item("Lane.Mana.KeepE").Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuLane.Item("Lane.Mana.KeepR").Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                    };
                menuLane.AddItem(
                    new MenuItem("Lane.Mana.MinMana", Tab + "Min. Mana Percent: ").SetValue(new Slider(30, 100, 0)));
                menuLane.AddItem(new MenuItem("Lane.Mana.KeepQ", Tab + "Q Spell").SetValue(true));
                menuLane.AddItem(new MenuItem("Lane.Mana.KeepW", Tab + "W Spell").SetValue(true));
                menuLane.AddItem(new MenuItem("Lane.Mana.KeepE", Tab + "E Spell").SetValue(true));
                menuLane.AddItem(new MenuItem("Lane.Mana.KeepR", Tab + "R Spell").SetValue(true));


                menuLane.AddItem(
                    new MenuItem("Lane.Exec", "Lane Clear Active!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press))).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
                Config.AddSubMenu(menuLane);
            }

            // Jungling Farm
            var menuJungle = new Menu("Jungle Mode", "Jungle Mode");
            Config.AddSubMenu(menuJungle);
            {
                menuJungle.AddItem(
                    new MenuItem("Jungle.UseQ", "Use Q").SetValue(
                        new StringList(new[] {"Off", "On", "Just for big Mobs"}, 1)));
                menuJungle.AddItem(
                    new MenuItem("Jungle.UseW", "Use W").SetValue(
                        new StringList(new[] {"Off", "On", "Just for big Mobs"}, 1)));
                menuJungle.AddItem(
                    new MenuItem("Jungle.UseE", "Use E").SetValue(
                        new StringList(new[] {"Off", "Mob Count > =1", "Mob Count > =2", "Mob Count > =3"}, 2)));

                menuJungle.AddItem(
                    new MenuItem("Jungle.Mana.Option", "Min. Mana Options").SetValue(
                        new StringList(new[] {"Don't check", "Min. Mana Percent", "Protect my mana (for Combo):"}))
                        .SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua)).ValueChanged +=
                    (sender, eventArgs) =>
                    {
                        menuJungle.Item("Jungle.Mana.MinMana")
                            .Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 1);

                        menuJungle.Item("Jungle.Mana.KeepQ")
                            .Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuJungle.Item("Jungle.Mana.KeepW")
                            .Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuJungle.Item("Jungle.Mana.KeepE")
                            .Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                        menuJungle.Item("Jungle.Mana.KeepR")
                            .Show(eventArgs.GetNewValue<StringList>().SelectedIndex == 2);
                    };

                menuJungle.AddItem(
                    new MenuItem("Jungle.Mana.MinMana", Tab + "Min. Mana Percent: ").SetValue(new Slider(30, 100, 0)));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.KeepQ", Tab + "Q Spell").SetValue(true));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.KeepW", Tab + "W Spell").SetValue(true));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.KeepE", Tab + "E Spell").SetValue(true));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.KeepR", Tab + "R Spell").SetValue(true));

                menuJungle.AddItem(
                    new MenuItem("Jungle.Mana.Dont.Title", "Dont control mana If I'm taking").SetFontStyle(
                        FontStyle.Regular, SharpDX.Color.IndianRed));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.Dont.Blue", Tab + "Blue Monster").SetValue(true));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.Dont.Red", Tab + "Red Monster").SetValue(false));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.Dont.Dragon", Tab + "Dragon").SetValue(true));
                menuJungle.AddItem(new MenuItem("Jungle.Mana.Dont.Baron", Tab + "Baron").SetValue(true));


                menuJungle.AddItem(
                    new MenuItem("JungleFarmActive", "JungleMode!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press))).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
            }

            menuMisc = new Menu("Misc", "Misc");
            {
                Config.AddSubMenu(menuMisc);
                menuMisc.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));
                menuMisc.AddItem(new MenuItem("PingLH", "Ping low health enemies (Only local)").SetValue(true));
            }

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            {
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("QRange", "Q Range").SetValue(
                            new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("WRange", "W Range").SetValue(
                            new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("ERange", "E Range").SetValue(
                            new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("RRange", "R Range").SetValue(
                            new Circle(false, Color.FromArgb(255, 255, 255, 255))));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("RRange2", "R Range (minimap)").SetValue(new Circle(true,
                            Color.FromArgb(255, 255, 255, 255))));

                var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
                Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);

                Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
                Utility.HpBarDamageIndicator.Enabled = true;
            }

            menuLane.Item("Lane.Mana.MinMana")
                .Show(menuLane.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex == 1);
            menuLane.Item("Lane.Mana.KeepQ")
                .Show(menuLane.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuLane.Item("Lane.Mana.KeepW")
                .Show(menuLane.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuLane.Item("Lane.Mana.KeepE")
                .Show(menuLane.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuLane.Item("Lane.Mana.KeepR")
                .Show(menuLane.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex == 2);

            menuJungle.Item("Jungle.Mana.MinMana")
                .Show(menuJungle.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex == 1);
            menuJungle.Item("Jungle.Mana.KeepQ")
                .Show(menuJungle.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuJungle.Item("Jungle.Mana.KeepW")
                .Show(menuJungle.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuJungle.Item("Jungle.Mana.KeepE")
                .Show(menuJungle.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex == 2);
            menuJungle.Item("Jungle.Mana.KeepR")
                .Show(menuJungle.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex == 2);


            PotionManager = new PotionManager();

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += DrawingOnOnEndScene;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Notifications.AddNotification(string.Format("{0} Loaded", ChampionName), 4000);
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs arg)
        {
            if (!sender.IsMe)
                return;

            if (arg.SData.Name.ToLower().Contains("pantheonq") || arg.SData.Name.ToLower().Contains("pantheonw"))
            {
                usedSpell = true;
            }
            else if (arg.SData.Name.ToLower().Contains("pantheone") || Player.HasBuff("sound", true))
            {
                usedSpell = true;
            }
            else if (arg.SData.Name.ToLower().Contains("attack"))
            {
                usedSpell = false;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color, 1);
            }

            Render.Circle.DrawCircle(Player.Position, 30f, Color.Red, 1, true);
        }

        private static void DrawingOnOnEndScene(EventArgs args)
        {
            var rCircle2 = Config.Item("RRange2").GetValue<Circle>();
            if (rCircle2.Active)
            {
                Utility.DrawCircle(ObjectManager.Player.Position, 5500, rCircle2.Color, 1, 23, true);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            shennBuffActive = Player.HasBuff("Sheen", true);

            if (Config.Item("PingLH").GetValue<bool>())
                foreach (
                    var enemy in
                        HeroManager.Enemies.Where(
                            t =>
                                ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready &&
                                t.IsValidTarget() && ComboDamage(t) > t.Health))
                {
                    Ping(enemy.Position.To2D());
                }

            if (!Orbwalking.CanMove(100))
                return;

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                (Config.Item("HarassActiveT").GetValue<KeyBind>().Active &&
                 Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo))
            {
                var existsMana = Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.ManaPercent >= existsMana)
                    Harass();
            }

            if (Config.Item("Lane.Exec").GetValue<KeyBind>().Active)
            {
                LaneMode();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                JungleMode();
            }
        }

        private static float ComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += Player.GetSpellDamage(t, SpellSlot.W);

            if (E.IsReady())
                fComboDamage += Player.GetSpellDamage(t, SpellSlot.E);

            if (PlayerSpells.IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            return (float) fComboDamage;
        }

        private static void Combo()
        {
            Obj_AI_Hero t;
            t = AssassinManager.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            //if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65) && (shennBuffActive || usedSpell))
            if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65) && (shennBuffActive))
                return;

            if (W.IsReady())
            {
                W.CastOnUnit(t);
            }
            else if (Q.IsReady())
            {
                Q.CastOnUnit(t);
            }
            else if (E.IsReady() && !Player.HasBuff("sound", true) && !Q.IsReady() && !W.IsReady())
            {
                E.Cast(t.Position);
            }

            if (PlayerSpells.IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready &&
                Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
            {
                Player.Spellbook.CastSpell(PlayerSpells.IgniteSlot, t);
            }
            CastItems();
        }

        private static void Harass()
        {
            var useQ = Config.Item("UseQHarass").GetValue<bool>() && Q.IsReady();
            var useE = Config.Item("UseEHarass").GetValue<bool>() && E.IsReady();

            Obj_AI_Hero t;

            if (useQ)
            {
                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                    Q.CastOnUnit(t);
            }

            if (useE && !Player.HasBuff("sound", true) && !Q.IsReady() && !W.IsReady())
            {
                t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    E.Cast(t.Position);
                }
            }
        }

        private static bool CheckModeManaState(string menuName, bool dontCheckmana = false)
        {
            if (dontCheckmana)
            {
                return true;
            }

            switch (Config.Item(menuName + ".Mana.Option").GetValue<StringList>().SelectedIndex)
            {
                case 1:
                    if (Player.ManaPercent < Config.Item(menuName + ".Mana.MinMana").GetValue<Slider>().Value)
                    {
                        return false;
                    }
                    break;

                case 2:
                    var protectMana = (Config.Item(menuName + ".Mana.KeepQ").GetValue<bool>() && Q.Level > 0
                        ? Q.ManaCost
                        : 0) +
                                      (Config.Item(menuName + ".Mana.KeepW").GetValue<bool>() && W.Level > 0
                                          ? W.ManaCost
                                          : 0) +
                                      (Config.Item(menuName + ".Mana.KeepE").GetValue<bool>() && E.Level > 0
                                          ? E.ManaCost
                                          : 0) +
                                      (Config.Item(menuName + ".Mana.KeepR").GetValue<bool>() && R.Level > 0 &&
                                       R.Cooldown < 20
                                          ? R.ManaCost
                                          : 0);

                    if (Player.Mana < protectMana)
                        return false;
                    break;
            }

            return true;
        }

        private static void JungleMode()
        {
            var bigBoys = Utils.GetMobs(W.Range, Utils.MobTypes.BigBoys);

            if (Config.Item("Jungle.Mana.Option").GetValue<StringList>().SelectedIndex != 0)
            {

                var dontCheckMana = false;
                if (bigBoys != null)
                {
                    Game.PrintChat(bigBoys.Name);
                    dontCheckMana = (Config.Item("Jungle.Mana.Dont.Blue").GetValue<bool>() &&
                                     bigBoys.Name.Contains("SRU_Blue"))
                                    ||
                                    (Config.Item("Jungle.Mana.Dont.Red").GetValue<bool>() &&
                                     bigBoys.Name.Contains("SRU_Red"))
                                    ||
                                    (Config.Item("Jungle.Mana.Dont.Dragon").GetValue<bool>() &&
                                     bigBoys.Name.Contains("SRU_Dragon"))
                                    ||
                                    (Config.Item("Jungle.Mana.Dont.Baron").GetValue<bool>() &&
                                     bigBoys.Name.Contains("SRU_Baron"));
                }

                if (!CheckModeManaState("Jungle", dontCheckMana))
                {
                    return;
                }
            }

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
            {
                return;
            }

            var mob = mobs[0];
            var useQ = Config.Item("Jungle.UseQ").GetValue<StringList>().SelectedIndex;
            var useW = Config.Item("Jungle.UseW").GetValue<StringList>().SelectedIndex;

            switch (useQ)
            {
                case 1:
                    Q.CastOnUnit(mobs[0]);
                    break;

                case 2:
                    if (bigBoys != null)
                        Q.CastOnUnit(bigBoys);
                    break;
            }

            switch (useW)
            {
                case 1:
                    W.CastOnUnit(mobs[0]);
                    break;

                case 2:
                    if (bigBoys != null)
                        W.CastOnUnit(bigBoys);
                    break;
            }

            var eMob = Config.Item("Jungle.UseE").GetValue<StringList>().SelectedIndex;
            if (E.IsReady() && eMob != 0)
            {
                if (mobs.Count >= eMob)
                {
                    E.Cast(mob.Position);
                }
            }

            if (mobs.Count >= 2)
            {
                foreach (var item in from item in Items.ItemDb
                    where
                        item.Value.ItemType == Items.EnumItemType.AoE
                        && item.Value.TargetingType == Items.EnumItemTargettingType.EnemyObjects
                    let iMinions =
                        MinionManager.GetMinions(
                            ObjectManager.Player.ServerPosition,
                            item.Value.Item.Range,
                            MinionTypes.All,
                            MinionTeam.Neutral)
                    where
                        item.Value.Item.IsReady()
                        && iMinions[0].Distance(Player.Position) < item.Value.Item.Range
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }


        }

        private static void LaneMode()
        {
            if (Config.Item("Lane.Mana.Option").GetValue<StringList>().SelectedIndex != 0 &&
                !CheckModeManaState("Jungle"))
            {
                return;
            }

            var useQ = Config.Item("Lane.UseQ").GetValue<StringList>().SelectedIndex;
            var useE = Config.Item("Lane.UseE").GetValue<bool>() && E.IsReady();

            var vMinions = MinionManager.GetMinions(ObjectManager.Player.Position, Q.Range);
            foreach (var minions in
                vMinions.Where(
                    minions => minions.Health < Q.GetDamage(minions)))
            {
                if (useQ == 1)
                {
                    Q.Cast(minions);
                }
                else if (useQ == 2)
                {
                    if (minions.Distance(Player.Position) > Orbwalking.GetRealAutoAttackRange(null) + 65)
                        Q.Cast(minions);
                }

            }

            if (useE)
            {
                var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width,
                    MinionTypes.Ranged);

                var locE = E.GetCircularFarmLocation(rangedMinionsE, E.Range);
                if (locE.MinionsHit >= 2 && E.IsInRange(locE.Position.To3D()))
                {
                    E.Cast(locE.Position);
                }
            }

            foreach (var item in from item in Items.ItemDb
                where
                    item.Value.ItemType == Items.EnumItemType.AoE
                    && item.Value.TargetingType == Items.EnumItemTargettingType.EnemyObjects
                let iMinions =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        item.Value.Item.Range)
                where
                    iMinions.Count >= 2 && item.Value.Item.IsReady()
                    && iMinions[0].Distance(Player.Position) < item.Value.Item.Range
                select item)
            {
                item.Value.Item.Cast();
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;

            if (unit.IsValidTarget(W.Range) && W.IsReady())
            {
                W.CastOnUnit(unit);
            }
        }

        private static void CastItems()
        {
            var t = AssassinManager.GetTarget(750, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget())
                return;

            foreach (var item in Items.ItemDb)
            {
                if (item.Value.ItemType == Items.EnumItemType.AoE &&
                    item.Value.TargetingType == Items.EnumItemTargettingType.EnemyObjects)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady())
                    {
                        item.Value.Item.Cast(Player);
                    }
                }

                if (item.Value.ItemType == Items.EnumItemType.Targeted &&
                    item.Value.TargetingType == Items.EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady())
                    {
                        item.Value.Item.Cast(t);
                    }
                }

            }
        }


        private static void Ping(Vector2 position)
        {
            if (LeagueSharp.Common.Utils.TickCount - LastPingT < 30*1000)
            {
                return;
            }

            LastPingT = LeagueSharp.Common.Utils.TickCount;
            PingLocation = position;
            SimplePing();

            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
            Utility.DelayAction.Add(800, SimplePing);
        }

        private static void SimplePing()
        {
            Game.ShowPing(PingCategory.Fallback, PingLocation, true);
        }

    }
}