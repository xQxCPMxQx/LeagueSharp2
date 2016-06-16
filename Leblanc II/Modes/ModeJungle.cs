using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Champion;
using Leblanc.Common;
using Color = SharpDX.Color;

namespace Leblanc.Modes
{
    internal static class ModeJungle
    {
        public static Menu MenuLocal { get; private set; }
        public static Menu MenuMinMana { get; private set; }

        private static Spell Q => PlayerSpells.Q;
        private static Spell W => PlayerSpells.W;
        private static Spell E => PlayerSpells.E;

        public static void Init(Menu mainMenu)
        {
            MenuLocal = new Menu("Jungle", "Jungle");
            {
                InitSimpleMenu();

                MenuLocal.AddItem(new MenuItem("Jungle.Youmuu.BlueRed", "Items: Use for Blue/Red").SetValue(new StringList(new[] { "Off", "Red", "Blue", "Both" }, 3))).SetFontStyle(FontStyle.Regular, Colors.ColorItems);
                MenuLocal.AddItem(new MenuItem("Jungle.Youmuu.BaronDragon", "Items: Use for Baron/Dragon").SetValue(new StringList(new[] { "Off", "Dragon", "Baron", "Both" }, 3))).SetFontStyle(FontStyle.Regular, Colors.ColorItems);
                MenuLocal.AddItem(new MenuItem("Jungle.Item", "Items: Other (Tiamat/Hydra)").SetValue(new StringList(new[] { "Off", "On" }, 1))).SetFontStyle(FontStyle.Regular, Colors.ColorItems);

            }
            mainMenu.AddSubMenu(MenuLocal);
            Game.OnUpdate += OnUpdate;
        }

        static void InitSimpleMenu()
        {
            MenuLocal.AddItem(new MenuItem("Jungle.Simple.Q.Big", "Q Big Mobs:").SetValue(new StringList(new[] { "Off", "On" }, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());
            MenuLocal.AddItem(new MenuItem("Jungle.Simple.Q.Small", "Q Small Mobs:").SetValue(new StringList(new[] { "Off", "On: If Killable" }, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.Q.MenuColor());

            string[] strESimple = new string[5];
            {
                strESimple[0] = "Off";
                strESimple[1] = "Big Mobs";
                for (var i = 2; i < 5; i++)
                {
                    strESimple[i] = "If Need to AA Count >= " + (i + 2);
                }
                MenuLocal.AddItem(new MenuItem("Jungle.Simple.W", "W:").SetValue(new StringList(strESimple, 4))).SetFontStyle(FontStyle.Regular, PlayerSpells.W.MenuColor());
            }

            MenuLocal.AddItem(new MenuItem("Jungle.Simple.E", "E:").SetValue(new StringList(new[] { "Off", "On: Big Mobs", "On: Big Mobs [Just can stun]" }, 1))).SetFontStyle(FontStyle.Regular, PlayerSpells.E.MenuColor());


            MenuMinMana = new Menu("Min. Mana Control", "Menu.MinMana");

            MenuMinMana.AddItem(new MenuItem("MinMana.Jungle", "Min. Mana %:").SetValue(new Slider(20, 100, 0))).SetFontStyle(FontStyle.Regular, Color.LightGreen);

            MenuMinMana.AddItem(new MenuItem("MinMana.DontCheckEnemyBuff", "Don't Check Min. Mana -> If Taking:").SetValue(new StringList(new[] { "Off", "Ally Buff", "Enemy Buff", "Both" }, 2))).SetFontStyle(FontStyle.Regular, Color.Wheat);
            MenuMinMana.AddItem(new MenuItem("MinMana.DontCheckBlueBuff", "Don't Check Min. Mana -> If Have Blue Buff:").SetValue(true)).SetFontStyle(FontStyle.Regular, Color.Wheat);

            MenuLocal.AddItem(new MenuItem("MinMana.Default", "Load Recommended Settings").SetValue(true))
                .SetFontStyle(FontStyle.Regular, Color.GreenYellow)
                .ValueChanged +=
                (sender, args) =>
                {
                    if (args.GetNewValue<bool>() == true)
                    {
                        LoadDefaultSettings();
                    }
                };

            MenuLocal.AddSubMenu(MenuMinMana);
        }

        public static void LoadDefaultSettings()
        {
            MenuLocal.Item("Jungle.Simple.Q.Big").SetValue(new StringList(new[] {"Off", "On"}, 1));
            MenuLocal.Item("Jungle.Simple.Q.Small").SetValue(new StringList(new[] {"Off", "On: If Killable"}, 1));

            string[] strESimple = new string[5];
            {
                strESimple[0] = "Off";
                strESimple[1] = "Big Mobs";
                for (var i = 2; i < 5; i++)
                {
                    strESimple[i] = "If Need to AA Count >= " + (i + 2);
                }
                MenuLocal.Item("Jungle.Simple.W").SetValue(new StringList(strESimple, 4));
            }

            MenuLocal.Item("Jungle.Simple.E").SetValue(new StringList(new[] {"Off", "On: Big Mobs", "On: Big Mobs [Just can stun]"}, 1));

            MenuMinMana.Item("MinMana.Jungle").SetValue(new Slider(20, 100, 0));
            MenuMinMana.Item("MinMana.DontCheckEnemyBuff").SetValue(new StringList(new[] { "Off", "Ally Buff", "Enemy Buff", "Both" }, 2));
            MenuMinMana.Item("MinMana.DontCheckBlueBuff").SetValue(true);
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                ExecuteSimpleMode();
            }
        }

        public static float JungleMinManaPercent(Obj_AI_Base mob)
        {
            // Enable / Disable Min Mana
            if (!ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active)
            {
                return 0f;
            }

            // Don't Control Min Mana if I have blue buff
            if (MenuMinMana.Item("MinMana.DontCheckBlueBuff").GetValue<bool>() && ObjectManager.Player.HasBlueBuff())
            {
                return 0f;
            }

            // Don't check min mana If I'm taking enemy blue / red
            var dontCheckMinMana = MenuMinMana.Item("MinMana.DontCheckEnemyBuff").GetValue<StringList>().SelectedIndex;

            if ((dontCheckMinMana == 1 || dontCheckMinMana == 3)
                && mob.GetMobTeam(Q.Range) == (CommonManaManager.GameObjectTeam)ObjectManager.Player.Team
                && (mob.SkinName == "SRU_Blue" || mob.SkinName == "SRU_Red"))
            {
                return 0f;
            }

            if ((dontCheckMinMana == 2 || dontCheckMinMana == 3)
                && mob.GetMobTeam(Q.Range) != (CommonManaManager.GameObjectTeam)ObjectManager.Player.Team
                && (mob.SkinName == "SRU_Blue" || mob.SkinName == "SRU_Red"))
            {
                return 0f;
            }

            return MenuMinMana.Item("MinMana.Jungle").GetValue<Slider>().Value;
        }

        private static void ExecuteSimpleMode()
        {
            if (!ModeConfig.MenuFarm.Item("Farm.Enable").GetValue<KeyBind>().Active)
            {
                return;
            }

            var mobs = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count <= 0)
            {
                return;
            }

            var mob = mobs[0];

            if (!Common.CommonHelper.ShouldCastSpell(mob))
            {
                return;
            }


            if (ObjectManager.Player.ManaPercent < JungleMinManaPercent(mob))
            {
                return;
            }

            if (Q.CanCast(mob))
            {

                var useQBig = MenuLocal.Item("Jungle.Simple.Q.Big").GetValue<StringList>().SelectedIndex;
                var useQSmall = MenuLocal.Item("Jungle.Simple.Q.Small").GetValue<StringList>().SelectedIndex;

                if (useQBig == 1 && CommonManaManager.GetMobType(mob, CommonManaManager.FromMobClass.ByType) == CommonManaManager.MobTypes.Big)
                {
                    //Champion.PlayerSpells.CastQObjects(mob);
                }

                if (useQSmall == 1 && CommonManaManager.GetMobType(mob, CommonManaManager.FromMobClass.ByType) != CommonManaManager.MobTypes.Big && mob.CanKillableWith(Q))
                {
                    //Champion.PlayerSpells.CastQObjects(mob);
                }
            }


            if (W.IsReady() && MenuLocal.Item("Jungle.Simple.W").GetValue<StringList>().SelectedIndex != 0 && mob.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65))
            {
                var totalAa = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.Team == GameObjectTeam.Neutral && m.IsValidTarget(Orbwalking.GetRealAutoAttackRange(null) + 65)).Sum(m => (int)m.Health);

                totalAa = (int)(totalAa / ObjectManager.Player.TotalAttackDamage);
                if (totalAa >= MenuLocal.Item("Jungle.Simple.W").GetValue<StringList>().SelectedIndex + 2 || CommonManaManager.GetMobType(mobs[0], CommonManaManager.FromMobClass.ByType) == CommonManaManager.MobTypes.Big)
                {
                    W.Cast();
                }
            }

            if (E.CanCast(mob) && MenuLocal.Item("Jungle.Simple.E").GetValue<StringList>().SelectedIndex != 0)
            {
                var useE = MenuLocal.Item("Jungle.Simple.E").GetValue<StringList>().SelectedIndex;

                if (useE == 1 && CommonManaManager.GetMobType(mob, CommonManaManager.FromMobClass.ByType) == CommonManaManager.MobTypes.Big)
                {
                    Champion.PlayerSpells.E.CastOnUnit(mob);
                }

                if (useE == 2 && CommonManaManager.GetMobType(mob, CommonManaManager.FromMobClass.ByType) == CommonManaManager.MobTypes.Big && mob.CanStun())
                {
                    Champion.PlayerSpells.E.CastOnUnit(mob);
                }
            }
        }
    }
}
