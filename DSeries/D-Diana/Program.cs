using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace D_Diana
{
    internal class Program
    {
        private const string ChampionName = "Diana";
        private static Orbwalking.Orbwalker _orbwalker;
        private static Spell _q, _w, _e, _r;
        private static Obj_SpellMissile _qpos;
        private static bool _qcreated;
        public static Menu Config;
        public static Menu TargetSelectorMenu;
        private static Obj_AI_Hero _player;
        private static readonly List<Spell> SpellList = new List<Spell>();
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis;
        private static SpellSlot _smiteSlot = SpellSlot.Unknown;
        private static Spell _smite;
        //Credits to Kurisu
        private static readonly int[] SmitePurple = {3713, 3726, 3725, 3726, 3723};
        private static readonly int[] SmiteGrey = {3711, 3722, 3721, 3720, 3719};
        private static readonly int[] SmiteRed = {3715, 3718, 3717, 3716, 3714};
        private static readonly int[] SmiteBlue = {3706, 3710, 3709, 3708, 3707};

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (ObjectManager.Player.CharData.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 830f);
            _w = new Spell(SpellSlot.W, 200f);
            _e = new Spell(SpellSlot.E, 420f);
            _r = new Spell(SpellSlot.R, 825f);

            _q.SetSkillshot(0.35f, 200f, 1800, false, SkillshotType.SkillshotCircle);

            SpellList.Add(_q);
            SpellList.Add(_w);
            SpellList.Add(_e);
            SpellList.Add(_r);

            _bilge = new Items.Item(3144, 475f);
            _blade = new Items.Item(3153, 425f);
            _hydra = new Items.Item(3074, 250f);
            _tiamat = new Items.Item(3077, 250f);
            _rand = new Items.Item(3143, 490f);
            _lotis = new Items.Item(3190, 590f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");
            SetSmiteSlot();

            //D Diana
            Config = new Menu("D-Diana", "D-Diana", true);

            //TargetSelector
            TargetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(TargetSelectorMenu);
            Config.AddSubMenu(TargetSelectorMenu);

            //Orbwalker
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "Use Ignite(rush for it)")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("smitecombo", "Use Smite in target")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQCombo", "Use Q")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWCombo", "Use W")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseECombo", "Use E")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRCombo", "Use R")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRSecond", "Use Second R")).SetValue(false);
            Config.SubMenu("Combo").AddItem(new MenuItem("Normalcombo", "Q-R Combo")).SetValue(true);
            Config.Item("Normalcombo").ValueChanged += SwitchCombo;
            Config.SubMenu("Combo").AddItem(new MenuItem("Misayacombo", "R-Q Combo").SetValue(false));
            Config.Item("Misayacombo").ValueChanged += SwitchMisaya;
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
            //Config.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo2", "Combo2!").SetValue(new KeyBind(32, KeyBindType.Press)));


            //Items public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143, lotis = 3190;
            Config.AddSubMenu(new Menu("items", "items"));
            Config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
            Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Tiamat", "Use Tiamat")).SetValue(true);
            Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hydra", "Use Hydra")).SetValue(true);
            Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "Use Bilge")).SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BilgeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1)));
            Config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Bilgemyhp", "Or your Hp < ").SetValue(new Slider(85, 1)));
            Config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "Use Blade")).SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("BladeEnemyhp", "If Enemy Hp <").SetValue(new Slider(85, 1)));
            Config.SubMenu("items")
                .SubMenu("Offensive")
                .AddItem(new MenuItem("Blademyhp", "Or Your  Hp <").SetValue(new Slider(85, 1)));
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
                .AddItem(new MenuItem("lotisminhp", "Solari if Ally Hp<").SetValue(new Slider(35, 1)));
            Config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usehppotions", "Use Healt potion/Flask/Biscuit"))
                .SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionhp", "If Health % <").SetValue(new Slider(35, 1)));
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usemppotions", "Use Mana potion/Flask/Biscuit"))
                .SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionmp", "If Mana % <").SetValue(new Slider(35, 1)));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQHarass", "Use Q")).SetValue(true);
            Config.SubMenu("Harass").AddItem(new MenuItem("UseWHarass", "Use W")).SetValue(true);
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass key").SetValue(new KeyBind("X".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "Harass(toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("Harrasmana", "Minimum Mana").SetValue(new Slider(60, 1)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddSubMenu(new Menu("LastHit", "LastHit"));
            Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseWLH", "W LaneClear")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("LastHit")
                .AddItem(new MenuItem("lastmana", "Minimum Mana% >").SetValue(new Slider(35, 1)));
            Config.SubMenu("Farm")
                .SubMenu("LastHit")
                .AddItem(
                    new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Config.SubMenu("Farm").AddSubMenu(new Menu("Lane", "Lane"));
            Config.SubMenu("Farm").SubMenu("Lane").AddItem(new MenuItem("UseQLane", "Use Q")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("Lane").AddItem(new MenuItem("UseWLane", "Use W")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(
                    new MenuItem("ActiveLane", "Farm key").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Farm")
                .SubMenu("Lane")
                .AddItem(new MenuItem("Lanemana", "Minimum Mana").SetValue(new Slider(60, 1)));

            //jungle
            Config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJungle", "Use Q")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseWJungle", "Use W")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle key").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("Junglemana", "Minimum Mana").SetValue(new Slider(60, 1)));

            //Smite 
            Config.AddSubMenu(new Menu("Smite", "Smite"));
            Config.SubMenu("Smite")
                .AddItem(
                    new MenuItem("Usesmite", "Use Smite(toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Smite").AddItem(new MenuItem("Useblue", "Smite Blue Early ")).SetValue(true);
            Config.SubMenu("Smite")
                .AddItem(new MenuItem("manaJ", "Smite Blue Early if MP% <").SetValue(new Slider(35, 1)));
            Config.SubMenu("Smite").AddItem(new MenuItem("Usered", "Smite Red Early ")).SetValue(true);
            Config.SubMenu("Smite")
                .AddItem(new MenuItem("healthJ", "Smite Red Early if HP% <").SetValue(new Slider(35, 1)));

            //Extra
            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Usepackes")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("AutoShield", "Auto W")).SetValue(true);
            // Config.SubMenu("Misc").AddItem(new MenuItem("Shieldper", "Self Health %")).SetValue(new Slider(40, 1, 100));
            Config.SubMenu("Misc")
                .AddItem(
                    new MenuItem("Escape", "Escape Key!").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Misc").AddItem(new MenuItem("Inter_E", "Interrupter E")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("Gap_W", "GapClosers W")).SetValue(true);

            //Kill Steal
            Config.AddSubMenu(new Menu("KillSteal", "Ks"));
            Config.SubMenu("Ks").AddItem(new MenuItem("ActiveKs", "Use KillSteal")).SetValue(true);
            Config.SubMenu("Ks").AddItem(new MenuItem("UseQKs", "Use Q")).SetValue(true);
            Config.SubMenu("Ks").AddItem(new MenuItem("UseRKs", "Use R")).SetValue(true);
            Config.SubMenu("Ks")
                .AddItem(new MenuItem("TargetRange", "R use if range >").SetValue(new Slider(400, 200, 600)));
            Config.SubMenu("Ks").AddItem(new MenuItem("UseIgnite", "Use Ignite")).SetValue(true);

            //Damage after combo:
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            //Drawings
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            Config.SubMenu("Drawings").AddItem(new MenuItem("Drawsmite", "Draw smite")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("ShowPassive", "Show Passive")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("combotext", "Show Selected Combo")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "Lag Free Circles").SetValue(true));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));

            Config.AddToMainMenu();

            new AssassinManager();
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
            Game.PrintChat("<font color='#881df2'>Diana By Diabaths With Misaya Combo by xSalice </font>Loaded!");
            Game.PrintChat(
                "<font color='#FF0000'>If You like my work and want to support me,  plz donate via paypal in </font> <font color='#FF9900'>ssssssssssmith@hotmail.com</font> (10) S");

            // Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (_e.IsReady() && sender.IsValidTarget(_e.Range) && Config.Item("Inter_E").GetValue<bool>())
                _e.Cast();
        }

        private static void SwitchCombo(object sender, OnValueChangeEventArgs e)
        {
            if (e.GetNewValue<bool>())
                Config.Item("Misayacombo").SetValue(false);
        }

        private static void SwitchMisaya(object sender, OnValueChangeEventArgs e)
        {
            if (e.GetNewValue<bool>())
                Config.Item("Normalcombo").SetValue(false);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            _player = ObjectManager.Player;

            _orbwalker.SetAttack(true);
            if (Config.Item("Usesmite").GetValue<KeyBind>().Active)
            {
                Smiteuse();
            }
            if (Config.Item("ActiveLast").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("lastmana").GetValue<Slider>().Value)
            {
                LastHit();
            }
            if (Config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                if (Config.Item("Misayacombo").GetValue<bool>())
                {
                    Misaya();
                }
                else if (Config.Item("Normalcombo").GetValue<bool>())
                {
                    Combo();
                }
            }
            if ((Config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 Config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("Harrasmana").GetValue<Slider>().Value)
            {
                Harass();
            }
            if (Config.Item("ActiveLane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("Lanemana").GetValue<Slider>().Value)
            {
                Farm();
            }
            if (Config.Item("ActiveJungle").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("Junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            Usepotion();
            if (Config.Item("Escape").GetValue<KeyBind>().Active)
            {
                Tragic();
            }
            if (Config.Item("ActiveKs").GetValue<bool>())
            {
                KillSteal();
            }
            /* if (Config.Item("AutoShield").GetValue<bool>() && !Config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                AutoW();
            }*/
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_w.IsReady() && gapcloser.Sender.IsValidTarget(_w.Range) && Config.Item("Gap_W").GetValue<bool>())
            {
                _w.Cast();
            }
        }

        private static void Smiteontarget(Obj_AI_Hero target)
        {
            var usesmite = Config.Item("smitecombo").GetValue<bool>();
            var itemscheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
            if (itemscheck && usesmite &&
                ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) == SpellState.Ready &&
                target.Distance(_player.Position) < _smite.Range)
            {
                ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, target);
            }
        }

        private static void Misaya()
        {
            var t = GetTarget(_q.Range);
            var useQ = Config.Item("UseQCombo").GetValue<bool>();
            var useW = Config.Item("UseWCombo").GetValue<bool>();
            var useE = Config.Item("UseECombo").GetValue<bool>();
            var useR = Config.Item("UseRCombo").GetValue<bool>();
            var ignitecombo = Config.Item("UseIgnitecombo").GetValue<bool>();
            var qmana = _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost;
            var rmana = _player.Spellbook.GetSpell(SpellSlot.R).ManaCost;

            Smiteontarget(t);

            if (t != null && _igniteSlot != SpellSlot.Unknown && ignitecombo &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (t.Health <= ComboDamage(t))
                {
                    _player.Spellbook.CastSpell(_igniteSlot, t);
                }
            }

            if (_player.Distance(t) <= _q.Range && useQ && useR && _q.IsReady() && _r.IsReady())
            {
                if (_q.GetPrediction(t).Hitchance >= HitChance.High && _player.Mana > qmana + rmana)

                {
                    _r.Cast(t, Packets());
                    _q.CastIfHitchanceEquals(t, HitChance.High, Packets());
                }
            }
            if (_player.Distance(t) <= _w.Range && useW && _w.IsReady())
            {
                _w.Cast();
            }
            if (_player.Distance(t) <= _e.Range && _player.Distance(t) >= _w.Range &&
                useE && _e.IsReady() && !_w.IsReady())
            {
                _e.Cast();
            }
            if (_player.Distance(t) <= _r.Range && Config.Item("UseRSecond").GetValue<bool>() && _r.IsReady() &&
                !_w.IsReady() && !_q.IsReady())
            {
                _r.Cast(t, Packets());
            }
            UseItemes(t);
        }

        private static void Combo()
        {
            var t = GetTarget(_q.Range, TargetSelector.DamageType.Magical);

            var ignitecombo = Config.Item("UseIgnitecombo").GetValue<bool>();
            Smiteontarget(t);
            if (_igniteSlot != SpellSlot.Unknown && ignitecombo &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (t.Health <= ComboDamage(t))
                {
                    _player.Spellbook.CastSpell(_igniteSlot, t);
                }
            }
            if (_player.Distance(t) <= _q.Range && Config.Item("UseQCombo").GetValue<bool>() && _q.IsReady() &&
                _q.GetPrediction(t).Hitchance >= HitChance.High)
            {
                _q.CastIfHitchanceEquals(t, HitChance.High, Packets());
            }
            if (_player.Distance(t) <= _r.Range && Config.Item("UseRCombo").GetValue<bool>() && _r.IsReady() &&
                (_qcreated
                 || t.HasBuff("dianamoonlight")))
            {
                _r.Cast(t, Packets());
            }
            if (_player.Distance(t) <= _w.Range && Config.Item("UseWCombo").GetValue<bool>() && _w.IsReady() &&
                !_q.IsReady())
            {
                _w.Cast();
            }
            if (_player.Distance(t) <= _e.Range && _player.Distance(t) >= _w.Range &&
                Config.Item("UseECombo").GetValue<bool>() && _e.IsReady() && !_w.IsReady())
            {
                _e.Cast();
            }
            if (_player.Distance(t) <= _r.Range && Config.Item("UseRSecond").GetValue<bool>() && _r.IsReady() &&
                !_w.IsReady() && !_q.IsReady())
            {
                _r.Cast(t, Packets());
            }
            UseItemes(t);
        }

        private static void UseItemes(Obj_AI_Hero target)
        {
            var iBilge = Config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth*(Config.Item("BilgeEnemyhp").GetValue<Slider>().Value)/100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth*(Config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
            var iBlade = Config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth*(Config.Item("BladeEnemyhp").GetValue<Slider>().Value)/100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth*(Config.Item("Blademyhp").GetValue<Slider>().Value)/100);
            var iOmen = Config.Item("Omen").GetValue<bool>();
            var iOmenenemys = ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(450)) >=
                              Config.Item("Omenenemys").GetValue<Slider>().Value;
            var iTiamat = Config.Item("Tiamat").GetValue<bool>();
            var iHydra = Config.Item("Hydra").GetValue<bool>();
            var ilotis = Config.Item("lotis").GetValue<bool>();

            if (_player.Distance(target) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);
            }
            if (_player.Distance(target) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);
            }
            if (iTiamat && _tiamat.IsReady() && target.IsValidTarget(_tiamat.Range))
            {
                _tiamat.Cast();
            }
            if (iHydra && _hydra.IsReady() && target.IsValidTarget(_hydra.Range))
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
                    if (hero.Health <= (hero.MaxHealth*(Config.Item("lotisminhp").GetValue<Slider>().Value)/100) &&
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
            var iusehppotion = Config.Item("usehppotions").GetValue<bool>();
            var iusepotionhp = _player.Health <=
                               (_player.MaxHealth*(Config.Item("usepotionhp").GetValue<Slider>().Value)/100);
            var iusemppotion = Config.Item("usemppotions").GetValue<bool>();
            var iusepotionmp = _player.Mana <=
                               (_player.MaxMana*(Config.Item("usepotionmp").GetValue<Slider>().Value)/100);
            if (_player.InFountain() || ObjectManager.Player.HasBuff("Recall")) return;

            if (_player.CountEnemiesInRange(800f) > 0 ||
                (mobs.Count > 0 && Config.Item("ActiveJungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
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
                    !(ObjectManager.Player.HasBuff("RegenerationPotion") ||
                      ObjectManager.Player.HasBuff("ItemCrystalFlask") ||
                      ObjectManager.Player.HasBuff("ItemMiniRegenPotion")))
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
                    !(ObjectManager.Player.HasBuff("FlaskOfCrystalWater") ||
                      ObjectManager.Player.HasBuff("ItemCrystalFlask") ||
                      ObjectManager.Player.HasBuff("ItemMiniRegenPotion")))
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

        private static float ComboDamage(Obj_AI_Hero hero)
        {
            var dmg = 0d;

            if (_q.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.Q)*2;
            if (_w.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.W);
            if (_r.IsReady())
                dmg += _player.GetSpellDamage(hero, SpellSlot.R);
            if (ObjectManager.Player.GetSpellSlot("SummonerIgnite") != SpellSlot.Unknown)
            {
                dmg += _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            dmg += _player.GetAutoAttackDamage(hero, true)*2;
            if (_player.HasBuff("dianaarcready"))
            {
                dmg += 15 + 5*ObjectManager.Player.Level;
            }
            if (ObjectManager.Player.HasBuff("LichBane"))
            {
                dmg += _player.BaseAttackDamage*0.75 + _player.FlatMagicDamageMod*0.5;
            }
            return (float) dmg;
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
            if (target != null)
            {
                if (_player.Distance(target) <= _q.Range && Config.Item("UseQHarass").GetValue<bool>() && _q.IsReady())
                {
                    _q.CastIfHitchanceEquals(target, HitChance.High, Packets());
                }
                if (_player.Distance(target) <= 200 && Config.Item("UseWHarass").GetValue<bool>() && _w.IsReady())
                {
                    _w.Cast();
                }
            }
        }

        private static void Farm()
        {
            if (!Orbwalking.CanMove(40)) return;

            var rangedMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range + _q.Width + 30,
                MinionTypes.Ranged);
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range + _q.Width + 30);
            var allMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _w.Range);

            var useQ = Config.Item("UseQLane").GetValue<bool>();
            var useW = Config.Item("UseWLane").GetValue<bool>();
            if (_q.IsReady() && useQ)
            {
                var fl1 = _q.GetCircularFarmLocation(rangedMinionsQ, _q.Width);
                var fl2 = _q.GetCircularFarmLocation(allMinionsQ, _q.Width);

                if (fl1.MinionsHit >= 3)
                {
                    _q.Cast(fl1.Position);
                }
                else if (fl2.MinionsHit >= 2 || allMinionsQ.Count == 1)
                {
                    _q.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.Q))
                            _q.Cast(minion);
            }
            if (_w.IsReady() && useW && allMinionsW.Count > 2)
            {
                _w.Cast();
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
                        spell => string.Equals(spell.Name, Smitetype(), StringComparison.CurrentCultureIgnoreCase)))
            {
                _smiteSlot = spell.Slot;
                _smite = new Spell(_smiteSlot, 700);
                return;
            }
        }

        private static int GetSmiteDmg()
        {
            var level = _player.Level;
            var index = _player.Level/5;
            float[] dmgs = {370 + 20*level, 330 + 30*level, 240 + 40*level, 100 + 50*level};
            return (int) dmgs[index];
        }

        //New map Monsters Name By SKO
        private static void Smiteuse()
        {
            var jungle = Config.Item("ActiveJungle").GetValue<KeyBind>().Active;
            if (ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) != SpellState.Ready) return;
            var useblue = Config.Item("Useblue").GetValue<bool>();
            var usered = Config.Item("Usered").GetValue<bool>();
            var health = (100*(_player.Mana/_player.MaxMana)) < Config.Item("healthJ").GetValue<Slider>().Value;
            var mana = (100*(_player.Mana/_player.MaxMana)) < Config.Item("manaJ").GetValue<Slider>().Value;
            string[] jungleMinions;
            if (Utility.Map.GetMap().Type.Equals(Utility.Map.MapType.TwistedTreeline))
            {
                jungleMinions = new[] {"TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf"};
            }
            else
            {
                jungleMinions = new[]
                {
                    "SRU_Blue", "SRU_Gromp", "SRU_Murkwolf", "SRU_Razorbeak", "SRU_Red", "SRU_Krug", "SRU_Dragon",
                    "SRU_Baron", "Sru_Crab"
                };
            }
            var minions = MinionManager.GetMinions(_player.Position, 1000, MinionTypes.All, MinionTeam.Neutral);
            if (minions.Any())
            {
                var smiteDmg = GetSmiteDmg();

                foreach (var minion in minions)
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

        private static void Tragic()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range + _q.Width + 30);
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (_q.IsReady()) _q.Cast(Game.CursorPos);
            if (_r.IsReady())
            {
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];

                    _r.CastOnUnit(mob);
                }
                else if (allMinionsQ.Count >= 1)
                {
                    _r.Cast(allMinionsQ[0]);
                }
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range);
            var useQ = Config.Item("UseQLH").GetValue<bool>();
            var useW = Config.Item("UseWLH").GetValue<bool>();
            foreach (var minion in allMinions)
            {
                if (useQ && _q.IsReady() && _player.Distance(minion) < _q.Range &&
                    minion.Health < 0.95*_player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion);
                }

                if (_w.IsReady() && useW && _player.Distance(minion) < _w.Range &&
                    minion.Health < 0.95*_player.GetSpellDamage(minion, SpellSlot.W))
                {
                    _w.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var useQ = Config.Item("UseQJungle").GetValue<bool>();
            var useW = Config.Item("UseWJungle").GetValue<bool>();
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useQ && _q.IsReady() && _player.Distance(mob) < _q.Range)
                {
                    _q.Cast(mob);
                }
                if (_w.IsReady() && useW && _player.Distance(mob) < _w.Range)
                {
                    _w.Cast();
                }
            }
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(_q.Range, TargetSelector.DamageType.Magical);
            var igniteDmg = _player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            var qhDmg = _player.GetSpellDamage(target, SpellSlot.Q);
            var rhDmg = _player.GetSpellDamage(target, SpellSlot.R);
            var rRange = (_player.Distance(target) >= Config.Item("TargetRange").GetValue<Slider>().Value);
            if (target != null && Config.Item("UseIgnite").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (igniteDmg > target.Health)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }

            if (_q.IsReady() && _player.Distance(target) <= _q.Range && Config.Item("UseQKs").GetValue<bool>())
            {
                if (target != null && target.Health <= qhDmg)
                {
                    _q.Cast(target, Packets());
                }
            }

            if (_r.IsReady() && _player.Distance(target) <= _r.Range && rRange && Config.Item("UseRKs").GetValue<bool>())
            {
                if (target != null && target.Health <= rhDmg)
                {
                    _r.Cast(target, Packets());
                }
            }
        }

        /* private static void AutoW()
        {
            if (_player.HasBuff("Recall") || ObjectManager.Player.InFountain()) return;
            if (_w.IsReady() &&
                _player.Health <= (_player.MaxHealth * (Config.Item("Shieldper").GetValue<Slider>().Value) / 100))
            {
                _w.Cast();
            }

        }*/

        private static bool Packets()
        {
            return Config.Item("usePackets").GetValue<bool>();
        }

        private static void OnCreate(GameObject sender, EventArgs args)
        {
            var spell = (Obj_SpellMissile) sender;
            var unit = spell.SpellCaster.Name;
            var caster = spell.SpellCaster;
            var name = spell.SData.Name;

            if (unit == ObjectManager.Player.Name && (name == "dianaarcthrow"))
            {
                // Game.PrintChat("Spell: " + name);
                _qpos = spell;
                _qcreated = true;
                return;
            }
            // credits 100% to brian0305
            if (sender != null && sender.IsValid && Config.Item("AutoShield").GetValue<bool>() &&
                _w.IsReady())
            {
                if (caster.IsEnemy)
                {
                    var shieldBuff = new[] {40, 55, 70, 85, 100}[_w.Level - 1] +
                                     1.3*_player.FlatMagicDamageMod;
                    if (spell.SData.Name.Contains("BasicAttack"))
                    {
                        if (spell.Target.IsMe && _player.Health <= caster.GetAutoAttackDamage(_player, true) &&
                            _player.Health + shieldBuff > caster.GetAutoAttackDamage(_player, true)) _w.Cast();
                    }
                    else if (spell.Target.IsMe || spell.EndPosition.Distance(_player.Position) <= 130)
                    {
                        if (spell.SData.Name == "summonerdot")
                        {
                            if (_player.Health <=
                                (caster as Obj_AI_Hero).GetSummonerSpellDamage(_player, Damage.SummonerSpell.Ignite) &&
                                _player.Health + shieldBuff >
                                (caster as Obj_AI_Hero).GetSummonerSpellDamage(_player, Damage.SummonerSpell.Ignite))
                                _w.Cast();
                        }
                        else if (_player.Health <=
                                 (caster as Obj_AI_Hero).GetSpellDamage(_player,
                                     (caster as Obj_AI_Hero).GetSpellSlot(spell.SData.Name), 1) &&
                                 _player.Health + shieldBuff >
                                 (caster as Obj_AI_Hero).GetSpellDamage(_player,
                                     (caster as Obj_AI_Hero).GetSpellSlot(spell.SData.Name), 1)) _w.Cast();
                    }
                }
            }
        }

        private static void OnDelete(GameObject sender, EventArgs args)
        {
            var spell = (Obj_SpellMissile) sender;
            var unit = spell.SpellCaster.Name;
            var name = spell.SData.Name;

            if (unit == ObjectManager.Player.Name && (name == "dianaarcthrow"))
            {
                _qpos = null;
                _qcreated = false;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var diana = Drawing.WorldToScreen(_player.Position);
            if (Config.Item("combotext").GetValue<bool>())
            {
                if (Config.Item("Misayacombo").GetValue<bool>())
                {
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.66f, Color.DarkOrange,
                        "R-Q Combo On");
                }
                else if (Config.Item("Normalcombo").GetValue<bool>())
                {
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.66f, Color.DarkOrange,
                        "Q-R Combo On");
                }
            }
            if (Config.Item("Drawsmite").GetValue<bool>())
            {
                if (Config.Item("Usesmite").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, Color.DarkOrange,
                        "Smite Is On");
                }
                else
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, Color.DarkRed,
                        "Smite Is Off");
            }
            if (_qpos != null)
                Render.Circle.DrawCircle(_qpos.Position, _qpos.BoundingRadius, Color.Red, 1);

            if (Config.Item("ShowPassive").GetValue<bool>())
            {
                if (_player.HasBuff("dianaarcready"))
                    Drawing.DrawText(diana[0] - 10, diana[1], Color.White, "P On");
                else
                    Drawing.DrawText(diana[0] - 10, diana[1], Color.Orange, "P Off");
            }

            if (Config.Item("DrawQ").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, Color.White, 1);
            }
            if (Config.Item("DrawW").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _w.Range, Color.White, 1);
            }
            if (Config.Item("DrawE").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, Color.White, 1);
            }
            if (Config.Item("DrawR").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, Color.White, 1);
            }
        }

        private static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = _q.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }
    }
}