using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class HealthManager {
        protected ImplantManager implantManager;
        protected InjuryManager injuryManager;
        protected Dictionary<ThingDef, BodyPartDictionary> bodyPartDictionaries = new Dictionary<ThingDef, BodyPartDictionary>();

        public HealthManager() {
            implantManager = new ImplantManager();
            injuryManager = new InjuryManager();
        }

        public ImplantManager ImplantManager {
            get { return implantManager; }
        }

        public InjuryManager InjuryManager {
            get { return injuryManager; }
        }

        public BodyPartDictionary GetBodyPartDictionary(ThingDef pawnThingDef) {
            BodyPartDictionary dictionary;
            if (!bodyPartDictionaries.TryGetValue(pawnThingDef, out dictionary)) {
                dictionary = new BodyPartDictionary(pawnThingDef);
                bodyPartDictionaries.Add(pawnThingDef, dictionary);
            }
            return dictionary;
        }

        public IEnumerable<BodyPartRecord> AllBodyParts(CustomPawn pawn) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.AllBodyParts;
        }

        public IEnumerable<BodyPartRecord> AllOutsideBodyParts(CustomPawn pawn) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.AllOutsideBodyParts;
        }

        public IEnumerable<BodyPartRecord> AllSkinCoveredBodyParts(CustomPawn pawn) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.AllSkinCoveredBodyParts;
        }

        public BodyPartRecord FirstBodyPartRecord(CustomPawn pawn, string bodyPartDefName) {
            BodyPartDictionary dictionary = GetBodyPartDictionary(pawn.Pawn.def);
            return dictionary.FirstBodyPartRecord(bodyPartDefName);
        }

        public BodyPartRecord FirstBodyPartRecord(CustomPawn pawn, BodyPartDef def) {
            return FirstBodyPartRecord(pawn, def.defName);
        }

    }
}

