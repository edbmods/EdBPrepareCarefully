using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace EdB.PrepareCarefully
{
	public class StandardEquipment : IEquipment
	{
		public ThingDef thingDef;
		public ThingDef stuffDef;
		public Gender gender = Gender.None;

		protected int numStacks = -1;
		protected int minNumStacks = -1;
		protected int maxNumStacks = -1;

		protected int countPerStack = -1;
		protected int minCountPerStack = -1;
		protected int maxCountPerStack = -1;

		public StandardEquipment()
		{
		}

		public StandardEquipment(ThingDef def, int count)
		{
			this.thingDef = def;
			this.NumStacks = 1;
			this.CountPerStack = count;
		}

		public StandardEquipment(ThingDef def, ThingDef stuffDef, int count)
		{
			this.thingDef = def;
			this.stuffDef = stuffDef;
			this.NumStacks = 1;
			this.CountPerStack = count;
		}

		public StandardEquipment(ThingDef def, ThingDef stuffDef, int numStacks, int minCountPerStack, int maxCountPerStack)
		{
			this.thingDef = def;
			this.stuffDef = stuffDef;
			this.NumStacks = numStacks;
			this.minCountPerStack = minCountPerStack;
			this.maxCountPerStack = maxCountPerStack;
		}

		public StandardEquipment(ThingDef def, ThingDef stuffDef, int minNumStacks, int maxNumStacks, int minCountPerStack, int maxCountPerStack)
		{
			this.thingDef = def;
			this.stuffDef = stuffDef;
			this.minNumStacks = minNumStacks;
			this.maxNumStacks = maxNumStacks;
			this.minCountPerStack = minCountPerStack;
			this.maxCountPerStack = maxCountPerStack;
		}

		public ThingDef ThingDef
		{
			get {
				return thingDef;
			}
		}

		public ThingDef StuffDef
		{
			get {
				return stuffDef;
			}
		}

		public Gender Gender
		{
			get {
				return gender;
			}
		}

		public int Count
		{
			get {
				int stacks;
				if (numStacks > -1) {
					stacks = numStacks;
				}
				else {
					stacks = new IntRange(minNumStacks, maxNumStacks).RandomInRange;
				}
				if (countPerStack > -1) {
					return stacks * countPerStack;
				}

				int count = 0;
				IntRange range = new IntRange(minCountPerStack, maxCountPerStack);
				for (int i = 0; i < numStacks; i++) {
					count += range.RandomInRange;
				}
				return count;
			}
		}

		public int NumStacks
		{
			get {
				return numStacks;
			}
			set {
				numStacks = minNumStacks = maxNumStacks = value;
			}
		}

		public int MinNumStacks
		{
			get {
				return minNumStacks;
			}
			set {
				minNumStacks = value;
			}
		}
			
		public int MaxNumStacks
		{
			get {
				return maxNumStacks;
			}
			set {
				maxNumStacks = value;
			}
		}

		public int CountPerStack
		{
			get {
				return countPerStack;
			}
			set {
				countPerStack = minCountPerStack = maxCountPerStack = value;
			}
		}

		public int MinCountPerStack
		{
			get {
				return minCountPerStack;
			}
			set {
				minCountPerStack = value;
			}
		}

		public int MaxCountPerStack
		{
			get {
				return maxCountPerStack;
			}
			set {
				maxCountPerStack = value;
			}
		}

	}
}

