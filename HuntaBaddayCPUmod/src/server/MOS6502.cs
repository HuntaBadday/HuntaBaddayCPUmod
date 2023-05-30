using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod
{
    public class MOS6502 : LogicComponent
    {
        // I 0      I 19
        // I 1      O 19
        // O 0      I 18
        // I 2      I 17
        // I 3      I 16
        // I 4      I 15
        // O 1      O 18
        // I 5      I 14    O 27
        // O 2      I 13    O 26
        // O 3      I 12    O 25
        // O 4      I 11    O 24
        // O 5      I 10    O 23
        // O 6      I 9     O 22
        // O 7      I 8     O 21
        // O 8      I 7     O 20
        // O 9      O 17
        // O 10     O 16
        // O 11     O 15
        // O 12     O 14
        // O 13     I 6
        
        const int phi0Pin = 17;
        const int rstPin = 19;
        const int irqPin = 2;
        const int nmiPin = 4;
        const int RWPin = 18;
        const int syncPin = 1;
        
        const int dataPinW = 20;
        const int dataPinR = 7;
        const int addressPin = 2;
        
        const int phi1Pin = 0;
        const int phi2Pin = 19;
        
        const int fetchState= 0;
        
        bool lastClkState = false;
        bool phi1 = false;
        bool phi2 = false;
        bool isPhi2 = false;
        bool resetTrigger = false;
        bool resetTriggerInt = false;
        
        byte ir = 0;
        ushort pc = 0;
        byte sp = 0;
        
        byte acc = 0;
        byte indexX = 0;
        byte indexY = 0;
        
        int state = 0;
        // 0 - Fetch
        // 1+ - Execute
        
        byte DBL = 0;
        byte DBL2 = 0;
        
        protected override void DoLogicUpdate(){
            phi1 = false;
            phi2 = false;
            
            base.Outputs[phi1Pin].On = !base.Inputs[phi0Pin].On;
            base.Outputs[phi2Pin].On = base.Inputs[phi0Pin].On;
            
            if(readPin(phi0Pin) != lastClkState){
                phi1 = !readPin(phi0Pin);
                phi2 = readPin(phi0Pin);
            } else {
                return;
            }
            lastClkState = readPin(phi0Pin);
            
            if(!readPin(rstPin) || phi2 && resetTrigger){
                resetTrigger = true;
                state = 0;
                flipState();
                return;
            } else {
                resetTrigger = false;
                resetTriggerInt = true;
            }
            
            if(state == 0 && phi1){
                setRW(1);
                setSync(1);
                setAddress(pc);
                setBus(0);
                return;
            }
            if(state == 0 && phi2){
                if(resetTriggerInt || !readPin(irqPin) || !readPin(nmiPin)){
                    resetTriggerInt = false;
                    ir = 0;
                    state = 1;
                } else {
                    ir = readBus();
                    pc++;
                    state = 1;
                    return;
                }
                setSync(0);
            }
            
            // ================================================================
            
            switch(ir){
                case 0x00:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress(pc);
                            } else {
                                getNextByte();
                            }
                            
                    }
                    mcCounter++;
                    break;
            }
            
        }
        
        protected byte getNextByte(){
            byte data = readBus();
            pc++;
            return data;
        }
        
        protected void flipState(){
            base.Outputs[28].On = !base.Outputs[28].On;
        }
        protected bool readPin(int pin){
            return base.Inputs[pin].On;
        }
        
        protected void setRW(int state){
            base.Outputs[RWPin].On = state != 0;
        }
        protected void setSync(int state){
            base.Outputs[syncPin].On = state != 0;
        }
        
        // Output data to address port
        protected void setAddress(ushort address){
            for(int i = 0; i < 16; i++){
                int state = (address>>i) & 1;
                base.Outputs[addressPin+i].On = state != 0;
            }
        }
        
        // Output data to data bus
        protected void setBus(byte data){
            for(int i = 0; i < 8; i++){
                int state = (data>>7-i) & 1;
                base.Outputs[dataPinW+i].On = state != 0;
            }
        }
        
        // Read the data bus
        protected byte readBus(){
            byte data = 0;
            for(int i = 0; i < 8; i++){
                data <<= 1;
                if(base.Inputs[dataPinR+i].On){
                    data |= 0x1;
                }
            }
            return data;
        }
    }
}