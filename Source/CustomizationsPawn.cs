using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully {

    public class CustomizationsSkill {
        public SkillDef SkillDef { get; set; }
        public int Level { get; set; }
        public Passion Passion { get; set; }
        public Passion OriginalPassion { get; set; }
        public int OriginalLevel { get; set; }
    }

    public class CustomizationsTrait {
        public TraitDef TraitDef { get; set; }
        public int Degree { get; set; }
    }

    public class CustomizationTitle {
        public Faction Faction { get; set; }
        public RoyalTitleDef TitleDef { get; set; }
        public int Honor { get; set; }
    }

    public class CustomizationsApparel {
        public ThingDef ThingDef { get; set; }
        public ThingDef StuffDef { get; set; }
        public QualityCategory? Quality { get; set; }
        public Color? Color {  get; set; }
        public float? HitPoints {  get; set; }
    }

    public class CustomizedPossession {
        public ThingDef ThingDef { get; set; }
        public int Count { get; set;}
    }

    public class CustomizedAbility {
        public AbilityDef AbilityDef { get; set; }
    }

    public class CustomizedGenes {
        public List<CustomizedGene> Endogenes { get; set; }
        public List<CustomizedGene> Xenogenes { get; set; }
    }

    public class CustomizedGene {
        public GeneDef GeneDef { get; set; }
    }

    public class CustomizationsPawn {

        public PawnKindDef PawnKind { get; set; }
        public XenotypeDef XenotypeDef { get; set; }
        public bool UniqueXenotype { get; set; }
        public string XenotypeName { get; set; }
        public CustomXenotype CustomXenotype { get; set; }
        public AlienRace AlienRace { get; set; }

        public Gender Gender { get; set; }

        // Name
        public string NameType = "Triple";
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public string SingleName { get; set; }

        // Appearance
        public HairDef Hair { get; set; }
        public Color HairColor { get; set; }
        public HeadTypeDef HeadType { get; set; }
        public BodyTypeDef BodyType { get; set; }
        public BeardDef Beard { get; set; }
        public FurDef Fur { get; set; }

        public TattooDef FaceTattoo { get; set; }
        public TattooDef BodyTattoo { get; set; }

        public Color SkinColor { get; set; }
        public Color? SkinColorOverride { get; set; }

        // Backstory
        public BackstoryDef ChildhoodBackstory { get; set; }
        public BackstoryDef AdulthoodBackstory { get; set; }
        public Color? FavoriteColor { get; set; }

        // Traits

        public List<CustomizationsTrait> Traits { get; set; } = new List<CustomizationsTrait>();

        // Age
        public long BiologicalAgeInTicks { get; set; }
        public long ChronologicalAgeInTicks { get; set; }

        // Skills
        public List<CustomizationsSkill> Skills { get; set; } = new List<CustomizationsSkill>();

        // Genes
        public CustomizedGenes Genes { get; set; } = null;

        // Apparel and Equipment
        public List<CustomizationsApparel> Apparel { get; set; } = new List<CustomizationsApparel>();
        public List<CustomizedPossession> Possessions { get; set; } = new List<CustomizedPossession>();

        // Abilities
        public List<CustomizedAbility> Abilities { get; set; } = new List<CustomizedAbility>();

        // Health
        public List<Injury> Injuries { get; set; } = new List<Injury>();
        public List<Implant> Implants { get; set; } = new List<Implant>();

        public List<CustomizedHediff> BodyParts = new List<CustomizedHediff>();

        // Titles
        public List<CustomizationTitle> Titles { get; set; } = new List<CustomizationTitle>();

        public Ideo Ideo { get; set; }
        public float? Certainty { get; set; }
    }
}
