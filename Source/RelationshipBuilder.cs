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

        public RelationshipBuilder(List<CustomRelationship> relationships, List<CustomParentChildGroup> parentChildGroups) {
            directRelationsField = typeof(Pawn_RelationsTracker).GetField("directRelations", BindingFlags.Instance | BindingFlags.NonPublic);
            pawnsWithField = typeof(Pawn_RelationsTracker).GetField("pawnsWithDirectRelationsWithMe", BindingFlags.Instance | BindingFlags.NonPublic);
            this.relationships = relationships;
            this.parentChildGroups = parentChildGroups;
        }
        public void Build() {
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

            // For any parent/child group that is missing parents, create a parent.
            foreach (var group in parentChildGroups) {
                if (group.Children.Count > 1 && group.Parents.Count == 0) {
                    CustomParentChildPawn parent = CreateParent(null, group.Children);
                    group.Parents.Add(parent);
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
                    if (parent.Hidden) {
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

            // Add pawns to the world.
            // TODO: Killing a pawn adds it to the world and doesn't force us to figure out which
            // faction we want to assign the pawn to (not sure that I understand why all relatives need
            // to live on the planet or be available in a spacer faction).  Should revisit this to
            // decide if that's really what we want to do.
            // Start by assigning each hidden pawn to a random faction that's not the player faction.
            // If the pawn ends up assigned to the player faction, the stats screen will count the pawn
            // as a killed colonist--we don't want that to happen.
            FactionManager factionManager = Find.World.factionManager;
            Faction newPawnFaction = factionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            if (newPawnFaction == null) {
                if (!factionManager.TryGetRandomNonColonyHumanlikeFaction(out newPawnFaction, false, true)) {
                    newPawnFaction = factionManager.AllFactions.RandomElementWithFallback(Faction.OfPlayer);
                }
            }
            // Kill the pawns.
            HashSet<Pawn> pawnsAddedToWorld = new HashSet<Pawn>();
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.Hidden) {
                        Pawn newPawn = parent.Pawn.Pawn;
                        newPawn.SetFactionDirect(newPawnFaction);
                        if (!pawnsAddedToWorld.Contains(newPawn)) {
                            newPawn.Kill(null);
                            pawnsAddedToWorld.Add(newPawn);
                        }
                    }
                    foreach (var child in group.Children) {
                        if (child.Hidden) {
                            Pawn newPawn = child.Pawn.Pawn;
                            newPawn.SetFactionDirect(newPawnFaction);
                            if (!pawnsAddedToWorld.Contains(newPawn)) {
                                newPawn.Kill(null);
                                pawnsAddedToWorld.Add(newPawn);
                            }
                        }
                    }
                }
            }
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
            CustomParentChildPawn result = new CustomParentChildPawn(parent);
            result.Hidden = true;
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
        }
        
    }
}
