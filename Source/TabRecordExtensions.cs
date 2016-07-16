using System;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public static class TabRecordExtensions
	{
		public static void DrawTab(this TabRecord tab, Rect rect)
		{
			Rect drawRect = new Rect(rect);
			drawRect.width = 30;
			Rect drawRect2 = new Rect(rect);
			drawRect2.width = 30;
			drawRect2.x = rect.x + rect.width - 30;
			Rect texRect = new Rect(0.53125f, 0, 0.46875f, 1);
			Rect drawRect3 = new Rect(rect);
			drawRect3.x += drawRect.width;
			drawRect3.width -= 60;
			Rect texRect2 = new Rect(30, 0, 4, (float)Textures.TextureTabAtlas.height).ToUVRect(new Vector2((float)Textures.TextureTabAtlas.width, (float)Textures.TextureTabAtlas.height));
			Widgets.DrawTexturePart(drawRect, new Rect(0, 0, 0.46875f, 1), Textures.TextureTabAtlas);
			Widgets.DrawTexturePart(drawRect3, texRect2, Textures.TextureTabAtlas);
			Widgets.DrawTexturePart(drawRect2, texRect, Textures.TextureTabAtlas);
			Rect rect2 = rect;
			if (rect.Contains(Event.current.mousePosition)) {
				GUI.color = Color.yellow;
			}
			Widgets.Label(new Rect(rect2.x - 6, rect2.y, rect2.width, rect2.height), tab.label);
			GUI.color = Color.white;
			if (!tab.selected) {
				Rect drawRect4 = new Rect(rect);
				drawRect4.y += rect.height;
				drawRect4.y -= 1;
				drawRect4.height = 1;
				Rect texRect3 = new Rect(0.5f, 0.01f, 0.01f, 0.01f);
				Widgets.DrawTexturePart(drawRect4, texRect3, Textures.TextureTabAtlas);
			}
		}

		public static void DrawNewColonistTab(this TabRecord tab, Rect rect)
		{
			if (rect.Contains(Event.current.mousePosition)) {
				GUI.color = new Color(1, 1, 1, 0.7f);
			}
			else {
				GUI.color = new Color(1, 1, 1, 0.2f);
			}
			Rect drawRect = new Rect(rect);
			drawRect.width = 30;
			Rect drawRect2 = new Rect(rect);
			drawRect2.width = 30;
			drawRect2.x = rect.x + rect.width - 30;
			Rect texRect = new Rect(0.53125f, 0, 0.46875f, 1);
			Rect drawRect3 = new Rect(rect);
			drawRect3.x += drawRect.width;
			drawRect3.width -= 60;
			Rect texRect2 = new Rect(30, 0, 4, (float)Textures.TextureTabAtlas.height).ToUVRect(new Vector2((float)Textures.TextureTabAtlas.width, (float)Textures.TextureTabAtlas.height));
			Widgets.DrawTexturePart(drawRect, new Rect(0, 0, 0.46875f, 1), Textures.TextureTabAtlas);
			Widgets.DrawTexturePart(drawRect3, texRect2, Textures.TextureTabAtlas);
			Widgets.DrawTexturePart(drawRect2, texRect, Textures.TextureTabAtlas);
			Rect rect2 = rect;
			GUI.color = new Color(0.7f, 0.7f, 0.7f);
			if (rect.Contains(Event.current.mousePosition)) {
				Rect drawRect4 = new Rect(rect);
				drawRect4.y += rect.height;
				drawRect4.y -= 1;
				drawRect4.height = 1;
				Rect texRect3 = new Rect(0.5f, 0.01f, 0.01f, 0.01f);
				Widgets.DrawTexturePart(drawRect4, texRect3, Textures.TextureTabAtlas);

				GUI.color = Color.yellow;
			}
			Widgets.Label(rect2, tab.label);
			GUI.color = Color.white;
		}
	}  
}

