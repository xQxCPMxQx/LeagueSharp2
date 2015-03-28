#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Xml.Linq;
using System.IO;
using System.Speech.Synthesis;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
#endregion

namespace Shen
{
    internal class Program
    {
        public const string ChampionName = "Shen";
        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        public static SpeechSynthesizer voice = new SpeechSynthesizer();

        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;

        private static SpellSlot IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        private static SpellSlot SmiteSlot = ObjectManager.Player.GetSpellSlot("SummonerSmite");
        private static SpellSlot FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
        private static SpellSlot TeleportSlot = ObjectManager.Player.GetSpellSlot("SummonerTeleport");

        private static Obj_AI_Hero xUltiableAlly;
        private Timer xTimer = new Timer();
        private double pbUnit;
        private int pbWidth, pbHeight, pbComplete;

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static Menu MenuTargetedItems;
        public static Menu MenuNonTargetedItems;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName) 
                return;
            
            if (ObjectManager.Player.IsDead) 
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

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            // Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                /* [ Don't Use Ult ] */
                Config.SubMenu("Combo").AddSubMenu(new Menu("Don't use Ult on", "DontUlt"));
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsMe))
                    Config.SubMenu("Combo").SubMenu("DontUlt").AddItem(new MenuItem("DontUlt" + ally.BaseSkinName, ally.BaseSkinName).SetValue(false));

                /* [ Ult Priority ] */
                Config.SubMenu("Combo").AddSubMenu(new Menu("Ultimate Priority", "UltPriority"));
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().Where(ally => ally.IsAlly && !ally.IsMe))
                    Config.SubMenu("Combo").SubMenu("UltPriority").AddItem(new MenuItem("UltPriority" + ally.BaseSkinName, ally.BaseSkinName).SetValue(new Slider(1, 1, 5)));

                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseEF", "Use Flash + E").SetValue(new KeyBind("T".ToCharArray()[0],KeyBindType.Press)));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseRE", "Use R | Everytime").SetValue(true));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseRK", "Use R | Confirm with this Key:").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Press)));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQT", "Use Q (Toggle)").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Toggle)));
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass").AddItem(new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            }

            /* [  Lane Clear ] */
            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            /* [  Jungling Farm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(false));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmEnergy", "Min. Energy Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press)));
            }

            // Extras
            //Config.AddSubMenu(new Menu("Extras", "Extras"));
            //Config.SubMenu("Extras").AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            // Extras -> Use Items 
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpellsE", "Interrupt: E").SetValue(true));
            MenuExtras.AddItem(new MenuItem("InterruptSpellsEF", "Interrupt: Flash + E").SetValue(true));

            Menu menuUseItems = new Menu("Use Items", "menuUseItems");
            {
                Config.SubMenu("Extras").AddSubMenu(menuUseItems);
                MenuTargetedItems = new Menu("Targeted Items", "menuTargetItems");
                {
                    menuUseItems.AddSubMenu(MenuTargetedItems);
                    MenuTargetedItems.AddItem(new MenuItem("item3153", "Blade of the Ruined King").SetValue(true));
                    MenuTargetedItems.AddItem(new MenuItem("item3143", "Randuin's Omen").SetValue(true));
                    MenuTargetedItems.AddItem(new MenuItem("item3144", "Bilgewater Cutlass").SetValue(true));
                    MenuTargetedItems.AddItem(new MenuItem("item3146", "Hextech Gunblade").SetValue(true));
                    MenuTargetedItems.AddItem(new MenuItem("item3184", "Entropy ").SetValue(true));
                }
                // Extras -> Use Items -> AOE Items
                MenuNonTargetedItems = new Menu("AOE Items", "menuNonTargetedItems");
                {
                    menuUseItems.AddSubMenu(MenuNonTargetedItems);
                    MenuNonTargetedItems.AddItem(new MenuItem("item3180", "Odyn's Veil").SetValue(true));
                    MenuNonTargetedItems.AddItem(new MenuItem("item3131", "Sword of the Divine").SetValue(true));
                    MenuNonTargetedItems.AddItem(new MenuItem("item3074", "Ravenous Hydra").SetValue(true));
                    MenuNonTargetedItems.AddItem(new MenuItem("item3077", "Tiamat ").SetValue(true));
                    MenuNonTargetedItems.AddItem(new MenuItem("item3142", "Youmuu's Ghostblade").SetValue(true));
                }
            }

            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            {
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Q Range").SetValue(new Circle(true, System.Drawing.Color.Gray)));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "E Range").SetValue(new Circle(false, System.Drawing.Color.Gray)));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEF", "Flash + E Range").SetValue(new Circle(false, System.Drawing.Color.Gray)));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawRswnp", "Show Who Need Help").SetValue(true));
            }

            /* [ Speech ] */
            Config.AddSubMenu(new Menu("Speech", "Speech"));
            {
                var xKey = char.ConvertFromUtf32((int)Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                
                Config.SubMenu("Speech").AddSubMenu(new Menu("Speech Test", "SpeechTest"));
                {
                    Config.SubMenu("Speech").SubMenu("SpeechTest").AddItem(new MenuItem("SpeechText", "Ezreal needs your help press " + xKey + " for ultimate!"));
                    Config.SubMenu("Speech").SubMenu("SpeechTest").AddItem(new MenuItem("SpeechButton", "Test Now").SetValue(false))
                        .ValueChanged += (sender, e) =>
                        {
                            if (e.GetNewValue<bool>())
                            {
                                Speech();
                                Config.Item("SpeechButton").SetValue(false);
                            }
                        };
                }

                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechVolume", "Volume").SetValue(new Slider(50, 10, 100)));//.ValueChanged += (sender, eventArgs) => { Game.PrintChat("AAA"); };
                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechRate", "Rate").SetValue(new Slider(3, -10, 10)));
                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechGender", "Gender").SetValue(new StringList(new[] {"Male", "Female"}, 1)));
                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechRepeatTime", "Repeat").SetValue(new StringList(new[] {"Repeat 1 Time ", "Repeat 2 Times", "Repeat 3 Times", "Repeat Everytime"}, 1)));
                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechRepeatDelay", "Repeat Delay Sec.").SetValue(new Slider(3, 1, 5)));
                Config.SubMenu("Speech").AddItem(new MenuItem("SpeechActive", "Enabled").SetValue(true));
            }

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx | </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));

            Speech();
        }

        public static void Speech()
        {
            if (!Config.Item("SpeechActive").GetValue<bool>())
                return;
            var xSpeechText = Config.Item("SpeechText").DisplayName.ToString();
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

        public static bool InShopRange(Obj_AI_Hero xAlly)
        {
            return (
                from shop in ObjectManager.Get<Obj_Shop>()
                where shop.IsAlly
                select shop).Any<Obj_Shop>(shop => Vector2.Distance(xAlly.Position.To2D(), shop.Position.To2D()) < 1250f);
        }

        public static int CountAlliesInRange(int range, Vector3 point)
        {
            return (
                from units in ObjectManager.Get<Obj_AI_Hero>()
                where units.IsAlly && units.IsVisible && !units.IsDead
                select units).Count<Obj_AI_Hero>(
                    units => Vector2.Distance(point.To2D(), units.Position.To2D()) <= (float) range);
        }

        public static int CountEnemysInRange(int range, Vector3 point)
        {
            return (
                from units in ObjectManager.Get<Obj_AI_Hero>()
                where units.IsValidTarget()
                select units).Count<Obj_AI_Hero>(
                    units => Vector2.Distance(point.To2D(), units.Position.To2D()) <= (float) range);
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

        static void DrawHelplessAllies()
        {
            var drawRswnp = Config.Item("DrawRswnp").GetValue<bool>();
            if (drawRswnp && (R.IsReady() || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).CooldownExpires < 2))
                
            {
                var xHeros = ObjectManager.Get<Obj_AI_Hero>().Where(xQ => !xQ.IsMe && xQ.IsVisible && !xQ.IsDead);
                var xAlly = xHeros.Where(xQ => xQ.IsAlly && !xQ.IsMe && !Config.Item("DontUlt" + xQ.BaseSkinName).GetValue<bool>()).OrderBy(xQ => xQ.Health).FirstOrDefault();
                var xEnemy = xHeros.Where(xQ => xQ.IsEnemy);

                
                if (xAlly.Health < xAlly.Level * 30 && !InShopRange(xAlly))
                {
                    foreach (var x1 in xEnemy.Where(x1 => x1.Distance(xAlly) < 600))
                    {
                        Game.PrintChat(xAlly.ChampionName + " -> " + x1.ChampionName);
                         xUltiableAlly = xAlly;
                        var xKey = char.ConvertFromUtf32((int)Config.Item("ComboUseRK").GetValue<KeyBind>().Key);

                    //Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.80f, System.Drawing.Color.Red, "Q is not ready! You can not Jump!");
                   // Game.PrintChat( xAlly.BaseSkinName + ":-> " + xPriority + " Needs Your Help! Press " + xKey + " for Ultimate!");

                    Drawing.DrawText(Drawing.Width * 0.40f, Drawing.Height * 0.80f, System.Drawing.Color.White, xAlly.BaseSkinName + " Needs Your Help! Press " + xKey + " for Ultimate!");
                    }
                    /*
                    var xPriority = Config.Item("UltPriority" + xAlly.BaseSkinName).GetValue<Slider>().Value;
                    xUltiableAlly = xAlly;
                    var xKey = char.ConvertFromUtf32((int)Config.Item("ComboUseRK").GetValue<KeyBind>().Key);
                    Drawing.DrawText(Drawing.Width * 0.40f, Drawing.Height * 0.80f, System.Drawing.Color.White, xAlly.BaseSkinName + " Needs Your Help! Press " + xKey + " for Ultimate!");
                    */
                    
                }
            }            
        }
        private static void Game_OnUpdate(EventArgs args)
        {
            DrawHelplessAllies();
            if (!Orbwalking.CanMove(100)) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }

            if (Config.Item("ComboUseRK").GetValue<KeyBind>().Active)
            {
                ComboUseRWithKey();
            }

            if (Config.Item("ComboUseEF").GetValue<KeyBind>().Active)
            {
                ComboFlashE();
            }

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
            {
                var existsMana = ObjectManager.Player.MaxMana / 100 * Config.Item("HarassEnergy").GetValue<Slider>().Value;
                if (ObjectManager.Player.Mana >= existsMana)
                    Harass();
            }

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                var existsMana = ObjectManager.Player.MaxMana / 100 * Config.Item("LaneClearEnergy").GetValue<Slider>().Value;
                if (ObjectManager.Player.Mana >= existsMana)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                var existsMana = ObjectManager.Player.MaxMana / 100 * Config.Item("JungleFarmEnergy").GetValue<Slider>().Value;
                if (ObjectManager.Player.Mana >= existsMana)
                    JungleFarm();
            }
        }

        private static void Combo()
        {
            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                    Q.CastOnUnit(t);
            }

            if (W.IsReady() && useW)
            {
                var t = TargetSelector.GetTarget(Q.Range / 2, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                    W.CastOnUnit(ObjectManager.Player);
            }

            if (E.IsReady() && useE)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
                {
                    E.Cast(t.Position);
                    UseItems(t);
                }
            }
        }

        private static void ComboUseRWithKey()
        {
            if (R.IsReady())
                R.CastOnUnit(xUltiableAlly);
            //Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(xUltiableAlly.NetworkId, SpellSlot.R)).Send();

        }

        private static void ComboFlashE()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
			
            var fqTarget = TargetSelector.GetTarget(Q.Range + 430, TargetSelector.DamageType.Physical);

            if (ObjectManager.Player.Distance(fqTarget) > E.Range && E.IsReady() && fqTarget != null &&
                FlashSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(FlashSlot) == SpellState.Ready)
            {
                ObjectManager.Player.Spellbook.CastSpell(FlashSlot, fqTarget.ServerPosition);
                Utility.DelayAction.Add(100, () => E.Cast(fqTarget.Position));
            }
        }
        private static void Harass()
        {
            var useQ = Config.Item("HarassUseQ").GetValue<bool>();

            if (Q.IsReady() && useQ)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget())
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
                W.CastOnUnit(ObjectManager.Player);
            }
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            if (useQ && Q.IsReady() && allMinionsQ.Count > 0)
            {
                if (allMinionsQ[0].Health < ObjectManager.Player.GetSpellDamage(allMinionsQ[0], SpellSlot.Q) + 20)
                    Q.CastOnUnit(allMinionsQ[0]);
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base t, InterruptableSpell args)
        {
            var interruptSpells = Config.Item("InterruptSpells").GetValue<KeyBind>().Active;
            if (!interruptSpells) return;

            if (ObjectManager.Player.Distance(t) < Q.Range)
            {
                E.Cast(t.Position);
            }
        }

        private static InventorySlot GetInventorySlot(int id)
        {
            return ObjectManager.Player.InventoryItems.FirstOrDefault(
                item => (item.Id == (ItemId)id && item.Stacks >= 1) || (item.Id == (ItemId)id && item.Charges >= 1));
        }

        public static void UseItems(Obj_AI_Hero vTarget)
        {
            if (vTarget == null) return;

            foreach (var itemID in from menuItem in MenuTargetedItems.Items
                                   let useItem =
                                        MenuTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                                       into itemId
                                       where Items.HasItem(itemId) &&
                                             Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                                       select itemId)
            {
                Items.UseItem(itemID, vTarget);
            }

            foreach (var itemID in from menuItem in MenuNonTargetedItems.Items
                                   let useItem =
                                        MenuNonTargetedItems.Item(menuItem.Name).GetValue<bool>()
                                   where useItem
                                   select Convert.ToInt16(menuItem.Name.Substring(4, 4))
                                       into itemId
                                       where Items.HasItem(itemId) &&
                                             Items.CanUseItem(itemId) && GetInventorySlot(itemId) != null
                                       select itemId)
            {
                Items.UseItem(itemID);
            }
        }
    }
}
