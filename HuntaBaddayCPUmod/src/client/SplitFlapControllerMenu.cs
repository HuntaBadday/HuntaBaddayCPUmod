using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using HuntaBaddayCPUmod.CustomData;
using LogicUI.MenuParts;
using LogicWorld.UI;

namespace HuntaBaddayCPUmod {
    public class SplitFlapControllerMenu : EditComponentMenu<ISplitFlapControllerData>, IAssignMyFields {
        public static void init() {
            WS.window("Split Flap Controller Menu")
                .setYPosition(150)
                .configureContent(content => content
                    .layoutVertical()
                    .addContainer("Box2", container => container
                        .layoutGrowGapVerticalInner()
                        .add(WS.textLine.setLocalizationKey("HuntaBaddayCPUmod.SplitFlapContAmount"))
                        .add(WS.slider
                            .injectionKey(nameof(amountSlider))
                            .fixedSize(500, 45)
                            .setInterval(1)
                            .setMin(1)
                            .setMax(16)
                        )
                    )
                )
                .add<SplitFlapControllerMenu>()
                .build();
        }
        
        [AssignMe]
        public InputSlider amountSlider;
        
        protected override void OnStartEditing() {
            int flipAmount = FirstComponentBeingEdited.Data.FlipAmount;
            amountSlider.SetValueWithoutNotify(flipAmount);
        }
        
        public override void Initialize() {
            base.Initialize();
            amountSlider.OnValueChangedInt += amountChanged;
        }
        
        private void amountChanged(int newAmount) {
            FirstComponentBeingEdited.Data.FlipAmount = newAmount;
        }
    }
}

