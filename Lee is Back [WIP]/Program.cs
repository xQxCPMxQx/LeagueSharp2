#region

using System;
using System.Collections.Generic;
using System.Linq;
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
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;
        public static Menu TargetSelectorMenu;
        private static Obj_AI_Hero _player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        public static int WaittingForWard;
        public static Vector3 WardCastPosition;
        private static Vector3 insdirec;
        private static Vector3 insecpos;
        private static int wardcount;
        private static int blop;
        private static float wardtime;
        private static float inscount;
 


        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                _player = ObjectManager.Player;
                if (ObjectManager.Player.BaseSkinName != ChampionName) return;
                _q = new Spell(SpellSlot.Q, 1100f);
                _w = new Spell(SpellSlot.W, 700f);
                _e = new Spell(SpellSlot.E, 350f);
                _r = new Spell(SpellSlot.R, 375f);

                _q.SetSkillshot(0.25f, 65f, 1800f, true, SkillshotType.SkillshotLine);

                _bilge = new Items.Item(3144, 475f);
                _blade = new Items.Item(3153, 425f);
                _hydra = new Items.Item(3074, 250f);
                _tiamat = new Items.Item(3077, 250f);
                _rand = new Items.Item(3143, 490f);
                _lotis = new Items.Item(3190, 590f);
                _youmuu = new Items.Item(3142, 10);
                _igniteSlot = _player.GetSpellSlot("SummonerDot");

                var enemy = from hero in ObjectManager.Get<Obj_AI_Hero>()
                            where hero.IsEnemy == true
                            select hero;

                _config = new Menu("Lee Is Back", "Lee Is Back", true);

                TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
                TargetSelector.AddToMenu(TargetSelectorMenu);
                _config.AddSubMenu(TargetSelectorMenu);

                _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));


                _config.AddSubMenu(new Menu("Combo", "Combo"));
                _config.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

                _config.AddSubMenu(new Menu("Harass", "Harass"));
                _config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

                _config.AddSubMenu(new Menu("items", "items"));
                _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Tiamat", "Use Tiamat")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hydra", "Use Hydra")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omen", "Use Randuin Omen"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omenenemys", "Randuin if enemys>").SetValue(new Slider(2, 1, 5)));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotis", "Use Iron Solari"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1, 100)));

                //Farm
                _config.AddSubMenu(new Menu("Farm", "Farm"));
                _config.SubMenu("Farm").AddSubMenu(new Menu("LaneFarm", "LaneFarm"));
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("UseItemslane", "Use Hydra/Tiamat"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseEL", "E LaneClear")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("Energylane", "Energy Lane% >").SetValue(new Slider(45, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(
                        new MenuItem("Activelane", "Lane clear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                _config.SubMenu("Farm").AddSubMenu(new Menu("LastHit", "LastHit"));
                _config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseELH", "E LastHit")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(new MenuItem("Energylast", "Energy lasthit% >").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(
                        new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

                _config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("UseItemsjungle", "Use Hydra/Tiamat"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseWJ", "W Jungle")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseEJ", "E Jungle")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("Energyjungle", "Energy Jungle% >").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(
                        new MenuItem("Activejungle", "Jungle!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                //Misc
                _config.AddSubMenu(new Menu("Misc", "Misc"));
                _config.SubMenu("Misc").AddItem(new MenuItem("UseIgnitekill", "Use Ignite KillSteal")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("UseQM", "Use Q KillSteal")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("UseEM", "Use E KillSteal")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("AutoE", "Auto E")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("wjump", "ward jump")).SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press));
                _config.SubMenu("Misc").AddItem(new MenuItem("insc", "insec")).SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press));
                _config.SubMenu("Misc").AddItem(new MenuItem("", ""));



                //Drawings
                _config.AddSubMenu(new Menu("Drawings", "Drawings"));
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQW", "Draw long harras")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("shadowd", "Shadow Position")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
                _config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
                _config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));
                _config.AddToMainMenu();
                new AssassinManager();
                new DamageIndicator();

                DamageIndicator.DamageToUnit = ComboDamage;

                Drawing.OnDraw += Drawing_OnDraw;
                Game.OnUpdate += Game_OnUpdate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
                Game.OnWndProc += OnWndProc;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintChat("Error something went wrong");
            }
        }
   
                    
        private static void Game_OnUpdate(EventArgs args)
        {
            if(Environment.TickCount>wardtime+5000)
            {
                wardcount = 0;
            }
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {          
                Combo(GetEnemy);
                
            }
            if (_config.Item("wjump").GetValue<KeyBind>().Active)
            {
                WardJump(Game.CursorPos, true, true);
            }
            if (_config.Item("ActiveHarass").GetValue<KeyBind>().Active)
            {
                Harass(GetEnemy);

            }
            if (_config.Item("insc").GetValue<KeyBind>().Active)
            {
                Insec(GetEnemy);

            }






            _player = ObjectManager.Player;


 

        }
        private static void OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 514)
            {
                insdirec = Game.CursorPos;

            }
        }

        private static void OnProcessSpell (Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsAlly || !sender.Type.Equals(GameObjectType.obj_AI_Hero) ||
                (((Obj_AI_Hero)sender).ChampionName != "MonkeyKing" && ((Obj_AI_Hero)sender).ChampionName != "Akali") ||
                sender.Position.Distance(_player.ServerPosition) >= 350  ||
                !_e.IsReady())
            {
                return;
            }
            if (args.SData.Name == "MonkeyKingDecoy" || args.SData.Name == "AkaliSmokeBomb")
            {
                _e.Cast();
            }
        }
        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3077) && Items.CanUseItem(3077))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Botrk);
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Bilgewater);
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_w.IsReady() && _q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q) / 2;
            if (_e.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);

            return (float)damage;
        }

        private static void Combo(Obj_AI_Hero t)
        {
            var target = t;


                if (target != null && _config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                        _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (ComboDamage(target) > target.Health || target.HasBuff("zedulttargetmark", true))
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                }


            
            
        }
        private static void Harass(Obj_AI_Hero t)
        {
            var target = t;

            var useItemsH = _config.Item("UseItemsharass").GetValue<bool>();

            if (useItemsH && _tiamat.IsReady() && target.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }

        }

        public static void WardJump(Vector3 pos, bool useWard = true, bool checkObjects = true, bool fullRange = false)
        {
            if (WStage!= WCastStage.First)
            {
                return;
            }
            pos = fullRange ? _player.ServerPosition.To2D().Extend(pos.To2D(), 600).To3D() : pos;
            WardCastPosition = NavMesh.GetCollisionFlags(pos).HasFlag(CollisionFlags.Wall)
                ? _player.GetPath(pos).Last()
                : pos;
            var jumpObject =
                ObjectManager.Get<Obj_AI_Base>()
                    .OrderBy(obj => obj.Distance(_player.ServerPosition))
                    .FirstOrDefault(
                        obj =>
                            obj.IsAlly && !obj.IsMe &&
                            (!(obj.Name.IndexOf("turret", StringComparison.InvariantCultureIgnoreCase) >= 0) &&
                             Vector3.DistanceSquared(pos, obj.ServerPosition) <= 150 * 150));
            if (jumpObject != null && checkObjects)
            {
                _w.CastOnUnit(jumpObject);
                return;
            }
            if (!useWard)
            {
                return;
            }

            if (Items.GetWardSlot() == null || Items.GetWardSlot().Stacks == 0)
            {
                return;
            }
            placeward(WardCastPosition);
        }
         private static void placeward(Vector3 castpos)
        {
            if (WStage!=WCastStage.First || wardcount!=0)
            {
                return;
            }
            var ward = Items.GetWardSlot();
            _player.Spellbook.CastSpell(ward.SpellSlot, castpos);
            wardtime = Environment.TickCount;
            wardcount = 1;
        }

        private static void Insec (Obj_AI_Hero t)
         {


            insecpos = t.ServerPosition.Extend(insdirec, -250);
            if ((_player.ServerPosition.Distance(insecpos) > 600 || inscount + 2000 > Environment.TickCount) && t != null && t.IsValidTarget() && QStage == QCastStage.First)
            {
                var qpred = _q.GetPrediction(t);
                _q.Cast(t);
                if (qpred.Hitchance == HitChance.Collision)
                {
                    var enemyqtry = ObjectManager.Get<Obj_AI_Base>().Where(enemyq => (enemyq.IsValidTarget()||(enemyq.IsMinion&&enemyq.IsEnemy)) && enemyq.Distance(insecpos)<500);
                    foreach( 
                        var enemyhit in enemyqtry.OrderBy(enemyhit=>enemyhit.Distance(insecpos)))
                        {

                            if (_q.GetPrediction(enemyhit).Hitchance >= HitChance.Medium)
                            _q.Cast(enemyhit);
                    }
                }
            }
            if (QStage == QCastStage.Second)
            {
                var enemy = ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(unit => unit.IsEnemy && (unit.HasBuff("BlindMonkQOne", true) || unit.HasBuff("blindmonkqonechaos", true)));
                if (enemy.Position.Distance(insecpos) < 550)
                {
                    _q.Cast();
                }
            }
            
            if (_player.Position.Distance(insecpos) < 600 )
                WardJump(insecpos,true,true,false);
            if (t.ServerPosition.Distance(insdirec) < _player.Position.Distance(insdirec) && !_w.IsReady())
            {
                _r.CastOnUnit(t);
                inscount = Environment.TickCount;
            }

        }
       
        static Obj_AI_Hero GetEnemy
        {
            get
            {
                var assassinRange = TargetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;

                var vEnemy = ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null &&
                            TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy.ServerPosition) < assassinRange);

                if (TargetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
                {
                    vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
                }

                Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

                Obj_AI_Hero t = !objAiHeroes.Any()
                    ? TargetSelector.GetTarget(1400, TargetSelector.DamageType.Magical)
                    : objAiHeroes[0];

                return t;

            }

        }

        public static Obj_AI_Base Marked
        {
            get { return ObjectManager.Get<Obj_AI_Base>().FirstOrDefault(unit => unit.IsEnemy && (unit.HasBuff("BlindMonkQOne", true) || unit.HasBuff("blindmonkqonechaos", true))); }
        }

        private static QCastStage QStage
        {
            get
            {
                if (!_q.IsReady()) return QCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Name == "BlindMonkQOne"
                    ? QCastStage.First
                    : QCastStage.Second);

            }
        }
        private static WCastStage  WStage
        {
            get
            {
                if (!_w.IsReady()) return WCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "blindmonkwtwo"
                    ? WCastStage.Second
                    : WCastStage.First);

            }
        }


        internal enum QCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum WCastStage
        {
            First,
            Second,
            Cooldown
        }
        private static void Drawing_OnDraw(EventArgs args)
        
        {
            Render.Circle.DrawCircle(insecpos, 100, System.Drawing.Color.Blue);
            Render.Circle.DrawCircle(insdirec, 100, System.Drawing.Color.Blue);



           
            if (_config.Item("damagetest").GetValue<bool>())
            {
                foreach (
                    var enemyVisible in
                        ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Rekt");
                    }
                    else if (ComboDamage(enemyVisible) + _player.GetAutoAttackDamage(enemyVisible, true) * 2 >
                             enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo + 2 AA = Rekt");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable with combo + 2AA");
                }
            }


            if (_config.Item("CircleLag").GetValue<bool>())
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Blue);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawQW").GetValue<bool>() && _config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.Yellow);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Blue);
                }
            }
            else
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawQW").GetValue<bool>() && _config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.White);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }
            }
        }


    }
}