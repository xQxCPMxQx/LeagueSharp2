#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Shen
{
    internal class Program
    {
        public const string ChampionName = "Shen";
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        public static SpeechSynthesizer voice = new SpeechSynthesizer();
        public static Enemies Enemies;
        public static Allies ChampionAllies;
        public static UltiStatus UltiStatus;
        public static Utils utils;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;
        private static SpellSlot TeleportSlot = ObjectManager.Player.GetSpellSlot("SummonerTeleport");
        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;

        private static float EManaCost { get { return ObjectManager.Player.GetSpell(SpellSlot.E).ManaCost; } }
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
                return;


            Q = new Spell(SpellSlot.Q, 520f);
            Q.SetTargetted(0.15f, float.MaxValue);
            SpellList.Add(Q);

            W = new Spell(SpellSlot.W);

            E = new Spell(SpellSlot.E, 500f);
            E.SetSkillshot(0.25f, 150f, float.MaxValue, false, SkillshotType.SkillshotLine);
            SpellList.Add(E);

            R = new Spell(SpellSlot.R);

            //Create the menu
            Config = new Menu("xQx | Shen", "Shen", true);
            //Config.AddItem(new MenuItem("Mode", "Play Style (WIP):").SetValue(new StringList(new[] {"Auto", "Fighter", "Protector"}, 1)));
            //Config.AddItem(new MenuItem("PC", "Choose Your PC for FPS Drop (WIP):").SetValue(new StringList(new[] { "Wooden PC", "Normal PC", "Monster!" }, 1)));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);
            new Utils();
            Enemies = new Enemies();
            ChampionAllies = new Allies();
            UltiStatus = new UltiStatus();

            

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboUseEF", "Use Flash + E").SetValue(new KeyBind("T".ToCharArray()[0],
                            KeyBindType.Press)));
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboUseRK", "Use R | Confirm with this Key:").SetValue(
                            new KeyBind("U".ToCharArray()[0], KeyBindType.Press)));
            }

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassUseQT", "Use Q (Toggle)").SetValue(new KeyBind("T".ToCharArray()[0],
                            KeyBindType.Toggle)));
                Config.SubMenu("Harass")
                    .AddItem(new MenuItem("HarassEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0],
                            KeyBindType.Press)));
            }

            /* [  Lane Clear ] */
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear")
                .AddItem(new MenuItem("LaneClearEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            /* [  Jungling Farm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(false));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("SmartShield", "Smart W")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("InterruptSpellsE", "Interrupter E")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("GapCloserE", "GapCloser E")).SetValue(true);
            
            // Extras
            //Config.AddSubMenu(new Menu("Extras", "Extras"));
            //Config.SubMenu("Extras").AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);

            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            {
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawQ", "Q Range").SetValue(new Circle(true, Color.Gray)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("DrawE", "E Range").SetValue(new Circle(false, Color.Gray)));
                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("DrawEF", "Flash + E Range").SetValue(new Circle(false, Color.Gray)));
            }
            Config.AddItem(new MenuItem("FleeActive", "Flee").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
            
            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            PlayerSpells.Initialize();

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;

            Game.PrintChat(
                string.Format(
                    "<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>",
                    ChampionName));

            //Speech();
        }

        public static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Config.Item("SmartShield").GetValue<bool>())
            {
                if (!sender.IsMe && sender.IsEnemy && ObjectManager.Player.Health < 200 && W.IsReady() && args.Target.IsMe) // for minions attack
                {
                    W.Cast();
                }

                else if (!sender.IsMe && sender.IsEnemy && (sender is Obj_AI_Hero || sender is Obj_AI_Turret) && args.Target.IsMe && W.IsReady())
                {
                    W.Cast();
                }
            }

            return;
            
                if (sender.IsEnemy && sender.IsValid && Config.Item("SmartShield").GetValue<bool>() && W.IsReady())
                {
                    if (args.SData.Name.ToLower().Contains("basicattack") && sender.Distance(ObjectManager.Player) < 500)
                    {
                        W.Cast();
                    }
                }
        }

        public static void Speech()
        {
            if (!Config.Item("SpeechActive").GetValue<bool>())
                return;
            var xSpeechText = Config.Item("SpeechText").DisplayName;
            var xSpeechVolume = Config.Item("SpeechVolume").GetValue<Slider>().Value;
            var xSpeechRate = Config.Item("SpeechRate").GetValue<Slider>().Value;
            var xSpeechGender = Config.Item("SpeechGender").GetValue<StringList>().SelectedIndex;
            var xSpeechRepeatTime = Config.Item("SpeechRepeatTime").GetValue<StringList>().SelectedIndex;
            var xSpeechRepeatDelay = Config.Item("SpeechRepeatDelay").GetValue<Slider>().Value;

            try
            {
                switch (xSpeechGender)
                {
                    case 0:
                        voice.SelectVoiceByHints(VoiceGender.Male);
                        break;
                    case 1:
                        voice.SelectVoiceByHints(VoiceGender.Female);
                        break;
                }
                voice.Volume = xSpeechVolume;
                voice.Rate = xSpeechRate;
                voice.SpeakAsync(xSpeechText);
            }
            catch (Exception e)
            {
                Game.PrintChat(e.Message);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (E.IsReady() && gapcloser.Sender.IsValidTarget(E.Range) && Config.Item("GapCloserE").GetValue<bool>())
            {
                E.Cast(gapcloser.Sender.Position);
            }
        }


        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q);

            if (E.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.E);

            if (R.IsReady())
                fComboDamage += ObjectManager.Player.GetSpellDamage(t, SpellSlot.R);

            if (PlayerSpells.IgniteSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready)
                fComboDamage += ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite);

            return (float)fComboDamage;
        }


        public static bool InShopRange(Obj_AI_Hero xAlly)
        {
            return (
                from shop in ObjectManager.Get<Obj_Shop>()
                where shop.IsAlly
                select shop).Any<Obj_Shop>(shop => Vector2.Distance(xAlly.Position.To2D(), shop.Position.To2D()) < 1250f);
        }

        public static int CountAlliesInRange(float range, Vector3 point)
        {
            return (
                from units in ObjectManager.Get<Obj_AI_Hero>()
                where units.IsAlly && units.IsVisible && !units.IsDead
                select units).Count<Obj_AI_Hero>(
                    units => Vector2.Distance(point.To2D(), units.Position.To2D()) <= range);
        }

        public static int CountEnemysInRange(float range, Vector3 point)
        {
            return (
                from units in ObjectManager.Get<Obj_AI_Hero>()
                where units.IsValidTarget()
                select units).Count<Obj_AI_Hero>(
                    units => Vector2.Distance(point.To2D(), units.Position.To2D()) <= range);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawQ = Config.Item("DrawQ").GetValue<Circle>();
            if (drawQ.Active && Q.Level > 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, drawQ.Color);

            var drawE = Config.Item("DrawE").GetValue<Circle>();
            if (drawE.Active && Q.Level > 0)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, drawE.Color);
        }

        private static void DrawHelplessAllies()
        {
            var drawRswnp = Config.Item("DrawRswnp").GetValue<bool>();
            if (drawRswnp && (R.IsReady() || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).CooldownExpires < 2))

            {
                var xHeros = ObjectManager.Get<Obj_AI_Hero>().Where(xQ => !xQ.IsMe && xQ.IsVisible && !xQ.IsDead);
                var xAlly =
                    xHeros.Where(
                        xQ =>
                            xQ.IsAlly && !xQ.IsMe && !Config.Item("DontUlt" + xQ.CharData.BaseSkinName).GetValue<bool>())
                        .OrderBy(xQ => xQ.Health)
                        .FirstOrDefault();
                var xEnemy = xHeros.Where(xQ => xQ.IsEnemy);


                if (xAlly.Health < xAlly.Level*30 && !InShopRange(xAlly))
                {
                    var xKey = char.ConvertFromUtf32((int) Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                    foreach (var x1 in xEnemy.Where(x1 => x1.Distance(xAlly) < 600))
                    {
                        Game.PrintChat(xAlly.ChampionName + " -> " + x1.ChampionName);

                        //Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.80f, System.Drawing.Color.Red, "Q is not ready! You can not Jump!");
                        // Game.PrintChat( xAlly.CharData.BaseSkinName + ":-> " + xPriority + " Needs Your Help! Press " + xKey + " for Ultimate!");

                        Drawing.DrawText(Drawing.Width*0.40f, Drawing.Height*0.80f, Color.White,
                            xAlly.CharData.BaseSkinName + " Needs Your Help! Press " + xKey + " for Ultimate!");
                    }
                    /*
                    var xPriority = Config.Item("UltPriority" + xAlly.CharData.BaseSkinName).GetValue<Slider>().Value;
                    xUltiableAlly = xAlly;
                    var xKey = char.ConvertFromUtf32((int)Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                    Drawing.DrawText(Drawing.Width * 0.40f, Drawing.Height * 0.80f, System.Drawing.Color.White, xAlly.CharData.BaseSkinName + " Needs Your Help! Press " + xKey + " for Ultimate!");
                    */
                }
            }
        }


        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (R.IsReady() && Config.Item("ComboUseRK").GetValue<KeyBind>().Active)
            {
                var t = Utils.ChampAlly;
                if (t != null && R.IsReady())
                {
                    R.CastOnUnit(t);
                }
            }

            if (!R.IsReady() && Config.Item("ComboUseRK").GetValue<KeyBind>().Active)
            {
                var xKey = char.ConvertFromUtf32((int)Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                Config.Item("ComboUseRK").SetValue(new KeyBind(xKey.ToCharArray()[0], KeyBindType.Press, false));
            }

            if (Config.Item("ComboUseEF").GetValue<KeyBind>().Active)
            {
                ComboFlashE();
            }

            if (Config.Item("FleeActive").GetValue<KeyBind>().Active)
            {
                var pos = Game.CursorPos;
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, pos);
                if (E.IsReady())
                    E.Cast(pos);
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                Config.Item("HarassUseQT").GetValue<KeyBind>().Active)
            {
                if (ObjectManager.Player.ManaPercent < Config.Item("HarassEnergy").GetValue<Slider>().Value) ;
                Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = ObjectManager.Player.MaxMana/100*
                                 Config.Item("LaneClearEnergy").GetValue<Slider>().Value;
                if (ObjectManager.Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = ObjectManager.Player.MaxMana/100*
                                 Config.Item("JungleFarmEnergy").GetValue<Slider>().Value;
                if (ObjectManager.Player.Mana >= existsMana)
                    JungleFarm();
            }
        }

        private static void Combo()
        {
            var t = Enemies.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            if (t == null)
            {
                return;
            }
            
            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();
            
            if (E.Level > 0 && !E.IsReady() && ObjectManager.Player.Mana > EManaCost + 25 &&
                t.Health > ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q) &&
                Math.Abs(ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).CooldownExpires) < 0.00001)
                return;

            if (E.IsReady() && useE)
            {
                if (t.IsValidTarget(E.Range))
                {
                    E.Cast(t.Position);
                    UseItems(t);
                }
            }

            else if (Q.IsReady() && useQ)
            {
                if (t.IsValidTarget(Q.Range))
                    Q.CastOnUnit(t);
            }


            if (t.IsValidTarget(550) && PlayerSpells.IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(PlayerSpells.IgniteSlot) == SpellState.Ready &&
                ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) > t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(PlayerSpells.IgniteSlot, t);
            }

        }

        private static void ComboUseRWithKey()
        {
        }

        private static void ComboFlashE()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var fqTarget = Enemies.GetTarget(E.Range + 500, TargetSelector.DamageType.Physical);

            if (ObjectManager.Player.Distance(fqTarget) > E.Range && E.IsReady() && fqTarget != null &&
                PlayerSpells.FlashSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(PlayerSpells.FlashSlot) == SpellState.Ready)
            {
                ObjectManager.Player.Spellbook.CastSpell(PlayerSpells.FlashSlot, fqTarget.ServerPosition);
                Utility.DelayAction.Add(100, () => E.Cast(fqTarget.Position));
            }
        }

        private static void Harass()
        {
            var useQ = Config.Item("HarassUseQ").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var t = Enemies.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(Q.Range))
                    Q.CastOnUnit(t);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];
            if (useQ && Q.IsReady() && mob.Health < ObjectManager.Player.GetSpellDamage(mob, SpellSlot.Q) + 30)
            {
                Q.CastOnUnit(mob);
            }

            if (useW && W.IsReady())
            {
                W.Cast();
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            if (useQ && Q.IsReady() && allMinionsQ.Count > 0)
            {
                if (allMinionsQ[0].Health < ObjectManager.Player.GetSpellDamage(allMinionsQ[0], SpellSlot.Q))
                    Q.CastOnUnit(allMinionsQ[0]);
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero t,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!Config.Item("InterruptSpellsE").GetValue<KeyBind>().Active)
                return;

            if (ObjectManager.Player.Distance(t) < E.Range)
            {
                E.Cast(t.Position);
            }
        }

        private static InventorySlot GetInventorySlot(int id)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(
                item => (item.Id == (ItemId) id && item.Stacks >= 1) || (item.Id == (ItemId) id && item.Charges >= 1));
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
    }
}
