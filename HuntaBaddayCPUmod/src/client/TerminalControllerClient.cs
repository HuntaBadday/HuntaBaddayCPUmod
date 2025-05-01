using HuntaBaddayCPUmod.CustomData;
using LogicWorld.Rendering.Components;

namespace HuntaBaddayCPUmod {
    public class TerminalControllerClient : ComponentClientCode<ITermControllerData> {
        protected override void SetDataDefaultValues() {
            Data.Initialize();
        }
    }
}