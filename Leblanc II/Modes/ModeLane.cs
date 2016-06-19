using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Leblanc.Common;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;

namespace Leblanc.Modes
{
    internal static class ModeLane
    {
        public static Menu MenuLocal { get; private set; }
        private static Spell Q => Champion.PlayerSpells.Q;
        private static Spell W => Champion.PlayerSpells.W;
        private static Spell E => Champion.PlayerSpells.E;
        private static Spell R => Champion.PlayerSpells.R;

        public static void Init(Menu mainMenu)
        {
            MenuLocal = new Menu("Lane", "Lane");
            {
                //MenuLocal.AddItem(new MenuItem("Lane.LaneQuick", "Fast Lane Clear Mode:").SetValue(new KeyBind('T', KeyBindType.Toggle))).SetFontStyle(FontStyle.Regular, SharpDX.Color.DarkKhaki).SetTooltip("Using all spell for fast clear lane. Tip: Use for under ally turret farm").Permashow(true, ObjectManager.Player.ChampionName + " | Quick Lane Clear");

                MenuLocal.AddItem(new MenuItem("Lane.UseQ", "Q Last Hit:").SetValue(new StringList(new[] { "Off", "On: Last Hit", "On: Unkillable Minions", "On: Both" }, 2))).SetFontStyle(FontStyle.Regular, Q.MenuColor());

                string[] strW = new string[6];
                {
                    strW[0] = "Off";
                    for (var i = 1; i < 6; i++)
                    {
                        strW[i] = "Killable Minion Count >= " + (i + 3);
                    }
                    MenuLocal.AddItem(new MenuItem("Lane.UseW", "W:").SetValue(new StringList(strW, 4))).SetFontStyle(FontStyle.Regular, W.MenuColor());
                }

                string[] strWR = new string[8];
                {
                    strWR[0] = "Off";
                    for (var i = 1; i < 8; i++)
                    {
                        strWR[i] = "Killable Minion Count >= " + (i + 3);
                    }
                    MenuLocal.AddItem(new MenuItem("Lane.UseR", "R [Mega W]:").SetValue(new StringList(strWR, 6))).SetFontStyle(FontStyle.Regular, W.MenuColor());
                }
                MenuLocal.AddItem(new MenuItem("Lane.MinMana.Alone", "Min. Mana: I'm Alone %").SetValue(new Slider(30, 100, 0))).SetFontStyle(FontStyle.Regular, SharpDX.Color.LightSkyBlue).SetTag(2);
                MenuLocal.AddItem(new MenuItem("Lane.MinMana.Enemy", "Min. Mana: I'm NOT Alone (Enemy Close) %").SetValue(new Slider(60, 100, 0))).SetFontStyle(FontStyle.Regular, SharpDX.Color.IndianRed).SetTag(2);
                MenuLocal.AddItem(new MenuItem("MinMana.Jungle.Default", "Load Recommended Settings").SetValue(true).SetTag(9)).SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow).ValueChanged += (sender, args) =>
                    {
                        if (args.GetNewValue<bool>() == true)
                        {
                            LoadDefaultSettings();
                        }
                    };
            }
            mainMenu.AddSubMenu(MenuLocal);

            Game.OnUpdate += OnUpdate;
        }

        public static void LoadDefaultSettings()
        {
            //MenuLocal.Item("Lane.LaneQuick").SetValue(new KeyBind('T', KeyBindType.Toggle));
            MenuLocal.Item("Lane.UseQ").SetValue(new StringList(new[] { "Off", "On: Last Hit", "On: Unkillable Minions", "On: Both" }, 2));

            string[] strW = new string[6];
            {
                strW[0] = "Off";
                for (var i = 1; i < 6; i++)
                {
                    strW[i] = "Killable Minion Count >= " + (i + 3);
                }
                MenuLocal.Item("Lane.UseW").SetValue(new StringList(strW, 4));
            }

            string[] strWR = new string[8];
            {
                strWR[0] = "Off";
                for (var i = 1; i < 8; i++)
                {
                    strWR[i] = "Killable Minion Count >= " + (i + 3);
                }
                MenuLocal.Item("Lane.UseR").SetValue(new StringList(strWR, 6));
            }

            MenuLocal.Item("Lane.MinMana.Alone").SetValue(new Slider(30, 100, 0));
            MenuLocal.Item("Lane.MinMana.Enemy").SetValue(new Slider(60, 100, 0));
        }

        public static float LaneMinManaPercent
        {
            get
            {
                if (ModeConfig.MenuFarm.Item("Farm.MinMana.Enable").GetValue<KeyBind>().Active)
                {
                    return HeroManager.Enemies.Find(e => e.IsValidTarget(2000) && !e.IsZombie) == null
                        ? MenuLocal.Item("Lane.MinMana.Alone").GetValue<Slider>().Value
                        : MenuLocal.Item("Lane.MinMana.Enemy").GetValue<Slider>().Value;
                }

                return 0f;
            }
        }
        private static void OnUpdate(EventArgs args)
        {
            if (ModeConfig.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.LaneClear)
            {
                return;
            }

            if (!ModeConfig.MenuFarm.Item("Farm.Enable").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (ObjectManager.Player.ManaPercent < LaneMinManaPercent)
            {
                return;
            }

            ExecuteQ();
        }

        private static void ExecuteQ()
        {
            var xUseQ = MenuLocal.Item("Lane.UseQ").GetValue<StringList>().SelectedIndex;
            if (Q.IsReady() && xUseQ != 0)
            {
                var minionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);

                if (xUseQ == 1 || xUseQ == 3)
                {
                    foreach (Obj_AI_Base vMinion in
                        from vMinion in minionsQ
                        let vMinionQDamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q)
                        where
                            vMinion.Health <= vMinionQDamage &&
                            vMinion.Health > ObjectManager.Player.GetAutoAttackDamage(vMinion)
                        select vMinion)
                    {
                        Q.CastOnUnit(vMinion);
                    }

                }

                if (xUseQ == 2 || xUseQ == 3)
                {

                    foreach (
                        var minion in
                            minionsQ.Where(
                                m =>
                                    HealthPrediction.GetHealthPrediction(m,
                                        (int)(ObjectManager.Player.AttackCastDelay * 1000), Game.Ping / 2) < 0)
                                .Where(m => m.Health <= Q.GetDamage(m)))
                    {
                        Q.CastOnUnit(minion);
                    }
                }
            }
        }

        private static void ExecuteQuickLaneClear()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (!MenuLocal.Item("Lane.Enable").GetValue<KeyBind>().Active)
            {
                return;
            }

            if (Q.IsReady())
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, Q.Range);

                foreach (
                    var minion in
                        MinionManager.GetMinions(Q.Range)
                            .Where(m => m.CanKillableWith(Q) && Q.CanCast(m)))
                {
                   // Champion.PlayerSpells.CastQObjects(minion);
                }
            }

            if (!Q.IsReady() && E.IsReady())
            {
                var minions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, E.Range);

                foreach (
                    var minion in
                        minions.Where(
                            m =>
                                HealthPrediction.GetHealthPrediction(m,
                                    (int) (ObjectManager.Player.AttackCastDelay*1000), Game.Ping/2 - 100) < 0)
                            .Where(m => m.CanKillableWith(E) && E.CanCast(m)))
                {
                    //Champion.PlayerSpells.CastQObjects(minion);
                }
            }
        }
    }
}