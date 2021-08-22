using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace EdB.PrepareCarefully {
    public class RelationshipBuilder {
        private List<CustomRelationship> relationships;
        private List<ParentChildGroup> parentChildGroups;
        FieldInfo directRelationsField;
        FieldInfo pawnsWithField;
        private List<int> compatibilityPool = new List<int>();

        public RelationshipBuilder(List<CustomRelationship> relationships, List<ParentChildGroup> parentChildGroups) {
            directRelationsField = typeof(Pawn_RelationsTracker).GetField("directRelations", BindingFlags.Instance | BindingFlags.NonPublic);
            pawnsWithField = typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);
            this.relationships = relationships;
            this.parentChildGroups = parentChildGroups;

            int compatibilityPoolSize = Mathf.Max(Mathf.Min(relationships.Count * 6, 12), 50);
            this.FillCompatibilityPool(compatibilityPoolSize);
        }

        public List<CustomPawn> Build() {
            // These include all the pawns that have relationships with them.
            HashSet<CustomPawn> relevantPawns = new HashSet<CustomPawn>();
            foreach (var rel in relationships) {
                relevantPawns.Add(rel.source);
                relevantPawns.Add(rel.target);
            }
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    relevantPawns.Add(parent);
                }
                foreach (var child in group.Children) {
                    relevantPawns.Add(child);
                }
            }
            
            // Remove any existing relationships from pawns for whom we've defined custom relationships.
            foreach (var pawn in relevantPawns) {
                pawn.Pawn.relations.ClearAllRelations();
            }

            // Remove all relationships between world pawns and the original starting and optional pawns.  We'll be recreating
            // those relationships between the world pawns and our custom pawns.
            HashSet<Pawn> allWorldPawns = new HashSet<Pawn>(Find.WorldPawns.AllPawnsAliveOrDead);
            HashSet<Pawn> startingAndOptionalPawns = new HashSet<Pawn>(Find.GameInitData.startingAndOptionalPawns);
            List<DirectPawnRelation> toRemove = new List<DirectPawnRelation>();
            foreach (var pawn in allWorldPawns) {
                toRemove.Clear();
                foreach (var rel in pawn.relations.DirectRelations) {
                    if (startingAndOptionalPawns.Contains(rel.otherPawn)) {
                        toRemove.Add(rel);
                    }
                }
                foreach (var rel in toRemove) {
                    pawn.relations.RemoveDirectRelation(rel);
                }
            }
            foreach (var pawn in startingAndOptionalPawns) {
                toRemove.Clear();
                foreach (var rel in pawn.relations.DirectRelations) {
                    if (allWorldPawns.Contains(rel.otherPawn)) {
                        toRemove.Add(rel);
                    }
                }
                foreach (var rel in toRemove) {
                    pawn.relations.RemoveDirectRelation(rel);
                }
            }

            // Add direct relationships.
            foreach (var rel in relationships) {
                AddRelationship(rel.source, rel.target, rel.def);
            }

            // For any parent/child group that is missing parents, create them.
            foreach (var group in parentChildGroups) {
                if (group.Children.Count > 1) {
                    // Siblings need to have 2 parents, or they will be considered half-siblings.
                    if (group.Parents.Count == 0) {
                        CustomPawn parent1 = CreateParent(Gender.Female, group.Children);
                        CustomPawn parent2 = CreateParent(Gender.Male, group.Children);
                        group.Parents.Add(parent1);
                        group.Parents.Add(parent2);
                    }
                    else if (group.Parents.Count == 1) {
                        if (group.Parents[0].Gender == Gender.Male) {
                            CustomPawn parent = CreateParent(Gender.Female, group.Children);
                            group.Parents.Add(parent);
                        }
                        else {
                            CustomPawn parent = CreateParent(Gender.Male, group.Children);
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
                CustomPawn oldestChild = null;
                foreach (var child in group.Children) {
                    if (child.BiologicalAge > age) {
                        age = child.BiologicalAge;
                        oldestChild = child;
                    }
                }
                if (oldestChild == null) {
                    continue;
                }
                foreach (var parent in group.Parents) {
                    if (parent.Hidden) {
                        int validAge = GetValidParentAge(parent, oldestChild);
                        if (validAge != parent.BiologicalAge) {
                            int diff = parent.ChronologicalAge - parent.BiologicalAge;
                            parent.BiologicalAge = validAge;
                            parent.ChronologicalAge = validAge + diff;
                        }
                    }
                }
            }

            // Add parent/child relationships.
            PawnRelationDef childDef = PawnRelationDefOf.Child;
            PawnRelationDef parentDef = PawnRelationDefOf.Parent;
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    foreach (var child in group.Children) {
                        AddRelationship(child, parent, parentDef);
                        //AddRelationship(parent, child, childDef);
                    }
                }
            }

            // Get all of the pawns to add to the world.
            HashSet<CustomPawn> worldPawns = new HashSet<CustomPawn>();
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.Hidden) {
                        CustomPawn newPawn = parent;
                        if (!worldPawns.Contains(newPawn)) {
                            worldPawns.Add(newPawn);
                        }
                    }
                    foreach (var child in group.Children) {
                        if (child.Hidden) {
                            CustomPawn newPawn = child;
                            if (!worldPawns.Contains(newPawn)) {
                                worldPawns.Add(newPawn);
                            }
                        }
                    }
                }
            }

            return worldPawns.ToList();
        }

        private int GetValidParentAge(CustomPawn parent, CustomPawn firstChild) {
            float ageOfOldestChild = firstChild.BiologicalAge;
            float lifeExpectancy = firstChild.Pawn.def.race.lifeExpectancy;
            float minAge = lifeExpectancy * 0.1625f;
            float maxAge = lifeExpectancy * 0.625f;
            float meanAge = lifeExpectancy * 0.325f;

            float ageDifference = parent.BiologicalAge - ageOfOldestChild;
            if (ageDifference < minAge) {
                float leftDistance = meanAge - minAge;
                float rightDistance = maxAge - meanAge;
                int age = (int)(Verse.Rand.GaussianAsymmetric(meanAge, leftDistance, rightDistance) + ageOfOldestChild);
                return age;
            }
            else {
                return parent.BiologicalAge;
            }
        }
        
        private CustomPawn CreateParent(Gender? gender, List<CustomPawn> children) {
            int ageOfOldestChild = 0;
            CustomPawn firstChild = null;
            foreach (var child in children) {
                if (child.BiologicalAge > ageOfOldestChild) {
                    ageOfOldestChild = child.BiologicalAge;
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

            CustomPawn parent = new CustomPawn(new Randomizer().GeneratePawn(new PawnGenerationRequestWrapper() {
                KindDef = firstChild.Pawn.kindDef,
                FixedBiologicalAge = age,
                FixedGender = gender
            }.Request));
            // Killing a pawn will pass it to the world, but not with the KeepForever flag set, so we have to do it manually.
            Find.WorldPawns.PassToWorld(parent.Pawn, RimWorld.Planet.PawnDiscardDecideMode.KeepForever);
            parent.Pawn.Kill(null, null);
            parent.Type = CustomPawnType.Temporary;
            return parent;
        }

        protected Pawn FindMatchingPawn(CustomPawn pawn) {
            // If the pawn is a world pawn, we need to create the relationships with the actual world pawn instead of the CustomPawn--which is a copy
            // of the actual pawn
            if (pawn.Type == CustomPawnType.Hidden) {
                return PrepareCarefully.Instance.FindOriginalPawnFromCopy(pawn);
            }
            else {
                return pawn.Pawn;
            }
        }

        public static void DebugWorldPawnRelationships() {
            String output = "Starting and Optional pawns:\n";
            Find.GameInitData.startingAndOptionalPawns.ForEach(p => {
                output += DebugOutputForPawn(p);
            });
            output += "World pawns:\n";
            Find.WorldPawns.AllPawnsAliveOrDead.ForEach(p => {
                output += DebugOutputForPawn(p);
            });
            output += "Custom Pawns:\n";
            PrepareCarefully.Instance.Pawns.ForEach(p => {
                output += DebugOutputForCustomPawn(p);
            });
            //Logger.Debug(output);
        }

        private static string DebugOutputForPawn(Pawn p) {
            string output = "";
            output += String.Format("- {0}[{1}]\n", p.LabelShort, p.GetUniqueLoadID());
            if (p.relations.DirectRelations.Count > 0) {
                output += "  - DirectRelations:\n";
                p.relations.DirectRelations.ForEach(r => {
                    output += String.Format("    - {0}: {1}[{2}]\n", r.def.defName, r.otherPawn.LabelShort, r.otherPawn.GetUniqueLoadID());
                });
            }
            if (p.relations.ChildrenCount > 0) {
                output += "  - Children:\n";
                foreach (var c in p.relations.Children) {
                    output += String.Format("    - {0}[{1}]\n", c.LabelShort, c.GetUniqueLoadID());
                }
            }
            return output;
        }

        private static string DebugOutputForCustomPawn(CustomPawn p) {
            string output = "";
            output += String.Format("- [{2}] {0}[{1}]\n", p.LabelShort, p.Pawn.GetUniqueLoadID(), p.Type);
            if (p.Pawn.relations.DirectRelations.Count > 0) {
                output += "  - DirectRelations:\n";
                p.Pawn.relations.DirectRelations.ForEach(r => {
                    output += String.Format("    - {0}: {1}[{2}]\n", r.def.defName, r.otherPawn.LabelShort, r.otherPawn.GetUniqueLoadID());
                });
            }
            if (p.Pawn.relations.ChildrenCount > 0) {
                output += "  - Children:\n";
                foreach (var c in p.Pawn.relations.Children) {
                    output += String.Format("    - {0}[{1}]\n", c.LabelShort, c.GetUniqueLoadID());
                }
            }
            return output;
        }

        private void AddRelationship(CustomPawn customSource, CustomPawn customTarget, PawnRelationDef def) {
            Pawn source = FindMatchingPawn(customSource);
            Pawn target = FindMatchingPawn(customTarget);
            if (source == null) {
                Logger.Warning("Could not find matching source pawn " + customSource.Pawn.LabelShort + ", " + customSource.Pawn.GetUniqueLoadID() + ", + customSource. of type " + customSource.Type + " to add relationship");
            }
            if (target == null) {
                Logger.Warning("Could not find matching target pawn " + customTarget.Pawn.LabelShort + ", " + customTarget.Pawn.GetUniqueLoadID() + " of type " + customTarget.Type + " to add relationship");
            }
            if (source == null || target == null) {
                return;
            }

            if (source.relations.DirectRelationExists(def, target)) {
                return;
            }
            source.relations.AddDirectRelation(def, target);

            // Try to find a better pawn compatibility, if it makes sense for the relationship.
            CarefullyPawnRelationDef pcDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
            if (pcDef != null) {
                if (pcDef.needsCompatibility) {
                    int originalId = target.thingIDNumber;
                    float originalScore = source.relations.ConstantPerPawnsPairCompatibilityOffset(originalId);
                    int bestId = originalId;
                    float bestScore = originalScore;
                    foreach (var id in compatibilityPool) {
                        float score = source.relations.ConstantPerPawnsPairCompatibilityOffset(id);
                        if (score > bestScore) {
                            bestScore = score;
                            bestId = id;
                        }
                    }
                    if (bestId != originalId) {
                        target.thingIDNumber = bestId;
                        compatibilityPool.Remove(bestId);
                        compatibilityPool.Add(originalId);
                    }
                }
            }
        }

        private void FillCompatibilityPool(int size) {
            int needed = size - compatibilityPool.Count;
            for (int i=0; i<needed; i++) {
                compatibilityPool.Add(Find.UniqueIDsManager.GetNextThingID());
            }
        }
        
    }
}
