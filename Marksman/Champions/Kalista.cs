#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Marksman.Utils;
using SharpDX;
using Color = System.Drawing.Color;
using SharpDX.Direct3D9;
using Collision = LeagueSharp.Common.Collision;
#endregion

namespace Marksman.Champions
{
    using System.Runtime.Remoting.Messaging;

    internal class EnemyMarker
    {
        public string ChampionName { get; set; }

        public double ExpireTime { get; set; }

        public int BuffCount { get; set; }
    }

    internal class AttackMinions
    {
        public Obj_AI_Minion Minion;

        public AttackMinions(Obj_AI_Minion minion)
        {
            Minion = minion;
        }
    }

    internal class Kalista : Champion
    {
        public static Spell Q, W, E, R;

        public static Font font;

        public static Obj_AI_Hero SoulBound { get; private set; }

        private static string kalistaEBuffName = "kalistaexpungemarker";

        private static List<Obj_AI_Minion> attackMinions = new List<Obj_AI_Minion>();

        private static List<EnemyMarker> xEnemyMarker = new List<EnemyMarker>();

        private static Dictionary<String, int> MarkedChampions = new Dictionary<String, int>();

        private static Dictionary<Vector3, Vector3> JumpPos = new Dictionary<Vector3, Vector3>();

        private static Dictionary<float, float> incomingDamage = new Dictionary<float, float>();

        private static Dictionary<float, float> InstantDamage = new Dictionary<float, float>();

        public static float IncomingDamage
        {
            get { return incomingDamage.Sum(e => e.Value) + InstantDamage.Sum(e => e.Value); }
        }

        public Kalista()
        {
            Q = new Spell(SpellSlot.Q, 1150);
            W = new Spell(SpellSlot.W, 5000);
            E = new Spell(SpellSlot.E, 1000);
            R = new Spell(SpellSlot.R, 1100);

            Q.SetSkillshot(0.25f, 40f, 2100f, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotLine);
            R.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            font = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 45,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default
                });


            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

            Utils.Utils.PrintMessage("Kalista loaded.");
        }

        private void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            font.Dispose();
        }

        private void DrawingOnOnPostReset(EventArgs args)
        {
            font.OnResetDevice();
        }

        private void DrawingOnOnPreReset(EventArgs args)
        {
            font.OnLostDevice();
        }


        public static int KalistaMarkerCount
        {
            get
            {
                return (from enemy in ObjectManager.Get<Obj_AI_Hero>().Where(tx => tx.IsEnemy && !tx.IsDead)
                    where ObjectManager.Player.Distance(enemy) < E.Range
                    from buff in enemy.Buffs
                    where buff.Name.Contains("kalistaexpungemarker")
                    select buff).Select(buff => buff.Count).FirstOrDefault();
            }
        }

        public override void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && GetValue<bool>("Combo.UseQ") && Q.IsReady())
            {
                var enemy = target as Obj_AI_Hero;
                if (enemy != null)
                {
                    if (ObjectManager.Player.TotalAttackDamage < enemy.Health + enemy.AllShield)
                    {
                        Q.Cast(enemy);
                    }
                }
            }
        }

        public override void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            var killableMinionCount = 0;
            foreach (var m in
                MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
                    .Where(x => E.CanCast(x) && x.Health < E.GetDamage(x)))
            {
                if (m.SkinName.ToLower() == "sru_chaosminionsiege" || m.SkinName.ToLower() == "sru_chaosminionsuper")
                    killableMinionCount += 2;
                else killableMinionCount++;

                Render.Circle.DrawCircle(m.Position, (float) (m.BoundingRadius*1.5), Color.White, 5);
            }

            foreach (var m in
                MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition,
                    E.Range,
                    MinionTypes.All,
                    MinionTeam.Neutral).Where(m => E.CanCast(m) && m.Health < E.GetDamage(m)))
            {
                if (m.SkinName.ToLower().Contains("baron") || m.SkinName.ToLower().Contains("dragon") && E.CanCast(m))
                    E.Cast(m);
                else Render.Circle.DrawCircle(m.Position, (float) (m.BoundingRadius*1.5), Color.White, 5);
            }

            Spell[] spellList = {Q, E, R};
            foreach (var spell in spellList)
            {
                var menuItem = GetValue<Circle>("Draw" + spell.Slot);
                if (menuItem.Active && spell.Level > 0)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
                }
            }

            var drawEStackCount = GetValue<Circle>("DrawEStackCount");
            if (drawEStackCount.Active)
            {
                xEnemyMarker.Clear();
                foreach (var xEnemy in
                    HeroManager.Enemies.Where(
                        tx => tx.IsEnemy && !tx.IsDead && ObjectManager.Player.Distance(tx) < E.Range))
                {
                    foreach (var buff in xEnemy.Buffs.Where(buff => buff.Name.Contains("kalistaexpungemarker")))
                    {
                        xEnemyMarker.Add(
                            new EnemyMarker
                            {
                                ChampionName = xEnemy.ChampionName,
                                ExpireTime = Game.Time + 4,
                                BuffCount = buff.Count
                            });
                    }
                }

                foreach (var markedEnemies in xEnemyMarker)
                {
                    foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>())
                    {
                        if (enemy.IsEnemy && !enemy.IsDead && ObjectManager.Player.Distance(enemy) <= E.Range
                            && enemy.ChampionName == markedEnemies.ChampionName)
                        {
                            if (!(markedEnemies.ExpireTime > Game.Time))
                            {
                                continue;
                            }
                            var xCoolDown = TimeSpan.FromSeconds(markedEnemies.ExpireTime - Game.Time);
                            var display = string.Format("{0}", markedEnemies.BuffCount);
                            Utils.Utils.DrawText(
                                font,
                                display,
                                (int) enemy.HPBarPosition.X - 10,
                                (int) enemy.HPBarPosition.Y,
                                SharpDX.Color.Wheat);
                            //Drawing.DrawText(enemy.HPBarPosition.X + 145, enemy.HPBarPosition.Y + 20, drawEStackCount.Color, display);
                        }
                    }
                }
            }
            var drawJumpPos = GetValue<Circle>("DrawJumpPos");
            if (drawJumpPos.Active)
            {
                foreach (var pos in JumpPos)
                {
                    if (ObjectManager.Player.Distance(pos.Key) <= 500f
                        || ObjectManager.Player.Distance(pos.Value) <= 500f)
                    {
                        Drawing.DrawCircle(pos.Key, 75f, drawJumpPos.Color);
                        Drawing.DrawCircle(pos.Value, 75f, drawJumpPos.Color);
                    }
                    if (ObjectManager.Player.Distance(pos.Key) <= 35f || ObjectManager.Player.Distance(pos.Value) <= 35f)
                    {
                        Render.Circle.DrawCircle(pos.Key, 70f, Color.GreenYellow);
                        Render.Circle.DrawCircle(pos.Value, 70f, Color.GreenYellow);
                    }
                }
            }
        }

        public void JumpTo()
        {
            if (!Q.IsReady())
            {
                Drawing.DrawText(
                    Drawing.Width*0.44f,
                    Drawing.Height*0.80f,
                    Color.Red,
                    "Q is not ready! You can not Jump!");
                return;
            }

            Drawing.DrawText(
                Drawing.Width*0.39f,
                Drawing.Height*0.80f,
                Color.White,
                "Jumping Mode is Active! Go to the nearest jump point!");

            foreach (var xTo in from pos in JumpPos
                where
                    ObjectManager.Player.Distance(pos.Key) <= 35f
                    || ObjectManager.Player.Distance(pos.Value) <= 35f
                let xTo = pos.Value
                select
                    ObjectManager.Player.Distance(pos.Key) < ObjectManager.Player.Distance(pos.Value)
                        ? pos.Value
                        : pos.Key)
            {
                Q.Cast(new Vector2(xTo.X, xTo.Y), true);
                //Packet.C2S.Move.Encoded(new Packet.C2S.Move.Struct(xTo.X, xTo.Y)).Send();
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, xTo);
            }
        }

        private static float GetEDamage(Obj_AI_Base t)
        {
            return E.IsReady() && E.CanCast(t) ? E.GetDamage(t) : 0;
        }

        public override void Game_OnGameUpdate(EventArgs args)
        {
            SoulBoundSaver();


            foreach (var e in HeroManager.Enemies.Where(e => e.IsValidTarget(E.Range)))
            {
                foreach (var b in e.Buffs.Where(buff => buff.Name.Contains("kalistaexpungemarker")))
                {
                    if (E.IsReady() && e.Health < GetEDamage(e))
                    {
                        E.Cast();
                    }
                }

            }
            //foreach (
            //    var e in
            //        HeroManager.Enemies.Where(e => e.IsRendKillable())
            //            .Where(e => E.CanCast(e) && e.IsKillableTarget(SpellSlot.E)))
            //{
            //    E.Cast();
            //}

            //if (GetValue<KeyBind>("JumpTo").Active)
            //{
            //    JumpTo();
            //}

            foreach (var myBoddy in
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(obj => obj.Name == "RobotBuddy" && obj.IsAlly && ObjectManager.Player.Distance(obj) < 1500))
            {
                Render.Circle.DrawCircle(myBoddy.Position, 75f, Color.Red);
            }


            Obj_AI_Hero t;

            if (Q.IsReady() && GetValue<KeyBind>("UseQTH").Active)
            {
                if (ObjectManager.Player.HasBuff("Recall"))
                {
                    return;
                }

                t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget(Q.Range) && ObjectManager.Player.Mana > E.ManaCost + Q.ManaCost)
                {
                    Q.Cast(t);
                }
            }

            if (ComboActive || HarassActive)
            {
                if (Orbwalking.CanMove(100))
                {
                    if (Q.IsReady())
                    {
                        t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                        if (!t.HasKindredUltiBuff() && t.IsValidTarget(Q.Range)
                            && ObjectManager.Player.Mana > E.ManaCost + Q.ManaCost)
                        {
                            Q.Cast(t);
                        }
                    }
                }
            }
        }

        public override void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!GetValue<bool>("Misc.BlockE"))
            {
                return;
            }

            if (args.Slot == SpellSlot.E)
            {
                var minion =
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
                        .Find(m => m.Health < E.GetDamage(m) + 10 && E.CanCast(m) && E.Cooldown < 0.0001);
                var enemy =
                    HeroManager.Enemies.Find(
                        e =>
                            e.Buffs.Any(
                                b =>
                                    b.Name.ToLower() == kalistaEBuffName && e.IsValidTarget(E.Range)
                                    && e.Health < E.GetDamage(e)));
                if (enemy == null && minion == null)
                {
                    args.Process = false;
                }
            }
        }

        public override void Obj_AI_Base_OnProcessSpellCast(
            Obj_AI_Base sender,
            GameObjectProcessSpellCastEventArgs args)
        {
            //attackMinions.Clear();
            if (sender == null) return;

            if (sender.IsEnemy)
            {
                if (SoulBound != null && Program.Config.Item("SoulBoundSaver").GetValue<bool>())
                {

                    if ((!(sender is Obj_AI_Hero) || args.SData.IsAutoAttack()) && args.Target != null
                        && args.Target.NetworkId == SoulBound.NetworkId)
                    {

                        incomingDamage.Add(
                            SoulBound.ServerPosition.Distance(sender.ServerPosition)/args.SData.MissileSpeed
                            + Game.Time,
                            (float) sender.GetAutoAttackDamage(SoulBound));
                    }


                    else if (sender is Obj_AI_Hero)
                    {
                        var attacker = (Obj_AI_Hero) sender;
                        var slot = attacker.GetSpellSlot(args.SData.Name);

                        if (slot != SpellSlot.Unknown)
                        {
                            if (slot == attacker.GetSpellSlot("SummonerDot") && args.Target != null
                                && args.Target.NetworkId == SoulBound.NetworkId)
                            {

                                InstantDamage.Add(
                                    Game.Time + 2,
                                    (float) attacker.GetSummonerSpellDamage(SoulBound, Damage.SummonerSpell.Ignite));
                            }
                            else if (slot.HasFlag(SpellSlot.Q | SpellSlot.W | SpellSlot.E | SpellSlot.R)
                                     && ((args.Target != null && args.Target.NetworkId == SoulBound.NetworkId)
                                         || args.End.Distance(SoulBound.ServerPosition, true)
                                         < Math.Pow(args.SData.LineWidth, 2)))
                            {

                                InstantDamage.Add(Game.Time + 2, (float) attacker.GetSpellDamage(SoulBound, slot));
                            }
                        }
                    }
                }
            }

            if (sender.IsMe && args.SData.Name == E.Instance.Name)
            {
                Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
            }
        }

        private static void SoulBoundSaver()
        {
            if (SoulBound == null)
            {
                SoulBound =
                    HeroManager.Allies.Find(
                        h => !h.IsMe && h.Buffs.Any(b => b.Caster.IsMe && b.Name == "kalistacoopstrikeally"));
            }
            else if (Program.Config.Item("SoulBoundSaver").GetValue<bool>() && R.IsReady())
            {
                if (SoulBound.HealthPercent < 5 && SoulBound.CountEnemiesInRange(500) > 0
                    || IncomingDamage > SoulBound.Health) R.Cast();
            }

            var itemsToRemove = incomingDamage.Where(entry => entry.Key < Game.Time).ToArray();
            foreach (var item in itemsToRemove) incomingDamage.Remove(item.Key);

            itemsToRemove = InstantDamage.Where(entry => entry.Key < Game.Time).ToArray();
            foreach (var item in itemsToRemove) InstantDamage.Remove(item.Key);
        }

        public override bool ComboMenu(Menu config)
        {
            config.AddItem(new MenuItem("Combo.UseQ" + Id, "Use Q").SetValue(true));
            config.AddItem(new MenuItem("SoulBoundSaver", "Auto SoulBound Saver").SetValue(true));
            return true;
        }

        public override bool HarassMenu(Menu config)
        {
            config.AddItem(new MenuItem("UseQH" + Id, "Use Q").SetValue(true));
            config.AddItem(
                new MenuItem("UseQTH" + Id, "Use Q (Toggle)").SetValue(
                    new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            return true;
        }

        public override bool MiscMenu(Menu config)
        {
            config.AddItem(new MenuItem("Misc.BlockE" + Id, "Block E if can not kill anything").SetValue(false));
            config.AddItem(new MenuItem("Misc.UseSlowE" + Id, "Use E for slow if it possible").SetValue(true));
            //config.AddItem(new MenuItem("JumpTo" + Id, "JumpTo").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

            return true;
        }

        public override bool DrawingMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("DrawQ" + Id, "Q range").SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            config.AddItem(
                new MenuItem("DrawE" + Id, "E range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawR" + Id, "R range").SetValue(new Circle(false, Color.FromArgb(100, 255, 255, 255))));
            config.AddItem(
                new MenuItem("DrawEStackCount" + Id, "E Stack Count").SetValue(new Circle(true, Color.White)));
            config.AddItem(
                new MenuItem("DrawJumpPos" + Id, "Jump Positions").SetValue(new Circle(false, Color.HotPink)));

            var damageAfterE = new MenuItem("DamageAfterE", "Damage After E").SetValue(true);
            config.AddItem(damageAfterE);

            Utility.HpBarDamageIndicator.DamageToUnit = GetEDamage;
            Utility.HpBarDamageIndicator.Enabled = damageAfterE.GetValue<bool>();
            damageAfterE.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            return true;
        }

        public override bool LaneClearMenu(Menu config)
        {

            string[] srtQ = new string[6];
            srtQ[0] = "Off";

            for (var i = 1; i < 6; i++)
            {
                srtQ[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseQ.Lane" + Id, "Use Q:").SetValue(new StringList(srtQ, 0)));
            config.AddItem(
                new MenuItem("UseQ.Mode.Lane" + Id, "Use Q Mode:").SetValue(
                    new StringList(new[] {"Everytime", "Just Out of AA Range"}, 1)));

            string[] strW = new string[6];
            strW[0] = "Off";

            for (var i = 1; i < 6; i++)
            {
                strW[i] = "Minion Count >= " + i;
            }

            config.AddItem(new MenuItem("UseE.Lane" + Id, "Use E:").SetValue(new StringList(strW, 0)));
            config.AddItem(new MenuItem("UseE.LaneNon" + Id, "Use E for Non Killable Minions:").SetValue(true));
            config.AddItem(
                new MenuItem("UseE.Prepare.Lane" + Id, "Prepare Minions for E Farm").SetValue(
                    new StringList(new[] {"Off", "On", "Just Under Ally Turret"}, 2)));


            return true;
        }

        public override bool JungleClearMenu(Menu config)
        {
            config.AddItem(
                new MenuItem("UseQJ" + Id, "Use Q").SetValue(
                    new StringList(new[] {"Off", "On", "Just big Monsters"}, 1)));
            config.AddItem(
                new MenuItem("UseEJ" + Id, "Use E").SetValue(
                    new StringList(new[] {"Off", "On", "Just big Monsters"}, 1)));
            return true;
        }

        private static List<Obj_AI_Base> qGetCollisionMinions(Obj_AI_Hero source, Vector3 targetposition)
        {
            var input = new PredictionInput {Unit = source, Radius = Q.Width, Delay = Q.Delay, Speed = Q.Speed,};

            input.CollisionObjects[0] = CollisionableObjects.Minions;

            return
                Collision.GetCollision(new List<Vector3> {targetposition}, input)
                    .OrderBy(obj => obj.Distance(source, false))
                    .ToList();
        }

        public override void ExecuteJungleClear()
        {
            if (Q.IsReady())
            {
                //var jungleMobs = GetValue<StringList>("UseEJ").SelectedIndex == 1
                //    ? Utils.Utils.GetMobs(Q.Range)
                //    : Utils.Utils.GetMobs(Q.Range, Utils.Utils.MobTypes.BigBoys);

                var jungleMobs = Utils.Utils.GetMobs(
                    Q.Range,
                    GetValue<StringList>("UseQJ").SelectedIndex == 1
                        ? Utils.Utils.MobTypes.All
                        : Utils.Utils.MobTypes.BigBoys);

                if (jungleMobs != null && ObjectManager.Player.Mana > E.ManaCost + Q.ManaCost) Q.Cast(jungleMobs);
            }

            if (E.IsReady())
            {
                var jungleMobs = Utils.Utils.GetMobs(
                    E.Range,
                    GetValue<StringList>("UseEJ").SelectedIndex == 1
                        ? Utils.Utils.MobTypes.All
                        : Utils.Utils.MobTypes.BigBoys);

                if (jungleMobs != null && E.CanCast(jungleMobs) && jungleMobs.Health < E.GetDamage(jungleMobs))
                    E.CastOnUnit(jungleMobs);
            }
        }

        public override void ExecuteLaneClear()
        {
            var prepareMinions = GetValue<StringList>("UseE.Prepare.Lane").SelectedIndex;
            if (prepareMinions != 0)
            {
                List<Obj_AI_Minion> list = new List<Obj_AI_Minion>();
                IEnumerable<Obj_AI_Minion> minions =
                    from m in
                        ObjectManager.Get<Obj_AI_Minion>()
                            .Where(
                                m =>
                                    m.Health > ObjectManager.Player.TotalAttackDamage
                                    && m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
                    select m;
                if (prepareMinions == 2)
                {
                    minions = minions.Where(m => m.IsUnderAllyTurret());
                }

                var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
                foreach (var m in objAiMinions)
                {
                    if (m.GetBuffCount(kalistaEBuffName) >= 0)
                    {
                        Render.Circle.DrawCircle(m.Position, 105f, Color.Blue);
                        list.Add(m);
                    }
                    else
                    {
                        list.Remove(m);
                    }
                }
                var enemy = HeroManager.Enemies.Find(e => e.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65));
                if (enemy == null)
                {
                    foreach (var l in objAiMinions.Except(list).ToList())
                    {
                        Program.CClass.Orbwalker.ForceTarget(l);
                    }
                }
                else
                {
                    Program.CClass.Orbwalker.ForceTarget(enemy);
                }
            }

            if (Q.IsReady())
            {
                var qCount = GetValue<StringList>("UseQ.Lane").SelectedIndex;
                if (qCount != 0)
                {
                    var minions = MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        Q.Range,
                        MinionTypes.All,
                        MinionTeam.Enemy);

                    foreach (var minion in minions.Where(x => x.Health <= Q.GetDamage(x)))
                    {
                        var killableMinionCount = 0;
                        foreach (
                            var colminion in
                                qGetCollisionMinions(
                                    ObjectManager.Player,
                                    ObjectManager.Player.ServerPosition.Extend(minion.ServerPosition, Q.Range)))
                        {
                            if (colminion.Health <= Q.GetDamage(colminion))
                            {
                                if (GetValue<StringList>("UseQ.Mode.Lane").SelectedIndex == 1
                                    && colminion.Distance(ObjectManager.Player)
                                    > Orbwalking.GetRealAutoAttackRange(null) + 65)
                                {
                                    killableMinionCount++;
                                }
                                else
                                {
                                    killableMinionCount++;
                                }
                            }
                            else break;
                        }

                        if (killableMinionCount >= qCount)
                        {
                            if (!ObjectManager.Player.IsWindingUp && !ObjectManager.Player.IsDashing())
                            {
                                Q.Cast(minion.ServerPosition);
                                break;
                            }
                        }
                    }
                }
            }

            if (E.IsReady())
            {
                var minECount = GetValue<StringList>("UseE.Lane").SelectedIndex;
                if (minECount != 0)
                {
                    var killableMinionCount = 0;
                    foreach (var m in
                        MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
                            .Where(x => E.CanCast(x) && x.Health < E.GetDamage(x)))
                    {
                        if (m.SkinName.ToLower().Contains("siege") || m.SkinName.ToLower().Contains("super"))
                        {
                            killableMinionCount += 2;
                        }
                        else
                        {
                            killableMinionCount++;
                        }
                    }

                    if (killableMinionCount >= minECount && E.IsReady()
                        && ObjectManager.Player.ManaPercent > E.ManaCost*2)
                    {
                        E.Cast();
                    }
                }
            }

            // Don't miss minion
            if (GetValue<bool>("UseE.LaneNon"))
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range*1);

                foreach (var n in minions)
                {
                    var xH = HealthPrediction.GetHealthPrediction(n, (int) (ObjectManager.Player.AttackCastDelay*1000),
                        Game.Ping/2 + 100);
                    if (xH < 0)
                    {
                        if (n.Health < E.GetDamage(n) && E.CanCast(n))
                        {
                            E.Cast(n);
                        }
                        else if (Q.IsReady() && Q.CanCast(n) &&
                                 n.Distance(ObjectManager.Player.Position) < Orbwalking.GetRealAutoAttackRange(null) + 75)
                        {
                            xH = HealthPrediction.GetHealthPrediction(n,
                                (int) (ObjectManager.Player.AttackCastDelay*1000), (int) Q.Speed);
                            if (xH < 0)
                            {
                                var input = new PredictionInput
                                {
                                    Unit = ObjectManager.Player,
                                    Radius = Q.Width,
                                    Delay = Q.Delay,
                                    Speed = Q.Speed,
                                };

                                input.CollisionObjects[0] = CollisionableObjects.Minions;

                                int count =
                                    Collision.GetCollision(new List<Vector3> {n.Position}, input)
                                        .OrderBy(obj => obj.Distance(ObjectManager.Player))
                                        .Count(obj => obj.NetworkId != n.NetworkId);
                                if (count == 0)
                                {
                                    Q.Cast(n);
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void PermaActive()
        {
            if (GetValue<bool>("Misc.UseSlowE"))
            {
                var minion =
                    MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
                        .Find(m => m.Health < E.GetDamage(m) + 10 && E.CanCast(m) && E.Cooldown < 0.0001);
                var enemy =
                    HeroManager.Enemies.Find(
                        e => e.Buffs.Any(b => b.Name.ToLower() == kalistaEBuffName && e.IsValidTarget(E.Range)));
                if ((E.CanCast(enemy) || E.CanCast(minion)) && minion != null && enemy != null
                    && ObjectManager.Player.ManaPercent > E.ManaCost*2)
                {
                    E.Cast();
                }
            }

            //if (Orbwalking.LastAATick + (ObjectManager.Player.AttackCastDelay * 1000) > LeagueSharp.Common.Utils.GameTimeTickCount)

            //if (Marksman.Utils.Orbwalking.LastAATick + (ObjectManager.Player.AttackCastDelay * 1000) > LeagueSharp.Common.Utils.GameTimeTickCount)

            //{
            //    var mm = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);

            //    foreach (var n in mm)
            //    {
            //        var xH = HealthPrediction.GetHealthPrediction(n, (int)(ObjectManager.Player.AttackCastDelay * 1000));
            //        Drawing.DrawText(n.Position.X, n.Position.Y, Color.Red, xH.ToString());
            //    }

            //    var nM =
            //        MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range)
            //            .Find(
            //                m =>
            //                m.Health < E.GetDamage(m) + 7 && E.CanCast(m)
            //                && m.Health < ObjectManager.Player.TotalAttackDamage);
            //    if (nM != null && E.CanCast(nM))
            //    {
            //        E.Cast(nM);
            //        return;
            //    }
            //}

            //if (E.IsReady() && E.CanCast(minion) && minion != null && minion.IsUnderAllyTurret())
            //{
            //    E.Cast();
            //}
        }
    }

    public static class Damages
    {

        private static readonly float[] RawRendDamage = {20, 30, 40, 50, 60};

        private static readonly float[] RawRendDamageMultiplier = {0.6f, 0.6f, 0.6f, 0.6f, 0.6f};

        private static readonly float[] RawRendDamagePerSpear = {10, 14, 19, 25, 32};

        private static readonly float[] RawRendDamagePerSpearMultiplier = {0.2f, 0.225f, 0.25f, 0.275f, 0.3f};

        static Damages()
        {

        }

        public static bool IsRendKillable(this Obj_AI_Base target)
        {
            // Validate unit
            if (target == null || !target.IsValidTarget() || !target.HasRendBuff())
            {
                return false;
            }

            // Take into account all kinds of shields
            var totalHealth = target.TotalShieldHealth();

            var hero = target as Obj_AI_Hero;
            if (hero != null)
            {
                if (hero.HasUndyingBuff() || hero.HasSpellShield()
                    || (hero.HasKindredUltiBuff() && hero.HealthPercent <= 10))
                {
                    return false;
                }

                if (hero.ChampionName == "Blitzcrank" && !target.HasBuff("BlitzcrankManaBarrierCD")
                    && !target.HasBuff("ManaBarrier"))
                {
                    totalHealth += target.Mana/2;
                }
            }

            return GetRendDamage(target) > totalHealth;
        }

        public static float GetRendDamage(Obj_AI_Hero target)
        {
            return Kalista.E.GetDamage(target);
        }

        public static float GetRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            return
                (float)
                    (ObjectManager.Player.CalcDamage(
                        target,
                        Damage.DamageType.Magical,
                        GetRawRendDamage(target, customStacks))
                     *(ObjectManager.Player.HasBuff("SummonerExhaustSlow") ? 0.6f : 1));
        }

        public static float GetRawRendDamage(Obj_AI_Base target, int customStacks = -1)
        {
            var stacks = (customStacks > -1 ? customStacks : target.HasRendBuff() ? target.GetRendBuff().Count : 0) - 1;
            if (stacks > -1)
            {
                var index = Kalista.E.Level - 1;
                return RawRendDamage[index] + stacks*RawRendDamagePerSpear[index]
                       + ObjectManager.Player.TotalAttackDamage
                       *(RawRendDamageMultiplier[index] + stacks*RawRendDamagePerSpearMultiplier[index]);
            }

            return 0;
        }

        public static bool HasRendBuff(this Obj_AI_Base target)
        {
            return target.GetRendBuff() != null;
        }

        public static BuffInstance GetRendBuff(this Obj_AI_Base target)
        {
            return target.Buffs.Find(b => b.Caster.IsMe && b.IsValid && b.DisplayName == "KalistaExpungeMarker");
        }
    }
}
