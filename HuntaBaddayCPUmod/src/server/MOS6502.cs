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
        
        // Pin name constants
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
        
        // List of instructions
        (int opcode, string instruction, string mode, int bytes)[] opcodes = new[]{
            (0x00, "brk", "impl", 0),
            
            (0x69, "adc", "imm", 1),
            (0x65, "adc", "zp", 1),
            (0x75, "adc", "zpx", 1),
            (0x6d, "adc", "abs", 2),
            (0x7d, "adc", "absx", 2),
            (0x79, "adc", "absy", 2),
            (0x61, "adc", "indx", 1),
            (0x71, "adc", "indy", 1),
            
            (0x29, "and", "imm", 1),
            (0x25, "and", "zp", 1),
            (0x35, "and", "zpx", 1),
            (0x2d, "and", "abs", 2),
            (0x3d, "and", "absx", 2),
            (0x39, "and", "absy", 2),
            (0x21, "and", "indx", 1),
            (0x31, "and", "indy", 1),
            
            (0x0a, "asl", "a", 0),
            (0x06, "asl", "zp", 1),
            (0x16, "asl", "zpx", 1),
            (0x0e, "asl", "abs", 2),
            (0x1e, "asl", "absx", 2),
            
            (0x90, "bcc", "rel", 1),
            (0xb0, "bcs", "rel", 1),
            (0xd0, "bne", "rel", 1),
            (0xf0, "beq", "rel", 1),
            (0x10, "bpl", "rel", 1),
            (0x30, "bmi", "rel", 1),
            (0x50, "bvc", "rel", 1),
            (0x70, "bvs", "rel", 1),
            
            (0x24, "bit", "zp", 1),
            (0x2c, "bit", "abs", 2),
            
            (0x18, "clc", "impl", 0),
            (0xd8, "cld", "impl", 0),
            (0x58, "cli", "impl", 0),
            (0xb8, "clv", "impl", 0),
            
            (0xc9, "cmp", "imm", 1),
            (0xc5, "cmp", "zp", 1),
            (0xd5, "cmp", "zpx", 1),
            (0xcd, "cmp", "abs", 2),
            (0xdd, "cmp", "absx", 2),
            (0xd9, "cmp", "absy", 2),
            (0xc1, "cmp", "indx", 1),
            (0xd1, "cmp", "indy", 1),
            
            (0xe0, "cpx", "imm", 1),
            (0xe4, "cpx", "zp", 1),
            (0xec, "cpx", "abs", 2),
            
            (0xc0, "cpy", "imm", 1),
            (0xc4, "cpy", "zp", 1),
            (0xcc, "cpy", "abs", 2),
            
            (0xc6, "dec", "zp", 1),
            (0xd6, "dec", "zpx", 1),
            (0xce, "dec", "abs", 2),
            (0xde, "dec", "absx", 2),
            
            (0xca, "dex", "impl", 0),
            (0x88, "dey", "impl", 0),
            
            (0x49, "eor", "imm", 1),
            (0x45, "eor", "zp", 1),
            (0x55, "eor", "zpx", 1),
            (0x4d, "eor", "abs", 2),
            (0x5d, "eor", "absx", 2),
            (0x59, "eor", "absy", 2),
            (0x41, "eor", "indx", 1),
            (0x51, "eor", "indy", 1),
            
            (0xe6, "inc", "zp", 1),
            (0xf6, "inc", "zpx", 1),
            (0xee, "inc", "abs", 2),
            (0xfe, "inc", "absx", 2),
            
            (0xe8, "inx", "impl", 0),
            (0xc8, "iny", "impl", 0),
            
            (0x4c, "jmp", "abs", 2),
            (0x6c, "jmpind", "ind", 2),
            
            (0x20, "jsr", "abs", 2),
            
            (0xa9, "lda", "imm", 1),
            (0xa5, "lda", "zp", 1),
            (0xb5, "lda", "zpx", 1),
            (0xad, "lda", "abs", 2),
            (0xbd, "lda", "absx", 2),
            (0xb9, "lda", "absy", 2),
            (0xa1, "lda", "indx", 1),
            (0xb1, "lda", "indy", 1),
            
            (0xa2, "ldx", "imm", 1),
            (0xa6, "ldx", "zp", 1),
            (0xb6, "ldx", "zpy", 1),
            (0xae, "ldx", "abs", 2),
            (0xbe, "ldx", "absy", 2),
            
            (0xa0, "ldy", "imm", 1),
            (0xa4, "ldy", "zp", 1),
            (0xb4, "ldy", "zpx", 1),
            (0xac, "ldy", "abs", 2),
            (0xbc, "ldy", "absx", 2),
            
            (0x4a, "lsr", "a", 0),
            (0x46, "lsr", "zp", 1),
            (0x56, "lsr", "zpx", 1),
            (0x4e, "lsr", "abs", 2),
            (0x5e, "lsr", "absx", 2),
            
            (0xea, "nop", "impl", 0),
            
            (0x09, "ora", "imm", 1),
            (0x05, "ora", "zp", 1),
            (0x15, "ora", "zpx", 1),
            (0x0d, "ora", "abs", 2),
            (0x1d, "ora", "absx", 2),
            (0x19, "ora", "absy", 2),
            (0x01, "ora", "indx", 1),
            (0x11, "ora", "indy", 1),
            
            (0x48, "pha", "impl", 0),
            (0x08, "php", "impl", 0),
            (0x68, "pla", "impl", 0),
            (0x28, "plp", "impl", 0),
            
            (0x2a, "rol", "a", 0),
            (0x26, "rol", "zp", 1),
            (0x36, "rol", "zpx", 1),
            (0x2e, "rol", "abs", 2),
            (0x3e, "rol", "absx", 2),
            
            (0x6a, "ror", "a", 0),
            (0x66, "ror", "zp", 1),
            (0x76, "ror", "zpx", 1),
            (0x6e, "ror", "abs", 2),
            (0x7e, "ror", "absx", 2),
            
            (0x40, "rti", "impl", 0),
            (0x60, "rts", "impl", 0),
            
            (0xe9, "sbc", "imm", 1),
            (0xe5, "sbc", "zp", 1),
            (0xf5, "sbc", "zpx", 1),
            (0xed, "sbc", "abs", 2),
            (0xfd, "sbc", "absx", 2),
            (0xf9, "sbc", "absy", 2),
            (0xe1, "sbc", "indx", 1),
            (0xf1, "sbc", "indy", 1),
            
            (0x38, "sec", "impl", 0),
            (0xf8, "sed", "impl", 0),
            (0x78, "sei", "impl", 0),
            
            (0x85, "sta", "zp", 1),
            (0x95, "sta", "zpx", 1),
            (0x8d, "sta", "abs", 2),
            (0x9d, "sta", "absx", 2),
            (0x99, "sta", "absy", 2),
            (0x81, "sta", "indx", 1),
            (0x81, "sta", "indy", 1),
            
            (0x86, "stx", "zp", 1),
            (0x96, "stx", "zpy", 1),
            (0x8e, "stx", "abs", 2),
            
            (0x84, "sty", "zp", 1),
            (0x94, "sty", "zpx", 1),
            (0x8c, "sty", "abs", 2),
            
            (0xaa, "tax", "impl", 0),
            (0xa8, "tay", "impl", 0),
            (0xba, "tsx", "impl", 0),
            (0x8a, "txa", "impl", 0),
            (0x9a, "txs", "impl", 0),
            (0x98, "tya", "impl", 0)
        };
        
        // Some state variables
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
        // 2 = IRQ
        // 3 = BRK
        
        // Registers
        byte ir = 0;
        string irInst = "";
        string irMode = "";
        int irAmt = 0;
        byte pcLo = 0;
        byte pcHi = 0;
        byte sp = 0;
        byte st = 0;
        
        byte acc = 0;
        byte indexX = 0;
        byte indexY = 0;
        
        int state = 0;
        // 0 - Fetch
        // 1+ - Execute
        int addrState = 0;
        int relState = 0;
        bool addressModeDone = false;
        
        byte DBL1 = 0;
        byte DBL2 = 0;
        byte ALUtmp = 0;
        byte tmp = 0;
        byte addrTmpLo = 0;
        byte addrTmpHi = 0;
        
        // Set this to latch data to a register on phi1
        string loadRegister = "";
        
        
        // Set this to end the instruction on next phi2
        bool endInstruction = false;
        
        protected override void DoLogicUpdate(){
            phi1 = false;
            phi2 = false;
            
            // Set the phi1 and phi2 pins
            base.Outputs[phi1Pin].On = !readPin(phi0Pin);
            base.Outputs[phi2Pin].On = readPin(phi0Pin);
            
            // Set overflow flag if the pin is low
            if(!readPin(setOFpin) && readPin(setOFpin) != lastOFpinState){
                st |= 0b01000000;
            }
            lastOFpinState = readPin(setOFpin);
            
            // Set phi1 and phi2 depending on if the clock turned off or on
            if(readPin(phi0Pin) && !lastClkState){
                phi2 = true;
            } else if(!readPin(phi0Pin) && lastClkState){
                phi1 = true;
            }
            lastClkState = readPin(phi0Pin);
            
            // Some instructions need to latch data from the data bus on the falling edge in the second clock phase.
            if(phi1){
                switch(loadRegister){
                    case "ir": ir = readBus();
                        break;
                    case "pclo": pcLo = readBus();
                        break;
                    case "pchi": pcHi = readBus();
                        break;
                    case "a": acc = readBus();
                        break;
                    case "x": indexX = readBus();
                        break;
                    case "y": indexY = readBus();
                        break;
                    case "dbl1": DBL1 = readBus();
                        break;
                    case "dbl2": DBL2 = readBus();
                        break;
                }
                loadRegister = "";
                setSync(false);
            }
            Logger.Info(acc.ToString());
            
            // Setup cpu for a reset
            if(!readPin(rstPin) && phi2){
                resetTriggerInt = true;
                resetTrigger = true;
                state = 0;
                return;
            } else if(phi1 && resetTrigger){
                resetTrigger = false;
                setRW(true);
                setSync(false);
                setAddress(0);
                setBus(0);
                base.Outputs[28].On = false;
                return;
            }
            
            // if state is 0, setup pins for a fetch
            if(state == 0 && phi1){
                setRW(true);
                setSync(true);
                setAddress16(pcLo, pcHi);
                setBus(0);
            }
            
            // Do fetch
            if(state == 0 && phi2){
                loadRegister = "ir";
                wasFetch = true;
                return;
            } else if(state == 0 && phi1 && wasFetch){
                // Force brk into ir if rst, irq or nmi was set
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
                } else {
                    interruptType = 3;
                    incrementPC();
                }
                int opcodeIndex = getOpcodeIndex(ir);
                if(opcodeIndex != -1){
                    irInst = opcodes[opcodeIndex].instruction;
                    irMode = opcodes[opcodeIndex].mode;
                    irAmt = opcodes[opcodeIndex].bytes;
                    state = 1;
                    addrState = 1;
                    addressModeDone = false;
                } else {
                    state = -1;
                    base.Outputs[28].On = true;
                }
                setSync(false);
                wasFetch = false;
            }
            
            // Dont do anything unless the clock clocked
            if(!phi1 && !phi2){
                return;
            }
            
            if(phi1){
                phi1exec();
            } else {
                phi2exec();
            }
            if(phi2 && endInstruction){
                state = 0;
                endInstruction = false;
            }
        }
        
        protected void phi1exec(){
            switch(irInst){
                case "brk":
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    if(interruptType == 3){
                        incrementPC();
                    }
                    break;
                    case 2:
                    setRW(false);
                    setAddress16(sp, 0x01);
                    setBus(pcHi);
                    sp--;
                    break;
                    case 3:
                    setAddress16(sp, 0x01);
                    setBus(pcLo);
                    sp--;
                    break;
                    case 4:
                    setAddress16(sp, 0x01);
                    if(interruptType == 3){
                        setBus((byte)(st|0b10000));
                    } else {
                        setBus(st);
                    }
                    sp--;
                    break;
                    case 5:
                    setRW(true);
                    setBus(0);
                    if(interruptType == 0){
                        setAddress(0xfffc);
                    } else if(interruptType == 1){
                        setAddress(0xfffa);
                    } else {
                        setAddress(0xfffe);
                    }
                    loadRegister = "pclo";
                    break;
                    case 6:
                    if(interruptType == 0){
                        setAddress(0xfffd);
                    } else if(interruptType == 1){
                        setAddress(0xfffb);
                    } else {
                        setAddress(0xffff);
                    }
                    loadRegister = "pchi";
                    endInstruction = true;
                    break;
                }
                if(interruptType == 0){
                    setRW(true);
                }
                break;
                case "nop":
                endInstruction = true;
                break;
                case "lda":
                if(doAddressMode()){
                    break;
                }
                loadRegister = "a";
                endInstruction = true;
                break;
                case "ldx":
                if(doAddressMode()){
                    break;
                }
                loadRegister = "x";
                endInstruction = true;
                break;
                case "ldy":
                if(doAddressMode()){
                    break;
                }
                loadRegister = "y";
                endInstruction = true;
                break;
                case "sta":
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(acc);
                endInstruction = true;
                break;
                case "stx":
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(indexX);
                endInstruction = true;
                break;
                case "sty":
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(indexY);
                endInstruction = true;
                break;
                case "jmp":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadRegister = "pchi";
                    pcLo = DBL1
                    endInstruction = true;
                    break;
                } break;
                case "jmpind":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadRegister = "dbl2";
                    break;
                    case 3:
                    setAddress16(DBL1, DBL2);
                    DBL1++;
                    if(DBL1 == 0){
                        DBL2++;
                    }
                    loadRegister = "pclo";
                    break;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    loadRegister = "pchi";
                    endInstruction = true;
                    break;
                } break;
            }
        }
        protected void phi2exec(){
            switch(irInst){
                
            }
            if(state != -1){
                state++;
                relState++;
            }
        }
        
        // Set the address mode
        protected bool doAddressMode(){
            if(addressModeDone){
                return false;
            }
            addressModeDone = true;
            relState = 0;
            switch(irMode){
                case "imm":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    return false;
                } break;
                case "zp":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    return false;
                } break;
                case "zpx":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1 += indexX;
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    return false;
                } break;
                case "abs":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadRegister = "dbl2";
                    break;
                    case 3:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case "absx":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadRegister = "dbl2";
                    break;
                    case 3:
                    DBL1 += indexX;
                    setAddress16(DBL1, DBL2);
                    if(DBL1+indexX > 0xff){
                        DBL2++;
                        break;
                    }
                    return false;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case "absy":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadRegister = "dbl2";
                    break;
                    case 3:
                    DBL1 += indexY;
                    setAddress16(DBL1, DBL2);
                    if(DBL1+indexY > 0xff){
                        DBL2++;
                        break;
                    }
                    return false;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case "indx":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(DBL1, 0)
                    DBL1 += indexX
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    DBL1++;
                    loadRegister = "dbl2";
                    break;
                    case 4:
                    setAddress16(DBL1, 0);
                    loadRegister = "dbl1";
                    break;
                    case 5:
                    setAddress16(DBL2, DBL1);
                    return false;
                } break;
                case "indy":
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadRegister = "dbl1";
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1++;
                    loadRegister = "dbl2";
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    loadRegister = "dbl1";
                    break;
                    case 4:
                    DBL2 += indexY;
                    setAddress16(DBL2, DBL1);
                    if(DBL2+indexY > 0xff){
                        DBL1++;
                        break;
                    }
                    return false;
                    case 5:
                    setAddress16(DBL2, DBL1);
                    return false;
                } break;
            }
            addressModeDone = false;
            addrState++;
            return true;
        }
        
        // Toggle debug output pin
        protected void flipState(){
            base.Outputs[28].On = !base.Outputs[28].On;
        }
        // Read a pin's state
        protected bool readPin(int pin){
            return base.Inputs[pin].On;
        }
        // Set the R/W pin
        protected void setRW(bool state){
            base.Outputs[RWPin].On = state;
        }
        // Set the sync pin
        protected void setSync(bool state){
            base.Outputs[syncPin].On = state;
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
        protected int getOpcodeIndex(byte opcode){
            for(int i = 0; i < opcodes.Length; i++){
                if(opcodes[i].opcode == (int)opcode){
                    return i;
                }
            }
            return -1;
        }
    }
}