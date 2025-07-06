using LogicWorld.Server.Circuitry;
using HuntaBaddayCPUmod.CustomData;

namespace HuntaBaddayCPUmod;

public class AddressDecoder : LogicComponent<IAddressDecoderData> {
    protected override void SetDataDefaultValues() {
        Data.Initialize();
    }

    protected override void DoLogicUpdate() {
        ushort val = GetData();
        if (val >= Data.StartAddress && val <= Data.EndAddress) {
            Outputs[0].On = true;
        } else {
            Outputs[0].On = false;
        }
    }
    
    ushort GetData() {
        ushort output = 0;
        for (int i = 0; i < 16; i++) {
            output <<= 1;
            output |= (ushort)(Inputs[i].On ? 1 : 0);
        }
        return output;
    }
}