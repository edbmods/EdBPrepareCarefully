using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public class RelationshipManager
	{
		protected Randomizer randomizer = new Randomizer();
		protected List<PawnRelationDef> allowedRelationships = new List<PawnRelationDef>();
		protected Dictionary<PawnRelationDef, PawnRelationDef> inverseRelationships = new Dictionary<PawnRelationDef, PawnRelationDef>();
		protected Dictionary<CustomPawn, Pawn> facadeToSimulatedPawnMap = new Dictionary<CustomPawn, Pawn>();
		protected Dictionary<Pawn, CustomPawn> simulatedPawnToFacadeMap = new Dictionary<Pawn, CustomPawn>();
		protected List<CustomRelationship> deletionList = new List<CustomRelationship>();
		protected bool dirty = true;

		protected RelationshipList relationships = new RelationshipList();
		protected RelationshipList derivedRelationships = new RelationshipList();

		public RelationshipManager(List<Pawn> originalPawns, List<CustomPawn> correspondingFacades)
		{


			PopulateAllowedRelationships();
			PopulateInverseRelationships();
			InitializeRelationships(originalPawns, correspondingFacades);
			ResetSimulation();
		}

		protected void PopulateAllowedRelationships()
		{
			allowedRelationships.AddRange(DefDatabase<PawnRelationDef>.AllDefs.ToList().FindAll((PawnRelationDef def) => {
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

		protected void PopulateInverseRelationships()
		{
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

		public PawnRelationDef FindInverseRelationship(PawnRelationDef def)
		{
			PawnRelationDef inverse;
			if (inverseRelationships.TryGetValue(def, out inverse)) {
				return inverse;
			}
			else {
				return null;
			}
		}

		public PawnRelationDef ComputeInverseRelationship(PawnRelationDef def)
		{
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

		public PawnRelationWorker FindPawnRelationWorker(PawnRelationDef def)
		{
			CarefullyPawnRelationDef carefullyDef = DefDatabase<CarefullyPawnRelationDef>.GetNamedSilentFail(def.defName);
			if (carefullyDef == null || carefullyDef.workerClass == null) {
				return def.Worker;
			}
			else {
				PawnRelationWorker worker = carefullyDef.Worker;
				if (worker != null) {
					return carefullyDef.Worker;
				}
				else {
					return def.Worker;
				}
			}
		}

		public void InitializeRelationships(List<Pawn> pawns, List<CustomPawn> correspondingFacades)
		{
			// Create a map so that we can lookup pawn facades based on their matching original pawn.
			Dictionary<Pawn, CustomPawn> pawnToFacadeMap = new Dictionary<Pawn, CustomPawn>();
			int pawnCount = pawns.Count;
			for (int i = 0; i < pawns.Count; i++) {
				pawnToFacadeMap.Add(pawns[i], correspondingFacades[i]);
			}

			// Go through each pawn and check for relationships between it and all other pawns.
			foreach (Pawn pawn in pawns) {
				foreach (Pawn other in pawns) {
					if (pawn == other) {
						continue;
					}

					// Find the corresponding pawn facades.
					CustomPawn thisPawnFacade = pawnToFacadeMap[pawn];
					CustomPawn otherPawnFacade = pawnToFacadeMap[other];

					// Go through each relationship between the two pawns.
					foreach (PawnRelationDef def in PawnRelationUtility.GetRelations(pawn, other)) {
						// If no relationship records exists for this relationship, add it.
						if (!relationships.Contains(def, thisPawnFacade, otherPawnFacade)) {
							relationships.Add(new CustomRelationship(def, FindInverseRelationship(def), thisPawnFacade, otherPawnFacade));
						}
					}
				}
			}
		}

		public void Clear()
		{
			this.relationships.Clear();
			this.derivedRelationships.Clear();
			Clean();
		}

		protected void ResetSimulation() {
			facadeToSimulatedPawnMap.Clear();
			simulatedPawnToFacadeMap.Clear();
			foreach (CustomPawn customPawn in PrepareCarefully.Instance.Pawns) {
				Pawn simulatedPawn = randomizer.GenerateColonist();
				simulatedPawn.relations.ClearAllRelations();
				simulatedPawn.Name = new NameTriple(customPawn.Name.First, customPawn.Name.Nick, customPawn.Name.Last);
				facadeToSimulatedPawnMap.Add(customPawn, simulatedPawn);
				simulatedPawnToFacadeMap.Add(simulatedPawn, customPawn);
			}

			AddAllRelationshipsToSimulation(false);
			AddAllRelationshipsToSimulation(true);

			dirty = true;
		}

		protected void AddAllRelationshipsToSimulation(bool implied)
		{
			PawnGenerationRequest request = new PawnGenerationRequest();
			foreach (CustomRelationship r in relationships) {
				if (r.def.implied == implied) {
					if (r.def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null) {
						continue;
					}
					Pawn simulatedSource = facadeToSimulatedPawnMap[r.source];
					Pawn simulatedTarget = facadeToSimulatedPawnMap[r.target];
					FindPawnRelationWorker(r.def).CreateRelation(simulatedSource, simulatedTarget, ref request);
				}
			}
		}

		public IEnumerable<PawnRelationDef> AllowedRelationships {
			get {
				return allowedRelationships;
			}
		}

		public IEnumerable<CustomRelationship> ExplicitRelationships {
			get {
				return relationships;
			}
		}

		public IEnumerable<CustomRelationship> AllRelationships {
			get {
				return relationships.Concat(DerivedRelationships);
			}
		}

		public RelationshipList DerivedRelationships {
			get {
				if (dirty) {
					Clean();
				}
				return derivedRelationships;
			}
		}

		protected void Clean() {
			DeleteRelationships();
			ResetSimulation();
			RecreateDerivedRelationships();
			ResetPawnRelationshipsFlag();
			dirty = false;
		}

		protected void ResetPawnRelationshipsFlag()
		{
			foreach (var p in PrepareCarefully.Instance.Pawns) {
				p.HasRelationships = false;
			}
			foreach (var r in relationships) {
				r.source.HasRelationships = true;
				r.target.HasRelationships = true;
			}
			foreach (var r in derivedRelationships) {
				r.source.HasRelationships = true;
				r.target.HasRelationships = true;
			}
		}

		protected void DeleteRelationships()
		{
			foreach (var r in deletionList) {
				relationships.Remove(r);
			}
			deletionList.Clear();
		}

		public void RecreateDerivedRelationships()
		{
			derivedRelationships.Clear();

			// Go through each pawn and check for relationships between it and all other pawns.
			foreach (Pawn pawn in simulatedPawnToFacadeMap.Keys) {
				foreach (Pawn other in simulatedPawnToFacadeMap.Keys) {
					if (pawn == other) {
						continue;
					}

					// Find the corresponding pawn facades.
					CustomPawn thisPawnFacade = simulatedPawnToFacadeMap[pawn];
					CustomPawn otherPawnFacade = simulatedPawnToFacadeMap[other];

					// Go through each relationship between the two pawns.
					foreach (PawnRelationDef def in PawnRelationUtility.GetRelations(pawn, other)) {
						// Don't add a derived relationship if we don't know what it's inverse is.
						PawnRelationDef inverse = FindInverseRelationship(def);
						if (inverse == null) {
							Log.Warning("Did not add derived relationship.  Could not determine inverse relationship.");
							continue;
						}
						// If no explicit relationship records exists for this relationship, add it as a derived relationship.
						if (!relationships.Contains(def, thisPawnFacade, otherPawnFacade)
							&& !derivedRelationships.Contains(def, thisPawnFacade, otherPawnFacade)) {
							derivedRelationships.Add(new CustomRelationship(def, inverse, thisPawnFacade, otherPawnFacade, false));
						}
					}
				}
			}
		}

		public void AddRelationship(PawnRelationDef def, CustomPawn source, CustomPawn target) {
			if (def.workerClass.GetMethod("CreateRelation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null) {
				return;
			}
			this.relationships.Add(new CustomRelationship(def, FindInverseRelationship(def), source, target));
			dirty = true;
		}

		public void DeleteRelationship(PawnRelationDef def, CustomPawn source, CustomPawn target) {
			CustomRelationship toRemove = relationships.Find(def, source, target);
			if (toRemove != null) {
				deletionList.Add(toRemove);
			}
			dirty = true;
		}

		public void DeletePawnRelationships(CustomPawn pawn) {
			List<CustomRelationship> toDelete = new List<CustomRelationship>();
			foreach (var r in relationships) {
				if (r.source == pawn || r.target == pawn) {
					deletionList.Add(r);
				}
			}
			dirty = true;
		}
	}
}

