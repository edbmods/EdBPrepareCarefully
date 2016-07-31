using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace EdB.PrepareCarefully
{
	public enum SortField
	{
		Name,
		Cost
	}

	public enum SortOrder
	{
		Ascending,
		Descending
	}

	public class PrepareCarefully
	{
		protected static PrepareCarefully instance = null;
		public static PrepareCarefully Instance {
			get {
				if (instance == null) {
					instance = new PrepareCarefully();
				}
				return instance;
			}
		}

		public static void RemoveInstance() {
			instance = null;
		}

		protected EquipmentDatabase equipmentDatabase = null;
		protected CostCalculator costCalculator = null;

		protected List<CustomPawn> pawns = new List<CustomPawn>();
		protected List<SelectedEquipment> equipment = new List<SelectedEquipment>();
		protected List<SelectedEquipment> removals = new List<SelectedEquipment>();
		protected bool active = false;
		protected string filename = "";
		protected ImplantManager implantManager;
		protected RelationshipManager relationshipManager;
		protected HealthManager healthManager = new HealthManager();

		public Page_ConfigureStartingPawns OriginalPage = null;

		public void NextPage()
		{
			if (OriginalPage != null) {
				typeof(Page_ConfigureStartingPawns).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(OriginalPage, null);
			}
		}

		protected Configuration config = new Configuration();
		public Configuration Config
		{
			get {
				return config;
			}
			set {
				config = value;
			}
		}

		protected State state = new State();
		public State State {
			get {
				return state;
			}
		}

		public RelationshipManager RelationshipManager {
			get {
				return relationshipManager;
			}
		}

		public HealthManager HealthManager {
			get {
				return healthManager;
			}
		}

		public SortField SortField { get; set; }
		public SortOrder NameSortOrder { get; set; }
		public SortOrder CostSortOrder { get; set; }
		public int StartingPoints { get; set; }

		public int PointsRemaining {
			get {
				if (config.fixedPointsEnabled) {
					return config.points - (int) Cost.total;
				}
				else {
					return StartingPoints - (int) Cost.total;
				}
			}
		}

		public PrepareCarefully() {
			implantManager = new ImplantManager();
			NameSortOrder = SortOrder.Ascending;
			CostSortOrder = SortOrder.Ascending;
			SortField = SortField.Name;
		}

		public void Configure(object o)
		{
			config = new Configuration();
			if (o == null) {
				return;
			}
			CopyConfiguration("minColonists", o, 0);
			CopyConfiguration("maxColonists", o, 0);
			CopyConfiguration("points", o, -1);
		}

		protected void CopyConfiguration(string fieldName, object o, object ignoreValue)
		{
			FieldInfo sourceField = o.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (sourceField == null) {
				return;
			}
			FieldInfo destField = typeof(Configuration).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (destField == null) {
				Log.Warning("Invalid Prepare Carefully configuration: " + fieldName);
				return;
			}
			try {
				object value = sourceField.GetValue(o);
				if (object.Equals(value, ignoreValue)) {
					return;
				}
				else {
					destField.SetValue(this.config, value);
				}
			}
			catch (Exception e) {
				Log.Warning("Failed to set configuration in Prepare Carefully for " + fieldName);
				Log.Warning(e.ToString());
			}
		}

		public void Clear()
		{
			this.Active = false;
			this.equipmentDatabase = new EquipmentDatabase();
			this.costCalculator = new CostCalculator();
			this.pawns.Clear();
			this.equipment.Clear();
		}

		protected Dictionary<CustomPawn, Pawn> facadeToPawnMap = new Dictionary<CustomPawn, Pawn>();
		protected Dictionary<Pawn, CustomPawn> pawnToFacadeMap = new Dictionary<Pawn, CustomPawn>();

		public void Initialize()
		{
			Clear();
			InitializePawns();
			InitializeRelationshipManager(this.pawns);
			InitializeDefaultEquipment();

			this.StartingPoints = (int) this.Cost.total;

			this.state = new State();
		}

		// TODO: Alpha 14
		// Think about whether or not this is the best approach.  Might need to do a bug report for the vanilla game?
		// The tribal scenario adds a weapon with an invalid thing/stuff combination (jade knife).  The 
		// knife ThingDef should allow the jade material, but it does not.  We need this workaround to
		// add the normally disallowed equipment to our equipment database.
		protected EquipmentDatabaseEntry AddNonStandardScenarioEquipmentEntry(EquipmentKey key)
		{
			int type = equipmentDatabase.ClassifyThingDef(key.thingDef);
			return equipmentDatabase.AddThingDefWithStuff(key.thingDef, key.stuffDef, type);
		}

		protected void InitializeDefaultEquipment()
		{
			// Go through all of the scenario steps that scatter resources near the player starting location and add
			// them to the resource/equipment list.
			foreach (ScenPart part in Verse.Find.Scenario.AllParts) {
				ScenPart_ScatterThingsNearPlayerStart nearPlayerStart = part as ScenPart_ScatterThingsNearPlayerStart;
				if (nearPlayerStart != null) {
					FieldInfo thingDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
					FieldInfo stuffDefField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
					FieldInfo countField = typeof(ScenPart_ScatterThingsNearPlayerStart).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
					ThingDef thingDef = (ThingDef)thingDefField.GetValue(nearPlayerStart);
					ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(nearPlayerStart);
					int count = (int)countField.GetValue(nearPlayerStart);
					EquipmentKey key = new EquipmentKey(thingDef, stuffDef);
					EquipmentDatabaseEntry entry = equipmentDatabase[key];
					if (entry == null) {
						entry = AddNonStandardScenarioEquipmentEntry(key);
					}
					if (entry != null) {
						AddEquipment(entry, count);
					}
				}

				// Go through all of the scenario steps that place starting equipment with the colonists and
				// add them to the resource/equipment list.
				ScenPart_StartingThing_Defined startingThing = part as ScenPart_StartingThing_Defined;
				if (startingThing != null) {
					FieldInfo thingDefField = typeof(ScenPart_StartingThing_Defined).GetField("thingDef", BindingFlags.Instance | BindingFlags.NonPublic);
					FieldInfo stuffDefField = typeof(ScenPart_StartingThing_Defined).GetField("stuff", BindingFlags.Instance | BindingFlags.NonPublic);
					FieldInfo countField = typeof(ScenPart_StartingThing_Defined).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
					ThingDef thingDef = (ThingDef)thingDefField.GetValue(startingThing);
					ThingDef stuffDef = (ThingDef)stuffDefField.GetValue(startingThing);
					int count = (int)countField.GetValue(startingThing);
					EquipmentKey key = new EquipmentKey(thingDef, stuffDef);
					EquipmentDatabaseEntry entry = equipmentDatabase[key];
					if (entry == null) {
						entry = AddNonStandardScenarioEquipmentEntry(key);
					}
					if (entry != null) {
						AddEquipment(entry, count);
					}
				}

				// Go through all of the scenario steps that spawn a pet and add the pet to the equipment/resource
				// list.
				ScenPart_StartingAnimal animal = part as ScenPart_StartingAnimal;
				if (animal != null) {
					FieldInfo animalCountField = typeof(ScenPart_StartingAnimal).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic);
					int count = (int)animalCountField.GetValue(animal);
					for (int i = 0; i < count; i++) {
						AddEquipment(RandomPet(animal));
					}
				}
			}
		}

		private static EquipmentDatabaseEntry RandomPet(ScenPart_StartingAnimal startingAnimal)
		{
			FieldInfo animalKindField = typeof(ScenPart_StartingAnimal).GetField("animalKind", BindingFlags.Instance | BindingFlags.NonPublic);
			MethodInfo randomPetsMethod = typeof(ScenPart_StartingAnimal).GetMethod("RandomPets", BindingFlags.Instance | BindingFlags.NonPublic);

			PawnKindDef animalKindDef = (PawnKindDef)animalKindField.GetValue(startingAnimal);
			if (animalKindDef == null) {
				IEnumerable<PawnKindDef> animalKindDefs = (IEnumerable<PawnKindDef>)randomPetsMethod.Invoke(startingAnimal, null);
				animalKindDef = animalKindDefs.RandomElementByWeight((PawnKindDef td) => td.RaceProps.petness);
			}

			List<EquipmentDatabaseEntry> entries = PrepareCarefully.Instance.EquipmentEntries.Animals.FindAll((EquipmentDatabaseEntry e) => {
				return e.def == animalKindDef.race;
			});
			if (entries.Count > 0) {
				EquipmentDatabaseEntry entry = entries.RandomElement();
				return entry;
			}
			else {
				return null;
			}
		}

		public bool Active
		{
			get {
				return active;
			}
			set {
				active = value;
			}
		}
		public string Filename
		{
			get {
				return filename;
			}
			set {
				filename = value;
			}
		}
		public void ClearPawns()
		{
			pawns.Clear();
		}
		public void AddPawn(CustomPawn customPawn)
		{
			pawns.Add(customPawn);
		}
		public void RemovePawn(CustomPawn customPawn)
		{
			pawns.Remove(customPawn);
		}
		public List<CustomPawn> Pawns {
			get {
				return pawns;
			}
		}
		public EquipmentDatabase EquipmentEntries
		{
			get {
				if (equipmentDatabase == null) {
					equipmentDatabase = new EquipmentDatabase();
				}
				return equipmentDatabase;
			}
		}

		protected List<Pawn> colonists = new List<Pawn>();

		public void CreateColonists()
		{
			colonists.Clear();
			foreach (CustomPawn customPawn in pawns) {
				colonists.Add(customPawn.ConvertToPawn(false));
			}

			Dictionary<CustomPawn, Pawn> pawnLookup = new Dictionary<CustomPawn, Pawn>();

			for (int i = 0; i < pawns.Count; i++) {
				CustomPawn customPawn = pawns[i];
				Pawn pawn = colonists[i];
				pawnLookup[customPawn] = pawn;
			}

			// Add relationships in two passes.  Add the non-implied relationships first and then the implied
			// relationships second.  This tends to result in less weirdness.
			AddRelationships(pawnLookup, RelationshipManager.ExplicitRelationships.Where((CustomRelationship r) => {
				return r.def.implied == false;
			}));
			AddRelationships(pawnLookup, RelationshipManager.ExplicitRelationships.Where((CustomRelationship r) => {
				return r.def.implied == true;
			}));
		}

		protected void AddRelationships(Dictionary<CustomPawn, Pawn> lookup, IEnumerable<CustomRelationship> relationships)
		{
			PawnGenerationRequest request = new PawnGenerationRequest();
			foreach (CustomRelationship r in RelationshipManager.ExplicitRelationships) {
				PawnRelationWorker worker = this.relationshipManager.FindPawnRelationWorker(r.def);
				if (worker.GetType().GetMethod("CreateRelation",
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) == null)
				{
					continue;
				}
				Pawn source = null;
				Pawn target = null;
				if (!lookup.TryGetValue(r.source, out source)) {
					Log.Warning("Prepare Carefully could not find source pawn when trying to create relationship (" + r.def.defName + "): " + r.source.Name);
					continue;
				}
				if (!lookup.TryGetValue(r.target, out target)) {
					Log.Warning("Prepare Carefully could not find target pawn when trying to create relationship (" + r.def.defName + "): " + r.target.Name);
					continue;
				}

				// TODO: If either of those lookups are failing, we're not tracking pawns properly
				// somewhere.  I've seen those lookups fail after deleting colonists (but not
				// consistently).  Should track down the root cause.

				worker.CreateRelation(source, target, ref request);
			}
		}

		public List<Pawn> Colonists {
			get {
				return colonists;
			}
		}

		public void ReplaceColonists()
		{
			List<Pawn> result = new List<Pawn>();
			foreach (CustomPawn customPawn in pawns) {
				result.Add(customPawn.ConvertToPawn());
			}
			Verse.Find.GameInitData.startingPawns = result;
		}

		public List<SelectedEquipment> Equipment {
			get {
				SyncRemovals();
				return equipment;
			}
		}
			
		public bool AddEquipment(EquipmentDatabaseEntry entry)
		{
			SelectedEquipment e = Find(entry);
			if (e == null) {
				equipment.Add(new SelectedEquipment(entry));
				return true;
			}
			else {
				e.count += entry.stackSize;
				return false;
			}
		}

		public bool AddEquipment(EquipmentDatabaseEntry entry, int count)
		{
			SelectedEquipment e = Find(entry);
			if (e == null) {
				equipment.Add(new SelectedEquipment(entry, count));
				return true;
			}
			else {
				e.count += count;
				return false;
			}
		}

		public void RemoveEquipment(SelectedEquipment equipment)
		{
			removals.Add(equipment);
		}

		public void RemoveEquipment(EquipmentDatabaseEntry entry)
		{
			SelectedEquipment e = Find(entry);
			if (e != null) {
				removals.Add(e);
			}
		}

		protected void SyncRemovals()
		{
			if (removals.Count > 0) {
				foreach (var e in removals) {
					equipment.Remove(e);
				}
				removals.Clear();
			}
		}

		public SelectedEquipment Find(EquipmentDatabaseEntry entry)
		{
			return equipment.Find((SelectedEquipment e) => {
				return e.def == entry.def && e.stuffDef == entry.stuffDef && e.gender == entry.gender;
			});
		}

		CostDetails cost = new CostDetails();
		public CostDetails Cost
		{
			get {
				if (costCalculator == null) {
					costCalculator = new CostCalculator();
				}
				costCalculator.Calculate(cost, this.pawns, this.equipment);
				return cost;
			}
		}

		public void InitializePawns()
		{
			this.facadeToPawnMap.Clear();
			this.pawnToFacadeMap.Clear();
			foreach (Pawn p in Verse.Find.GameInitData.startingPawns) {
				CustomPawn f = new CustomPawn(p);
				facadeToPawnMap.Add(f, p);
				pawnToFacadeMap.Add(p, f);
				this.pawns.Add(f);
				healthManager.InjuryManager.InitializePawnInjuries(p, f);
			}
		}
			
		public void InitializeRelationshipManager(List<CustomPawn> pawns)
		{
			List<CustomPawn> facades = new List<CustomPawn>();
			foreach (Pawn pawn in Verse.Find.GameInitData.startingPawns) {
				facades.Add(pawnToFacadeMap[pawn]);
			}
			relationshipManager = new RelationshipManager(Verse.Find.GameInitData.startingPawns, facades);
		}

		public bool FindScenPart()
		{
			if (DefDatabase<MapGeneratorDef>.AllDefs.Count() == 1) {
				MapGeneratorDef def = DefDatabase<MapGeneratorDef>.AllDefs.First();
				if (def != null) {
					foreach (var g in def.genSteps) {
						if (g.GetType().FullName.Equals("EdB.PrepareCarefully.Genstep_ScenParts")) {
							return true;
						}
					}
					return false;
				}
			}
			// TODO: We can't figure this out in every situation.  If there's more than one
			// map generator, the game is going to pick one at random, and we can't know at this
			// point which one it's going to pick.  In that case, we'll assume that everything is
			// good.
			return true;
		}
	}
}

