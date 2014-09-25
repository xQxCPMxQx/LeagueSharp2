#region
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Color = System.Drawing.Color;
#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";
        public static readonly Obj_AI_Hero vPlayer = ObjectManager.Player;

        private static readonly List<Texture> Enemies2 = new List<Texture>();

        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static bool leBlancClone;
        private static double soulShackleTimeExperies;

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R, SpellJump;

        public static SpellSlot IgniteSlot;

        public static Items.Item Fqc = new Items.Item(3092, 750); // Frost Queen's Claim; 
        public static Items.Item Dfg = new Items.Item(3188, 750);
        
        //Menu
        public static Menu Config;
        public static Menu MenuExtras;
        public static bool LeBlancClone
        {
            get
            {
                return leBlancClone;
            }
            set
            {
                leBlancClone = value;
            }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += new Program().Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            if (vPlayer.BaseSkinName != ChampionName) return;
            //Create the spells
            Q = new Spell(SpellSlot.Q, 720);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 900);
            R = new Spell(SpellSlot.R, 720);

            Q.SetTargetted(0.5f, 1500f);
            W.SetSkillshot(0.5f, 200f, 1200f, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 100f, 1750f, true, SkillshotType.SkillshotLine);
            R.SetTargetted(0.5f, 1500f);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            IgniteSlot = vPlayer.GetSpellSlot("SummonerDot");

            //Create the menu
            Config = new Menu(ChampionName, ChampionName, true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo menu:
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgniteCombo", "Use Ignite").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseDFGCombo", "Use Deathfire Grasp").SetValue(true));

            Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Combo on", "DontCombo"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != vPlayer.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontCombo")
                    .AddItem(new MenuItem("DontCombo" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }

            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));


            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(
                        new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Harass menu:
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWQHarass", "Use W+Q").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassMode", "Harass Mode: ").SetValue(
                        new StringList(new[] { "Q+W+E", "W+Q+E" })));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(
                        new KeyBind(Config.Item("Farm").GetValue<KeyBind>().Key, KeyBindType.Press)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (toggle)!").SetValue(
                        new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle)));

            //Farming menu:
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseQLaneClear", "Use Q").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseWLaneClear", "Use W").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("UseELaneClear", "Use E").SetValue(false));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));

            Config.SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("LaneClearActive", "Lane Clear!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //JungleFarm menu:
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ")
                .SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            //Misc
            MenuExtras = new Menu("Extras", "Extras");
            Config.AddSubMenu(MenuExtras);
            MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));

            //Drawings menu:
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(true,  Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WObjectPosition", "W Object Position").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WObjectTimeTick", "Show W Tick").SetValue(true));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WQRange", "W+Q Range").SetValue(new Circle(false, Color.GreenYellow)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("EActiveRange", "E Active Range").SetValue(new Circle(false, Color.GreenYellow)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));

            new PotionManager();
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;

            Interrupter.OnPosibleToInterrupt += Interrupter_OnPosibleToInterrupt;

            Drawing.OnDraw += Drawing_OnDraw;
           
            Game.PrintChat(String.Format("<font color='#70DBDB'>xQx </font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'> Loaded!</font>", ChampionName));
        }

        private static Obj_AI_Hero EnemyHaveSoulShackle
        {
            get
            {
                foreach (var hero in from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => vPlayer.Distance(hero) <= 1100) where hero.IsEnemy 
                                     from buff in hero.Buffs where buff.Name.Contains("LeblancSoulShackle") 
                                     select hero)
                {
                    soulShackleTimeExperies = Game.Time + 2;
                    return hero;
                }
                soulShackleTimeExperies = 0;
                return null;
            }
        }
        private static bool DrawEnemySoulShackle
        {
            get
            { return (from hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => vPlayer.Distance(hero) <= 1100) where hero.IsEnemy 
                      from buff in hero.Buffs 
                      select (buff.Name.Contains("LeblancSoulShackle"))).FirstOrDefault(); }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;
            if (E.IsReady())
            {
                E.Cast(unit);
            }
            else if (R.IsReady() && vPlayer.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
            {
                R.Cast(unit);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        ExpireTime = Game.Time + 3
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (!sender.Name.Contains("displacement_blink_indicator")) return;
            
            for (var i = 0; i < ExistingSlide.Count; i++)
            {
                if (ExistingSlide[i].NetworkId == sender.NetworkId)
                {
                    ExistingSlide.RemoveAt(i);
                    return;
                }
            }
        }

        public static bool LeBlancStillJumped
        {
            get
            {
                if (!W.IsReady() || vPlayer.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn") return true;
                SpellJump = W;
                return false;
            }

        }

        private static void UseSpellR(Obj_AI_Hero vTarget)
        {
            var rMode = vPlayer.Spellbook.GetSpell(SpellSlot.R).Name;

            if (rMode != "LeblancChaosOrbM" || !R.IsReady()) return;

            R.CastOnUnit(vTarget);

            switch (rMode)
            {
                case "LeblancChaosOrbM":
                    {
                        R.Range = Q.Range;
                        R.SetTargetted(0.5f, float.MaxValue);
                        R.CastOnUnit(vTarget);
                        break;
                    }
                case "LeblancSlideM":
                    {
                        R.Range = W.Range;
                        R.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.SkillshotCircle);
                        R.Cast(vTarget);
                        break;
                    }
                case "LeblancSoulShackleM":
                    {
                        R.Range = E.Range;
                        R.SetSkillshot(0.5f, 100f, 1000f, true, SkillshotType.SkillshotLine);
                        R.Cast(vTarget);
                        break;
                    }
            }
        }

        private static void UserSummoners(Obj_AI_Hero target)
        {
            var useDfg = Config.Item("UseDFGCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgniteCombo").GetValue<bool>();

            if (Dfg.IsReady() && useDfg)
            {
                Dfg.Cast(target);
            }

            if (Fqc.IsReady())
            {
                Fqc.Cast(target.ServerPosition);
            }
          
            if (useIgnite && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (vPlayer.Distance(target) < 650 && GetComboDamage(target) >= target.Health)
                {
                    vPlayer.SummonerSpellbook.CastSpell(IgniteSlot, target);
                }
            }

        }

        private static void Combo()
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();

            var useDFG = Config.Item("UseDFGCombo").GetValue<bool>();
            var useIgnite = Config.Item("UseIgniteCombo").GetValue<bool>();

            if (Q.IsReady() && R.IsReady() && qTarget != null)
            {
                if (qTarget != null)
                    useR = (Config.Item("DontCombo" + qTarget.BaseSkinName) != null &&
                            Config.Item("DontCombo" + qTarget.BaseSkinName).GetValue<bool>() == false) && useR && useQ;
                {
                        Q.CastOnUnit(qTarget);
                        if (vPlayer.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                            R.CastOnUnit(qTarget);
                }
            }
            else
            {
                if (useW && wTarget != null && W.IsReady() && !LeBlancStillJumped)
                {
                    W.Cast(wTarget);
                }

                if (useE && eTarget != null && E.IsReady())
                {
                    E.Cast(eTarget);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }

                if (useR && qTarget != null && R.IsReady() && 
                    vPlayer.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.Cast(qTarget);
                }

            }

            if (qTarget != null && Dfg.IsReady() && useDFG)
            {
                Dfg.Cast(qTarget);
            }

            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (vPlayer.Distance(qTarget) < 650 && GetComboDamage(qTarget) >= qTarget.Health)
                {
                    vPlayer.SummonerSpellbook.CastSpell(IgniteSlot, qTarget);
                }
            }
        }

        private static void Harass()
        {
            var existsMana = vPlayer.MaxMana / 100 * Config.Item("HarassMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;


            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var wqTarget = SimpleTs.GetTarget(W.Range + Q.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("UseQHarass").GetValue<bool>();
            var useW = Config.Item("UseWHarass").GetValue<bool>();
            var useE = Config.Item("UseEHarass").GetValue<bool>();
            var useWQ = Config.Item("UseWQHarass").GetValue<bool>();

            if (useWQ && wqTarget != null)
            {
                if (Q.IsReady() && W.IsReady() && !LeBlancStillJumped)
                {
                    W.Cast(wqTarget.ServerPosition);
                    Q.CastOnUnit(wqTarget);
                }
            }

            if (useQ && qTarget != null) 
            {
                if (Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                    if (vPlayer.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                        R.CastOnUnit(qTarget);
                }
            }

            if (useW && wTarget != null)
            {
                if (W.IsReady() && !LeBlancStillJumped)
                {
                    W.Cast(wTarget);
                }
            }

            if (useE && eTarget != null)
            {
                if (E.IsReady())
                {
                    E.Cast(eTarget);
                }
            }
        }

        private static float GetComboDamage(Obj_AI_Base vTarget)
        {
            var fComboDamage = 0d;

            if (Q.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.Q);

            if (W.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.W);

            if (E.IsReady())
                fComboDamage += vPlayer.GetSpellDamage(vTarget, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += vPlayer.GetSummonerSpellDamage(vTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128))
                fComboDamage += vPlayer.GetItemDamage(vTarget, Damage.DamageItems.Dfg); 

            if (Items.CanUseItem(3092))
                fComboDamage += vPlayer.GetItemDamage(vTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useIgnite)
        {
            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (useE && eTarget != null && E.IsReady())
                E.Cast(eTarget);

            if (useW && wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
            }

            if (useQ && qTarget != null && Q.IsReady())
                Q.Cast(qTarget);


            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                vPlayer.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (vPlayer.Distance(qTarget) < 650 && GetComboDamage(qTarget) > qTarget.Health)
                {
                    vPlayer.SummonerSpellbook.CastSpell(IgniteSlot, qTarget);
                }
            }

            if (!useR || !R.IsReady()) return;
            
            var rMode = vPlayer.Spellbook.GetSpell(SpellSlot.R).Name;
            switch (rMode)
            {
                case "LeblancChaosOrbM":
                {
                    R = Q;
                    if (qTarget != null)
                        R.Cast(qTarget);
                    break;
                }
                case "LeblancSlideM":
                {
                    R = W;
                    if (wTarget != null)
                        R.CastIfWillHit(wTarget);
                    break;
                }
                case "LeblancSoulShackleM":
                {
                    R = E;
                    if (eTarget != null)
                        R.CastIfWillHit(eTarget);
                    break;
                }
            }
        }

        private static void LaneClear()
        {
            if (!Orbwalking.CanMove(40)) return;

            var existsMana = vPlayer.MaxMana / 100 * Config.Item("LaneClearMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;

            var useQ = Config.Item("UseQLaneClear").GetValue<bool>();
            var useW = Config.Item("UseWLaneClear").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(vPlayer.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (Obj_AI_Base vMinion in 
                    from vMinion in minionsQ let vMinionEDamage = vPlayer.GetSpellDamage(vMinion, SpellSlot.Q)
                        where vMinion.Health <= vMinionEDamage 
                            select vMinion)
                {
                    Q.CastOnUnit(vMinion);
                }
            }

            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 30);
            if (!useW || !W.IsReady()) return;
            var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Width * 0.75f);
            if (minionsW.MinionsHit < 3 || !W.InRange(minionsW.Position.To3D())) return;
            W.Cast(minionsW.Position);

        }

        private static void JungleFarm()
        {
            var existsMana = vPlayer.MaxMana / 100 * Config.Item("JungleFarmMana").GetValue<Slider>().Value;
            if (vPlayer.Mana <= existsMana) return;

            var useQ = Config.Item("UseQJFarm").GetValue<bool>();
            var useW = Config.Item("UseWJFarm").GetValue<bool>();
            var useE = Config.Item("UseEJFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (useQ && Q.IsReady())
                Q.CastOnUnit(mob);

            if (useW && W.IsReady())
                W.Cast(mob);

            if (useE && E.IsReady())
                E.CastOnUnit(mob);
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (vPlayer.IsDead) return;

            Orbwalker.SetAttacks(true);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                    Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                {
                    Harass();
                }

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(vPlayer.Position, spell.Range, menuItem.Color);
            }

            var wObjectPosition = Config.Item("WObjectPosition").GetValue<Circle>();
            var wObjectTimeTick = Config.Item("WObjectTimeTick").GetValue<bool>();

            var eActiveRange = Config.Item("EActiveRange").GetValue<Circle>();

            var wqRange = Config.Item("WQRange").GetValue<Circle>();

            if (wqRange.Active && Q.IsReady() && W.IsReady())
            {
                Utility.DrawCircle(vPlayer.Position, W.Range + Q.Range, eActiveRange.Color);
            }
            
            if (eActiveRange.Active && EnemyHaveSoulShackle != null)
            {
                Utility.DrawCircle(vPlayer.Position, 1100f, eActiveRange.Color);
            }

            foreach (var existingSlide in ExistingSlide)
            {
                if (wObjectPosition.Active)
                    Utility.DrawCircle(existingSlide.Position, 110f, wObjectPosition.Color);

                if (!wObjectTimeTick) continue;
                if (!(existingSlide.ExpireTime > Game.Time)) continue;

                var time = TimeSpan.FromSeconds(existingSlide.ExpireTime - Game.Time);

                var pos = Drawing.WorldToScreen(existingSlide.Position);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65, Color.GreenYellow, display);
            }
        }
    }
}
