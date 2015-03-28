using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace D_Zyra
{
    class Program
    {
        private const string ChampionName = "Zyra";

        private static Orbwalking.Orbwalker _orbwalker;

        private static Spell _q, _w, _e, _r, _passive;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static Int32 _lastSkin;

        private static SpellSlot _igniteSlot;

        private static Items.Item _rand, _lotis, _youmuu, _blade, _bilge, _hextech, _frostqueen, _mikael;

        private static SpellSlot _smiteSlot = SpellSlot.Unknown;

        private static Spell _smite;

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
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 800);
            _w = new Spell(SpellSlot.W, 825);
            _e = new Spell(SpellSlot.E, 1100);
            _r = new Spell(SpellSlot.R, 700);
            _passive = new Spell(SpellSlot.Q, 1470);

            _q.SetSkillshot(0.6f, 85f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _e.SetSkillshot(0.5f, 70f, 1150f, false, SkillshotType.SkillshotLine);
            _r.SetSkillshot(0.5f, 500f, 20f, false, SkillshotType.SkillshotCircle);
            _passive.SetSkillshot(0.5f, 70f, 1400f, false, SkillshotType.SkillshotLine);

            _igniteSlot = _player.GetSpellSlot("SummonerDot");

            _hextech = new Items.Item(3146, 700);
            _youmuu = new Items.Item(3142, 10);
            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);
            _rand = new Items.Item(3143, 490f);
            _lotis = new Items.Item(3190, 590f);
            _frostqueen = new Items.Item(3092, 800f);
            _mikael =new Items.Item(3222, 600f);
            SetSmiteSlot();

            //D Zyra
            _config = new Menu("D-Zyra", "D-Zyra", true);

           //TargetSelector
           var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
           TargetSelector.AddToMenu(targetSelectorMenu);
           _config.AddSubMenu(targetSelectorMenu);


            //Orbwalker
            _config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));


            //Combo useignite
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("smitecombo", "Use Smite")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useignite", "Use Ignite")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useQC", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW_Passive", "Plant on Q").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("useEC", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useWE_Passive", "Plant on E").SetValue(true));
            _config.SubMenu("Combo").AddItem(new MenuItem("use_ulti", "Use R If Killable")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("UseRE", "Use AutoR")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("MinTargets", "AutoR if Min Targets >=").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));

            _config.AddSubMenu(new Menu("items", "items"));
            _config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "Use Youmuu's")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hextech", "Hextech Gunblade")).SetValue(true);
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("HextechEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hextechmyhp", "Or Your  Hp <").SetValue(new Slider(85, 1, 100)));
            _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("frostQ", "Use Frost Queen")).SetValue(true);

            _config.SubMenu("items").AddSubMenu(new Menu("Deffensive", "Deffensive"));
            _config.SubMenu("items").SubMenu("Deffensive").AddItem(new MenuItem("Omen", "Use Randuin Omen")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").AddItem(new MenuItem("Omenenemys", "Randuin if enemys>").SetValue(new Slider(2, 1, 5)));
            _config.SubMenu("items").SubMenu("Deffensive").AddItem(new MenuItem("lotis", "Use Iron Solari")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").AddItem(new MenuItem("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items").SubMenu("Deffensive").AddSubMenu(new Menu("Cleanse", "Cleanse"));
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddSubMenu(new Menu("Mikael's Crucible", "mikael"));
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("usemikael", "Use Mikael's to remove Debuffs")).SetValue(true);
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("mikaelusehp", "Or Use if Mikael's Ally Hp <%").SetValue(new Slider(25, 1, 100)));
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => (hero.IsAlly|| hero.IsMe)))
                _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").SubMenu("mikael").AddItem(new MenuItem("mikaeluse" + hero.BaseSkinName, hero.BaseSkinName).SetValue(true));
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
            _config.SubMenu("items").SubMenu("Deffensive").SubMenu("Cleanse").AddItem(new MenuItem("Cleansemode", "")).SetValue(new StringList(new string[2] { "Cleanse Always", "Cleanse in Combo" }));

            _config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usehppotions", "Use Healt potion/Flask/Biscuit")).SetValue(true);
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usepotionhp", "If Health % <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usemppotions", "Use Mana potion/Flask/Biscuit")).SetValue(true);
            _config.SubMenu("items").SubMenu("Potions").AddItem(new MenuItem("usepotionmp", "If Mana % <").SetValue(new Slider(35, 1, 100)));

            //harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("useQH", "Use Q").SetValue(true));
            _config.SubMenu("Harass").AddItem(new MenuItem("useW_Passiveh", "Plant on Q").SetValue(true));
            _config.SubMenu("Harass").AddItem(new MenuItem("useEH", "Use E").SetValue(true));
            _config.SubMenu("Harass").AddItem(new MenuItem("useWE_Passiveh", "Plant on E").SetValue(true));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("harassmana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "AutoHarass (toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //Farm
            _config.AddSubMenu(new Menu("Farm", "Farm"));
            _config.SubMenu("Farm").AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("useQL", "Use Q").SetValue(true));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("useW_Passivel", "Plant on Q").SetValue(true));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("useEL", "Use E").SetValue(true));
            _config.SubMenu("Farm").SubMenu("LaneClear").AddItem(new MenuItem("useWE_Passivel", "Plant on E").SetValue(true));
            
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(new MenuItem("lanemana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("LaneClear")
                .AddItem(
                    new MenuItem("Activelane", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            _config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("useQJ", "Use Q").SetValue(true));
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("useW_Passivej", "Plant on Q").SetValue(true));
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("useEJ", "Use E").SetValue(true));
            _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("useWE_Passivej", "Plant on E").SetValue(true));
            _config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("junglemana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            //Smite ActiveJungle
            _config.AddSubMenu(new Menu("Smite", "Smite"));
            _config.SubMenu("Smite")
                .AddItem(
                    new MenuItem("Usesmite", "Use Smite(toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            _config.SubMenu("Smite").AddItem(new MenuItem("Useblue", "Smite Blue Early ")).SetValue(true);
            _config.SubMenu("Smite")
                .AddItem(new MenuItem("manaJ", "Smite Blue Early if MP% <").SetValue(new Slider(35, 1, 100)));
            _config.SubMenu("Smite").AddItem(new MenuItem("Usered", "Smite Red Early ")).SetValue(true);
            _config.SubMenu("Smite")
                .AddItem(new MenuItem("healthJ", "Smite Red Early if HP% <").SetValue(new Slider(35, 1, 100)));

            //Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Usepackes")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useQkill", "Q to Killsteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("useEkill", "E to Killsteal")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("Inter_E", "Interrupter E")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("Gap_E", "GapClosers E")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("usefrostq", "Frost Queen to GapClosers")).SetValue(true);
            _config.SubMenu("Misc").AddItem(new MenuItem("support", "Support Mode")).SetValue(false);

            //Damage after combo:
            MenuItem dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };
            //Draw
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DrawQ", "Draw Q").SetValue(true));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DrawW", "Draw W").SetValue(true));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DrawE", "Draw E").SetValue(true));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DrawR", "Draw R").SetValue(true));
            _config.SubMenu("Drawing").AddItem(dmgAfterComboItem);
            _config.SubMenu("Drawing").AddItem(new MenuItem("damagetest", "Damage Text")).SetValue(true);
            _config.SubMenu("Drawing").AddItem(new MenuItem("Drawsmite", "Draw smite")).SetValue(true);
            _config.SubMenu("Drawing").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));

            _config.AddToMainMenu();

            Game.PrintChat("<font color='#881df2'>D-Zyra by Diabaths </font> Loaded.");
            Game.PrintChat(
               "<font color='#FF0000'>If You like my work and want to support, and keep it always up to date plz donate via paypal in </font> <font color='#FF9900'>ssssssssssmith@hotmail.com</font> (10) S");
            Game.PrintChat(
               "<font color='#FF9900'>Please help me to buy new account(2x banned)</font>");
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;
        }

        private static void Game_OnUpdate(EventArgs args)
        {

            if (ZyraisZombie())
            {
                CastPassive();
                return;
            }
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if ((_config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 _config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                (100 * (_player.Mana / _player.MaxMana)) > _config.Item("harassmana").GetValue<Slider>().Value && !_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Harass();

            }
            if (_config.Item("Activelane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("lanemana").GetValue<Slider>().Value)
            {
                Laneclear();
            }
            if (_config.Item("ActiveJungle").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > _config.Item("junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            Usepotion();
            if (_config.Item("Usesmite").GetValue<KeyBind>().Active)
            {
                Smiteuse();
            }
            _player = ObjectManager.Player;

            _orbwalker.SetAttack(true);
            KillSteal();
            Usecleanse();

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
        private static float ComboDamage(Obj_AI_Hero hero)
        {
            var dmg = 0d;

            if (_q.IsReady())
            {
                if (_w.IsReady())
                {
                    dmg += _player.GetSpellDamage(hero, SpellSlot.Q) + (23 + 6.5*ObjectManager.Player.Level) +
                           (1.2*_player.FlatMagicDamageMod);
                }
                else dmg += _player.GetSpellDamage(hero, SpellSlot.Q);
            }

            if (_e.IsReady())
            {
                if (_w.IsReady())
                {
                    dmg += _player.GetSpellDamage(hero, SpellSlot.E) + (23 + 6.5*ObjectManager.Player.Level) +
                           (1.2*_player.FlatMagicDamageMod);
                }
                else dmg += _player.GetSpellDamage(hero, SpellSlot.E);
            }
            if (_r.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.R);
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
            dmg += _player.GetAutoAttackDamage(hero, true);
            return (float) dmg;
        }
        
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var pos = _e.GetPrediction(gapcloser.Sender).CastPosition;
            if (_config.Item("Gap_E").GetValue<bool>())
            {
                if (_e.IsReady() && gapcloser.Sender.IsValidTarget(_e.Range) && _w.IsReady())
                {
                    _e.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, Packets());
                    Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 2, pos.Y - 2, pos.Z), Packets()));
                    Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 2, pos.Y + 2, pos.Z), Packets()));
                }
                else if (_e.IsReady() && gapcloser.Sender.IsValidTarget(_e.Range))
                {
                    _e.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, Packets());
                }
            }
            if (Items.HasItem(3092) && Items.CanUseItem(3092) && _config.Item("usefrostq").GetValue<bool>() &&
                gapcloser.Sender.IsValidTarget(800))
            {
                _frostqueen.Cast(gapcloser.Sender);
            }
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            var pos = _e.GetPrediction(unit).CastPosition;
            if (!_config.Item("Inter_E").GetValue<bool>()) return;
            if ( _e.IsReady() && unit.IsValidTarget(_e.Range) && _w.IsReady())
            {
                _e.CastIfHitchanceEquals(unit, HitChance.High, Packets());
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 2, pos.Y - 2, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 2, pos.Y + 2, pos.Z), Packets()));
            }
            else if (_e.IsReady() && unit.IsValidTarget(_e.Range))
            {
                _e.CastIfHitchanceEquals(unit, HitChance.High, Packets());
            }
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
        private static void Combo()
        {
            var useignite = _config.Item("useignite").GetValue<bool>();
            var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
            if (_config.Item("UseRE").GetValue<bool>() || _config.Item("use_ulti").GetValue<bool>())
                CastREnemy();
           UseItemes();
            Smiteontarget();
            if (useignite && _igniteSlot != SpellSlot.Unknown && target.IsValidTarget(600) &&
                   _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
               if (target.Health <= 1.2*ComboDamage(target))
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
          if (_config.Item("useQC").GetValue<bool>())
                CastQEnemy();
            if (_config.Item("useEC").GetValue<bool>())
                CastEEnemy();
           }

        private static void Harass()
        {
            if (_config.Item("useQH").GetValue<bool>())
                CastQEnemyharass();
            if (_config.Item("useEH").GetValue<bool>())
                CastEEnemyharass();
        }

        private static void Laneclear()
        {
            if (_config.Item("useQL").GetValue<bool>())
                CastQMinion();
            if (_config.Item("useEL").GetValue<bool>())
                CastEMinion();
        }

        private static void JungleClear()
        {
            if (_config.Item("useQJ").GetValue<bool>())
                CastQjungleMinion();
            if (_config.Item("useEJ").GetValue<bool>())
                CastEjungleMinion();
        }

        private static bool Packets()
        {
            return _config.Item("usePackets").GetValue<bool>();
        }

        private static bool ZyraisZombie()
        {
            return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Name ==
                   ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Name ||
                   ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name ==
                   ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name;
        }
        private static void CastEjungleMinion()
        {
            if (!_e.IsReady())
                return;
            var minions = MinionManager.GetMinions(_player.ServerPosition, _e.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;
            var castPostion =
                MinionManager.GetBestLineFarmLocation(
                    minions.Select(minion => minion.ServerPosition.To2D()).ToList(),
                    _e.Width, _e.Range);
           _e.Cast(castPostion.Position, Packets());
            if (_config.Item("useWE_Passivej").GetValue<bool>() && _w.IsReady())
            {
            var pos = _e.GetCircularFarmLocation(minions);
                Utility.DelayAction.Add(50, () => _w.Cast(pos.Position.To3D(), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(pos.Position.To3D(), Packets()));
            }
        }
        private static void CastEMinion()
        {
            if (!_e.IsReady())
                return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _e.Range, MinionTypes.All);
            if (minions.Count == 0)
                return;
            var castPostion =
                MinionManager.GetBestLineFarmLocation(
                    minions.Select(minion => minion.ServerPosition.To2D()).ToList(),
                    _e.Width, _e.Range);
            _e.Cast(castPostion.Position, Packets());
            if (_config.Item("useWE_Passivel").GetValue<bool>() && _w.IsReady())
            {
                var pos = _e.GetCircularFarmLocation(minions);
                Utility.DelayAction.Add(50, () => _w.Cast(pos.Position.To3D(), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(pos.Position.To3D(), Packets()));
            }
        }

       
        private static void CastQjungleMinion()
        {
            if (!_q.IsReady())
                return;
            var minions = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
               MinionTypes.All,
               MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (minions.Count == 0)
                return;
            var castPostion =
                MinionManager.GetBestCircularFarmLocation(
                    minions.Select(minion => minion.ServerPosition.To2D()).ToList(), _q.Width, _q.Range);
            _q.Cast(castPostion.Position, Packets());
            if (_config.Item("useW_Passivej").GetValue<bool>() && _w.IsReady())
            {
                var pos = castPostion.Position.To3D();
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
            }
        }
        private static void CastQMinion()
        {
           if (!_q.IsReady())
                return;
            var minions = MinionManager.GetMinions(ObjectManager.Player.Position, _q.Range + (_q.Width/2),
                 MinionTypes.All);
            if (minions.Count == 0)
                return;
            var castPostion =
                MinionManager.GetBestCircularFarmLocation(
                    minions.Select(minion => minion.ServerPosition.To2D()).ToList(), _q.Width, _q.Range);
            _q.Cast(castPostion.Position, Packets());
            if (_config.Item("useW_Passivel").GetValue<bool>() && _w.IsReady())
            {
                var pos = castPostion.Position.To3D();
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
            }
        }

        private static void CastREnemy()
        {
            var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
            var rpred = _r.GetPrediction(target, true);
            if (!target.IsValidTarget(_r.Range) || !_r.IsReady()) return;

            if (ComboDamage(target)*1.3> target.Health && _config.Item("use_ulti").GetValue<bool>() &&
                _r.GetPrediction(target).Hitchance >= HitChance.High)
            {
                _r.Cast(rpred.CastPosition, Packets());
            }
            if (ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(_r.Range)) >=
                _config.Item("MinTargets").GetValue<Slider>().Value
                && _r.GetPrediction(target).Hitchance >= HitChance.High)
            {
                _r.Cast(target);
            }
        }

        private static int GetNumberHitByR(Obj_AI_Hero target)
        {
            int Enemys = 0;
            foreach (Obj_AI_Hero enemys in ObjectManager.Get<Obj_AI_Hero>())
            {
                var pred = _r.GetPrediction(enemys, true);
                if (pred.Hitchance >= HitChance.High && !enemys.IsMe && enemys.IsEnemy && Vector3.Distance(_player.Position, pred.UnitPosition) <= _r.Range)
                {
                    Enemys = Enemys + 1;
                }
            }
            return Enemys;
        }

        private static void CastQEnemy()
        {
            if (!_q.IsReady())
                return;
            var target = TargetSelector.GetTarget(_q.Range + (_q.Width/2), TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget(_q.Range))
                return;
            _q.CastIfHitchanceEquals(target, HitChance.High, Packets());
            if (_w.IsReady() && _config.Item("useW_Passive").GetValue<bool>())
            {
                var pos = _q.GetPrediction(target).CastPosition;
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 2, pos.Y - 2, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 2, pos.Y + 2, pos.Z), Packets()));
            }
        }
        private static void CastQEnemyharass()
        {
            if (!_q.IsReady())
                return;
            var target = TargetSelector.GetTarget(_q.Range + (_q.Width / 2), TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget(_q.Range))
                return;
            _q.CastIfHitchanceEquals(target, HitChance.High, Packets());
           if (_w.IsReady() && _config.Item("useW_Passiveh").GetValue<bool>())
            {
                var pos = _q.GetPrediction(target).CastPosition;
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 2, pos.Y - 2, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 2, pos.Y + 2, pos.Z), Packets()));
            }
        }
        private static void CastEEnemy()
        {
            if (!_e.IsReady())
                return;
            var target = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget(_e.Range))
                return;
            _e.CastIfHitchanceEquals(target, HitChance.High, Packets());
            if (_w.IsReady() &&
             _config.Item("useWE_Passive").GetValue<bool>() )
            {
               var pos = _e.GetPrediction(target).CastPosition;
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
            }
        }
        private static void CastEEnemyharass()
        {
            if (!_e.IsReady())
                return;
            var target = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget(_e.Range))
                return;
            _e.CastIfHitchanceEquals(target, HitChance.High, Packets());
            if (_w.IsReady() &&
             _config.Item("useWE_Passiveh").GetValue<bool>())
            {
                var pos = _e.GetPrediction(target).CastPosition;
                Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
            }
        }
        private static void CastPassive()
        {
            if (!_passive.IsReady())
                return;
            var target = TargetSelector.GetTarget(_passive.Range, TargetSelector.DamageType.Magical);
            if (!target.IsValidTarget(_e.Range))
                return;
            _passive.CastIfHitchanceEquals(target, HitChance.High, Packets());
        }

        private static void KillSteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var useq = _config.Item("useQkill").GetValue<bool>();
                var usee = _config.Item("useEkill").GetValue<bool>();
                var whDmg = _player.GetSpellDamage(hero, SpellSlot.W);
                var qhDmg = _player.GetSpellDamage(hero, SpellSlot.Q);
                var ehDmg = _player.GetSpellDamage(hero, SpellSlot.E);
                var emana = _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
                var qmana = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
                if (useq && hero.IsValidTarget(_q.Range) && _q.IsReady())
                {
                    if (qhDmg >= hero.Health && qmana < _player.Mana)
                    {
                        _q.CastIfHitchanceEquals(hero, HitChance.High, Packets());

                    }
                    else if (qhDmg + whDmg > hero.Health && _player.Mana >= qmana && _w.IsReady())
                    {
                        _q.CastIfHitchanceEquals(hero, HitChance.High, Packets());
                        var pos = _e.GetPrediction(hero).CastPosition;
                        Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                        Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
                    }
                }
                if (usee && hero.IsValidTarget(_e.Range) && _e.IsReady())
                {
                    if (ehDmg >= hero.Health && emana < _player.Mana)
                    {
                        _e.CastIfHitchanceEquals(hero, HitChance.High, Packets());

                    }
                    else if (ehDmg + whDmg > hero.Health && _player.Mana >= emana && _w.IsReady())
                    {
                        _e.CastIfHitchanceEquals(hero, HitChance.High, Packets());
                        var pos = _e.GetPrediction(hero).CastPosition;
                        Utility.DelayAction.Add(50, () => _w.Cast(new Vector3(pos.X - 5, pos.Y - 5, pos.Z), Packets()));
                        Utility.DelayAction.Add(150, () => _w.Cast(new Vector3(pos.X + 5, pos.Y + 5, pos.Z), Packets()));
                    }
                }
            }
        }

        private static void UseItemes()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var iBilge = _config.Item("Bilge").GetValue<bool>();
                var iBilgeEnemyhp = hero.Health <=
                                    (hero.MaxHealth * (_config.Item("BilgeEnemyhp").GetValue<Slider>().Value) / 100);
                var iBilgemyhp = _player.Health <=
                                 (_player.MaxHealth*(_config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
                var iBlade = _config.Item("Blade").GetValue<bool>();
                var iBladeEnemyhp = hero.Health <=
                                    (hero.MaxHealth * (_config.Item("BladeEnemyhp").GetValue<Slider>().Value) / 100);
                var iBlademyhp = _player.Health <=
                                 (_player.MaxHealth*(_config.Item("Blademyhp").GetValue<Slider>().Value)/100);
                var iYoumuu = _config.Item("Youmuu").GetValue<bool>();
                var iHextech = _config.Item("Hextech").GetValue<bool>();
                var iHextechEnemyhp = hero.Health <=
                                      (hero.MaxHealth*(_config.Item("HextechEnemyhp").GetValue<Slider>().Value)/100);
                var iHextechmyhp = _player.Health <=
                                   (_player.MaxHealth*(_config.Item("Hextechmyhp").GetValue<Slider>().Value)/100);
                var iOmen = _config.Item("Omen").GetValue<bool>();
                var iOmenenemys =  hero.CountEnemysInRange(450) >= _config.Item("Omenenemys").GetValue<Slider>().Value;
                var ifrost = _config.Item("frostQ").GetValue<bool>();

                if (hero.IsValidTarget(450) && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
                {
                    _bilge.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
                {
                    _blade.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iYoumuu && _youmuu.IsReady())
                {
                    _youmuu.Cast();
                }
                if (hero.IsValidTarget(700) && iHextech && (iHextechEnemyhp || iHextechmyhp) && _hextech.IsReady())
                {
                    _hextech.Cast(hero);
                }
                if (iOmenenemys && iOmen && _rand.IsReady())
                {
                    _rand.Cast();

                }
                if (ifrost && _frostqueen.IsReady() && hero.IsValidTarget(_frostqueen.Range))
                {
                    _frostqueen.Cast(hero);

                }
            }
            var ilotis = _config.Item("lotis").GetValue<bool>();
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth * (_config.Item("lotisminhp").GetValue<Slider>().Value) / 100) &&
                        hero.Distance(_player.ServerPosition) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
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
                (mobs.Count > 0 && _config.Item("ActiveJungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
                  SmiteRed.Any(i => Items.HasItem(i)) || SmitePurple.Any(i => Items.HasItem(i)) ||
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
            var jungle = _config.Item("ActiveJungle").GetValue<KeyBind>().Active;
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
                                (hero.MaxHealth * (_config.Item("mikaelusehp").GetValue<Slider>().Value) / 100);
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

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (_config.Item("Drawsmite").GetValue<bool>() )
            {
                if (_config.Item("Usesmite").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, System.Drawing.Color.DarkOrange,
                        "Smite Is On");
                }
                else
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, System.Drawing.Color.DarkRed,
                        "Smite Is Off");
            }
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
       