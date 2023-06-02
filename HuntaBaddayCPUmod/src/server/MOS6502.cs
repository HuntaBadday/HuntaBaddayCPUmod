using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod
{
    public class MOS6502 : LogicComponent
    {
        // Reference for me to know what input and output id each pin is
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
        const int rdyPin = 1;
        const int setOFpin = 18;
        
        const int dataPinW = 20;
        const int dataPinR = 7;
        const int addressPin = 2;
        
        const int phi1Pin = 0;
        const int phi2Pin = 19;
        
        bool lastClkState = false;
        bool lastClkStateU = false;
        bool lastOFpinState = false;
        bool phi1 = false;
        bool phi2 = false;
        bool isPhi2 = false;
        bool resetTrigger = false;
        bool resetTriggerInt = false;
        bool wasFetch = false;
        int interruptType = 0;
        // 0 = RST
        // 1 = NMI
        // 2 = BRK/IRQ
        
        byte ir = 0;
        byte pcLo = 0;
        byte pcHi = 0;
        byte sp = 0;
        byte st = 0;
        
        byte acc = 0;
        byte indexX = 0;
        byte indexY = 0;
        
        float state = 0;
        // 0 - Fetch
        // 1+ - Execute
        
        byte DBL1 = 0;
        byte DBL2 = 0;
        byte ALUtmp = 0;
        byte tmp = 0;
        
        bool loadIr = false;
        bool loadPCL = false;
        bool loadPCH = false;
        bool loadAcc = false;
        bool loadIndexX = false;
        bool loadIndexY = false;
        bool loadDBL1 = false;
        bool loadDBL2 = false;
        
        protected override void DoLogicUpdate(){
            phi1 = false;
            phi2 = false;
            
            base.Outputs[phi1Pin].On = !base.Inputs[phi0Pin].On;
            base.Outputs[phi2Pin].On = base.Inputs[phi0Pin].On;
            
            if(!readPin(setOFpin) && readPin(setOFpin) != lastOFpinState){
                st |= 0b01000000;
            }
            lastOFpinState = readPin(setOFpin);
            
            if(readPin(phi0Pin) != lastClkStateU && readPin(phi0Pin) != lastClkState){
                if(readPin(rdyPin) || !base.Outputs[RWPin].On){
                    phi1 = !readPin(phi0Pin);
                    phi2 = readPin(phi0Pin);
                }
                lastClkStateU = readPin(phi0Pin);
            } else {
                lastClkStateU = readPin(phi0Pin);
                return;
            }
            lastClkState = readPin(phi0Pin);
            
            if(phi1){
                if(loadIr){
                    ir = readBus();
                } else if(loadPCL){
                    pcLo = readBus();
                } else if(loadPCH){
                    pcHi = readBus();
                } else if(loadAcc){
                    acc = readBus();
                } else if(loadIndexX){
                    indexX = readBus();
                } else if(loadIndexY){
                    indexY = readBus();
                } else if(loadDBL1){
                    DBL1 = readBus();
                } else if(loadDBL2){
                    DBL2 = readBus();
                }
                loadIr = false;
                loadPCL = false;
                loadPCH = false;
                loadAcc = false;
                loadIndexX = false;
                loadIndexY = false;
                loadDBL1 = false;
                loadDBL2 = false;
                setSync(0);
            }
            
            if(!readPin(rstPin) || phi2 && resetTrigger){
                resetTrigger = true;
                resetTriggerInt = true;
                state = 0;
                return;
            } else if(phi1 && resetTrigger){
                resetTrigger = false;
            }
            
            if(state == 0 && phi1){
                setRW(1);
                setSync(1);
                setAddress16(pcLo, pcHi);
                setBus(0);
                return;
            }
            
            if(state == 0 && phi2){
                if(resetTriggerInt || !readPin(irqPin) || !readPin(nmiPin)){
                    if(resetTriggerInt){
                        interruptType = 0;
                    } else if(!readPin(nmiPin)){
                        interruptType = 1;
                    } else if(!readPin(irqPin)){
                        interruptType = 2;
                    }
                    resetTriggerInt = false;
                    ir = 0;
                    state = 1;
                    wasFetch = false;
                } else {
                    interruptType = 2;
                    loadIr = true;
                    incrementPC();
                    state = 1;
                    wasFetch = true;
                }
                return;
            }
            
            // ================================================================
            
            switch(ir){
                // BRK
                case 0x00:
                    switch(state){
                        case 1:
                            if(wasFetch){
                                if(phi1){
                                    setAddress16(pcLo, pcHi);
                                } else {
                                    loadDBL1 = true;
                                    incrementPC();
                                }
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress((ushort)(0x100+sp));
                                setBus(pcHi);
                                setRW(0);
                                if(interruptType == 0){
                                    setRW(1);
                                }
                            } else {
                                sp--;
                            }
                            break;
                        case 3:
                            if(phi1){
                                setAddress((ushort)(0x100+sp));
                                setBus(pcLo);
                            } else {
                                sp--;
                            }
                            break;
                        case 4:
                            if(phi1){
                                setAddress((ushort)(0x100+sp));
                                byte tmp = st;
                                if(wasFetch){
                                    tmp |= 0b00010000;
                                }
                                setBus(tmp);
                            } else {
                                sp--;
                            }
                            break;
                        case 5:
                            if(phi1){
                                setRW(1);
                                setBus(0);
                                if(interruptType == 0){
                                    setAddress(0xfffc);
                                } else if(interruptType == 1){
                                    setAddress(0xfffa);
                                } else if(interruptType == 2){
                                    setAddress(0xfffe);
                                }
                            } else {
                                loadPCL = true;
                            }
                            break;
                        case 6:
                            if(phi1){
                                setRW(1);
                                if(interruptType == 0){
                                    setAddress(0xfffd);
                                } else if(interruptType == 1){
                                    setAddress(0xfffb);
                                } else if(interruptType == 2){
                                    setAddress(0xffff);
                                }
                            } else {
                                loadPCH = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                // LDA #
                case 0xa9:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadAcc = true;
                                incrementPC();
                                state = -1;
                            }
                            break;
                    }
                    break;
                // LDA zp
                case 0xa5:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress16(DBL1, 0);
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                // LDA zp,x
                case 0xb5:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                ALUtmp = (byte)(DBL1+indexX);
                            } else {
                                
                            }
                            break;
                        case 3:
                            if(phi1){
                                setAddress16(ALUtmp, 0);
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                //LDA abs
                case 0xad:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL2 = true;
                                incrementPC();
                            }
                            break;
                        case 3:
                            if(phi1){
                                setAddress16(DBL1, DBL2);
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                //LDA abs,x
                case 0xbd:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL2 = true;
                                incrementPC();
                            }
                            break;
                        case 3:
                            if(phi1){
                                if(DBL1+indexX >= 0x100){
                                    DBL1 += indexX;
                                    DBL2++;
                                } else {
                                    DBL1 += indexX;
                                    setAddress16(DBL1, DBL2);
                                    state = 2;
                                }
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                        case 4:
                            if(phi1){
                                setAddress16(DBL1, DBL2);
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                //LDA abs,x
                case 0xb9:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL2 = true;
                            }
                            break;
                        case 3:
                            if(phi1){
                                if(DBL1+indexY >= 0x100){
                                    DBL1 += indexY;
                                    DBL2++;
                                } else {
                                    DBL1 += indexY;
                                    setAddress16(DBL1, DBL2);
                                    state = 2;
                                }
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                        case 4:
                            if(phi1){
                                setAddress16(DBL1, DBL2);
                            } else {
                                loadAcc = true;
                                state = -1;
                            }
                            break;
                    }
                    break;
                // STA zp
                case 0x85:
                    switch(state){
                        case 1:
                            if(phi1){
                                setAddress16(pcLo, pcHi);
                            } else {
                                loadDBL1 = true;
                                incrementPC();
                            }
                            break;
                        case 2:
                            if(phi1){
                                setAddress16(DBL1, 0);
                                setBus(acc);
                                setRW(0);
                            } else {
                                state = -1;
                            }
                            break;
                    }
                    break;
                // NOP
                case 0xea:
                    if(phi2){
                        state = -1;
                    }
                    break;
            }
            if(phi2){
                state++;
            }
            
        }
        
        // Toggle debug output pin
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
        
        // Increment the pc
        protected void incrementPC(){
            if(pcLo + 1 >= 0x100){
                pcHi++;
            }
            pcLo++;
            
        }
        // Set address from 2 bytes
        protected void setAddress16(byte lo, byte hi){
            setAddress((ushort)(lo | (hi<<8)));
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