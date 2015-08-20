#region
using System;
using System.Linq;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
#endregion

namespace XinZhao
{
    internal class Program
    {
        public static string ChampionName = "XinZhao";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static Orbwalking.Orbwalker Orbwalker;

        public static Utils Utils;
        public static AssassinManager AssassinManager;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;


        public static Items.Item Tiamat = new Items.Item(3077, 375);
        public static Items.Item Hydra = new Items.Item(3074, 375);

        public static Menu Config;

        private static string Tab
        {
            get { return "    "; }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.CharData.BaseSkinName != ChampionName)
                return;

            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 480);

            CreateChampionMenu();

            //PlayerSpells.Initialize();
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;

            WelcomeMessage();
        }

        static int GetHitsR
        {
            get
            {
                return
                    HeroManager.Enemies.Where(h => h.IsValidTarget(R.Range))
                        .Where(
                            enemy =>
                                R.WillHit(enemy, Player.Position) &&
                                Player.Distance(enemy.ServerPosition, true) < R.Range)
                        .ToList()
                        .Count;
            }
        }
        static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Config.Item("BlockR").GetValue<bool>())
            {
                return;
            }


            if (args.Slot == SpellSlot.R && GetHitsR == 0)
            {
                args.Process = false;
            }
        }

        static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;

            if (unit.IsValidTarget(R.Range) && args.DangerLevel >= Interrupter2.DangerLevel.Medium &&
                !unit.HasBuff("xenzhaointimidate"))
            {
                R.Cast();
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
                return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                {
                    LaneClear();
                    JungleFarm();
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawERange = Config.Item("DrawERange").GetValue<Circle>();
            if (drawERange.Active)
                Render.Circle.DrawCircle(Player.Position, E.Range, drawERange.Color, 1);

            var drawRRange = Config.Item("DrawRRange").GetValue<Circle>();
            if (drawRRange.Active)
                Render.Circle.DrawCircle(Player.Position, R.Range, drawRRange.Color, 1);

            var drawEMinRange = Config.Item("DrawEMinRange").GetValue<Circle>();
            if (drawEMinRange.Active)
            {
                var eMinRange = Config.Item("EMinRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(Player.Position, eMinRange, drawEMinRange.Color, 1);
            }

            /* [ Draw Can Be Thrown Enemy ] */
            var drawThrownEnemy = Config.SubMenu("Drawings").Item("DrawThrown").GetValue<Circle>();
            if (drawThrownEnemy.Active)
            {
                foreach (var enemy in
                    from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy =>
                                    !enemy.IsDead && enemy.IsEnemy && Player.Distance(enemy) < R.Range && R.IsReady())
                    from buff in enemy.Buffs.Where(buff => !buff.Name.Contains("xenzhaointimidate"))
                    select enemy)
                {
                    Render.Circle.DrawCircle(enemy.Position, 90f, Color.Blue, 1);
                }
            }
        }

        public static void Combo()
        {
            var t = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (!t.IsValidTarget())
                return;
            var AARange = Orbwalking.GetRealAutoAttackRange(null) + 65;
            if (t.IsValidTarget(AARange) && Q.IsReady())
            {
                Q.Cast();
            }

            if (t.IsValidTarget(AARange) && W.IsReady())
            {
                W.Cast();
            }

            if (t.IsValidTarget(E.Range) && E.IsReady())
            {
                var eMinRange = Config.Item("EMinRange").GetValue<Slider>().Value;
                if (ObjectManager.Player.Distance(t) >= eMinRange)
                    E.CastOnUnit(t);
            }

            if (Player.Distance(t) <= 450)
            {
                UseItems(t);
            }

            if (PlayerSpells.IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >= t.Health)
                {
                    Player.Spellbook.CastSpell(PlayerSpells.IgniteSlot, t);
                }
            }

            if (R.IsReady() &&
                Config.Item("ComboUserR").GetValue<bool>() &&
                GetHitsR >= Config.Item("ComboUseRS").GetValue<Slider>().Value)
            {
                R.Cast();
            }

            if (Tiamat.IsReady() && Player.Distance(t) <= Tiamat.Range)
                Tiamat.Cast();

            if (Hydra.IsReady() && Player.Distance(t) <= Hydra.Range)
                Tiamat.Cast();
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();

            var allMinions = MinionManager.GetMinions(
                Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.NotAlly);

            if ((useQ || useW))
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, 400);
                foreach (var vMinion in
                    from vMinion in minionsQ where vMinion.IsEnemy select vMinion)
                {
                    if (useQ && Q.IsReady())
                        Q.Cast();
                    if (useW && W.IsReady())
                        W.Cast();
                }
            }

            if (allMinions.Count >= 2)
            {
                if (Tiamat.IsReady())
                    Tiamat.Cast();

                if (Hydra.IsReady())
                    Hydra.Cast();
            }

            if (useE && E.IsReady())
            {

                var locE = E.GetCircularFarmLocation(allMinions);
                if (allMinions.Count == allMinions.Count(m => Player.Distance(m) < E.Range) && locE.MinionsHit >= 2 &&
                    locE.Position.IsValid())
                    E.Cast(locE.Position);
            }

        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            var mob = mobs[0];
            if (useQ && Q.IsReady() && mobs.Count >= 1)
                Q.Cast();

            if (useW && W.IsReady() && mobs.Count >= 1)
                W.Cast();

            if (useE && E.IsReady() && mobs.Count >= 2)
                E.CastOnUnit(mob);

            if (mobs.Count >= 2)
            {
                if (Tiamat.IsReady())
                    Tiamat.Cast();

                if (Hydra.IsReady())
                    Hydra.Cast();
            }

        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero t)
        {
            if (t == null)
                return;

            int[] targeted = new[] { 3153, 3144, 3146, 3184 };
            foreach (
                var itemId in
                    targeted.Where(
                        itemId =>
                            Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null &&
                            t.IsValidTarget(450)))
            {
                Items.UseItem(itemId, t);
            }

            int[] nonTarget = new[] { 3180, 3143, 3131, 3074, 3077, 3142 };
            foreach (
                var itemId in
                    nonTarget.Where(
                        itemId =>
                            Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null &&
                            t.IsValidTarget(450)))
            {
                Items.UseItem(itemId);
            }
        }


        private static void CreateChampionMenu()
        {
            Config = new Menu("xQx | XinZhao", ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            Utils = new Utils();
            AssassinManager = new AssassinManager();

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("EMinRange", "Min. E Range").SetValue(new Slider(300, 200, 500)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseRS", Tab + "Min. Enemy Count:").SetValue(new Slider(2, 5, 1)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));


            Config.AddSubMenu(new Menu("Lane/Jungle Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawERange", "E range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEMinRange", "E min. range").SetValue(new Circle(false, Color.Aqua)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawRRange", "R range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawThrown", "Can be thrown enemy").SetValue(new Circle(false, Color.PowderBlue)));

            Config.SubMenu("Misc").AddItem(new MenuItem("InterruptSpells", "Interrupt spells using R").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("BlockR", "Block R if it won't hit").SetValue(false));

            /*
            new PotionManager();
            */
            Config.AddToMainMenu();
        }

        private static void WelcomeMessage()
        {
            Notifications.AddNotification(ChampionName + " Loaded!", 4000);
        }

    }
}
