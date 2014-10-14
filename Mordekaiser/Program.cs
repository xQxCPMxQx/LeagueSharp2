using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace Mordekaiser
{
    class Program
    {
        public const string ChampionName = "Mordekaiser";
        public static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, W, E, R;

        public static List<Items.Item> ItemList = new List<Items.Item>();
        public static Items.Item Dfg, Hex;

        public static Menu Config;
        public static Menu MenuExtras;

        private static SpellSlot IgniteSlot = Player.GetSpellSlot("SummonerDot");

        private const float WDamageRange = 270f;
        private const float SlaveActvationRange = 2000f;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.BaseSkinName != ChampionName) return;
            if (Player.IsDead) return;

            /* [ Set Items ]*/
            Dfg = new Items.Item(3128, 750);
            ItemList.Add(Dfg);
            
            Hex = new Items.Item(3146, 750);
            ItemList.Add(Hex);

            /* [ Set Spells ]*/
            Q = new Spell(SpellSlot.Q, 300);
            SpellList.Add(Q);

            W = new Spell(SpellSlot.W, 780);
            W.SetTargetted(0.5f, 1500f);
            SpellList.Add(W);

            E = new Spell(SpellSlot.E, 670);
            E.SetSkillshot(0.25f, 10f*2*(float) Math.PI/180, 2000f, false, SkillshotType.SkillshotCone);
            SpellList.Add(E);

            R = new Spell(SpellSlot.R, 850);
            R.SetTargetted(0.5f, 1500f);
            SpellList.Add(R);

            /* [ Set Menu ] */
            Config = new Menu(string.Format("xQx | {0}", ChampionName), ChampionName, true);
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            SimpleTs.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            /* [ Combo ] */
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseQ", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseW", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseE", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("ComboUseR", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Ult On", "DontUlt"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != Player.Team))
            {
                Config.SubMenu("Combo")
                    .SubMenu("DontUlt")
                    .AddItem(new MenuItem("DontUlt" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            }
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo!").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseW", "Use W").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("HarassUseE", "Use E").SetValue(true));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (Toggle)!").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            /* [ Farming ] */
            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(
                new MenuItem("LaneClearActive", "Lane Clear!").SetValue(
                    new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            /* [ JungleFarm ] */
            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "JungleFarm!").SetValue(
                        new KeyBind(Config.Item("LaneClear").GetValue<KeyBind>().Key, KeyBindType.Press)));

            /* [ Extras ] */
            MenuExtras = new Menu("Extras", "Extras");
            MenuExtras.AddItem(new MenuItem("ShieldSelf", "Sheild Self").SetValue(true));
            MenuExtras.AddItem(new MenuItem("ShieldAlly", "Sheild Ally").SetValue(false));
            Config.AddSubMenu(MenuExtras);

            /* [ Drawing ] */
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "W Available Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawWAffectedRange", "W Affected Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "E Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "R Range").SetValue(new Circle(true, Color.Pink)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawAloneEnemy", "Q Alone Target").SetValue(new Circle(false, Color.Pink)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawSlavePos", "Ult Slave Pos.").SetValue(new Circle(false, Color.Pink)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawSlaveRange", "Ult Slave Range").SetValue(new Circle(false, Color.Pink)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("DrawThickness", "Draw Thickness").SetValue(new Slider(1, 5, 5)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQuality", "Draw Quality").SetValue(new Slider(5, 30, 30)));

            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;

            WelcomeMessage();
        }

        private static void Obj_AI_Hero_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (MordekaiserHaveSlave && !sender.Name.Contains("inion"))
            {
               // Game.PrintChat(sender.Name + " : " + args.SData.Name);
                // Ult casting
             
            }
        }
        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
          //  if (RGhost)
        //    if (sender.Name.Contains("orde") || sender.Name.Contains("relia"))
        //        Game.PrintChat(sender.Name);
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
          
        }

        private static bool MordekaiserHaveSlave
        {
            get { return Player.Spellbook.GetSpell(SpellSlot.R).Name == "mordekaisercotgguide"; }
            
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawThickness = Config.Item("DrawThickness" ).GetValue<Slider>().Value;
            var drawQuality = Config.Item("DrawQuality").GetValue<Slider>().Value;

            foreach (var spell in SpellList.Where(spell => spell != Q && spell != W))
            {
                var menuItem = Config.Item("Draw" + spell.Slot).GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color, drawThickness, drawQuality);
            }

            var drawWAffectedRange = Config.Item("DrawWAffectedRange").GetValue<Circle>();
            if (drawWAffectedRange.Active && W.Level > 0 && W.IsReady())
                Utility.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(ObjectManager.Player),
                    drawWAffectedRange.Color, drawThickness, drawQuality);

            if (Config.Item("DrawAloneEnemy").GetValue<Circle>().Active && Q.Level > 0 && Q.IsReady())
            { 
                var vTarget = SimpleTs.GetTarget(Player.AttackRange, SimpleTs.DamageType.Magical);
                if (TargetAlone(vTarget))
                    Utility.DrawCircle(vTarget.Position, 75f, Config.Item("DrawAloneEnemy").GetValue<Circle>().Color,
                        drawThickness, drawQuality);
            }

            if (MordekaiserHaveSlave)
            {
                var drawSlaveRange = Config.Item("DrawSlaveRange").GetValue<Circle>();
                if (drawSlaveRange.Active)
                    Utility.DrawCircle(Player.Position, SlaveActvationRange, drawSlaveRange.Color, drawThickness,
                        drawQuality);

                if (!Config.Item("DrawSlavePos").GetValue<Circle>().Active) return;

                var drawSlavePos = Config.Item("DrawSlavePos").GetValue<Circle>();

                var xMinion =
                    ObjectManager.Get<Obj_AI_Minion>().Where(minion => Player.Distance(minion) < 2000 && minion.IsAlly);
                var xEnemy = ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && enemy.IsDead);

                var xList = from xM in xMinion
                    join xE in xEnemy on new {pEquals1 = xM.Name, pEquals2 = xM.NetworkId}
                        equals new {pEquals1 = xE.Name, pEquals2 = xE.NetworkId}
                    select new {xM.Position, xM.Name, xM.NetworkId};

                    foreach (var xL in xList)
                        Utility.DrawCircle(xL.Position, 75f, drawSlavePos.Color, drawThickness, drawQuality);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (!Orbwalking.CanMove(100)) return;

            if (MordekaiserHaveSlave)
            {
            }
           
            Orbwalker.SetAttacks(true);

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();
            
            if (Config.Item("HarassActive").GetValue<KeyBind>().Active || Config.Item("HarassActiveT").GetValue<KeyBind>().Active)
                Harass();

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                LaneClear();

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                JungleFarm();
        }

        private static void Combo()
        {
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            var rGhostArea = SimpleTs.GetTarget(1500f, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();
            var useR = Config.Item("ComboUseR").GetValue<bool>();

            if (useQ && Q.IsReady() && Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                Q.Cast();

            if (useW && wTarget != null && Player.Distance(wTarget) <= WDamageRange)
                W.CastOnUnit(Player);

            if (useE && eTarget != null)
                E.Cast(eTarget.Position);

            if (MordekaiserHaveSlave && rGhostArea != null)
                R.Cast(rGhostArea.Position);

            if (!MordekaiserHaveSlave && rTarget.Health < Player.GetSpellDamage(rTarget, SpellSlot.R))
                R.CastOnUnit(rTarget);
            
            foreach (var item in ItemList.Where(item => item.IsReady()))
                item.Cast(wTarget);
        }

        private static void Harass()
        {
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            var useQ = Config.Item("ComboUseQ").GetValue<bool>();
            var useW = Config.Item("ComboUseW").GetValue<bool>();
            var useE = Config.Item("ComboUseE").GetValue<bool>();

            if (useQ && Q.IsReady() && Player.Distance(wTarget) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
                Q.Cast();

            if (useW && wTarget != null && Player.Distance(wTarget) <= WDamageRange)
                W.CastOnUnit(Player);

            if (useE && eTarget != null)
                E.Cast(eTarget.Position);
        }

        private static void LaneClear()
        {
            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();
            var useE = Config.Item("LaneClearUseE").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(Player.ServerPosition,
                    Orbwalking.GetRealAutoAttackRange(ObjectManager.Player), MinionTypes.All, MinionTeam.NotAlly);
                foreach (var vMinion in from vMinion in minionsQ
                    let vMinionEDamage = Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    //where vMinion.Health <= vMinionEDamage && vMinion.Health > Player.GetAutoAttackDamage(vMinion)
                    select vMinion) 
                {
                    Q.Cast(vMinion);
                }
            }

            if (useW && W.IsReady())
            {
                var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Orbwalking.GetRealAutoAttackRange(ObjectManager.Player));
                var minionsW = W.GetCircularFarmLocation(rangedMinionsW, Orbwalking.GetRealAutoAttackRange(ObjectManager.Player) * 0.25f);
                if (minionsW.MinionsHit < 2 || !E.InRange(minionsW.Position.To3D()))
                    return;
                W.CastOnUnit(Player);
            }

            if (useE && E.IsReady())
            {
                var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width);
                var minionsE = W.GetCircularFarmLocation(rangedMinionsE, W.Width*0.45f);
                if (minionsE.MinionsHit < 2 || !E.InRange(minionsE.Position.To3D()))
                    return;
                E.Cast(minionsE.Position);
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
            if (useQ && Q.IsReady())
                Q.Cast();

            if (useW && W.IsReady())
                W.CastOnUnit(Player);

            if (useE && E.IsReady())
                E.Cast(mob.Position);
        }
        private static bool TargetAlone(Obj_AI_Hero vTarget)
        {
            var objects =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(x => x.IsMinion && x.IsEnemy && x.IsValid && vTarget.Distance(x) < 240);
            return !objects.Any();
        }

        private static double CalcQDamage
        {
            get
            {
                var qDamageVisitors = new float[] {80, 110, 140, 170, 200};
                var qDamageAlone = new float[] {132, 181, 230, 280, 330};

                var qTarget = SimpleTs.GetTarget(600, SimpleTs.DamageType.Magical);

                var fxQDamage = TargetAlone(qTarget)
                    ? qDamageAlone[Q.Level] + Player.BaseAttackDamage*1.65 + Player.BaseAbilityDamage*.66
                    : qDamageVisitors[Q.Level] + Player.BaseAttackDamage + Player.BaseAbilityDamage*.40;
                return fxQDamage;
            }
        }

        private static float GetComboDamage()
        {
            var fComboDamage = 0d;

            var qTarget = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            var wTarget = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Magical);
            var eTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);
            var rTarget = SimpleTs.GetTarget(E.Range, SimpleTs.DamageType.Magical);

            if (Q.IsReady() && qTarget != null)
                fComboDamage += Player.GetSpellDamage(wTarget, SpellSlot.Q);

            if (W.IsReady() && wTarget != null)
                fComboDamage += Player.GetSpellDamage(wTarget, SpellSlot.W);

            if (E.IsReady() && eTarget != null)
                fComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);

            if (R.IsReady() && rTarget != null)
                fComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);

            if (IgniteSlot != SpellSlot.Unknown && Player.SummonerSpellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                fComboDamage += Player.GetSummonerSpellDamage(wTarget, Damage.SummonerSpell.Ignite);

            if (Items.CanUseItem(3128))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.Dfg);

            if (Items.CanUseItem(3092))
                fComboDamage += Player.GetItemDamage(wTarget, Damage.DamageItems.FrostQueenClaim);

            return (float)fComboDamage;
        }

        private static void WelcomeMessage()
        {
            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded!</font>",
                    ChampionName));
        }
    }
}
