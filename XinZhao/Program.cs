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

        public static Obj_AI_Hero Player { get { return ObjectManager.Player; } }

        public static string Tab { get { return "       "; } }

        public static Orbwalking.Orbwalker Orbwalker;

        //public static LxOrbwalker LxOrbwalker;

        public static Utils Utils;

        public static Items Items;

        public static AssassinManager AssassinManager;

        public static Spell Q, W, E, R;

        public static List<Spell> SpellList = new List<Spell>();

        public static Menu Config;

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

            Items = new Items();

            Config = new Menu("xQx | " + ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            //LxOrbwalker = new LxOrbwalker();
            //Config.AddSubMenu(LxOrbwalker.Menu);
            
            Utils = new Utils();
            Sprite.Load();

            AssassinManager = new AssassinManager();
            AssassinManager.Load();
            
            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("EMinRange", "Min. E Range").SetValue(new Slider(300, 200, 500)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseRS", Utils.Tab + "Min. Enemy Count:").SetValue(new Slider(2, 5, 1)));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));


            var mLane = new Menu("Lane Mode", "LaneMode");
            mLane.AddItem(new MenuItem("EnabledFarm", "Enable! (On/Off: Mouse Scroll)").SetValue(true));
            mLane.AddItem(new MenuItem("Lane.UseQ", "Use Q").SetValue(false));
            mLane.AddItem(new MenuItem("Lane.UseW", "Use W").SetValue(false));
            mLane.AddItem(new MenuItem("Lane.UseE", "Use E").SetValue(false));
            mLane.AddItem(new MenuItem("Lane.Mana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            mLane.AddItem(new MenuItem("Lane.Active", "Lane Clear!").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
            Config.AddSubMenu(mLane);

            var mJungle = new Menu("Jungle Mode", "JungleMode");
            mJungle.AddItem(new MenuItem("Jungle.UseQ", "Use Q").SetValue(false));
            mJungle.AddItem(new MenuItem("Jungle.UseW", "Use W").SetValue(false));
            mJungle.AddItem(new MenuItem("Jungle.UseE", "Use E").SetValue(false));
            mJungle.AddItem(new MenuItem("Jungle.Mana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            mJungle.AddItem(new MenuItem("Jungle.Active", "Jungle Clear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.AddSubMenu(mJungle);

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawERange", "E range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEMinRange", "E min. range").SetValue(new Circle(false, Color.Aqua)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawRRange", "R range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawThrown", "Can be thrown enemy").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("EnabledFarmPermashow", "ShowItem Farm Permashow").SetValue(true))
                .ValueChanged +=
                    (s, ar) =>
                    {
                        if (ar.GetNewValue<bool>())
                        {
                            Config.Item("EnabledFarm").Permashow(true, "Enabled Farm");
                        }
                        else
                        {
                            Config.Item("EnabledFarm").Permashow(false);
                        }
                    };
            Config.Item("EnabledFarm").Permashow(Config.Item("EnabledFarmPermashow").GetValue<bool>(), "Enabled Farm");

            Config.SubMenu("Misc").AddItem(new MenuItem("InterruptSpells", "Interrupt spells using R").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("BlockR", "Block R if it won't hit").SetValue(false));

            Config.AddToMainMenu();

            PlayerSpells.Initialize();

            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;

            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;

            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;

            Spellbook.OnCastSpell += Spellbook_OnCastSpell;

            Drawing.OnDraw += Drawing_OnDraw;

            WelcomeMessage();
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (!(args.Target is Obj_AI_Hero) || Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
            {
                return;
            }

            if (Q.IsReady())
            {
                Q.Cast();
            }

            if (W.IsReady())
            {
                W.Cast();
            }
        }

        private static int GetHitsR
        {
            get { { return Player.CountEnemiesInRange(R.Range); } }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
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

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
            {
                return;
            }

            if (unit.IsValidTarget(R.Range) && args.DangerLevel >= Interrupter2.DangerLevel.Medium &&
                !unit.HasBuff("xenzhaointimidate"))
            {
                R.Cast();
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
            {
                return;
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("Lane.Active").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana/100*Config.Item("Lane.Mana").GetValue<Slider>().Value;
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
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, drawERange.Color, 1);
            }

            var drawRRange = Config.Item("DrawRRange").GetValue<Circle>();
            if (drawRRange.Active)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, drawRRange.Color, 1);
            }

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
                foreach (
                    var enemy in
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
            {
                return;
            }

            if (t.IsValidTarget(E.Range) && E.IsReady())
            {
                var eMinRange = Config.Item("EMinRange").GetValue<Slider>().Value;
                if (ObjectManager.Player.Distance(t) >= eMinRange)
                {
                    E.CastOnUnit(t);
                }

                if (E.GetDamage(t) > t.Health)
                {
                    E.CastOnUnit(t);
                }
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
                Config.Item("ComboUseR").GetValue<bool>() &&
                GetHitsR >= Config.Item("ComboUseRS").GetValue<Slider>().Value)
            {
                R.Cast();
            }

            CastItems();
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

        private static void LaneClear()
        {
            if (!Config.Item("EnabledFarm").GetValue<bool>()) return;

            var useQ = Config.Item("Lane.UseQ").GetValue<bool>();
            var useW = Config.Item("Lane.UseW").GetValue<bool>();
            var useE = Config.Item("Lane.UseE").GetValue<bool>();

            var allMinions = MinionManager.GetMinions(
                Player.ServerPosition,
                E.Range,
                MinionTypes.All,
                MinionTeam.NotAlly);

            if ((useQ || useW))
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, 400);
                foreach (var vMinion in
                    from vMinion in minionsQ where vMinion.IsEnemy select vMinion)
                {
                    if (useQ && Q.IsReady()) Q.Cast();
                    if (useW && W.IsReady()) W.Cast();
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

            if (useE && E.IsReady())
            {
                var locE = E.GetCircularFarmLocation(allMinions);
                if (allMinions.Count == allMinions.Count(m => Player.Distance(m) < E.Range) && locE.MinionsHit >= 2
                    && locE.Position.IsValid()) E.Cast(locE.Position);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("Lane.UseQ").GetValue<bool>();
            var useW = Config.Item("Lane.UseW").GetValue<bool>();
            var useE = Config.Item("Lane.UseE").GetValue<bool>();

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

            if (useE && E.IsReady() && mobs.Count >= 1)
                E.CastOnUnit(mob);

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

        private static InventorySlot GetInventorySlot(int ID)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) ID && item.Stacks >= 1) || (item.Id == (ItemId) ID && item.Charges >= 1));
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x20a)
                return;

            Config.Item("EnabledFarm").SetValue(!Config.Item("EnabledFarm").GetValue<bool>());
        }
        
        private static void WelcomeMessage()
        {
            Notifications.AddNotification(ChampionName + " Loaded!", 4000);
        }

    }
}
