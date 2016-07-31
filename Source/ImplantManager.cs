using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully
{
	public class ImplantManager
	{
		protected Dictionary<BodyPartDef, List<BodyPartRecord>> bodyPartListLookup = new Dictionary<BodyPartDef, List<BodyPartRecord>>();
		protected List<RecipeDef> recipes = new List<RecipeDef>();
		protected Dictionary<RecipeDef, List<BodyPartRecord>> recipeBodyParts = new Dictionary<RecipeDef, List<BodyPartRecord>>();
		protected HashSet<BodyPartRecord> replaceableParts = new HashSet<BodyPartRecord>();
		protected Dictionary<BodyPartRecord, HashSet<BodyPartRecord>> bodyPartAncestors = new Dictionary<BodyPartRecord, HashSet<BodyPartRecord>>();

		public List<RecipeDef> Recipes {
			get {
				return recipes;
			}
		}

		public ImplantManager()
		{
			bodyPartListLookup = new Dictionary<BodyPartDef, List<BodyPartRecord>>();

			// Go through all of the body part records in a human.  Each record is an individual body part, and you
			// will see more than one instance of each record.  For example, you'll see 12 "Rib" records, one for each
			// of the 12 rib bones.  For each body part type (i.e. "Rib"), we store a list of each body part record
			// in a dictionary, so that we can get a list of all records that match a given type/definition.
			BodyDef bodyDef = BodyDefOf.Human;
			foreach (BodyPartRecord record in bodyDef.AllParts) {
				List<BodyPartRecord> records;
				if (!bodyPartListLookup.TryGetValue(record.def, out records)) {
					records = new List<BodyPartRecord>();
					bodyPartListLookup[record.def] = records;
				}
				records.Add(record);
				AddAncestors(record);
			}

			// Find all recipes that replace a body part.
			recipes.AddRange(DefDatabase<RecipeDef>.AllDefs.Where((RecipeDef def) => {
				if (def.addsHediff != null && def.appliedOnFixedBodyParts != null && def.appliedOnFixedBodyParts.Count > 0
					&& (def.recipeUsers.NullOrEmpty() || def.recipeUsers.Contains(ThingDefOf.Human)) ) {
					return true;
				}
				else {
					return false;
				}
			}));

			// Iterate the recipes. Populate a lookup with all of the body parts that apply to a given recipe.
			foreach (var r in recipes) {

				// Get the list of recipe body parts from the dictionary or create it if it doesn't exist.
				List<BodyPartRecord> bodyPartRecords;
				if (!recipeBodyParts.TryGetValue(r, out bodyPartRecords)) {
					bodyPartRecords = new List<BodyPartRecord>();
					recipeBodyParts.Add(r, bodyPartRecords);
				}
				// Add all of the body part records for that recipe to the list.
				foreach (var bodyPartDef in r.appliedOnFixedBodyParts) {
					if (bodyPartListLookup.ContainsKey(bodyPartDef)) {
						List<BodyPartRecord> records = bodyPartListLookup[bodyPartDef];
						bodyPartRecords.AddRange(records);
						foreach (var record in records) {
							replaceableParts.Add(record);
						}
					}
				}
			}

			// Sort the recipes
			recipes.Sort((RecipeDef a, RecipeDef b) => {
				return a.LabelCap.CompareTo(b.LabelCap);
			});
		}

		public List<BodyPartRecord> PartsForRecipe(RecipeDef recipe)
		{
			return this.recipeBodyParts[recipe];
		}

		public IEnumerable<BodyPartRecord> PartAncestors(BodyPartRecord part)
		{
			return bodyPartAncestors[part];
		}

		public bool AncestorIsImplant(BodyPartRecord record, CustomPawn pawn)
		{
			foreach (BodyPartRecord ancestor in bodyPartAncestors[record]) {
				if (pawn.IsImplantedPart(ancestor)) {
					return true;
				}
			}
			return false;
		}

		protected void AddAncestors(BodyPartRecord record)
		{
			if (bodyPartAncestors.ContainsKey(record)) {
				return;
			}
			else {
				bodyPartAncestors.Add(record, new HashSet<BodyPartRecord>());
			}
			for (BodyPartRecord parentRecord = record.parent; parentRecord != null; parentRecord = parentRecord.parent) {
				bodyPartAncestors[record].Add(parentRecord);
			}
		}

		// TODO: This is problematic for body parts that appear multiple times in a body (i.e. ribs).
		// Calling this will return the first one that it finds.  There's no distinguishing between multiple
		// parts of the same type.
		public BodyPartRecord FindReplaceableBodyPartByName(string name)
		{
			foreach (var record in replaceableParts) {
				if (record.def.defName == name) {
					return record;
				}
			}
			return null;
		}

	}
}

