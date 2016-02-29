using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using KaiHelper.Properties;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SharpDX.Direct3D9;
using Font = SharpDX.Direct3D9.Font;
using Rectangle = SharpDX.Rectangle;

namespace KaiHelper.Tracker
{
    public class SkillBar
    {
        private readonly Dictionary<string, Texture> summonerSpellTextures = new Dictionary<string, Texture>(StringComparer.InvariantCultureIgnoreCase);

        public Texture ButtonRedTexture;
        public Texture FrameLevelTexture;
        public Texture HudTexture;
        public Menu MenuSkillBar;
        public Font SmallText;
        public SpellSlot[] SpellSlots = {SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R};
        public Sprite Sprite;
        public SpellSlot[] SummonerSpellSlots = {SpellSlot.Summoner1, SpellSlot.Summoner2};

        public SkillBar(Menu config)
        {
            MenuSkillBar = config.AddSubMenu(new Menu("Cooldown Tracker", "SkillBar"));
            MenuSkillBar.AddItem(new MenuItem("OnAllies", "On Allies").SetValue(false));
            MenuSkillBar.AddItem(new MenuItem("OnEnemies", "On Enemies").SetValue(true));
            Sprite = new Sprite(Drawing.Direct3DDevice);
            HudTexture = Texture.FromMemory(Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.main, typeof (byte[])), 127, 41, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
            FrameLevelTexture = Texture.FromMemory(Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.spell_level, typeof (byte[])), 2, 3, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
            ButtonRedTexture = Texture.FromMemory(Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(Resources.disable, typeof (byte[])), 14, 14, 0, Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
            SmallText = new Font(Drawing.Direct3DDevice, new FontDescription
            {
                    FaceName = "Calibri",
                    Height = 13,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.Default,
                });

            AppDomain.CurrentDomain.DomainUnload += DomainUnload;
            AppDomain.CurrentDomain.ProcessExit += DomainUnload;
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private void DomainUnload(object sender, EventArgs eventArgs)
        {
            SmallText.Dispose();
            Sprite.Dispose();
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            foreach (Obj_AI_Hero champion in ObjectManager.Get<Obj_AI_Hero>())
            {
                foreach (SpellDataInst spellDataInst in champion.Spellbook.Spells)
                {
                    if (SummonerSpellSlots.Contains(spellDataInst.Slot))
                    {
                        if (!summonerSpellTextures.ContainsKey(spellDataInst.Name))
                        {
                            if (spellDataInst.Name == "summonersmite")
                            {
                                summonerSpellTextures.Add(spellDataInst.Name, GetTexture(spellDataInst.Name));
                                summonerSpellTextures.Add("itemsmiteaoe", GetTexture(spellDataInst.Name));
                                summonerSpellTextures.Add("s5_summonersmiteduel", GetTexture(spellDataInst.Name));
                                summonerSpellTextures.Add("s5_summonersmiteplayerganker", GetTexture(spellDataInst.Name));
                                summonerSpellTextures.Add("s5_summonersmitequick", GetTexture(spellDataInst.Name));
                            }
                            else
                            {
                                Game.PrintChat(spellDataInst.Name.ToString());
                                summonerSpellTextures.Add(spellDataInst.Name, GetTexture(spellDataInst.Name));
                            }
                        }
                    }
                }
                foreach (SpellSlot spellSlot in SpellSlots)
                {
                    if (!summonerSpellTextures.ContainsKey(champion.ChampionName + "_" + spellSlot))
                    {
                        summonerSpellTextures.Add(
                            champion.ChampionName + "_" + spellSlot,
                            GetTexture(champion.ChampionName + "_" + spellSlot, false));
                    }
                }
            }
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private Texture GetTexture(string spellName, bool summoner = true)
        {
            var bitmap = (Bitmap) (Resources.ResourceManager.GetObject(spellName) ?? Resources.Unknown);
            if (!summoner)
            {
                return Texture.FromMemory(
                    Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(bitmap, typeof (byte[])), 14, 14, 0,
                    Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
            }
            return Texture.FromMemory(
                Drawing.Direct3DDevice, (byte[]) new ImageConverter().ConvertTo(bitmap, typeof (byte[])), 12, 240, 0,
                Usage.None, Format.A1, Pool.Managed, Filter.Default, Filter.Default, 0);
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            try
            {
                foreach (Obj_AI_Hero hero in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            hero =>
                                hero.IsValid && !hero.IsDead && !hero.IsMe && hero.IsHPBarRendered &&
                                (hero.IsEnemy && MenuSkillBar.Item("OnEnemies").GetValue<bool>() ||
                                 hero.IsAlly && MenuSkillBar.Item("OnAllies").GetValue<bool>())))
                {
                    Vector2 skillStateBarPos;
                    if (hero.IsEnemy)
                    {
                        skillStateBarPos = hero.HPBarPosition + new Vector2(-10, 17);
                    }
                    else
                    {
                        skillStateBarPos = hero.HPBarPosition + new Vector2(-10, 14);
                    }
                    var x = (int) skillStateBarPos.X;
                    var y = (int) skillStateBarPos.Y;
                    Sprite.Begin();
                    Sprite.Draw(HudTexture, new ColorBGRA(255, 255, 255, 255), null, new Vector3(-x, -y, 0));
                    for (int index = 0; index < SummonerSpellSlots.Length; index++)
                    {
                        SpellDataInst summonerSpell = hero.Spellbook.GetSpell(SummonerSpellSlots[index]);
                        float t = summonerSpell.CooldownExpires - Game.Time;
                        float percent = (Math.Abs(summonerSpell.Cooldown) > float.Epsilon)
                            ? t/summonerSpell.Cooldown
                            : 1f;
                        int n = (t > 0) ? (int) (19*(1f - percent)) : 19;
                        string s = string.Format(t < 1f ? "{0:0.0}" : "{0:0}", t);
                        if (t > 0)
                        {
                            Helper.DrawText(SmallText, s, x - 10, y + 2 + 19*index, new ColorBGRA(255, 255, 255, 255));
                        }
                        Sprite.Draw(
                            summonerSpellTextures[summonerSpell.Name], new ColorBGRA(255, 255, 255, 255),
                            new Rectangle(0, 12*n, 12, 12), new Vector3(-x - 3, -y - 3 - 18*index, 0));
                    }
                    for (int index = 0; index < SpellSlots.Length; index++)
                    {
                        SpellSlot spellSlot = SpellSlots[index];
                        SpellDataInst spell = hero.Spellbook.GetSpell(spellSlot);
                        for (int i = 1; i <= 5; i++)
                        {
                            if (spell.Level == i)
                            {
                                for (int j = 1; j <= i; j++)
                                {
                                    Sprite.Draw(
                                        FrameLevelTexture, new ColorBGRA(255, 255, 255, 255), new Rectangle(0, 0, 2, 3),
                                        new Vector3(-x - 18 - index*17 - j*3, -y - 36, 0));
                                }
                            }
                        }
                        Sprite.Draw(
                            summonerSpellTextures[hero.ChampionName + "_" + spellSlot],
                            new ColorBGRA(255, 255, 255, 255), new Rectangle(0, 0, 14, 14),
                            new Vector3(-x - 21 - index*17, -y - 20, 0));
                        if (spell.State == SpellState.Cooldown || spell.State == SpellState.NotLearned)
                        {
                            Sprite.Draw(
                                ButtonRedTexture, new ColorBGRA(0, 0, 0, 180), new Rectangle(0, 0, 14, 14),
                                new Vector3(-x - 21 - index*17, -y - 20, 0));
                        }
                    }
                    Sprite.End();
                    for (int index = 0; index < SpellSlots.Length; index++)
                    {
                        SpellSlot spellSlot = SpellSlots[index];
                        SpellDataInst spell = hero.Spellbook.GetSpell(spellSlot);
                        float t = spell.CooldownExpires - Game.Time;
                        if (!(t > 0) || !(t < 100))
                        {
                            continue;
                        }
                        string s = string.Format(t < 1f ? "{0:0.0}" : "{0:0}", t);
                        Helper.DrawText(
                            SmallText, s, x + 16 + index*17 + 12, y + 21, new ColorBGRA(255, 255, 255, 255));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Sprite.End();
            }
        }
    }
}