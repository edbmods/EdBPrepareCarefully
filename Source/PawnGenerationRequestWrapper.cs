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
        private Gender? fixedGender = null;
        private bool worldPawnFactionDoesntMatter = false;
        public PawnGenerationRequestWrapper() {
        }
        private PawnGenerationRequest CreateRequest() {
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
                false, //bool mustBeCapableOfViolence = false,
                0f, //float colonistRelationChanceFactor = 1f,
                false, //bool forceAddFreeWarmLayerIfNeeded = false,
                true, //bool allowGay = true,
                false, //bool allowFood = true,
                false, // bool inhabitant = false
                false, // bool certainlyBeenInCryptosleep = false
                false, // bool forceRedressWorldPawnIfFormerColonist = false
                worldPawnFactionDoesntMatter, // bool worldPawnFactionDoesntMatter = false
                null, // Predicate < Pawn > validatorPreGear = null
                null, // Predicate < Pawn > validatorPostGear = null
                null, // float ? minChanceToRedressWorldPawn = null
                fixedBiologicalAge, // float ? fixedBiologicalAge = null
                null, // float ? fixedChronologicalAge = null
                fixedGender, // Gender ? fixedGender = null
                null, // float ? fixedMelanin = null
                null // string fixedLastName = null
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
        public Gender? FixedGender {
            set {
                fixedGender = value;
            }
        }
    }
}
