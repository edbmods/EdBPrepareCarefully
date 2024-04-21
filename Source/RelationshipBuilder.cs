using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipBuilder {
        private List<CustomizedRelationship> relationships;
        private List<ParentChildGroup> parentChildGroups;
        FieldInfo directRelationsField = null;
        FieldInfo pawnsWithField = null;
        private List<int> compatibilityPool = new List<int>();

        public RelationshipBuilder(List<CustomizedRelationship> relationships, List<ParentChildGroup> parentChildGroups) {
            directRelationsField = typeof(Pawn_RelationsTracker).GetField("directRelations", BindingFlags.Instance | BindingFlags.NonPublic);
            pawnsWithField = typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);
            this.relationships = relationships;
            this.parentChildGroups = parentChildGroups;

            int compatibilityPoolSize = Mathf.Max(Mathf.Min(relationships.Count * 6, 12), 50);
            this.FillCompatibilityPool(compatibilityPoolSize);
        }

        public List<CustomizedPawn> Build() {
            // These include all the pawns that have relationships with them.
            HashSet<CustomizedPawn> relevantPawns = new HashSet<CustomizedPawn>();
            foreach (var rel in relationships) {
                relevantPawns.Add(rel.Source);
                relevantPawns.Add(rel.Target);
            }
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    relevantPawns.Add(parent);
                }
                foreach (var child in group.Children) {
                    relevantPawns.Add(child);
                }
            }
            // Create a real pawn for each temporary pawn that has a relationship
            foreach (var pawn in relevantPawns) {
                if (pawn.Pawn == null) {
                    pawn.Pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequestWrapper() {
                        Context = PawnGenerationContext.NonPlayer,
                        FixedGender = pawn.TemporaryPawn?.Gender,
                    }.Request);
                    pawn.Pawn.Kill(null, null);
                }
            }

            // Go through each starting pawn and evaluate that any existing relationships need to be remain.
            // If not, remove them.
            PawnRelationDef parentDef = PawnRelationDefOf.Parent;
            foreach (var pawn in relevantPawns) {
                if (pawn.Type == CustomizedPawnType.Hidden) {
                    continue;
                }
                int relationCount = pawn.Pawn.relations.DirectRelations.Count;
                if (relationCount == 0) {
                    continue;
                }
                Logger.Debug("Checking existing relations for " + pawn.Pawn.LabelCap + ", " + relationCount);
                for (int i = 0; i < relationCount; i++) {
                    var r = pawn.Pawn.relations.DirectRelations[i];
                    Logger.Debug(string.Format("  [{0}]: relationship {1} between {2} and {3}", i, r.def.defName, pawn.Pawn.LabelShortCap, r.otherPawn.LabelShortCap));
                }
                var validity = new List<bool>(Enumerable.Range(0, relationCount).Select(i => false));
                foreach (var group in parentChildGroups) {
                    foreach (var parent in group.Parents) {
                        foreach (var child in group.Children) {
                            int index = pawn.Pawn.relations.DirectRelations.FirstIndexOf(r => {
                                return r.def == parentDef && r.otherPawn == parent.Pawn && child.Pawn == pawn.Pawn;
                            });
                            if (index != -1) {
                                Logger.Debug(string.Format("  Found direct parent relation between child {0} and parent {1} at index: {2}", pawn.Pawn.LabelShortCap, parent.Pawn.LabelShortCap, index));
                                validity[index] = true;
                            }
                        }
                    }
                }
                foreach (var relationship in relationships) {
                    int index = pawn.Pawn.relations.DirectRelations.FirstIndexOf(r => {
                        return r.def == relationship.Def &&
                           ((pawn == relationship.Target && r.otherPawn == relationship.Source.Pawn)
                           || (pawn == relationship.Source && r.otherPawn == relationship.Target.Pawn));
                    });
                    if (index != -1) {
                        Logger.Debug(string.Format("  Found direct relation {0} between pawn {1} and target {2} at index: {3}", relationship.Def.defName, pawn.Pawn.LabelShortCap, relationship.Source.Pawn.LabelShortCap, index));
                        validity[index] = true;
                    }
                }
                Logger.Debug("  " + string.Join(", ", validity.Select(v => v ? "1" : "0")));
                List<DirectPawnRelation> relationsToRemove = new List<DirectPawnRelation>();
                for (int i=0; i<relationCount; i++) {
                    if (!validity[i]) {
                        var r = pawn.Pawn.relations.DirectRelations[i];
                        relationsToRemove.Add(r);
                        Logger.Debug(string.Format("  Didn't find [{0}]: relationship {1} between {2} and {3}", i, r.def.defName, pawn.Pawn.LabelShortCap, r.otherPawn.LabelShortCap));
                    }
                }
                foreach (var r in relationsToRemove) {
                    pawn.Pawn.relations.RemoveDirectRelation(r);
                }
            }

            // Add direct relationships.
            foreach (var rel in relationships) {
                AddRelationship(rel.Source, rel.Target, rel.Def);
            }

            // For any parent/child group that is missing parents, create them.
            foreach (var group in parentChildGroups) {
                if (group.Children.Count > 1) {
                    // Siblings need to have 2 parents, or they will be considered half-siblings.
                    if (group.Parents.Count == 0) {
                        CustomizedPawn parent1 = CreateParent(Gender.Female, group.Children);
                        CustomizedPawn parent2 = CreateParent(Gender.Male, group.Children);
                        group.Parents.Add(parent1);
                        group.Parents.Add(parent2);
                    }
                    else if (group.Parents.Count == 1) {
                        if (group.Parents[0].Pawn.gender == Gender.Male) {
                            CustomizedPawn parent = CreateParent(Gender.Female, group.Children);
                            group.Parents.Add(parent);
                        }
                        else {
                            CustomizedPawn parent = CreateParent(Gender.Male, group.Children);
                            group.Parents.Add(parent);
                        }
                    }
                }
            }

            // Validate that any hidden parents are a reasonable age.
            foreach (var group in parentChildGroups) {
                if (group.Children.Count == 0) {
                    continue;
                }
                float age = 0;
                CustomizedPawn oldestChild = null;
                foreach (var child in group.Children) {
                    if (child.Pawn.ageTracker.AgeBiologicalYears > age) {
                        age = child.Pawn.ageTracker.AgeBiologicalYears;
                        oldestChild = child;
                    }
                }
                if (oldestChild == null) {
                    continue;
                }
                foreach (var parent in group.Parents) {
                    if (!IsPawnVisible(parent)) {
                        int validAge = GetValidParentAge(parent, oldestChild);
                        if (validAge != parent.Pawn.ageTracker.AgeBiologicalYears) {
                            long diff = parent.Pawn.ageTracker.AgeChronologicalYears - parent.Pawn.ageTracker.AgeBiologicalYears;
                            parent.Pawn.ageTracker.AgeBiologicalTicks = validAge * UtilityAge.TicksPerYear;
                            parent.Pawn.ageTracker.AgeChronologicalTicks = (validAge + diff) * UtilityAge.TicksPerYear;
                        }
                    }
                }
            }

            // Add parent/child relationships.
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    foreach (var child in group.Children) {
                        AddRelationship(child, parent, parentDef);
                    }
                }
            }

            // Get all of the pawns to add to the world.
            // TODO: Revisit this
            //HashSet<CustomizedPawn> worldPawns = new HashSet<CustomizedPawn>();
            //foreach (var group in parentChildGroups) {
            //    foreach (var parent in group.Parents) {
            //        if (!IsPawnVisible(parent)) {
            //            CustomizedPawn newPawn = parent;
            //            if (!worldPawns.Contains(newPawn)) {
            //                worldPawns.Add(newPawn);
            //            }
            //        }
            //        foreach (var child in group.Children) {
            //            if (!IsPawnVisible(child)) {
            //                CustomizedPawn newPawn = child;
            //                if (!worldPawns.Contains(newPawn)) {
            //                    worldPawns.Add(newPawn);
            //                }
            //            }
            //        }
            //    }
            //}

            return new List<CustomizedPawn>();
        }

        public bool IsPawnVisible(CustomizedPawn pawn) {
            return pawn.Type != CustomizedPawnType.Hidden && pawn.Type != CustomizedPawnType.Temporary;
        }


        private int GetValidParentAge(CustomizedPawn parent, CustomizedPawn firstChild) {
            float ageOfOldestChild = firstChild.Pawn.ageTracker.AgeBiologicalYears;
            float lifeExpectancy = firstChild.Pawn.def.race.lifeExpectancy;
            float minAge = lifeExpectancy * 0.1625f;
            float maxAge = lifeExpectancy * 0.625f;
            float meanAge = lifeExpectancy * 0.325f;

            float ageDifference = parent.Pawn.ageTracker.AgeBiologicalYears - ageOfOldestChild;
            if (ageDifference < minAge) {
                float leftDistance = meanAge - minAge;
                float rightDistance = maxAge - meanAge;
                int age = (int)(Verse.Rand.GaussianAsymmetric(meanAge, leftDistance, rightDistance) + ageOfOldestChild);
                return age;
            }
            else {
                return parent.Pawn.ageTracker.AgeBiologicalYears;
            }
        }
        
        private CustomizedPawn CreateParent(Gender? gender, List<CustomizedPawn> children) {
            int ageOfOldestChild = 0;
            CustomizedPawn firstChild = null;
            foreach (var child in children) {
                if (child.Pawn.ageTracker.AgeBiologicalYears > ageOfOldestChild) {
                    ageOfOldestChild = child.Pawn.ageTracker.AgeBiologicalYears;
                    firstChild = child;
                }
            }
            float lifeExpectancy = firstChild.Pawn.def.race.lifeExpectancy;
            float minAge = lifeExpectancy * 0.1625f;
            float maxAge = lifeExpectancy * 0.625f;
            float meanAge = lifeExpectancy * 0.325f;
            float leftDistance = meanAge - minAge;
            float rightDistance = maxAge - meanAge;
            int age = (int)(Verse.Rand.GaussianAsymmetric(meanAge, leftDistance, rightDistance) + ageOfOldestChild);

            CustomizedPawn parent = new CustomizedPawn() {
                Pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequestWrapper() {
                    KindDef = firstChild.Pawn.kindDef,
                    FixedBiologicalAge = age,
                    FixedGender = gender
                }.Request),
                Type = CustomizedPawnType.Temporary
            };
            parent.Pawn.Kill(null, null);
            return parent;
        }

        // TODO
        //public static void DebugWorldPawnRelationships() {
        //    String output = "Starting and Optional pawns:\n";
        //    Find.GameInitData.startingAndOptionalPawns.ForEach(p => {
        //        output += DebugOutputForPawn(p);
        //    });
        //    output += "World pawns:\n";
        //    Find.WorldPawns.AllPawnsAliveOrDead.ForEach(p => {
        //        output += DebugOutputForPawn(p);
        //    });
        //    output += "Customized Pawns:\n";
        //    PrepareCarefully.Instance.Pawns.ForEach(p => {
        //        output += DebugOutputForCustomizedPawn(p);
        //    });
        //    //Logger.Debug(output);
        //}

        //private static string DebugOutputForPawn(Pawn p) {
        //    string output = "";
        //    output += String.Format("- {0}[{1}]\n", p.LabelShort, p.GetUniqueLoadID());
        //    if (p.relations.DirectRelations.Count > 0) {
        //        output += "  - DirectRelations:\n";
        //        p.relations.DirectRelations.ForEach(r => {
        //            output += String.Format("    - {0}: {1}[{2}]\n", r.def.defName, r.otherPawn.LabelShort, r.otherPawn.GetUniqueLoadID());
        //        });
        //    }
        //    if (p.relations.ChildrenCount > 0) {
        //        output += "  - Children:\n";
        //        foreach (var c in p.relations.Children) {
        //            output += String.Format("    - {0}[{1}]\n", c.LabelShort, c.GetUniqueLoadID());
        //        }
        //    }
        //    return output;
        //}

        //private static string DebugOutputForCustomizedPawn(CustomizedPawn p) {
        //    string output = "";
        //    output += String.Format("- [{2}] {0}[{1}]\n", p.LabelShort, p.Pawn.GetUniqueLoadID(), p.Type);
        //    if (p.Pawn.relations.DirectRelations.Count > 0) {
        //        output += "  - DirectRelations:\n";
        //        p.Pawn.relations.DirectRelations.ForEach(r => {
        //            output += String.Format("    - {0}: {1}[{2}]\n", r.def.defName, r.otherPawn.LabelShort, r.otherPawn.GetUniqueLoadID());
        //        });
        //    }
        //    if (p.Pawn.relations.ChildrenCount > 0) {
        //        output += "  - Children:\n";
        //        foreach (var c in p.Pawn.relations.Children) {
        //            output += String.Format("    - {0}[{1}]\n", c.LabelShort, c.GetUniqueLoadID());
        //        }
        //    }
        //    return output;
        //}

        private void AddRelationship(CustomizedPawn customSource, CustomizedPawn customTarget, PawnRelationDef def) {
            Pawn source = customSource.Pawn;
            Pawn target = customTarget.Pawn;
            if (source == null || target == null) {
                return;
            }

            if (source.relations.DirectRelationExists(def, target)) {
                Logger.Debug(string.Format("Skipped adding direct relation {0} between {1} and {2}. Already there", def.defName, source.LabelShortCap, target.LabelShortCap));
                return;
            }
            source.relations.AddDirectRelation(def, target);
            Logger.Debug(string.Format("Added direct relation {0} between {1} and {2}", def.defName, source.LabelShortCap, target.LabelShortCap));

            // Try to find a better pawn compatibility, if it makes sense for the relationship.
            //CarefullyPawnRelationDef pcDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
            //if (pcDef != null) {
            //    if (pcDef.needsCompatibility) {
            //        int originalId = target.thingIDNumber;
            //        float originalScore = source.relations.ConstantPerPawnsPairCompatibilityOffset(originalId);
            //        int bestId = originalId;
            //        float bestScore = originalScore;
            //        foreach (var id in compatibilityPool) {
            //            float score = source.relations.ConstantPerPawnsPairCompatibilityOffset(id);
            //            if (score > bestScore) {
            //                bestScore = score;
            //                bestId = id;
            //            }
            //        }
            //        if (bestId != originalId) {
            //            target.thingIDNumber = bestId;
            //            compatibilityPool.Remove(bestId);
            //            compatibilityPool.Add(originalId);
            //            Logger.Debug(String.Format("Improved compatibility between {0} and {1} for relationship {2}", source?.LabelCap, target?.LabelCap, def?.defName));
            //        }
            //    }
            //}
        }

        private void FillCompatibilityPool(int size) {
            int needed = size - compatibilityPool.Count;
            for (int i=0; i<needed; i++) {
                compatibilityPool.Add(Find.UniqueIDsManager.GetNextThingID());
            }
        }
        
    }
}
