using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class SaveRecordPawnV3 : IExposable
	{
		public Gender gender;
		public string adulthood;
		public string childhood;
		public List<string> traitNames = new List<string>();
		public List<int> traitDegrees = new List<int>();
		public Color skinColor;
		public string hairDef;
		public Color hairColor;
		public string headGraphicPath;
		public string firstName;
		public string lastName;
		public string nickName;
		public int age;
		public int biologicalAge;
		public int chronologicalAge;
		public List<string> skillNames = new List<string>();
		public List<int> skillValues = new List<int>();
		public List<Passion> passions = new List<Passion>();
		public List<Passion> originalPassions = new List<Passion>();
		public List<string> apparel = new List<string>();
		public List<int> apparelLayers = new List<int>();
		public List<string> apparelStuff = new List<string>();
		public List<Color> apparelColors = new List<Color>();
		public bool randomInjuries = true;
		public bool randomRelations = false;
		public List<SaveRecordImplantV3> implants = new List<SaveRecordImplantV3>();
		public List<SaveRecordInjuryV3> injuries = new List<SaveRecordInjuryV3>();

		public SaveRecordPawnV3()
		{

		}

		public SaveRecordPawnV3(CustomPawn pawn)
		{
			this.gender = pawn.Gender;
			// TODO: Alpha 16.  Figure out if this is going to break backwards compatibility.
			//this.adulthood = pawn.Adulthood.uniqueSaveKey;
			//this.childhood = pawn.Childhood.uniqueSaveKey;
			if (pawn.Adulthood != null) {
				this.adulthood = pawn.Adulthood.identifier;
			}
			else {
				this.adulthood = pawn.LastSelectedAdulthood.identifier;
			}
			this.childhood = pawn.Childhood.identifier;
			this.skinColor = pawn.SkinColor;
			this.hairDef = pawn.HairDef.defName;
			this.hairColor = pawn.GetColor(PawnLayers.Hair);
			this.headGraphicPath = pawn.HeadGraphicPath;
			this.firstName = pawn.FirstName;
			this.nickName = pawn.NickName;
			this.lastName = pawn.LastName;
			this.age = 0;
			this.biologicalAge = pawn.BiologicalAge;
			this.chronologicalAge = pawn.ChronologicalAge;
			foreach (var trait in pawn.Traits) {
				if (trait != null) {
					this.traitNames.Add(trait.def.defName);
					this.traitDegrees.Add(trait.Degree);
				}
			}
			foreach (var skill in pawn.Pawn.skills.skills) {
				this.skillNames.Add(skill.def.defName);
				this.skillValues.Add(pawn.GetUnmodifiedSkillLevel(skill.def));
				this.passions.Add(pawn.currentPassions[skill.def]);
				this.originalPassions.Add(pawn.originalPassions[skill.def]);
			}
			for (int layer = 0; layer < PawnLayers.Count; layer++) {
				ThingDef thingDef = pawn.GetAcceptedApparel(layer);
				ThingDef stuffDef = pawn.GetSelectedStuff(layer);
				Color color = pawn.GetColor(layer);
				if (thingDef != null) {
					this.apparelLayers.Add(layer);
					this.apparel.Add(thingDef.defName);
					this.apparelStuff.Add(stuffDef != null ? stuffDef.defName : "");
					this.apparelColors.Add(color);
				}
			}
			this.randomInjuries = pawn.RandomInjuries;
			foreach (Implant implant in pawn.Implants) {
				this.implants.Add(new SaveRecordImplantV3(implant));
			}
			foreach (Injury injury in pawn.Injuries) {
				this.injuries.Add(new SaveRecordInjuryV3(injury));
			}
		}

		public void ExposeData()
		{
			Scribe_Values.LookValue<Gender>(ref this.gender, "gender", Gender.Male, false);
			Scribe_Values.LookValue<string>(ref this.childhood, "childhood", null, false);
			Scribe_Values.LookValue<string>(ref this.adulthood, "adulthood", null, false);
			Scribe_Collections.LookList<string>(ref this.traitNames, "traitNames", LookMode.Value, null);
			Scribe_Collections.LookList<int>(ref this.traitDegrees, "traitDegrees", LookMode.Value, null);
			Scribe_Values.LookValue<Color>(ref this.skinColor, "skinColor", Color.white, false);
			Scribe_Values.LookValue<string>(ref this.hairDef, "hairDef", null, false);
			Scribe_Values.LookValue<Color>(ref this.hairColor, "hairColor", Color.white, false);
			Scribe_Values.LookValue<string>(ref this.headGraphicPath, "headGraphicPath", null, false);
			Scribe_Values.LookValue<string>(ref this.firstName, "firstName", null, false);
			Scribe_Values.LookValue<string>(ref this.nickName, "nickName", null, false);
			Scribe_Values.LookValue<string>(ref this.lastName, "lastName", null, false);
			if (Scribe.mode == LoadSaveMode.LoadingVars) {
				Scribe_Values.LookValue<int>(ref this.age, "age", 0, false);
			}
			Scribe_Values.LookValue<int>(ref this.biologicalAge, "biologicalAge", 0, false);
			Scribe_Values.LookValue<int>(ref this.chronologicalAge, "chronologicalAge", 0, false);
			Scribe_Collections.LookList<string>(ref this.skillNames, "skillNames", LookMode.Value, null);
			Scribe_Collections.LookList<int>(ref this.skillValues, "skillValues", LookMode.Value, null);
			Scribe_Collections.LookList<Passion>(ref this.passions, "passions", LookMode.Value, null);
			Scribe_Collections.LookList<string>(ref this.apparel, "apparel", LookMode.Value, null);
			Scribe_Collections.LookList<int>(ref this.apparelLayers, "apparelLayers", LookMode.Value, null);
			Scribe_Collections.LookList<string>(ref this.apparelStuff, "apparelStuff", LookMode.Value, null);
			Scribe_Collections.LookList<Color>(ref this.apparelColors, "apparelColors", LookMode.Value, null);
			Scribe_Values.LookValue<bool>(ref this.randomInjuries, "randomInjuries", false, true);

			if (Scribe.mode == LoadSaveMode.Saving) {
				Scribe_Collections.LookList<SaveRecordImplantV3>(ref this.implants, "implants", LookMode.Deep, null);
			}
			else {
				if (Scribe.curParent["implants"] != null) {
					Scribe_Collections.LookList<SaveRecordImplantV3>(ref this.implants, "implants", LookMode.Deep, null);
				}
			}

			if (Scribe.mode == LoadSaveMode.Saving) {
				Scribe_Collections.LookList<SaveRecordInjuryV3>(ref this.injuries, "injuries", LookMode.Deep, null);
			}
			else {
				if (Scribe.curParent["implants"] != null) {
					Scribe_Collections.LookList<SaveRecordInjuryV3>(ref this.injuries, "injuries", LookMode.Deep, null);
				}
			}
		}

		public HairDef FindHairDef(string name)
		{
			return DefDatabase<HairDef>.GetNamedSilentFail(name);
		}

		public Backstory FindBackstory(string name)
		{
			return BackstoryDatabase.allBackstories.Values.ToList().Find((Backstory b) => {
				return b.identifier.Equals(name);
			});
		}

		public Trait FindTrait(string name, int degree)
		{
			foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs) {
				if (!def.defName.Equals(name)) {
					continue;
				}
				List<TraitDegreeData> degreeData = def.degreeDatas;
				int count = degreeData.Count;
				if (count > 0) {
					for (int i = 0; i < count; i++) {
						if (degree == degreeData[i].degree) {
							Trait trait = new Trait(def, degreeData[i].degree, true);
							return trait;
						}
					}
				}
				else {
					return new Trait(def, 0, true);
				}
			}
			return null;
		}

		public SkillDef FindSkillDef(Pawn pawn, string name)
		{
			foreach (var skill in pawn.skills.skills) {
				if (skill.def.defName.Equals(name)) {
					return skill.def;
				}
			}
			return null;
		}
	}
}

