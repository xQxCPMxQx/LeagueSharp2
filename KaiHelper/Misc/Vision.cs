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
        private readonly Menu _menu;
        private List<Vector2> _pointList;
        private Vector3 _position;
        private int _range;
        public Vision(Menu menu)
        {
            _menu = menu.AddSubMenu(new Menu("Enemy vision", "Enemyvision"));
            _menu.AddItem(new MenuItem("VongTron", "Only Circle").SetValue(false));
            _menu.AddItem(new MenuItem("NguoiChoiTest", "Test by me").SetValue(false));
            _menu.AddItem(new MenuItem("Active", "Active").SetValue(false));
            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
        }

        void Game_OnGameUpdate(EventArgs args)
        {
            if (!_menu.Item("Active").GetValue<bool>())
            {
                return;
            }
            var result = new Obj_AI_Base();
            if (_menu.Item("NguoiChoiTest").GetValue<bool>())
            {
                result = ObjectManager.Player;
            }
            else
            {
                float dist = float.MaxValue;
                foreach (Obj_AI_Base objectEnemy in ObjectManager.Get<Obj_AI_Base>().Where(o =>
                    o.Team != ObjectManager.Player.Team &&
                    !o.IsDead &&!o.Name.ToUpper().StartsWith("SRU")))
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
            _position = result.Position;
            if (result is Obj_AI_Hero || result is Obj_AI_Turret)
            {
                _range = 1300;
            }
            else
            {
                var rangeWard = Ward.IsWard(result.SkinName);
                 if (rangeWard==0)
                     _range = 1200;
            }
            _pointList = RangePoints(result.Position, _range);
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
            if (!_menu.Item("Active").GetValue<bool>())
            {
                return;
            }
            if (_menu.Item("VongTron").GetValue<bool>())
            {
                Render.Circle.DrawCircle(_position, _range, Color.PaleVioletRed);
                return;
            }
            for (int i = 0; i < _pointList.Count - 1; i++)
            {
                Drawing.DrawLine(_pointList[i], _pointList[i + 1], 1, Color.PaleVioletRed);
            }
        }
    }
}