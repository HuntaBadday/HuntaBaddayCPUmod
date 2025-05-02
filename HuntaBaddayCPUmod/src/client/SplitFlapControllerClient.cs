using HuntaBaddayCPUmod.CustomData;
using LogicWorld.Rendering.Components;

namespace HuntaBaddayCPUmod {
    public class SplitFlapControllerClient : ComponentClientCode<ISplitFlapControllerData> {
        protected override void SetDataDefaultValues() {
            Data.Initialize();
        }
    }
}