using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using KaiHelper.Properties;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = SharpDX.Color;

namespace KaiHelper.Tracker
{
    internal class LastPosition
    {
        private readonly List<ChampionTracker> _championsTracker = new List<ChampionTracker>();
        private readonly Obj_SpawnPoint _enemySpawn = ObjectManager.Get<Obj_SpawnPoint>().FirstOrDefault(x => x.IsEnemy);
        public static Menu Menu;

        public LastPosition(Menu timer)
        {
            Menu = timer.AddSubMenu(new Menu("Last Potion (F5)", "Last Position"));
            Menu.AddItem(new MenuItem("Scale", "Scale Image").SetValue(new Slider(20, 1, 50)));
            Menu.AddItem(new MenuItem("Opacity", "Opacity").SetValue(new Slider(70)));
            Menu.AddItem(new MenuItem("TextSize", "Text Size").SetValue(new Slider(15, 1)));
            Menu.AddItem(new MenuItem("ALP", "Active").SetValue(true));
            foreach (
                Obj_AI_Hero champion in
                    ObjectManager.Get<Obj_AI_Hero>().Where(champion => champion.Team != ObjectManager.Player.Team))
            {
                Console.WriteLine(champion.ChampionName);
                //_championsTracker.Add(new ChampionTracker(champion));
            }
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            Obj_AI_Base.OnTeleport += ObjAiBaseOnOnTeleport;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            foreach (
                Obj_AI_Hero champion in
                    ObjectManager.Get<Obj_AI_Hero>().Where(champion => champion.Team != ObjectManager.Player.Team))
            {
                //Console.WriteLine(champion.ChampionName);
                _championsTracker.Add(new ChampionTracker(champion));
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!Menu.Item("ALP").GetValue<bool>())
            {
                return;
            }
            foreach (ChampionTracker champion in _championsTracker)
            {
                if (champion.Champion.ServerPosition != champion.RecallPostion)
                {
                    champion.LastPotion = champion.Champion.ServerPosition;
                }
                if (champion.Champion.IsVisible)
                {
                    champion.StartInvisibleTime = Game.ClockTime;
                }
            }
        }

        private void ObjAiBaseOnOnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
        {
            try
            {
                //if (!Menu.Item("ALP").GetValue<bool>())
                //{
                //    return;
                //}
            
                var unit = sender as Obj_AI_Hero;
                if (unit == null || !unit.IsValid || unit.IsAlly)
                {
                    return;
                }
                Packet.S2C.Teleport.Struct recall = Packet.S2C.Teleport.Decoded(sender, args);
                if (recall.Type == Packet.S2C.Teleport.Type.Recall)
                {
                    ChampionTracker cham = _championsTracker.FirstOrDefault(
                        c => c.Champion.NetworkId == recall.UnitNetworkId);
                    if (cham != null)
                    {
                        cham.RecallPostion = cham.Champion.ServerPosition;
                        cham.Text.Color = Color.Red;
                        if (recall.Status == Packet.S2C.Teleport.Status.Finish)
                        {
                            cham.LastPotion = _enemySpawn.Position;
                            cham.Text.Color = Color.White;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        internal class ChampionTracker
        {
            public readonly Render.Text Text;

            public Vector3 LastPotion;
            public Vector3 RecallPostion;
            public float StartInvisibleTime;

            public ChampionTracker(Obj_AI_Hero champion)
            {
                Champion = champion;
                LastPotion = champion.ServerPosition;
                StartInvisibleTime = Game.ClockTime;
                var sprite =
                    new Render.Sprite(
                        Helper.ChangeOpacity(
                                ResourceImages.GetChampionSquare(champion.SkinName) ??
                                ResourceImages.GetChampionSquare("Aatrox"), Opacity),new Vector2(0,0));
                sprite.GrayScale();
                sprite.Scale = new Vector2(Scale, Scale);
                sprite.VisibleCondition = sender => TrackerCondition;
                sprite.PositionUpdate =
                    () => Drawing.WorldToMinimap(LastPotion) + new Vector2(-(sprite.Width / 2), -(sprite.Height / 2));
                sprite.Add(0);
                Text = new Render.Text(0, 0, "", Menu.Item("TextSize").GetValue<Slider>().Value, Color.White)
                {
                    VisibleCondition = sender => TrackerCondition,
                    PositionUpdate = () => Drawing.WorldToMinimap(LastPotion),
                    TextUpdate = () => Helper.FormatTime(Game.ClockTime - StartInvisibleTime),
                    OutLined = true,
                    Centered = true
                };
                Text.Add(0);
                AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
            }
            private void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
            {
                Text.Remove();
                Text.Dispose();
            }
            public Obj_AI_Hero Champion { get; private set; }

            private bool TrackerCondition => !Champion.IsVisible && !Champion.IsDead && Menu.Item("ALP").GetValue<bool>();

            public float Opacity => (float)Menu.Item("Opacity").GetValue<Slider>().Value / 100;

            private float Scale => (float)Menu.Item("Scale").GetValue<Slider>().Value / 100;
        }
    }
}