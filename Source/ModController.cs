using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
    class ModController : UnityEngine.MonoBehaviour
    {
		public static readonly string ModName = "EdB Prepare Carefully";
		public static readonly string Version = "0.13.0.3";

		Window currentLayer = null;
		bool gameplay = false;

		public ModController()
		{

		}

		// Called when the MonoBehavior first starts up.
		public virtual void Start()
		{
			this.enabled = true;
		}

		// Called whenever the game switches from the main menu (level 0) into gameplay
		// (level 1).
		public void OnLevelWasLoaded(int level)
		{
			// Level 0 means we're in the game menus.  Enable the behavior so that
			// it starts checking every frame to see if it needs to inject the custom UI
			if (level == 0) {
				this.gameplay = false;
				this.enabled = true;
				PrepareCarefully.Instance.Active = false;
			}
			// Level 1 means we're in gameplay.  Disable the behavior because we don't
			// need to check anything every frame.
			else if (level == 1) {
				this.gameplay = true;

				// Disable this component
				this.enabled = false;
			}
		}

		// Called every frame when the mod is enabled
		public virtual void Update()
		{
			try {
				if (!gameplay) {
					MenusUpdate();
				}
				else {
					GameplayUpdate();
				}
			}
			catch (Exception e) {
				this.enabled = false;
				Log.Error(e.ToString());
			}
        }

		// Check if the top user interface element is the vanilla character creation
		// screen.  If it is, and if the mod is enabled, swap in the custom version.
		public virtual void MenusUpdate()
		{
			// Keep track of the user interface element that's currently on the
			// top of the layer stack.
			bool layerChanged = false;
			Window layer = this.TopWindow;
			if (layer != this.currentLayer) {
				//Log.Message("Window changed: " + (layer == null ? "null" : layer.GetType().FullName));
				this.currentLayer = layer;
				layerChanged = true;
			}

			// Check the class name to see if it's the vanilla character creation screen
			if (layer != null) {
				if ("RimWorld.Page_ConfigureStartingPawns".Equals(layer.GetType().FullName)) {
					if (ModEnabled) {
						ResetTextures();
						Page page = layer as Page;
						Page_ConfigureStartingPawns replacement = new Page_ConfigureStartingPawns();
						replacement.nextAct = page.nextAct;
						replacement.next = page.next;
						PrepareCarefully.Instance.OriginalPage = replacement;
						Find.WindowStack.TryRemove(layer, true);
						Find.WindowStack.Add(replacement);
						Log.Message("Swapped in EdB Prepare Carefully Character Creation Page");
					}
					else {
						if (layerChanged) {
							Log.Message("EdB Prepare Carefully not enabled.  Did not replace Character Creation Page");
						}
					}
				}
			}
		}

		public virtual void GameplayUpdate()
		{
		}
			
		// Find the top window in the stack that isn't the Console.  If we don't do this, all of our logic
		// around swapping in a replacement window will fail when the console is up.
		public Window TopWindow
		{
			get {
				// The accessors reference properties that might be null with no null-checks, so we need to do some
				// non-obvious null-checks ourselves here.
				if (Find.UIRoot != null && Find.UIRoot.windows != null && Find.WindowStack != null && Find.WindowStack.Windows != null) {
					// Iterate the layers.
					foreach (Window window in Find.WindowStack.Windows.Reverse()) {
						if (window != null && window.GetType().FullName != "Verse.ImmediateWindow"
								&& window.GetType().FullName != "Verse.EditWindow_Log") {
							return window;
						}
					}
				}
				return null;
			}
		}

		public void ResetTextures()
		{
			Textures.Reset();
		}

		public bool ModEnabled
		{
			get {
				ModMetaData mod = ModLister.AllInstalledMods.First((ModMetaData m) => {
					return m.Name.Equals(ModName);
				});
				if (mod == null) {
					return false;
				}
				return mod.Active;
			}
		}
    }
}
