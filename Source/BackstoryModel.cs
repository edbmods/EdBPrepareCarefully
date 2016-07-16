using RimWorld;
using System;
using Verse;

namespace EdB.PrepareCarefully
{
	public class BackstoryModel
	{
		protected Backstory backstory;
		protected bool nameSpecific;
		protected bool genderSpecific;
		protected PawnBio bio;

		public BackstoryModel()
		{
		}

		public BackstoryModel(PawnBio bio, BackstorySlot slot)
		{
			this.bio = bio;
			this.backstory = slot == BackstorySlot.Childhood ? bio.childhood : bio.adulthood;

		}

		public Backstory Backstory {
			get {
				return backstory;
			}
		}

		public bool IsNameSpecific {
			get {
				return nameSpecific;
			}
		}

		public bool IsGenderSpecific {
			get {
				return bio.gender != GenderPossibility.Either;
			}
		}

		public Gender Gender {
			get {
				return bio.gender == GenderPossibility.Male ? Gender.Male : (bio.gender == GenderPossibility.Female ? Gender.Female : Gender.None);
			}
		}
	}
}

