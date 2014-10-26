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
    class Program
    {
        public static string ChampionName = "XinZhao";
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, E, W, R;

        private static readonly SpellSlot SmiteSlot = Player.GetSpellSlot("SummonerSmite");
        private static readonly SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        public static Items.Item Tiamat = new Items.Item(3077, 375);
        public static Items.Item Hydra = new Items.Item(3074, 375); 

        public static Menu Config;
        public static Menu MenuTargetSelector;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;

        private static int DelayTick { get; set; }
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName) return;
            if (Player.IsDead) return;

            Q = new Spell(SpellSlot.Q, 0);
            W = new Spell(SpellSlot.W, 0);
            E = new Spell(SpellSlot.E, 600);
            R = new Spell(SpellSlot.R, 480);

            DelayTick = 0;

            CreateChampionMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            WelcomeMessage();
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100)) return;

            if (DelayTick - Environment.TickCount <= 250)
            {
                UseSummoners();
                DelayTick = Environment.TickCount;
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                var assassinRange = MenuTargetSelector.Item("AssassinRange").GetValue<Slider>().Value;
                Obj_AI_Hero vTarget = null;
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>()
                                                           .Where(enemy => enemy.Team != Player.Team &&
                                                                           !enemy.IsDead && enemy.IsVisible &&
                                                                           MenuTargetSelector.Item("Assassin" + enemy.ChampionName) != null &&
                                                                           MenuTargetSelector.Item("Assassin" + enemy.ChampionName).GetValue<bool>())
                                                           .OrderBy(enemy => enemy.Distance(Game.CursorPos))
                        )
                {
                    vTarget = Player.Distance(enemy) < assassinRange ? enemy : null;
                }
                Combo(vTarget);
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    JungleFarm();
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var drawQRange = Config.Item("DrawQRange").GetValue<Circle>();
            if (drawQRange.Active)
                Utility.DrawCircle(Player.Position, E.Range, drawQRange.Color);

            var drawRRange = Config.Item("DrawRRange").GetValue<Circle>();
            if (drawRRange.Active)
                Utility.DrawCircle(Player.Position, R.Range, drawRRange.Color);

            /* [ Draw Smite ] */
            var drawSmite = Config.Item("SmiteRange").GetValue<Circle>();
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active && drawSmite.Active)
                Utility.DrawCircle(Player.Position, 700f, drawSmite.Color);

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
                                        !enemy.IsDead && enemy.IsEnemy && Player.Distance(enemy) < R.Range &&
                                        R.IsReady())
                        from buff in enemy.Buffs.Where(buff => !buff.Name.Contains("xenzhaointimidate"))
                        select enemy) 
                {
                    Utility.DrawCircle(enemy.Position, 90f, Color.White, 1, 5);
                    Utility.DrawCircle(enemy.Position, 95f, drawThrownEnemy.Color, 1, 5);
                }
            }
        }

        public static void Combo(Obj_AI_Hero vTarget)
        {
            if (vTarget == null)
                vTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Physical);

            if (vTarget == null) return;

            if (vTarget.IsValidTarget(E.Range) && Q.IsReady())
                Q.Cast();

            if (vTarget.IsValidTarget(E.Range) && W.IsReady())
                W.Cast();

            if (vTarget.IsValidTarget(E.Range) && E.IsReady())
                E.CastOnUnit(vTarget);

            if (Player.Distance(vTarget) <= 400)
                UseItems(vTarget);

            if (Player.Distance(vTarget) <= E.Range)
                UseItems(vTarget, true);

            if (IgniteSlot != SpellSlot.Unknown &&
                Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite) >= vTarget.Health)
                {
                    Player.SummonerSpellbook.CastSpell(IgniteSlot, vTarget);
                }
            }

            if (Tiamat.IsReady() && Player.Distance(vTarget) <= Tiamat.Range)
                Tiamat.Cast();

            if (Hydra.IsReady() && Player.Distance(vTarget) <= Hydra.Range)
                Tiamat.Cast();
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();


            var allMinions = MinionManager.GetMinions(Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.NotAlly);

            if ((useQ || useW))
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition, 400);
                foreach (var vMinion in
                                       from vMinion in minionsQ
                                       where vMinion.IsEnemy
                                       select vMinion)
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
                if (allMinions.Count == allMinions.Count(m => Player.Distance(m) < E.Range) && locE.MinionsHit >= 2 && locE.Position.IsValid())
                    E.Cast(locE.Position);
            }

        }
        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>();
            var useE = Config.Item("JungleFarmUseE").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

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
            return ObjectManager.Player.InventoryItems.FirstOrDefault(item => (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget, bool useNonTargetedItems = false)
        {
            if (vTarget == null) return;
            foreach (var itemID in from menuItem in MenuTargetedItems.Items
                                   let useItem = MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4))
                                       into itemID
                                       where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null
                                       select itemID)
            {
                Items.UseItem(itemID, vTarget);
            }

            if (!useNonTargetedItems)
                return;

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items
                                   let useItem = MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.ToString().Substring(4, 4))
                                       into itemID
                                       where Items.HasItem(itemID) && Items.CanUseItem(itemID) && GetInventorySlot(itemID) != null
                                       select itemID)
            {
                Items.UseItem(itemID);
            }
        }

        private static void UseSummoners()
        {
            if (SmiteSlot == SpellSlot.Unknown)
                return;

            if (!Config.Item("AutoSmite").GetValue<KeyBind>().Active) return;

            string[] monsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
            var firstOrDefault = Player.SummonerSpellbook.Spells.FirstOrDefault(
                spell => spell.Name.Contains("mite"));
            if (firstOrDefault == null) return;

            var vMonsters = MinionManager.GetMinions(Player.ServerPosition, firstOrDefault.SData.CastRange[0],
                MinionTypes.All, MinionTeam.NotAlly);
            foreach (
                var vMonster in
                    vMonsters.Where(
                        vMonster =>
                            vMonster != null && !vMonster.IsDead && !Player.IsDead && !Player.IsStunned &&
                            SmiteSlot != SpellSlot.Unknown &&
                            Player.SummonerSpellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                        .Where(
                            vMonster =>
                                (vMonster.Health < Player.GetSummonerSpellDamage(vMonster, Damage.SummonerSpell.Smite)) &&
                                (monsterNames.Any(name => vMonster.BaseSkinName.StartsWith(name))))) 
            {
                Player.SummonerSpellbook.CastSpell(SmiteSlot, vMonster);
            }
        }
        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base vTarget, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells) return;

            if (Player.Distance(vTarget) < R.Range)// && !vTarget.HasBuff("XinDamage"))
            {
                R.Cast();
            }
        }

        private static void CreateChampionMenu()
        {
            Config = new Menu("xQx | XinZhao", ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalker"));

            MenuTargetSelector = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(MenuTargetSelector);
            Config.AddSubMenu(MenuTargetSelector);

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!")
                .SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            /* [ Lane Clear ] */
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear!")
                .SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            /* [ Jungling Farm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("AutoSmite", "Auto Smite").SetValue(new KeyBind('N', KeyBindType.Toggle)));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm!")
                .SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawQRange", "Q Range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawRRange", "R Range").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawThrown", "Can be thrown enemy").SetValue(new Circle(false, Color.PowderBlue)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("SmiteRange", "Smite Range").SetValue(new Circle(false, Color.PowderBlue)));

            /* [  Extras -> Use Items ] */
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            /* [  Extras -> Use Items ] */
            var menuUseItems = new Menu("Use Items", "menuUseItems");
            MenuExtras.AddSubMenu(menuUseItems);

            /* [ Extras -> Use Items -> Targeted Items ] */
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));

            /* [ Extras -> Use Items -> AOE Items ] */
            MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));

            new PotionManager();
            new AssassinManager();
            Config.AddToMainMenu();
        }
        private static void WelcomeMessage()
        {
            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx |</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded!</font>",
                    ChampionName));
        }

    }
}
