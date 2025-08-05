using System;
using System.IO;
using System.IO.Compression;
using System.Timers;
using LogicWorld.Server.Circuitry;
using HuntaBaddayCPUmod.CustomData;
using HuntasICLib.Serial;
using HuntasICLib.CPU;
using HuntasICLib.Memory;
using HuntasICLib.Timer;
using HuntasICLib;
namespace HuntaBaddayCPUmod;

public class TSC3301 : LogicComponent<IRamData> {
    public override bool HasPersistentValues => true;
    
    // INPUT PINS
    const int DP0IN = 0;
    const int DP1IN = 16;
    const int SER0IN = 32;
    const int SER1IN = 33;
    const int TNETIN = 34;
    const int CNT0 = 35;
    const int CNT1 = 36;
    const int AUX0 = 37;
    const int AUX1 = 38;
    const int AUX2 = 39;
    const int AUX3 = 40;
    const int IRQ2 = 41;
    const int IRQ3 = 42;
    const int CLOCK = 43;
    const int RESET = 44;
    const int LOAD = 45;
    
    // OUTPUT PINS
    const int DP0OUT = 0;
    const int DP1OUT = 16;
    const int SER0OUT = 32;
    const int SER1OUT = 33;
    const int TNETOUT = 34;
    const int DP0READ = 35;
    const int DP0WRITE = 36;
    const int DP1READ = 37;
    const int DP1WRITE = 38;
    const int TA0OUT = 39;
    const int TB0OUT = 40;
    const int TA1OUT = 41;
    const int TB1OUT = 42;
    
    // MODULES
    
    LWC33 CPU = new LWC33();
    
    SerialReceiver SER0Recv = new SerialReceiver(16);
    SerialTransmitter SER0Trans = new SerialTransmitter(16);
    SerialReceiver SER1Recv = new SerialReceiver(16);
    SerialTransmitter SER1Trans = new SerialTransmitter(16);
    
    BufferFIFO16b SER0Buffer = new BufferFIFO16b(1024);
    BufferFIFO16b SER1Buffer = new BufferFIFO16b(1024);
    
    BufferFIFO16b Buffer0 = new BufferFIFO16b(4096);
    BufferFIFO16b Buffer1 = new BufferFIFO16b(4096);
    BufferLIFO16b Buffer2 = new BufferLIFO16b(4096);
    BufferLIFO16b Buffer3 = new BufferLIFO16b(4096);
    
    TSC6530 Timer0 = new TSC6530();
    TSC6530 Timer1 = new TSC6530();
    
    TNETReceiver TNETRecv = new TNETReceiver();
    TNETTransmitter TNETTrans = new TNETTransmitter();
    
    // SYSTEM VARS
    ushort[] memory = new ushort[0x10000];
    
    byte[] currentTnetPacket = new byte[0];
    int tnetIndex = 0;
    
    MemoryStream tnetSendBuffer = new MemoryStream();
    
    // STATE TRACKING
    int lastIntState;
    
    bool lastDevReadState;
    bool lastDevWriteState;
    
    // MISC
    bool loadFromSave;

    protected override void Initialize() {
        loadFromSave = true;
    }

    protected override void SetDataDefaultValues() {
        Data.Initialize();
    }

    protected override void DoLogicUpdate() {
        // Pipe I/O
        CPU.setCarryState = false; // Reset to off
        
        CPU.resetState = Inputs[RESET].On;
        CPU.auxState = (byte)ReadBus(AUX0, 4);
        
        int intState = ReadBus(IRQ2, 2);
        int intChange = ~lastIntState & intState & 0x3;
        CPU.interruptPinStates |= (byte)(intChange << 2);
        lastIntState = intState;
        
        Timer0.readState = CPU.devReadState;
        Timer0.writeState = CPU.devWriteState;
        Timer0.databusInput = (byte)CPU.deviceBusOutput;
        Timer0.addressInput = (byte)(CPU.deviceAddrOutput&0x7);
        Timer0.clkState = Inputs[CLOCK].On;
        Timer0.rstState = Inputs[RESET].On;
        Timer0.csState = false;
        Timer0.cntState = Inputs[CNT0].On;
        
        Timer1.readState = CPU.devReadState;
        Timer1.writeState = CPU.devWriteState;
        Timer1.databusInput = (byte)CPU.deviceBusOutput;
        Timer1.addressInput = (byte)(CPU.deviceAddrOutput&0x7);
        Timer1.clkState = Inputs[CLOCK].On;
        Timer1.rstState = Inputs[RESET].On;
        Timer1.csState = false;
        Timer1.cntState = Inputs[CNT1].On;
        
        if (CPU.deviceAddrOutput is >= 0x100 and <= 0x107) {
            Timer0.csState = true;
            CPU.deviceBusInput = Timer0.databusOutput;
        } else if (CPU.deviceAddrOutput is >= 0x108 and <= 0x10f) {
            Timer1.csState = true;
            CPU.deviceBusInput = Timer1.databusOutput;
        }
        
        Outputs[DP0READ].On = false;
        Outputs[DP0WRITE].On = false;
        Outputs[DP1READ].On = false;
        Outputs[DP1WRITE].On = false;
        
        if (CPU.devReadState && !lastDevReadState) {
            switch (CPU.deviceAddrOutput) {
                case 0x110: // TNET DATA
                    if (tnetIndex >= currentTnetPacket.Length) {
                        CPU.setCarryState = true;
                    } else {
                        CPU.deviceBusInput = currentTnetPacket[tnetIndex++];
                    }
                    break;
                case 0x111: // TNET DATA EXTENDED
                    if (tnetIndex >= currentTnetPacket.Length-1) {
                        CPU.setCarryState = true;
                    } else {
                        CPU.deviceBusInput |= currentTnetPacket[tnetIndex++];
                        CPU.deviceBusInput |= (ushort)(currentTnetPacket[tnetIndex++] << 8);
                    }
                    break;
                case 0x112: // TNET Control
                    CPU.deviceBusInput = (ushort)((tnetSendBuffer.Length == 0 ? 0x2 : 0) | (TNETRecv.Available() ? 0x4 : 0) | (tnetIndex >= currentTnetPacket.Length ? 0x8 : 0));
                    break;
                case 0x114: // Buffer 0
                    CPU.setCarryState = !Buffer0.dataAvailable;
                    CPU.deviceBusInput = Buffer0.Read();
                    break;
                case 0x115: // Buffer 1
                    CPU.setCarryState = !Buffer1.dataAvailable;
                    CPU.deviceBusInput = Buffer1.Read();
                    break;
                case 0x116: // Buffer 2
                    CPU.setCarryState = !Buffer2.dataAvailable;
                    CPU.deviceBusInput = Buffer2.Read();
                    break;
                case 0x117: // Buffer 3
                    CPU.setCarryState = !Buffer3.dataAvailable;
                    CPU.deviceBusInput = Buffer3.Read();
                    break;
                case 0x118: // Buffer control
                    CPU.deviceBusInput = (ushort)((Buffer0.dataAvailable ? 0x1 : 0) | (Buffer1.dataAvailable ? 0x2 : 0) | (Buffer2.dataAvailable ? 0x4 : 0) | (Buffer3.dataAvailable ? 0x8 : 0));
                    break;
                case 0x11A: // Serial 0 data
                    CPU.setCarryState = !SER0Buffer.dataAvailable;
                    CPU.deviceBusInput = SER0Buffer.Read();
                    break;
                case 0x11B: // Serial 1 data
                    CPU.setCarryState = !SER1Buffer.dataAvailable;
                    CPU.deviceBusInput = SER1Buffer.Read();
                    break;
                case 0x11C: // Serial control
                    CPU.deviceBusInput = (ushort)((SER0Buffer.dataAvailable ? 0x1 : 0) | (SER1Buffer.dataAvailable ? 0x2 : 0));
                    break;
            }
        }
        // Special for GPIO
        if (CPU.devReadState) {
            switch (CPU.deviceAddrOutput) {
                case 0x11d: // GPIO 0
                    CPU.deviceBusInput = ReadBus(DP0IN, 16);
                    Outputs[DP0READ].On = true;
                    break;
                case 0x11e: // GPIO 1
                    CPU.deviceBusInput = ReadBus(DP1IN, 16);
                    Outputs[DP1READ].On = true;
                    break;
            }
        }
        if (CPU.devWriteState && !lastDevWriteState) {
            switch (CPU.deviceAddrOutput) {
                case 0x110: // TNET DATA
                    if (tnetSendBuffer.Length < 1020) tnetSendBuffer.WriteByte((byte)CPU.deviceBusOutput);
                    break;
                case 0x111: // TNET DATA EXTENDED
                    if (tnetSendBuffer.Length < 1019) {
                        tnetSendBuffer.WriteByte((byte)CPU.deviceBusOutput);
                        tnetSendBuffer.WriteByte((byte)(CPU.deviceBusOutput >> 8));
                    }
                    break;
                case 0x112: // TNET Control
                    if ((CPU.deviceBusOutput & 0x1) != 0) {
                        byte[] data = tnetSendBuffer.ToArray();
                        TNETTrans.Send(data);
                        tnetSendBuffer.SetLength(0);
                    }
                    if ((CPU.deviceBusOutput & 0x2) != 0) tnetSendBuffer.SetLength(0);
                    if ((CPU.deviceBusOutput & 0x4) != 0) {
                        currentTnetPacket = TNETRecv.GetNextPacket();
                        tnetIndex = 0;
                    }
                    if ((CPU.deviceBusOutput & 0x8) != 0) {
                        currentTnetPacket = new byte[0];
                        tnetIndex = 0;
                        TNETRecv.Reset();
                    }
                    break;
                case 0x114: // Buffer 0
                    Buffer0.Write(CPU.deviceBusOutput);
                    break;
                case 0x115: // Buffer 1
                    Buffer1.Write(CPU.deviceBusOutput);
                    break;
                case 0x116: // Buffer 2
                    Buffer2.Write(CPU.deviceBusOutput);
                    break;
                case 0x117: // Buffer 3
                    Buffer3.Write(CPU.deviceBusOutput);
                    break;
                case 0x118: // Buffer control
                    if ((CPU.deviceBusOutput & 0x1) != 0) Buffer0.Reset();
                    if ((CPU.deviceBusOutput & 0x2) != 0) Buffer1.Reset();
                    if ((CPU.deviceBusOutput & 0x4) != 0) Buffer2.Reset();
                    if ((CPU.deviceBusOutput & 0x8) != 0) Buffer3.Reset();
                    break;
                case 0x11A: // Serial 0 data
                    SER0Trans.Transmit(CPU.deviceBusOutput);
                    break;
                case 0x11B: // Serial 1 data
                    SER1Trans.Transmit(CPU.deviceBusOutput);
                    break;
                case 0x11C: // Serial control
                    if ((CPU.deviceBusOutput & 0x1) != 0) SER0Buffer.Reset();
                    if ((CPU.deviceBusOutput & 0x2) != 0) SER1Buffer.Reset();
                    break;
                case 0x11F:
                    if ((CPU.deviceBusOutput & 0x1) != 0) CPU.interruptPinStates &= 0b1011;
                    if ((CPU.deviceBusOutput & 0x2) != 0) CPU.interruptPinStates &= 0b0111;
                    break;
            }
        }
        // Special for GPIO
        if (CPU.devWriteState) {
            switch (CPU.deviceAddrOutput) {
                case 0x11d: // GPIO 0
                    WriteBus(DP0OUT, CPU.deviceBusOutput, 16);
                    Outputs[DP0WRITE].On = true;
                    break;
                case 0x11e: // GPIO 1
                    WriteBus(DP1OUT, CPU.deviceBusOutput, 16);
                    Outputs[DP1WRITE].On = true;
                    break;
            }
        }
        
        // Interrupts
        CPU.interruptPinStates &= 0b1100;
        CPU.interruptPinStates |= (byte)(Timer0.irqState||Timer1.irqState ? 0x1 : 0);
        CPU.interruptPinStates |= (byte)(TNETRecv.Available() ? 0x2 : 0);
        
        // Read memory
        CPU.dataBusInput = (ushort)(CPU.readState ? memory[CPU.addressOutput] : 0);
        
        // Write memory
        if (CPU.writeState && CPU.addressOutput < 0xe000) memory[CPU.addressOutput] = CPU.dataBusOutput;
        
        // Clock clock
        CPU.clockState = Inputs[CLOCK].On;
        
        // Track states
        lastDevReadState = CPU.devReadState;
        lastDevWriteState = CPU.devWriteState;
        
        // Update logic of all modules
        CPU.UpdateLogic();
        Timer0.LogicUpdate();
        Timer1.LogicUpdate();
        if (SER0Recv.LogicUpdate(Inputs[SER0IN].On))
            SER0Buffer.Write((ushort)SER0Recv.value);
        if (SER1Recv.LogicUpdate(Inputs[SER1IN].On))
            SER1Buffer.Write((ushort)SER1Recv.value);
        Outputs[SER0OUT].On = SER0Trans.LogicUpdate();
        Outputs[SER1OUT].On = SER1Trans.LogicUpdate();
        TNETRecv.LogicUpdate(Inputs[TNETIN].On);
        Outputs[TNETOUT].On = TNETTrans.LogicUpdate();
        
        // Extra reset stuff
        if (Inputs[RESET].On) {
            Buffer0.Reset();
            Buffer1.Reset();
            Buffer2.Reset();
            Buffer3.Reset();
            SER0Buffer.Reset();
            SER1Buffer.Reset();
            TNETRecv.Reset();
            TNETTrans.Reset();
            
            tnetIndex = 0;
            currentTnetPacket = new byte[0];
            tnetSendBuffer.SetLength(0);
            
            CPU.interruptPinStates &= 0b0011;
        }
        
        // Output timer outputs to bus
        Outputs[TA0OUT].On = Timer0.taOutState;
        Outputs[TB0OUT].On = Timer0.tbOutState;
        Outputs[TA1OUT].On = Timer1.taOutState;
        Outputs[TB1OUT].On = Timer1.tbOutState;
        
        QueueLogicUpdate();
    }
    
    void WriteBus(int pin, ushort value, int bits) {
        for (int i = 0; i < bits; i++) {
            Outputs[pin+i].On = (value & 1) != 0;
            value >>= 1;
        }
    }
    
    ushort ReadBus(int pin, int bits) {
        ushort output = 0;
        for (int i = bits-1; i >= 0; i--) {
            output <<= 1;
            output |= (ushort)(Inputs[pin+i].On ? 1 : 0);
        }
        return output;
    }
    
    protected override void OnCustomDataUpdated() {
        if (Data.State == 1 && Data.ClientIncomingData != null || loadFromSave && Data.Data != null) {
            if (Data.State == 1) {
                Logger.Info("Loading data from client");
                try {
                    byte[] data = Decompress(Data.ClientIncomingData);
                    Buffer.BlockCopy(data, 0, memory, 0x1c000, data.Length > 0x4000 ? 0x4000 : data.Length);
                } catch (Exception ex) {
                    Logger.Error("HuntaBaddayCPUmod - Loading data from client failed with exception: " + ex);
                }
            } else {
                try {
                    byte[] hcData = Decompress(Data.Data);
                    HCUnpacker unpacker = new HCUnpacker(hcData);
                    byte[] tsc3301data = unpacker.ReadNext();
                    if (tsc3301data != null) {
                        MemoryStream m = new MemoryStream(tsc3301data);
                        BinaryReader r = new BinaryReader(m);
                        byte[] mem = r.ReadBytes(memory.Length*2);
                        Buffer.BlockCopy(mem, 0, memory, 0, mem.Length);
                        lastIntState = r.ReadInt32();
                        lastDevReadState = r.ReadBoolean();
                        lastDevWriteState = r.ReadBoolean();
                    }
                    
                    CPU.deserializeCPUState(unpacker.ReadNext());
                    SER0Recv.Deserialize(unpacker.ReadNext());
                    SER0Trans.Deserialize(unpacker.ReadNext());
                    SER1Recv.Deserialize(unpacker.ReadNext());
                    SER1Trans.Deserialize(unpacker.ReadNext());
                    SER0Buffer.Deserialize(unpacker.ReadNext());
                    SER1Buffer.Deserialize(unpacker.ReadNext());
                    Buffer0.Deserialize(unpacker.ReadNext());
                    Buffer1.Deserialize(unpacker.ReadNext());
                    Buffer2.Deserialize(unpacker.ReadNext());
                    Buffer3.Deserialize(unpacker.ReadNext());
                    Timer0.Deserialize(unpacker.ReadNext());
                    Timer1.Deserialize(unpacker.ReadNext());
                } catch (Exception ex) {
                    Logger.Error("HuntaBaddayCPUmod - Loading data fromm save failed with exception: " + ex);
                }
            }
            
            loadFromSave = false;
            if (Data.State == 1) {
                Data.State = 0;
                Data.ClientIncomingData = new byte[0];
            }
            QueueLogicUpdate();
        }
    }
    
    protected override void SavePersistentValuesToCustomData() {
        /*
           ushort[] memory = new ushort[0x10000];
           
           int lastIntState;
           
           bool lastDevReadState;
           bool lastDevWriteState;
           
           // OTHER
           LWC33 CPU = new LWC33();
           
           SerialReceiver SER0Recv = new SerialReceiver(16);
           SerialTransmitter SER0Trans = new SerialTransmitter(16);
           SerialReceiver SER1Recv = new SerialReceiver(16);
           SerialTransmitter SER1Trans = new SerialTransmitter(16);
           
           BufferFIFO16b SER0Buffer = new BufferFIFO16b(1024);
           BufferFIFO16b SER1Buffer = new BufferFIFO16b(1024);
           
           BufferFIFO16b Buffer0 = new BufferFIFO16b(4096);
           BufferFIFO16b Buffer1 = new BufferFIFO16b(4096);
           BufferFIFO16b Buffer2 = new BufferFIFO16b(4096);
           BufferFIFO16b Buffer3 = new BufferFIFO16b(4096);
           
           TSC6530 Timer0 = new TSC6530();
           TSC6530 Timer1 = new TSC6530();
           
           TNETReceiver TNETRecv = new TNETReceiver();
           TNETTransmitter TNETTrans = new TNETTransmitter();
        */
        
        MemoryStream m = new MemoryStream();
        BinaryWriter w = new BinaryWriter(m);
        
        byte[] memory2 = new byte[memory.Length*2];
        Buffer.BlockCopy(memory, 0, memory2, 0, memory2.Length);
        w.Write(memory2);
        w.Write(lastIntState);
        w.Write(lastDevReadState);
        w.Write(lastDevWriteState);
        
        HCPacker packer = new HCPacker();
        packer.Write(m.ToArray());
        packer.Write(CPU.serializeCPUState());
        packer.Write(SER0Recv.Serialize());
        packer.Write(SER0Trans.Serialize());
        packer.Write(SER1Recv.Serialize());
        packer.Write(SER1Trans.Serialize());
        packer.Write(SER0Buffer.Serialize());
        packer.Write(SER1Buffer.Serialize());
        packer.Write(Buffer0.Serialize());
        packer.Write(Buffer1.Serialize());
        packer.Write(Buffer2.Serialize());
        packer.Write(Buffer3.Serialize());
        packer.Write(Timer0.Serialize());
        packer.Write(Timer1.Serialize());
        
        Data.Data = Compress(packer.ToArray());
    }
    
    byte[] Decompress(byte[] compressedData) {
        MemoryStream outstream = new MemoryStream();
        byte[] temp = new byte[1024];
        MemoryStream memstream = new MemoryStream(compressedData);
        DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
        int bytesRead;
        while((bytesRead = decompressor.Read(temp, 0, temp.Length)) > 0){
            outstream.Write(temp, 0, bytesRead);
        }
        return outstream.ToArray();
    }
    
    byte[] Compress(byte[] data) {
        MemoryStream output = new MemoryStream();
        using (DeflateStream compressor = new DeflateStream(output, CompressionLevel.Optimal))
            compressor.Write(data);
        return output.ToArray();
    }
}