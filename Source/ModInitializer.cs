
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	// This class gets instantiated 
	public class ModInitializer : ITab
    {
        protected GameObject gameObject = null;

		public ModInitializer() : base()
        {
			LongEventHandler.ExecuteWhenFinished(() => {
				Log.Message("Initialized the EdB Prepare Carefully mod");
				gameObject = new GameObject("EdBPrepareCarefullyController");
				gameObject.AddComponent<ModController>();
				MonoBehaviour.DontDestroyOnLoad(gameObject);
			});
        }

		protected override void FillTab() {
			return;
		}
    }
}
