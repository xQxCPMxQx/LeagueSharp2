#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
#endregion

namespace JaxQx
{
    internal class Program
    {
        public const string ChampionName = "Jax";

        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        private static bool usedSpell = true;

        private static bool shennBuffActive = false;

        public static AssassinManager AssassinManager;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;

        public static Spell W;

        public static Spell E;

        public static Spell R;

        public static string[] Wards =
            {
                "RelicSmallLantern", "RelicLantern", "SightWard", "wrigglelantern",
                "ItemGhostWard", "VisionWard", "BantamTrap", "JackInTheBox",
                "CaitlynYordleTrap", "Bushwhack"
            };

        public static Map map;

        private static SpellSlot igniteSlot;

        public static float WardRange = 600f;

        public static int DelayTick;

        //Menu
        public static Menu Config;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Jax") return;

            Q = new Spell(SpellSlot.Q, 680f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            Q.SetTargetted(0.50f, 75f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            igniteSlot = Player.GetSpellSlot("SummonerDot");

            //Create the menu
            Config = new Menu("xQx | Jax", "Jax", true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            AssassinManager = new AssassinManager();

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ComboUseQMinRange", "Min. Q Range").SetValue(new Slider(250, (int)Q.Range)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow));

            // Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("UseQHarassDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassMode", "Harass Mode: ").SetValue(
                        new StringList(new[] { "Q+W", "Q+E", "Default" })));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)); 

            // Lane Clear
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("UseQLaneClearDontUnderTurret", "Don't Under Turret Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow));

            // Jungling Farm
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJungleFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJungleFarm", "Use W").SetValue(false));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJungleFarm", "Use E").SetValue(false));
            Config.SubMenu("JungleFarm")
                .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)); ;

            // Extra
            var misc = new Menu("Misc", "Misc");
            Config.AddSubMenu(misc);
            misc.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));
            misc.AddItem(new MenuItem("Misc.AutoW", "Auto Hit W if possible").SetValue(true));

            // Drawing
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQRange", "Q range").SetValue(
                        new Circle(true, System.Drawing.Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQMinRange", "Min. Q range").SetValue(
                        new Circle(true, System.Drawing.Color.GreenYellow)));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawWard", "Ward Range").SetValue(
                        new Circle(false, System.Drawing.Color.FromArgb(255, 255, 255, 255))));

            /* [ Damage After Combo ] */
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            Config.AddItem(
                new MenuItem("Ward", "Ward Jump / Flee").SetValue(new KeyBind('A', KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow));
            Config.AddToMainMenu();
            
            map = new Map();
            
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;

            Notifications.AddNotification(String.Format("{0} Loaded", ChampionName), 4000);
        }
        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero && W.IsReady() && Config.Item("Misc.AutoW").GetValue<bool>())
                W.Cast();
        }
        
        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQRange = Config.Item("DrawQRange").GetValue<Circle>();
            if (drawQRange.Active)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, drawQRange.Color, 1);
            }

            var drawWard = Config.Item("DrawWard").GetValue<Circle>();
            if (drawWard.Active)
            {
                Render.Circle.DrawCircle(Player.Position, WardRange, drawWard.Color, 1);
            }

            var drawMinQRange = Config.Item("DrawQMinRange").GetValue<Circle>();
            if (drawMinQRange.Active)
            {
                var minQRange = Config.Item("ComboUseQMinRange").GetValue<Slider>().Value;
                Render.Circle.DrawCircle(Player.Position, minQRange, drawMinQRange.Color, 1);
            }
        }

        private static float GetComboDamage(Obj_AI_Base t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady()) fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (W.IsReady()) fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

            if (E.IsReady()) fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (igniteSlot != SpellSlot.Unknown
                && ObjectManager.Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready) fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128)) fComboDamage += ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.Botrk);

            return (float)fComboDamage;
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs arg)
        {
            if (!sender.IsMe) return;

            if (arg.SData.Name.ToLower().Contains("jaxleapstrike") || arg.SData.Name.ToLower().Contains("jaxempowertwo")
                || arg.SData.Name.ToLower().Contains("jaxcounterstrike"))
            {
                usedSpell = true;
            }
            else
            {
                usedSpell = false;
            }

            if (Wards.ToList().Contains(arg.SData.Name))
            {
                Jumper.testSpellCast = arg.End.To2D();
                Polygon pol;
                if ((pol = map.getInWhichPolygon(arg.End.To2D())) != null)
                {
                    Jumper.testSpellProj = pol.getProjOnPolygon(arg.End.To2D());
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            shennBuffActive = Player.HasBuff("Sheen", true);

            if (DelayTick - Environment.TickCount <= 250)
            {
                DelayTick = Environment.TickCount;
            }

            if (Config.Item("Ward").GetValue<KeyBind>().Active)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                Jumper.wardJump(Game.CursorPos.To2D());
            }

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana) Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana) LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = Player.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
                if (Player.Mana >= existsMana) JungleFarm();
            }
        }

        private static void Combo()
        {
            var t = AssassinManager.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (t == null)
            {
                return;
            }

            if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 95) && (shennBuffActive || usedSpell)) return;

            var minQRange = Config.Item("ComboUseQMinRange").GetValue<Slider>().Value;

            if (Q.IsReady() && Player.Distance(t) >= minQRange && ObjectManager.Player.Distance(t) <= Q.Range)
            {
                if (E.IsReady()) E.Cast();
                Q.Cast(t);
            }

            if (ObjectManager.Player.Distance(t) <= E.Range) UseItems(t);

            if (W.IsReady() && ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(t)) > 0) W.Cast();

            if (E.IsReady() && ObjectManager.Player.CountEnemiesInRange(Orbwalking.GetRealAutoAttackRange(t)) > 0) E.Cast();

            if (igniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
            {
                if (Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health
                    && ObjectManager.Player.Distance(t) <= 500)
                {
                    Player.Spellbook.CastSpell(igniteSlot, t);
                }
            }

            if (R.IsReady())
            {
                if (Player.Distance(t) < Player.AttackRange)
                {
                    if (
                        ObjectManager.Player.CountEnemiesInRange(
                            (int)Orbwalking.GetRealAutoAttackRange(ObjectManager.Player)) >= 2
                        || t.Health > Player.Health)
                    {
                        R.CastOnUnit(Player);
                    }
                }
            }
        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (t == null)
            {
                return;
            }
            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQHarassDontUnderTurret").GetValue<bool>();

            switch (Config.Item("HarassMode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    {
                        if (Q.IsReady() && W.IsReady() && t != null && useQ && useW)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!t.UnderTurret())
                                {
                                    Q.Cast(t);
                                    W.Cast();
                                }
                            }
                            else
                            {
                                Q.Cast(t);
                                W.Cast();
                            }
                        }
                        break;
                    }
                case 1:
                    {
                        if (Q.IsReady() && E.IsReady() && t != null && useQ && useE)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!t.UnderTurret())
                                {
                                    Q.Cast(t);
                                    E.Cast();
                                }
                            }
                            else
                            {
                                Q.Cast(t);
                                E.Cast();
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        if (Q.IsReady() && useQ && t != null && useQ)
                        {
                            if (useQDontUnderTurret)
                            {
                                if (!t.UnderTurret()) Q.Cast(t);
                            }
                            else Q.Cast(t);
                            UseItems(t);
                        }

                        if (W.IsReady() && useW && t != null && t.IsValidTarget(E.Range))
                        {
                            W.Cast();
                        }

                        if (E.IsReady() && useE && t != null && t.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(Player);
                        }
                        break;
                    }
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();
            var useE = Config.Item("UseELaneClear").GetValue<bool>();
            var useQDontUnderTurret = Config.Item("UseQLaneClearDontUnderTurret").GetValue<bool>();

            var vMinions = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var vMinion in vMinions)
            {
                if (useQ && Q.IsReady() && Player.Distance(vMinion) > Orbwalking.GetRealAutoAttackRange(Player))
                {
                    if (useQDontUnderTurret)
                    {
                        if (!vMinion.UnderTurret()) Q.Cast(vMinion);
                    }
                    else Q.Cast(vMinion);
                }

                if (useW && W.IsReady()) W.Cast();

                if (useE && E.IsReady()) E.CastOnUnit(Player);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJungleFarm").GetValue<bool>();
            var useW = Config.Item("UseWJungleFarm").GetValue<bool>();
            var useE = Config.Item("UseEJungleFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                Player.ServerPosition,
                Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            if (Q.IsReady() && useQ && Player.Distance(mobs[0]) > Player.AttackRange) Q.Cast(mobs[0]);

            if (W.IsReady() && useW) W.Cast();

            if (E.IsReady() && useE) E.CastOnUnit(Player);
        }

        private static void Interrupter2_OnInterruptableTarget(
            Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells || !E.IsReady()) return;

            if (Player.Distance(unit) <= E.Range)
            {
                E.Cast();
            }
        }

        private static InventorySlot GetInventorySlot(int ID)
        {
            return
                ObjectManager.Player.InventoryItems.FirstOrDefault(
                    item => (item.Id == (ItemId)ID && item.Stacks >= 1) || (item.Id == (ItemId)ID && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero t)
        {
            if (t == null) return;

            int[] targeted = new[] { 3153, 3144, 3146, 3184 };
            foreach (var itemId in
                targeted.Where(
                    itemId =>
                    Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                    && t.IsValidTarget(450)))
            {
                Items.UseItem(itemId, t);
            }

            int[] nonTarget = new[] { 3180, 3143, 3131, 3074, 3077, 3142 };
            foreach (var itemId in
                nonTarget.Where(
                    itemId =>
                    Items.HasItem(itemId) && Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                    && t.IsValidTarget(450)))
            {
                Items.UseItem(itemId);
            }
        }
    }
}
