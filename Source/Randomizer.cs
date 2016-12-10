using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Reflection;

namespace EdB.PrepareCarefully
{
	public class Randomizer
	{
		public Pawn GenerateColonist()
		{
			PawnKindDef kindDef = Faction.OfPlayer.def.basicMemberKind;
			Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kindDef, Faction.OfPlayer,
					PawnGenerationContext.PlayerStarter, null, true, false, false, false, false, false, 0f, false, true,
			        false, null, null, null, null, null, null));
			return pawn;
		}

		public void RandomizeAll(CustomPawn customPawn)
		{
			Pawn pawn = GenerateColonist();
			customPawn.InitializeWithPawn(pawn);
		}

		public void RandomizeBackstory(CustomPawn customPawn)
		{
			MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("SetBackstoryInSlot", BindingFlags.Static | BindingFlags.NonPublic);
			object[] arguments = new object[] { customPawn.Pawn, BackstorySlot.Childhood, null, Faction.OfPlayer.def };
			method.Invoke(null, arguments);
			customPawn.Childhood = arguments[2] as Backstory;
			arguments = new object[] { customPawn.Pawn, BackstorySlot.Adulthood, null, Faction.OfPlayer.def };
			method.Invoke(null, arguments);
			customPawn.Adulthood = arguments[2] as Backstory;
		}

		public static Backstory RandomAdulthood(CustomPawn customPawn)
		{
			MethodInfo method = typeof(PawnBioAndNameGenerator).GetMethod("SetBackstoryInSlot", BindingFlags.Static | BindingFlags.NonPublic);
			object[] arguments = new object[] { customPawn.Pawn, BackstorySlot.Adulthood, null, Faction.OfPlayer.def };
			method.Invoke(null, arguments);
			Backstory result = arguments[2] as Backstory;
			return result;
		}

		public void RandomizeTraits(CustomPawn customPawn)
		{
			Pawn pawn = GenerateColonist();
			List<Trait> traits = pawn.story.traits.allTraits;
			if (traits.Count > 0) {
				customPawn.SetTrait(0, traits[0]);
			}
			else {
				customPawn.SetTrait(0, null);
			}
			if (traits.Count > 1 && customPawn.GetTrait(0) != traits[1]) {
				customPawn.SetTrait(1, traits[1]);
			}
			else {
				customPawn.SetTrait(1, null);
			}
			if (traits.Count > 2 && customPawn.GetTrait(0) != traits[2] && customPawn.GetTrait(1) != traits[2]) {
				customPawn.SetTrait(2, traits[2]);
			}
			else {
				customPawn.SetTrait(2, null);
			}
		}

		public void RandomizeAppearance(CustomPawn customPawn)
		{
			Pawn pawn;
			int tries = 0;
			do {
				pawn = GenerateColonist();
				tries++;
			}
			while (pawn.gender != customPawn.Gender && tries < 1000);

			customPawn.HairDef = pawn.story.hairDef;
			customPawn.SetColor(PawnLayers.Hair, pawn.story.hairColor);
			customPawn.HeadGraphicPath = pawn.story.HeadGraphicPath;
			customPawn.MelaninLevel = pawn.story.melanin;

			for (int i = 0; i < PawnLayers.Count; i++) {
				if (PawnLayers.IsApparelLayer(i)) {
					customPawn.SetSelectedStuff(i, null);
					customPawn.SetSelectedApparel(i, null);
				}
			}
			foreach (Apparel current in pawn.apparel.WornApparel) {
				int layer = PawnLayers.ToPawnLayerIndex(current.def.apparel);
				if (layer != -1) {
					customPawn.SetSelectedStuff(layer, current.Stuff);
					customPawn.SetSelectedApparel(layer, current.def);
				}
			}
		}

		public void RandomizeName(CustomPawn customPawn)
		{
			Pawn pawn = GenerateColonist();
			pawn.gender = customPawn.Gender;
			//Name name = NameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
			Name name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null);
			NameTriple nameTriple = name as NameTriple;
			customPawn.Name = nameTriple;
		}
	}
}

