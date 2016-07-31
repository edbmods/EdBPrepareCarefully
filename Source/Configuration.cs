using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Configuration
	{
		public bool showPoints = true;
		public int points = 12000;
		public int minColonists = 1;
		public int maxColonists = 12;
		public Nullable<int> hardMaxColonists = null;
		public bool pointsEnabled = false;
		public bool fixedPointsEnabled = false;
	}
}

