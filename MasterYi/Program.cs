
#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace MasterYiQx
{
    internal class Program
    {
        public const string ChampionName = "MasterYi";
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> PlayerSpellList = new List<Spell>();
        public static Spell Q;
        public static Spell E;
        public static Spell W;
        public static Spell R;

        private static readonly SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        public static Items.Item Tiamat = new Items.Item(3077, 375);
        public static Items.Item Hydra = new Items.Item(3074, 375);

        public static int DelayTick = 0;
        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;
        public static Menu MenuSupportedSpells;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != "MasterYi")
                return;
            if (Player.IsDead)
                return;

            Q = new Spell(SpellSlot.Q, 600f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 200f);
            R = new Spell(SpellSlot.R);

            Q.SetTargetted(0.50f, 75f);

            PlayerSpellList.Add(Q);
            PlayerSpellList.Add(W);
            PlayerSpellList.Add(E);
            PlayerSpellList.Add(R);


            //Create the menu
            Config = new Menu("xQx | MasterYi", "MasterYi", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            new AssassinManager();
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("UseQComboDontUnderTurret", "Don't Q Under Enemy Turret").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Q Under Enemy Turret").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent:").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(
                        new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Q Under Enemy Turret").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent:").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent:").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("HealSettings", "HealSettings"));
            Config.SubMenu("HealSettings").AddItem(new MenuItem("HealUseW", "Use W").SetValue(true));
            Config.SubMenu("HealSettings")
                .AddItem(new MenuItem("HealPercent", "Min. Health Percent:").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("HealSettings")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent:").SetValue(new Slider(50, 100, 0)));

            // Extras
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);

            // Extras -> Use Items 
            var menuUseItems = new Menu("Use Items", "menuUseItems");
            MenuExtras.AddSubMenu(menuUseItems);

            // Extras -> Use Items -> Targeted Items
            MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
            menuUseItems.AddSubMenu(MenuTargetedItems);
            MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
            MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy").SetValue(true));

            // Extras -> Use Items -> AOE Items
            MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
            menuUseItems.AddSubMenu(MenuNonTargetedItems);
            MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
            MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));


            MenuSupportedSpells = new Menu("Q Dodge Spells", "suppspells");

            foreach (var xEnemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                Obj_AI_Hero enemy = xEnemy;
                foreach (var ccList in SpellList.BuffList.Where(xList => xList.ChampionName == enemy.ChampionName))
                {
                    MenuSupportedSpells.AddItem(new MenuItem(ccList.BuffName, ccList.DisplayName)).SetValue(true);
                }
            }
            Config.AddSubMenu(MenuSupportedSpells);


            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQRange", "Q Range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            new PotionManager();

            Config.AddToMainMenu();


            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            //Obj_AI_Base.OnProcessSpellCast += Game_OnProcessSpellCast;

            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}" +
                    "</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }

        private static void Game_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.Type == GameObjectType.obj_AI_Hero && sender.IsEnemy)
            {
                foreach (var spell in
                    MenuSupportedSpells.Items.SelectMany(
                        t =>
                            SpellList.BuffList.Where(
                                xSpell => xSpell.CanBlockWith.ToList().Contains(CanBlockWith.MasterYiQ))
                                .Where(
                                    spell => t.Name == args.SData.Name && t.Name == spell.BuffName && t.GetValue<bool>()))
                    )
                {
                    switch (spell.SkillType)
                    {
                        case SkillShotType.SkillshotTargeted:
                            if (Q.IsReady())
                            {
                                Game.PrintChat("SkillShotType.SkillshotTargeted");
                                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                                if (t.IsValidTarget())
                                {
                                    Q.CastOnUnit(t);
                                }
                                else
                                {
                                    var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                                        MinionTypes.All, MinionTeam.NotAlly);

                                    var closestMinion = new Obj_AI_Base();
                                    if (allMinions.Any())
                                    {
                                        foreach (var minion in allMinions)
                                        {
                                            if (allMinions.IndexOf(minion) == 0)
                                            {
                                                closestMinion = minion;
                                            }
                                            else if (Player.Distance(minion.Position) <
                                                     Player.Distance(closestMinion.Position))
                                            {
                                                closestMinion = minion;
                                            }
                                        }
                                        if (!closestMinion.IsValidTarget())
                                            return;

                                        Q.CastOnUnit(closestMinion);
                                    }
                                }
                            }
                            break;
                        case SkillShotType.SkillshotCircle:
                            if (ObjectManager.Player.Distance(args.End) <= 300f)
                            {
                                Game.PrintChat("SkillShotType.SkillshotCircle");
                                if (Q.IsReady())
                                {
                                    var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                                    if (t.IsValidTarget())
                                    {
                                        Q.CastOnUnit(t);
                                    }
                                    else
                                    {
                                        var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                                            MinionTypes.All, MinionTeam.NotAlly);

                                        var closestMinion = new Obj_AI_Base();
                                        if (allMinions.Any())
                                        {
                                            foreach (var minion in allMinions)
                                            {
                                                if (allMinions.IndexOf(minion) == 0)
                                                {
                                                    closestMinion = minion;
                                                }
                                                else if (Player.Distance(minion.Position) <
                                                         Player.Distance(closestMinion.Position))
                                                {
                                                    closestMinion = minion;
                                                }
                                            }
                                            if (!closestMinion.IsValidTarget())
                                                return;

                                            Q.CastOnUnit(closestMinion);
                                        }
                                    }
                                }

                            }
                            break;
                        case SkillShotType.SkillshotLine:
                            if (ObjectManager.Player.Distance(args.End) <= 100f)
                            {
                                Game.PrintChat("SkillShotType.SkillshotLine");
                                if (Q.IsReady())
                                {
                                    var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                                    if (t.IsValidTarget())
                                    {
                                        Q.CastOnUnit(t);
                                    }
                                    else
                                    {
                                        var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                                            MinionTypes.All, MinionTeam.NotAlly);

                                        var closestMinion = new Obj_AI_Base();
                                        if (allMinions.Any())
                                        {
                                            foreach (var minion in allMinions)
                                            {
                                                if (allMinions.IndexOf(minion) == 0)
                                                {
                                                    closestMinion = minion;
                                                }
                                                else if (Player.Distance(minion.Position) <
                                                         Player.Distance(closestMinion.Position))
                                                {
                                                    closestMinion = minion;
                                                }
                                            }
                                            if (!closestMinion.IsValidTarget())
                                                return;

                                            Q.CastOnUnit(closestMinion);
                                        }
                                    }
                                }

                            }
                            break;
                        case SkillShotType.SkillshotUnknown:
                            if (ObjectManager.Player.Distance(args.End) <= 500f ||
                                ObjectManager.Player.Distance(sender.Position) <= 500)
                            {
                                Game.PrintChat("SkillShotType.SkillshotUnknown");
                                if (Q.IsReady())
                                {
                                    var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                                    if (t.IsValidTarget())
                                    {
                                        Q.CastOnUnit(t);
                                    }
                                    else
                                    {
                                        var allMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                                            MinionTypes.All, MinionTeam.NotAlly);

                                        var closestMinion = new Obj_AI_Base();
                                        if (allMinions.Any())
                                        {
                                            foreach (var minion in allMinions)
                                            {
                                                if (allMinions.IndexOf(minion) == 0)
                                                {
                                                    closestMinion = minion;
                                                }
                                                else if (Player.Distance(minion.Position) <
                                                         Player.Distance(closestMinion.Position))
                                                {
                                                    closestMinion = minion;
                                                }
                                            }
                                            if (!closestMinion.IsValidTarget())
                                                return;

                                            Q.CastOnUnit(closestMinion);
                                        }
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQRange = Config.Item("DrawQRange").GetValue<Circle>();
            if (drawQRange.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawQRange.Color);
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
                            enemy.Team != Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            Player.Distance(enemy) < assassinRange);

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


        private static void Game_OnUpdate(EventArgs args)
        {
            if (!Orbwalking.CanMove(100))
                return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana/100*Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana/100*Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana/100*Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana)
                    JungleFarm();
            }

            if (Config.Item("HealUseW").GetValue<KeyBind>().Active)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => !enemy.IsAlly))
                {
                    if (Player.Health > enemy.Health && Player.Level >= enemy.Level)
                        return;
                    if (Player.Distance(enemy) > Q.Range + 200 && enemy.IsValid && enemy.IsVisible)
                        return;
                }

                var existsHp = Player.MaxMana/100*Config.Item("HealPercent").GetValue<Slider>().Value;
                if (Player.Health <= existsHp)
                    W.Cast(Player, true);
            }
        }

        private static void Combo()
        {
            var t = GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQComboDontUnderTurret").GetValue<bool>();

            if (Q.IsReady() && useQ && t != null)
            {
                if (useQDontUnderTurret)
                {
                    if (!t.UnderTurret())
                        Q.CastOnUnit(t);
                }
                else
                {
                    Q.CastOnUnit(t);
                }
            }

            if (Player.Distance(t) <= E.Range + 50)
                UseItems(t, true);

            if (t != null)
                UseItems(t);

            if (E.IsReady() && useE && Player.Distance(t) <= E.Range)
                E.Cast();

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (t != null && Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }

            if (R.IsReady() && useR && t != null)
            {
                if (Player.CountEnemiesInRange((int) Q.Range) >= 2)
                {
                    R.CastOnUnit(Player);
                }
            }

            if (Tiamat.IsReady() && Player.Distance(t) <= Tiamat.Range)
                Tiamat.Cast();

            if (Hydra.IsReady() && Player.Distance(t) <= Hydra.Range)
                Tiamat.Cast();

        }

        private static void Harass()
        {
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQLaneClearDontUnderTurret").GetValue<bool>();

            var allMinions = MinionManager.GetMinions(
                Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);


            if (allMinions.Count >= 2)
            {
                if (Tiamat.IsReady())
                    Tiamat.Cast();

                if (Hydra.IsReady())
                    Hydra.Cast();
            }

            if (Q.IsReady() && useQ)
            {
                var closestMinion = new Obj_AI_Base();
                if (allMinions.Any())
                {
                    foreach (var minion in allMinions)
                    {
                        if (allMinions.IndexOf(minion) == 0)
                        {
                            closestMinion = minion;
                        }
                        else if (Player.Distance(minion.Position) < Player.Distance(closestMinion.Position))
                        {
                            closestMinion = minion;
                        }
                    }
                    if (!closestMinion.IsValidTarget())
                        return;

                    Q.Cast(closestMinion);
                }
            }
        }

        private static void JungleFarm()
        {
            if (!Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                return;

            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;

            var mob = mobs[0];
            if (useE && E.IsReady() && Player.Distance(mob) < Orbwalking.GetRealAutoAttackRange(Player))
            {
                E.Cast(mob);
            }

            if (useQ && Q.IsReady())
            {
                Q.Cast(mob);
            }

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
                Player.InventoryItems.FirstOrDefault(
                    item =>
                        (item.Id == (ItemId) ID && item.Stacks >= 1) || (item.Id == (ItemId) ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget, bool useNonTargetedItems = false)
        {
            if (vTarget == null)
                return;
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
    }
}
