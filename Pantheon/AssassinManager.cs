using System;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Pantheon
{
    using System.Collections.Generic;
    using System.Xml;

    using SharpDX;
    using SharpDX.Direct3D9;

    using Color = System.Drawing.Color;

    internal enum TargetSelect
    {
        Pantheon,
        LeagueSharp
    }

    internal class AssassinManager
    {
        public Menu Config;
        public static Font Text;

        private TargetSelect Selector
        {
            get
            {
                return this.Config.Item("TS").GetValue<StringList>().SelectedIndex == 0
                    ? TargetSelect.Pantheon
                    : TargetSelect.LeagueSharp;
            }
        }

        public void Load()
        {
            Text = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Malgun Gothic",
                    Height = 21,
                    OutputPrecision = FontPrecision.Default,
                    Weight = FontWeight.Bold,
                    Quality = FontQuality.ClearTypeNatural
                });

            this.Config = new Menu("Target Selector", "AssassinTargetSelector").SetFontStyle(FontStyle.Regular,
                SharpDX.Color.Cyan);

            var menuTargetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(menuTargetSelector);
            }

            this.Config.AddItem(
                new MenuItem("TS", "Active Target Selector:").SetValue(
                    new StringList(new[] {"Pantheon Target Selector", "L# Target Selector"})))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.GreenYellow)
                .ValueChanged += (sender, args) =>
                {
                    this.Config.Items.ForEach(
                        i =>
                        {
                            i.Show();
                            switch (args.GetNewValue<StringList>().SelectedIndex)
                            {
                                case 0:
                                    if (i.Tag == 22) i.Show(false);
                                    break;
                                case 1:
                                    if (i.Tag == 11 || i.Tag == 12) i.Show(false);
                                    break;
                            }
                        });
                };

            menuTargetSelector.Items.ForEach(
                i =>
                {
                    this.Config.AddItem(i);
                    i.SetTag(22);
                });

            this.Config.AddItem(
                new MenuItem("Set", "Target Select Mode:").SetValue(
                    new StringList(new[] {"Single Target Select", "Multi Target Select"})))
                .SetFontStyle(FontStyle.Regular, SharpDX.Color.LightCoral)
                .SetTag(11);
            this.Config.AddItem(new MenuItem("Range", "Range (Recommend: 1000):"))
                .SetValue(new Slider(1150, (int) Program.Q.Range, (int) Program.Q.Range*2))
                .SetTag(11);

            this.Config.AddItem(
                new MenuItem("Targets", "Targets:").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            foreach (var e in HeroManager.Enemies)
            {
                this.Config.AddItem(
                    new MenuItem("enemy_" + e.ChampionName, string.Format("{0}Focus {1}", Program.Tab, e.ChampionName))
                        .SetValue(false)).SetTag(12);
            }

            this.Config.AddItem(
                new MenuItem("Draw.Title", "Drawings").SetFontStyle(FontStyle.Regular, SharpDX.Color.Aqua).SetTag(11));
            this.Config.AddItem(
                new MenuItem("Draw.Range", Program.Tab + "Range").SetValue(new Circle(true, Color.Gray)).SetTag(11));
            this.Config.AddItem(
                new MenuItem("Draw.Enemy", Program.Tab + "Active Enemy").SetValue(new Circle(true, Color.GreenYellow))
                    .SetTag(11));
            this.Config.AddItem(
                new MenuItem("Draw.Status", Program.Tab + "Show Enemy:").SetValue(
                    new StringList(new[] {"Off", "Notification Text", "Sprite", "Both"}, 0)));
            Program.Config.AddSubMenu(this.Config);

            Game.OnWndProc += this.Game_OnWndProc;
            Drawing.OnDraw += this.Drawing_OnDraw;

            this.RefreshMenuItemsStatus();
        }

        private void RefreshMenuItemsStatus()
        {

            this.Config.Items.ForEach(
                i =>
                {
                    i.Show();
                    switch (this.Selector)
                    {
                        case TargetSelect.Pantheon:
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

        public void ClearAssassinList()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy))
            {
                this.Config.Item("enemy_" + enemy.ChampionName).SetValue(false);
            }
        }

        private void Game_OnWndProc(WndEventArgs args)
        {
            if (this.Selector != TargetSelect.Pantheon)
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
                            Program.Config.Item("Set").GetValue<StringList>().SelectedIndex;

                        switch (xSelect)
                        {
                            case 0:
                                ClearAssassinList();
                                Program.Config.Item("enemy_" + objAiHero.ChampionName).SetValue(true);
                                break;
                            case 1:
                                var menuStatus = Program.Config.Item("enemy_" + objAiHero.ChampionName).GetValue<bool>();
                                Program.Config.Item("enemy_" + objAiHero.ChampionName).SetValue(!menuStatus);
                                break;
                        }
                    }
                }
            }
        }

        public Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (this.Selector != TargetSelect.Pantheon)
            {
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);
            }

            vDefaultRange = Math.Abs(vDefaultRange) < 0.00001
                ? Program.E.Range
                : this.Config.Item("Range").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(e => e.Team != Program.Player.Team && !e.IsDead && e.IsVisible)
                    .Where(e => this.Config.Item("enemy_" + e.ChampionName) != null)
                    .Where(e => this.Config.Item("enemy_" + e.ChampionName).GetValue<bool>())
                    .Where(e => Program.Player.Distance(e) < vDefaultRange)
                    .Where(jKukuri => "jQuery" != "White guy");

            if (this.Config.Item("Set").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            var objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            var t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            var drawEnemy = this.Config.Item("Draw.Enemy").GetValue<Circle>();
            if (drawEnemy.Active)
            {
                var t = this.GetTarget(Program.E.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    Render.Circle.DrawCircle(t.Position, (float) (t.BoundingRadius*1.5), drawEnemy.Color);
                }
            }

            if (this.Selector != TargetSelect.Pantheon)
            {
                return;
            }

            var rangeColor = this.Config.Item("Draw.Range").GetValue<Circle>();
            var range = this.Config.Item("Range").GetValue<Slider>().Value;
            if (rangeColor.Active)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, range, rangeColor.Color);
            }
            var drawStatus = this.Config.Item("Draw.Status").GetValue<StringList>().SelectedIndex;
            if (drawStatus == 1 || drawStatus == 3)
            {
                foreach (var e in
                    HeroManager.Enemies.Where(
                        e =>
                            e.IsVisible && !e.IsDead && this.Config.Item("enemy_" + e.ChampionName) != null
                            && this.Config.Item("enemy_" + e.ChampionName).GetValue<bool>()))
                {
                    DrawText(
                        Text,
                        "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 27,
                        e.HPBarPosition.Y - 23,
                        SharpDX.Color.Black);

                    DrawText(
                        Text,
                        "1st Priority Target",
                        e.HPBarPosition.X + e.BoundingRadius/2f - (e.CharData.BaseSkinName.Length/2f) - 29,
                        e.HPBarPosition.Y - 25,
                        SharpDX.Color.IndianRed);
                }
            }
        }

        public static void DrawText(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int) vPosX, (int) vPosY, vColor);
        }

    }
}