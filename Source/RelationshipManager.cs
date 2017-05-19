using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully {
    public class RelationshipManager {
        protected Randomizer randomizer = new Randomizer();
        protected List<PawnRelationDef> allowedRelationships = new List<PawnRelationDef>();
        protected Dictionary<PawnRelationDef, PawnRelationDef> inverseRelationships = new Dictionary<PawnRelationDef, PawnRelationDef>();
        //protected Dictionary<CustomPawn, Pawn> facadeToSimulatedPawnMap = new Dictionary<CustomPawn, Pawn>();
        //protected Dictionary<Pawn, CustomPawn> simulatedPawnToFacadeMap = new Dictionary<Pawn, CustomPawn>();
        protected List<CustomRelationship> deletionList = new List<CustomRelationship>();
        protected List<CustomParentChildPawn> parentChildPawns = new List<CustomParentChildPawn>();
        protected Dictionary<Pawn, CustomParentChildPawn> parentChildPawnLookup = new Dictionary<Pawn, CustomParentChildPawn>();
        protected Dictionary<CustomPawn, CustomParentChildPawn> parentChildCustomPawnLookup = new Dictionary<CustomPawn, CustomParentChildPawn>();
        protected bool dirty = true;
        protected int HiddenParentChildIndex = 1;

        protected RelationshipList relationships = new RelationshipList();
        //protected RelationshipList derivedRelationships = new RelationshipList();

        public RelationshipManager(List<Pawn> originalPawns, List<CustomPawn> correspondingFacades) {
            PopulateAllowedRelationships();
            PopulateInverseRelationships();
            InitializeRelationshipsForStartingPawns(originalPawns, correspondingFacades);
        }

        public int NextHiddenParentChildIndex {
            get {
                return HiddenParentChildIndex++;
            }
        }

        protected void PopulateAllowedRelationships() {
            allowedRelationships.AddRange(DefDatabase<PawnRelationDef>.AllDefs.ToList().FindAll((PawnRelationDef def) => {
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
            }));
        }

        protected void PopulateInverseRelationships() {
            foreach (var def in DefDatabase<PawnRelationDef>.AllDefs) {
                PawnRelationDef inverse = null;
                CarefullyPawnRelationDef extended = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
                if (extended != null && extended.inverse != null) {
                    inverse = DefDatabase<PawnRelationDef>.GetNamedSilentFail(extended.inverse);
                }
                else {
                    inverse = ComputeInverseRelationship(def);
                }
                if (inverse != null) {
                    inverseRelationships[def] = inverse;
                }
            }
        }

        public PawnRelationDef FindInverseRelationship(PawnRelationDef def) {
            PawnRelationDef inverse;
            if (inverseRelationships.TryGetValue(def, out inverse)) {
                return inverse;
            }
            else {
                return null;
            }
        }

        public PawnRelationDef ComputeInverseRelationship(PawnRelationDef def) {
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
                    Log.Message("Returned carefully worker for " + def.defName + ", " + worker.GetType().FullName);
                    return carefullyDef.Worker;
                }
                else {
                    return def.Worker;
                }
            }
        }

        private List<CustomParentChildGroup> parentChildGroups = new List<CustomParentChildGroup>();
        public List<CustomParentChildGroup> ParentChildGroups {
            get {
                return parentChildGroups;
            }
        }

        private void InitializeParentChildGroupsForStartingPawns(List<Pawn> pawns, List<CustomPawn> correspondingFacades) {
            // Create a map so that we can look up custom pawns based on their matching original pawn.
            Dictionary<Pawn, CustomPawn> pawnToFacadeMap = new Dictionary<Pawn, CustomPawn>();
            int pawnCount = pawns.Count;
            for (int i = 0; i < pawns.Count; i++) {
                pawnToFacadeMap.Add(pawns[i], correspondingFacades[i]);
            }
            
            // Go through each pawn and look for a child/parent relationship between it and all other pawns.
            Dictionary<Pawn, CustomParentChildGroup> groupLookup = new Dictionary<Pawn, CustomParentChildGroup>();
            foreach (Pawn child in pawns) {
                foreach (var r in child.relations.DirectRelations) {
                    //Log.Message("Relationship: " + r.def.defName + ", " + child.LabelShort + " & " + r.otherPawn.LabelShort);
                    if (r.def == PawnRelationDefOf.Parent) {
                        Pawn parent = r.otherPawn;
                        CustomParentChildPawn parentCustomPawn = parentChildPawnLookup[parent];
                        CustomParentChildPawn childCustomPawn = parentChildPawnLookup[child];

                        // See if the child has an existing parent/child group.  If not, create the group.
                        // If so, just add the parent.
                        CustomParentChildGroup group;
                        if (!groupLookup.TryGetValue(child, out group)) {
                            group = new CustomParentChildGroup();
                            group.Children.Add(childCustomPawn);
                            groupLookup.Add(child, group);
                        }
                        group.Parents.Add(parentCustomPawn);
                    }
                }
            }

            SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        private void InitializeParentChildGroupsForLoadedPawns(List<CustomRelationship> relationships) {

            Dictionary<CustomParentChildPawn, CustomParentChildGroup> groupLookup = new Dictionary<CustomParentChildPawn, CustomParentChildGroup>();
            foreach (var relationship in relationships) {
                CustomParentChildPawn parent = null;
                CustomParentChildPawn child = null;
                if (relationship.def == PawnRelationDefOf.Parent) {
                    parentChildCustomPawnLookup.TryGetValue(relationship.source, out child);
                    parentChildCustomPawnLookup.TryGetValue(relationship.target, out parent);
                }
                else if (relationship.def == PawnRelationDefOf.Child) {
                    parentChildCustomPawnLookup.TryGetValue(relationship.target, out child);
                    parentChildCustomPawnLookup.TryGetValue(relationship.source, out parent);
                }
                if (parent == null) {
                    Log.Warning("Could not add relationship because of missing parent");
                    continue;
                }
                if (child == null) {
                    Log.Warning("Could not add relationship because of missing child");
                    continue;
                }

                // See if the child has an existing parent/child group.  If not, create the group.
                // If so, just add the parent.
                CustomParentChildGroup group;
                if (!groupLookup.TryGetValue(child, out group)) {
                    group = new CustomParentChildGroup();
                    group.Children.Add(child);
                    groupLookup.Add(child, group);
                }
                group.Parents.Add(parent);
            }

            SortAndDedupeParentChildGroups(groupLookup.Values);
        }

        private void SortAndDedupeParentChildGroups(IEnumerable<CustomParentChildGroup> groups) {
            
            // Sort the parents.
            Dictionary<int, CustomParentChildGroup> parentLookup = new Dictionary<int, CustomParentChildGroup>();
            HashSet<CustomParentChildGroup> groupsToRemove = new HashSet<CustomParentChildGroup>();
            foreach (var group in groups) {
                group.Parents.Sort((CustomParentChildPawn a, CustomParentChildPawn b) => {
                    if (a.Pawn == null || b.Pawn == null) {
                        if (a.Pawn == b.Pawn) {
                            return 0;
                        }
                        else {
                            return (a.Pawn == null) ? -1 : 1;
                        }
                    }
                    return a.Pawn.Id.CompareTo(b.Pawn.Id);
                });
            }

            // Generate a hash for the sorted list of parents, using a lookup to find groups
            // that have the same parents in them.  For any group with duplicate parents, copy
            // the children from that group into the existing group, and mark the duplicate
            // group for removal.
            foreach (var group in groups) {
                int hash = 0;
                foreach (var parent in group.Parents) {
                    hash = hash ^ EqualityComparer<string>.Default.GetHashCode(parent.Pawn.Id);
                }
                CustomParentChildGroup existing;
                if (parentLookup.TryGetValue(hash, out existing)) {
                    //Log.Message("Removing duplicate group: " + group);
                    //Log.Message("  Duplicate of group: " + existing);
                    foreach (var child in group.Children) {
                        existing.Children.Add(child);
                    }
                    //Log.Message("  Added children from dupe: " + existing);
                    groupsToRemove.Add(group);
                }
                else {
                    parentLookup.Add(hash, group);
                }
            }

            // Create the final list, discarding the groups that were merged.
            List<CustomParentChildGroup> result = new List<CustomParentChildGroup>();
            foreach (var group in groups) {
                if (!groupsToRemove.Contains(group)) {
                    result.Add(group);
                    //Log.Message(group.ToString());
                }
            }

            // Assign indices to hidden pawns (indices are used to name pawns, i.e. "Unknown 1" and "Unknown 2").
            // We do this here (and not when we initially created the hidden pawns) so that the initial indices will
            // start at 1 and count up from there as they are displayed from left to right in the UI.
            HiddenParentChildIndex = 1;
            foreach (var group in result) {
                foreach (var parent in group.Parents) {
                    if (parent.Hidden && parent.Index == 0) {
                        parent.Index = HiddenParentChildIndex++;
                    }
                }
                foreach (var child in group.Children) {
                    if (child.Hidden && child.Index == 0) {
                        child.Index = HiddenParentChildIndex++;
                    }
                }
            }

            parentChildGroups = result;
        }

        public IEnumerable<CustomParentChildPawn> ParentChildPawns {
            get {
                return parentChildPawns;
            }
        }

        public CustomParentChildPawn AddHiddenParentChildPawn(CustomPawn customPawn) {
            CustomParentChildPawn parentChildPawn = new CustomParentChildPawn(customPawn, true);
            parentChildPawns.Add(parentChildPawn);
            parentChildPawnLookup.Add(customPawn.Pawn, parentChildPawn);
            parentChildCustomPawnLookup.Add(customPawn, parentChildPawn);
            return parentChildPawn;
        }

        public CustomParentChildPawn AddHiddenParentChildPawn(Pawn pawn) {
            CustomPawn customPawn = new CustomPawn(pawn);
            return AddHiddenParentChildPawn(customPawn);
        }

        public CustomParentChildPawn AddVisibleParentChildPawn(CustomPawn customPawn) {
            return AddVisibleParentChildPawn(customPawn.Pawn, customPawn);
        }

        public CustomParentChildPawn AddVisibleParentChildPawn(Pawn pawn, CustomPawn customPawn) {
            CustomParentChildPawn parentChildPawn = new CustomParentChildPawn(customPawn, false);
            parentChildPawns.Add(parentChildPawn);
            parentChildPawnLookup.Add(pawn, parentChildPawn);
            parentChildCustomPawnLookup.Add(customPawn, parentChildPawn);
            return parentChildPawn;
        }

        public void CreateParentChildPawnsForStartingPawns(List<Pawn> pawns, List<CustomPawn> correspondingCustomPawns) {
            parentChildPawns.Clear();
            parentChildPawnLookup.Clear();
            parentChildCustomPawnLookup.Clear();

            // Create parent/child pawn records for each colonist.
            int count = pawns.Count;
            for (int i=0; i< count; i++) {
                AddVisibleParentChildPawn(pawns[i], correspondingCustomPawns[i]);
            }

            // Create parent/child pawn records for all hidden parents for each colonist.  Theoretically, this
            // could include hidden children as well, but that would not happen with vanilla relationship generation.
            foreach (Pawn child in pawns) {
                foreach (var r in child.relations.DirectRelations) {
                    if (r.def == PawnRelationDefOf.Parent) {
                        Pawn parent = r.otherPawn;
                        if (!parentChildPawnLookup.ContainsKey(parent)) {
                            //Log.Warning("Creating custom pawn for missing parent pawn " + parent.LabelShort);
                            AddHiddenParentChildPawn(parent);
                        }
                        if (!parentChildPawnLookup.ContainsKey(child)) {
                            //Log.Warning("Creating custom pawn for missing child pawn " + child.LabelShort);
                            AddHiddenParentChildPawn(child);
                        }
                    }
                }
            }
        }

        public void CreateParentChildPawnsForLoadedPawns(List<CustomPawn> colonists, List<CustomPawn> hiddenPawns) {
            parentChildPawns.Clear();
            parentChildPawnLookup.Clear();
            parentChildCustomPawnLookup.Clear();

            // Create parent/child pawn records for each colonist.
            foreach (var pawn in colonists) {
                AddVisibleParentChildPawn(pawn);
            }

            foreach (var pawn in hiddenPawns) {
                AddHiddenParentChildPawn(pawn);
            }
        }

        public void InitializeRelationshipsForStartingPawns(List<Pawn> pawns, List<CustomPawn> correspondingCustomPawns) {
            CreateParentChildPawnsForStartingPawns(pawns, correspondingCustomPawns);
            InitializeParentChildGroupsForStartingPawns(pawns, correspondingCustomPawns);
            // Create a map so that we can lookup custom pawns based on their matching original pawn.
            Dictionary<Pawn, CustomPawn> pawnToCustomPawnDictionary = new Dictionary<Pawn, CustomPawn>();
            int pawnCount = pawns.Count;
            for (int i = 0; i < pawns.Count; i++) {
                pawnToCustomPawnDictionary.Add(pawns[i], correspondingCustomPawns[i]);
            }

            // Go through each pawn and check for relationships between it and all other pawns.
            foreach (Pawn pawn in pawns) {
                foreach (Pawn other in pawns) {
                    if (pawn == other) {
                        continue;
                    }

                    // Find the corresponding pawn facades.
                    CustomPawn thisCustomPawn = pawnToCustomPawnDictionary[pawn];
                    CustomPawn otherCustomPawn = pawnToCustomPawnDictionary[other];

                    // Go through each relationship between the two pawns.
                    foreach (PawnRelationDef def in PawnRelationUtility.GetRelations(pawn, other)) {
                        // Don't add blood relations.
                        if (def.familyByBloodRelation) {
                            continue;
                        }
                        if (def.implied) {
                            continue;
                        }
                        // Otherwise, if no relationship records exists for this relationship, add it.
                        if (!relationships.Contains(def, thisCustomPawn, otherCustomPawn)) {
                            relationships.Add(new CustomRelationship(def, FindInverseRelationship(def), thisCustomPawn, otherCustomPawn));
                        }
                    }
                }
            }
        }

        public void InitializeParentChildRelationshipsForLoadedPawns(List<CustomRelationship> relationships, List<CustomPawn> colonists, List<CustomPawn> hiddenPawns) {
            CreateParentChildPawnsForLoadedPawns(colonists, hiddenPawns);
            InitializeParentChildGroupsForLoadedPawns(relationships);
        }

        public void Clear() {
            this.relationships.Clear();
            this.parentChildGroups.Clear();
            Clean();
        }
        
        public IEnumerable<PawnRelationDef> AllowedRelationships {
            get {
                return allowedRelationships;
            }
        }
        
        public IEnumerable<CustomRelationship> Relationships {
            get {
                return relationships;
            }
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
            this.relationships.Add(new CustomRelationship(def, FindInverseRelationship(def), source, target));
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
        
        public void RemoveParentChildGroup(CustomParentChildGroup group) {
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
            List<CustomParentChildGroup> groupsToRemove = new List<CustomParentChildGroup>();
            foreach (var group in parentChildGroups) {
                int index = group.Parents.FindIndex((CustomParentChildPawn p) => { return p.Pawn == pawn; });
                if (index != -1) {
                    group.Parents.RemoveAt(index);
                }
                index = group.Children.FindIndex((CustomParentChildPawn p) => { return p.Pawn == pawn; });
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
            parentChildPawns.RemoveAll((CustomParentChildPawn p) => { return p.Pawn == pawn; });
            dirty = true;
        }
        
    }
}

