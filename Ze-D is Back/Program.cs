#region
/*
* Credits to:
 * Trees (Damage indicator)
 * Kurisu (ult on dangerous)
 * xQx assasin target selector
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using zedisback.Common;
using Color = System.Drawing.Color;

#endregion

namespace Zed
{
    class Program
    {
        private const string ChampionName = "Zed";
        private static List<Spell> SpellList = new List<Spell>();
        public static Spell Q, _w, E, R;
        private static Orbwalking.Orbwalker _orbwalker;
        public static Menu Config;
        private static Obj_AI_Hero _player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        private static Vector3 linepos;
        private static int clockon;
        private static int countults;
        private static int countdanger;
        private static int ticktock;
        private static Vector3 rpos;
        private static int shadowdelay = 0;
        private static int delayw = 500;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                _player = ObjectManager.Player;
                if (ObjectManager.Player.CharData.BaseSkinName != ChampionName) return;
                Q = new Spell(SpellSlot.Q, 900f);
                _w = new Spell(SpellSlot.W, 700f);
                E = new Spell(SpellSlot.E, 270f);
                R = new Spell(SpellSlot.R, 650f);

                Q.SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);

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
                // Just menu things test
                Config = new Menu("Zed Is Back", "Zed Is Back", true);

                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
                _orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

                CommonTargetSelector.Init(Config);
                //Combo
                Config.AddSubMenu(new Menu("Combo", "Combo"));
                Config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W (also gap close)")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite(rush for it)")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("UseUlt", "Use Ultimate")).SetValue(true);
                Config.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
                Config.SubMenu("Combo")
                    .AddItem(new MenuItem("TheLine", "The Line Combo").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));

                //Harass
                Config.AddSubMenu(new Menu("Harass", "Harass"));
                Config.SubMenu("Harass").AddItem(new MenuItem("longhar", "Long Poke (toggle)").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
                Config.SubMenu("Harass").AddItem(new MenuItem("UseItemsharass", "Use Tiamat/Hydra")).SetValue(true);
                Config.SubMenu("Harass").AddItem(new MenuItem("UseWH", "Use W")).SetValue(true);
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

                //items
                Config.AddSubMenu(new Menu("items", "items"));
                Config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
                Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
                Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Tiamat", "Use Tiamat")).SetValue(true);
                Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hydra", "Use Hydra")).SetValue(true);
                Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
                Config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
                Config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
                Config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omen", "Use Randuin Omen"))
                    .SetValue(true);
                Config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omenenemys", "Randuin if enemys>").SetValue(new Slider(2, 1, 5)));
                Config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotis", "Use Iron Solari"))
                    .SetValue(true);
                Config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1, 100)));

                //Farm
                Config.AddSubMenu(new Menu("Farm", "Farm"));
                Config.SubMenu("Farm").AddSubMenu(new Menu("LaneFarm", "LaneFarm"));
                Config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("UseItemslane", "Use Hydra/Tiamat"))
                    .SetValue(true);
                Config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
                Config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseEL", "E LaneClear")).SetValue(true);
                Config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("Energylane", "Energy Lane% >").SetValue(new Slider(45, 1, 100)));
                Config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(
                        new MenuItem("Activelane", "Lane clear!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                Config.SubMenu("Farm").AddSubMenu(new Menu("LastHit", "LastHit"));
                Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
                Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseELH", "E LastHit")).SetValue(true);
                Config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(new MenuItem("Energylast", "Energy lasthit% >").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(
                        new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

                Config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
                Config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("UseItemsjungle", "Use Hydra/Tiamat"))
                    .SetValue(true);
                Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
                Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseWJ", "W Jungle")).SetValue(true);
                Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseEJ", "E Jungle")).SetValue(true);
                Config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("Energyjungle", "Energy Jungle% >").SetValue(new Slider(85, 1, 100)));
                Config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(
                        new MenuItem("Activejungle", "Jungle!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                //Misc
                Config.AddSubMenu(new Menu("Misc", "Misc"));
                Config.SubMenu("Misc").AddItem(new MenuItem("UseIgnitekill", "Use Ignite KillSteal")).SetValue(true);
                Config.SubMenu("Misc").AddItem(new MenuItem("UseQM", "Use Q KillSteal")).SetValue(true);
                Config.SubMenu("Misc").AddItem(new MenuItem("UseEM", "Use E KillSteal")).SetValue(true);
                Config.SubMenu("Misc").AddItem(new MenuItem("AutoE", "Auto E")).SetValue(true);
                Config.SubMenu("Misc").AddItem(new MenuItem("rdodge", "R Dodge Dangerous")).SetValue(true);
                Config.SubMenu("Misc").AddItem(new MenuItem("", ""));
                foreach (var e in enemy)
                {
                    SpellDataInst rdata = e.Spellbook.GetSpell(SpellSlot.R);
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(rdata.SData.Name)))
                        Config.SubMenu("Misc").AddItem(new MenuItem("ds" + e.SkinName, rdata.SData.Name)).SetValue(true);
                }


                //Drawings
                Config.AddSubMenu(new Menu("Drawings", "Drawings"));
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQW", "Draw long harras")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("DrawHP", "Draw HP bar")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("shadowd", "Shadow Position")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
                Config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));
                Config.AddToMainMenu();
                new DamageIndicator();

                DamageIndicator.DamageToUnit = ComboDamage;
                Game.PrintChat("<font color='#881df2'>Zed is Back by jackisback</font> Loaded.");
                Game.PrintChat("<font color='#f2881d'>if you wanna help me to pay my internet bills^^ paypal= bulut@live.co.uk</font>");

                Drawing.OnDraw += Drawing_OnDraw;
                Game.OnUpdate += Game_OnUpdate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintChat("Error something went wrong");
            }



        }

        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs castedSpell)
        {
            if (unit.Type != GameObjectType.obj_AI_Hero)
            {
                return;
            }

            if (unit.IsMe)
            {
                return;
            }

            if (unit.IsEnemy)
            {
                if (Config.Item("ds" + unit.SkinName) != null 
                    && R.IsReady() && Config.Item("rdodge").GetValue<bool>() 
                    && UltStage == UltCastStage.First 
                    && Config.Item("ds" + unit.SkinName).GetValue<bool>())
                {
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(castedSpell.SData.Name)) &&
                        (unit.Distance(_player.ServerPosition) < 650f || _player.Distance(castedSpell.End) <= 250f))
                    {
                        if (castedSpell.SData.Name == "SyndraR")
                        {
                            clockon = Environment.TickCount + 150;
                            countdanger = countdanger + 1;
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(640, TargetSelector.DamageType.Physical);
                            R.Cast(target);
                        }
                    }
                }
            }

            if (unit.IsMe && castedSpell.SData.Name == "zedult")
            {
                ticktock = Environment.TickCount + 200;

            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {          
                Combo();
                
            }
            if (Config.Item("TheLine").GetValue<KeyBind>().Active)
            {
                TheLine();
            }
            if (Config.Item("ActiveHarass").GetValue<KeyBind>().Active)
            {
                Harass();

            }
            if (Config.Item("Activelane").GetValue<KeyBind>().Active)
            {
                Laneclear();
            }
            if (Config.Item("Activejungle").GetValue<KeyBind>().Active)
            {
                JungleClear();
            }
            if (Config.Item("ActiveLast").GetValue<KeyBind>().Active)
            {
                LastHit();
            }
            if (Config.Item("AutoE").GetValue<bool>())
            {
                CastE();
            }

            if (Environment.TickCount >= clockon && countdanger > countults)
            {
                R.Cast(TargetSelector.GetTarget(640, TargetSelector.DamageType.Physical));
                countults = countults + 1;
            }


            //if (LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R)
            //{
                Obj_AI_Minion shadow;
                shadow = ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");

                if (shadow != null)
                {
                    rpos = shadow.ServerPosition;
                }
            //}


            _player = ObjectManager.Player;


            KillSteal();

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
            if (Q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_w.IsReady() && Q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q)/2;
            if (E.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (R.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);
            damage += (R.Level*0.15 + 0.05)*
                      (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite));

            return (float)damage;
        }

        private static void Combo()
        {
            var target = CommonTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget())
            {
                return;
            }

            var overkill= Q.GetDamage(target) + E.GetDamage(target) +_player.GetAutoAttackDamage(target, true) * 2;
            var doubleu = _player.Spellbook.GetSpell(SpellSlot.W);


            if (Config.Item("UseUlt").GetValue<bool>() && UltStage == UltCastStage.First && (overkill < target.Health ||
                (!_w.IsReady()&& doubleu.Cooldown>2f && _player.GetSpellDamage(target, SpellSlot.Q) < target.Health && target.Distance(_player.Position) > 400)))
            {
                if ((target.Distance(_player.Position) > 700 && target.MoveSpeed > _player.MoveSpeed) || target.Distance(_player.Position) > 800)
                {
                    CastW(target);
                    _w.Cast();
                    
                }
                R.Cast(target);
            }

            else
            {
                if (target != null && Config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                        _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (ComboDamage(target) > target.Health || target.HasBuff("zedulttargetmark"))
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                }
                if (target != null && ShadowStage == ShadowCastStage.First && Config.Item("UseWC").GetValue<bool>() &&
                        target.Distance(_player.Position) > 400 && target.Distance(_player.Position) < 1300)
                {
                    CastW(target);
                }
                if (WShadow != null && target != null && ShadowStage == ShadowCastStage.Second && Config.Item("UseWC").GetValue<bool>() &&
                    target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position))
                {
                    _w.Cast();
                }


                UseItemes(target);
                CastE();
                CastQ(target);

            }
            
            
        }

        private static void TheLine()
        {
            var target = CommonTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            if (target == null)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                _player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if ( !R.IsReady() || target.Distance(_player.Position) >= 640)
            {
                return;
            }
            if (UltStage == UltCastStage.First)  
                R.Cast(target);
            linepos = target.Position.Extend(_player.ServerPosition, -500);

            if (target != null && ShadowStage == ShadowCastStage.First &&  UltStage == UltCastStage.Second)
            {
                UseItemes(target);
                
                if (LastCastedSpell.LastCastPacketSent.Slot != SpellSlot.W)
                {
                    _w.Cast(linepos);
                    CastE();
                    CastQ(target);
                    
                    
                    if (target != null && Config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                            _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                
                }
            }

            if (target != null && WShadow != null && UltStage == UltCastStage.Second && target.Distance(_player.Position) > 250 && (target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position)))
            {
                _w.Cast();
            }

        }

        private static void _CastQ(Obj_AI_Hero target)
        {
            throw new NotImplementedException();
        }

        private static void Harass()
        {
            var target = CommonTargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Physical);

            var useItemsH = Config.Item("UseItemsharass").GetValue<bool>();

            if (target.IsValidTarget() && Config.Item("longhar").GetValue<KeyBind>().Active && _w.IsReady() && Q.IsReady() && ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost && target.Distance(_player.Position) > 850 &&
                target.Distance(_player.Position) < 1400)
            {
                CastW(target);
            }

            if (target.IsValidTarget() && (ShadowStage == ShadowCastStage.Second || ShadowStage == ShadowCastStage.Cooldown || !(Config.Item("UseWH").GetValue<bool>()))
                            && Q.IsReady() &&
                                (target.Distance(_player.Position) <= 900 || target.Distance(WShadow.ServerPosition) <= 900))
            {
                CastQ(target);
            }

            if (target.IsValidTarget() && _w.IsReady() && Q.IsReady() && Config.Item("UseWH").GetValue<bool>() &&
                ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost )
            {
                if (target.Distance(_player.Position) < 750)

                CastW(target);
            }
            
            CastE();
         
            if (useItemsH && _tiamat.IsReady() && target.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }
            
        }

        private static void Laneclear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);
            var mymana = (_player.Mana >= (_player.MaxMana*Config.Item("Energylane").GetValue<Slider>().Value)/100);

            var useItemsl = Config.Item("UseItemslane").GetValue<bool>();
            var useQl = Config.Item("UseQL").GetValue<bool>();
            var useEl = Config.Item("UseEL").GetValue<bool>();
            if (Q.IsReady() && useQl && mymana)
            {
                var fl2 = Q.GetLineFarmLocation(allMinionsQ, Q.Width);

                if (fl2.MinionsHit >= 3)
                {
                    Q.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                            Q.Cast(minion);
            }

            if (E.IsReady() && useEl && mymana)
            {
                if (allMinionsE.Count > 2)
                {
                    E.Cast();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                            E.Cast();
            }

            if (useItemsl && _tiamat.IsReady() && allMinionsE.Count > 2)
            {
                _tiamat.Cast();
            }
            if (useItemsl && _hydra.IsReady() && allMinionsE.Count > 2)
            {
                _hydra.Cast();
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * Config.Item("Energylast").GetValue<Slider>().Value) / 100);
            var useQ = Config.Item("UseQLH").GetValue<bool>();
            var useE = Config.Item("UseELH").GetValue<bool>();
            foreach (var minion in allMinions)
            {
                if (mymana && useQ && Q.IsReady() && _player.Distance(minion.ServerPosition) < Q.Range &&
                    minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    Q.Cast(minion);
                }

                if (mymana && E.IsReady() && useE && _player.Distance(minion.ServerPosition) < E.Range &&
                    minion.Health < 0.95 * _player.GetSpellDamage(minion, SpellSlot.E))
                {
                    E.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * Config.Item("Energyjungle").GetValue<Slider>().Value) / 100);
            var useItemsJ = Config.Item("UseItemsjungle").GetValue<bool>();
            var useQ = Config.Item("UseQJ").GetValue<bool>();
            var useW = Config.Item("UseWJ").GetValue<bool>();
            var useE = Config.Item("UseEJ").GetValue<bool>();

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (mymana && _w.IsReady() && useW && _player.Distance(mob.ServerPosition) < Q.Range)
                {
                    _w.Cast(mob.Position);
                }
                if (mymana && useQ && Q.IsReady() && _player.Distance(mob.ServerPosition) < Q.Range)
                {
                    CastQ(mob);
                }
                if (mymana && E.IsReady() && useE && _player.Distance(mob.ServerPosition) < E.Range)
                {
                    E.Cast();
                }

                if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob.ServerPosition) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob.ServerPosition) < _hydra.Range)
                {
                    _hydra.Cast();
                }
            }

        }


        private static void UseItemes(Obj_AI_Hero target)
        {
            var iBilge = Config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth * (Config.Item("BilgeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth * (Config.Item("Bilgemyhp").GetValue<Slider>().Value) / 100);
            var iBlade = Config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth * (Config.Item("BladeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth * (Config.Item("Blademyhp").GetValue<Slider>().Value) / 100);
            var iOmen = Config.Item("Omen").GetValue<bool>();
            var iOmenenemys = ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(450)) >=
                              Config.Item("Omenenemys").GetValue<Slider>().Value;
            var iTiamat = Config.Item("Tiamat").GetValue<bool>();
            var iHydra = Config.Item("Hydra").GetValue<bool>();
            var ilotis = Config.Item("lotis").GetValue<bool>();
            var iYoumuu = Config.Item("Youmuu").GetValue<bool>();
            //var ihp = Config.Item("Hppotion").GetValue<bool>();
            // var ihpuse = _player.Health <= (_player.MaxHealth * (Config.Item("Hppotionuse").GetValue<Slider>().Value) / 100);
            //var imp = Config.Item("Mppotion").GetValue<bool>();
            //var impuse = _player.Health <= (_player.MaxHealth * (Config.Item("Mppotionuse").GetValue<Slider>().Value) / 100);

            if (_player.Distance(target.ServerPosition) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iTiamat && _tiamat.IsReady())
            {
                _tiamat.Cast();

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iHydra && _hydra.IsReady())
            {
                _hydra.Cast();

            }
            if (iOmenenemys && iOmen && _rand.IsReady())
            {
                _rand.Cast();

            }
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth * (Config.Item("lotisminhp").GetValue<Slider>().Value) / 100) &&
                        hero.Distance(_player.ServerPosition) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
                }
            }
            if (_player.Distance(target.ServerPosition) <= 350 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();

            }
        }

        private static Obj_AI_Minion WShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition != rpos) && minion.Name == "Shadow");
            }
        }
        private static Obj_AI_Minion RShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition == rpos) && minion.Name == "Shadow");
            }
        }

        private static UltCastStage UltStage
        {
            get
            {
                if (!R.IsReady()) return UltCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "zedult"
                    ? UltCastStage.First
                    : UltCastStage.Second);
            }
        }


        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedW"
                //return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);
               
            }
        }

        private static void CastW(Obj_AI_Base target)
        {
            if (delayw >= Environment.TickCount - shadowdelay || ShadowStage != ShadowCastStage.First || 
                ( target.HasBuff("zedulttargetmark") && LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R && UltStage == UltCastStage.Cooldown))
                return;

            var herew = target.Position.Extend(ObjectManager.Player.Position, -200);
        
            _w.Cast(herew, true);
            shadowdelay = Environment.TickCount;

        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (!Q.IsReady()) return;
            
            if (WShadow != null && target.Distance(WShadow.ServerPosition) <= 900 && target.Distance(_player.ServerPosition)>450)
            {

                    var shadowpred = Q.GetPrediction(target);
                Q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                 if (shadowpred.Hitchance >= HitChance.Medium)
                    Q.Cast(target);

              
            }
            else
            {
                
                Q.UpdateSourcePosition(_player.ServerPosition, _player.ServerPosition);
                var normalpred = Q.GetPrediction(target);

                if (normalpred.CastPosition.Distance(_player.ServerPosition) < 900 && normalpred.Hitchance >= HitChance.Medium)
                {
                    Q.Cast(target);
                }
               

            }
                

        }

        private static void CastE()
        {
            if (!E.IsReady()) return;
            if (ObjectManager.Get<Obj_AI_Hero>()
                .Count(
                    hero =>
                        hero.IsValidTarget() &&
                        (hero.Distance(ObjectManager.Player.ServerPosition) <= E.Range ||
                         (WShadow != null && hero.Distance(WShadow.ServerPosition) <= E.Range))) > 0)
                E.Cast();
        }

        internal enum UltCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);

             

            if (target.IsValidTarget() && Config.Item("UseIgnitekill").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (
                    _igniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready &&
                    ObjectManager.Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && _player.Distance(target.ServerPosition) <= 600)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (target.IsValidTarget() && Q.IsReady() && Config.Item("UseQM").GetValue<bool>() && Q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.ServerPosition) <= Q.Range)
                {
                    Q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.ServerPosition) <= Q.Range)
                {
                    Q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                    Q.Cast(target);
                }
                else if (RShadow != null && RShadow.Distance(target.ServerPosition) <= Q.Range)
                {
                    Q.UpdateSourcePosition(RShadow.ServerPosition, RShadow.ServerPosition);
                    Q.Cast(target);
                }
            }
            
         if (target.IsValidTarget() && Q.IsReady() && Config.Item("UseQM").GetValue<bool>() && Q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.ServerPosition) <= Q.Range)
                {
                    Q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.ServerPosition) <= Q.Range)
                {
                    Q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                    Q.Cast(target);
                }
            }
            if (E.IsReady() && Config.Item("UseEM").GetValue<bool>())
            {
                var t = CommonTargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                
                if (t.IsValidTarget() && E.GetDamage(t) > t.Health || (WShadow != null && WShadow.Distance(t.ServerPosition) <= E.Range))
                {
                    E.Cast();
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (RShadow != null)
            {
                Render.Circle.DrawCircle(RShadow.ServerPosition, RShadow.BoundingRadius * 2, Color.Blue);
            }


           
            if (Config.Item("shadowd").GetValue<bool>())
            {
                if (WShadow != null)
                {
                    if (ShadowStage == ShadowCastStage.Cooldown)
                    {
                        Render.Circle.DrawCircle(WShadow.ServerPosition, WShadow.BoundingRadius * 1.5f, Color.Red);
                    }
                    else if (WShadow != null && ShadowStage == ShadowCastStage.Second)
                    {
                        Render.Circle.DrawCircle(WShadow.ServerPosition, WShadow.BoundingRadius * 1.5f, Color.Yellow);
                    }
                }
            }
            if (Config.Item("damagetest").GetValue<bool>())
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

            if (Config.Item("CircleLag").GetValue<bool>())
            {
                if (Config.Item("DrawQ").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Blue);
                }
                if (Config.Item("DrawE").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
                }
                if (Config.Item("DrawQW").GetValue<bool>() && Config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.Yellow);
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
                if (Config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
                }
                if (Config.Item("DrawQW").GetValue<bool>() && Config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.White);
                }
                if (Config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.White);
                }
            }
        }
    }
}
