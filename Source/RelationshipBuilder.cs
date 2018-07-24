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
        private List<CustomParentChildGroup> parentChildGroups;
        FieldInfo directRelationsField;
        FieldInfo pawnsWithField;
        private List<int> compatibilityPool = new List<int>();

        public RelationshipBuilder(List<CustomRelationship> relationships, List<CustomParentChildGroup> parentChildGroups) {
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
                    relevantPawns.Add(parent.Pawn);
                }
                foreach (var child in group.Children) {
                    relevantPawns.Add(child.Pawn);
                }
            }
            
            // Remove all relationships.
            foreach (var pawn in relevantPawns) {
                pawn.Pawn.relations.ClearAllRelations();
            }

            FieldInfo directRelationsField = typeof(Pawn_RelationsTracker).GetField("directRelations", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo pawnsWithField = typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);

            // Add direct relationships.
            foreach (var rel in relationships) {
                AddRelationship(rel.source.Pawn, rel.target.Pawn, rel.def);
            }

            // For any parent/child group that is missing parents, create them.
            foreach (var group in parentChildGroups) {
                if (group.Children.Count > 1) {
                    // Siblings need to have 2 parents, or they will be considered half-siblings.
                    if (group.Parents.Count == 0) {
                        CustomParentChildPawn parent1 = CreateParent(Gender.Female, group.Children);
                        CustomParentChildPawn parent2 = CreateParent(Gender.Male, group.Children);
                        group.Parents.Add(parent1);
                        group.Parents.Add(parent2);
                    }
                    else if (group.Parents.Count == 1) {
                        if (group.Parents[0].Pawn.Gender == Gender.Male) {
                            CustomParentChildPawn parent = CreateParent(Gender.Female, group.Children);
                            group.Parents.Add(parent);
                        }
                        else {
                            CustomParentChildPawn parent = CreateParent(Gender.Male, group.Children);
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
                    if (child.Pawn.BiologicalAge > age) {
                        age = child.Pawn.BiologicalAge;
                        oldestChild = child.Pawn;
                    }
                }
                if (oldestChild == null) {
                    continue;
                }
                foreach (var parent in group.Parents) {
                    if (parent.Pawn.Hidden) {
                        int validAge = GetValidParentAge(parent.Pawn, oldestChild);
                        if (validAge != parent.Pawn.BiologicalAge) {
                            int diff = parent.Pawn.ChronologicalAge - parent.Pawn.BiologicalAge;
                            parent.Pawn.BiologicalAge = validAge;
                            parent.Pawn.ChronologicalAge = validAge + diff;
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
                        AddRelationship(child.Pawn.Pawn, parent.Pawn.Pawn, parentDef);
                        AddRelationship(parent.Pawn.Pawn, child.Pawn.Pawn, childDef);
                    }
                }
            }

            // Get all of the pawns to add to the world.
            HashSet<CustomPawn> worldPawns = new HashSet<CustomPawn>();
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.Pawn.Hidden) {
                        CustomPawn newPawn = parent.Pawn;
                        if (!worldPawns.Contains(newPawn)) {
                            worldPawns.Add(newPawn);
                        }
                    }
                    foreach (var child in group.Children) {
                        if (child.Pawn.Hidden) {
                            CustomPawn newPawn = child.Pawn;
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

        private CustomParentChildPawn CreateParent(Gender? gender, List<CustomParentChildPawn> children) {
            int ageOfOldestChild = 0;
            CustomPawn firstChild = null;
            foreach (var child in children) {
                if (child.Pawn.BiologicalAge > ageOfOldestChild) {
                    ageOfOldestChild = child.Pawn.BiologicalAge;
                    firstChild = child.Pawn;
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
            parent.Type = CustomPawnType.Hidden;
            CustomParentChildPawn result = new CustomParentChildPawn(parent);
            return result;
        }

        private void AddRelationship(Pawn source, Pawn target, PawnRelationDef def) {
            List<DirectPawnRelation> sourceDirectRelations = directRelationsField.GetValue(source.relations) as List<DirectPawnRelation>;
            List<DirectPawnRelation> targetDirectRelations = directRelationsField.GetValue(target.relations) as List<DirectPawnRelation>;
            HashSet<Pawn> sourcePawnsWithDirectRelationsWithMe = pawnsWithField.GetValue(target.relations) as HashSet<Pawn>;
            HashSet<Pawn> targetPawnsWithDirectRelationsWithMe = pawnsWithField.GetValue(target.relations) as HashSet<Pawn>;
            sourceDirectRelations.Add(new DirectPawnRelation(def, target, 0));
            targetPawnsWithDirectRelationsWithMe.Add(source);
            if (def.reflexive) {
                targetDirectRelations.Add(new DirectPawnRelation(def, source, 0));
                sourcePawnsWithDirectRelationsWithMe.Add(target);
            }

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
