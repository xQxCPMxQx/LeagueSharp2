using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace D_Ezreal
{
    internal class Program
    {
        private const string ChampionName = "Ezreal";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static SpellSlot _igniteSlot;

        private static Int32 _lastSkin;
        private static Vector2 _pingLocation;

        private static int _lastPingT = 0;


        private static readonly int[] Ezrealap = {2, 3, 2, 1, 2, 4, 2, 3, 2, 3, 4, 3, 3, 1, 1, 4, 1, 1};
        private static readonly int[] Ezrealad = {1, 3, 1, 2, 1, 4, 1, 3, 1, 3, 4, 3, 2, 3, 2, 4, 2, 2};
        private static readonly int[] EzrealQwe = {1, 3, 2, 1, 1, 4, 1, 2, 1, 2, 4, 2, 2, 3, 3, 4, 3, 3};
        private static readonly int[] EzrealWqe = {1, 3, 2, 2, 2, 4, 2, 1, 2, 1, 4, 1, 1, 3, 3, 4, 3, 3};

        private static readonly int[] SmitePurple = {3713, 3726, 3725, 3726, 3723};
        private static readonly int[] SmiteGrey = {3711, 3722, 3721, 3720, 3719};
        private static readonly int[] SmiteRed = {3715, 3718, 3717, 3716, 3714};
        private static readonly int[] SmiteBlue = {3706, 3710, 3709, 3708, 3707};
        private static Items.Item _youmuu, _blade, _bilge, _hextech;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Ping(Vector2 position)
        {
            if (Environment.TickCount - _lastPingT < 30*1000) return;
            _lastPingT = Environment.TickCount;
            _pingLocation = position;
            SimplePing();
            Utility.DelayAction.Add(150, SimplePing);
            Utility.DelayAction.Add(300, SimplePing);
            Utility.DelayAction.Add(400, SimplePing);
        }

        private static void SimplePing()
        {
            Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(_pingLocation.X, _pingLocation.Y, 0, 0,
                Packet.PingType.Fallback)).Process();
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 1150);
            _w = new Spell(SpellSlot.W, 1000);
            _e = new Spell(SpellSlot.E, 475);
            _r = new Spell(SpellSlot.R, 3000);

            _q.SetSkillshot(0.5f, 80f, 1200f, true, SkillshotType.SkillshotLine);
            _w.SetSkillshot(0.5f, 80f, 1200f, false, SkillshotType.SkillshotLine);
            _e.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);
            _r.SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            _hextech = new Items.Item(3146, 700);
            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            //D Ezreal
            _config = new Menu("D-Ezreal", "D-Ezreal", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            //Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite(rush for it)")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            //Ulti Factions
            _config.AddSubMenu(new Menu("R Factions", "R Factions"));
            _config.SubMenu("R Factions").AddSubMenu(new Menu("Use R combo", "Use R combo"));
            _config.SubMenu("R Factions")
                .SubMenu("Use R combo")
                .AddItem(new MenuItem("UseRcombo", "Use R in Combo"))
                .SetValue(true);
            _config.SubMenu("R Factions")
                .SubMenu("Use R combo")
                .AddItem(new MenuItem("UseRrush", "Rush R if ComboDmg>=Tagret HP"))
                .SetValue(true);
            _config.SubMenu("R Factions")
                .SubMenu("Use R combo")
                .AddItem(new MenuItem("UseRC", "Use R if R.Dmg>Targ. HP"))
                .SetValue(true);
            _config.SubMenu("R Factions")
                .SubMenu("Use R combo")
                .AddItem(new MenuItem("UseRE", "Auto R if Hit X Enemys"))
                .SetValue(true);
            _config.SubMenu("R Factions")
                .SubMenu("Use R combo")
                .AddItem(new MenuItem("MinTargets", "Auto R if Hit X Enemys").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("R Factions").SubMenu("Use R combo").AddItem(new MenuItem("", "Use R in Targets Below"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != _player.Team))
                _config.SubMenu("R Factions")
                    .SubMenu("Use R combo")
                    .AddItem(new MenuItem("castRezreal" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(true));
            _config.SubMenu("R Factions").AddSubMenu(new Menu("Use R killsteal", "Use R killsteal"));
            _config.SubMenu("R Factions")
                .SubMenu("Use R killsteal")
                .AddItem(new MenuItem("UseRM", "Use R KillSteal"))
                .SetValue(true);
            _config.SubMenu("R Factions").SubMenu("Use R killsteal").AddItem(new MenuItem("", "Use R in Targets Below"));
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != _player.Team))
                _config.SubMenu("R Factions")
                    .SubMenu("Use R killsteal")
                    .AddItem(new MenuItem("castRkill" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
            _config.SubMenu("R Factions")
                .AddItem(new MenuItem("Minrange", "Min R range to Use").SetValue(new Slider(800, 0, 1500)));
            _config.SubMenu("R Factions")
                .AddItem(new MenuItem("Maxrange", "Max R range to Use").SetValue(new Slider(3000, 1500, 5000)));

            //items
            _config.AddSubMenu(new Menu("items", "items"));
            _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("usemuramana", "Use Muramana"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("muramanamin", "Use Muramana until MP < %").SetValue(new Slider(25, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
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
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextech", "Hextech Gunblade"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("HextechEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Hextechmyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));

            _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
            _config.SubMenu("items").SubMenu("Deffensive").AddSubMenu(new Menu("Cleanse", "Cleanse"));
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("useqss", "Use QSS/Mercurial Scimitar/Dervish Blade")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("blind", "Blind")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("charm", "Charm")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("fear", "Fear")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("flee", "Flee")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("snare", "Snare")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("taunt", "Taunt")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("suppression", "Suppression")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("stun", "Stun")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("polymorph", "Polymorph")).SetValue(false);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("silence", "Silence")).SetValue(false);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("zedultexecute", "Zed Ult")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("Cleansemode", "Use Cleanse")).SetValue(new StringList(new string[2] { "Always", "In Combo" }));

            _config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usehppotions", "Use Healt potion/Flask/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionhp", "If Health % <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usemppotions", "Use Mana potion/Flask/Biscuit"))
                .SetValue(true);
            _config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionmp", "If Mana % <").SetValue(new Slider(35, 1, 100)));

            //Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseWH", "Use W")).SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "AutoHarass (toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Farm
            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(new MenuItem("Lanemana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("ActiveLane", "Farm key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lasthit", "Lasthit"));
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(new MenuItem("lastmana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Lasthit")
                .AddItem(
                    new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("JungleClear", "JungleClear"));
            _config.SubMenu("Farm").SubMenu("JungleClear").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
            _config.SubMenu("Farm")
                .SubMenu("JungleClear")
                .AddItem(new MenuItem("Junglemana", "Minimum Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("JungleClear")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle key").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            //Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc")
                .AddItem(new MenuItem("pingulti", "Ping If R Dmg>Enemy Health (only local)").SetValue(false));
            _config.SubMenu("Misc").AddItem(new MenuItem("useQK", "Use Q KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useWK", "Use W KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useEK", "Use (E-Q) or (E-W) KillSteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQdash", "Auto Q dashing")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQimmo", "Auto Q Immobile")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQstun", "Auto Q Taunt/Fear/Charm/Snare")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("EZAutoLevel", "Auto Level")).SetValue(false);
            _config.SubMenu("Misc").AddItem(new MenuItem("EZStyle", "Level Sequence").SetValue(
                new StringList(new[] {"W-E-Q", "W-Q-E", "Q-E-W", "Q-W-E"})));
            _config.SubMenu("Misc").AddItem(new MenuItem("skinez", "Use Custom Skin").SetValue(false));
            _config.SubMenu("Misc").AddItem(new MenuItem("skinezreal", "Skin Changer").SetValue(new Slider(4, 1, 8)));

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            //Drawings
            _config.AddSubMenu(new Menu("Drawings", "Drawings"));
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            _config.SubMenu("Drawings").AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));

            _config.AddToMainMenu();
            Game.PrintChat("<font color='#881df2'>D-Ezreal by Diabaths</font> Loaded.");
            Game.PrintChat(
                "<font color='#FF0000'>If You like my work and want to support me,  plz donate via paypal in </font> <font color='#FF9900'>ssssssssssmith@hotmail.com</font> (10) S");
            if (_config.Item("skinez").GetValue<bool>())
            {
                GenModelPacket(_player.ChampionName, _config.Item("skinezreal").GetValue<Slider>().Value);
                _lastSkin = _config.Item("skinezreal").GetValue<Slider>().Value;
            }
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            _config.Item("EZAutoLevel").ValueChanged += LevelUpMode;
            if (_config.Item("EZAutoLevel").GetValue<bool>())
            {
                var level = new AutoLevel(Style());
            }

        }

        private static void LevelUpMode(object sender, OnValueChangeEventArgs e)
        {
            AutoLevel.Enabled(e.GetNewValue<bool>());
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_config.Item("pingulti").GetValue<bool>())
            {
                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    ObjectManager.Player.Spellbook.CanUseSpell(SpellSlot.R) == SpellState.Ready &&
                                    hero.IsValidTarget(30000) &&
                                    _player.GetSpellDamage(hero, SpellSlot.R)*0.9 > hero.Health)
                    )
                {
                    Game.PrintChat("ping");
                    Ping(enemy.Position.To2D());
                }
            }
            if (_config.Item("skinez").GetValue<bool>() && SkinChanged())
            {
                GenModelPacket(_player.ChampionName, _config.Item("skinezreal").GetValue<Slider>().Value);
                _lastSkin = _config.Item("skinezreal").GetValue<Slider>().Value;
            }
            if (_config.Item("ActiveLane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("Lanemana").GetValue<Slider>().Value)
            {
                Laneclear();
            }
            _r.Range = _config.Item("Maxrange").GetValue<Slider>().Value;
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var qpred = _q.GetPrediction(target);
            var manacheck = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                            _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            if (_player.Mana >= manacheck && qpred.Hitchance == HitChance.Immobile && _q.IsReady() &&
                _config.Item("useQimmo").GetValue<bool>())
            {
                _q.Cast(qpred.CastPosition);
            }
            if (_player.Mana >= manacheck && qpred.Hitchance == HitChance.Dashing && _q.IsReady() &&
                _config.Item("useQdash").GetValue<bool>())
            {
                _q.Cast(qpred.CastPosition);
            }
            if (_player.Mana >= manacheck && qpred.Hitchance == HitChance.High && _q.IsReady() &&
                _config.Item("useQstun").GetValue<bool>() &&
                (target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Charm) ||
                 target.HasBuffOfType(BuffType.Fear) ||
                 target.HasBuffOfType(BuffType.Taunt)))
            {
                _q.Cast(qpred.CastPosition);
            }
            _player = ObjectManager.Player;
            _orbwalker.SetAttack(true);
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

            if (_config.Item("ActiveLast").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("lastmana").GetValue<Slider>().Value)
            {
                LastHit();
            }
            Muramana();
            KillSteal();
            Usepotion();
            Usecleanse();

        }

        private static int[] Style()
        {
            switch (_config.Item("EZStyle").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    return Ezrealap;
                case 1:
                    return EzrealWqe;
                case 2:
                    return Ezrealad;
                case 3:
                    return EzrealQwe;
                default:
                    return null;
            }
        }

        private static void GenModelPacket(string champ, int skinId)
        {
            Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(_player.NetworkId, skinId, champ))
                .Process();
        }

        private static bool SkinChanged()
        {
            return (_config.Item("skinezreal").GetValue<Slider>().Value != _lastSkin);
        }

        private static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            var combo = _config.Item("ActiveCombo").GetValue<KeyBind>().Active;
            if (combo && unit.IsMe && (target is Obj_AI_Hero))
            {
                if (useQ && _q.IsReady())
                {
                    var t = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);

                    if (unit.IsValidTarget() && _q.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _q.Cast(t);
                    }
                }
                if (useW && _w.IsReady())
                {
                    var t = TargetSelector.GetTarget(_w.Range, TargetSelector.DamageType.Magical);
                    if (unit.IsValidTarget() && _w.GetPrediction(t).Hitchance >= HitChance.High)
                    {
                        _w.Cast(t, false, true);
                    }
                }
            }
        }

        private static void Muramana()
        {
            var muranama = _player.Mana >=
                           (_player.MaxMana*(_config.Item("muramanamin").GetValue<Slider>().Value)/100);
            if (!_config.Item("usemuramana").GetValue<bool>()) return;
            if (muranama && _player.Buffs.Count(buf => buf.Name == "Muramana") == 0 &&
                _config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Items.UseItem(3042);
            }
            if ((!muranama || !_config.Item("ActiveCombo").GetValue<KeyBind>().Active) &&
                _player.Buffs.Count(buf => buf.Name == "Muramana") == 1)
            {
                Items.UseItem(3042);
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var ignitecombo = _config.Item("UseIgnitecombo").GetValue<bool>();
            var useQ = _config.Item("UseQC").GetValue<bool>();
            var useW = _config.Item("UseWC").GetValue<bool>();
            var useR = _config.Item("UseRcombo").GetValue<bool>();
            if (target != null) UseItemes(target);
            if (target != null && _igniteSlot != SpellSlot.Unknown && ignitecombo &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (target.Health <= ComboDamage(target))
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (target != null && _q.IsReady() && useQ && _q.GetPrediction(target).Hitchance >= HitChance.High)
            {
                if (_player.Distance(target) <= _q.Range - 50)
                {
                    _q.Cast(target);
                }
            }
            if (target != null && _w.IsReady() && useW && _w.GetPrediction(target).Hitchance >= HitChance.High)
            {
                if (_player.Distance(target) <= _w.Range - 50)
                {
                    _w.Cast(target, false, true);
                }
            }
            if (useR)
            {
                UseRcombo();
            }
            UseItemes(target);
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Physical);
            var useQ = _config.Item("UseQH").GetValue<bool>();
            var useW = _config.Item("UseWH").GetValue<bool>();
            if (target != null && _q.IsReady() && useQ && _q.GetPrediction(target).Hitchance >= HitChance.High)
            {
                if (_player.Distance(target) <= _q.Range - 50)
                {
                    _q.Cast(target);
                }
            }
            if (target != null && _w.IsReady() && useW && _w.GetPrediction(target).Hitchance >= HitChance.High)
            {
                if (_player.Distance(target) <= _w.Range - 50)
                {
                    _w.Cast(target, false, true);
                }
            }
        }

        private static void Laneclear()
        {
            if (_config.Item("UseQL").GetValue<bool>()) Farm_skills(_q, true);
        }

        private static void LastHit()
        {
            if (_config.Item("UseQLH").GetValue<bool>()) Farm_skills(_q, true);
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = _config.Item("UseQJ").GetValue<bool>();
            if (mobs.Count <= 0) return;
            var mob = mobs[0];
            if (useQ && _q.IsReady())
            {
                _q.Cast(mob);
            }
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
                dmg += _player.GetSpellDamage(hero, SpellSlot.R)*0.9;
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Bilgewater);
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Botrk);
            if (Items.HasItem(3146) && Items.CanUseItem(3146))
                dmg += _player.GetItemDamage(hero, Damage.DamageItems.Hexgun);
            if (ObjectManager.Player.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                dmg += _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            if (ObjectManager.Player.HasBuff("LichBane"))
            {
                dmg += _player.BaseAttackDamage*0.75 + _player.FlatMagicDamageMod*0.5;
            }
            dmg += _player.GetAutoAttackDamage(hero, true)*2;
            return (float) dmg;
        }

        private static void Usepotion()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var iusehppotion = _config.Item("usehppotions").GetValue<bool>();
            var iusepotionhp = _player.Health <=
                               (_player.MaxHealth*(_config.Item("usepotionhp").GetValue<Slider>().Value)/100);
            var iusemppotion = _config.Item("usemppotions").GetValue<bool>();
            var iusepotionmp = _player.Mana <=
                               (_player.MaxMana*(_config.Item("usepotionmp").GetValue<Slider>().Value)/100);
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.HasBuff("Recall")) return;

            if (ObjectManager.Player.CountEnemiesInRange(800) > 0 ||
                (mobs.Count > 0 && _config.Item("ActiveJungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
                                                                                               SmiteBlue.Any(
                                                                                                   i => Items.HasItem(i)) ||
                                                                                               SmiteRed.Any(
                                                                                                   i => Items.HasItem(i)) ||
                                                                                               SmitePurple.Any(
                                                                                                   i => Items.HasItem(i)) ||
                                                                                               SmiteBlue.Any(
                                                                                                   i => Items.HasItem(i)) ||
                                                                                               SmiteGrey.Any(
                                                                                                   i => Items.HasItem(i))
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
                                  (target.MaxHealth*(_config.Item("HextechEnemyhp").GetValue<Slider>().Value)/100);
            var iHextechmyhp = _player.Health <=
                               (_player.MaxHealth*(_config.Item("Hextechmyhp").GetValue<Slider>().Value)/100);

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

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
            var useq = _config.Item("useQK").GetValue<bool>();
            var usew = _config.Item("useWK").GetValue<bool>();
            var usee = _config.Item("useEK").GetValue<bool>();
            var user = _config.Item("UseRM").GetValue<bool>();
            var whDmg = _player.GetSpellDamage(target, SpellSlot.W);
            var qhDmg = _player.GetSpellDamage(target, SpellSlot.Q);
            var rhDmg = _player.GetSpellDamage(target, SpellSlot.R);
            var emana = _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            var wmana = _player.Spellbook.GetSpell(SpellSlot.W).ManaCost;
            var qmana = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            var rmana = _player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
            if (usew && _w.IsReady() && whDmg - 20 > target.Health)
            {
                if (_player.Distance(target) <= _w.Range && _player.Mana > wmana &&
                    _w.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    _w.Cast(target, false, true);
                }
                else if (usee && _player.Distance(target) >= _w.Range && _player.Mana > wmana + emana && _e.IsReady()
                         && _w.GetPrediction(target).CollisionObjects.Count > 0)
                {
                    _e.Cast(target.Position);
                    _w.Cast(target.ServerPosition);
                }
            }
            if (useq && _q.IsReady() && qhDmg - 20 > target.Health)
            {
                if (_player.Mana > qmana && _player.Distance(target) <= _q.Range &&
                    _q.GetPrediction(target).Hitchance >= HitChance.High)
                {
                    _q.Cast(target);
                }
                else if (_e.IsReady() && usee && _player.Distance(target) >= _q.Range && _player.Mana > qmana + emana &&
                         _q.GetPrediction(target).CollisionObjects.Count > 0)
                {
                    _e.Cast(target.Position);
                    _q.Cast(target.ServerPosition);
                }
            }
            if (user && _player.Mana > rmana && _r.IsReady())
            {
                foreach (
                    var hero in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                hero =>
                                    hero.IsValidTarget(_r.Range) && _r.GetPrediction(hero).Hitchance >= HitChance.High &&
                                    rhDmg - 20 > hero.Health &&
                                    _config.Item("castRkill" + hero.BaseSkinName) != null &&
                                    _config.Item("castRkill" + hero.BaseSkinName).GetValue<bool>() == true &&
                                    !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("Undying Rage")))
                    _r.Cast(hero, false, true);
            }
        }

        private static void UseRcombo()
        {
            if (!_r.IsReady()) return;
            var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
            var minrange = _config.Item("Minrange").GetValue<Slider>().Value;
            var rDmg = _player.GetSpellDamage(target, SpellSlot.R)*0.9;
            var rrush = _config.Item("UseRrush").GetValue<bool>();
            var rsolo = _config.Item("UseRC").GetValue<bool>();
            var autoR = _config.Item("UseRE").GetValue<bool>();
            if (target == null) return;
            if (target.HasBuff("JudicatorIntervention") || target.HasBuff("Undying Rage")) return;

            if (_player.Distance(target) > minrange && _r.GetPrediction(target).Hitchance >= HitChance.High)
            {
                if (_config.Item("castRezreal" + target.BaseSkinName) != null &&
                    _config.Item("castRezreal" + target.BaseSkinName).GetValue<bool>() == true)
                {
                    if (rDmg > target.Health && rsolo)
                    {
                        _r.Cast(target);
                    }
                    else if (ComboDamage(target) > target.Health && rrush)
                    {
                        _r.Cast(target);
                    }
                }
            }
            if (!autoR) return;
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget(_r.Range)))
            {
                var fuckr = _r.GetPrediction(hero, true);
                if (fuckr.AoeTargetsHitCount >= _config.Item("MinTargets").GetValue<Slider>().Value &&
                    _r.GetPrediction(hero).Hitchance >= HitChance.High)
                {
                    _r.Cast(hero, false, true);

                }
            }
        }

        private static void Farm_skills(Spell spell, bool skillshot = false)
        {
            if (!_q.IsReady())
                return;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            foreach (var minion in allMinions)
            {
                if (!minion.IsValidTarget())
                    continue;
                var minionInRangeAa = Orbwalking.InAutoAttackRange(minion);
                var minionInRangeSpell = minion.Distance(ObjectManager.Player) <= spell.Range;
                var minionKillableAa = _player.GetAutoAttackDamage(minion, true) - 30 >= minion.Health;
                var minionKillableSpell = _player.GetSpellDamage(minion, SpellSlot.Q) - 30 >= minion.Health;
                var lastHit = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit;
                var laneClear = _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear;
                if ((lastHit && minionInRangeSpell && minionKillableSpell) &&
                    _q.GetPrediction(minion).CollisionObjects.Count > 0 && skillshot)
                {
                    spell.Cast(minion.Position);
                }
                if (laneClear && minionInRangeSpell)
                {
                    if (minionKillableSpell && _q.GetPrediction(minion).CollisionObjects.Count > 0 && skillshot)
                        spell.Cast(minion.Position);
                    else
                        spell.Cast(minion);
                }
            }
        }

        private static void Usecleanse()
        {
            if (_player.IsDead ||
                (_config.Item("Cleansemode").GetValue<StringList>().SelectedIndex == 1 &&
                 !_config.Item("ActiveCombo").GetValue<KeyBind>().Active)) return;
            if (Cleanse(_player) && _config.Item("useqss").GetValue<bool>())
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140))
                        Utility.DelayAction.Add(500, () => Items.UseItem(3140));
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139))
                        Utility.DelayAction.Add(500, () => Items.UseItem(3139));
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137))
                        Utility.DelayAction.Add(500, () => Items.UseItem(3137));
                }
                else
                {
                    if (Items.HasItem(3140) && Items.CanUseItem(3140)) Items.UseItem(3140);
                    else if (Items.HasItem(3139) && Items.CanUseItem(3139)) Items.UseItem(3139);
                    else if (Items.HasItem(3137) && Items.CanUseItem(3137)) Items.UseItem(3137);
                }
            }
        }

        private static bool Cleanse(Obj_AI_Hero hero)
        {
            bool cc = false;
            if (_config.Item("blind").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Blind))
                {
                    cc = true;
                }
            }
            if (_config.Item("charm").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Charm))
                {
                    cc = true;
                }
            }
            if (_config.Item("fear").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Fear))
                {
                    cc = true;
                }
            }
            if (_config.Item("flee").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Flee))
                {
                    cc = true;
                }
            }
            if (_config.Item("snare").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Snare))
                {
                    cc = true;
                }
            }
            if (_config.Item("taunt").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Taunt))
                {
                    cc = true;
                }
            }
            if (_config.Item("suppression").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Suppression))
                {
                    cc = true;
                }
            }
            if (_config.Item("stun").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Stun))
                {
                    cc = true;
                }
            }
            if (_config.Item("polymorph").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Polymorph))
                {
                    cc = true;
                }
            }
            if (_config.Item("silence").GetValue<bool>())
            {
                if (hero.HasBuffOfType(BuffType.Silence))
                {
                    cc = true;
                }
            }
            if (_config.Item("zedultexecute").GetValue<bool>())
            {
                if (_player.HasBuff("zedulttargetmark"))
                {
                    cc = true;
                }
            }
           return cc;
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
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Utility.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Gray,
                        _config.Item("CircleThickness").GetValue<Slider>().Value,
                        _config.Item("CircleQuality").GetValue<Slider>().Value);
                }
            }
            else
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawW").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }

                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }
            }
        }
    }
}
    

     