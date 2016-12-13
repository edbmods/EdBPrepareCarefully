using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace EdB.PrepareCarefully
{
	public class Dialog_Options<T> : Window
	{
		protected Vector2 ContentMargin = new Vector2(10f, 18f);
		protected Vector2 WindowSize = new Vector2(440f, 584f);
		protected Vector2 ButtonSize = new Vector2(140f, 40f);
		protected float HeaderHeight = 32;
		protected Vector2 ContentSize;
		protected float FooterHeight = 40f;
		protected Rect ContentRect;
		protected Rect ScrollRect;
		protected Rect FooterRect;
		protected Rect HeaderRect;
		protected Rect CancelButtonRect;
		protected Rect ConfirmButtonRect;
		protected Rect SingleButtonRect;
		protected ScrollView ScrollView = new ScrollView();
		protected IEnumerable<T> options;
		protected bool confirmButtonClicked = false;
		protected float WindowPadding = 18;

		public bool IncludeNone = false;

		protected string headerLabel = null;
		public string HeaderLabel {
			get {
				return headerLabel;
			}
			set {
				headerLabel = value;
				ComputeSizes();
			}
		}
		public string ConfirmButtonLabel = "EdB.PrepareCarefully.Close";

		public string CancelButtonLabel = null;

		public Action Initialize = () => { };
		public Func<T, string> NameFunc = (T) => {
			return "";
		};
		public Func<T, string> DescriptionFunc;
		public Func<T, bool> SelectedFunc = (T) => {
			return false;
		};
		public Func<T, bool> EnabledFunc = (T) => {
			return true;
		};
		public Func<string> ConfirmValidation = () => {
			return null;
		};
		public Action<T> SelectAction = (T) => { };
		public Action CloseAction = () => { };
		public Func<bool> NoneEnabledFunc = () => {
			return true;
		};
		public Func<bool> NoneSelectedFunc = () => {
			return false;
		};
		public Action SelectNoneAction = () => {};

		public Dialog_Options(IEnumerable<T> options)
		{
			this.closeOnEscapeKey = true;
			//this.doCloseButton = true;
			this.doCloseX = true;
			this.absorbInputAroundWindow = true;
			this.forcePause = true;

			this.options = options;

			ComputeSizes();
		}

		public IEnumerable<T> Options {
			get { return options; }
			set { options = value; }
		}

		protected void ComputeSizes()
		{
			float headerSize = 0;
			if (HeaderLabel != null) {
				headerSize = HeaderHeight;
			}

			ContentSize = new Vector2(WindowSize.x - WindowPadding * 2 - ContentMargin.x * 2,
				WindowSize.y - WindowPadding * 2  - ContentMargin.y * 2 - FooterHeight - headerSize);

			ContentRect = new Rect(ContentMargin.x, ContentMargin.y + headerSize, ContentSize.x, ContentSize.y);

			ScrollRect = new Rect(0, 0, ContentRect.width, ContentRect.height);

			HeaderRect = new Rect(ContentMargin.x, ContentMargin.y, ContentSize.x, HeaderHeight);

			FooterRect = new Rect(ContentMargin.x, ContentRect.y + ContentSize.y + 20,
				ContentSize.x, FooterHeight);

			SingleButtonRect = new Rect(ContentSize.x / 2 - ButtonSize.x / 2,
				(FooterHeight / 2) - (ButtonSize.y / 2),
				ButtonSize.x, ButtonSize.y);

			CancelButtonRect = new Rect(0,
				(FooterHeight / 2) - (ButtonSize.y / 2),
				ButtonSize.x, ButtonSize.y);
			ConfirmButtonRect = new Rect(ContentSize.x - ButtonSize.x,
				(FooterHeight / 2) - (ButtonSize.y / 2),
				ButtonSize.x, ButtonSize.y);

		}


		public override Vector2 InitialSize {
			get {
				return new Vector2(WindowSize.x, WindowSize.y);
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			GUI.color = Color.white;
			if (HeaderLabel != null) {
				Text.Font = GameFont.Medium;
				Widgets.Label(HeaderRect, HeaderLabel.Translate());
			}

			Text.Font = GameFont.Small;
			GUI.BeginGroup(ContentRect);
			ScrollView.Begin(ScrollRect);

			float cursor = 0;

			if (IncludeNone) {
				float height = Text.CalcHeight("EdB.None".Translate(), ContentSize.x - 32);
				if (height < 30) {
					height = 30;
				}
				bool isEnabled = NoneEnabledFunc();
				bool isSelected = NoneSelectedFunc();
				if (Widgets.RadioButtonLabeled(new Rect(0, cursor, ContentSize.x - 32, height), "EdB.None".Translate(), isSelected)) {
					SelectNoneAction();
				}
				cursor += height;
				cursor += 2;
			}

			Rect itemRect = new Rect(0, cursor, ContentSize.x - 32, 0);
			foreach (T option in options)
			{
				string name = NameFunc(option);
				bool selected = SelectedFunc(option);
				bool enabled = EnabledFunc != null ? EnabledFunc(option) : true;

				float height = Text.CalcHeight(name, ContentSize.x - 32);
				if (height < 30) {
					height = 30;
				}
				itemRect.height = height;
				Vector2 size = Text.CalcSize(name);
				if (size.x > ContentSize.x - 32) {
					size.x = ContentSize.x;
				}
				size.y = height;

				if (enabled) {
					GUI.color = Color.white;
					if (Widgets.RadioButtonLabeled(itemRect, name, selected)) {
						SelectAction(option);
					}
				}
				else {
					GUI.color = new Color(0.65f, 0.65f, 0.65f);
					Widgets.Label(new Rect(0, cursor + 2, ContentSize.x - 32, height), name);
					Texture2D image = Textures.TextureRadioButtonOff;
					Vector2 topLeft = new Vector2(itemRect.x + itemRect.width - 24, itemRect.y + itemRect.height / 2 - 12);
					GUI.color = new Color(1, 1, 1, 0.28f);
					GUI.DrawTexture(new Rect(topLeft.x, topLeft.y, 24, 24), image);
					GUI.color = Color.white;
				}

				if (DescriptionFunc != null) {
					Rect tipRect = new Rect(itemRect.x, itemRect.y, size.x, size.y);
					TooltipHandler.TipRegion(tipRect, DescriptionFunc(option));
				}

				cursor += height;
				cursor += 2;
				itemRect.y = cursor;
			}
			ScrollView.End(cursor);
			GUI.EndGroup();
			GUI.color = Color.white;

			GUI.BeginGroup(FooterRect);
			Rect buttonRect = SingleButtonRect;
			if (CancelButtonLabel != null) {
				if (Widgets.ButtonText(CancelButtonRect, CancelButtonLabel.Translate(), true, true, true)) {
					this.Close(true);
				}
				buttonRect = ConfirmButtonRect;
			}
			if (Widgets.ButtonText(buttonRect, ConfirmButtonLabel.Translate(), true, true, true)) {
				string validationMessage = ConfirmValidation();
				if (validationMessage != null) {
					Messages.Message(validationMessage.Translate(), MessageSound.RejectInput);
				}
				else {
					this.Confirm();
				}
			}
			GUI.EndGroup();
		}

		protected void Confirm()
		{
			confirmButtonClicked = true;
			this.Close(true);
		}
			
		public override void PostClose()
		{
			if (ConfirmButtonLabel != null) {
				if (confirmButtonClicked && CloseAction != null) {
					CloseAction();
				}
			}
			else {
				if (CloseAction != null) {
					CloseAction();
				}
			}
		}
	}
}

