using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class EquipmentDatabaseEntry
	{
		public static readonly int TypeUncategorized = 0;
		public static readonly int TypeResource = 1;
		public static readonly int TypeFood = 2;
		public static readonly int TypeWeapon = 3;
		public static readonly int TypeApparel = 4;
		public static readonly int TypeAnimal = 5;
		public static readonly int TypeMedical = 6;
		public static readonly int TypeBuilding = 7;

		public ThingDef def;
		public ThingDef stuffDef = null;
		public Gender gender = Gender.None;
		public Thing thing = null;
		public int type;
		public int stackSize;
		public double cost = 0;
		public Color color = Color.white;
		public bool stacks = true;
		public bool gear = false;
		public bool animal = false;
		protected string label = null;
		public bool hideFromPortrait = false;

		public bool Minifiable {
			get {
				return def.Minifiable && def.building != null;
			}
		}

		public string Label {
			get {
				if (label == null) {
					if (thing != null && animal == true) {
						return LabelForAnimal;
					}
					else {
						return GenLabel.ThingLabel(def, stuffDef, stackSize).CapitalizeFirst();
					}
				}
				else {
					return label;
				}
			}
		}

		public string LabelNoCount {
			get {
				if (label == null) {
					if (thing != null && animal == true) {
						return LabelForAnimal;
					}
					else {
						return GenLabel.ThingLabel(def, stuffDef, 1).CapitalizeFirst();
					}
				}
				else {
					return label;
				}
			}
		}

		public string LabelForAnimal {
			get {
				Pawn pawn = thing as Pawn;
				return "PawnMainDescGendered".Translate(new object[] {
					pawn.gender.GetLabel(),
					pawn.kindDef.label
				}).CapitalizeFirst();
			}
		}

		public EquipmentKey EquipmentKey {
			get {
				return new EquipmentKey(def, stuffDef, gender);
			}
		}

		public override string ToString()
		{
			return string.Format("[EquipmentDatabaseEntry: def = {0}, stuffDef = {1}]", (def != null ? def.defName : "null"), (stuffDef != null ? stuffDef.defName : "null"));
		}
	}



}

