using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Verse.Sound;
namespace EdB.PrepareCarefully {
    public class ImplantManager {
        protected Dictionary<ThingDef, BodyPartDictionary> bodyPartDictionaries = new Dictionary<ThingDef, BodyPartDictionary>();

        public ImplantManager() {

        }

        public BodyPartDictionary GetBodyPartDictionary(ThingDef pawnThingDef) {
            BodyPartDictionary dictionary;
            if (!bodyPartDictionaries.TryGetValue(pawnThingDef, out dictionary)) {
                dictionary = new BodyPartDictionary(pawnThingDef);
                bodyPartDictionaries.Add(pawnThingDef, dictionary);
            }
            return dictionary;
        }

        public List<RecipeDef> RecipesForPawn(CustomPawn pawn) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.ImplantRecipes;
        }

        public List<BodyPartRecord> PartsForRecipe(Pawn pawn, RecipeDef recipe) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.def);
            return dictionary.PartsForRecipe(recipe);
        }

        public bool AncestorIsImplant(CustomPawn pawn, BodyPartRecord record) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.AncestorIsImplant(record, pawn);
        }

        // TODO: This is problematic for body parts that appear multiple times in a body (i.e. ribs).
        // Calling this will return the first one that it finds.  There's no distinguishing between multiple
        // parts of the same type.
        public BodyPartRecord FindReplaceableBodyPartByName(Pawn pawn, string name) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.def);
            return dictionary.FindReplaceableBodyPartByName(name);
        }
        
    }
}

