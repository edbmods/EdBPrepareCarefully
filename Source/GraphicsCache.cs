using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class GraphicsCache
	{
		static protected GraphicsCache instance;

		static public GraphicsCache Instance {
			get {
				if (instance == null) {
					instance = new GraphicsCache();
				}
				return instance;
			}
		}

		protected Dictionary<BodyType, Dictionary<ThingDef, Graphic>> bodyTypes = new Dictionary<BodyType, Dictionary<ThingDef, Graphic>>();
		Dictionary<HairDef, Graphic> hairs = new Dictionary<HairDef, Graphic>();
		protected List<Graphic> heads = new List<Graphic>();
		protected List<string> headPaths = new List<string>();
		protected Dictionary<ThingDef, Graphic> things = new Dictionary<ThingDef, Graphic>();

		public GraphicsCache()
		{
			InitializeHeads();
		}

		protected void InitializeHeads()
		{
			MethodInfo headGraphicsMethod = typeof(GraphicDatabaseHeadRecords).GetMethod("BuildDatabaseIfNecessary", BindingFlags.Static | BindingFlags.NonPublic);
			headGraphicsMethod.Invoke(null, null);

			string[] headsFolderPaths = new string[] {
				"Things/Pawn/Humanlike/Heads/Male",
				"Things/Pawn/Humanlike/Heads/Female"
			};
			for (int i = 0; i < headsFolderPaths.Length; i++) {
				string text = headsFolderPaths[i];
				foreach (string current in GraphicDatabaseUtility.GraphicNamesInFolder(text)) {
					string headPath = text + "/" + current;
					headPaths.Add(headPath);
				}
			}
		}

		public Graphic_Multi GetHead(string path)
		{
			return GraphicDatabaseHeadRecords.GetHeadNamed(path, Color.white);
		}
			
		public List<string> MaleHeadPaths {
			get {
				return headPaths.FindAll((string path) => { return !path.Contains("Female"); });
			}
		}

		public List<string> FemaleHeadPaths {
			get {
				return headPaths.FindAll((string path) => { return !path.Contains("Male"); });
			}
		}

		public Graphic GetHair(HairDef def)
		{
			if (hairs.ContainsKey(def)) {
				return hairs[def];
			}
			else {
				Graphic graphic = CreateGraphic(def);
				if (graphic != null) {
					hairs[def] = graphic;
				}
				return graphic;
			}
		}

		public Graphic GetApparel(ThingDef def, BodyType bodyType)
		{
			Dictionary<ThingDef, Graphic> lookup;
			if (!bodyTypes.TryGetValue(bodyType, out lookup)) {
				lookup = new Dictionary<ThingDef, Graphic>();
				bodyTypes[bodyType] = lookup;
			}

			if (lookup.ContainsKey(def)) {
				return lookup[def];
			}
			else {
				Graphic graphic = CreateGraphic(def, bodyType);
				if (graphic != null) {
					lookup[def] = graphic;
				}
				return graphic;
			}
		}

		protected Graphic CreateGraphic(HairDef def)
		{
			if (def.texPath != null) {
				return GraphicDatabase.Get<Graphic_Multi>(def.texPath, ShaderDatabase.Cutout, new Vector2(38, 38), Color.white, Color.white);
			}
			return null;
		}

		protected Graphic CreateGraphic(ThingDef def, BodyType bodyType)
		{
			if (def.apparel != null) {
				if (String.IsNullOrEmpty(def.apparel.wornGraphicPath)) {
					return null;
				}
				string graphicPath;
				if (def.apparel.LastLayer == ApparelLayer.Overhead) {
					graphicPath = def.apparel.wornGraphicPath;
				}
				else {
					graphicPath = def.apparel.wornGraphicPath + "_" + bodyType.ToString();
				}
				return GraphicDatabase.Get<Graphic_Multi>(graphicPath, ShaderDatabase.Cutout, new Vector2(38, 38), Color.white, Color.white);
			}
			return null;
		}
	}
}

