using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace D_Kayle
{
    internal class Program
    {
        private const string ChampionName = "Kayle";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r;

        private static readonly List<Spell> SpellList = new List<Spell>();

        private static SpellSlot _igniteSlot;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static Int32 _lastSkin;

        private static SpellSlot _smiteSlot = SpellSlot.Unknown;

        private static Spell _smite;

        private static Items.Item _rand, _lotis, _frostqueen, _mikael;
        //Credits to Kurisu
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
            if (ObjectManager.Player.BaseSkinName != ChampionName)
            {
                Game.PrintChat("Please use Kayle~");
                return;
            }


            _q = new Spell(SpellSlot.Q, 650f);
            _w = new Spell(SpellSlot.W, 900f);
            _e = new Spell(SpellSlot.E, 675f);
            _r = new Spell(SpellSlot.R, 900f);

            SetSmiteSlot();

            SpellList.Add(_q);
            SpellList.Add(_w);
            SpellList.Add(_e);
            SpellList.Add(_r);

            _rand = new Items.Item(3143, 490f);
            _lotis = new Items.Item(3190, 590f);
            _frostqueen = new Items.Item(3092, 800f);
            _mikael = new Items.Item(3222, 600f);

            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            //D Nidalee
            _config = new Menu("D-Kayle", "D-Kayle", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

            //Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("smitecombo", "Use Smite on Target")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));


            _config.AddSubMenu(new Menu("items", "items"));
            _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("frostQ", "Use Frost Queen")).SetValue(true);
            _config.SubMenu("items").AddSubMenu(new Menu("Defensive", "Defensive"));
            _config.SubMenu("items").SubMenu("Defensive").AddItem(new MenuItem("Omen", "Use Randuin's Omen")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").AddItem(new MenuItem("Omenenemys", "Use Randuin's if Targets >=").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("items").SubMenu("Defensive").AddItem(new MenuItem("lotis", "Use Iron Solari")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").AddItem(new MenuItem("lotisminhp", "Use Solari if Ally % HP <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items").SubMenu("Defensive").AddSubMenu(new Menu("Cleanse", "Cleanse"));
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddSubMenu(new Menu("Mikael's Crucible", "mikael"));
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("usemikael", "Use Mikael's to Remove Debuffs")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("mikaelusehp", "Use Mikael's if Ally % HP <").SetValue(new Slider(25, 1, 100)));
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly))
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("mikaeluse" + hero.BaseSkinName, hero.BaseSkinName).SetValue(true));
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("useqss", "Use QSS/Mercurial Scimitar/Dervish Blade")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("blind", "Blind")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("charm", "Charm")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("fear", "Fear")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("flee", "Flee")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("snare", "Snare")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("taunt", "Taunt")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("suppression", "Suppression")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("stun", "Stun")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("polymorph", "Polymorph")).SetValue(false);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("silence", "Silence")).SetValue(false);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("zedultexecute", "Zed's Death Mark")).SetValue(true);
            _config.SubMenu("items").SubMenu("Defensive").SubMenu("Cleanse").AddItem(new MenuItem("Cleansemode", "")).SetValue(new StringList(new string[2] { "Cleanse Always", "Cleanse in Combo" }));

            _config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usehppotions", "Use Health Potion/Flask/Biscuit")).SetValue(true);
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usepotionhp", "If % HP <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usemppotions", "Use Mana Potion/Flask/Biscuit")).SetValue(true);
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usepotionmp", "If % Mana <").SetValue(new Slider(35, 1, 100)));

            //utilities
            _config.AddSubMenu(new Menu("Utilities", "utilities"));
            _config.SubMenu("utilities").AddItem(new MenuItem("onmeW", "Use W on Self")).SetValue(true);
            _config.SubMenu("utilities").AddItem(new MenuItem("healper", "Self % HP")).SetValue(new Slider(40, 1, 100));
            _config.SubMenu("utilities").AddItem(new MenuItem("onmeR", "Use R on Self")).SetValue(true);
            _config.SubMenu("utilities").AddItem(new MenuItem("ultiSelfHP", "Self % HP")).SetValue(new Slider(40, 1, 100));

            _config.SubMenu("utilities").AddSubMenu(new Menu("Use W on Ally", "Use W Ally"));
             _config.SubMenu("utilities").SubMenu("Use W Ally").AddItem(new MenuItem("allyW", "Use W on Ally")).SetValue(true);
             _config.SubMenu("utilities").SubMenu("Use W Ally").AddItem(new MenuItem("allyhealper", "Ally % HP")).SetValue(new Slider(40, 1, 100));
             foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe))
             _config.SubMenu("utilities").SubMenu("Use W Ally").AddItem(new MenuItem("usewally" + hero.BaseSkinName, hero.BaseSkinName).SetValue(true));

             _config.SubMenu("utilities").AddSubMenu(new Menu("Use R on Ally", "Use R Ally"));
             _config.SubMenu("utilities").SubMenu("Use R Ally").AddItem(new MenuItem("allyR", "Use R on Ally")).SetValue(true);
             _config.SubMenu("utilities").SubMenu("Use R Ally").AddItem(new MenuItem("ultiallyHP", "Ally % HP")).SetValue(new Slider(40, 1, 100));
             foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe))
                _config.SubMenu("utilities").SubMenu("Use R Ally").AddItem(new MenuItem("userally" + hero.BaseSkinName, hero.BaseSkinName).SetValue(true));

            //Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("UseEHarass", "Use E")).SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "Auto-Harass (Toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass").SetValue(new KeyBind("X".ToCharArray()[0],
                        KeyBindType.Press)));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));

            //Farm
            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lane Clear", "Laneclear"));
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("UseQLane", "Use Q")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("UseELane", "Use E")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("Farmmana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm").SubMenu("Laneclear").AddItem(new MenuItem("Activelane", "Lane Clear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Lasthit", "Lasthit"));
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseQLast", "Use Q")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("UseELast", "Use E")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("lasthitmana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm").SubMenu("Lasthit").AddItem(new MenuItem("activelast", "Last Hit").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Jungleclear", "Jungleclear"));
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseQjungle", "Use Q")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("UseEjungle", "Use E")).SetValue(true);
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("junglemana", "Min. % Mana").SetValue(new Slider(60, 1, 100)));
            _config.SubMenu("Farm").SubMenu("Jungleclear").AddItem(new MenuItem("Activejungle", "Jungle Clear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
           
            //Smite 
            _config.AddSubMenu(new Menu("Smite", "Smite"));
            _config.SubMenu("Smite").AddItem(new MenuItem("Usesmite", "Use Smite (Toggle)").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Toggle)));
            _config.SubMenu("Smite").AddItem(new MenuItem("Useblue", "Smite Blue-Camp Early")).SetValue(true);
            _config.SubMenu("Smite").AddItem(new MenuItem("manaJ", "Smite Blue-Camp if % MP <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("Smite").AddItem(new MenuItem("Usered", "Smite Red-Camp Early")).SetValue(true);
            _config.SubMenu("Smite").AddItem(new MenuItem("healthJ", "Smite Red-Camp if % HP <").SetValue(new Slider(35, 1, 100)));

            //Kill Steal
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc").AddItem(new MenuItem("UseQKs", "Use Q to Killsteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("UseIgnite", "Use Ignite to Killsteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("skinKa", "Use Custom Skin").SetValue(false));
            _config.SubMenu("Misc").AddItem(new MenuItem("skinKayle", "Skin Changer").SetValue(new Slider(4, 1, 8)));
            _config.SubMenu("Misc").AddItem(new MenuItem("GapCloserE", "Use Q against Gapcloser")).SetValue(true);
            _config.SubMenu("Misc")
                .AddItem(
                    new MenuItem("Escape", "Escape").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            _config.SubMenu("Misc").AddItem(new MenuItem("support", "Support Mode")).SetValue(false);

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
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            _config.SubMenu("Drawings").AddItem(new MenuItem("Drawsmite", "Draw Smite")).SetValue(true);
            _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag-Free Circles").SetValue(true));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circle Quality").SetValue(new Slider(100, 100, 10)));
            _config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circle Thickness").SetValue(new Slider(1, 10, 1)));

            _config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

            //Game.PrintChat("<font color='#881df2'>D-Kayle By Diabaths </font>Loaded!");
            if (_config.Item("skinKa").GetValue<bool>())
            {
                GenModelPacket(_player.ChampionName, _config.Item("skinKayle").GetValue<Slider>().Value);
                _lastSkin = _config.Item("skinKayle").GetValue<Slider>().Value;
            }
            //Game.PrintChat(
            //   "<font color='#FF0000'>If You like my work and want to support me,  plz donate via paypal in </font> <font //color='#FF9900'>ssssssssssmith@hotmail.com</font> (10) S");
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            _player = ObjectManager.Player;
            _orbwalker.SetAttack(true);
            if (_config.Item("skinKa").GetValue<bool>() && SkinChanged())
            {
                GenModelPacket(_player.ChampionName, _config.Item("skinKayle").GetValue<Slider>().Value);
                _lastSkin = _config.Item("skinKayle").GetValue<Slider>().Value;
            }
            if (_config.Item("Escape").GetValue<KeyBind>().Active)
            {
                Escape();
            }
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (!_config.Item("ActiveCombo").GetValue<KeyBind>().Active && (_config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 _config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                 (100 * (_player.Mana / _player.MaxMana)) > _config.Item("Harrasmana").GetValue<Slider>().Value)
            {
               Harass();
            }
            if (_config.Item("activelast").GetValue<KeyBind>().Active &&
                (100 * (_player.Mana / _player.MaxMana)) > _config.Item("lasthitmana").GetValue<Slider>().Value)
            {
                Lasthit();
            }
            if (_config.Item("Activelane").GetValue<KeyBind>().Active &&
                (100 * (_player.Mana / _player.MaxMana)) > _config.Item("Farmmana").GetValue<Slider>().Value)
            {
               Farm();
            }
            if (_config.Item("Activejungle").GetValue<KeyBind>().Active &&
                (100 * (_player.Mana / _player.MaxMana)) > _config.Item("junglemana").GetValue<Slider>().Value)
            {
                JungleFarm();
            }
            Usepotion();
            if (_config.Item("Usesmite").GetValue<KeyBind>().Active)
            {
                Smiteuse();
            }
            AutoW();
            AutoR();
            AllyR();
            AllyW();
            KillSteal();
            Usecleanse();
        }

        private static void UseItemes()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var iOmen = _config.Item("Omen").GetValue<bool>();
                var iOmenenemys = hero.CountEnemysInRange(450) >= _config.Item("Omenenemys").GetValue<Slider>().Value;
                var ifrost = _config.Item("frostQ").GetValue<bool>();

                if (ifrost && _frostqueen.IsReady() && hero.IsValidTarget(_frostqueen.Range))
                {
                    _frostqueen.Cast();

                }
                if (iOmenenemys && iOmen && _rand.IsReady() && hero.IsValidTarget(_rand.Range))
                {
                    _rand.Cast();

                }
            }
            var ilotis = _config.Item("lotis").GetValue<bool>();
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth*(_config.Item("lotisminhp").GetValue<Slider>().Value)/100) &&
                        hero.Distance(_player.ServerPosition) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
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
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => (hero.IsAlly || hero.IsMe)))
            {
                var usemikael = _config.Item("usemikael").GetValue<bool>();
                var mikaeluse = hero.Health <=
                                (hero.MaxHealth*(_config.Item("mikaelusehp").GetValue<Slider>().Value)/100);
                if (((Cleanse(hero) && usemikael) || mikaeluse) && _config.Item("mikaeluse" + hero.BaseSkinName) != null &&
                    _config.Item("mikaeluse" + hero.BaseSkinName).GetValue<bool>() == true)
                {
                    if (_mikael.IsReady() && hero.Distance(_player.ServerPosition) <= _mikael.Range)
                    {
                        if (_player.HasBuff("zedulttargetmark"))
                            Utility.DelayAction.Add(500, () => _mikael.Cast(hero));
                        else
                            _mikael.Cast(hero);
                    }
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
        // princer007  Code
        static int Getallies(float range)
        {
            int allies = 0;
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.IsAlly && !hero.IsMe && _player.Distance(hero) <= range) allies++;
            return allies;
        }
        static void Orbwalking_BeforeAttack(LeagueSharp.Common.Orbwalking.BeforeAttackEventArgs args)
        {
            if (Getallies(1000) > 0 && ((Obj_AI_Base)_orbwalker.GetTarget()).IsMinion && /*args.Unit.IsMinion &&*/ _config.Item("support").GetValue<bool>()) args.Process = false;
        }
        private static void Smiteontarget()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var usesmite = _config.Item("smitecombo").GetValue<bool>();
                var itemscheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
                if (itemscheck && usesmite &&
                    ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) == SpellState.Ready &&
                    hero.IsValidTarget(_smite.Range))
                {
                    ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, hero);
                }
            }
        }
        private static void GenModelPacket(string champ, int skinId)
        {
            Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(_player.NetworkId, skinId, champ))
                .Process();
        }

        private static bool SkinChanged()
        {
            return (_config.Item("skinKayle").GetValue<Slider>().Value != _lastSkin);
        }

        private static bool Packets()
        {
            return _config.Item("usePackets").GetValue<bool>();
        }

        private static void AutoR()
        {
            if (_player.HasBuff("Recall") || ObjectManager.Player.InFountain()) return;
            if (_config.Item("onmeR").GetValue<bool>() && _config.Item("onmeR").GetValue<bool>() &&
                (_player.Health / _player.MaxHealth) * 100 <= _config.Item("ultiSelfHP").GetValue<Slider>().Value &&
                _r.IsReady() && ObjectManager.Player.CountEnemiesInRange(650) > 0)
            {
                _r.Cast(_player);
            }
        }

        private static void AllyR()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe))
            {
                if (_player.HasBuff("Recall") || ObjectManager.Player.InFountain()) return;
                if (_config.Item("allyR").GetValue<bool>() &&
                    (hero.Health / hero.MaxHealth) * 100 <= _config.Item("ultiallyHP").GetValue<Slider>().Value &&
                    _r.IsReady() && ObjectManager.Player.CountEnemiesInRange(1000) > 0 &&
                    hero.Distance(_player.ServerPosition) <= _r.Range)
                    if (_config.Item("userally" + hero.BaseSkinName) != null &&
                    _config.Item("userally" + hero.BaseSkinName).GetValue<bool>() == true)
                {
                    _r.Cast(hero);
                }
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
                (mobs.Count > 0 && _config.Item("Activejungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
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
        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var itemscheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_e.IsReady() || ObjectManager.Player.HasBuff("JudicatorRighteousFury"))
            {
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
                damage = damage + _player.GetAutoAttackDamage(enemy, true)*4;
            }
            if (itemscheck &&
                ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) == SpellState.Ready)
            {
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Smite);
            }
            if (ObjectManager.Player.HasBuff("LichBane"))
            {
                damage += _player.BaseAttackDamage * 0.75 + _player.FlatMagicDamageMod * 0.5;
            }

            return (float) damage;
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_r.Range + 200, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                Smiteontarget();
                UseItemes();
                if (target.IsValidTarget(600) && _config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
               _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (ComboDamage(target) > target.Health -100)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                }
                if (_config.Item("UseQCombo").GetValue<bool>() && _q.IsReady() && target.IsValidTarget(_q.Range))
                {
                    _q.Cast(target, Packets());

                }
                if (_config.Item("UseECombo").GetValue<bool>() && _e.IsReady() && target.CountEnemysInRange(650) > 0)
                {
                    _e.Cast();
                }

                if (_w.IsReady() && _config.Item("UseWCombo").GetValue<bool>() && target.IsValidTarget(_q.Range))
                {
                    _w.Cast(_player);
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_q.IsReady() && gapcloser.Sender.IsValidTarget(_q.Range) &&
                _config.Item("GapCloserE").GetValue<bool>())
            {
                _q.Cast(gapcloser.Sender, Packets());
            }
        }

        private static void Escape()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
            _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (_player.Spellbook.CanUseSpell(SpellSlot.W) == SpellState.Ready && _player.IsMe)
            {
                if (_w.IsReady() && ObjectManager.Player.CountEnemiesInRange(1200) > 0)
                {
                    _player.Spellbook.CastSpell(SpellSlot.W, _player);
                }
            }
            if (target.IsValidTarget(_q.Range) && _q.IsReady())
            {
                _q.Cast(target);
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget(_q.Range) && _q.IsReady() && _config.Item("UseQHarass").GetValue<bool>())
            {
                _q.Cast(target, Packets());
            }

            if (target.IsValidTarget(_q.Range) && _e.IsReady() &&
               _config.Item("UseEHarass").GetValue<bool>())
                _e.Cast();
        }


        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;
            var minions = MinionManager.GetMinions(_player.ServerPosition, _q.Range);
            foreach (var minion in minions)
            {
                if (_config.Item("UseQLane").GetValue<bool>() && _q.IsReady())
                {
                    if (minions.Count > 2)
                    {
                        _q.Cast(minion, Packets());

                    }
                    else
                        foreach (var minionQ in minions)
                            if (!Orbwalking.InAutoAttackRange(minion) &&
                                minionQ.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                                _q.Cast(minionQ, Packets());
                }
                if (_config.Item("UseELane").GetValue<bool>() && _e.IsReady())
                {
                    if (minions.Count > 2)
                    {
                        _e.Cast();

                    }
                    else
                        foreach (var minionE in minions)
                            if (!Orbwalking.InAutoAttackRange(minion) &&
                                minionE.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                                _e.Cast();
                }
            }
        }

        private static void Lasthit()
        {
            if (!Orbwalking.CanMove(40)) return;
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var useQ = _config.Item("UseQLast").GetValue<bool>();
            var useE = _config.Item("UseELast").GetValue<bool>();
            foreach (var minion in allMinions)
            {
                if (useQ && _q.IsReady() && minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion, Packets());
                }

                if (_e.IsReady() && useE && minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E) &&
                    allMinions.Count > 2)
                {
                    _e.Cast();
                }
            }
        }

        private static void JungleFarm()
        {
            if (!Orbwalking.CanMove(40)) return;
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);
          
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (_config.Item("UseQjungle").GetValue<bool>() && _q.IsReady())
                {
                    _q.Cast(mob, Packets());
                }
                if (_config.Item("UseQjungle").GetValue<bool>() && _e.IsReady())
                {
                    _e.Cast();
                }
            }
        }

        private static void AutoW()
        {
            if (_player.Spellbook.CanUseSpell(SpellSlot.W) == SpellState.Ready && _player.IsMe)
            {

                if (_player.HasBuff("Recall") || ObjectManager.Player.InFountain()) return;

                if (_config.Item("onmeW").GetValue<bool>() && _w.IsReady() &&
                    _player.Health <= (_player.MaxHealth * (_config.Item("healper").GetValue<Slider>().Value) / 100))
                {
                    _player.Spellbook.CastSpell(SpellSlot.W, _player);
                }
            }
        }

        private static void AllyW()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly && !hero.IsMe))
            {
                if (_player.HasBuff("Recall") || hero.HasBuff("Recall") || ObjectManager.Player.InFountain()) return;
                if (_config.Item("allyW").GetValue<bool>() && 
                    (hero.Health / hero.MaxHealth) * 100 <= _config.Item("allyhealper").GetValue<Slider>().Value &&
                    _w.IsReady() && ObjectManager.Player.CountEnemiesInRange(1200) > 0 &&
                    hero.Distance(_player.ServerPosition) <= _w.Range )
                    if (_config.Item("usewally" + hero.BaseSkinName) != null &&
                    _config.Item("usewally" + hero.BaseSkinName).GetValue<bool>() == true)
                {
                    _w.Cast(hero);
                }
            }
        }

        private static void KillSteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var igniteDmg = _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
                var qhDmg = _player.GetSpellDamage(hero, SpellSlot.Q);

                if (hero.IsValidTarget(600) && _config.Item("UseIgnite").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                    _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (igniteDmg > hero.Health)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, hero);
                    }
                }

                if (_q.IsReady() && hero.IsValidTarget(_q.Range)  &&
                    _config.Item("UseQKs").GetValue<bool>())
                {
                    if (hero.Health <= qhDmg)
                    {
                        _q.Cast(hero, Packets());
                    }
                }
            }
        }

        //Credits to Kurisu
        private static string Smitetype()
        {
            if (SmiteBlue.Any(i => Items.HasItem(i)))
            {
                return "s5_summonersmiteplayerganker";
            }
            if (SmiteRed.Any(i => Items.HasItem(i)))
            {
                return "s5_summonersmiteduel";
            }
            if (SmiteGrey.Any(i => Items.HasItem(i)))
            {
                return "s5_summonersmitequick";
            }
            if (SmitePurple.Any(i => Items.HasItem(i)))
            {
                return "itemsmiteaoe";
            }
            return "summonersmite";
        }

        //Credits to metaphorce
        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => String.Equals(spell.Name, Smitetype(), StringComparison.CurrentCultureIgnoreCase)))
            {
                _smiteSlot = spell.Slot;
                _smite = new Spell(_smiteSlot, 700);
                return;
            }
        }
        private static int GetSmiteDmg()
        {
            int level = _player.Level;
            int index = _player.Level / 5;
            float[] dmgs = { 370 + 20 * level, 330 + 30 * level, 240 + 40 * level, 100 + 50 * level };
            return (int)dmgs[index];
        }

        //New map Monsters Name By SKO
        private static void Smiteuse()
        {
            var jungle = _config.Item("Activejungle").GetValue<KeyBind>().Active;
            if (ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) != SpellState.Ready) return;
            var useblue = _config.Item("Useblue").GetValue<bool>();
            var usered = _config.Item("Usered").GetValue<bool>();
            var health = (100 * (_player.Mana / _player.MaxMana)) < _config.Item("healthJ").GetValue<Slider>().Value;
            var mana = (100 * (_player.Mana / _player.MaxMana)) < _config.Item("manaJ").GetValue<Slider>().Value;
            string[] jungleMinions;
            if (Utility.Map.GetMap().Type.Equals(Utility.Map.MapType.TwistedTreeline))
            {
                jungleMinions = new string[] { "TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf" };
            }
            else
            {
                jungleMinions = new string[]
                {
                    "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "SRU_Dragon",
                    "SRU_Baron", "Sru_Crab"
                };
            }
            var minions = MinionManager.GetMinions(_player.Position, 1000, MinionTypes.All, MinionTeam.Neutral);
            if (minions.Count() > 0)
            {
                int smiteDmg = GetSmiteDmg();

                foreach (Obj_AI_Base minion in minions)
                {
                    if (Utility.Map.GetMap().Type.Equals(Utility.Map.MapType.TwistedTreeline) &&
                        minion.Health <= smiteDmg &&
                        jungleMinions.Any(name => minion.Name.Substring(0, minion.Name.Length - 5).Equals(name)))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, minion);
                    }
                    if (minion.Health <= smiteDmg && jungleMinions.Any(name => minion.Name.StartsWith(name)) &&
                        !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, minion);
                    }
                    else if (jungle && useblue && mana && minion.Health >= smiteDmg &&
                             jungleMinions.Any(name => minion.Name.StartsWith("SRU_Blue")) &&
                             !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, minion);
                    }
                    else if (jungle && usered && health && minion.Health >= smiteDmg &&
                             jungleMinions.Any(name => minion.Name.StartsWith("SRU_Red")) &&
                             !jungleMinions.Any(name => minion.Name.Contains("Mini")))
                    {
                        ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, minion);
                    }
                }
            }
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_config.Item("Drawsmite").GetValue<bool>())
            {
                if (_config.Item("Usesmite").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawText(Drawing.Width * 0.90f, Drawing.Height * 0.68f, System.Drawing.Color.DarkOrange,
                        "Smite Is On");
                }
                else
                    Drawing.DrawText(Drawing.Width * 0.90f, Drawing.Height * 0.68f, System.Drawing.Color.DarkRed,
                        "Smite Is Off");
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
