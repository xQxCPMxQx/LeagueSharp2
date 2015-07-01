using System;
using System.Linq;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

//gg

namespace D_Jarvan
{
    internal class Program
    {
        public const string ChampionName = "JarvanIV";

        private static Orbwalking.Orbwalker _orbwalker;

        public static Spell _q, _w, E, _r;

        private static SpellSlot _igniteSlot;

        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis;

        public static Menu Config;

        private static Obj_AI_Hero _player;

        private static bool _haveulti;

        private static SpellSlot _smiteSlot = SpellSlot.Unknown;

        private static Spell _smite;

        private static SpellSlot _flashSlot;

        private static Vector3 _epos = default(Vector3);

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
            if (ObjectManager.Player.BaseSkinName != ChampionName) return;

            _q = new Spell(SpellSlot.Q, 770f);
            _w = new Spell(SpellSlot.W, 300f);
            E = new Spell(SpellSlot.E, 830f);
            _r = new Spell(SpellSlot.R, 650f);

            _q.SetSkillshot(0.5f, 70f, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.5f, 70f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            _igniteSlot = _player.GetSpellSlot("SummonerDot");
            _flashSlot = _player.GetSpellSlot("SummonerFlash");
            SetSmiteSlot();

            _bilge = new Items.Item(3144, 450f);
            _blade = new Items.Item(3153, 450f);
            _hydra = new Items.Item(3074, 250f);
            _tiamat = new Items.Item(3077, 250f);
            _rand = new Items.Item(3143, 490f);
            _lotis = new Items.Item(3190, 590f);

            //D Jarvan
            Config = new Menu("D-Jarvan", "D-Jarvan", true);

            //TargetSelector
            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            new AssassinManager();

            //Orbwalker
            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            _orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            //Combo
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("UseIgnite", "Use Ignite")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("smitecombo", "Use Smite in target")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseQC", "Use Q")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "Use W")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseEC", "Use E")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRC", "Use R(killable)")).SetValue(true);
            Config.SubMenu("Combo").AddItem(new MenuItem("UseRE", "AutoR Min Targ")).SetValue(true);
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("MinTargets", "Ult when>=min enemy(COMBO)").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("ActiveCombo", "Combo!").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ActiveComboEQR", "ComboEQ-R!").SetValue(new KeyBind("T".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Combo")
                .AddItem(
                    new MenuItem("ComboeqFlash", "ComboEQ- Flash!").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Press)));
            Config.SubMenu("Combo")
                .AddItem(new MenuItem("FlashDista", "Flash Distance").SetValue(new Slider(700, 700, 1000)));

            //Items public static Int32 Tiamat = 3077, Hydra = 3074, Blade = 3153, Bilge = 3144, Rand = 3143, lotis = 3190;
            Config.AddSubMenu(new Menu("items", "items"));
            Config.SubMenu("items").AddSubMenu(new Menu("Offensive", "Offensive"));
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
            Config.SubMenu("items").AddSubMenu(new Menu("Potions", "Potions"));
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usehppotions", "Use Healt potion/Flask/Biscuit"))
                .SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionhp", "If Health % <").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usemppotions", "Use Mana potion/Flask/Biscuit"))
                .SetValue(true);
            Config.SubMenu("items")
                .SubMenu("Potions")
                .AddItem(new MenuItem("usepotionmp", "If Mana % <").SetValue(new Slider(35, 1, 100)));

            //Harass
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseQH", "Use Q")).SetValue(true);
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEH", "Use E")).SetValue(true);
            Config.SubMenu("Harass").AddItem(new MenuItem("UseEQH", "Use EQ Combo")).SetValue(true);
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("UseEQHHP", "EQ If Your Hp > ").SetValue(new Slider(85, 1, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("UseItemsharass", "Use Tiamat/Hydra")).SetValue(true);
            Config.SubMenu("Harass")
                .AddItem(new MenuItem("harassmana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("harasstoggle", "AutoHarass (toggle)").SetValue(new KeyBind("G".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuItem("ActiveHarass", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

            //LaneClear
            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddSubMenu(new Menu("LaneFarm", "LaneFarm"));
            Config.SubMenu("Farm")
                .SubMenu("LaneFarm")
                .AddItem(new MenuItem("UseItemslane", "Use Items in LaneClear"))
                .SetValue(true);
            Config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseQL", "Q LaneClear")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseEL", "E LaneClear")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseWL", "W LaneClear")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("LaneFarm")
                .AddItem(new MenuItem("UseWLHP", "use W if Hp% <").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("LaneFarm")
                .AddItem(new MenuItem("lanemana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("LaneFarm")
                .AddItem(
                    new MenuItem("Activelane", "LaneClear!").SetValue(new KeyBind("V".ToCharArray()[0],
                        KeyBindType.Press)));

            Config.SubMenu("Farm").AddSubMenu(new Menu("LastHit", "LastHit"));
            Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseQLH", "Q LastHit")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseELH", "E LastHit")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseWLH", "W LaneClear")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("LastHit")
                .AddItem(new MenuItem("UseWLHHP", "use W if Hp% <").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("LastHit")
                .AddItem(new MenuItem("lastmana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("LastHit")
                .AddItem(
                    new MenuItem("ActiveLast", "LastHit!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

            Config.SubMenu("Farm").AddSubMenu(new Menu("Jungle", "Jungle"));
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("UseItemsjungle", "Use Items in jungle"))
                .SetValue(true);

            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJ", "Q Jungle")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseEJ", "E Jungle")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseWJ", "W Jungle")).SetValue(true);
            Config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem(" UseEQJ", "EQ In Jungle")).SetValue(true);
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("UseWJHP", "use W if Hp% <").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(new MenuItem("junglemana", "Minimum Mana% >").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Farm")
                .SubMenu("Jungle")
                .AddItem(
                    new MenuItem("ActiveJungle", "Jungle!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

            //Smite ActiveJungle
            Config.AddSubMenu(new Menu("Smite", "Smite"));
            Config.SubMenu("Smite")
                .AddItem(
                    new MenuItem("Usesmite", "Use Smite(toggle)").SetValue(new KeyBind("H".ToCharArray()[0],
                        KeyBindType.Toggle)));
            Config.SubMenu("Smite").AddItem(new MenuItem("Useblue", "Smite Blue Early ")).SetValue(true);
            Config.SubMenu("Smite")
                .AddItem(new MenuItem("manaJ", "Smite Blue Early if MP% <").SetValue(new Slider(35, 1, 100)));
            Config.SubMenu("Smite").AddItem(new MenuItem("Usered", "Smite Red Early ")).SetValue(true);
            Config.SubMenu("Smite")
                .AddItem(new MenuItem("healthJ", "Smite Red Early if HP% <").SetValue(new Slider(35, 1, 100)));

            //Forest
            Config.AddSubMenu(new Menu("Forest Gump", "Forest Gump"));
            Config.SubMenu("Forest Gump").AddItem(new MenuItem("UseEQF", "Use EQ in Mouse ")).SetValue(true);
            Config.SubMenu("Forest Gump").AddItem(new MenuItem("UseWF", "Use W ")).SetValue(true);
            Config.SubMenu("Forest Gump")
                .AddItem(
                    new MenuItem("Forest", "Active Forest Gump!").SetValue(new KeyBind("Z".ToCharArray()[0],
                        KeyBindType.Press)));


            //Misc
            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("UseIgnitekill", "Use Ignite KillSteal")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("UseQM", "Use Q KillSteal")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("UseRM", "Use R KillSteal")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("Gap_W", "W GapClosers")).SetValue(true);
            Config.SubMenu("Misc").AddItem(new MenuItem("UseEQInt", "EQ to Interrupt")).SetValue(true);
            // Config.SubMenu("Misc").AddItem(new MenuItem("MinTargetsgap", "min enemy >=(GapClosers)").SetValue(new Slider(2, 1, 5)));
            Config.SubMenu("Misc").AddItem(new MenuItem("usePackets", "Usepackes")).SetValue(true);

            //Drawings
            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("Draw.Enable", "Enable Draw")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Draw Q")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawW", "Draw W")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "Draw E")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "Draw R")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawQR", "Draw EQ-R")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawEQF", "Draw EQ-Flash")).SetValue(true);
            Config.SubMenu("Drawings").AddItem(new MenuItem("Drawsmite", "Draw smite")).SetValue(true);
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleQuality", "Circles Quality").SetValue(new Slider(100, 100, 10)));
            Config.SubMenu("Drawings")
                .AddItem(new MenuItem("CircleThickness", "Circles Thickness").SetValue(new Slider(1, 10, 1)));
            

            Config.AddToMainMenu();
            Sprite.Load();
            
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += OnCreateObj;
            GameObject.OnDelete += OnDeleteObj;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter2_OnInterruptableTarget;
            Utility.HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            Notifications.AddNotification(String.Format("{0} Loaded", ChampionName), 4000);
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Config.Item("Forest").GetValue<KeyBind>().Active)
            {
                Forest();
            }
            if (Config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            if (Config.Item("ActiveComboEQR").GetValue<KeyBind>().Active)
            {
                ComboEqr();
            }
            if ((Config.Item("ActiveHarass").GetValue<KeyBind>().Active ||
                 Config.Item("harasstoggle").GetValue<KeyBind>().Active) &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("harassmana").GetValue<Slider>().Value)
            {
                Harass();

            }
            if (Config.Item("Activelane").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("lanemana").GetValue<Slider>().Value)
            {
                Laneclear();
            }
            if (Config.Item("ActiveJungle").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("junglemana").GetValue<Slider>().Value)
            {
                JungleClear();
            }
            if (Config.Item("ActiveLast").GetValue<KeyBind>().Active &&
                (100*(_player.Mana/_player.MaxMana)) > Config.Item("lastmana").GetValue<Slider>().Value)
            {
                LastHit();
            }
            Usepotion();
            if (Config.Item("Usesmite").GetValue<KeyBind>().Active)
            {
                Smiteuse();
            }
            if (Config.Item("ComboeqFlash").GetValue<KeyBind>().Active)
            {
                ComboeqFlash();
            }

            _player = ObjectManager.Player;

            _orbwalker.SetAttack(true);

            KillSteal();

        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (_w.IsReady() && gapcloser.Sender.IsValidTarget(_w.Range) && Config.Item("Gap_W").GetValue<bool>())
            {
                _w.Cast(gapcloser.Sender, Packets());
            }
        }

        private static void Interrupter2_OnInterruptableTarget(Obj_AI_Hero unit,
            Interrupter2.InterruptableTargetEventArgs args)
        {
            if (unit.IsValidTarget(_q.Range) && Config.Item("UseEQInt").GetValue<bool>())
            {
                if (E.IsReady() && _q.IsReady())
                {
                    E.Cast(unit, Packets());
                }
                if (_q.IsReady() && _epos != default(Vector3) && unit.IsValidTarget(200, true, _epos))
                {
                    _q.Cast(_epos, Packets());
                }
            }
        }

        private static void GenModelPacket(string champ, int skinId)
        {
            Packet.S2C.UpdateModel.Encoded(new Packet.S2C.UpdateModel.Struct(_player.NetworkId, skinId, champ))
                .Process();
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
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q)*2*1.2;
            if (E.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);

            damage += _player.GetAutoAttackDamage(enemy, true)*1.1;
            damage += _player.GetAutoAttackDamage(enemy, true);
            return (float) damage;
        }

        private static void Smiteontarget()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var usesmite = Config.Item("smitecombo").GetValue<bool>();
                var itemscheck = SmiteBlue.Any(i => Items.HasItem(i)) || SmiteRed.Any(i => Items.HasItem(i));
                if (itemscheck && usesmite &&
                    ObjectManager.Player.Spellbook.CanUseSpell(_smiteSlot) == SpellState.Ready &&
                    hero.IsValidTarget(_smite.Range))
                {
                    ObjectManager.Player.Spellbook.CastSpell(_smiteSlot, hero);
                }
            }
        }

        public static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(vDefaultRange) < 0.00001)
                vDefaultRange = E.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != _player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            _player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void Combo()
        {
            var useQ = Config.Item("UseQC").GetValue<bool>();
            var useW = Config.Item("UseWC").GetValue<bool>();
            var useE = Config.Item("UseEC").GetValue<bool>();
            var useR = Config.Item("UseRC").GetValue<bool>();
            var autoR = Config.Item("UseRE").GetValue<bool>();
            //var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var t = GetTarget(E.Range, TargetSelector.DamageType.Magical);


            Smiteontarget();
            if (t.IsValidTarget(600) && Config.Item("UseIgnite").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (ComboDamage(t) > t.Health)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, t);
                }
            }
            if (useR && _r.IsReady())
            {
                if (t.IsValidTarget(_q.Range) && !_haveulti)
                    if (!t.HasBuff("JudicatorIntervention") && !t.HasBuff("Undying Rage") &&
                        ComboDamage(t) > t.Health)
                        _r.CastIfHitchanceEquals(t, HitChance.Medium, Packets());
            }
            if (useE && E.IsReady() && t.IsValidTarget(_q.Range) && _q.IsReady())
            {
                //xsalice Code
                var vec = t.ServerPosition - _player.ServerPosition;
                var castBehind = E.GetPrediction(t).CastPosition + Vector3.Normalize(vec)*100;
                E.Cast(castBehind, Packets());
            }
            if (useQ && t.IsValidTarget(_q.Range) && _q.IsReady() && _epos != default(Vector3) &&
                t.IsValidTarget(200, true, _epos))
            {
                _q.Cast(_epos, Packets());
            }

            if (useW && _w.IsReady())
            {
                if (t.IsValidTarget(_w.Range))
                    _w.Cast();
            }
            if (useQ && _q.IsReady() && !E.IsReady())
            {
                if (t.IsValidTarget(_q.Range))
                    _q.Cast(t, Packets(), true);
            }
            if (_r.IsReady() && autoR && !_haveulti)
            {
                if (GetNumberHitByR(t) >=
                    Config.Item("MinTargets").GetValue<Slider>().Value && t.IsValidTarget(_r.Range))
                    _r.Cast(t, Packets(), true);
            }
            UseItemes();
        }

        private static int GetNumberHitByR(Obj_AI_Hero target)
        {
            int Enemys = 0;
            foreach (Obj_AI_Hero enemys in ObjectManager.Get<Obj_AI_Hero>())
            {
                var pred = _r.GetPrediction(enemys, true);
                if (pred.Hitchance >= HitChance.High && !enemys.IsMe && enemys.IsEnemy &&
                    Vector3.Distance(_player.Position, pred.UnitPosition) <= _r.Range)
                {
                    Enemys = Enemys + 1;
                }
            }
            return Enemys;
        }

        private static void ComboEqr()
        {
            var manacheck = _player.Mana >
                            _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                            _player.Spellbook.GetSpell(SpellSlot.E).ManaCost +
                            _player.Spellbook.GetSpell(SpellSlot.R).ManaCost;
            var t = TargetSelector.GetTarget(_q.Range + _r.Range, TargetSelector.DamageType.Magical);
            if (t == null)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                _player.IssueOrder(GameObjectOrder.AttackUnit, t);
            }
            Smiteontarget();
            if (E.IsReady() && _q.IsReady() && manacheck && t.IsValidTarget(_q.Range))
            {
                E.Cast(t.ServerPosition, Packets());
                _q.Cast(t.ServerPosition, Packets());
            }
            if (t.IsValidTarget(600) && Config.Item("UseIgnite").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (ComboDamage(t) > t.Health)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, t);
                }
            }
            if (_r.IsReady() && !_haveulti && t.IsValidTarget(_r.Range))
            {
                _r.CastIfHitchanceEquals(t, HitChance.Immobile, Packets());
            }
            if (_w.IsReady())
            {
                if (t.IsValidTarget(_w.Range))
                    _w.Cast();
            }
            UseItemes();
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
            var useQ = Config.Item("UseQH").GetValue<bool>();
            var useE = Config.Item("UseEH").GetValue<bool>();
            var useEq = Config.Item("UseEQH").GetValue<bool>();
            var useEqhp = (100*(_player.Health/_player.MaxHealth)) > Config.Item("UseEQHHP").GetValue<Slider>().Value;
            var useItemsH = Config.Item("UseItemsharass").GetValue<bool>();
            if (useEqhp && useEq && _q.IsReady() && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(E.Range))
                    E.Cast(t, Packets());
                _q.Cast(t, Packets());
            }
            if (useQ && _q.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(_q.Range))
                    _q.Cast(t, Packets());
            }
            if (useE && E.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(E.Range))
                    E.Cast(t, Packets());
            }

            if (useItemsH && _tiamat.IsReady() && target.IsValidTarget(_tiamat.Range))
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.IsValidTarget(_hydra.Range))
            {
                _hydra.Cast();
            }
        }

        private static void ComboeqFlash()
        {
            var flashDista = Config.Item("FlashDista").GetValue<Slider>().Value;
            var manacheck = _player.Mana >
                            _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                            _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            var t = TargetSelector.GetTarget(_q.Range + 800, TargetSelector.DamageType.Magical);
            if (t == null)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                _player.IssueOrder(GameObjectOrder.AttackUnit, t);
            }
            Smiteontarget();
            if (_flashSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_flashSlot) == SpellState.Ready)
            {
                if (E.IsReady() && _q.IsReady() && manacheck && !t.IsValidTarget(_q.Range))
                {
                    E.Cast(Game.CursorPos, Packets());
                }
                if (_epos != default(Vector3) && _q.IsInRange(_epos))
                {
                    _q.Cast(_epos, Packets());
                }

                if (t.IsValidTarget(flashDista) && !_q.IsReady())
                {
                    _player.Spellbook.CastSpell(_flashSlot, t.ServerPosition);
                }
            }
            UseItemes();
        }

        private static void Laneclear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var rangedMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range + _q.Width,
                MinionTypes.Ranged);
            var rangedMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width,
                MinionTypes.Ranged);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range + E.Width,
                MinionTypes.All);
            var useItemsl = Config.Item("UseItemslane").GetValue<bool>();
            var useQl = Config.Item("UseQL").GetValue<bool>();
            var useEl = Config.Item("UseEL").GetValue<bool>();
            var useWl = Config.Item("UseWL").GetValue<bool>();
            var usewhp = (100*(_player.Health/_player.MaxHealth)) < Config.Item("UseWLHP").GetValue<Slider>().Value;

            if (_q.IsReady() && useQl)
            {
                var fl1 = _q.GetLineFarmLocation(rangedMinionsQ, _q.Width);
                var fl2 = _q.GetLineFarmLocation(allMinionsQ, _q.Width);

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

            if (E.IsReady() && useEl)
            {
                var fl1 = E.GetCircularFarmLocation(rangedMinionsE, E.Width);
                var fl2 = E.GetCircularFarmLocation(allMinionsE, E.Width);

                if (fl1.MinionsHit >= 3)
                {
                    E.Cast(fl1.Position);
                }
                else if (fl2.MinionsHit >= 2 || allMinionsE.Count == 1)
                {
                    E.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75*_player.GetSpellDamage(minion, SpellSlot.E))
                            E.Cast(minion);
            }
            if (usewhp && useWl && _w.IsReady() && allMinionsQ.Count > 0)
            {
                _w.Cast();

            }
            foreach (var minion in allMinionsQ)
            {
                if (useItemsl && _tiamat.IsReady() && _player.Distance(minion) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsl && _hydra.IsReady() && _player.Distance(minion) < _hydra.Range)
                {
                    _hydra.Cast();
                }
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var useQ = Config.Item("UseQLH").GetValue<bool>();
            var useW = Config.Item("UseWLH").GetValue<bool>();
            var useE = Config.Item("UseELH").GetValue<bool>();
            var usewhp = (100*(_player.Health/_player.MaxHealth)) < Config.Item("UseWLHHP").GetValue<Slider>().Value;
            foreach (var minion in allMinions)
            {
                if (useQ && _q.IsReady() && _player.Distance(minion) < _q.Range &&
                    minion.Health < 0.95*_player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion, Packets());
                }

                if (E.IsReady() && useE && _player.Distance(minion) < E.Range &&
                    minion.Health < 0.95*_player.GetSpellDamage(minion, SpellSlot.E))
                {
                    E.Cast(minion, Packets());
                }
                if (usewhp && useW && _w.IsReady() && allMinions.Count > 0)
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
            var useItemsJ = Config.Item("UseItemsjungle").GetValue<bool>();
            var useQ = Config.Item("UseQJ").GetValue<bool>();
            var useW = Config.Item("UseWJ").GetValue<bool>();
            var useE = Config.Item("UseEJ").GetValue<bool>();
            var useEq = Config.Item(" UseEQJ").GetValue<bool>();
            var usewhp = (100*(_player.Health/_player.MaxHealth)) < Config.Item("UseWJHP").GetValue<Slider>().Value;

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useEq)
                {
                    if (E.IsReady() && useE && _player.Distance(mob) < _q.Range)
                    {
                        E.Cast(mob, Packets());
                    }
                    if (useQ && _q.IsReady() && _player.Distance(mob) < _q.Range)
                    {
                        _q.Cast(mob, Packets());
                    }
                }
                else
                {
                    if (useQ && _q.IsReady() && _player.Distance(mob) < _q.Range)
                    {
                        _q.Cast(mob, Packets());
                    }
                    if (E.IsReady() && useE && _player.Distance(mob) < _q.Range)
                    {
                        E.Cast(mob, Packets());
                    }
                }
                if (_w.IsReady() && useW && usewhp && _player.Distance(mob) < _w.Range)
                {
                    _w.Cast();
                }
                if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob) < _hydra.Range)
                {
                    _hydra.Cast();
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

        private static bool Packets()
        {
            return Config.Item("usePackets").GetValue<bool>();
        }

        private static int GetSmiteDmg()
        {
            int level = _player.Level;
            int index = _player.Level/5;
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
                jungleMinions = new string[] {"TT_Spiderboss", "TT_NWraith", "TT_NGolem", "TT_NWolf"};
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

        private static void UseItemes()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var iBilge = Config.Item("Bilge").GetValue<bool>();
                var iBilgeEnemyhp = hero.Health <=
                                    (hero.MaxHealth*(Config.Item("BilgeEnemyhp").GetValue<Slider>().Value)/100);
                var iBilgemyhp = _player.Health <=
                                 (_player.MaxHealth*(Config.Item("Bilgemyhp").GetValue<Slider>().Value)/100);
                var iBlade = Config.Item("Blade").GetValue<bool>();
                var iBladeEnemyhp = hero.Health <=
                                    (hero.MaxHealth*(Config.Item("BladeEnemyhp").GetValue<Slider>().Value)/100);
                var iBlademyhp = _player.Health <=
                                 (_player.MaxHealth*(Config.Item("Blademyhp").GetValue<Slider>().Value)/100);
                var iOmen = Config.Item("Omen").GetValue<bool>();
                var iOmenenemys = hero.CountEnemiesInRange(350) >= Config.Item("Omenenemys").GetValue<Slider>().Value;
                var iTiamat = Config.Item("Tiamat").GetValue<bool>();
                var iHydra = Config.Item("Hydra").GetValue<bool>();


                if (hero.IsValidTarget(450) && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
                {
                    _bilge.Cast(hero);

                }
                if (hero.IsValidTarget(450) && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
                {
                    _blade.Cast(hero);

                }
                if (iTiamat && _tiamat.IsReady() && hero.IsValidTarget(_tiamat.Range))
                {
                    _tiamat.Cast();

                }
                if (iHydra && _hydra.IsReady() && hero.IsValidTarget(_hydra.Range))
                {
                    _hydra.Cast();

                }
                if (iOmenenemys && iOmen && _rand.IsReady())
                {
                    _rand.Cast();

                }
            }
            var ilotis = Config.Item("lotis").GetValue<bool>();
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
            if (ObjectManager.Player.InFountain() || ObjectManager.Player.HasBuff("Recall")) return;

            if (ObjectManager.Player.CountEnemiesInRange(800) > 0 ||
                (mobs.Count > 0 && Config.Item("ActiveJungle").GetValue<KeyBind>().Active && (Items.HasItem(1039) ||
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

        private static void KillSteal()
        {
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
            {
                var igniteDmg = _player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
                if (hero.IsValidTarget(600) && Config.Item("UseIgnitekill").GetValue<bool>() &&
                    _igniteSlot != SpellSlot.Unknown &&
                    _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                {
                    if (igniteDmg > hero.Health)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, hero);
                    }
                }
                if (_q.IsReady() && Config.Item("UseQM").GetValue<bool>())
                {
                    if (hero != null && _q.GetDamage(hero) > hero.Health && _player.Distance(hero) <= _q.Range)
                    {
                        _q.Cast(hero, Packets());
                    }
                }
                if (_r.IsReady() && Config.Item("UseRM").GetValue<bool>())
                {
                    if (hero != null)
                        if (!hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("Undying Rage") &&
                            _r.GetDamage(hero) > hero.Health)
                            _r.Cast(hero, Packets(), true);
                }
            }
        }

        private static void Forest()
        {
            var manacheck = _player.Mana >
                            _player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                            _player.Spellbook.GetSpell(SpellSlot.E).ManaCost;
            _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            if (Config.Item("UseEQF").GetValue<bool>() && _q.IsReady() && E.IsReady() && manacheck)
            {
                E.Cast(Game.CursorPos, Packets());
                _q.Cast(Game.CursorPos, Packets());
            }
            if (Config.Item("UseWF").GetValue<bool>() && _w.IsReady() && target != null &&
                _player.Distance(target) < _w.Range)
            {
                _w.Cast();
            }

        }

        private static void OnCreateObj(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter)) return;
            var obj = (Obj_GeneralParticleEmitter) sender;
            if (sender.Name == "JarvanDemacianStandard_buf_green.troy")
            {
                _epos = sender.Position;
            }
            if (obj != null && obj.IsMe && obj.Name == "JarvanCataclysm_tar")

                //debug
                //if (unit == ObjectManager.Player.Name)
            {
                // Game.PrintChat("Spell: " + name);
                _haveulti = true;
                return;
            }
        }

        private static void OnDeleteObj(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter)) return;
            if (sender.Name == "JarvanDemacianStandard_buf_green.troy")
            {
                _epos = default(Vector3);
            }
            var obj = (Obj_GeneralParticleEmitter) sender;
            if (obj != null && obj.IsMe && obj.Name == "JarvanCataclysm_tar")
            {
                _haveulti = false;
                return;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!Config.Item("Draw.Enable").GetValue<bool>())
                return;

            if (Config.Item("Drawsmite").GetValue<bool>() && _smiteSlot != SpellSlot.Unknown)
            {
                if (Config.Item("Usesmite").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, System.Drawing.Color.DarkOrange,
                        "Smite Is On");
                }
                else
                    Drawing.DrawText(Drawing.Width*0.90f, Drawing.Height*0.68f, System.Drawing.Color.DarkRed,
                        "Smite Is Off");
            }

            if (Config.Item("DrawQ").GetValue<bool>())
            {

                Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
            }
            if (Config.Item("DrawW").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _w.Range, System.Drawing.Color.White);
            }
            if (Config.Item("DrawE").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.White);
            }

            if (Config.Item("DrawR").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
            }
            if (Config.Item("DrawQR").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range + _r.Range, System.Drawing.Color.White);
            }
            if (Config.Item("DrawEQF").GetValue<bool>())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position,
                    _q.Range + Config.Item("FlashDista").GetValue<Slider>().Value, System.Drawing.Color.White);
            }
        }
    }
}
     
  
 




