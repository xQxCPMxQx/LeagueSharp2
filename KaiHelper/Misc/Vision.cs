using System;
using System.Collections.Generic;
using System.Linq;
using KaiHelper.Tracker;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace KaiHelper.Misc
{
    internal class Vision
    {
        private readonly Menu menu;
        private List<Vector2> pointList;
        private Vector3 position;
        private int range;
        public Vision(Menu menu)
        {
            this.menu = menu.AddSubMenu(new Menu("Enemy vision", "Enemyvision"));
            this.menu.AddItem(new MenuItem("VongTron", "Only Circle").SetValue(false));
            this.menu.AddItem(new MenuItem("NguoiChoiTest", "Test by me").SetValue(false));
            this.menu.AddItem(new MenuItem("Active", "Active").SetValue(false));
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            if (!menu.Item("Active").GetValue<bool>())
            {
                return;
            }

            var result = new Obj_AI_Base();
            if (menu.Item("NguoiChoiTest").GetValue<bool>())
            {
                result = ObjectManager.Player;
            }
            else
            {
                float dist = float.MaxValue;
                foreach (var objectEnemy in HeroManager.Enemies.Where(e => !e.IsDead))
                {
                    float distance = Vector3.Distance(ObjectManager.Player.Position, objectEnemy.Position);
                    if (!(distance < dist))
                    {
                        continue;
                    }
                    dist = distance;
                    result = objectEnemy;
                }
            }
            position = result.Position;
            if (result is Obj_AI_Hero || result is Obj_AI_Turret)
            {
                range = 1300;
            }
            else
            {
                var rangeWard = Ward.IsWard(result.SkinName);
                 if (rangeWard==0)
                     range = 1200;
            }
            pointList = RangePoints(result.Position, range);
        }

        public static bool LaVatCan(Vector3 position)
        {
            if (!NavMesh.GetCollisionFlags(position).HasFlag(CollisionFlags.Grass))
            {
                return !NavMesh.GetCollisionFlags(position).HasFlag(CollisionFlags.Building) &&
                       NavMesh.GetCollisionFlags(position).HasFlag(CollisionFlags.Wall);
            }
            return true;
        }

        public static List<Vector2> RangePoints(Vector3 position,int tamNhin)
        {
            var listPoint = new List<Vector2>();
            for (int i = 0; i <= 360; i += 1)
            {
                double cosX = Math.Cos(i * Math.PI / 180);
                double sinX = Math.Sin(i * Math.PI / 180);
                var vongngoai = new Vector3(
                    (float)(position.X + tamNhin * cosX), (float)(position.Y + tamNhin * sinX),
                    ObjectManager.Player.Position.Z);
                for (int j = 0; j < tamNhin; j += 100)
                {
                    var vongtrong = new Vector3(
                        (float)(position.X + j * cosX), (float)(position.Y + j * sinX),
                        ObjectManager.Player.Position.Z);
                    if (!LaVatCan(vongtrong))
                    {
                        continue;
                    }
                    if (j != 0)
                    {
                        int left = j - 99, right = j;
                        do
                        {
                            int middle = (left + right) / 2;
                            vongtrong = new Vector3(
                                (float)(position.X + middle * cosX), (float)(position.Y + middle * sinX),
                                ObjectManager.Player.Position.Z);
                            if (LaVatCan(vongtrong))
                            {
                                right = middle;
                            }
                            else
                            {
                                left = middle + 1;
                            }
                        } while (left < right);
                    }
                    vongngoai = vongtrong;
                    break;
                }
                listPoint.Add(Drawing.WorldToScreen(vongngoai));
            }
            return listPoint;
        }
        private void Game_OnDraw(EventArgs args)
        {
            if (!menu.Item("Active").GetValue<bool>())
            {
                return;
            }
            if (menu.Item("VongTron").GetValue<bool>())
            {
                Render.Circle.DrawCircle(position, range, Color.PaleVioletRed);
                return;
            }
            for (int i = 0; i < pointList.Count - 1; i++)
            {
                Drawing.DrawLine(pointList[i], pointList[i + 1], 1, Color.PaleVioletRed);
            }
        }
    }
}