using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Orianna
{
    internal class Program
    {
        public static string ChampionName = "Orianna";
        public static Orbwalking.Orbwalker Orbwalker;
        public static Spell Q, W, E, R;
        public static Menu Config;
        public static Vector3 BallPos;
        public static bool isBallMoving;
        public static Obj_AI_Hero target;

        public static List<Spell> SpellList = new List<Spell>();
        public static Dictionary<string, List<string>> GinitiatorList = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> GinterruptList = new Dictionary<string, List<string>>();
        public static Dictionary<string, string> GswitchList = new Dictionary<string, string>();

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        #region OnGameLoad

        private static void UseItems()
        {
            const int vItemId = 3029;
            bool hasItem = Items.HasItem(vItemId);    
            const int xDangerousRange = 1100;
                var xCanUse = ObjectManager.Player.Health <=
                              ObjectManager.Player.MaxHealth/100*15;

                if (xCanUse && !ObjectManager.Player.InShop() && hasItem && Utility.CountEnemiesInRange(xDangerousRange) > 0)
                {
                    
                    Items.UseItem(vItemId, ObjectManager.Player);
                }
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.BaseSkinName != ChampionName)
                return;
            var initiatorList = new Dictionary<string, List<string>>();
            var interruptList = new Dictionary<string, List<string>>();
            var switchList = new Dictionary<string, string>();

            var viSkills = CreateList("ViQ", "ViR");
            initiatorList.Add("Vi", viSkills);

            var malphSkills = CreateList("Landslide");
            initiatorList.Add("Malphite", malphSkills);
            
            var noctSkills = CreateList("NocturneParanoia");
            initiatorList.Add("Nocturne", noctSkills);
            
            var zacSkills = CreateList("ZacE");
            initiatorList.Add("Zac", zacSkills);

            var wukongSkills = CreateList("MonkeyKingNimbus", "MonkeyKingSpinToWin", "SummonerFlash");
            initiatorList.Add("MonkeyKing", wukongSkills);
            
            var shyvSkills = CreateList("ShyvanaTransformCast");
            initiatorList.Add("Shyvana", shyvSkills);
            
            var threshSkills = CreateList("threshqleap");
            initiatorList.Add("Thresh", threshSkills);
            
            var aatroxSkills = CreateList("AatroxQ");
            initiatorList.Add("Aatrox", aatroxSkills);
            
            var renekSkills = CreateList("RenektonSliceAndDice");
            initiatorList.Add("Renekton", renekSkills);
            
            var kennenSkills = CreateList("KennenLightningRush", "SummonerFlash");
            initiatorList.Add("Kennen", kennenSkills);
            
            var olafSkills = CreateList("OlafRagnarok");
            initiatorList.Add("Olaf", olafSkills);
            
            var udyrSkills = CreateList("UdyrBearStance");
            initiatorList.Add("Udyr", udyrSkills);
            
            var voliSkills = CreateList("VolibearQ");
            initiatorList.Add("Volibear", voliSkills);
            
            var talonSkills = CreateList("TalonCutthroat");
            initiatorList.Add("Talon", talonSkills);
            
            var jarvanSkills = CreateList("JarvanIVDragonStrike");
            initiatorList.Add("JarvanIV", jarvanSkills);
            
            var warwickSkills = CreateList("InfiniteDuress");
            initiatorList.Add("Warwick", warwickSkills);
            
            var jaxSkills = CreateList("JaxLeapStrike");
            initiatorList.Add("Jax", jaxSkills);
            
            var yasuoSkills = CreateList("YasuoRKnockUPComboW");
            initiatorList.Add("Yasuo", yasuoSkills);
            
            var dianaSkills = CreateList("DianaTeleport");
            initiatorList.Add("Diana", dianaSkills);
            
            var leeSkills = CreateList("BlindMonkQTwo");
            initiatorList.Add("LeeSin", leeSkills);
            
            var shenSkills = CreateList("ShenShadowDash");
            initiatorList.Add("Shen", shenSkills);
            
            var alistarSkills = CreateList("Headbutt");
            initiatorList.Add("Alistar", alistarSkills);
            
            var amumuSkills = CreateList("BandageToss");
            initiatorList.Add("Amumu", amumuSkills);
            
            var urgotSkills = CreateList("UrgotSwap2");
            
            initiatorList.Add("Urgot", urgotSkills);
            
            var rengarSkills = CreateList("RengarR");
            initiatorList.Add("Rengar", rengarSkills);

            //InterrupList
            List<string> katSkills = CreateList("KatarinaR");
            interruptList.Add("Katarina", katSkills);
            List<string> mahlzSkills = CreateList("AlZaharNetherGrasp");
            interruptList.Add("Malzahar", mahlzSkills);
            List<string> warwickIntSkills = CreateList("InfiniteDuress");
            interruptList.Add("Warwick", warwickIntSkills);
            List<string> velkozSkills = CreateList("VelkozR");
            interruptList.Add("Velkoz", velkozSkills);

            //switch for baseSkinName
            //Udyr
            switchList.Add("udyrphoenix", "Udyr");
            switchList.Add("udyrtiger", "Udyr");
            switchList.Add("udyrturtle", "Udyr");

            GinitiatorList = initiatorList;
            GinterruptList = interruptList;
            GswitchList = switchList;

            Q = new Spell(SpellSlot.Q, 825f);
            W = new Spell(SpellSlot.W, 0f);
            E = new Spell(SpellSlot.E, 1095f);
            R = new Spell(SpellSlot.R, float.MaxValue);

            //Q.SetSkillshot(0f, 80f, 1200f, false, SkillshotType.SkillshotCircle);
            Q.SetSkillshot(0.2f, 80f, 1200f, false, SkillshotType.SkillshotCircle);
            W.SetSkillshot(0.25f, 275f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.25f, 100f, 1700f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.6f, 400f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            BallPos = ObjectManager.Player.ServerPosition;

            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("UltKillable", "Auto-Ult if Killable").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("AutoW", "Auto-W Enemies").SetValue(false));
            Config.SubMenu("Combo").AddItem(new MenuItem("AllIn", "All-in if Killable").SetValue(true));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("MinTargets", "Min. Target(s) to Use R").SetValue(new Slider(1, 5, 0)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("UltMinToggle", "Use R if >= X Target(s)").SetValue(
                        new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("HealthSliderE", "Use E in Combo if % HP <").SetValue(new Slider(60, 100, 0)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboActive", "Combo").SetValue(new KeyBind("Z".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("ManaSliderHarass", "Min. % Mana").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("A".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("HarassActiveT", "Harass (Toggle)").SetValue(new KeyBind("L".ToCharArray()[0],
                        KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("Lane Clear", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseQFarm", "Use Q").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseWFarm", "Use W").SetValue(true));
            Config.SubMenu("Farm").AddItem(new MenuItem("UseEFarm", "Use E").SetValue(true));
            Config.SubMenu("Farm")
                .AddItem(new MenuItem("ManaSliderFarm", "Min. % Mana").SetValue(new Slider(25, 100, 0)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuItem("FarmActive", "Lane Clear").SetValue(new KeyBind("B".ToCharArray()[0], KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Jungle Clear", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseQJFarm", "Use Q").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseWJFarm", "Use W").SetValue(true));
            Config.SubMenu("JungleFarm").AddItem(new MenuItem("UseEJFarm", "Use E").SetValue(true));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuItem("JungleFarmActive", "Jungle Clear").SetValue(new KeyBind("S".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawQRange", "Draw Q Range").SetValue(new Circle(true,
                        Color.FromArgb(100, 255, 0, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawWRange", "Draw W Range").SetValue(new Circle(false,
                        Color.FromArgb(100, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawERange", "Draw E Range").SetValue(new Circle(true,
                        Color.FromArgb(100, 255, 255, 255))));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuItem("DrawRRange", "Draw R Range").SetValue(new Circle(false,
                        Color.FromArgb(100, 255, 255, 255))));

            Config.AddSubMenu(new Menu("Auto-E Allied Initiators", "AutoEInit"));
            Config.SubMenu("AutoEInit").AddItem(new MenuItem("InitEnabled", "Enabled").SetValue(true));
            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (!current.IsMe && current.IsAlly && GinitiatorList.ContainsKey(current.BaseSkinName))
                {
                    string eChamp = "AutoE" + current.BaseSkinName;
                    Config.SubMenu("AutoEInit").AddItem(new MenuItem(eChamp, current.BaseSkinName).SetValue(true));
                }
            }

            Config.AddSubMenu(new Menu("Debug", "Debug"));
            Config.SubMenu("Debug").AddItem(new MenuItem("DebugR", "Enable Debug Mode")).SetValue(false);
            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        #endregion

        #region OnDraw

        private static void Drawing_OnDraw(EventArgs args)
        {
            var qValue = Config.Item("DrawQRange").GetValue<Circle>();
            if (qValue.Active)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, qValue.Color, 1);

            var wValue = Config.Item("DrawWRange").GetValue<Circle>();
            if (wValue.Active)
                Render.Circle.DrawCircle(BallManager.CurrentBallDrawPosition, W.Width, wValue.Color, 1);

            var eValue = Config.Item("DrawERange").GetValue<Circle>();
            if (eValue.Active)
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, eValue.Color, 1);

            var rValue = Config.Item("DrawRRange").GetValue<Circle>();
            if (rValue.Active)
            {
                Render.Circle.DrawCircle(BallManager.CurrentBallDrawPosition, R.Width, rValue.Color, 1);
            }
        }

        #endregion

        #region OnUpdate

        private static void Game_OnUpdate(EventArgs args)
        {
            //For Auto E Initiators 
            TickChecks();
            UseItems();
            //Combo & Harass
            if (Config.Item("ComboActive").GetValue<KeyBind>().Active ||
                ((Config.Item("HarassActive").GetValue<KeyBind>().Active ||
                  Config.Item("HarassActiveT").GetValue<KeyBind>().Active) &&
                 (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100) >
                 Config.Item("ManaSliderHarass").GetValue<Slider>().Value))
            {
                target = TargetSelector.GetTarget(1125f, TargetSelector.DamageType.Magical);
                if (target != null)
                {
                    bool comboActive = Config.Item("ComboActive").GetValue<KeyBind>().Active;
                    bool harassActive = Config.Item("HarassActive").GetValue<KeyBind>().Active;
                    bool harassActiveT = Config.Item("HarassActiveT").GetValue<KeyBind>().Active;
                    var useR = Config.Item("UseRCombo").GetValue<bool>();
                    var autoUlt = Config.Item("UltKillable").GetValue<bool>();
                    var useW = Config.Item("UseWCombo").GetValue<bool>();
                    var useQ = Config.Item("UseQCombo").GetValue<bool>();
                    var allIn = Config.Item("AllIn").GetValue<bool>();
                    bool ultMinToggle = Config.Item("UltMinToggle").GetValue<KeyBind>().Active;

                    if (comboActive && allIn && useQ && useW && useR && Q.IsReady())
                    {
                        if (isAlone(target) &&
                            ObjectManager.Player.ServerPosition.Distance(target.ServerPosition) <= 825 &&
                            GetComboDamage(target) >= target.Health)
                        {
                            PredictionOutput prediction = Q.GetPrediction(target);
                            if (prediction.Hitchance >= HitChance.High)
                            {
                                Q.Cast(prediction.CastPosition);
                                CastW(target);
                                if (GetNumberHitByR(target) >= 1)
                                {
                                    R.Cast();
                                }
                                if (CastIgnite(target))
                                {
                                }
                            }
                        }
                    }

                    if (((comboActive &&
                          Config.Item("UseQCombo").GetValue<bool>()) ||
                         ((harassActive || harassActiveT) &&
                          Config.Item("UseQHarass").GetValue<bool>())) && Q.IsReady())
                    {
                        Tuple<Vector3, int> check = getMECQPos(target);
                        Vector3 position = check.Item1;
                        int num = check.Item2;

                        if (num == 3 && R.IsReady() && useR)
                        {
                            Q.Cast(position, true);
                        }
                        if (num == 2 && W.IsReady() && useW)
                        {
                            Q.Cast(position, true);
                        }
                        if (num == 1)
                        {
                            Q.Cast(position, true);
                        }
                        if (num == 4)
                        {
                            Q.Cast(position, true);
                        }
                    }

                    if (Config.Item("AutoW").GetValue<bool>() && W.IsReady())
                    {
                        if (!isBallMoving)
                        {
                            CastW(target);
                        }
                    }

                    if (((comboActive &&
                          Config.Item("UseWCombo").GetValue<bool>()) ||
                         ((harassActive || harassActiveT) &&
                          Config.Item("UseWHarass").GetValue<bool>())) && W.IsReady())
                    {
                        if (!isBallMoving)
                        {
                            CastW(target);
                        }
                    }

                    if (((comboActive &&
                          Config.Item("UseECombo").GetValue<bool>()) ||
                         ((harassActive || harassActiveT) &&
                          Config.Item("UseEHarass").GetValue<bool>())) && E.IsReady())
                    {
                        if ((Vector3.Distance(BallPos, target.ServerPosition) > 925) &&
                            Vector3.Distance(ObjectManager.Player.ServerPosition, target.ServerPosition) <= Q.Range)
                        {
                            E.CastOnUnit(ObjectManager.Player);
                        }
                        CastE(target);
                    }

                    if (comboActive && Config.Item("UseRCombo").GetValue<bool>() && ultMinToggle)
                    {
                        if (!isBallMoving)
                        {
                            CastR(target);
                        }
                    }

                    //R if killable
                    if (comboActive && useR && autoUlt && R.IsReady() &&
                        ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health &&
                        !BallManager.IsBallMoving)
                    {
                        if (willHitRKill(target))
                        {
                            R.Cast();
                        }
                    }
                }
            }

            if (Config.Item("FarmActive").GetValue<KeyBind>().Active ||
                Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                Obj_AI_Hero farmTarget = TargetSelector.GetTarget(1125f, TargetSelector.DamageType.Magical);
                if (farmTarget != null && Config.Item("FarmActive").GetValue<KeyBind>().Active)
                {
                    FarmWTarget(farmTarget);
                }
                if (Config.Item("FarmActive").GetValue<KeyBind>().Active)
                {
                    Farm();
                }
                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                {
                    JungleFarm();
                }
            }
        }

        #endregion

        #region Casts

        private static void CastQ(Obj_AI_Base target)
        {
            PredictionOutput prediction = Q.GetPrediction(target);
            if (prediction.Hitchance >= HitChance.Medium)
            {
                if (ObjectManager.Player.ServerPosition.Distance(prediction.CastPosition) <= Q.Range + Q.Width)
                {
                    Q.Cast(prediction.CastPosition, true);
                }
            }
        }

        private static void CastW(Obj_AI_Base target)
        {
            if (BallManager.IsBallMoving)
                return;

            int hit = GetNumberHitByW(target);
            if (hit >= 1)
            {
                W.Cast();
                //ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W);
            }
        }

        private static void CastE(Obj_AI_Base target)
        {
            int numHit = GetNumberHeroesHitByE();
            float healthPer = (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100;
            float useEHealthBelow = Config.Item("HealthSliderE").GetValue<Slider>().Value;
            bool useE = healthPer <= useEHealthBelow;
            if (!isBallMoving && BallPos != ObjectManager.Player.Position && numHit >= 1 && useE)
            {
                E.CastOnUnit(ObjectManager.Player);
            }
        }

        private static void CastR(Obj_AI_Base target)
        {
            if (BallManager.IsBallMoving)
            {
                return;
            }
            if (GetNumberHitByR(target) >= Config.Item("MinTargets").GetValue<Slider>().Value)
            {
                R.Cast(target, true, true);
            }
        }

        #endregion

        #region HitCountChecks

        private static int GetNumberHitByW(Obj_AI_Base target)
        {
            return
            ObjectManager.Get<Obj_AI_Hero>()
                         .Count(
                              current =>
                                        !current.IsMe && current.IsEnemy &&
                                        Vector3.Distance(BallManager.CurrentBallPosition, current.ServerPosition) <= W.Width - 14);
        }

        private static int GetNumberHeroesHitByE()
        {
            List<Obj_AI_Base> heroresult = (from hero in ObjectManager.Get<Obj_AI_Hero>()
                let input = new PredictionInput
                {
                    Unit = hero,
                    Delay = E.Delay,
                    Radius = E.Width,
                    Speed = E.Speed,
                    From = BallManager.CurrentBallPosition,
                }
                where hero.IsValidTarget(input.Range + input.Radius + 100, true, input.From)
                let prediction = Prediction.GetPrediction(input)
                where
                    prediction.UnitPosition
                        .To2D()
                        .Distance(input.From.To2D(), ObjectManager.Player.ServerPosition.To2D(), true, true) <=
                    Math.Pow((input.Radius + 50 + hero.BoundingRadius), 2)
                select hero).Cast<Obj_AI_Base>().ToList();
            return heroresult.Count;
        }

        private static List<Obj_AI_Base> GetMinionsHitByE()
        {
            return (from minion in ObjectManager.Get<Obj_AI_Minion>()
                let input = new PredictionInput
                {
                    Unit = minion,
                    Delay = E.Delay,
                    Radius = E.Width,
                    Speed = E.Speed,
                    From = BallManager.CurrentBallPosition,
                }
                where minion.IsValidTarget(input.Range + input.Radius + 100, true, input.From)
                let prediction = Prediction.GetPrediction(input)
                where
                    prediction.UnitPosition
                        .To2D()
                        .Distance(input.From.To2D(), ObjectManager.Player.ServerPosition.To2D(), true, true) <=
                    Math.Pow((input.Radius + 15 + minion.BoundingRadius), 2)
                select minion).Cast<Obj_AI_Base>().ToList();
        }

        private static int GetNumberHitByR(Obj_AI_Base target)
        {
            var debug = Config.Item("DebugR").GetValue<bool>();
            if (debug)
            {
                Game.PrintChat("Hitbox size is: " + target.BoundingRadius);
            }
            int totalHit = 0;
            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
            {
                PredictionOutput prediction = R.GetPrediction(current, true);
                if (debug)
                {
                    Game.PrintChat("HitChance is: " + prediction.Hitchance);
                }
                if (prediction.Hitchance >= HitChance.High && !current.IsMe && current.IsEnemy &&
                    Vector3.Distance(BallPos, prediction.CastPosition) <= R.Width - 20) 
                {
                    totalHit = totalHit + 1;
                    Drawing.DrawCircle(current.Position, 75f, Color.Red);
                }
                
            }
            if (debug)
            {
                Game.PrintChat("Targets hit is: " + totalHit);
            }
            return totalHit;
        }

        #endregion

        #region Utility functions

        private static List<T> CreateList<T>(params T[] values)
        {
            return new List<T>(values);
        }

        private static Tuple<Vector3, int> getMECQPos(Obj_AI_Hero target)
        {
            var pointsList = new List<Vector2>();
            PredictionOutput targetPred = Q.GetPrediction(target);
            if (targetPred.Hitchance >= HitChance.High)
            {
                pointsList.Add(targetPred.CastPosition.To2D());
            }
            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (!current.IsMe && current.NetworkId != target.NetworkId && current.IsEnemy &&
                    current.IsValidTarget(Q.Range + (R.Width / 2)))
                {
                    PredictionOutput prediction = Q.GetPrediction(current);
                    if (prediction.Hitchance >= HitChance.High)
                    {
                        pointsList.Add(prediction.CastPosition.To2D());
                    }
                }
            }

            while (pointsList.Count != 0)
            {
                MEC.MecCircle circle = MEC.GetMec(pointsList);
                int numPoints = pointsList.Count;

                if (circle.Radius <= (R.Width / 2) && numPoints >= 3 && R.IsReady())
                {
                    return Tuple.Create(circle.Center.To3D(), 3);
                }

                if (circle.Radius <= (W.Width / 2) && numPoints >= 2 && W.IsReady())
                {
                    return Tuple.Create(circle.Center.To3D(), 2);
                }

                if (pointsList.Count == 1)
                {
                    return Tuple.Create(circle.Center.To3D(), 1);
                }

                if (circle.Radius <= ((Q.Width / 2) + 50) && numPoints > 1)
                {
                    return Tuple.Create(circle.Center.To3D(), 4);
                }

                try
                {
                    float distance = -1f;
                    int index = 0;
                    Vector2 point = pointsList.ElementAt(0);
                    for (int i = 1; i == numPoints; i++)
                    {
                        if (Vector2.Distance(pointsList.ElementAt(i), point) >= distance)
                        {
                            distance = Vector2.Distance(pointsList.ElementAt(i), point);
                            index = i;
                        }
                    }
                    pointsList.RemoveAt(index);
                }
                catch (ArgumentOutOfRangeException)
                {
                    var outOfRange = new Vector3(0);
                    return Tuple.Create(outOfRange, -1);
                }
            }
            var noResult = new Vector3(0);
            return Tuple.Create(noResult, -1);
        }

        private static bool willHitRKill(Obj_AI_Base target)
        {
            PredictionOutput prediction = R.GetPrediction(target);
            if (prediction.Hitchance >= HitChance.High &&
                Vector3.Distance(BallPos, prediction.CastPosition) <= R.Width - (target.BoundingRadius / 2))
            {
                return true;
            }
            return false;
        }

        private static float GetComboDamage(Obj_AI_Base enemy)
        {
            double damage = 0d;
            Obj_AI_Hero Player = ObjectManager.Player;

            SpellSlot igniteSlot = Player.GetSpellSlot("SummonerIgnite");
            bool igniteReady = ObjectManager.Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready;
            if (igniteSlot != SpellSlot.Unknown && igniteReady)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);

            if (Q.IsReady())
            {
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);
            }

            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);

            if (R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }

        private static bool isAlone(Obj_AI_Hero target)
        {
            int numEnemies = 0;
            foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (!current.IsMe && current.NetworkId != target.NetworkId && current.IsEnemy &&
                    current.Distance(target.ServerPosition) <= 1000)
                {
                    numEnemies += 1;
                }
            }

            return numEnemies == 0;
        }

        private static SpellDataInst GetIgnite()
        {
            SpellDataInst[] spells = ObjectManager.Player.Spellbook.Spells;
            return spells.FirstOrDefault(spell => spell.Name == "SummonerDot");
        }

        private static bool CastIgnite(Obj_AI_Hero enemy)
        {
            if (!enemy.IsValid || !enemy.IsVisible || !enemy.IsTargetable || enemy.IsDead)
            {
                return false;
            }
            SpellDataInst ignite = GetIgnite();
            if (ignite != null && ignite.Slot != SpellSlot.Unknown && ignite.State == SpellState.Ready &&
                ObjectManager.Player.CanCast)
            {
                ObjectManager.Player.Spellbook.CastSpell(ignite.Slot, enemy);
                return true;
            }
            return false;
        }

        private static Tuple<Vector3, int> GetMecqFarmPos()
        {
            List<Vector2> pointsList = (from current in ObjectManager.Get<Obj_AI_Minion>()
                where
                    current.IsEnemy &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, current.Position) <= (Q.Range + (W.Width/2))
                let prediction = Q.GetPrediction(current)
                let damage = ObjectManager.Player.GetSpellDamage(current, SpellSlot.W)*0.75
                where prediction.Hitchance >= HitChance.High && damage > current.Health
                select prediction.CastPosition.To2D()).ToList();

            while (pointsList.Count != 0)
            {
                MEC.MecCircle circle = MEC.GetMec(pointsList);
                int numPoints = pointsList.Count;

                if (circle.Radius <= (W.Width / 2) && numPoints >= 2 && W.IsReady())
                {
                    return Tuple.Create(circle.Center.To3D(), numPoints);
                }

                try
                {
                    float distance = -1f;
                    int index = 0;
                    Vector2 point = pointsList.ElementAt(0);
                    for (int i = 1; i == numPoints; i++)
                    {
                        if (Vector2.Distance(pointsList.ElementAt(i), point) >= distance)
                        {
                            distance = Vector2.Distance(pointsList.ElementAt(i), point);
                            index = i;
                        }
                    }
                    pointsList.RemoveAt(index);
                }
                catch (ArgumentOutOfRangeException)
                {
                    var outOfRange = new Vector3(0);
                    return Tuple.Create(outOfRange, -1);
                }
            }
            var noResult = new Vector3(0);
            return Tuple.Create(noResult, -1);
        }

        private static void TickChecks()
        {
            BallPos = BallManager.CurrentBallPosition;
            isBallMoving = BallManager.IsBallMoving;
            R.UpdateSourcePosition(BallPos);
            W.UpdateSourcePosition(BallPos);

            var eInitiators = Config.Item("InitEnabled").GetValue<bool>();
            if (E.IsReady() && eInitiators)
            {
                foreach (Obj_AI_Hero current in ObjectManager.Get<Obj_AI_Hero>())
                {
                    target = TargetSelector.GetTarget(1125f, TargetSelector.DamageType.Magical);
                    String champName = current.BaseSkinName;
                    String result;
                    List<string> spellList;
                    if (GswitchList.TryGetValue(champName, out result))
                    {
                        champName = result;
                    }
                    if (!current.IsMe && current.IsAlly &&
                        Vector3.Distance(ObjectManager.Player.ServerPosition, current.Position) < E.Range &&
                        GinitiatorList.TryGetValue(champName, out spellList))
                    {
                        string stringCheck = "AutoE" + champName;
                        string spellName = current.LastCastedSpellName();
                        if (Config.Item(stringCheck).GetValue<bool>() && (current.LastCastedspell() != null) &&
                            (current.LastCastedSpellName() != null) && spellList.Contains(spellName) &&
                            (Environment.TickCount - current.LastCastedSpellT()) < 1.5)
                        {
                            E.CastOnUnit(current);
                            if (target != null &&
                                GetNumberHitByR(target) >= Config.Item("MinTargets").GetValue<Slider>().Value &&
                                R.IsReady())
                            {
                                R.Cast(target, true, true);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Farming

        private static void FarmWTarget(Obj_AI_Hero target)
        {
            if (!Orbwalking.CanMove(40))
            {
                return;
            }
            if (Config.Item("ManaSliderFarm").GetValue<Slider>().Value >
                ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100)
            {
                return;
            }

            List<Obj_AI_Base> rangedMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.Ranged);
            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All);

            var useQ = Config.Item("UseQFarm").GetValue<bool>();
            var useW = Config.Item("UseWFarm").GetValue<bool>();
            var useE = Config.Item("UseEFarm").GetValue<bool>();
            var killableMinions = new List<Obj_AI_Base>();

            foreach (Obj_AI_Base minion in allMinions)
            {
                if (!Orbwalking.InAutoAttackRange(minion) &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, minion.Position) <= Q.Range)
                {
                    PredictionOutput prediction = Q.GetPrediction(minion);
                    double Qdamage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) * 0.85;

                    if (Qdamage >= Q.GetHealthPrediction(minion))
                    {
                        killableMinions.Add(minion);
                    }
                }
            }

            if (useQ && Q.IsReady())
            {
                Tuple<Vector3, int> mecLocation = GetMecqFarmPos();
                Vector3 position = mecLocation.Item1;
                int killableCount = mecLocation.Item2;
                if (killableCount >= 2)
                {
                    Q.Cast(position, true);
                }
            }

            if (useW && W.IsReady())
            {
                int minionsHit =
                    allMinions.Count(
                        minion =>
                            Vector3.Distance(BallManager.CurrentBallDrawPosition, minion.ServerPosition) <= W.Width &&
                            W.GetDamage(minion) > minion.Health);
                if (minionsHit >= 2)
                {
                    W.Cast();
                    //ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W);
                }
            }

            if (Q.IsReady() && useQ)
            {
                if (killableMinions.Count == 0 && target != null)
                {
                    PredictionOutput prediction = Q.GetPrediction(target);
                    if (prediction.Hitchance == HitChance.OutOfRange)
                    {
                        Q.Cast(ObjectManager.Player
                            .ServerPosition
                            .To2D()
                            .Extend(prediction.CastPosition.To2D(), Q.Range - 5));
                    }
                    else if (prediction.Hitchance >= HitChance.High)
                    {
                        Q.Cast(prediction.CastPosition);
                    }
                }
                else
                {
                    foreach (Obj_AI_Base minion in killableMinions)
                    {
                        Q.CastOnUnit(minion);
                    }
                }
            }

            if (E.IsReady() && useE)
            {
                List<Obj_AI_Base> collisionList = GetMinionsHitByE();
                foreach (Obj_AI_Base minion in collisionList)
                {
                    double damage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E) * 0.88;
                    if (E.IsReady() && damage >= E.GetHealthPrediction(minion))
                    {
                        E.CastOnUnit(ObjectManager.Player);
                    }
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40))
            {
                return;
            }
            if (Config.Item("ManaSliderFarm").GetValue<Slider>().Value >
                ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100)
            {
                return;
            }

            List<Obj_AI_Base> allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

            var useQ = Config.Item("UseQFarm").GetValue<bool>();
            var useW = Config.Item("UseWFarm").GetValue<bool>();
            var useE = Config.Item("UseEFarm").GetValue<bool>();

            List<Obj_AI_Base> killableMinions = (from minion in allMinions
                where
                    !Orbwalking.InAutoAttackRange(minion) &&
                    Vector3.Distance(ObjectManager.Player.ServerPosition, minion.Position) <= Q.Range
                let prediction = Q.GetPrediction(minion)
                let Qdamage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q)*0.85
                where Qdamage >= Q.GetHealthPrediction(minion)
                select minion).ToList();

            if (useQ && Q.IsReady())
            {
                Tuple<Vector3, int> mecLocation = GetMecqFarmPos();
                Vector3 position = mecLocation.Item1;
                int hitCount = mecLocation.Item2;
                if (hitCount >= 2)
                {
                    Q.Cast(position, true);
                }
            }

            if (useW && W.IsReady())
            {
                var minionsHit =
                    allMinions.Count(
                        minion =>
                            Vector3.Distance(BallManager.CurrentBallDrawPosition, minion.ServerPosition) <= W.Width &&
                            W.GetDamage(minion) > minion.Health);
                if (minionsHit >= 2)
                {
                    W.Cast();
                    //ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W);
                }
            }

            if (Q.IsReady() && useQ)
            {
                foreach (Obj_AI_Base minion in killableMinions)
                {
                    Q.CastOnUnit(minion);
                }
            }

            if (E.IsReady() && useE)
            {
                var collisionList = GetMinionsHitByE();
                foreach (var minion in from minion in collisionList
                    let damage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.E)*0.88
                    where E.IsReady() && damage >= E.GetHealthPrediction(minion)
                    select minion)
                {
                    E.CastOnUnit(ObjectManager.Player);
                }
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJFarm").GetValue<bool>();
            var useW = Config.Item("UseWJFarm").GetValue<bool>();
            var useE = Config.Item("UseEJFarm").GetValue<bool>();

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0) return;

            var mob = mobs[0];

            if (Q.IsReady() && useQ)
            {
                Q.Cast(mob);
            }

            if (useW && W.IsReady() &&
                Vector3.Distance(BallManager.CurrentBallDrawPosition, mob.ServerPosition) <= W.Width)
            {
                W.Cast();
                //ObjectManager.Player.Spellbook.CastSpell(SpellSlot.W);
            }

            if (useE && E.IsReady())
            {
                var collisionList = GetMinionsHitByE();
                var hitCount = collisionList.Count;
                var healthPer = ObjectManager.Player.Health / ObjectManager.Player.MaxHealth;
                var manaPer = ObjectManager.Player.Mana / ObjectManager.Player.MaxMana;
                if (hitCount >= 1 || (healthPer < 0.40 && manaPer >= 0.20))
                {
                    E.CastOnUnit(ObjectManager.Player);
                }
            }
        }
        #endregion
    }
}
