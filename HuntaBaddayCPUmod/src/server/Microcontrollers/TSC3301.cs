using LogicWorld.Server.Circuitry;
using HuntaBaddayCPUmod.CustomData;
namespace HuntaBaddayCPUmod;

public class TSC3301 : LogicComponent<IRamData> {
    protected override void SetDataDefaultValues() {
        Data.Initialize();
    }
}