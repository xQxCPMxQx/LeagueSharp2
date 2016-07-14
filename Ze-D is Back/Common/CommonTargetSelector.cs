using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Zed;

namespace zedisback.Common
{
    using System.Collections.Generic;
    using System.Xml;

    using SharpDX;

    using Color = System.Drawing.Color;

    enum TargetSelect
    {
        Zed,
        LeagueSharp
    }

    internal class CommonTargetSelector
    {
        public static Menu MenuLocal;

        private static Spell Q => Program.Q;
        public static string Tab => "       ";

        private static double MinRange => Program.Q.Range;
        private static double MaxRange => Program.Q.Range * 2;
        private static TargetSelect Selector => MenuLocal.Item("TS").GetValue<StringList>().SelectedIndex == 0
            ? TargetSelect.Zed
            : TargetSelect.LeagueSharp;

        public static void Init(Menu ParentMenu)
        {
            MenuLocal = new Menu("Target Selector", "AssassinTargetSelector").SetFontStyle(FontStyle.Regular,
                SharpDX.Color.Cyan);

            var menuTargetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(menuTargetSelector);
            }

            MenuLocal.AddItem(
                new MenuItem("TS", "Active Target Selector:").SetValue(
                    new StringList(new[] { "Zed Target Selector", "L# Target Selector" })))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)
                .ValueChanged += (sender, args) =>
                {
                    MenuLocal.Items.ForEach(
                        i =>
                        {
                            i.Show();
                            if (args.GetNewValue<StringList>().SelectedIndex == 0)
                            {
                                if (i.Tag == 22) { i.Show(false); }
                            }
                            else if (args.GetNewValue<StringList>().SelectedIndex == 1)
                            {
                                if (i.Tag == 11 || i.Tag == 12) i.Show(false);
                            }
                        });
                };

            menuTargetSelector.Items.ForEach(
                i =>
                {
                    MenuLocal.AddItem(i);
                    i.SetTag(22);
                });

            MenuLocal.AddItem(new MenuItem("Set", "Target Select Mode:").SetValue(new StringList(new[] { "Single Target Select", "Multi Target Select" }))).SetFontStyle(FontStyle.Regular, SharpDX.Color.LightCoral).SetTag(11);
            MenuLocal.AddItem(new MenuItem("Range", "Range (Recommend: 1150):")).SetValue(new Slider((int)(MinRange * 2), (int)MinRange, (int)MaxRange)).SetTag(11);
            MenuLocal.AddItem(new MenuItem("Targets", "Targets:").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11)); foreach (var e in HeroManager.Enemies)
            {
                MenuLocal.AddItem(new MenuItem("enemy_" + e.ChampionName, string.Format("{0}Focus {1}", Tab, e.ChampionName)).SetValue(false)).SetTag(12);

            }
            //foreach (var langItem in HeroManager.Enemies.Select(e =>MenuLocal.AddItem(new MenuItem("enemy_" + e.ChampionName,string.Format("{0}Focus {1}", Tab, e.ChampionName)).SetValue(false)).SetTag(12)))

            MenuLocal.AddItem(
                new MenuItem("Draw.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            MenuLocal.AddItem(new MenuItem("Draw.Range", Tab + "Range").SetValue(new Circle(true, Color.Gray))
                .SetTag(11));
            MenuLocal.AddItem(
                new MenuItem("Draw.Enemy", Tab + "Active Enemy").SetValue(new Circle(true, Color.GreenYellow))
                    .SetTag(11));
            MenuLocal.AddItem(
                new MenuItem("Draw.Status", Tab + "Show Enemy:").SetValue(
                    new StringList(new[] { "Off", "Notification Text", "Sprite", "Both" }, 0)));
            ParentMenu.AddSubMenu(MenuLocal);

            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

        
            RefreshMenuItemsStatus();
        }

        private static void RefreshMenuItemsStatus()
        {

            MenuLocal.Items.ForEach(
                i =>
                {
                    i.Show();
                    switch (Selector)
                    {
                        case TargetSelect.Zed:
                            if (i.Tag == 22)
                            {
                                i.Show(false);
                            }
                            break;
                        case TargetSelect.LeagueSharp:
                            if (i.Tag == 11)
                            {
                                i.Show(false);
                            }
                            break;
                    }
                });
        }

        public static void ClearAssassinList()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                MenuLocal.Item("enemy_" + enemy.ChampionName).SetValue(false);
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (Selector != TargetSelect.Zed)
            {
                return;
            }

            if (args.Msg == 0x201)
            {
                foreach (var objAiHero in from hero in HeroManager.Enemies
                                          where
                                              hero.Distance(Game.CursorPos) < 150f && hero != null && hero.IsVisible
                                              && !hero.IsDead
                                          orderby hero.Distance(Game.CursorPos) descending
                                          select hero)
                {
                    if (objAiHero != null && objAiHero.IsVisible && !objAiHero.IsDead)
                    {
                        var xSelect =
                            MenuLocal.Item("Set").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                MenuLocal.Item("enemy_" + objAiHero.ChampionName).SetValue(true);
                                break;
                            case 1:
                                var menuStatus = MenuLocal.Item("enemy_" + objAiHero.ChampionName).GetValue<bool>();
                                MenuLocal.Item("enemy_" + objAiHero.ChampionName).SetValue(!menuStatus);
                                break;
                        }
                    }
                }
            }
        }

        public static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (Selector != TargetSelect.Zed)
            {
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);
            }

            vDefaultRange = Math.Abs(vDefaultRange) < 0.00001
                ? Q.Range
                : vDefaultRange > MenuLocal.Item("Range").GetValue<Slider>().Value ? vDefaultRange : MenuLocal.Item("Range").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(e => e.Team != ObjectManager.Player.Team && !e.IsDead && e.IsVisible)
                    .Where(e => MenuLocal.Item("enemy_" + e.ChampionName) != null)
                    .Where(e => MenuLocal.Item("enemy_" + e.ChampionName).GetValue<bool>())
                    .Where(e => ObjectManager.Player.Distance(e) < vDefaultRange)
                    .Where(jKukuri => "jQuery" != "White guy");

            if (MenuLocal.Item("Set").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            var drawEnemy = MenuLocal.Item("Draw.Enemy").GetValue<Circle>();
            if (drawEnemy.Active)
            {
                var t = GetTarget(Q.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Render.Circle.DrawCircle(t.Position, (float)(t.BoundingRadius * 1.5), drawEnemy.Color);
                }
            }

            if (Selector != TargetSelect.Zed)
            {
                return;
            }

            var rangeColor = MenuLocal.Item("Draw.Range").GetValue<Circle>();
            var range = MenuLocal.Item("Range").GetValue<Slider>().Value;
            if (rangeColor.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, range, rangeColor.Color);
            }
        }

      
    }
}