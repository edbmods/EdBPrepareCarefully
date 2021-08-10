using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipDefinitionHelper {
        protected Randomizer randomizer = new Randomizer();

        public List<PawnRelationDef> AllowedRelationships { get; set; } = new List<PawnRelationDef>();
        public Dictionary<PawnRelationDef, PawnRelationDef> InverseRelationships { get; set; } = new Dictionary<PawnRelationDef, PawnRelationDef>();

        public RelationshipDefinitionHelper() {
            AllowedRelationships = InitializeAllowedRelationships();
            InverseRelationships = InitializeInverseRelationships();
        }

        public PawnRelationDef FindInverseRelationship(PawnRelationDef def) {
            PawnRelationDef inverse;
            if (InverseRelationships.TryGetValue(def, out inverse)) {
                return inverse;
            }
            else {
                return null;
            }
        }

        protected List<PawnRelationDef> InitializeAllowedRelationships() {
            return DefDatabase<PawnRelationDef>.AllDefs.Where((PawnRelationDef def) => {
                if (def.familyByBloodRelation) {
                    return false;
                }
                CarefullyPawnRelationDef extended = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
                if (extended != null && extended.animal) {
                    return false;
                }
                MethodInfo info = def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (info == null) {
                    return false;
                }
                else {
                    return true;
                }
            }).ToList();
        }

        protected Dictionary<PawnRelationDef, PawnRelationDef> InitializeInverseRelationships() {
            Dictionary<PawnRelationDef, PawnRelationDef> result = new Dictionary<PawnRelationDef, PawnRelationDef>();
            foreach (var def in DefDatabase<PawnRelationDef>.AllDefs) {
                CarefullyPawnRelationDef extended = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
                PawnRelationDef inverse;
                if (extended != null && extended.inverse != null) {
                    inverse = DefDatabase<PawnRelationDef>.GetNamedSilentFail(extended.inverse);
                }
                else {
                    inverse = TryToComputeInverseRelationship(def);
                }
                if (inverse != null) {
                    result[def] = inverse;
                }
            }
            return result;
        }

        // We try to determine the inverse of a relationship by adding the relationship between two pawns.  If we're able to add
        // the relationship, the target pawn should have the inverse relationship.
        public PawnRelationDef TryToComputeInverseRelationship(PawnRelationDef def) {
            Pawn source = randomizer.GenerateColonist();
            Pawn target = randomizer.GenerateColonist();
            MethodInfo info = def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (info == null) {
                return null;
            }
            var worker = FindPawnRelationWorker(def);
            PawnGenerationRequest req = new PawnGenerationRequest();
            worker.CreateRelation(source, target, ref req);
            foreach (PawnRelationDef d in PawnRelationUtility.GetRelations(target, source)) {
                return d;
            }
            return null;
        }

        public PawnRelationWorker FindPawnRelationWorker(PawnRelationDef def) {
            CarefullyPawnRelationDef carefullyDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
            if (carefullyDef == null || carefullyDef.workerClass == null) {
                return def.Worker;
            }
            else {
                PawnRelationWorker worker = carefullyDef.Worker;
                if (worker != null) {
                    //Logger.Debug("Returned carefully worker for " + def.defName + ", " + worker.GetType().FullName);
                    return carefullyDef.Worker;
                }
                else {
                    return def.Worker;
                }
            }
        }
    }
}

