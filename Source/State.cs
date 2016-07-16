using System;
using System.Collections.Generic;
using System.Linq;

namespace EdB.PrepareCarefully
{
	public class State
	{
		protected int currentPawnIndex;

		public int CurrentPawnIndex {
			get {
				return currentPawnIndex;
			}
			set {
				currentPawnIndex = value;
			}
		}

		public CustomPawn CurrentPawn {
			get {
				return PrepareCarefully.Instance.Pawns[currentPawnIndex];
			}
		}
	}
}

