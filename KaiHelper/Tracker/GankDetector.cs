using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace KaiHelper.Tracker
{
    public class Time
    {
        public bool CalledInvisible;
        public bool CalledVisible;
        public int InvisibleTime;
        public bool Pinged;
        public int StartInvisibleTime;
        public int StartVisibleTime;
        public int VisibleTime;
    }

    public class GankDetector
    {
        private readonly Dictionary<Obj_AI_Hero, Time> _enemies = new Dictionary<Obj_AI_Hero, Time>();
        private readonly Menu _menuGank;

        public GankDetector(Menu config)
        {
            _menuGank = config.AddSubMenu(new Menu("Gank Alerter", "GDetect"));
            _menuGank.AddItem(new MenuItem("InvisibleTime", "Invisisble Time").SetValue(new Slider(5, 1, 10)));
            _menuGank.AddItem(new MenuItem("VisibleTime", "Visible Time").SetValue(new Slider(3, 1, 5)));
            _menuGank.AddItem(new MenuItem("TriggerRange", "Trigger Range").SetValue(new Slider(3000, 1, 3000)));
            _menuGank.AddItem(new MenuItem("CircalRange", "Circal Range").SetValue(new Slider(2500, 1, 3000)));
            _menuGank.AddItem(new MenuItem("GankType", "Type").SetValue(new StringList(new []{"Line","Circle","Both"})));
            _menuGank.AddItem(new MenuItem("Ping", "Chat Alerter").SetValue(true));
            _menuGank.AddItem(new MenuItem("GankActive", "Active").SetValue(true));
            CustomEvents.Game.OnGameLoad += (args =>
            {
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsEnemy))
                {
                    _enemies.Add(hero, new Time());
                }
                Game.OnUpdate += Game_OnGameUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
            });
            
        }
        private void Drawing_OnDraw(EventArgs args)
        {
            if (!IsActive)
            {
                return;
            }
            foreach (Obj_AI_Hero hero in
                _enemies.Select(enemy => enemy.Key)
                    .Where(
                        hero =>
                            !hero.IsDead && hero.IsVisible && _enemies[hero].InvisibleTime >= InvisibleTime &&
                            _enemies[hero].VisibleTime <= VisibleTime &&
                            hero.Distance(ObjectManager.Player.Position) <= TriggerGank))
            {
                switch (GankType)
                {
                    case 0:
                        Drawing.DrawLine(Drawing.WorldToScreen(ObjectManager.Player.Position), Drawing.WorldToScreen(hero.Position), 5, Color.Crimson);
                        break;
                    case 1: 
                        Render.Circle.DrawCircle(hero.Position, CircalGank, Color.Red, 20);
                        Render.Circle.DrawCircle(hero.Position, CircalGank, Color.FromArgb(15, Color.Red), -142857);
                        break;
                    default:
                        Render.Circle.DrawCircle(hero.Position, CircalGank, Color.Red, 20);
                        Render.Circle.DrawCircle(hero.Position, CircalGank, Color.FromArgb(15, Color.Red), -142857);
                        Drawing.DrawLine(Drawing.WorldToScreen(ObjectManager.Player.Position), Drawing.WorldToScreen(hero.Position), 5, Color.Crimson);
                        break;
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!IsActive)
            {
                return;
            }
            foreach (var enemy in _enemies)
            {
                UpdateTime(enemy);
                Obj_AI_Hero hero = enemy.Key;
                if (hero.IsDead || !hero.IsVisible || _enemies[hero].InvisibleTime < InvisibleTime ||
                    _enemies[hero].VisibleTime > VisibleTime ||
                    !(hero.Distance(ObjectManager.Player.Position) <= TriggerGank))
                {
                    continue;
                }
                //var t = MenuGank.Item("Ping").GetValue<StringList>();
                if (!ChatAlert)
                {
                    continue;
                }
                if (!_enemies[hero].Pinged)
                {
                    _enemies[hero].Pinged = true;
                    Game.PrintChat("<font color = \"#FF0000\">Gank: </font>" + hero.ChampionName);
                    //switch (t.SelectedIndex)
                    //{
                    //    case 0:
                    //        Packet.S2C.Ping.Encoded(new Packet.S2C.Ping.Struct(hero.Position.X, hero.Position.Y,
                    //            0, 0, Packet.PingType.Danger)).Process();
                    //        break;
                    //    case 1:
                    //        Packet.C2S.Ping.Encoded(
                    //            new Packet.C2S.Ping.Struct(hero.Position.X + new Random(10).Next(-200, 200),
                    //                hero.Position.Y + new Random(10).Next(-200, 200), 0, Packet.PingType.Danger))
                    //            .Send();
                    //        break;
                    //}
                    Utility.DelayAction.Add((VisibleTime + 1) * 1000, () => { _enemies[hero].Pinged = false; });
                }
            }
        }

        private void UpdateTime(KeyValuePair<Obj_AI_Hero, Time> enemy)
        {
            Obj_AI_Hero hero = enemy.Key;
            if (!hero.IsValid)
            {
                return;
            }
            if (hero.IsVisible)
            {
                if (!_enemies[hero].CalledVisible)
                {
                    _enemies[hero].CalledVisible = true;
                    _enemies[hero].StartVisibleTime = Environment.TickCount;
                }
                _enemies[hero].CalledInvisible = false;
                _enemies[hero].VisibleTime = (Environment.TickCount - _enemies[hero].StartVisibleTime) / 1000;
            }
            else
            {
                if (!_enemies[hero].CalledInvisible)
                {
                    _enemies[hero].CalledInvisible = true;
                    _enemies[hero].StartInvisibleTime = Environment.TickCount;
                }
                _enemies[hero].CalledVisible = false;
                _enemies[hero].InvisibleTime = (Environment.TickCount - _enemies[hero].StartInvisibleTime) / 1000;
            }
        }
        public int TriggerGank { get { return _menuGank.Item("TriggerRange").GetValue<Slider>().Value; } }
        public int CircalGank { get { return _menuGank.Item("CircalRange").GetValue<Slider>().Value; } }
        public int InvisibleTime { get { return _menuGank.Item("InvisibleTime").GetValue<Slider>().Value; } }
        public int VisibleTime { get { return _menuGank.Item("VisibleTime").GetValue<Slider>().Value; } }
        public bool IsActive { get { return _menuGank.Item("GankActive").GetValue<bool>(); } }
        public int GankType { get { return _menuGank.Item("GankType").GetValue<StringList>().SelectedIndex; } }
        public bool ChatAlert { get { return _menuGank.Item("Ping").GetValue<bool>(); } }
    }
}