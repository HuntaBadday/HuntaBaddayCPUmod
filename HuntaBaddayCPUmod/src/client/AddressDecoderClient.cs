using LogicWorld.Rendering.Components;
using HuntaBaddayCPUmod.CustomData;

namespace HuntaBaddayCPUmod;

public class AddressDecoderClient : ComponentClientCode<IAddressDecoderData> {
    protected override void SetDataDefaultValues() {
        Data.Initialize();
    }
}