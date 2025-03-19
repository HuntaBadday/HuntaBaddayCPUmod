using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod {
    public class LWC33_Comp : LogicComponent {
        public override bool HasPersistentValues => true;
        
        TSC_LWC33 cpu = new TSC_LWC33();
        
        // INPUT PINS
        const int MEMIN = 0;
        const int DEVIN = 16;
        const int AUX = 32;
        const int INTS = 36;
        const int RST = 40;
        const int PAUSE = 41;
        const int CLOCK = 42;
        const int SETCARRY = 43;
        
        // OUTPUT PINS
        const int MEMOUT = 0;
        const int DEVOUT = 16;
        const int MEMADDR = 32;
        const int DEVADDR = 48;
        const int SEGMENT = 64;
        const int MEMW = 68;
        const int MEMR = 69;
        const int DEVW = 70;
        const int DEVR = 71;
        const int SYNC = 72;
        
        protected override void DoLogicUpdate() {
            // Set CPU inputs to component inputs
            cpu.dataBusInput = readMem();
            cpu.deviceBusInput = readDev();
            cpu.auxState = readAux();
            cpu.interruptPinStates = readInts();
            cpu.resetState = Inputs[RST].On;
            cpu.pauseState = Inputs[PAUSE].On;
            cpu.clockState = Inputs[CLOCK].On;
            cpu.setCarryState = Inputs[SETCARRY].On;
            
            cpu.UpdateLogic();
            
            // Set component outputs to cpu output
            writeMem(cpu.dataBusOutput);
            setMemAddr(cpu.addressOutput);
            writeDev(cpu.deviceBusOutput);
            setDevAddr(cpu.deviceAddrOutput);
            setSegment(cpu.segmentOutput);
            Outputs[MEMW].On = cpu.writeState;
            Outputs[MEMR].On = cpu.readState;
            Outputs[DEVW].On = cpu.devWriteState;
            Outputs[DEVR].On = cpu.devReadState;
            Outputs[SYNC].On = cpu.syncState;
        }
        
        // Read pins to data
        ushort readMem() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output >>= 1;
                output |= (ushort)(Inputs[MEMIN+i].On ? 0x8000 : 0);
            }
            return output;
        }
        
        // Read pins to data
        ushort readDev() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output >>= 1;
                output |= (ushort)(Inputs[DEVIN+i].On ? 0x8000 : 0);
            }
            return output;
        }
        
        // Read pins to data
        byte readAux() {
            byte output = 0;
            for (int i = 0; i < 4; i++) {
                output >>= 1;
                output |= (byte)(Inputs[AUX+i].On ? 0x8 : 0);
            }
            return output;
        }
        
        // Read pins to data
        byte readInts() {
            byte output = 0;
            for (int i = 0; i < 4; i++) {
                output >>= 1;
                output |= (byte)(Inputs[INTS+i].On ? 0x8 : 0);
            }
            return output;
        }
        
        // Set pins to data
        void writeMem(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[MEMOUT+i].On = (data&1) == 1;
                data >>= 1;
            }
        }
        
        // Set pins to data
        void writeDev(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[DEVOUT+i].On = (data&1) == 1;
                data >>= 1;
            }
        }
        
        // Set pins to data
        void setMemAddr(ushort addr) {
            for (int i = 0; i < 16; i++) {
                Outputs[MEMADDR+i].On = (addr&1) == 1;
                addr >>= 1;
            }
        }
        
        // Set pins to data
        void setDevAddr(ushort addr) {
            for (int i = 0; i < 16; i++) {
                Outputs[DEVADDR+i].On = (addr&1) == 1;
                addr >>= 1;
            }
        }
        
        // Set pins to data
        void setSegment(ushort segment) {
            for (int i = 0; i < 4; i++) {
                Outputs[SEGMENT+i].On = (segment&1) == 1;
                segment >>= 1;
            }
        }
        
        protected override byte[] SerializeCustomData() {
            return cpu.serializeCPUState();
        }
        
        protected override void DeserializeData(byte[] data) {
            cpu.deserializeCPUState(data);
        }
        
        // Make sure only these pins trigger logic update
        protected bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == CLOCK || index == SETCARRY || index >= AUX && index < AUX+4;
        }
    }
}