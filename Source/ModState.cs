using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace EdB.PrepareCarefully {
    public class ModState {
        public Page_ConfigureStartingPawns OriginalPage { get; set; }
        public List<string> MissingWorkTypes {  get; set; } = new List<string>();

        public int ColonyPawnCount { get; set; }
        public int TotalPawnCount { get; set; }
        public int WorldPawnCount => TotalPawnCount - ColonyPawnCount;

        public Customizations Customizations { get; set; } = new Customizations();
        public List<ScenPart> OriginalScenarioParts { get; set; }
        public HashSet<ScenPart> ReplacedScenarioParts { get; set; } = new HashSet<ScenPart>();
        public CostDetailsRefactored PointCost { get; set; } = new CostDetailsRefactored();


        public int StartingPoints { get; set; } = 0;
        public Dictionary<CustomizedPawn, Dictionary<SkillDef, int>> CachedSkillGains { get; set; } = new Dictionary<CustomizedPawn, Dictionary<SkillDef, int>>();

        public Dictionary<Pawn, CustomizationsPawn> OriginalPawnCustomizations;

    }
}
