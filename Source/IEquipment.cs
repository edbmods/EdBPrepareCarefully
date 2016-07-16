using System;
using Verse;

namespace EdB.PrepareCarefully
{
	public interface IEquipment
	{
		int Count {
			get;
		}

		ThingDef ThingDef {
			get;
		}

		ThingDef StuffDef {
			get;
		}

		Gender Gender {
			get;
		}
	}
}

