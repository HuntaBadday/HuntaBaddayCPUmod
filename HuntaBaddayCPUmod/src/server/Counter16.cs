using System;
using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod {
    public class Counter16 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int DATAIN = 0;
        const int SET = 16;
        const int READ = 17;
        const int DOWN = 18;
        const int RESET = 19;
        const int UP = 20;
        
        const int DATAOUT = 0;
        const int FIN = 16;
        
        ushort counterValue = 0;
        
        protected override void DoLogicUpdate() {
            if (Inputs[RESET].On) {
                counterValue = 0;
            } else if (Inputs[SET].On) {
                counterValue = readData();
                
            } else if (Inputs[UP].On) {
                counterValue++;
                QueueLogicUpdate();
            } else if (Inputs[DOWN].On) {
                counterValue--;
                QueueLogicUpdate();
            }
            
            if (Inputs[READ].On) {
                writeData(counterValue);
            } else {
                writeData(0);
            }
            Outputs[FIN].On = readData() == counterValue;
        }
        
        ushort readData() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output >>= 1;
                output |= (ushort)(Inputs[DATAIN+i].On ? 0x8000 : 0);
            }
            return output;
        }
        
        void writeData(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[DATAOUT+i].On = (data&1) != 0;
                data >>= 1;
            }
        }
        
        protected override byte[] SerializeCustomData(){
            return BitConverter.GetBytes(counterValue);
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) return;
            if (data.Length != 2) return;
            counterValue = BitConverter.ToUInt16(data);
        }
    }
}