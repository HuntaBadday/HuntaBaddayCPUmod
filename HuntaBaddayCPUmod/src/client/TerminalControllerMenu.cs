using EccsGuiBuilder.Client.Layouts.Helper;
using EccsGuiBuilder.Client.Wrappers;
using EccsGuiBuilder.Client.Wrappers.AutoAssign;
using HuntaBaddayCPUmod.CustomData;
using LogicUI.MenuParts;
using LogicWorld.UI;

namespace HuntaBaddayCPUmod {
    public class TerminalControllerMenu : EditComponentMenu<ITermControllerData>, IAssignMyFields {
        public static void init() {
            WS.window("TerminalControllerMenu")
                .setYPosition(150)
                .configureContent(content => content
                    .layoutVertical()
                    .addContainer("Box1", container => container
                        .layoutGrowGapVerticalInner()
                        .add(WS.textLine.setLocalizationKey("HuntaBaddayCPUmod.TermContMenuWidth"))
                        .add(WS.slider
                            .injectionKey(nameof(widthSlider))
                            .fixedSize(500, 45)
                            .setInterval(1)
                            .setMin(1)
                            .setMax(64)
                        )
                    )
                    .addContainer("Box2", container => container
                        .layoutGrowGapVerticalInner()
                        .add(WS.textLine.setLocalizationKey("HuntaBaddayCPUmod.TermContMenuHeight"))
                        .add(WS.slider
                            .injectionKey(nameof(heightSlider))
                            .fixedSize(500, 45)
                            .setInterval(1)
                            .setMin(1)
                            .setMax(64)
                        )
                    )
                )
                .add<TerminalControllerMenu>()
                .build();
        }
        
        [AssignMe]
        public InputSlider widthSlider;
        [AssignMe]
        public InputSlider heightSlider;
        
        protected override void OnStartEditing() {
            byte width = FirstComponentBeingEdited.Data.Width;
            byte height = FirstComponentBeingEdited.Data.Height;
            widthSlider.SetValueWithoutNotify(width);
            heightSlider.SetValueWithoutNotify(height);
        }
        
        public override void Initialize() {
            base.Initialize();
            widthSlider.OnValueChangedInt += widthChanged;
            heightSlider.OnValueChangedInt += heightChanged;
        }
        
        private void widthChanged(int newWidth) {
            FirstComponentBeingEdited.Data.Width = (byte)newWidth;
        }
        
        private void heightChanged(int newHeight) {
            FirstComponentBeingEdited.Data.Height = (byte)newHeight;
        }
    }
}

