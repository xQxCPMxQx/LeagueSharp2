using System;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;

namespace Nocturne.Common
{
    using System.Linq;

    internal static class CommonManaBar
    {
        public static Line DxLine;

        public static Device DxDevice = Drawing.Direct3DDevice;

        public static float Width = 104;
        public static Menu LocalMenu => Modes.ModeDraw.SubMenuManaBarIndicator;
        private static Spell Q => PlayerSpells.Q;
        private static Spell W => PlayerSpells.W;
        private static Spell E => PlayerSpells.E;
        private static Spell R => PlayerSpells.R;

        private static bool InTeamFight(this Obj_AI_Hero player, float range)
        {
            var enemies = HeroManager.Enemies.Where(e => e.Distance(player.Position) < range);

            if (enemies.Any())
            {
                return true;
            }

            return false;
        }

        private static bool InJungle(this Obj_AI_Hero player, float range)
        {
            var mobs = LeagueSharp.Common.MinionManager.GetMinions(
                player.ServerPosition,
                range,
                MinionTypes.All,
                MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static bool InLane(this Obj_AI_Hero player, float range)
        {
            var minions = LeagueSharp.Common.MinionManager.GetMinions(player.ServerPosition, range);

            if (minions.Count > 0)
            {
                return true;
            }

            return false;
        }

        internal static void Initialize(Menu mainMenu)
        {
            DxLine = new Line(DxDevice) {Width = 4};

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

            Drawing.OnEndScene += eventArgs =>
            {
                var color = new ColorBGRA(255, 255, 255, 255);

                var qMana = new[] {0, 40, 50, 60, 70, 80};
                var wMana = new[] {0, 60, 70, 80, 90, 100}; // W Mana Cost doesnt works :/
                var eMana = new[] {0, 50, 50, 50, 50, 50};
                var rMana = new[] {0, 100, 100, 100};

                var totalCostMana = 0;

                if (LocalMenu.Item(Modes.ModeDraw.GetPcModeStringValue + "DrawManaBar.Q").GetValue<bool>())
                {
                    totalCostMana += qMana[Q.Level];
                }

                if (LocalMenu.Item(Modes.ModeDraw.GetPcModeStringValue + "DrawManaBar.W").GetValue<bool>())
                {
                    totalCostMana += wMana[W.Level];
                }

                if (LocalMenu.Item(Modes.ModeDraw.GetPcModeStringValue + "DrawManaBar.E").GetValue<bool>())
                {
                    totalCostMana += eMana[E.Level];
                }

                if (LocalMenu.Item(Modes.ModeDraw.GetPcModeStringValue + "DrawManaBar.R").GetValue<bool>())
                {
                    totalCostMana += rMana[R.Level];
                }

                DrawManaPercent(totalCostMana,
                    totalCostMana > ObjectManager.Player.Mana
                        ? new ColorBGRA(255, 0, 0, 255)
                        : new ColorBGRA(255, 255, 255, 255));

            };
        }

        private static Vector2 Offset => new Vector2(34, 9);

        public static Vector2 StartPosition
            =>
                new Vector2(ObjectManager.Player.HPBarPosition.X + Offset.X,
                    ObjectManager.Player.HPBarPosition.Y + Offset.Y + 8);

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            DxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            DxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            DxLine.OnLostDevice();
        }

        private static float GetManaProc(float manaPer)
        {
            return (manaPer/ObjectManager.Player.MaxMana);
        }

        private static Vector2 GetHpPosAfterDmg(float mana)
        {
            float w = Width/ObjectManager.Player.MaxMana*mana;
            return new Vector2(StartPosition.X + w, StartPosition.Y);
        }

        public static void DrawManaPercent(float dmg, ColorBGRA color)
        {
            Vector2 pos = GetHpPosAfterDmg(dmg);

            FillManaBar(pos, color);
        }

        private static void FillManaBar(Vector2 pos, ColorBGRA color)
        {
            DxLine.Begin();
            DxLine.Draw(
                new[] {new Vector2((int) pos.X, (int) pos.Y + 4f), new Vector2((int) pos.X + 2, (int) pos.Y + 4f)},
                color);
            DxLine.End();
        }
    }
}