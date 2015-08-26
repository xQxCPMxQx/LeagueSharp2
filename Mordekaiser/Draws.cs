using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace Mordekaiser
{
    internal class Draws
    {
        public Draws()
        {
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Utils.Player.Self.IsDead)
                return;

            DrawW();
            DrawE();
            DrawR();
            DrawGhost();
        }

        private static void DrawW()
        {
            if (!Menu.MenuW.Item("Allies.Active").GetValue<bool>()) return;

            var drawSearch = Menu.MenuW.Item("DrawW.Search").GetValue<Circle>();
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(Utils.Player.Self.Position, Spells.W.Range, drawSearch.Color, 1);
            }

            var dmgRadiusDraw = Menu.MenuW.Item("DrawW.DamageRadius").GetValue<Circle>();
            if (dmgRadiusDraw.Active)
            {
                Render.Circle.DrawCircle(
                    Utils.Player.Self.Position,
                    Menu.MenuW.Item("UseW.DamageRadius").GetValue<Slider>().Value,
                    dmgRadiusDraw.Color);
            }
        }

        private static void DrawE()
        {
            var drawSearch = Menu.MenuE.Item("DrawE.Search").GetValue<Circle>();
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(Utils.Player.Self.Position, Spells.E.Range, drawSearch.Color, 1);
            }
        }

        private static void DrawR()
        {
            if (!Menu.MenuR.Item("Enemies.Active").GetValue<bool>()) return;

            if (Menu.MenuR.Item("DrawR.Status.Show").GetValue<StringList>().SelectedIndex == 1)
            {
                foreach (var a in HeroManager.Enemies.Where(e => e.IsVisible && !e.IsDead && !e.IsZombie))
                {
                    var vSelected = (Menu.MenuR.Item("Selected" + a.ChampionName).GetValue<StringList>().SelectedIndex);

                    if (Menu.MenuR.Item("DrawR.Status.Show").GetValue<StringList>().SelectedIndex == 2 && vSelected != 3) continue;

                    if (vSelected != 0)
                        Utils.DrawText(
                            vSelected == 3 ? Utils.TextBold : Utils.Text,
                            "Use Ultimate: "
                            + Menu.MenuR.Item("Selected" + a.ChampionName).GetValue<StringList>().SelectedValue,
                            a.HPBarPosition.X + a.BoundingRadius / 2 - 20,
                            a.HPBarPosition.Y - 20,
                            vSelected == 3
                                ? SharpDX.Color.Red
                                : (vSelected == 2 ? SharpDX.Color.Yellow : SharpDX.Color.Gray));
                }
            }

            var drawSearch = Menu.MenuR.Item("DrawR.Search").GetValue<Circle>();
            if (drawSearch.Active)
            {
                Render.Circle.DrawCircle(Utils.Player.Self.Position, Spells.R.Range, drawSearch.Color, 1);
            }
        }

        private static void DrawGhost()
        {
            var ghost = Utils.HowToTrainYourDragon;
            if (ghost == null) 
                return;

            if (Menu.MenuGhost.Item("Ghost.Draw.Position").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(ghost.Position, 105f, Menu.MenuGhost.Item("Draw.Ghost.Position").GetValue<Circle>().Color);
            }

            if (Menu.MenuGhost.Item("Ghost.Draw.AARange").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(ghost.Position, ghost.AttackRange, Menu.MenuGhost.Item("Draw.Ghost.AARange").GetValue<Circle>().Color);
            }

            if (Menu.MenuGhost.Item("Ghost.Draw.ControlRange").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(ghost.Position, 1500f, Menu.MenuGhost.Item("Draw.Ghost.ControlRange").GetValue<Circle>().Color);
            }


        }
    }
}