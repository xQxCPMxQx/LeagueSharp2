#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 Replay.cs is part of SFXUtility.

 SFXUtility is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXUtility is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXUtility. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

#region

using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using LeagueSharp;
using LeagueSharp.Common;
using SFXUtility.Classes;
using SFXUtility.Library.Extensions.NET;
using SFXUtility.Library.Extensions.SharpDX;
using SFXUtility.Library.Logger;
using SFXUtility.Properties;
using SharpDX.Direct3D9;
using Timer = System.Timers.Timer;

#endregion

namespace SFXUtility.Features.Detectors
{
    internal class Replay : Child<Detectors>
    {
        private readonly Timer _timer = new Timer { Enabled = false };
        private bool _isRecording;
        private Texture _recordTexture;
        private Sprite _sprite;

        public Replay(Detectors parent) : base(parent)
        {
            OnLoad();
        }

        public override string Name
        {
            get { return "Replay op.gg"; }
        }

        protected override void OnEnable()
        {
            Drawing.OnEndScene += OnDrawingEndScene;

            if (!_isRecording)
            {
                OnTimerElapsed(null, null);

                if (_timer != null)
                {
                    _timer.Enabled = true;
                    _timer.Start();
                }
            }

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Drawing.OnEndScene -= OnDrawingEndScene;

            if (_timer != null && _timer.Enabled)
            {
                _timer.Enabled = false;
                _timer.Stop();
            }

            base.OnDisable();
        }

        private void OnDrawingEndScene(EventArgs args)
        {
            try
            {
                if (Drawing.Direct3DDevice == null || Drawing.Direct3DDevice.IsDisposed || !_isRecording ||
                    !Menu.Item(Name + "IsRecording").GetValue<bool>())
                {
                    return;
                }

                _sprite.Begin(SpriteFlags.AlphaBlend);

                _sprite.DrawCentered(_recordTexture, 20, 20);

                _sprite.End();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected sealed override void OnLoad()
        {
            try
            {
                Menu = new Menu(Name, Name);
                Menu.AddItem(new MenuItem(Name + "DoRecord", "Do Record").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "IsRecording", "Notify Record").SetValue(false));
                Menu.AddItem(new MenuItem(Name + "CheckInterval", "Check Interval").SetValue(new Slider(3, 1, 10)))
                    .ValueChanged += delegate(object o, OnValueChangeEventArgs args)
                    {
                        if (_timer != null)
                        {
                            _timer.Interval = args.GetNewValue<Slider>().Value * 60 * 1000;
                        }
                    };

                Menu.AddItem(new MenuItem(Name + "Enabled", "Enabled").SetValue(false));

                Parent.Menu.AddSubMenu(Menu);

                _sprite = MDrawing.GetSprite();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        protected override void OnInitialize()
        {
            try
            {
                _recordTexture = Resources.RC_On.ToTexture();

                _timer.Interval = Menu.Item(Name + "CheckInterval").GetValue<Slider>().Value * 60 * 1000;
                _timer.Elapsed += OnTimerElapsed;

                base.OnInitialize();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                new Thread(
                    async () =>
                    {
                        try
                        {
                            if (!_isRecording && Menu.Item(Name + "DoRecord").GetValue<bool>())
                            {
                                _isRecording = await Spectator.DoRecord();
                            }
                            if (!_isRecording && Menu.Item(Name + "IsRecording").GetValue<bool>())
                            {
                                _isRecording = await Spectator.IsRecoding();
                            }
                            if (_isRecording)
                            {
                                _timer.Enabled = false;
                                _timer.Stop();
                            }
                        }
                        catch (Exception ex)
                        {
                            Global.Logger.AddItem(new LogItem(ex));
                        }
                    }).Start();
            }
            catch (Exception ex)
            {
                Global.Logger.AddItem(new LogItem(ex));
            }
        }

        internal class Spectator
        {
            private static readonly string DoRecordUrl =
                "http://{region}op.gg/summoner/ajax/requestRecording.json/gameId={game_id}";

            private static readonly string IsRecordingUrl = "http://{region}op.gg/summoner/ajax/spectator/";
            private static readonly string UpdateUrl = "http://{region}op.gg/summoner/ajax/update.json/";
            private static readonly string MainUrl = "http://{region}op.gg/summoner/userName={name}";
            private static bool _updated;

            static Spectator()
            {
                Region = PlatformId.Last().IsNumeric()
                    ? PlatformId.Remove(PlatformId.Length - 1).ToLower()
                    : PlatformId.ToLower();
                Region = Region.Contains("kr", StringComparison.OrdinalIgnoreCase) ? string.Empty : Region + ".";
                DoRecordUrl = DoRecordUrl.Replace("{region}", Region).Replace("{game_id}", GameId.ToString());
                IsRecordingUrl = IsRecordingUrl.Replace("{region}", Region);
                UpdateUrl = UpdateUrl.Replace("{region}", Region);
                MainUrl = MainUrl.Replace("{region}", Region)
                    .Replace("{name}", HttpUtility.UrlEncode(ObjectManager.Player.Name));
            }

            public static string PlatformId
            {
                get { return Game.Region; }
            }

            private static string Region { get; set; }

            public static long GameId
            {
                get { return Game.Id; }
            }

            public static int SummonerId { get; private set; }

            public static void UpdateSummonerId()
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        var response = client.DownloadString(new Uri(MainUrl));
                        int id;
                        if (
                            int.TryParse(
                                response.Between(
                                    "SummonerRefresh.RefreshUser(this, ", ")", StringComparison.OrdinalIgnoreCase),
                                out id))
                        {
                            SummonerId = id;
                        }
                    }
                    catch (Exception ex)
                    {
                        Global.Logger.AddItem(new LogItem(ex));
                    }
                }
            }

            public static async Task<bool> DoRecord()
            {
                if (!_updated)
                {
                    _updated = await DoUpdate();
                    if (!_updated)
                    {
                        return false;
                    }
                }
                using (var client = new WebClient())
                {
                    try
                    {
                        var response = await client.DownloadStringTaskAsync(new Uri(DoRecordUrl));
                        return response.Trim().Contains("\"success\":true", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            public static async Task<bool> DoUpdate()
            {
                if (SummonerId == 0)
                {
                    UpdateSummonerId();
                    if (SummonerId == 0)
                    {
                        return false;
                    }
                }
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        await client.UploadStringTaskAsync(new Uri(IsRecordingUrl), "summonerId=" + SummonerId);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            public static async Task<bool> IsRecoding()
            {
                if (!_updated)
                {
                    _updated = await DoUpdate();
                    if (!_updated)
                    {
                        return false;
                    }
                }
                using (var client = new WebClient())
                {
                    try
                    {
                        client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                        var response =
                            await
                                client.UploadStringTaskAsync(
                                    new Uri(IsRecordingUrl),
                                    "userName=" + HttpUtility.UrlEncode(ObjectManager.Player.Name) + "&force=false");
                        return response.Contains("NowRecording", StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
        }
    }
}