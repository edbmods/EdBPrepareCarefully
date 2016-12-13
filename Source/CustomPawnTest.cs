using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EdB.PrepareCarefully
{
	public class xxCustomPawn
	{
		protected Pawn pawn;

		protected Backstory childhood;
		protected Backstory adulthood;

		public xxCustomPawn(Pawn pawn)
		{
			CopyPawn(pawn);
		}

		public void CopyPawn(Pawn pawn)
		{
			this.pawn = pawn;
			this.childhood = pawn.story.adulthood;
			this.adulthood = pawn.story.childhood;
		}

		public Pawn CreatePawn() {
			return null;
		}

		public Backstory Adulthood {
			get {
				return this.adulthood;
			}
			set {
				this.adulthood = value;
			}
		}

		public Backstory Childhood {
			get {
				return this.childhood;
			}
			set {
				this.childhood = value;
			}
		}

		public int GetSkillLevel(SkillDef def) {
			int count = this.pawn.skills.skills.Count;
			for (int i = 0; i < count; i++) {
				if (this.pawn.skills.skills[i].def == def) {
					SkillRecord record = this.pawn.skills.skills[i];
					return record.Level;
				}
			}
			throw new IndexOutOfRangeException();
		}

		public void SetSkillLevel(SkillDef def, int level) {
			int count = this.pawn.skills.skills.Count;
			for (int i = 0; i < count; i++) {
				if (this.pawn.skills.skills[i].def == def) {
					SkillRecord record = this.pawn.skills.skills[i];
					record.Level = level;
					return;
				}
			}
			throw new IndexOutOfRangeException();
		}

	}
}

