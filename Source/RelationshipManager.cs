using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipManager {
        protected List<CustomRelationship> deletionList = new List<CustomRelationship>();
        protected List<CustomPawn> parentChildPawns = new List<CustomPawn>();

        protected bool dirty = true;
        protected int HiddenPawnIndex = 1;
        protected int TemporaryPawnIndex = 1;

        protected Randomizer randomizer = new Randomizer();
        protected RelationshipDefinitionHelper definitions;

        protected List<CustomPawn> hiddenPawns = new List<CustomPawn>();
        protected List<CustomPawn> temporaryPawns = new List<CustomPawn>();
        protected RelationshipList relationships = new RelationshipList();
        protected List<ParentChildGroup> parentChildGroups = new List<ParentChildGroup>();

        // The list of relationship definitions from which to choose--excluding parent/child relationship
        public IEnumerable<PawnRelationDef> AllowedRelationships {
            get {
                return definitions.AllowedRelationships;
            }
        }

        // The list of defined relationships between pawns--exluding parent/child relationships
        public IEnumerable<CustomRelationship> Relationships {
            get {
                return relationships;
            }
        }

        // The list of parent/child relationship groups
        public List<ParentChildGroup> ParentChildGroups {
            get {
                return parentChildGroups;
            }
        }

        // The list of temporary pawns created to link pawns together in an indirect relationship (i.e. you need
        // have parent pawns to define a sibling relationship)
        public List<CustomPawn> TemporaryPawns {
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

        public RelationshipManager() { }

        public void InitializeWithPawns(List<CustomPawn> customPawns) {
            definitions = new RelationshipDefinitionHelper();
            InitializeHiddenPawns(customPawns);
            parentChildGroups = InitializeParentChildGroupsForStartingPawns(customPawns);
            InitializeRelationshipsForStartingPawns(customPawns);
            // Add a male and a female pawn to the new hidden pawn list.
            temporaryPawns.Add(CreateNewTemporaryPawn(Gender.Female));
            temporaryPawns.Add(CreateNewTemporaryPawn(Gender.Male));
            // Assign indices to hidden pawns (indices are used to name pawns, i.e. "Unknown 1" and "Unknown 2").
            // We do this here (and not when we initially created the hidden pawns) so that the initial indices will
            // start at 1 and count up from there as they are displayed from left to right in the UI.
            ReassignHiddenPawnIndices();
        }

        public void InitializeHiddenPawns(List<CustomPawn> customPawns) {
            HashSet<String> customPawnLookup = new HashSet<string>();
            foreach (var p in customPawns) {
                customPawnLookup.Add(p.Pawn.GetUniqueLoadID());
            }
            foreach (var p in Find.WorldPawns.AllPawnsAlive) {
                foreach (var r in p.relations.DirectRelations) {
                    if (customPawnLookup.Contains(r.otherPawn.GetUniqueLoadID())) {
                        hiddenPawns.Add(new CustomPawn(p) {
                            Index = NextHiddenParentChildIndex,
                            Type = CustomPawnType.Hidden
                        });
                        break;
                    }
                }
            }
        }

        public PawnRelationDef FindInverseRelationship(PawnRelationDef relationDef) {
            return definitions.FindInverseRelationship(relationDef);
        }

        protected Dictionary<String, CustomPawn> PawnToCustomPawnMap(IEnumerable<CustomPawn> customPawns) {
            Dictionary<String, CustomPawn> result = new Dictionary<String, CustomPawn>();
            foreach (var pawn in customPawns) {
                result.Add(pawn.Pawn.GetUniqueLoadID(), pawn);
            }
            return result;
        }

        protected List<ParentChildGroup> InitializeParentChildGroupsForStartingPawns(List<CustomPawn> customPawns) {
            // Create a map so that we can look up custom pawns based on their matching pawn.
            int pawnCount = customPawns.Count;
            Dictionary<String, CustomPawn> pawnToCustomPawnMap = PawnToCustomPawnMap(customPawns);
            
            // Go through each pawn and look for a child/parent relationship between it and all other pawns.
            Dictionary<Pawn, ParentChildGroup> groupLookup = new Dictionary<Pawn, ParentChildGroup>();

            foreach (CustomPawn child in customPawns) {
                foreach (var r in child.Pawn.relations.DirectRelations.Where(r => r.def == PawnRelationDefOf.Parent)) {
                    Pawn parent = r.otherPawn;
                    if (pawnToCustomPawnMap.TryGetValue(parent.GetUniqueLoadID(), out CustomPawn parentCustomPawn)) {
                        // See if the child has an existing parent/child group.  If not, create the group.
                        // If so, just add the parent to the existing group.
                        if (!groupLookup.TryGetValue(child.Pawn, out ParentChildGroup group)) {
                            group = new ParentChildGroup();
                            group.Children.Add(child);
                            groupLookup.Add(child.Pawn, group);
                        }
                        group.Parents.Add(parentCustomPawn);
                    }
                    else {
                        Logger.Warning("Failed to initialize parent-child relationship for child " + child.Pawn.LabelShort + "-" + child.Pawn.GetHashCode()
                            + " and parent " + parent.LabelShort + "-" + parent.GetHashCode() + ". Couldn't find the parent in the provided set of pawns.");
                    }
                }
            }

            return SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        public void InitializeWithRelationships(List<CustomRelationship> relationships) {
            List<CustomRelationship> parentChildRelationships = new List<CustomRelationship>();
            List<CustomRelationship> otherRelationships = new List<CustomRelationship>();
            foreach (var r in relationships) {
                if (r.def.defName == "Parent" || r.def.defName == "Child") {
                    parentChildRelationships.Add(r);
                }
                else {
                    otherRelationships.Add(r);
                }
            }
            foreach (var r in otherRelationships) {
                AddRelationship(r.def, r.source, r.target);
            }

            Dictionary<CustomPawn, ParentChildGroup> groupLookup = new Dictionary<CustomPawn, ParentChildGroup>();
            foreach (var relationship in parentChildRelationships) {
                CustomPawn parent = null;
                CustomPawn child = null;
                if (relationship.def == PawnRelationDefOf.Parent) {
                    child = relationship.source;
                    parent = relationship.target;
                }
                else if (relationship.def == PawnRelationDefOf.Child) {
                    child = relationship.target;
                    parent = relationship.source;
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

            parentChildGroups = SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        private List<ParentChildGroup> SortAndDedupeParentChildGroups(IEnumerable<ParentChildGroup> groups) {
            // Sort the parents.
            Dictionary<int, ParentChildGroup> parentLookup = new Dictionary<int, ParentChildGroup>();
            HashSet<ParentChildGroup> groupsToRemove = new HashSet<ParentChildGroup>();
            foreach (var group in groups) {
                group.Parents.Sort((CustomPawn a, CustomPawn b) => {
                    if (a == null || b == null) {
                        if (a == b) {
                            return 0;
                        }
                        else {
                            return (a == null) ? -1 : 1;
                        }
                    }
                    return a.Id.CompareTo(b.Id);
                });
            }

            // Generate a hash for the sorted list of parents, using a lookup to find groups
            // that have the same parents in them.  For any group with duplicate parents, copy
            // the children from that group into the existing group, and mark the duplicate
            // group for removal.
            foreach (var group in groups) {
                int hash = 0;
                foreach (var parent in group.Parents) {
                    hash ^= EqualityComparer<string>.Default.GetHashCode(parent.Id);
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
            HiddenPawnIndex = 1;
            TemporaryPawnIndex = 1;
            foreach (var group in parentChildGroups) {
                foreach (var parent in group.Parents) {
                    if (parent.Type == CustomPawnType.Hidden && parent.Index == null) {
                        parent.Index = HiddenPawnIndex++;
                    }
                    else if (parent.Type == CustomPawnType.Temporary && parent.Index == null) {
                        parent.Index = TemporaryPawnIndex++;
                    }
                }
                foreach (var child in group.Children) {
                    if (child.Type == CustomPawnType.Hidden && child.Index == null) {
                        child.Index = HiddenPawnIndex++;
                    }
                    else if (child.Type == CustomPawnType.Temporary && child.Index == null) {
                        child.Index = TemporaryPawnIndex++;
                    }
                }
            }
            foreach (var r in relationships) {
                if (r.source.Type == CustomPawnType.Hidden && r.source.Index == null) {
                    r.source.Index = HiddenPawnIndex++;
                }
                if (r.target.Type == CustomPawnType.Hidden && r.target.Index == null) {
                    r.target.Index = HiddenPawnIndex++;
                }
            }
        }

        public IEnumerable<CustomPawn> ParentChildPawns {
            get {
                return parentChildPawns;
            }
        }

        public IEnumerable<CustomPawn> ColonyAndWorldPawns {
            get {
                return ParentChildPawns.Where((CustomPawn p) => {
                    return !p.Hidden;
                });
            }
        }

        public IEnumerable<CustomPawn> HiddenParentChildPawns {
            get {
                return ParentChildPawns.Where((CustomPawn p) => {
                    return p.Type == CustomPawnType.Hidden || p.Type == CustomPawnType.Temporary;
                });
            }
        }

        public IEnumerable<CustomPawn> AvailableColonyPawns {
            get {
                return PrepareCarefully.Instance.Pawns.Where(p => p.Type == CustomPawnType.Colonist);
            }
        }

        public IEnumerable<CustomPawn> AvailableWorldPawns {
            get {
                return PrepareCarefully.Instance.Pawns.Where(p => p.Type == CustomPawnType.World);
            }
        }

        public IEnumerable<CustomPawn> ColonyAndWorldPawnsForRelationships {
            get {
                return PrepareCarefully.Instance.Pawns;
            }
        }

        public IEnumerable<CustomPawn> AvailableHiddenPawns {
            get {
                return hiddenPawns;
            }
        }

        public IEnumerable<CustomPawn> AvailableTemporaryPawns {
            get {
                return temporaryPawns;
            }
        }

        public CustomPawn AddHiddenParentChildPawn(CustomPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public CustomPawn AddTemporaryParentChildPawn(CustomPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public CustomPawn AddVisibleParentChildPawn(CustomPawn customPawn) {
            return AddVisibleParentChildPawn(customPawn.Pawn, customPawn);
        }

        public CustomPawn AddVisibleParentChildPawn(Pawn pawn, CustomPawn customPawn) {
            parentChildPawns.Add(customPawn);
            return customPawn;
        }

        public void InitializeWithCustomPawns(IEnumerable<CustomPawn> pawns) {
            parentChildPawns.Clear();

            // Create parent/child pawn records for each colonist.
            foreach (var pawn in pawns) {
                if (pawn.Type == CustomPawnType.Temporary) {
                    AddTemporaryParentChildPawn(pawn);
                }
                else if (pawn.Type == CustomPawnType.Hidden) {
                    AddHiddenParentChildPawn(pawn);
                }
                else {
                    AddVisibleParentChildPawn(pawn);
                }
            }
        }

        public void InitializeRelationshipsForStartingPawns(List<CustomPawn> customPawns) {

            // Go through each pawn and check for relationships between it and all other pawns.
            foreach (CustomPawn pawn in customPawns) {
                foreach (CustomPawn other in customPawns) {
                    if (pawn == other) {
                        continue;
                    }

                    // Find the corresponding pawn facades.
                    CustomPawn thisCustomPawn = pawn;
                    CustomPawn otherCustomPawn = other;

                    // Go through each relationship between the two pawns.
                    foreach (PawnRelationDef def in PawnRelationUtility.GetRelations(pawn.Pawn, other.Pawn)) {
                        // Don't add blood relations.
                        if (def.familyByBloodRelation) {
                            continue;
                        }
                        if (def.implied) {
                            continue;
                        }
                        // Otherwise, if no relationship records exists for this relationship, add it.
                        if (!relationships.Contains(def, thisCustomPawn, otherCustomPawn)) {
                            relationships.Add(new CustomRelationship(def, definitions.FindInverseRelationship(def), thisCustomPawn, otherCustomPawn));
                        }
                    }
                }
            }
        }

        public void Clear() {
            this.parentChildPawns.Clear();
            this.relationships.Clear();
            this.parentChildGroups.Clear();
            Clean();
        }
        
        protected void Clean() {
            dirty = false;
        }

        protected void DeleteRelationships() {
            foreach (var r in deletionList) {
                relationships.Remove(r);
            }
            deletionList.Clear();
        }

        public void AddRelationship(PawnRelationDef def, CustomPawn source, CustomPawn target) {
            if (def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null) {
                return;
            }
            this.relationships.Add(new CustomRelationship(def, definitions.FindInverseRelationship(def), source, target));
            dirty = true;
        }

        public void DeleteRelationship(CustomRelationship relationship) {
            this.relationships.Remove(relationship);
        }
        public void DeleteRelationship(PawnRelationDef def, CustomPawn source, CustomPawn target) {
            CustomRelationship toRemove = relationships.Find(def, source, target);
            if (toRemove != null) {
                deletionList.Add(toRemove);
            }
            dirty = true;
        }
        
        public void RemoveParentChildGroup(ParentChildGroup group) {
            parentChildGroups.Remove(group);
        }

        public void DeletePawn(CustomPawn pawn) {
            List<CustomRelationship> toDelete = new List<CustomRelationship>();
            foreach (var r in relationships) {
                if (r.source == pawn || r.target == pawn) {
                    deletionList.Add(r);
                }
            }
            // Remove the pawn from any parent/child group that they are in.  If the parent/child
            // group is empty after their removal, remove that group.
            List<ParentChildGroup> groupsToRemove = new List<ParentChildGroup>();
            foreach (var group in parentChildGroups) {
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
                parentChildGroups.Remove(group);
            }
            parentChildPawns.RemoveAll((CustomPawn p) => { return p == pawn; });
            
            foreach (var r in deletionList) {
                relationships.Remove(r);
            }

            this.parentChildPawns.RemoveAll((CustomPawn p) => {
                return (p == pawn);
            });

            dirty = true;
        }

        public CustomPawn ReplaceNewTemporaryCharacter(int index) {
            var pawn = temporaryPawns[index];
            temporaryPawns[index] = CreateNewTemporaryPawn(pawn.Gender);
            CustomPawn result = AddTemporaryParentChildPawn(pawn);
            return result;
        }

        public CustomPawn CreateNewTemporaryPawn(Gender gender) {
            CustomPawn pawn = new CustomPawn(new Randomizer().GeneratePawn(new PawnGenerationRequestWrapper() {
                Faction = null,
                Context = PawnGenerationContext.NonPlayer,
                FixedGender = gender
            }.Request));
            pawn.Type = CustomPawnType.Temporary;
            pawn.Index = NextTemporaryParentChildIndex;
            return pawn;
        }

    }
}

