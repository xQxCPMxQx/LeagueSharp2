namespace SFXUtility.Class
{
	using LeagueSharp;
	using LeagueSharp.Common;
	public static class SpellHumanizer
	{
		static SpellHumanizer()
		{
			Enabled = false;
			Spellbook.OnCastSpell += Spellbook_OnCastSpell;
		}
		public static bool Enabled { get; set; }
		public static bool Debug { get; set; }
		private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
		{
			if (!Enabled || sender == null || !sender.Owner.IsValid || !sender.Owner.IsMe)
			{
				return;
			}
			if (ObjectManager.Player.Spellbook.GetSpell(args.Slot).State == SpellState.Cooldown)
			{
				args.Process = false;
			}
		}
	}
}