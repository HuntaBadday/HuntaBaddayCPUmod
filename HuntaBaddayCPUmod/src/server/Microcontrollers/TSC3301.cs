using LogicWorld.Server.Circuitry;
using HuntaBaddayCPUmod.CustomData;
using HuntasICLib.Serial;
using HuntasICLib.CPU;
using HuntasICLib.Memory;
using HuntasICLib.Timer;
namespace HuntaBaddayCPUmod;

public class TSC3301 : LogicComponent<IRamData> {
    public override bool HasPersistentValues => true;
    
    // INPUT PINS
    const int DP0IN = 0;
    const int DP1IN = 16;
    const int SER0IN = 17;
    const int SER1IN = 18;
    const int TNETIN = 19;
    const int CNT0 = 20;
    const int CNT1 = 21;
    const int AUX0 = 22;
    const int AUX1 = 23;
    const int AUX2 = 24;
    const int AUX3 = 25;
    const int IRQ2 = 26;
    const int IRQ3 = 27;
    const int RUN = 28;
    const int RESET = 29;
    const int LOAD = 30;
    
    // OUTPUT PINS
    const int DP0OUT = 0;
    const int DP1OUT = 16;
    const int SER0OUT = 17;
    const int SER1OUT = 18;
    const int TNETOUT = 19;
    const int DP0READ = 20;
    const int DP0WRITE = 21;
    const int DP1READ = 22;
    const int DP1WRITE = 23;
    const int TA0OUT = 24;
    const int TB0OUT = 25;
    const int TA1OUT = 26;
    const int TB1OUT = 27;
    
    // MODULES
    
    LWC33 CPU = new LWC33();
    
    SerialReceiver SER0Recv = new SerialReceiver(16);
    SerialTransmitter SER0Trans = new SerialTransmitter(16);
    SerialReceiver SER1Recv = new SerialReceiver(16);
    SerialTransmitter SER1Trans = new SerialTransmitter(16);
    
    BufferFIFO16b SER0Buffer = new BufferFIFO16b(1024);
    BufferFIFO16b SER1Buffer = new BufferFIFO16b(1024);
    
    BufferFIFO16b Buffer0 = new BufferFIFO16b(8192);
    BufferFIFO16b Buffer1 = new BufferFIFO16b(8192);
    BufferFIFO16b Buffer2 = new BufferFIFO16b(8192);
    BufferFIFO16b Buffer3 = new BufferFIFO16b(8192);
    
    TSC6530 Timer0 = new TSC6530();
    TSC6530 Timer1 = new TSC6530();
    protected override void SetDataDefaultValues() {
        Data.Initialize();
    }

    protected override void DoLogicUpdate() {
        
    }
}