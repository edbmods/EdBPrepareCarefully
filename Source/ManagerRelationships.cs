using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class ManagerRelationships {
        protected List<CustomizedRelationship> deletionList = new List<CustomizedRelationship>();
        protected List<CustomizedPawn> parentChildPawns = new List<CustomizedPawn>();

        protected bool dirty = true;
        protected int HiddenPawnIndex = 1;
        protected int TemporaryPawnIndex = 1;

        public RelationshipDefinitionHelper RelationshipDefinitionHelper { get; set; }

        protected List<CustomizedPawn> hiddenPawns = new List<CustomizedPawn>();
        protected List<CustomizedPawn> temporaryPawns = new List<CustomizedPawn>();
        public ModState State { get; set; }

        // The list of relationship definitions from which to choose--excluding parent/child relationship
        public IEnumerable<PawnRelationDef> AllowedRelationships {
            get {
                return RelationshipDefinitionHelper.AllowedRelationships;
            }
        }

        // The list of temporary pawns created to link pawns together in an indirect relationship (i.e. you need
        // have parent pawns to define a sibling relationship)
        public List<CustomizedPawn> TemporaryPawns {
            get {
                return temporaryPawns;
            }
        }

        public int NextHiddenParentChildIndex {
            get {
                return HiddenPawnIndex++;
            }
        }

        public int NextTemporaryParentChildIndex {
            get {
                return TemporaryPawnIndex++;
            }
        }

        public void InitializeWithPawns(IEnumerable<CustomizedPawn> customPawns) {
            InitializeHiddenPawns(customPawns);
            State.Customizations.ParentChildGroups = InitializeParentChildGroupsForStartingPawns(customPawns);
            InitializeRelationshipsForStartingPawns(customPawns);
            // Assign indices to hidden pawns (indices are used to name pawns, i.e. "Unknown 1" and "Unknown 2").
            // We do this here (and not when we initially created the hidden pawns) so that the initial indices will
            // start at 1 and count up from there as they are displayed from left to right in the UI.
            ReassignHiddenPawnIndices();
            // Add a male and a female pawn to the new temporary pawn list.
            temporaryPawns.Add(CreateNewTemporaryPawn(Gender.Female));
            temporaryPawns.Add(CreateNewTemporaryPawn(Gender.Male));
        }

        // If there are any world pawns that our starting pawns have a relationship with, then store
        // those world pawns as hidden pawns.
        public void InitializeHiddenPawns(IEnumerable<CustomizedPawn> customPawns) {
            Logger.Debug("InitializeHiddenPawns()");
            HashSet<Pawn> customPawnLookup = new HashSet<Pawn>();
            foreach (var p in customPawns) {
                customPawnLookup.Add(p.Pawn);
            }
            foreach (var p in customPawns) {
                foreach (var r in p.Pawn.relations.DirectRelations) {
                    Pawn otherPawn = r.otherPawn;
                    if (!customPawnLookup.Contains(otherPawn)) {
                        if (!hiddenPawns.ContainsAny(c => c.Pawn == otherPawn)) {
                            Logger.Debug("  Added hidden pawn: " + otherPawn.LabelShort + ", faction = " + otherPawn.Faction?.Name + ", leader = " + (otherPawn.Faction?.leader == otherPawn ? "true" : "false"));
                            hiddenPawns.Add(new CustomizedPawn() {
                                Pawn = otherPawn,
                                Type = CustomizedPawnType.Hidden,
                                TemporaryPawn = new TemporaryPawn() {
                                    Index = NextHiddenParentChildIndex,
                                    Gender = otherPawn.gender
                                }
                            });
                        }
                    }
                }
            }
            //foreach (var p in Find.WorldPawns.AllPawnsAlive.Concat(Find.WorldPawns.AllPawnsDead).Concat(Find.WorldPawns.ForcefullyKeptPawns)) {
            //    bool added = false;
            //    foreach (var r in p.relations.DirectRelations) {
            //        if (customPawnLookup.Contains(r.otherPawn.GetUniqueLoadID())) {
            //            added = true;
            //            Logger.Debug("  Added hidden pawn: " + p.Label + ", faction = " + p.Faction?.Name + ", leader = " + (p.Faction?.leader == p ? "true" : "false"));
            //            hiddenPawns.Add(new CustomizedPawn() {
            //                Pawn = p,
            //                Index = NextHiddenParentChildIndex,
            //                Type = CustomizedPawnType.Hidden,
            //                OriginalPawn = p
            //            });
            //            break;
            //        }
            //    }
            //    if (!added) {
            //        Logger.Debug("  Skipped hidden pawn: " + p.Label + ", faction = " + p.Faction?.Name + ", leader = " + (p.Faction?.leader == p ? "true" : "false"));
            //    }
            //}
            Logger.Debug("Hidden pawn count: " + hiddenPawns.Count);
        }

        public PawnRelationDef FindInverseRelationship(PawnRelationDef relationDef) {
            return RelationshipDefinitionHelper.FindInverseRelationship(relationDef);
        }

        protected Dictionary<Pawn, CustomizedPawn> PawnToCustomizedPawnMap(IEnumerable<CustomizedPawn> customPawns) {
            var result = new Dictionary<Pawn, CustomizedPawn>();
            foreach (var customPawn in customPawns) {
                if (customPawn.Pawn != null) {
                    result.Add(customPawn.Pawn, customPawn);
                }
            }
            return result;
        }

        protected List<ParentChildGroup> InitializeParentChildGroupsForStartingPawns(IEnumerable<CustomizedPawn> customPawns) {
            // Create a map so that we can look up custom pawns based on their matching pawn.
            int pawnCount = customPawns.Count();
            Dictionary<Pawn, CustomizedPawn> pawnToCustomizedPawnMap = PawnToCustomizedPawnMap(customPawns.Concat(hiddenPawns));
            
            // Go through each pawn and look for a child/parent relationship between it and all other pawns.
            Dictionary<Pawn, ParentChildGroup> groupLookup = new Dictionary<Pawn, ParentChildGroup>();

            foreach (CustomizedPawn child in customPawns) {
                foreach (var r in child?.Pawn.relations.DirectRelations.Where(r => r.def == PawnRelationDefOf.Parent)) {
                    Pawn parent = r.otherPawn;
                    if (pawnToCustomizedPawnMap.TryGetValue(parent, out CustomizedPawn parentCustomizedPawn)) {
                        // See if the child has an existing parent/child group.  If not, create the group.
                        // If so, just add the parent to the existing group.
                        if (!groupLookup.TryGetValue(child.Pawn, out ParentChildGroup group)) {
                            group = new ParentChildGroup();
                            group.Children.Add(child);
                            groupLookup.Add(child.Pawn, group);
                        }
                        group.Parents.Add(parentCustomizedPawn);
                    }
                    else {
                        Logger.Warning("Failed to initialize parent-child relationship for child " + child.Pawn.LabelShort + "-" + child.Pawn.GetHashCode()
                            + " and parent " + parent.LabelShort + "-" + parent.GetHashCode() + ". Couldn't find the parent in the provided set of pawns.");
                    }
                }
            }

            return SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        public void InitializeWithRelationships(List<CustomizedRelationship> relationships) {
            List<CustomizedRelationship> parentChildRelationships = new List<CustomizedRelationship>();
            List<CustomizedRelationship> otherRelationships = new List<CustomizedRelationship>();
            foreach (var r in relationships) {
                if (r.Def.defName == "Parent" || r.Def.defName == "Child") {
                    parentChildRelationships.Add(r);
                }
                else {
                    otherRelationships.Add(r);
                }
            }
            foreach (var r in otherRelationships) {
                AddRelationship(r.Def, r.Source, r.Target);
            }

            Dictionary<CustomizedPawn, ParentChildGroup> groupLookup = new Dictionary<CustomizedPawn, ParentChildGroup>();
            foreach (var relationship in parentChildRelationships) {
                CustomizedPawn parent = null;
                CustomizedPawn child = null;
                if (relationship.Def == PawnRelationDefOf.Parent) {
                    child = relationship.Source;
                    parent = relationship.Target;
                }
                else if (relationship.Def == PawnRelationDefOf.Child) {
                    child = relationship.Target;
                    parent = relationship.Source;
                }
                if (parent == null) {
                    Logger.Warning("Could not add relationship because of missing parent");
                    continue;
                }
                if (child == null) {
                    Logger.Warning("Could not add relationship because of missing child");
                    continue;
                }

                // See if the child has an existing parent/child group.  If not, create the group.
                // If so, just add the parent.
                ParentChildGroup group;
                if (!groupLookup.TryGetValue(child, out group)) {
                    group = new ParentChildGroup();
                    group.Children.Add(child);
                    groupLookup.Add(child, group);
                }
                group.Parents.Add(parent);
            }

            State.Customizations.ParentChildGroups = SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        private List<ParentChildGroup> SortAndDedupeParentChildGroups(IEnumerable<ParentChildGroup> groups) {
            // Sort the parents.
            Dictionary<int, ParentChildGroup> parentLookup = new Dictionary<int, ParentChildGroup>();
            HashSet<ParentChildGroup> groupsToRemove = new HashSet<ParentChildGroup>();
            foreach (var group in groups) {
                group.Parents.Sort((CustomizedPawn a, CustomizedPawn b) => {
                    if (a == null || b == null) {
                        if (a == b) {
                            return 0;
                        }
                        else {
                            return (a == null) ? -1 : 1;
                        }
                    }
                    // TODO: Evaluate
                    //return string.Compare(a.Id, b.Id);
                    return string.Compare(a.Pawn.GetUniqueLoadID(), b.Pawn.GetUniqueLoadID());
                });
            }

            // Generate a hash for the sorted list of parents, using a lookup to find groups
            // that have the same parents in them.  For any group with duplicate parents, copy
            // the children from that group into the existing group, and mark the duplicate
            // group for removal.
            foreach (var group in groups) {
                int hash = 0;
                foreach (var parent in group.Parents) {
                    // TODO: Evalute Id
                    //hash ^= EqualityComparer<string>.Default.GetHashCode(parent.Id);
                    hash ^= EqualityComparer<string>.Default.GetHashCode(parent.Pawn.GetUniqueLoadID());
                }
                if (parentLookup.TryGetValue(hash, out var existing)) {
                    //Logger.Debug("Removing duplicate group: " + group);
                    //Logger.Debug("  Duplicate of group: " + existing);
                    foreach (var child in group.Children) {
                        existing.Children.Add(child);
                    }
                    //Logger.Debug("  Added children from dupe: " + existing);
                    groupsToRemove.Add(group);
                }
                else {
                    parentLookup.Add(hash, group);
                }
            }

            // Create the final list, discarding the groups that were merged.
            List<ParentChildGroup> result = new List<ParentChildGroup>();
            foreach (var group in groups) {
                if (!groupsToRemove.Contains(group)) {
                    result.Add(group);
                    //Logger.Debug(group.ToString());
                }
            }
            
            return result;
        }

        public void ReassignHiddenPawnIndices() {
            Logger.Debug("ReassignHiddenPawnIndices()");
            // Reset all of the indices
            HiddenPawnIndex = 1;
            TemporaryPawnIndex = 1;
            foreach (var group in State.Customizations.ParentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.TemporaryPawn != null) {
                        parent.TemporaryPawn.Index = 0;
                    }
                }
                foreach (var child in group.Children) {
                    if (child.TemporaryPawn != null) {
                        child.TemporaryPawn.Index = 0;
                    }
                }
            }
            foreach (var r in State.Customizations.Relationships) {
                if (r.Source.TemporaryPawn != null) {
                    r.Source.TemporaryPawn.Index = 0;
                }
                if (r.Target.TemporaryPawn != null) {
                    r.Target.TemporaryPawn.Index = 0;
                }
            }

            // Reassign all of the indices.  Pawns may appear multiple times in the relationship lists, so we only
            // give them a new index if we haven't already assigned one.
            foreach (var group in State.Customizations.ParentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.Type == CustomizedPawnType.Hidden && parent.TemporaryPawn.Index == 0) {
                        parent.TemporaryPawn.Index = HiddenPawnIndex++;
                    }
                    else if (parent.Type == CustomizedPawnType.Temporary && parent.TemporaryPawn.Index == 0) {
                        parent.TemporaryPawn.Index = TemporaryPawnIndex++;
                    }
                }
                foreach (var child in group.Children) {
                    if (child.Type == CustomizedPawnType.Hidden && child.TemporaryPawn.Index == 0) {
                        child.TemporaryPawn.Index = HiddenPawnIndex++;
                    }
                    else if (child.Type == CustomizedPawnType.Temporary && child.TemporaryPawn.Index == 0) {
                        child.TemporaryPawn.Index = TemporaryPawnIndex++;
                    }
                }
            }
            foreach (var r in State.Customizations.Relationships) {
                if (r.Source.Type == CustomizedPawnType.Hidden && r.Source.TemporaryPawn.Index == 0) {
                    r.Source.TemporaryPawn.Index = HiddenPawnIndex++;
                }
                else if (r.Source.Type == CustomizedPawnType.Temporary && r.Source.TemporaryPawn.Index == 0) {
                    r.Source.TemporaryPawn.Index = TemporaryPawnIndex++;
                }
                if (r.Target.Type == CustomizedPawnType.Hidden && r.Target.TemporaryPawn.Index == 0) {
                    r.Target.TemporaryPawn.Index = HiddenPawnIndex++;
                }
                else if (r.Target.Type == CustomizedPawnType.Temporary && r.Target.TemporaryPawn.Index == 0) {
                    r.Target.TemporaryPawn.Index = TemporaryPawnIndex++;
                }
            }
            foreach (var p in temporaryPawns) {
                p.TemporaryPawn.Index = TemporaryPawnIndex++;
            }
            foreach (var p in hiddenPawns) {
                p.TemporaryPawn.Index = HiddenPawnIndex++;
            }
        }

        public IEnumerable<CustomizedPawn> ParentChildPawns {
            get {
                return parentChildPawns;
            }
        }

        public IEnumerable<CustomizedPawn> ColonyAndWorldPawns {
            get {
                return ParentChildPawns.Where((CustomizedPawn p) => {
                    return p.Type != CustomizedPawnType.Hidden;
                });
            }
        }

        public IEnumerable<CustomizedPawn> HiddenParentChildPawns {
            get {
                return ParentChildPawns.Where((CustomizedPawn p) => {
                    return p.Type == CustomizedPawnType.Hidden || p.Type == CustomizedPawnType.Temporary;
                });
            }
        }

        public IEnumerable<CustomizedPawn> AvailableColonyPawns {
            get {
                return State.Customizations.ColonyPawns;
            }
        }

        public IEnumerable<CustomizedPawn> AvailableWorldPawns {
            get {
                return State.Customizations.WorldPawns;
            }
        }

        public IEnumerable<CustomizedPawn> ColonyAndWorldPawnsForRelationships {
            get {
                return State.Customizations.AllPawns;
            }
        }

        public IEnumerable<CustomizedPawn> AvailableHiddenPawns {
            get {
                return hiddenPawns;
            }
        }

        public IEnumerable<CustomizedPawn> AvailableTemporaryPawns {
            get {
                return temporaryPawns;
            }
        }

        public CustomizedPawn AddHiddenParentChildPawn(CustomizedPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public CustomizedPawn AddTemporaryParentChildPawn(CustomizedPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public CustomizedPawn AddVisibleParentChildPawn(CustomizedPawn customPawn) {
            return AddVisibleParentChildPawn(customPawn.Pawn, customPawn);
        }

        public CustomizedPawn AddVisibleParentChildPawn(Pawn pawn, CustomizedPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public void InitializeWithCustomizedPawns(IEnumerable<CustomizedPawn> pawns) {
            parentChildPawns.Clear();

            // Create parent/child pawn records for each colonist.
            foreach (var pawn in pawns) {
                if (pawn.Type == CustomizedPawnType.Temporary) {
                    AddTemporaryParentChildPawn(pawn);
                }
                else if (pawn.Type == CustomizedPawnType.Hidden) {
                    AddHiddenParentChildPawn(pawn);
                }
                else {
                    AddVisibleParentChildPawn(pawn);
                }
            }
        }

        public void InitializeRelationshipsForStartingPawns(IEnumerable<CustomizedPawn> customPawns) {
            Logger.Debug("InitializeRelationshipsForStartingPawns(): " + customPawns?.Count());
            IEnumerable<CustomizedPawn> allPawns = customPawns.Concat(hiddenPawns);

            // Go through each pawn and check for relationships between it and all other pawns.
            foreach (CustomizedPawn pawn in allPawns) {
                foreach (CustomizedPawn other in allPawns) {
                    if (pawn == other) {
                        continue;
                    }

                    // Find the corresponding pawn facades.
                    CustomizedPawn thisCustomizedPawn = pawn;
                    CustomizedPawn otherCustomizedPawn = other;

                    // Go through each relationship between the two pawns.
                    foreach (PawnRelationDef def in PawnRelationUtility.GetRelations(pawn.Pawn, other.Pawn)) {
                        // Don't add blood relations.
                        if (def.familyByBloodRelation) {
                            //Logger.Debug("Skipping blood relation: " + def?.defName);
                            continue;
                        }
                        if (def.implied) {
                            //Logger.Debug("Skipping implied relation: " + def?.defName);
                            continue;
                        }
                        Logger.Debug("Found relationship: " + def?.defName);
                        // Otherwise, if no relationship records exists for this relationship, add it.
                        if (!State.Customizations.Relationships.Contains(def, thisCustomizedPawn, otherCustomizedPawn)) {
                            //Logger.Debug("Add relationship: " + def?.defName);
                            State.Customizations.Relationships.Add(new CustomizedRelationship(def, RelationshipDefinitionHelper.FindInverseRelationship(def), thisCustomizedPawn, otherCustomizedPawn));
                        }
                        else {
                            //Logger.Debug("Didn't add relationship because it was already added");
                        }
                    }
                }
            }
        }

        public void Clear() {
            this.parentChildPawns.Clear();
            State.Customizations.Relationships.Clear();
            State.Customizations.ParentChildGroups.Clear();
            Clean();
        }
        
        protected void Clean() {
            dirty = false;
        }

        protected void DeleteRelationships() {
            foreach (var r in deletionList) {
                State.Customizations.Relationships.Remove(r);
            }
            deletionList.Clear();
        }

        public void AddRelationship(CustomizedRelationship r) {
            AddRelationship(r.Def, r.Source, r.Target);
        }

        public void AddRelationship(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            if (def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null) {
                return;
            }
            this.State.Customizations.Relationships.Add(new CustomizedRelationship(def, RelationshipDefinitionHelper.FindInverseRelationship(def), source, target));
            dirty = true;
        }


        public void DeleteRelationship(CustomizedRelationship relationship) {
            State.Customizations.Relationships.Remove(relationship);
        }
        public void DeleteRelationship(PawnRelationDef def, CustomizedPawn source, CustomizedPawn target) {
            CustomizedRelationship toRemove = State.Customizations.Relationships.Find(def, source, target);
            if (toRemove != null) {
                deletionList.Add(toRemove);
            }
            dirty = true;
        }
        
        public void RemoveParentChildGroup(ParentChildGroup group) {
            State.Customizations.ParentChildGroups.Remove(group);
        }

        public void DeletePawn(CustomizedPawn pawn) {
            List<CustomizedRelationship> toDelete = new List<CustomizedRelationship>();
            foreach (var r in State.Customizations.Relationships) {
                if (r.Source == pawn || r.Target == pawn) {
                    deletionList.Add(r);
                }
            }
            // Remove the pawn from any parent/child group that they are in.  If the parent/child
            // group is empty after their removal, remove that group.
            var groupsToRemove = new List<ParentChildGroup>();
            foreach (var group in State.Customizations.ParentChildGroups) {
                int index = group.Parents.IndexOf(pawn);
                if (index != -1) {
                    group.Parents.RemoveAt(index);
                }
                index = group.Children.IndexOf(pawn);
                if (index != -1) {
                    group.Children.RemoveAt(index);
                }
                if (group.Parents.Count == 0 && group.Children.Count == 0) {
                    groupsToRemove.Add(group);
                }
            }
            foreach (var group in groupsToRemove) {
                State.Customizations.ParentChildGroups.Remove(group);
            }
            parentChildPawns.RemoveAll((CustomizedPawn p) => { return p == pawn; });
            
            foreach (var r in deletionList) {
                State.Customizations.Relationships.Remove(r);
            }

            this.parentChildPawns.RemoveAll((CustomizedPawn p) => {
                return (p == pawn);
            });

            dirty = true;
        }

        public CustomizedPawn ReplaceNewTemporaryCharacter(int index) {
            var pawn = temporaryPawns[index];
            temporaryPawns[index] = CreateNewTemporaryPawn(pawn.TemporaryPawn.Gender);
            CustomizedPawn result = AddTemporaryParentChildPawn(pawn);
            return result;
        }

        public CustomizedPawn CreateNewTemporaryPawn(Gender gender) {
            CustomizedPawn pawn = new CustomizedPawn() {
                Customizations = null,
                Type = CustomizedPawnType.Temporary,
                TemporaryPawn = new TemporaryPawn() {
                    Index = NextTemporaryParentChildIndex,
                    Gender = gender
                }
            };
            return pawn;
        }

        public void RemoveAllRelationshipsForPawn(Pawn pawn) {
            if (pawn == null) {
                return;
            }
            State.Customizations.Relationships.RemoveAll(r => r.Source.Pawn == pawn || r.Target.Pawn == pawn);
            foreach (var group in State.Customizations.ParentChildGroups) {
                group.Parents.RemoveAll(p => p.Pawn == pawn);
                group.Children.RemoveAll(p => p.Pawn == pawn);
            }
            State.Customizations.ParentChildGroups.RemoveAll(g => g.Parents.Count + g.Children.Count <= 1);
        }

        public RelationshipBuilder GetRelationshipBuilder() {
            return new RelationshipBuilder(State.Customizations.AllPawns, State.Customizations.Relationships, State.Customizations.ParentChildGroups);
        }
    }
}

