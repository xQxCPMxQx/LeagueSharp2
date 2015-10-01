using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;


namespace D_Tristana
{
    internal class program
    {
        private const string ChampionName = "Tristana";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static SpellSlot _igniteSlot;

        private static Items.Item _youmuu, _blade, _bilge, _hextech;

        //AP Style
        private static List<int> tristap = new List<int> {2, 1, 2, 1, 2, 3, 2, 1, 2, 1, 3, 1, 0, 0, 0, 3, 0, 0};
        //AD Style
        private static List<int> tristad = new List<int> {2, 1, 2, 0, 2, 3, 2, 0, 2, 0, 3, 0, 0, 0, 1, 3, 1, 1};

        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 0);
            _w = new Spell(SpellSlot.W, 900);
            _e = new Spell(SpellSlot.E, 541);
            _r = new Spell(SpellSlot.R, 541);

            _w.SetSkillshot(0.25f, 150, 1200, false, SkillshotType.SkillshotCircle);

            _hextech = new Items.Item(3146, 700);
            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            //D Tristana
            _config = new Menu("D-Tristana", "D-Tristana", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            //Combo  
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("Style", ""))
                .SetValue(
                    new StringList(new string[2] {"AP Tristana", "AD Tristana"}));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseEC", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddSubMenu(new Menu("AP Tristana Options", "AP Style"));
            _config.SubMenu("Combo").SubMenu("AP Style").AddItem(new MenuItem("UseWCP", "Use W")).SetValue(true);
            _config.SubMenu("Combo")
                .SubMenu("AP Style")
                .AddItem(new MenuItem("apdiveintower", "Use W to Dive into Enemy Tower"))
                .SetValue(true);
            _config.SubMenu("Combo").AddSubMenu(new Menu("AD Tristana Options", "AD Style"));
            _config.SubMenu("Combo").SubMenu("AD Style").AddItem(new MenuItem("UseWCD", "Use W")).SetValue(true);
            _config.SubMenu("Combo")
                .SubMenu("AD Style")
                .AddItem(new MenuItem("UseWHE", "Use W if Self % HP >").SetValue(new Slider(65, 1, 100)));
            _config.SubMenu("Combo")
                .SubMenu("AD Style")
                .AddItem(new MenuItem("EnemyC", "Use W if Enemy in Range <").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("Combo")
                .SubMenu("AD Style")
                .AddItem(new MenuItem("addiveintower", "Use W to Dive into Enemy Tower"))
                .SetValue(true);
            _config.SubMenu("Combo").AddSubMenu(new Menu("Use R", "Use R"));
            _config.SubMenu("Combo").SubMenu("Use R").AddItem(new MenuItem("UseRC", "Use R")).SetValue(true);
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != _player.Team))
                _config.SubMenu("Combo")
                    .SubMenu("Use R")
                    .AddItem(new MenuItem("castR" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));

            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));


            //Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseEH", "Use E")).SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "Auto-Harass (Toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            _config.AddSubMenu(new Menu("Items", "items"));
            _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Cutlass")).SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Bilgemyhp", "If Self % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use BotRK")).SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BladeEnemyhp", "If Enemy % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Blademyhp", "If Self % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextech", "Use Hextech Gunblade"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("HextechEnemyhp", "If Enemy % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextechmyhp", "If Self % HP <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usehppotions", "Use Health Potion/Flask/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionhp", "If % HP <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usemppotions", "Use Mana Potion/Flask/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionmp", "If % Mana <").SetValue(new Slider(35, 1, 100)));

            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lane", "Lane"));
            _config.SubMenu("Farm").SubMenu("Lane").AddItem(new MenuItem("UseQL", "Use Q")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Lane").AddItem(new MenuItem("UseWL", "Use W (AP Tristana Only)")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(new MenuItem("UseWLane", "Use W if Self % HP >").SetValue(new Slider(65, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(new MenuItem("Enemylane", "Use R if Enemy in Range <").SetValue(new Slider(2, 0, 5)));
            _config.SubMenu("Farm").SubMenu("Lane").AddItem(new MenuItem("UseEL", "Use E")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(
                    new MenuItem("ActiveLane", "Lane Clear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(new MenuItem("Lanemana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));
            //jungle
            _config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJ", "Use Q")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("UseWJ", "Use W (AP Tristana Only)"))
                .SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseEJ", "Use E")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle Clear").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            _config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("Junglemana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));

            //Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc").AddSubMenu(new Menu("Use R", "Use R"));
            _config.SubMenu("Misc").SubMenu("Use R").AddItem(new MenuItem("UseRM", "Use R to Killsteal")).SetValue(true);
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != _player.Team))
                _config.SubMenu("Misc")
                    .SubMenu("Use R")
                    .AddItem(new MenuItem("castRkill" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            _config.SubMenu("Misc").AddItem(new MenuItem("useWK", "Use W KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useEK", "Use E to Killsteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("UseRGap", "Use R Gapclosers")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("UseRInter", "Use R to Interrupt")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("AutoLevel", "Auto-Level")).SetValue(false);

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw Combo Damage").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            //Drawings
            _config.AddSubMenu(new Menu("Drawings", "Drawings"));
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            _config.SubMenu("Drawings").AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag-Free Circles").SetValue(true));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circle Quality").SetValue(new Slider(100, 100, 10)));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circle Thickness").SetValue(new Slider(1, 10, 1)));

            _config.AddToMainMenu();
            //Game.PrintChat("<font color='#881df2'>D-Tristana by Diabaths (WIP)</font> Loaded.");
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            CustomEvents.Unit.OnLevelUp += OnLevelUp;
        }

        private static void Game_OnUpdate(EventArgs args)
        {

            if (_config.Item("ActiveJungle").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if ((_config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 _config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Harrasmana").GetValue<Slider>().Value)
            {
               Harass();
            }
            if (_config.Item("ActiveLane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Lanemana").GetValue<Slider>().Value)
            {
                Laneclear();
            }
            _player = ObjectManager.Player;

            _orbwalker.SetAttack(true);
            Usepotion();
            KillSteal();
            _e.Range = 550 + 9*(ObjectManager.Player.Level - 1);
            _r.Range = 550 + 9*(ObjectManager.Player.Level - 1);
        }

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            var dmg = 0d;

            if (_q.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.Q);
            if (_w.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.W);
            if (_e.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.E);
            if (_r.IsReady())
            {
                if (_config.Item("Style").GetValue<StringList>().SelectedIndex == 0)
                {
                    dmg += _player.GetSpellDamage(hero, SpellSlot.R)*1.2;
                }
                else
                {
                    dmg += _player.GetSpellDamage(hero, SpellSlot.R);
                }
            }
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Botrk);
            if (Items.HasItem(3146) && Items.CanUseItem(3146))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Hexgun);
            if (ObjectManager.Player.HasBuff("LichBane"))
            {
                dmg += _player.BaseAttackDamage * 0.75 + _player.FlatMagicDamageMod * 0.5;
            }
            if (ObjectManager.Player.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                dmg += _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            dmg += _player.GetAutoAttackDamage(hero, true)*2;
            return (float) dmg;
        }

        private static void UseItemes(Obj_AI_Hero target)
        {
            var iBilge = _config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth*(_config.Item("BilgeEnemyhp").GetValue<Slider>().Value)/100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth*(_config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
            var iBlade = _config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth*(_config.Item("BladeEnemyhp").GetValue<Slider>().Value)/100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth*(_config.Item("Blademyhp").GetValue<Slider>().Value)/100);
            var iYoumuu = _config.Item("Youmuu").GetValue<bool>();
            var iHextech = _config.Item("Hextech").GetValue<bool>();
            var iHextechEnemyhp = target.Health <=
                                  (target.MaxHealth * (_config.Item("HextechEnemyhp").GetValue<Slider>().Value) / 100);
            var iHextechmyhp = _player.Health <=
                               (_player.MaxHealth * (_config.Item("Hextechmyhp").GetValue<Slider>().Value) / 100);

            if (_player.Distance(target) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target) <= 450 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();
            }
            if (_player.Distance(target) <= 700 && iHextech && (iHextechEnemyhp || iHextechmyhp) && _hextech.IsReady())
            {
                _hextech.Cast(target);
            }
        }
        private static void Usepotion()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var iusehppotion = _config.Item("usehppotions").GetValue<bool>();
            var iusepotionhp = _player.Health <=
                               (_player.MaxHealth * (_config.Item("usepotionhp").GetValue<Slider>().Value) / 100);
            var iusemppotion = _config.Item("usemppotions").GetValue<bool>();
            var iusepotionmp = _player.Mana <=
                               (_player.MaxMana * (_config.Item("usepotionmp").GetValue<Slider>().Value) / 100);
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.HasBuff("Recall")) return;

            if (ObjectManager.Player.CountEnemiesInRange(800) > 0 ||
                (mobs.Count > 0 && _config.Item("ActiveJungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
                 SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i)) || SmitePurple.Any(i => Items.HasItem(i)) ||
                  SmiteBlue.Any(i => Items.HasItem(i)) || SmiteGrey.Any(i => Items.HasItem(i))
                     )))
            {
                if (iusepotionhp && iusehppotion &&
                     !(ObjectManager.Player.HasBuff("RegenerationPotion", true) ||
                       ObjectManager.Player.HasBuff("ItemCrystalFlask", true) ||
                       ObjectManager.Player.HasBuff("ItemMiniRegenPotion", true)))
                {
                    if (Items.HasItem(2041) && Items.CanUseItem(2041))
                    {
                        Items.UseItem(2041);
                    }
                    else if (Items.HasItem(2010) && Items.CanUseItem(2010))
                    {
                        Items.UseItem(2010);
                    }
                    else if (Items.HasItem(2003) && Items.CanUseItem(2003))
                    {
                        Items.UseItem(2003);
                    }
                }


                if (iusepotionmp && iusemppotion &&
                    !(ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true) ||
                      ObjectManager.Player.HasBuff("ItemCrystalFlask", true) ||
                      ObjectManager.Player.HasBuff("ItemMiniRegenPotion", true)))
                {
                    if (Items.HasItem(2041) && Items.CanUseItem(2041))
                    {
                        Items.UseItem(2041);
                    }
                    else if (Items.HasItem(2010) && Items.CanUseItem(2010))
                    {
                        Items.UseItem(2010);
                    }
                    else if (Items.HasItem(2004) && Items.CanUseItem(2004))
                    {
                        Items.UseItem(2004);
                    }
                }
            }
        }

        private static void OnLevelUp(LeagueSharp.Obj_AI_Base sender,
            LeagueSharp.Common.CustomEvents.Unit.OnLevelUpEventArgs args)
        {
            if (!sender.IsValid || !sender.IsMe)
                return;

            if (!_config.Item("AutoLevel").GetValue<bool>()) return;
            if (_config.Item("Style").GetValue<StringList>().SelectedIndex == 0)
                _player.Spellbook.LevelUpSpell((SpellSlot) tristap[args.NewLevel - 1]);
            else if (_config.Item("Style").GetValue<StringList>().SelectedIndex == 1)
                _player.Spellbook.LevelUpSpell((SpellSlot) tristad[args.NewLevel - 1]);
        }

        private static void Combo()
        {
            var eTarget = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            var ignitecombo = _config.Item("UseIgnitecombo").GetValue<bool>();
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useE = _config.Item("UseEC").GetValue<bool>();
            var useR = _config.Item("UseRC").GetValue<bool>();
            if (eTarget != null)
            {
                UseItemes(eTarget);
                if (_igniteSlot != SpellSlot.Unknown && ignitecombo &&
                    _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (eTarget.Health <= ComboDamage(eTarget))
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, eTarget);
                    }
                }
                if (useE && _e.IsReady() && eTarget.IsValidTarget())
                {
                    _e.CastOnUnit(eTarget);
                }
                if (useQ && _q.IsReady())
                {
                    _q.CastOnUnit(ObjectManager.Player);
                }
                UseW(eTarget);
                if (useR && _r.IsReady() && _config.Item("castR" + eTarget.BaseSkinName) != null &&
                    _config.Item("castR" + eTarget.BaseSkinName).GetValue<bool>() == true)
                {
                    foreach (
                        var hero in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(
                                    hero =>
                                        hero.IsValidTarget(_r.Range) &&
                                        ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R) - 20 > hero.Health))
                        _r.CastOnUnit(hero);
                }
            }
        }

        private static void UseW(Obj_AI_Hero eTarget)
        {
            var useWd = _config.Item("UseWCD").GetValue<bool>();
            var useWp = _config.Item("UseWCP").GetValue<bool>();
            var apstyle = _config.Item("Style").GetValue<StringList>().SelectedIndex == 0;
            var adstyle = _config.Item("Style").GetValue<StringList>().SelectedIndex == 1;
            var usewhE = (100*(_player.Health/_player.MaxHealth)) > _config.Item("UseWHE").GetValue<Slider>().Value;
            var apdiveTower = _config.Item("apdiveintower").GetValue<bool>();
            var addiveTower = _config.Item("addiveintower").GetValue<bool>();
            var edmg = _player.GetSpellDamage(eTarget, SpellSlot.E);
            if (Utility.UnderTurret(eTarget) && ((!apdiveTower && apstyle) || (!addiveTower && adstyle))) return;

            if (_player.Distance(eTarget) > Orbwalking.GetRealAutoAttackRange(_player) && adstyle && useWd &&
                _w.IsReady() && usewhE &&
                ObjectManager.Player.CountEnemiesInRange(1300) <= _config.Item("EnemyC").GetValue<Slider>().Value)
            {
                _w.Cast(eTarget.Position);
            }
            if (apstyle && useWp && _w.IsReady() && ComboDamage(eTarget)>eTarget.Health)
            {
                if (_player.Distance(eTarget) <= _w.Range)
                {
                    _w.Cast(EnemyJumb(eTarget));
                }
                else if (_player.Distance(eTarget) >
                         _w.Range + Orbwalking.GetRealAutoAttackRange(_player) - 200 && _e.IsReady() &&
                         edmg +100 > eTarget.Health)
                {
                    _w.Cast(eTarget.Position);
                }
            }
        }

        private static void Harass()
        {
            var eTarget = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Physical);
            var useQ = _config.Item("UseQH").GetValue<bool>();
            var useE = _config.Item("UseEH").GetValue<bool>();
            if (eTarget != null)
            {
                if (useE && _e.IsReady() && eTarget.IsValidTarget())
                {
                    _e.CastOnUnit(eTarget);
                }
                if (useQ && _q.IsReady())
                {
                    _q.CastOnUnit(ObjectManager.Player);
                }
            }
        }

        private static void Laneclear()
        {
            var rangedMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _w.Range + _w.Width + 30,
                MinionTypes.Ranged);
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _w.Range + _w.Width + 30,
                MinionTypes.All);
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _e.Range, MinionTypes.All);
            var usewlane = (100*(_player.Health/_player.MaxHealth)) > _config.Item("UseWLane").GetValue<Slider>().Value;
            var useQ = _config.Item("UseQL").GetValue<bool>();
            var useW = _config.Item("UseWL").GetValue<bool>();
            var useE = _config.Item("UseEL").GetValue<bool>();
            var tristanap = _config.Item("Style").GetValue<StringList>().SelectedIndex == 0;
            foreach (var minion in allMinions)
            {
                if (allMinions.Count > 3)
                {
                    if (useE && _e.IsReady())
                    {
                        _e.Cast(minion);
                    }
                    if (useQ && _q.IsReady())
                    {
                        _q.CastOnUnit(ObjectManager.Player);
                    }
                }
            }
            if (usewlane && useW && _w.IsReady() && tristanap &&
                ObjectManager.Player.CountEnemiesInRange(1300) <= _config.Item("Enemylane").GetValue<Slider>().Value)
            {
                var fl1 = _w.GetCircularFarmLocation(rangedMinionsQ, _w.Width);
                var fl2 = _w.GetCircularFarmLocation(allMinionsQ, _w.Width);

                if (fl1.MinionsHit >= 3 && !_player.UnderTurret())
                {
                    _w.Cast(fl1.Position);
                }
                else if ((fl2.MinionsHit >= 2 || allMinionsQ.Count == 1) && !_player.UnderTurret())
                {
                    _w.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) && !_player.UnderTurret() &&
                            minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.Q))
                            _w.Cast(minion);
            }
        }
        
        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _e.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = _config.Item("UseQJ").GetValue<bool>();
            var useW = _config.Item("UseWJ").GetValue<bool>();
            var useE = _config.Item("UseEJ").GetValue<bool>();
            var tristanap = _config.Item("Style").GetValue<StringList>().SelectedIndex == 0;
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (_e.IsReady() && useE)
                {
                    _e.Cast(mob);
                }
                if (useQ && _q.IsReady())
                {
                    _q.CastOnUnit(ObjectManager.Player);
                }
                if (useW && _w.IsReady() && tristanap)
                {
                    _w.Cast(mob);
                }

            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_r.IsReady() && gapcloser.Sender.IsValidTarget(_r.Range) && _config.Item("UseRGap").GetValue<bool>())
                _r.CastOnUnit(gapcloser.Sender);
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (_r.IsReady() && unit.IsValidTarget(_r.Range) && _config.Item("UseRInter").GetValue<bool>())
                _r.CastOnUnit(unit);
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
            var usew = _config.Item("useWK").GetValue<bool>();
            var usee = _config.Item("useEK").GetValue<bool>();
            var user = _config.Item("UseRM").GetValue<bool>();
            var whDmg = _player.GetSpellDamage(target, SpellSlot.W);
            var ehDmg = _player.GetSpellDamage(target, SpellSlot.E);
            var rhDmg = _player.GetSpellDamage(target, SpellSlot.R);
            var wmana = _player.Spellbook.GetSpell(SpellSlot.W).ManaCost;
            var emana = _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            var rmana = _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            if (usew && _player.Mana > wmana && _w.IsReady() && whDmg - 20 > target.Health &&
                _player.Distance(target) <= _w.Range && !Utility.UnderTurret(target) && ObjectManager.Player.CountEnemiesInRange(1300) <= 1)
            {
                _w.Cast(EnemyJumb(target));
            }
            else if (usee && _player.Mana > emana && _e.IsReady() && ehDmg - 20 > target.Health &&
                     _player.Distance(target) <= _e.Range)
            {
                _e.Cast(target);
            }
            else if (_e.IsReady() && _w.IsReady() && usee && usew && _player.Mana > wmana + emana &&
                     ehDmg - 20 > target.Health &&
                     _player.Distance(target) > _e.Range && _player.Distance(target) < _e.Range + _w.Range &&
                     !Utility.UnderTurret(target) && ObjectManager.Player.CountEnemiesInRange(1300) <= 1)
            {
                _w.Cast(target.Position);
                _e.Cast(target);
            }
            else if (user && _player.Mana > rmana && _r.IsReady())
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(_r.Range) && rhDmg - 20 > hero.Health &&
                                    _config.Item("castRkill" + hero.BaseSkinName) != null &&
                                    _config.Item("castRkill" + hero.BaseSkinName).GetValue<bool>() == true &&
                                    !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("Undying Rage")))
                    _r.CastOnUnit(hero);
            }
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useE = _config.Item("UseEC").GetValue<bool>();
            var t = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
            var combo = _config.Item("ActiveCombo").GetValue<KeyBind>().Active;
            if (combo && unit.IsMe && (target is Obj_AI_Hero))
            {
                if (useE && _e.IsReady() && unit.IsValidTarget() && t !=null)
                {
                    _e.CastOnUnit(t);
                }
                if (useQ && _q.IsReady())
                {
                    _q.CastOnUnit(ObjectManager.Player);
                }
            }
        }

        private static Vector3 EnemyJumb(Obj_AI_Hero enemy)
        {
            if (ObjectManager.Player.Position.Distance(enemy.Position) <= _w.Range)
                return enemy.Position;
            var newpos = enemy.Position - ObjectManager.Player.Position;
            newpos.Normalize();
            return ObjectManager.Player.Position + (newpos*_w.Range);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
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
                        "Combo+AA=Rekt");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                        Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                        "Unkillable");
                }
            }
            if (_config.Item("CircleLag").GetValue<bool>())
            {
                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.Orange,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, 550 + 9 * (ObjectManager.Player.Level - 1), System.Drawing.Color.Orange,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, 550 + 9 * (ObjectManager.Player.Level - 1), System.Drawing.Color.Orange,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
            }
            else
            {

                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 550 + 9 * (ObjectManager.Player.Level - 1), System.Drawing.Color.White);
                }

                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 550 + 9 * (ObjectManager.Player.Level - 1), System.Drawing.Color.White);
                }
            }
        }
    }
}
