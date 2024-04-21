using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace EdB.PrepareCarefully {
    public class DialogIdeos : Window {
        public delegate void IdeoUpdatedHandler(Ideo ideo);

        public event IdeoUpdatedHandler IdeoUpdated;

        public CustomizedPawn Pawn { get; set; }

        private Vector2 scrollPosition_ideoList;

        private float scrollViewHeight_ideoList;

        private Vector2 scrollPosition_ideoDetails;

        private float scrollViewHeight_ideoDetails;

        public override Vector2 InitialSize => new Vector2(1010f, Mathf.Min(1000f, UI.screenHeight));

        public DialogIdeos() {
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect) {
            IdeoUIUtility.DoIdeoListAndDetails(new Rect(inRect.x, inRect.y, inRect.width, inRect.height - Window.CloseButSize.y),
                ref scrollPosition_ideoList,//ref Vector2 scrollPosition_list
                ref scrollViewHeight_ideoList,//ref float scrollViewHeight_list
                ref scrollPosition_ideoDetails,//ref Vector2 scrollPosition_details
                ref scrollViewHeight_ideoDetails,//ref float scrollViewHeight_details
                false,//bool editMode = false
                false,//bool showCreateIdeoButton = false
                null,//List<Pawn> pawns = null
                null,//Ideo onlyEditIdeo = null
                null,//Action createCustomBtnActOverride = null
                false,//bool forArchonexusRestart = false
                null, //(Pawn p) => Pawn.Pawn.ideo?.Ideo,//Func<Pawn, Ideo> pawnIdeoGetter = null
                null,//Action<Ideo> ideoLoadedFromFile = null
                false, //bool showLoadExistingIdeoBtn = false
                false, //bool allowLoad = true
                null //Action createFluidBtnAct = null
            );
        }

        public override void PreOpen() {
            base.PreOpen();
            IdeoUIUtility.selected = Pawn.Pawn.ideo?.Ideo;
        }

        public override void PostClose() {
            base.PostClose();
            IdeoUpdated?.Invoke(IdeoUIUtility.selected);
        }
    }
}






