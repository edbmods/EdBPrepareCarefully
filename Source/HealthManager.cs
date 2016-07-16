using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public class HealthManager
	{
		protected ImplantManager implantManager;
		protected InjuryManager injuryManager;
		protected List<ThingDef> implantParts;
		protected Dictionary<RecipeDef, ThingDef> implantsForRecipes = new Dictionary<RecipeDef, ThingDef>();
		protected List<RecipeDef> implantRecipes;
		protected Dictionary<RecipeDef, List<BodyPartDef>> partsForRecipe = new Dictionary<RecipeDef, List<BodyPartDef>>();
		protected List<BodyPartRecord> allBodyParts = new List<BodyPartRecord>();
		protected List<BodyPartRecord> allOutsideBodyParts = new List<BodyPartRecord>();
		protected List<BodyPartRecord> allSkinCoveredBodyParts = new List<BodyPartRecord>();

		public HealthManager()
		{
			implantManager = new ImplantManager();
			injuryManager = new InjuryManager();
			InitializeBodyParts();
		}

		public ImplantManager ImplantManager {
			get { return implantManager; }
		}

		public InjuryManager InjuryManager {
			get { return injuryManager; }
		}

		public IEnumerable<RecipeDef> ImplantRecipes {
			get { return implantManager.Recipes; }
		}

		public IEnumerable<BodyPartRecord> AllBodyParts {
			get { return allBodyParts; }
		}

		public IEnumerable<BodyPartRecord> AllOutsideBodyParts {
			get { return allOutsideBodyParts; }
		}

		public IEnumerable<BodyPartRecord> AllSkinCoveredBodyParts {
			get { return allSkinCoveredBodyParts; }
		}

		public BodyPartRecord FirstBodyPartRecord(string bodyPartDefName) {
			foreach (BodyPartRecord record in BodyDefOf.Human.AllParts) {
				if (record.def.defName == bodyPartDefName) {
					return record;
				}
			}
			return null;
		}

		public BodyPartRecord FirstBodyPartRecord(BodyPartDef def) {
			return FirstBodyPartRecord(def.defName);
		}

		protected void InitializeBodyParts()
		{
			BodyDef bodyDef = BodyDefOf.Human;
			foreach (BodyPartRecord record in bodyDef.AllParts) {
				allBodyParts.Add(record);
				if (record.depth == BodyPartDepth.Outside) {
					allOutsideBodyParts.Add(record);
				}
				FieldInfo skinCoveredField = typeof(BodyPartDef).GetField("skinCovered", BindingFlags.Instance | BindingFlags.NonPublic);
				Boolean value = (Boolean)skinCoveredField.GetValue(record.def);
				if (value == true) {
					allSkinCoveredBodyParts.Add(record);
				}
			}
		}

	}
}

