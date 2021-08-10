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
    class PawnGenerationRequestWrapper {
        private PawnKindDef kindDef = Faction.OfPlayer.def.basicMemberKind;
        private Faction faction = Faction.OfPlayer;
        private PawnGenerationContext context = PawnGenerationContext.PlayerStarter;
        private float? fixedBiologicalAge = null;
        private float? fixedChronologicalAge = null;
        private Gender? fixedGender = null;
        private bool worldPawnFactionDoesntMatter = false;
        private bool mustBeCapableOfViolence = false;
        private Ideo fixedIdeology = null;
        public PawnGenerationRequestWrapper() {
        }
        private PawnGenerationRequest CreateRequest() {
            //public PawnGenerationRequest (

            //string fixedBirthName = null, 
            //RoyalTitleDef fixedTitle = null)

            /*
             * PawnKindDef kind
             * Faction faction = null
             * PawnGenerationContext context = PawnGenerationContext.NonPlayer
             * int tile = -1
             * bool forceGenerateNewPawn = false
             * bool newborn = false
             * bool allowDead = false
             * bool allowDowned = false
             * bool canGeneratePawnRelations = true
             * bool mustBeCapableOfViolence = false
             * float colonistRelationChanceFactor = 1
             * bool forceAddFreeWarmLayerIfNeeded = false
             * bool allowGay = true
             * bool allowFood = true
             * bool allowAddictions = true
             * bool inhabitant = false
             * bool certainlyBeenInCryptosleep = false
             * bool forceRedressWorldPawnIfFormerColonist = false
             * bool worldPawnFactionDoesntMatter = false
             * float biocodeWeaponChance = 0
             * float biocodeApparelChance = 0
             * Pawn extraPawnForExtraRelationChance = null
             * float relationWithExtraPawnChanceFactor = 1
             * Predicate<Pawn> validatorPreGear = null
             * Predicate<Pawn> validatorPostGear = null
             * IEnumerable<TraitDef> forcedTraits = null
             * IEnumerable<TraitDef> prohibitedTraits = null
             * float? minChanceToRedressWorldPawn = null
             * float? fixedBiologicalAge = null
             * float? fixedChronologicalAge = null
             * Gender? fixedGender = null
             * float? fixedMelanin = null
             * string fixedLastName = null
             * string fixedBirthName = null
             * RoyalTitleDef fixedTitle = null
             * Ideo fixedIdeo = null
             * bool forceNoIdeo = false
             * bool forceNoBackstory = false);
             */

            return new PawnGenerationRequest(
                kindDef, // PawnKindDef kind
                faction, // Faction faction = null
                context, // PawnGenerationContext context = PawnGenerationContext.NonPlayer
                -1, //int tile = -1,
                true, //bool forceGenerateNewPawn = false,
                false, //bool newborn = false,
                false, //bool allowDead = false,
                false, //bool allowDowned = false,
                false, //bool canGeneratePawnRelations = true,
                mustBeCapableOfViolence, //bool mustBeCapableOfViolence = false,
                0f, //float colonistRelationChanceFactor = 1f,
                false, //bool forceAddFreeWarmLayerIfNeeded = false,
                true, //bool allowGay = true,
                false, //bool allowFood = true,
                false, //bool allowAddictions = true, 
                false, // bool inhabitant = false
                false, // bool certainlyBeenInCryptosleep = false
                false, // bool forceRedressWorldPawnIfFormerColonist = false
                worldPawnFactionDoesntMatter, // bool worldPawnFactionDoesntMatter = false
                0f, //float biocodeWeaponChance = 0f, 
                0f, //float biocodeApparelChance = 0f,
                null, //Pawn extraPawnForExtraRelationChance = null, 
                1f, //float relationWithExtraPawnChanceFactor = 1f, 
                null, // Predicate < Pawn > validatorPreGear = null
                null, // Predicate < Pawn > validatorPostGear = null
                Enumerable.Empty<TraitDef>(), //IEnumerable<TraitDef> forcedTraits = null, 
                Enumerable.Empty<TraitDef>(), //IEnumerable<TraitDef> prohibitedTraits = null,
                null, // float ? minChanceToRedressWorldPawn = null
                fixedBiologicalAge, // float ? fixedBiologicalAge = null
                fixedChronologicalAge, // float ? fixedChronologicalAge = null
                fixedGender, // Gender ? fixedGender = null
                null, // float ? fixedMelanin = null
                null, // string fixedLastName = null
                null, //string fixedBirthName = null, 
                null, //RoyalTitleDef fixedTitle = null
                fixedIdeology, //Ideo fixedIdeo = null
                false, //bool forceNoIdeo = false
                false //bool forceNoBackstory = false
            );
        }
        public PawnGenerationRequest Request {
            get {
                return CreateRequest();
            }
        }
        public PawnKindDef KindDef {
            set {
                kindDef = value;
            }
        }
        public Faction Faction {
            set {
                faction = value;
            }
        }
        public PawnGenerationContext Context {
            set {
                context = value;
            }
        }
        public bool WorldPawnFactionDoesntMatter {
            set {
                worldPawnFactionDoesntMatter = value;
            }
        }
        public float? FixedBiologicalAge {
            set {
                fixedBiologicalAge = value;
            }
        }
        public float? FixedChronologicalAge {
            set {
                fixedChronologicalAge = value;
            }
        }
        public Gender? FixedGender {
            set {
                fixedGender = value;
            }
        }
        public bool MustBeCapableOfViolence {
            set {
                mustBeCapableOfViolence = value;
            }
        }
        public Ideo FixedIdeology {
            set {
                fixedIdeology = value;
            }
        }
    }
}
