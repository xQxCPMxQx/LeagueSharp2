using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace KaiHelper.Tracker
{
    internal class WayPoint
    {
        private readonly Font _largefont;
        private readonly Menu _menu;
        private readonly Font _smallfont;

        public WayPoint(Menu menu)
        {
            _largefont = new Font(Drawing.Direct3DDevice, new FontDescription { FaceName = "Calibri", Height = 20, });
            _smallfont = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Calibri",
                    Height = 13,
                    Weight = FontWeight.Regular,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });
            _menu = menu.AddSubMenu(new Menu("Waypoint", "WaypointTracker"));
            _menu.AddItem(new MenuItem("AWPMiniMap", "On MiniMap").SetValue(false));
            _menu.AddItem(new MenuItem("AWPMap", "Active").SetValue(false));
            Drawing.OnEndScene += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (!_menu.Item("AWPMap").GetValue<bool>())
            {
                return;
            }
            foreach (Obj_AI_Hero hero in
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(hero => hero.IsEnemy && hero.IsVisible && !hero.IsDead && hero.IsValid && hero.IsMoving))
            {
                List<Vector2> waypoints = hero.GetWaypoints();
                int numPoint = waypoints.Count - 1;
                int lengthPoint = waypoints.Count - 2;
                float timer = 0;
                for (int i = 0; i < numPoint; i++)
                {
                    Vector3 beginPoint = waypoints[i].To3D();
                    Vector3 endPoint = waypoints[i + 1].To3D();
                    timer += beginPoint.Distance(endPoint) / ObjectManager.Player.MoveSpeed;
                    Vector2 p1Map = Drawing.WorldToScreen(beginPoint);
                    Vector2 p2Map = Drawing.WorldToScreen(endPoint);
                    if (i != lengthPoint)
                    {
                        Drawing.DrawLine(p1Map[0], p1Map[1], p2Map[0], p2Map[1], 2, Color.White);
                    }
                    else
                    {
                        float r = 25 / p2Map.Distance(p1Map);
                        var enp = new Vector2(r * p1Map.X + (1 - r) * p2Map.X, r * p1Map.Y + (1 - r) * p2Map.Y);
                        Drawing.DrawLine(p1Map[0], p1Map[1], enp[0], enp[1], 2, Color.White);
                        Render.Circle.DrawCircle(endPoint, 50, Color.Red);
                        Render.Circle.DrawCircle(endPoint, 50, Color.FromArgb(50, Color.Red), -2);
                        Helper.DrawText(_largefont, timer.ToString("F"), (int) p2Map[0], (int) p2Map[1] - 10, SharpDX.Color.White);
                        Helper.DrawText(_largefont, hero.SkinName, (int) p2Map[0], (int) p2Map[1] + 18, SharpDX.Color.White);
                    }
                    if (_menu.Item("AWPMiniMap").GetValue<bool>())
                    {
                        Vector2 p1MiMap = Drawing.WorldToMinimap(beginPoint);
                        Vector2 p2MiMap = Drawing.WorldToMinimap(endPoint);
                        if (i == lengthPoint)
                        {
                            Helper.DrawText(_smallfont, hero.SkinName, (int) p2MiMap.X, (int) p2MiMap.Y - 6, SharpDX.Color.Pink);
                        }
                        Drawing.DrawLine(p1MiMap[0], p1MiMap[1], p2MiMap[0], p2MiMap[1], 1, Color.Yellow);
                    }
                }
            }
        }
    }
}