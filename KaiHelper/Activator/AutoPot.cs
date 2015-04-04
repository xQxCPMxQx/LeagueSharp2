using System;
using LeagueSharp;
using LeagueSharp.Common;

namespace KaiHelper.Activator
{
    class AutoPot
    {
        class BarPot
        {
            public BarPot(float health, float mana = 0)
            {
                Health = health;
                Mana = mana;
                HealthPercent = Helper.GetPercent(Health, ObjectManager.Player.MaxHealth);
                ManaPercent = Helper.GetPercent(Mana, ObjectManager.Player.MaxMana);
            }

            private float Health { get; set; }
            private float Mana { get; set; }

            public float HealthPercent { get; private set; }
            public float ManaPercent { get; private set; }

            public static BarPot operator +(BarPot a, BarPot b)
            {
                return new BarPot(a.Health + b.Health, a.Mana + b.Mana);
            }
        }
        public AutoPot(Menu menu)
        {
            _menu = menu.AddSubMenu(new Menu("Potion Manager", "PotionManager"));
            _menu.AddItem(new MenuItem("HPTrigger", "HP Trigger Percent").SetValue(new Slider(30)));
            _menu.AddItem(new MenuItem("HealthPotion", "Health Potion").SetValue(true));
            _menu.AddItem(new MenuItem("MPTrigger", "MP Trigger Percent").SetValue(new Slider(30)));
            _menu.AddItem(new MenuItem("ManaPotion", "Mana Potion").SetValue(true));
            _healthPotion = new Potion(ItemId.Health_Potion);
            _manaPotion = new Potion(ItemId.Mana_Potion);
            _biscuitPotion = new Potion((ItemId)2010);
            _flaskPotion = new Potion((ItemId)2041);
            MenuItem autoarrangeMenu = _menu.AddItem(new MenuItem("AutoArrange", "Auto Arrange").SetValue(false));
            autoarrangeMenu.ValueChanged += (sender, e) =>
            {
                if (e.GetNewValue<bool>())
                {
                    ResetTrigger();
                }
            };
            Game.OnUpdate += Game_OnGameUpdate;
            Champion.OnLevelUp += OnLevelUp;
        }
        private void OnLevelUp(Obj_AI_Hero champion, Champion.OnLevelUpEventAgrs agrs)
        {
            if (_menu.Item("AutoArrange").GetValue<bool>())
            {
                ResetTrigger();
            }
        }

        private void ResetTrigger()
        {
            HpTrigger = 100 - _healthPotion.HitHealthPercent;
            if (ObjectManager.Player.MaxMana <= 0)
            {
                ManaCheck = false;
                ManaTrigger = 0;
            }
            else
            {
                ManaTrigger = 100 - _manaPotion.HitManaPercent;
            }
        }


        private void Game_OnGameUpdate(EventArgs args)
        {
            if (!ObjectManager.Player.IsDead && !ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                BarPot lastBar = new BarPot(ObjectManager.Player.PredictedHealth(_healthPotion.ProcessTime), ObjectManager.Player.PredictedMana(_manaPotion.ProcessTime));
                bool hasEnemy = Utility.CountEnemiesInRange(800) > 0;
                if (HealthCheck && ((lastBar.HealthPercent <= HpTrigger && hasEnemy || (lastBar.HealthPercent < 50))))
                {
                    if ((lastBar.ManaPercent <= ManaTrigger && hasEnemy || lastBar.ManaPercent < 50) && _flaskPotion.IsReady())
                    {
                        _flaskPotion.Cast();
                        return;
                    }
                    if (_healthPotion.IsReady())
                    {
                        _healthPotion.Cast();
                    }
                    else if (_biscuitPotion.IsReady())
                    {
                        _biscuitPotion.Cast();
                    }
                    else if (_flaskPotion.IsReady())
                    {
                        _flaskPotion.Cast();
                        return;
                    }
                }
                if (ManaCheck && (lastBar.ManaPercent <= ManaTrigger && hasEnemy || lastBar.ManaPercent < 50))
                {
                    if (_manaPotion.IsReady())
                    {
                        _manaPotion.Cast();
                    }
                    else if (_flaskPotion.IsReady())
                    {
                        _flaskPotion.Cast();
                    }
                }
            }
        }
        private readonly Menu _menu;
        private readonly Potion _healthPotion;
        private readonly Potion _manaPotion;
        private readonly Potion _biscuitPotion;
        private readonly Potion _flaskPotion;

        public int HpTrigger
        {
            get { return _menu.Item("HPTrigger").GetValue<Slider>().Value; }
            set { _menu.Item("HPTrigger").SetValue(new Slider(value)); }
        }
        public int ManaTrigger
        {
            get { return _menu.Item("MPTrigger").GetValue<Slider>().Value; }
            set { _menu.Item("MPTrigger").SetValue(new Slider(value)); }
        }
        public bool HealthCheck
        {
            get { return _menu.Item("HealthPotion").GetValue<bool>(); }
            set { _menu.Item("HealthPotion").SetValue(value); }
        }
        public bool ManaCheck
        {
            get { return _menu.Item("ManaPotion").GetValue<bool>(); }
            set { _menu.Item("ManaPotion").SetValue(value); }
        }
    }
}