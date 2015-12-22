#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LeagueSharp;
using LeagueSharp.Common;
using System.Threading.Tasks;
using LeagueSharp.Common.Data;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;
using Collision = LeagueSharp.Common.Collision;

#endregion

namespace LeeSin
{
    class Program
    {
        private const string ChampionName = "LeeSin";

        public static Spell Q, W, E, R;
        public static Menu Config;
        public static Orbwalking.Orbwalker Orbwalker;
        
        private static readonly Items.Item ItemYoumuu = new Items.Item(3142, 225f);
        public static Menu MenuCombo { get; private set; }
        public static Menu MenuInsec { get; private set; }
        public static Menu MenuHarass { get; private set; }
        public static Menu MenuLane { get; private set; }
        public static Menu MenuJungle { get; private set; }
        public static Menu MenuFlee { get; private set; }
        public static Menu MenuMisc { get; private set; }
        public static Menu MenuDrawing { get; private set; }
        

        private static Obj_AI_Base insobj;
        private static SpellSlot igniteSlot;
        private static SpellSlot flashSlot;
        public static SpellSlot SmiteDamageSlot;
        private static SpellSlot SmiteHpSlot;
        public static Vector3 WardCastPosition;
        private static Vector3 insdirec;
        private static Vector3 InsecJumpPosition;
        private static Vector3 movepoint;
        private static Vector3 jumppoint;
        private static Vector3 wpos;
        private static Vector3 wallcheck;
        private static Vector3 firstpos;

        private static int canmove=1;
        private static int instypecheck;
        private static float wardTime;
//        private static float jumpTime;
        private static float inscount;
        private static float counttime;

        public static float QCastTime, Q2CastTime;
        public static float WCastTime;
        public static float ECastTime;

        private static float casttime;
        private static bool walljump;
        private static bool checker;

        private static Vector3 vCenterTop;
        private static Vector3 vCenterBottom;
        private static Vector3 vCenterLeft;
        private static Vector3 vCenterRight;

        private static Vector3 vRightTop;
        private static Vector3 vRightBottom;
        private static Vector3 vLeftTop;
        private static Vector3 vLeftBottom;

        private static Vector3[] vTopRightCenter;
        private static Vector3[] vBottomLeftCenter;
        private static Vector3[] vTopLeftCenter;
        private static Vector3[] vBottomRigthCenter;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 1060f);
            Q.SetSkillshot(0.25f, 70f, 1800f, true, SkillshotType.SkillshotLine);

            W = new Spell(SpellSlot.W, 700f);
            E = new Spell(SpellSlot.E, 430f);
            R = new Spell(SpellSlot.R, 375f);

            igniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
            flashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
            SmiteDamageSlot = ObjectManager.Player.GetSpellSlot(SmitetypeDmg());
            SmiteHpSlot = ObjectManager.Player.GetSpellSlot(SmitetypeHp());

            Config = new Menu("Lee is Back!", "Lee Is Back", true).SetFontStyle(FontStyle.Regular,
                SharpDX.Color.GreenYellow);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
            Orbwalker.SetAttack(true);

            new AssassinManager().Initialize();
            new GameItems().Initialize();

            MenuCombo = new Menu("Combo", "Combo");
            {
                MenuCombo.AddItem(new MenuItem("Combo.SmiteQ", "Combo: Smite + Q")).SetValue(new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, R.MenuColor()); ;
                MenuCombo.AddItem(new MenuItem("Combo.WJumpQ", "Combo: W + Q for the Far Enemy")).SetValue(new StringList(new[] {"Off", "On"}, 1)).SetFontStyle(FontStyle.Regular, R.MenuColor()); ;
                MenuCombo.AddItem(new MenuItem("UseSmitecombo", "Use Smite(rush for it)")).SetValue(true);
                MenuCombo.AddItem(new MenuItem("Combo.W.UseNormal", "W: Normal Attack")).SetValue(false).SetFontStyle(FontStyle.Regular, W.MenuColor());
                MenuCombo.AddItem(new MenuItem("Combo.W.JumpForQ", "W: Jump to the Best Position for Q").SetValue(new StringList(new[] { "Off", "Use Ward", "Use Ally Minion/Champion", "Both" }, 2))).SetFontStyle(FontStyle.Regular, W.MenuColor()); ;
                MenuCombo.AddItem(new MenuItem("Combo.W.JumpToEnemyFoot", "W: Jump to the Enemy's Foot").SetValue(new StringList(new []{ "Off", "Use Ward", "Use Ally Minion/Champion", "Both" }, 2))).SetFontStyle(FontStyle.Regular, W.MenuColor()); ;

                Config.AddSubMenu(MenuCombo);
            }

            MenuInsec = new Menu("Insec Settings", "MenuInsec");
            {
                MenuInsec.AddItem(new MenuItem("Insec.Priority.Ally", "Use Ally Champion/Minions")).SetValue(new StringList(new[] {"Don't Use", "1. Priority", "2. Priority", "3. Priority"}, 1));
                MenuInsec.AddItem(new MenuItem("Insec.Priority.Ward", "Use Ward")).SetValue(new StringList(new[] {"Don't Use", "1. Priority", "2. Priority", "3. Priority"}, 2));
                MenuInsec.AddItem(new MenuItem("Insec.Priority.Flash", "Use Flash")).SetValue(new StringList(new[] {"Don't Use", "1. Priority", "2. Priority", "3. Priority"}, 3));

                //                    menuInsec.AddItem(new MenuItem("Insec.UseMinion", "Use Minions")).SetValue(false);
                //                  menuInsec.AddItem(new MenuItem("Insec.UseFlash", "Use Flash if No Wards")).SetValue(true);
                MenuInsec.AddItem(new MenuItem("Insec.Draw", "Draw Insec Last Position")).SetValue(new Circle(true, Color.Aqua));
                Config.AddSubMenu(MenuInsec);
            }

            MenuHarass = new Menu("Harass", "Harass");
            {
                MenuHarass.AddItem(new MenuItem("Harass.Q", "Q:")).SetValue(new StringList(new[] {"Off", "Just IsReady Q", "Complete Q"}, 1));
                MenuHarass.AddItem(new MenuItem("Harass.E", "E:")).SetValue(new StringList(new[] {"Off", "Manual: Just on Harass Mode", "Auto: If Can Hit to Enemy"}));
                MenuHarass.AddItem(new MenuItem("Harass.QW", "Hit Q and Back W:")).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Toggle));
                MenuHarass.AddItem(new MenuItem("Harass.Items", "Use Tiamat/Hydra")).SetValue(true);
                //                    menuHarass.AddItem(new MenuItem("UseQ1Har", "Use Q1 Harass")).SetValue(true);
                //                  menuHarass.AddItem(new MenuItem("UseQ2Har",Harass.Active "Use Q2 Harass")).SetValue(true);
                Config.AddSubMenu(MenuHarass);
            }
            //Farm
            MenuLane = new Menu("Lane", "Lane");
            {
                MenuLane.AddItem(new MenuItem("Lane.UseQ", "Q:")).SetValue(true);
                MenuLane.AddItem(new MenuItem("Lane.UseE", "E:")).SetValue(true);
                MenuLane.AddItem(new MenuItem("Lane.UseItems", "Use Items")).SetValue(true);
                MenuLane.AddItem(new MenuItem("Lane.Active", "Lane Clear!").SetValue(new KeyBind("V".ToCharArray()[0],KeyBindType.Press))).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow);
                MenuLane.AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
                Config.AddSubMenu(MenuLane);
            }

            MenuJungle = new Menu("Jungle", "Jungle");
            {
                MenuJungle.AddItem(new MenuItem("Jungle.UseItems", "Use Items")).SetValue(true);
                MenuJungle.AddItem(new MenuItem("Jungle.UseQ", "Q Jungle")).SetValue(true);
                MenuJungle.AddItem(new MenuItem("Jungle.UseW", "W Jungle")).SetValue(true);
                MenuJungle.AddItem(new MenuItem("Jungle.UseE", "E Jungle")).SetValue(true);
                MenuJungle.AddItem(new MenuItem("PriW", "W>E? (off E>W)")).SetValue(true);
                MenuJungle.AddItem(new MenuItem("Activejungle", "Jungle!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                MenuJungle.AddItem(new MenuItem("Jungle.StealAndRun", "Steal Dragon/Baron And Run!")).SetValue(true);
                Config.AddSubMenu(MenuJungle);
            }

            MenuMisc = new Menu("Misc", "Misc");
            {
                MenuMisc.AddItem(new MenuItem("UseIgnitekill", "Use Ignite KillSteal")).SetValue(true);
                MenuMisc.AddItem(new MenuItem("UseEM", "Use E KillSteal")).SetValue(true);
                MenuMisc.AddItem(new MenuItem("UseRM", "Use R KillSteal")).SetValue(true);
                MenuMisc.AddItem(new MenuItem("wjmax", "ward jump max range?")).SetValue(false);
                Config.AddSubMenu(MenuMisc);
            }

            Config.AddItem(new MenuItem("Insec", "Insec")).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.Aquamarine);

            MenuFlee = new Menu("Flee", "Flee");
            {
                MenuFlee.AddItem(new MenuItem("Flee.UseQ", "Q:")).SetValue(true);
                MenuFlee.AddItem(new MenuItem("Flee.UseW", "W:")).SetValue(true);
                MenuFlee.AddItem(new MenuItem("Flee.Range", "Object Search Range")).SetValue(new Slider(250, 100, 350));
                MenuFlee.AddItem(new MenuItem("Flee.Draw.W", "Draw W Range")).SetValue(new Circle(true, Color.Aqua));
                MenuFlee.AddItem(new MenuItem("Flee.Draw", "Draw Object Search Range (Cursor)")).SetValue(new Circle(true, Color.Aqua));
                MenuFlee.AddItem(new MenuItem("Flee.Active", "Flee Active!")).SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)).SetFontStyle(FontStyle.Regular, SharpDX.Color.IndianRed);
                Config.AddSubMenu(MenuFlee);
            }
            
            //Drawings
            MenuDrawing = new Menu("Drawings", "Drawings");
            {
                MenuDrawing.AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
                MenuDrawing.AddItem(new MenuItem("Draw.W", "Draw W")).SetValue(new StringList(new []{"Off", "On", "Only Insec Mode Active", "Only Flee Mode Active", "Both"}));
                MenuDrawing.AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
                MenuDrawing.AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
                MenuDrawing.AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
                MenuDrawing.AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
                MenuDrawing.AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
                MenuDrawing.AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));
                Config.AddSubMenu(MenuDrawing);
            }
            Config.AddToMainMenu();

            new DamageIndicator();

            Game.OnUpdate += Game_OnUpdate;
            Game.OnUpdate += Game_OnUpdate_Insec;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnWndProc += OnWndProc;
            Orbwalking.BeforeAttack += OrbwalkingBeforeAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            //Drawing.OnDraw += Drawing_OnDraw_xQx;
            Drawing.OnDraw += Drawing_OnDraw_Insec;
            Drawing.OnDraw += Drawing_OnDraw_GetBestPositionForWQCombo;
            Drawing.OnDraw += Drawing_OnDraw_Flee;
            //Drawing.OnDraw += Drawing_OnDraw_Enemy2;
            Drawing.OnDraw += Drawing_OnDraw_WJumpToEnemy;
            Drawing.OnDraw += Drawing_OnDraw;

            DamageIndicator.DamageToUnit = ComboDamage;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            switch (args.Slot)
            {
                case SpellSlot.Q:
                {
                    QCastTime = Environment.TickCount;
                    break;
                }
                case SpellSlot.W:
                {
                    WCastTime = Environment.TickCount;
                    break;
                }
                case SpellSlot.E:
                {
                    ECastTime = Environment.TickCount;
                    break;
                }
            }
        }

        private static void OrbwalkingBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target is Obj_AI_Hero)
            {
                foreach (
                    KeyValuePair
                        <string, GameItems.Tuple<Items.Item, GameItems.EnumItemType, GameItems.EnumItemTargettingType>>
                        item in
                        GameItems.ItemDb.Where(
                            i =>
                                i.Value.ItemType == GameItems.EnumItemType.OnTarget
                                && i.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyHero &&
                                i.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast();
                }
            }
        }

        private static void Combo2()
        {
            var t = AssassinManager.GetTarget(Q.Range + W.Range);
            if (!t.IsValidTarget())
            {
                return;
            }
        }

        private static void Game_OnUpdate_Insec(EventArgs args)
        {
            if (!Config.Item("Insec").GetValue<KeyBind>().Active)
            {
                return;
            }

            var t = AssassinManager.GetTarget(R.Range);
            if (!t.IsValidTarget())
            {
                return;
            }

            if (ObjectManager.Player.Position.Distance(InsecJumpPosition) < t.Position.Distance(InsecJumpPosition))
            {
                R.CastOnUnit(t);
            }

            if (ObjectManager.Player.Position.Distance(InsecJumpPosition) < 200)
            {
                R.CastOnUnit(t);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {


            if (Config.Item("Flee.Active").GetValue<KeyBind>().Active)
            {
                Flee();
            }

            if (!ObjectManager.Player.HasBuff("Recall"))
            {
                if (EStage == ECastStage.IsCasted && Environment.TickCount > ECastTime + 2700)
                {
                    E.Cast();
                }

                if (WStage == WCastStage.IsCasted && Environment.TickCount > WCastTime + 2500)
                {
                    W.Cast();
                }

                if (Config.Item("Insec").GetValue<KeyBind>().Active)
                {
                    Insec();
                }
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (MenuCombo.Item("Combo.SmiteQ").GetValue<StringList>().SelectedIndex == 1)
                {
                    Combos.SmiteQCombo(Q);
                    Combos.ComboQwJumpRq();
                }
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }


            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                JungleClear();
                LaneClear();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }

            return;
            //foreach (var enemy in HeroManager.Enemies)
            //{
            //    if (enemy.HasBlindMonkBuff())
            //    {
            //        Game.PrintChat(enemy.ChampionName + " : Have ");
            //    }
            //    //return;
            //    //foreach (var buff in enemy.Buffs)
            //    //{
            //    //    if (buff.Name.ToLower().Contains("blind"))
            //    //    {
            //    //        Game.PrintChat(enemy.ChampionName + " : " + buff.Name.ToLower());
            //    //    }
            //    //}

            //}







        }

        private static void OnWndProc(WndEventArgs args)
        {
            return;
            if (args.Msg == 515 || args.Msg == 513)
            {
                if (args.Msg == 515)
                {
                    insdirec = Game.CursorPos;
                    instypecheck = 1;
                }

                var boohoo = ObjectManager.Get<Obj_AI_Base>()
                         .OrderBy(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                         .FirstOrDefault(
                             obj =>
                                 obj.IsAlly && !obj.IsMe && !obj.IsMinion && (obj is Obj_AI_Turret || obj is Obj_AI_Hero) &&
                                  Game.CursorPos.Distance(obj.ServerPosition) <= Q.Range * 2);

                if (args.Msg == 513 && boohoo != null)
                {
                    insobj = boohoo;
                    instypecheck = 2;
                }
            }

        }

        private static void OnProcessSpell (Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                casttime = Environment.TickCount;
            }

            if (sender.IsAlly || !sender.Type.Equals(GameObjectType.obj_AI_Hero) ||
                (((Obj_AI_Hero)sender).ChampionName != "MonkeyKing" && ((Obj_AI_Hero)sender).ChampionName != "Akali") ||
                sender.Position.Distance(ObjectManager.Player.ServerPosition) >= 330  ||
                !E.IsReady())
            {
                return;
            }

            if (args.SData.Name == "MonkeyKingDecoy" || args.SData.Name == "AkaliSmokeBomb")
            {
                E.Cast();
            }
        }

        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            if (igniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
            {
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            }

            if (SmiteDamageSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteDamageSlot) == SpellState.Ready)
            {
                damage += 20 + 8 * ObjectManager.Player.Level;
            }

            if (Items.HasItem(3077) && Items.CanUseItem(3077))
            {
                damage += ObjectManager.Player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            }

            if (Items.HasItem(3074) && Items.CanUseItem(3074))
            {
                damage += ObjectManager.Player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            }

            if (Items.HasItem(3153) && Items.CanUseItem(3153))
            {
                damage += ObjectManager.Player.GetItemDamage(enemy, Damage.DamageItems.Botrk);
            }

            if (Items.HasItem(3144) && Items.CanUseItem(3144))
            {
                damage += ObjectManager.Player.GetItemDamage(enemy, Damage.DamageItems.Bilgewater);
            }

            if (QStage == QCastStage.IsReady)
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q)*2;
            }

            if (EStage == ECastStage.IsReady)
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);
            }

            return (float)damage;
        }
        private static bool Passive()
        {
            if (ObjectManager.Player.HasBuff("blindmonkpassive_cosmetic"))
            {
                return true;
            }
            else
                return false;
        }

        private static void Combo()
        {
            var t = AssassinManager.GetTarget(Q.Range);
            if (!t.IsValidTarget())
            {
                return;
            }
            
            if (igniteSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready && ComboDamage(t) > t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(igniteSlot, t);
            }

            if (Config.Item("Combo.W.UseNormal").GetValue<bool>() && t.Distance(ObjectManager.Player.Position) <= Orbwalking.GetRealAutoAttackRange(ObjectManager.Player))
            {
                if (WStage == WCastStage.IsReady || !Passive())
                {
                    CastSelfW();
                }

                if (WStage == WCastStage.IsCasted && (!Passive() || Environment.TickCount > WCastTime + 2500))
                {
                    W.Cast();
                }
            }

            if (Config.Item("UseSmitecombo").GetValue<bool>() && SmiteDamageSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(SmiteDamageSlot) == SpellState.Ready)
            {
                if (ComboDamage(t) > t.Health)
                {
                    ObjectManager.Player.Spellbook.CastSpell(SmiteDamageSlot, t);
                }
            }
            if (t.HasBlindMonkBuff() && !t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65) && QStage == QCastStage.IsCasted)
            {
                Q.Cast();
            }

            if (t.IsValidTarget() && Q.IsReady() && t.Distance(ObjectManager.Player.Position) < 1100)
            {
                CastQ1(t);
            }

            if (t.HasBlindMonkBuff() && (ComboDamage(t) > t.Health || Environment.TickCount > QCastTime + 2500))
                Q.Cast();
            

            CastECombo();

        }
        private static void Harass()
        {
            var t = AssassinManager.GetTarget(Q.Range * 2);
            if (!t.IsValidTarget())
            {
                return;
            }

            //var jumpObject =
            //    ObjectManager.Get<Obj_AI_Base>()
            //        .OrderBy(obj => obj.Distance(firstpos))
            //        .FirstOrDefault(
            //            obj =>
            //                obj.IsAlly && !obj.IsMe &&
            //                !(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
            //                obj.Distance(t.Position) < 550);

            var useQ = Config.Item("Harass.Q").GetValue<StringList>().SelectedIndex;
            if (useQ != 0)
            {
                Game.PrintChat(useQ.ToString());
                switch (useQ)
                {
                    case 1:
                    {
                        CastQ1(t);
                        break;
                    }
                    case 2:
                    {
                        CastQ1(t);
                        if (t.HasBlindMonkBuff())
                        {
                            Q.Cast();
                            Q2CastTime = Environment.TickCount;
                        }
                        break;
                    }
                }
            }


            //var useE = Config.Item("Harass.E").GetValue<StringList>().SelectedIndex;

            //if (Config.Item("UseEHar").GetValue<bool>())
            //{
            //    CastECombo();
            //}

            //if (Config.Item("UseQ1Har").GetValue<bool>())
            //{
            //    CastQ1(t);
            //}

            //if (Config.Item("UseQ2Har").GetValue<bool>() && (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos")) && jumpObject != null && WStage==WCastStage.IsReady)
            //{
            //    Q.Cast();
            //    Q2CastTime = Environment.TickCount;
            //}

            //if (ObjectManager.Player.Distance(t.Position) < 300 && !Q.IsReady()&& Q2CastTime+2500>Environment.TickCount&&Environment.TickCount>Q2CastTime+500)
            //    CastW(jumpObject);
        }

        public static void WardJump(Vector3 jumpPosition, bool useAllyObjects = true, bool useWard = true, bool useFlash = false)
        {
            if (WStage!= WCastStage.IsReady)
            {
                return;
            }

            WardCastPosition = NavMesh.GetCollisionFlags(jumpPosition).HasFlag(CollisionFlags.Wall) ? ObjectManager.Player.GetPath(jumpPosition).Last(): jumpPosition;

            if (useAllyObjects)
            {
                var jumpObject =
                    ObjectManager.Get<Obj_AI_Base>()
                        .OrderBy(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                        .FirstOrDefault(
                            obj =>
                                obj.IsAlly && !obj.IsMe && !(obj is Obj_AI_Turret) &&
                                obj.Position.Distance(jumpPosition) <= MenuFlee.Item("Flee.Range").GetValue<Slider>().Value);

                if (jumpObject != null)
                {
                    W.CastOnUnit(jumpObject);
                    //CastW(jumpObject);
                    return;
                }
            }
            /*
            foreach (
                var objects in
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            o =>
                                o.Distance(ObjectManager.Player.Position) < W.Range && o.Name.ToLower().Contains("ward") &&
                                o is Obj_AI_Minion)
                        .Where(objects => objects.IsAlly))
                        {
                            Game.PrintChat(objects.Name + " : " + objects.Type.ToString());
                        }
            */
            if (useWard && Items.GetWardSlot() != null && Items.GetWardSlot().Stacks != 0)
            {
                PutWard(WardCastPosition);
                return;
            }

            if (useFlash && ObjectManager.Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready)
            {
                ObjectManager.Player.Spellbook.CastSpell(flashSlot, jumpPosition);
                return;
            }
        }

        private static void PutWard(Vector3 position)
        {
            if (WStage != WCastStage.IsReady)
            {
                return;
            }

            if (Environment.TickCount < wardTime + 250)
            {
                return;
            }

            wardTime = Environment.TickCount;
            var ward = Items.GetWardSlot();
            if (ward.IsValidSlot() && ward.Stacks > 0)
            {
                ObjectManager.Player.Spellbook.CastSpell(ward.SpellSlot, position);
            }
        }

        private static void Flee()
        {
            var pos = Game.CursorPos;
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, pos);
            if (pos.IsWall())
            {
                return;
            }

            if (pos.Distance(ObjectManager.Player.Position) < ObjectManager.Player.BoundingRadius*2)
            {
                return;
            }

            WardJump(Game.CursorPos);

            return;
                if (ObjectManager.Player.Distance(Game.CursorPos) >= 700 || walljump)
                {
                    if (Game.CursorPos.Distance(wallcheck) > 50)
                    {
                        walljump = false;
                        checker = false;
                        for (var i = 0; i < 40; i++)
                        {
                            var p = Game.CursorPos.Extend(ObjectManager.Player.Position, 10*i);
                            if (NavMesh.GetCollisionFlags(p).HasFlag(CollisionFlags.Wall))
                            {
                                jumppoint = p;
                                wallcheck = Game.CursorPos;
                                walljump = true;
                                break;
                            }
                        }

                        if (walljump)
                        {
                            foreach (
                                var qPosition in
                                    GetPossibleJumpPositions(jumppoint)
                                        .OrderBy(qPosition => qPosition.Distance(jumppoint)))
                            {
                                if (ObjectManager.Player.Position.Distance(qPosition) <
                                    ObjectManager.Player.Position.Distance(jumppoint))
                                {
                                    movepoint = qPosition;
                                    wpos = movepoint.Distance(jumppoint) > 600
                                        ? movepoint.Extend(jumppoint, 595)
                                        : jumppoint;

                                    break;
                                }
                                checker = true;
                                break;
                            }
                        }
                    }
                    var jumpObj = ObjectManager.Get<Obj_AI_Base>()
                        .OrderBy(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                        .FirstOrDefault(obj => obj.IsAlly && !obj.IsMe && obj.Distance(movepoint) <= 700 &&
                                               (!(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >=
                                                  0) &&
                                                obj.Distance(jumppoint) <= 200));



                    if (walljump == false || movepoint.Distance(Game.CursorPos) > ObjectManager.Player.Distance(Game.CursorPos) + 150)
                    {
                        movepoint = Game.CursorPos;
                        jumppoint = Game.CursorPos;
                    }

                    if (jumpObj == null && Items.GetWardSlot() != null && Items.GetWardSlot().Stacks != 0)
                    {
                        PutWard(wpos);
                    }

                    if (ObjectManager.Player.Position.Distance(jumppoint) <= 700 && jumpObj != null)
                    {
                        CastW(jumpObj);
                        walljump = false;
                    }


                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, movepoint);
                }
                else
                    WardJump(jumppoint);
        }

        private static IEnumerable<Vector3> GetPossibleJumpPositions(Vector3 pos)
        {
            var pointList = new List<Vector3>();

            for (var j = 680; j >= 50; j -= 50)
            {
                var offset = (int)(2 * Math.PI * j / 50);

                for (var i = 0; i <= offset; i++)
                {
                    var angle = i * Math.PI * 2 / offset;
                    var point = new Vector3((float)(pos.X + j * Math.Cos(angle)),
                        (float)(pos.Y - j * Math.Sin(angle)),
                        pos.Z);

                    if (!NavMesh.GetCollisionFlags(point).HasFlag(CollisionFlags.Wall)&&point.Distance(ObjectManager.Player.Position)<pos.Distance(ObjectManager.Player.Position)-400&&
                        point.Distance(pos.Extend(ObjectManager.Player.Position, 600)) <= 250)
                        pointList.Add(point);
                }
            }

            return pointList;
        }
        private static void Insec()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (!R.IsReady())
            {
                return;
            }

            var t = AssassinManager.GetTarget(Q.Range*2);
            if (!t.IsValidTarget())
            {
                return;
            }
            
            Obj_AI_Base insecDirection = ObjectManager.Get<Obj_AI_Base>()
                .OrderBy(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                .FirstOrDefault(
                    obj =>
                        obj.IsAlly && !obj.IsMe && !obj.IsMinion && (obj is Obj_AI_Turret || obj is Obj_AI_Hero) &&
                        obj.Distance(ObjectManager.Player.Position) < Q.Range * 10);

            if (insecDirection == null)
            {
                return;
            }
            
            if (ObjectManager.Player.Position.Distance(InsecJumpPosition) < W.Range)
            {
                WardJump(InsecJumpPosition);
                return;
            }

            if (t.IsValidTarget(Q.Range) && QStage == QCastStage.IsReady)
            {
                Combos.SmiteQCombo(Q);
                Q.Cast(t);
            }

            foreach (var minions in ObjectManager.Get<Obj_AI_Base>().Where(o => o.IsEnemy && !o.IsDead && o.Health > Q.GetDamage(o) + 20 && o.Distance(InsecJumpPosition) < W.Range + 50 && o.IsValidTarget(Q.Range)))
            {
                    Q.Cast(minions);
                Render.Circle.DrawCircle(minions.Position, 105f, Color.OrangeRed);

                if (minions.HasBlindMonkBuff())
                {
                    Q.Cast();
                }
            }

            
            
            if (ObjectManager.Get<Obj_AI_Base>().Where(o => o.HasBlindMonkBuff() && o.Distance(InsecJumpPosition) < W.Range).Any(obj => QStage == QCastStage.IsCasted))
            {
                Q.Cast();
                return;
            }
            
            
            //if (insobj != null && instypecheck==2)
            //     insdirec = insobj.Position;

             if (t.ServerPosition.Distance(insdirec) + 100 < ObjectManager.Player.Position.Distance(insdirec) && R.IsReady())
             {
                 R.CastOnUnit(t);
                 if (LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R)
                 {
                     inscount = Environment.TickCount;
                     canmove = 1;
                 }
             }

             if (canmove==1)
             {
                 ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
             }

             if (!R.IsReady() ||((Items.GetWardSlot() == null || Items.GetWardSlot().Stacks == 0 || WStage != WCastStage.IsReady) && ObjectManager.Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Cooldown))
             {
                 canmove = 1;
                 return;
             }
             
            if ((ObjectManager.Player.ServerPosition.Distance(InsecJumpPosition) > 600 || inscount + 500 > Environment.TickCount) && t.IsValidTarget() && QStage == QCastStage.IsReady)
            {
                var qpred = Q.GetPrediction(t);
                if (qpred.Hitchance >= HitChance.Medium)
                Q.Cast(t);
                if (qpred.Hitchance == HitChance.Collision && Config.Item("Insec.UseMinion").GetValue<bool>())
                {
                    var enemyqtry = ObjectManager.Get<Obj_AI_Base>().Where(enemyq => (enemyq.IsValidTarget()||(enemyq.IsMinion&&enemyq.IsEnemy)) && enemyq.Distance(InsecJumpPosition)<500);
                    foreach (var enemyhit in enemyqtry.OrderBy(enemyhit=>enemyhit.Distance(InsecJumpPosition)).Where(enemyhit => Q.GetPrediction(enemyhit).Hitchance >= HitChance.Medium && enemyhit.Distance(InsecJumpPosition) < 500 && ObjectManager.Player.GetSpellDamage(enemyhit, SpellSlot.Q)<enemyhit.Health))
                    {
                        Q.Cast(enemyhit);
                    }
                }
            }
            if (QStage == QCastStage.IsCasted)
            {
                var enemy = HeroManager.Enemies.FirstOrDefault(unit => unit.IsEnemy && unit.HasBlindMonkBuff());
                if (enemy != null && enemy.Position.Distance(InsecJumpPosition) < 550)
                {
                    Q.Cast();
                    canmove = 0;
                }
            }

            if (ObjectManager.Player.Position.Distance(InsecJumpPosition) < 600)
            {
                if ((Items.GetWardSlot() == null || Items.GetWardSlot().Stacks == 0 || WStage != WCastStage.IsReady) && Config.Item("Insec.UseFlash").GetValue<bool>()&&
                    ObjectManager.Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready&&ObjectManager.Player.Position.Distance(t.Position)<R.Range&& Environment.TickCount>counttime+3000)
                {
                    R.CastOnUnit(t);
                    Utility.DelayAction.Add(Game.Ping + 125, () => ObjectManager.Player.Spellbook.CastSpell(flashSlot, InsecJumpPosition));
                    canmove = 0;
                }
                else
                WardJump(InsecJumpPosition);
                counttime = Environment.TickCount;
                canmove = 0;
            }
        }

        internal enum QCastStage { NotReady, IsReady, IsCasted }
        internal enum WCastStage { NotReady, IsReady, IsCasted }
        internal enum ECastStage { NotReady, IsReady, IsCasted }
        
        private static QCastStage QStage
        {
            get
            {
                if (!Q.IsReady())
                {
                    return QCastStage.NotReady;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Name == "BlindMonkQOne"
                    ? QCastStage.IsReady
                    : QCastStage.IsCasted);

            }
        }
        private static WCastStage  WStage
        {
            get
            {
                if (!W.IsReady())
                {
                    return WCastStage.NotReady;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "blindmonkwtwo"
                    ? WCastStage.IsCasted
                    : WCastStage.IsReady);

            }
        }

        private static ECastStage EStage
        {
            get
            {
                if (!E.IsReady())
                {
                    return ECastStage.NotReady;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Name == "BlindMonkEOne"
                    ? ECastStage.IsReady
                    : ECastStage.IsCasted);
            }
        }

        private static void CastSelfW()
        {
            if (500 >= Environment.TickCount - WCastTime || WStage != WCastStage.IsReady)
            {
                return;
            }

            W.Cast();
            WCastTime=Environment.TickCount;
            
        }
        private static void CastW(Obj_AI_Base obj)
        {
            if (500 >= Environment.TickCount - WCastTime || WStage != WCastStage.IsReady)
            {
                return;
            }

            W.CastOnUnit(obj);
            WCastTime = Environment.TickCount;

        }

        private static void CastECombo()
        {
            if (!E.IsReady())
            {
                return;
            }

            var enemy = HeroManager.Enemies.Find(e => e.IsValidTarget(E.Range) && !e.IsZombie);
            if (enemy != null)
            {
                CastE1();
                return;
            }

            if (EStage == ECastStage.IsCasted && ((Environment.TickCount > casttime + 200 && !Passive()) || Environment.TickCount > ECastTime + 2700))
            {
                E.Cast();
            }
        }

        private static void CastE1()
        {
            if (500 >= Environment.TickCount - ECastTime || EStage != ECastStage.IsReady)
            {
                return;
            }

            E.Cast();
            ECastTime = Environment.TickCount;
        }

        private static void CastQ1(Obj_AI_Base t)
        {
            if (QStage != QCastStage.IsReady) return;
            Q.Cast(t);
            return;
            var qpred = Q.GetPrediction(t);
            if (qpred.Hitchance >= HitChance.Medium && qpred.CastPosition.Distance(ObjectManager.Player.ServerPosition) < 1100)
            {
                Q.Cast(t);
                firstpos = ObjectManager.Player.Position;
                QCastTime = Environment.TickCount;
            }
        }

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string SmitetypeDmg()
        {
            if (SmiteBlue.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(a => Items.HasItem(a)))
            {
                return "s5_summonersmiteduel";

            }
            return "summonersmite";
        }
        private static string SmitetypeHp()
        {
            if (SmitePurple.Any(a => Items.HasItem(a)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        private static void CastItems()
        {
            var t = AssassinManager.GetTarget(E.Range, TargetSelector.DamageType.Physical);
            if (!t.IsValidTarget()) return;

            foreach (var item in
                GameItems.ItemDb.Where(
                    item =>
                        item.Value.ItemType == GameItems.EnumItemType.AoE
                        && item.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyObjects)
                    .Where(item => t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()))
            {
                item.Value.Item.Cast();
            }

            foreach (var item in
                GameItems.ItemDb.Where(
                    item =>
                        item.Value.ItemType == GameItems.EnumItemType.Targeted
                        && item.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyHero)
                    .Where(item => t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()))
            {
                item.Value.Item.Cast(t);
            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);

            var useQl = Config.Item("Lane.UseQ").GetValue<bool>();
            var useEl = Config.Item("Lane.UseE").GetValue<bool>();
            if (allMinionsQ.Count == 0)
                return;
            if (EStage == ECastStage.IsCasted && ((Environment.TickCount > casttime + 200 && !Passive()) || Environment.TickCount > ECastTime + 2700))
                E.Cast();
            if (QStage == QCastStage.IsCasted && (Environment.TickCount > QCastTime + 2700 || Environment.TickCount > casttime + 200 && !Passive()))
                Q.Cast();

            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) &&useQl&&
                    minion.Health < ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)*0.70)
                    Q.Cast(minion);
                else if (Orbwalking.InAutoAttackRange(minion) && useQl&&
                    minion.Health > ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) * 2)
                    CastQ1(minion);
            }
             
            

            if (E.IsReady() && useEl)
            {
                if (allMinionsE.Count > 2)
                {
                    CastE1();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.90 * ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E))
                            CastE1();
            }

            if (Config.Item("Lane.UseItems").GetValue<bool>())
            {
                var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range,MinionTypes.All,MinionTeam.Enemy,MinionOrderTypes.MaxHealth);

                if (allMinions.Count <= 1) return;

                foreach (var item in from item in GameItems.ItemDb
                    where
                        item.Value.ItemType == GameItems.EnumItemType.AoE
                        && item.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyObjects
                    let iMinions = allMinions
                    where
                        item.Value.Item.IsReady()
                        && iMinions[0].Distance(ObjectManager.Player.Position) < item.Value.Item.Range
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }
        }

        private static void LastHit()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            var useQ = Config.Item("UseQLH").GetValue<bool>();
            foreach (var minion in allMinionsQ)
            {
                if (QStage == QCastStage.IsReady && useQ &&ObjectManager.Player.Distance(minion.ServerPosition) < Q.Range &&
                    minion.Health < 0.90 * ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    CastQ1(minion);
                }
            }
        }
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            var useQ = Config.Item("Jungle.UseQ").GetValue<bool>();
            var useW = Config.Item("Jungle.UseW").GetValue<bool>();
            var useE = Config.Item("Jungle.UseE").GetValue<bool>();
            
            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (QStage == QCastStage.IsCasted && (mob.Health < Q.GetDamage(mob) && ((mob.HasBuff("BlindMonkQOne") || mob.HasBuff("blindmonkqonechaos"))) ||
                     Environment.TickCount > QCastTime + 2700 || ((Environment.TickCount > casttime + 200 && !Passive()))))
                {
                    Q.Cast();
                }

                if (WStage == WCastStage.IsCasted && ((Environment.TickCount > casttime + 200 && !Passive()) || Environment.TickCount > WCastTime + 2700))
                    W.Cast();
                if (EStage == ECastStage.IsCasted && ((Environment.TickCount > casttime + 200 && !Passive()) || Environment.TickCount > ECastTime + 2700))
                    E.Cast();
                if (!Passive() && useQ && Q.IsReady() && Environment.TickCount > casttime + 200 || mob.Health < Q.GetDamage(mob)*2)
                    CastQ1(mob);
                else if (!Passive() && Config.Item("PriW").GetValue<bool>() && useW && W.IsReady()&& Environment.TickCount>casttime+200)
                    CastSelfW();
                else if (!Passive() && useE && E.IsReady() && mob.Distance(ObjectManager.Player.Position) < E.Range && Environment.TickCount > casttime + 200 || mob.Health < E.GetDamage(mob))
                    CastE1();
            

            if (Config.Item("Jungle.UseItems").GetValue<bool>())
            {
                foreach (var item in from item in GameItems.ItemDb
                    where
                        item.Value.ItemType == GameItems.EnumItemType.AoE
                        && item.Value.TargetingType == GameItems.EnumItemTargettingType.EnemyObjects
                    let iMinions = mobs
                    where item.Value.Item.IsReady() && iMinions[0].IsValidTarget(item.Value.Item.Range)
                    select item)
                {
                    item.Value.Item.Cast();
                }
            }
            }
        }
        private static void KillSteal()
        {
            var enemyVisible =
                        ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget() && ObjectManager.Player.Distance(enemy.ServerPosition) <= 600).FirstOrDefault();

            {
                if (ObjectManager.Player.GetSummonerSpellDamage(enemyVisible, Damage.SummonerSpell.Ignite) > enemyVisible.Health &&igniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready)
                {
                    ObjectManager.Player.Spellbook.CastSpell(igniteSlot, enemyVisible);
                }
            }
            if (R.IsReady() && Config.Item("UseRM").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                if (ObjectManager.Player.GetSpellDamage(t, SpellSlot.R) > t.Health && ObjectManager.Player.Distance(t.ServerPosition) <= R.Range)
                    R.CastOnUnit(t);
            }


            if (E.IsReady() && Config.Item("UseEM").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (E.GetDamage(t) > t.Health && ObjectManager.Player.Distance(t.ServerPosition) <= E.Range )
                {
                    E.Cast();
                }
            }
        }

        private static IEnumerable<Obj_AI_Base> QGetCollisionMinions(Obj_AI_Base source, Vector3 targetposition)
        {
            var input = new PredictionInput { Unit = source, Radius = Q.Width};

            input.CollisionObjects[0] = CollisionableObjects.Minions;
            input.CollisionObjects[1] = CollisionableObjects.YasuoWall;

            return Collision.GetCollision(new List<Vector3> { targetposition }, input).OrderBy(obj => obj.Distance(source, false)).ToList();
        }

        public static Vector3 CenterOfVectors(Vector3[] vectors)
        {
            var sum = Vector3.Zero;
            if (vectors == null || vectors.Length == 0)
                return sum;

            sum = vectors.Aggregate(sum, (current, vec) => current + vec);
            return sum / vectors.Length;
        }

        private static bool CollisionObjects(Vector3 from, Vector3 to)
        {
            return Combos.QGetCollisionMinions(from, to, Q.Width, Q.Range, new CollisionableObjects[(int)CollisionableObjects.Minions]).Any();
         
        }

        private static void Drawing_OnDraw_WJumpToEnemy(EventArgs args)
        {
            if (Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.Combo)
                return;
            
            var t = AssassinManager.GetTarget(W.Range + 100);

            if (!t.IsValidTarget())
                return;

            if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
            {
                return;
            }

            if (!CollisionObjects(ObjectManager.Player.Position, t.Position) && QStage == QCastStage.IsReady)
            {
                return;
            }

            if (QStage == QCastStage.NotReady && WStage == WCastStage.IsReady && (Q.Cooldown < 4 || E.Cooldown < 4))
            {
                WardJump(t.Position);
            }
        }

        private static void Drawing_OnDraw_Enemy2(EventArgs args)
        {
            Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range + W.Range, Color.Black);
            Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.White);

            var t = AssassinManager.GetTarget(Q.Range + W.Range);

            if (!t.IsValidTarget() || t.IsValidTarget(Q.Range))
            {
                return;
            }

            if (t.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
            {
                return;
            }

            var xPos = t.ServerPosition.Extend(ObjectManager.Player.Position, ObjectManager.Player.Distance(t.Position) -  W.Range);

            if (xPos.Distance(ObjectManager.Player.Position) > ObjectManager.Player.Distance(t.Position))
            {
                return;
            }

            Render.Circle.DrawCircle(xPos, 105f, Color.Red);
            
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                {
                    if (QStage == QCastStage.IsReady && WStage == WCastStage.IsReady)
                    {
                        WardJump(xPos);
                    }
                }
        }

        private static void Drawing_OnDraw_Insec(EventArgs args)
        {
            if (!R.IsReady())
            {
                return;
            }

            Obj_AI_Base insecDirection = ObjectManager.Get<Obj_AI_Base>()
                .OrderBy(obj => obj.Distance(ObjectManager.Player.ServerPosition))
                .FirstOrDefault(
                    obj =>
                        obj.IsAlly && !obj.IsMe && !obj.IsMinion && (obj is Obj_AI_Turret || obj is Obj_AI_Hero) &&
                        obj.Distance(ObjectManager.Player.Position) < Q.Range * 10);

            if (insecDirection != null)
            {
                var t = AssassinManager.GetTarget(Q.Range * 2);
                InsecJumpPosition = t.ServerPosition.Extend(insecDirection.Position, -150);

                Render.Circle.DrawCircle(InsecJumpPosition, 150f, Color.Blue);
                Render.Circle.DrawCircle(insecDirection.Position, 150f, Color.Red);

                var tt = t.ServerPosition.Extend(insecDirection.Position, +300);

                var startpos = t.Position;
                var endpos = tt;
                var endpos1 = tt +
                              (startpos - endpos).To2D().Normalized().Rotated(25 * (float)Math.PI / 180).To3D() *
                              t.BoundingRadius * 2;
                var endpos2 = tt +
                              (startpos - endpos).To2D().Normalized().Rotated(-25 * (float)Math.PI / 180).To3D() *
                              t.BoundingRadius * 2;

                var width = 2;

                var x = new Geometry.Polygon.Line(startpos, endpos);
                x.Draw(Color.Blue, width);
                var y = new Geometry.Polygon.Line(endpos, endpos1);
                y.Draw(Color.Blue, width);
                var z = new Geometry.Polygon.Line(endpos, endpos2);
                z.Draw(Color.Blue, width);
            }

        }

        private static void Drawing_OnDraw_Flee(EventArgs args)
        {
            if (MenuFlee.Item("Flee.Active").GetValue<KeyBind>().Active)
            {
                if (MenuFlee.Item("Flee.Draw").GetValue<Circle>().Active)
                {
                    var fleeDraw = MenuFlee.Item("Flee.Draw").GetValue<Circle>();
                    Render.Circle.DrawCircle(Game.CursorPos, MenuFlee.Item("Flee.Range").GetValue<Slider>().Value,
                        fleeDraw.Color);

                    var fleeDrawW = MenuFlee.Item("Flee.Draw.W").GetValue<Circle>();
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, fleeDrawW.Color);
                }
            }
        }

        private static void Drawing_OnDraw_GetBestPositionForWQCombo(EventArgs args)
        {
            var t = AssassinManager.GetTarget(Q.Range + W.Range);
            if (!t.IsValidTarget())
            {
                return;
            }

            if (!CollisionObjects(ObjectManager.Player.ServerPosition, t.ServerPosition))
            {
                return;
            }

            var x = t.Position.X;
            var y = t.Position.Y;

            var length = W.Range;

            var ls32 = (int) (length*Math.Sqrt(3)/2);
            var half = (int) (length/2);

            vCenterTop = new Vector3(x, y + ls32, 0);
            vCenterBottom = new Vector3(x, y - ls32, 0);
            vCenterLeft = new Vector3(x - length, y, 0);
            vCenterRight = new Vector3(x + length, y, 0);

            vRightTop = new Vector3(x + half, y + ls32, 0);
            vRightBottom = new Vector3(x + half, y - ls32, 0);
            vLeftTop = new Vector3(x - half, y + ls32, 0);
            vLeftBottom = new Vector3(x - half, y - ls32, 0);

            vTopRightCenter = new[] {vCenterRight, vRightTop};
            vBottomLeftCenter = new[] {vCenterLeft, vLeftBottom};
            vTopLeftCenter = new[] {vLeftTop, vCenterLeft};
            vBottomRigthCenter = new[] {vCenterRight, vRightBottom};

            Render.Circle.DrawCircle(vCenterRight, Q.Width, Color.Red, 5); // Center - Right
            Render.Circle.DrawCircle(vCenterLeft, Q.Width, Color.Yellow, 5); // Center - Left
            Render.Circle.DrawCircle(vCenterTop, Q.Width, Color.DarkBlue, 5); // Center - Top
            Render.Circle.DrawCircle(vCenterBottom, Q.Width, Color.DarkBlue, 5); // Center - Bottom

            Render.Circle.DrawCircle(vRightTop, Q.Width, Color.Blue, 5); // Right - Top
            Render.Circle.DrawCircle(vRightBottom, Q.Width, Color.Blue, 5); // Right - Bottom
            Render.Circle.DrawCircle(vLeftTop, Q.Width, Color.Aqua, 5); // Left - Top
            Render.Circle.DrawCircle(vLeftBottom, Q.Width, Color.Aqua, 5); // Left - Bottom

            Render.Circle.DrawCircle(CenterOfVectors(vTopRightCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vBottomLeftCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vTopLeftCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vBottomRigthCenter), Q.Width, Color.Black, 5);

            List<Vector3> xList = new List<Vector3>
            {
                vCenterTop,
                vCenterBottom,
                vCenterLeft,
                vCenterRight,
                vRightTop,
                vRightBottom,
                vLeftTop,
                vLeftBottom,
                CenterOfVectors(vTopRightCenter),
                CenterOfVectors(vBottomLeftCenter),
                CenterOfVectors(vTopLeftCenter),
                CenterOfVectors(vBottomRigthCenter)
            };

            List<Vector3> posList = new List<Vector3>();

            foreach (var l in xList)
            {
                for (var i = 1; i < t.Position.Distance(vCenterTop)/Q.Width; i++)
                {
                    var pos = t.ServerPosition.To2D().Extend(l.To2D(), i*Q.Width).To3D();
                    if (pos.Distance(ObjectManager.Player.Position) <= W.Range)
                    {
                        IEnumerable<Obj_AI_Base> xM = Combos.QGetCollisionMinions(t.ServerPosition, pos, Q.Width, Q.Range, new CollisionableObjects[(int) CollisionableObjects.Minions]);

                        IEnumerable<Obj_AI_Base> objAiBases = xM as Obj_AI_Base[] ?? xM.ToArray();
                        if (xM != null && !objAiBases.Any())
                        {
                            posList.Add(pos);
                        }
                    }
                }

            }

            var xPos = posList.OrderBy(o => o.Distance(t.Position)).FirstOrDefault();

            Render.Circle.DrawCircle(xPos, Q.Width, xPos.IsWall() ? Color.Red : Color.White, xPos.IsWall() ? 5 : 2);

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                if (Q.IsReady() && WStage == WCastStage.IsReady && xPos != Vector3.Zero)
                {
                    WardJump(xPos);
                }

            }
        }

        private static void Drawing_OnDraw_xQx(EventArgs args)
        {
            var x = ObjectManager.Player.Position.X;
            var y = ObjectManager.Player.Position.Y;

            var length = W.Range;

            var ls32 = (int)(length * Math.Sqrt(3) / 2);
            var half = (int)(length / 2);

            vCenterTop = new Vector3(x, y + ls32, 0);
            vCenterBottom = new Vector3(x, y - ls32, 0);
            vCenterLeft = new Vector3(x - length, y, 0);
            vCenterRight = new Vector3(x + length, y, 0);

            vRightTop = new Vector3(x + half, y + ls32, 0);
            vRightBottom = new Vector3(x + half, y - ls32, 0);
            vLeftTop = new Vector3(x - half, y + ls32, 0);
            vLeftBottom = new Vector3(x - half, y - ls32, 0);

            vTopRightCenter = new[] { vCenterRight, vRightTop };
            vBottomLeftCenter = new[] { vCenterLeft, vLeftBottom };
            vTopLeftCenter = new[] { vLeftTop, vCenterLeft };
            vBottomRigthCenter = new[] { vCenterRight, vRightBottom };

            Render.Circle.DrawCircle(vCenterRight, Q.Width, Color.Red, 5); // Center - Right
            Render.Circle.DrawCircle(vCenterLeft, Q.Width, Color.Yellow, 5); // Center - Left
            Render.Circle.DrawCircle(vCenterTop, Q.Width, Color.DarkBlue, 5); // Center - Top
            Render.Circle.DrawCircle(vCenterBottom, Q.Width, Color.DarkBlue, 5); // Center - Bottom

            Render.Circle.DrawCircle(vRightTop, Q.Width, Color.Blue, 5); // Right - Top
            Render.Circle.DrawCircle(vRightBottom, Q.Width, Color.Blue, 5); // Right - Bottom
            Render.Circle.DrawCircle(vLeftTop, Q.Width, Color.Aqua, 5); // Left - Top
            Render.Circle.DrawCircle(vLeftBottom, Q.Width, Color.Aqua, 5); // Left - Bottom
            
            Render.Circle.DrawCircle(CenterOfVectors(vTopRightCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vBottomLeftCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vTopLeftCenter), Q.Width, Color.Black, 5);
            Render.Circle.DrawCircle(CenterOfVectors(vBottomRigthCenter), Q.Width, Color.Black, 5);
            
            List<Vector3> xList = new List<Vector3>
            {
                vCenterTop,
                vCenterBottom,
                vCenterLeft,
                vCenterRight,
                vRightTop,
                vRightBottom,
                vLeftTop,
                vLeftBottom,
                CenterOfVectors(vTopRightCenter),
                CenterOfVectors(vBottomLeftCenter),
                CenterOfVectors(vTopLeftCenter),
                CenterOfVectors(vBottomRigthCenter)
            };

            var t = AssassinManager.GetTarget(Q.Range + W.Range);
            if (t.IsValidTarget())
            {
                foreach (var l in xList.OrderByDescending(o => o.Distance(t.Position)))
                {
                    if (l.Distance(t.Position) <= ObjectManager.Player.Position.Distance(t.Position))
                    {
                        for (var i = 1; i < ObjectManager.Player.Position.Distance(vCenterTop)/Q.Width; i++)
                        {
                            
                            var pos = ObjectManager.Player.ServerPosition.To2D().Extend(l.To2D(), i * Q.Width).To3D();
                            if (pos.Distance(ObjectManager.Player.Position) < ObjectManager.Player.BoundingRadius)
                                return;

                            IEnumerable<Obj_AI_Base> xM = Combos.QGetCollisionMinions(pos, t.ServerPosition, Q.Width, Q.Range, new CollisionableObjects[(int)CollisionableObjects.Minions]);

                            IEnumerable<Obj_AI_Base> objAiBases = xM as Obj_AI_Base[] ?? xM.ToArray();

                            if (xM != null && !objAiBases.Any())
                            {
                                if (Q.IsReady() && WStage == WCastStage.IsReady)
                                {
                                    WardJump(pos);
                                    Q.Cast(t.Position);
                                    //LeagueSharp.Common.Utility.DelayAction.Add(250, () => { Q.Cast(t); });

                                }

                                Render.Circle.DrawCircle(pos, Q.Width, pos.IsWall() ? Color.Red : Color.White, pos.IsWall() ? 5 : 2);
                            
                            }
                        }
                    }

                }
            }
            return;
            for (var i = 1; i < ObjectManager.Player.Position.Distance(vCenterTop) / Q.Width; i++)
            {
                
                var pos = ObjectManager.Player.ServerPosition.To2D().Extend(vCenterRight.To2D(), i * Q.Width).To3D();
                
                var pos1 = ObjectManager.Player.ServerPosition.To2D().Extend(vCenterLeft.To2D(), i * Q.Width).To3D();
                var pos2 = ObjectManager.Player.ServerPosition.To2D().Extend(vCenterTop.To2D(), i * Q.Width).To3D();
                var pos3 = ObjectManager.Player.ServerPosition.To2D().Extend(vCenterBottom.To2D(), i * Q.Width).To3D();

                var pos4 = ObjectManager.Player.ServerPosition.To2D().Extend(vRightTop.To2D(), i * Q.Width).To3D();
                var pos5 = ObjectManager.Player.ServerPosition.To2D().Extend(vRightBottom.To2D(), i * Q.Width).To3D();
                var pos6 = ObjectManager.Player.ServerPosition.To2D().Extend(vLeftTop.To2D(), i * Q.Width).To3D();
                var pos7 = ObjectManager.Player.ServerPosition.To2D().Extend(vLeftBottom.To2D(), i * Q.Width).To3D();


                var p1 = ObjectManager.Player.ServerPosition.To2D().Extend(CenterOfVectors(vTopRightCenter).To2D(), i * Q.Width).To3D();
                var p2 = ObjectManager.Player.ServerPosition.To2D().Extend(CenterOfVectors(vBottomLeftCenter).To2D(), i * Q.Width).To3D();
                var p3 = ObjectManager.Player.ServerPosition.To2D().Extend(CenterOfVectors(vTopLeftCenter).To2D(), i * Q.Width).To3D();
                var p4 = ObjectManager.Player.ServerPosition.To2D().Extend(CenterOfVectors(vBottomRigthCenter).To2D(), i * Q.Width).To3D();


                Render.Circle.DrawCircle(pos, 50f, pos.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos1, 50f, pos1.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos2, 50f, pos2.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos3, 50f, pos3.IsWall() ? Color.Red : Color.Aqua);

                Render.Circle.DrawCircle(pos4, 50f, pos4.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos5, 50f, pos5.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos6, 50f, pos6.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(pos7, 50f, pos7.IsWall() ? Color.Red : Color.Aqua);

                Render.Circle.DrawCircle(p1, 50f, p1.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(p2, 50f, p2.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(p3, 50f, p3.IsWall() ? Color.Red : Color.Aqua);
                Render.Circle.DrawCircle(p4, 50f, p4.IsWall() ? Color.Red : Color.Aqua);


            }
            return;
            for (var i = 1; i < Q.Range/50; i++)
                    {
                    ///var targetBehind = t.Position + Vector3.Normalize(t.ServerPosition - ObjectManager.Player.Position) - (i * 50);

                        var endpos1 = ObjectManager.Player.Position - 150;
                Render.Circle.DrawCircle(new Vector3(ObjectManager.Player.Position.X + Q.Width, ObjectManager.Player.Position.Y - Q.Range + Q.Width / 2,  0), 50f, Color.Red);
                Render.Circle.DrawCircle(new Vector3(ObjectManager.Player.Position.X + Q.Width * 2, ObjectManager.Player.Position.Y - Q.Range + Q.Width / 2 * 2, 0), 50f, Color.Red);
                Render.Circle.DrawCircle(new Vector3(ObjectManager.Player.Position.X + Q.Width * 3, ObjectManager.Player.Position.Y - Q.Range + Q.Width / 2 * 2, 0), 50f, Color.Red);
                Render.Circle.DrawCircle(new Vector3(ObjectManager.Player.Position.X + Q.Width * 4, ObjectManager.Player.Position.Y - Q.Range + Q.Width / 2 * 3, 0), 50f, Color.Red);
                Render.Circle.DrawCircle(new Vector3(ObjectManager.Player.Position.X + Q.Width * 5, ObjectManager.Player.Position.Y - Q.Range + Q.Width / 2 * 3, 0), 50f, Color.Red);

                // var targetBehind2 = t.Position + Vector3.Normalize(t.ServerPosition - endpos1) - (i * 50);
                //Render.Circle.DrawCircle(targetBehind, Q.Width, Color.Black);

            }
            

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var drawW = MenuDrawing.Item("Draw.W").GetValue<StringList>().SelectedIndex;
            switch (drawW)
            {
                case 1:
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
                        break;
                    }
                case 2:
                    {
                        if (Config.Item("Insec").GetValue<KeyBind>().Active)
                        {
                            Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
                        }
                        break;
                    }
                case 3:
                    {
                        if (Config.Item("Flee").GetValue<KeyBind>().Active)
                        {
                            Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
                        }
                        break;
                    }
                case 4:
                    {
                        if (Config.Item("Insec").GetValue<KeyBind>().Active || Config.Item("Flee").GetValue<KeyBind>().Active)
                        {
                            Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
                        }
                        break;
                    }
            }

            if (Config.Item("Flee.Active").GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(Game.CursorPos, 200, System.Drawing.Color.Blue);
                Render.Circle.DrawCircle(jumppoint, 50, System.Drawing.Color.Red);
                Render.Circle.DrawCircle(movepoint, 50, System.Drawing.Color.White);
            }

      

            //var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            //var t = AssassinManager.GetTarget(Q.Range);
            //if (!t.IsValidTarget())
            //    return;

            //foreach (var minion in minions.Where(x => x.IsValidTarget(Q.Range)))
            //{
            //    IEnumerable<Obj_AI_Base> xM = QGetCollisionMinions(ObjectManager.Player, t.ServerPosition);
            //    IEnumerable<Obj_AI_Base> objAiBases = xM as Obj_AI_Base[] ?? xM.ToArray();
            //    if (xM != null && objAiBases.Count() == 1)
            //    {
            //        var xxx = objAiBases.FirstOrDefault();
            //        if (xxx != null)
            //        {
            //            Render.Circle.DrawCircle(xxx.Position, 105f, Color.Red);
            //            if (SmiteDamageSlot != SpellSlot.Unknown && ObjectManager.Player.Spellbook.CanUseSpell(SmiteDamageSlot) == SpellState.Ready)
            //            {
            //                if (xxx.Health < 540 && Q.IsReady())
            //                {
            //                    Q.Cast(t.Position);
            //                    ObjectManager.Player.Spellbook.CastSpell(SmiteDamageSlot, xxx);
            //                }
            //            }
            //        }
            //    }

            //    return;
            //    Obj_AI_Base m = QGetCollisionMinions(ObjectManager.Player, t.ServerPosition).OrderByDescending(o => o).FirstOrDefault();
            //    Render.Circle.DrawCircle(m.Position, 105f, Color.Red);

            //    //foreach (Obj_AI_Base colminion in QGetCollisionMinions(ObjectManager.Player, t.ServerPosition))
            //    //{
            //    //    Render.Circle.DrawCircle(colminion.Position, 105f, Color.Red);
            //    //}

            //}
            //var t = Game.CursorPos;

            //for (var i = 1; i < (ObjectManager.Player.Distance(Game.CursorPos) / 40); i++)
            //{
            //    var targetBehind = Game.CursorPos + Vector3.Normalize(ObjectManager.Player.Position - Game.CursorPos)*(i*40);

            //    Render.Circle.DrawCircle(targetBehind, 40f, targetBehind.IsWall() ? Color.Red : Color.GreenYellow);
            //}


            if (checker)
            {
                Drawing.DrawText(Drawing.WorldToScreen(jumppoint)[0] + 50, Drawing.WorldToScreen(jumppoint)[1] + 40, Color.Red, "NOT JUMPABLE");
            }


            if (Config.Item("Insec").GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(InsecJumpPosition, 75, System.Drawing.Color.Blue);
                Render.Circle.DrawCircle(insdirec, 100, System.Drawing.Color.Green);
            }
            

            if (Config.Item("damagetest").GetValue<bool>())
            {
                foreach (var enemyVisible in ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget()))

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50, Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red, "Combo=Rekt");
                    }
            }


            if (Config.Item("CircleLag").GetValue<bool>())
            {
                if (Config.Item("DrawQ").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Blue);
                }

                if (Config.Item("Draw.W").GetValue<bool>())
                {
                    
                }
                if (Config.Item("DrawE").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
                }
                if (Config.Item("DrawR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Blue);
                }
            }
            else
            {
                if (Config.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.White);
                }
                if (Config.Item("Draw.W").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.White);
                }
                if (Config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
                }

                if (Config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.White);
                }
            }
        }
    }
}
