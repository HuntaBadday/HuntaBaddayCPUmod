using LogicAPI.Server.Components;
using System;

namespace HuntaBaddayCPUmod
{
    public class MOS6502 : LogicComponent
    {
        public override bool HasPersistentValues => true;
        // Reference to know what input and output id each pin is
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
        
        // Values for lookup
        const int imm = 0;
        const int zp = 1;
        const int zpx = 2;
        const int zpy = 3;
        const int abs = 4;
        const int absx = 5;
        const int absy = 6;
        const int indx = 7;
        const int indy = 8;
        const int impl = 9;
        const int addrAcc = 10;
        
        const int ADC = 0;
        const int AND = 1;
        const int ASL = 2;
        const int BCC = 3;
        const int BCS = 4;
        const int BEQ = 5;
        const int BIT = 6;
        const int BMI = 7;
        const int BNE = 8;
        const int BPL = 9;
        const int BRK = 10;
        const int BVC = 11;
        const int BVS = 12;
        const int CLC = 13;
        const int CLD = 14;
        const int CLI = 15;
        const int CLV = 16;
        const int CMP = 17;
        const int CPX = 18;
        const int CPY = 19;
        const int DEC = 20;
        const int DEX = 21;
        const int DEY = 22;
        const int EOR = 23;
        const int INC = 24;
        const int INX = 25;
        const int INY = 26;
        const int JMP = 27;
        const int JSR = 28;
        const int LDA = 29;
        const int LDX = 30;
        const int LDY = 31;
        const int LSR = 32;
        const int NOP = 33;
        const int ORA = 34;
        const int PHA = 35;
        const int PHP = 36;
        const int PLA = 37;
        const int PLP = 38;
        const int ROL = 39;
        const int ROR = 40;
        const int RTI = 41;
        const int RTS = 42;
        const int SBC = 43;
        const int SEC = 44;
        const int SED = 45;
        const int SEI = 46;
        const int STA = 47;
        const int STX = 48;
        const int STY = 49;
        const int TAX = 50;
        const int TAY = 51;
        const int TSX = 52;
        const int TXA = 53;
        const int TXS = 54;
        const int TYA = 55;
        
        const int JMPIND = 56;
        
        // List of instructions
        (int opcode, int instruction, int mode, int bytes)[] opcodes = new[]{
            (0x00, BRK, impl, 0),
            
            (0x69, ADC, imm, 1),
            (0x65, ADC, zp, 1),
            (0x75, ADC, zpx, 1),
            (0x6d, ADC, abs, 2),
            (0x7d, ADC, absx, 2),
            (0x79, ADC, absy, 2),
            (0x61, ADC, indx, 1),
            (0x71, ADC, indy, 1),
            
            (0x29, AND, imm, 1),
            (0x25, AND, zp, 1),
            (0x35, AND, zpx, 1),
            (0x2d, AND, abs, 2),
            (0x3d, AND, absx, 2),
            (0x39, AND, absy, 2),
            (0x21, AND, indx, 1),
            (0x31, AND, indy, 1),
            
            (0x0a, ASL, addrAcc, 0),
            (0x06, ASL, zp, 1),
            (0x16, ASL, zpx, 1),
            (0x0e, ASL, abs, 2),
            (0x1e, ASL, absx, 2),
            
            (0x90, BCC, imm, 1),
            (0xb0, BCS, imm, 1),
            (0xd0, BNE, imm, 1),
            (0xf0, BEQ, imm, 1),
            (0x10, BPL, imm, 1),
            (0x30, BMI, imm, 1),
            (0x50, BVC, imm, 1),
            (0x70, BVS, imm, 1),
            
            (0x24, BIT, zp, 1),
            (0x2c, BIT, abs, 2),
            
            (0x18, CLC, impl, 0),
            (0xd8, CLD, impl, 0),
            (0x58, CLI, impl, 0),
            (0xb8, CLV, impl, 0),
            
            (0xc9, CMP, imm, 1),
            (0xc5, CMP, zp, 1),
            (0xd5, CMP, zpx, 1),
            (0xcd, CMP, abs, 2),
            (0xdd, CMP, absx, 2),
            (0xd9, CMP, absy, 2),
            (0xc1, CMP, indx, 1),
            (0xd1, CMP, indy, 1),
            
            (0xe0, CPX, imm, 1),
            (0xe4, CPX, zp, 1),
            (0xec, CPX, abs, 2),
            
            (0xc0, CPY, imm, 1),
            (0xc4, CPY, zp, 1),
            (0xcc, CPY, abs, 2),
            
            (0xc6, DEC, zp, 1),
            (0xd6, DEC, zpx, 1),
            (0xce, DEC, abs, 2),
            (0xde, DEC, absx, 2),
            
            (0xca, DEX, impl, 0),
            (0x88, DEY, impl, 0),
            
            (0x49, EOR, imm, 1),
            (0x45, EOR, zp, 1),
            (0x55, EOR, zpx, 1),
            (0x4d, EOR, abs, 2),
            (0x5d, EOR, absx, 2),
            (0x59, EOR, absy, 2),
            (0x41, EOR, indx, 1),
            (0x51, EOR, indy, 1),
            
            (0xe6, INC, zp, 1),
            (0xf6, INC, zpx, 1),
            (0xee, INC, abs, 2),
            (0xfe, INC, absx, 2),
            
            (0xe8, INX, impl, 0),
            (0xc8, INY, impl, 0),
            
            (0x4c, JMP, abs, 2),
            (0x6c, JMPIND, abs, 2),
            
            (0x20, JSR, abs, 2),
            
            (0xa9, LDA, imm, 1),
            (0xa5, LDA, zp, 1),
            (0xb5, LDA, zpx, 1),
            (0xad, LDA, abs, 2),
            (0xbd, LDA, absx, 2),
            (0xb9, LDA, absy, 2),
            (0xa1, LDA, indx, 1),
            (0xb1, LDA, indy, 1),
            
            (0xa2, LDX, imm, 1),
            (0xa6, LDX, zp, 1),
            (0xb6, LDX, zpy, 1),
            (0xae, LDX, abs, 2),
            (0xbe, LDX, absy, 2),
            
            (0xa0, LDY, imm, 1),
            (0xa4, LDY, zp, 1),
            (0xb4, LDY, zpx, 1),
            (0xac, LDY, abs, 2),
            (0xbc, LDY, absx, 2),
            
            (0x4a, LSR, addrAcc, 0),
            (0x46, LSR, zp, 1),
            (0x56, LSR, zpx, 1),
            (0x4e, LSR, abs, 2),
            (0x5e, LSR, absx, 2),
            
            (0xea, NOP, impl, 0),
            
            (0x09, ORA, imm, 1),
            (0x05, ORA, zp, 1),
            (0x15, ORA, zpx, 1),
            (0x0d, ORA, abs, 2),
            (0x1d, ORA, absx, 2),
            (0x19, ORA, absy, 2),
            (0x01, ORA, indx, 1),
            (0x11, ORA, indy, 1),
            
            (0x48, PHA, impl, 0),
            (0x08, PHP, impl, 0),
            (0x68, PLA, impl, 0),
            (0x28, PLP, impl, 0),
            
            (0x2a, ROL, addrAcc, 0),
            (0x26, ROL, zp, 1),
            (0x36, ROL, zpx, 1),
            (0x2e, ROL, abs, 2),
            (0x3e, ROL, absx, 2),
            
            (0x6a, ROR, addrAcc, 0),
            (0x66, ROR, zp, 1),
            (0x76, ROR, zpx, 1),
            (0x6e, ROR, abs, 2),
            (0x7e, ROR, absx, 2),
            
            (0x40, RTI, impl, 0),
            (0x60, RTS, impl, 0),
            
            (0xe9, SBC, imm, 1),
            (0xe5, SBC, zp, 1),
            (0xf5, SBC, zpx, 1),
            (0xed, SBC, abs, 2),
            (0xfd, SBC, absx, 2),
            (0xf9, SBC, absy, 2),
            (0xe1, SBC, indx, 1),
            (0xf1, SBC, indy, 1),
            
            (0x38, SEC, impl, 0),
            (0xf8, SED, impl, 0),
            (0x78, SEI, impl, 0),
            
            (0x85, STA, zp, 1),
            (0x95, STA, zpx, 1),
            (0x8d, STA, abs, 2),
            (0x9d, STA, absx, 2),
            (0x99, STA, absy, 2),
            (0x81, STA, indx, 1),
            (0x81, STA, indy, 1),
            
            (0x86, STX, zp, 1),
            (0x96, STX, zpy, 1),
            (0x8e, STX, abs, 2),
            
            (0x84, STY, zp, 1),
            (0x94, STY, zpx, 1),
            (0x8c, STY, abs, 2),
            
            (0xaa, TAX, impl, 0),
            (0xa8, TAY, impl, 0),
            (0xba, TSX, impl, 0),
            (0x8a, TXA, impl, 0),
            (0x9a, TXS, impl, 0),
            (0x98, TYA, impl, 0)
        };
        
        // Some state variables
        bool lastClkState = false;
        bool pause = false;
        bool lastRdy = false;
        bool lastOFpinState = false;
        bool phi1 = false;
        bool phi2 = false;
        bool isPhi2 = false;
        bool resetTrigger = false;
        bool resetTriggerInt = false;
        bool wasFetch = false;
        bool insideInterrupt = false;
        int interruptType = 0;
        // 0 = RST
        // 1 = NMI
        // 2 = IRQ
        // 3 = BRK
        
        // Registers
        byte ir = 0;
        int irInst = -1;
        int irMode = 0;
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
        bool addressModeAcc = false;
        
        byte DBL1 = 0;
        byte DBL2 = 0;
        
        // Set one of these to latch it after phi2
        bool loadIr = false;
        bool loadPCL = false;
        bool loadPCH = false;
        bool loadA = false;
        bool loadX = false;
        bool loadY = false;
        bool loadSt = false;
        bool loadDBL1 = false;
        bool loadDBL2 = false;
        
        // Set this to end the instruction on next phi2
        bool endInstruction = false;
        
        protected override void DoLogicUpdate(){
            phi1 = false;
            phi2 = false;
            
            // Set the phi1 and phi2 pins
            base.Outputs[phi1Pin].On = !readPin(phi0Pin);
            base.Outputs[phi2Pin].On = readPin(phi0Pin);
            
            // Set overflow flag if the pin is low
            if(!readPin(setOFpin) && lastOFpinState){
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
                if(loadIr){
                    ir = readBus();
                } else if(loadPCL){
                    pcLo = readBus();
                } else if(loadPCH){
                    pcHi = readBus();
                } else if(loadA){
                    acc = readBus();
                    statusZN(acc);
                } else if(loadX){
                    indexX = readBus();
                    statusZN(indexX);
                } else if(loadY){
                    indexY = readBus();
                    statusZN(indexY);
                } else if(loadSt){
                    st = (byte)(readBus()&0xcf);
                } else if(loadDBL1){
                    DBL1 = readBus();
                } else if(loadDBL2){
                    DBL2 = readBus();
                }
                loadIr = false;
                loadPCL = false;
                loadPCH = false;
                loadA = false;
                loadX = false;
                loadY = false;
                loadSt = false;
                loadDBL1 = false;
                loadDBL2 = false;
                setSync(false);
                //Logger.Info("A: "+acc.ToString("X2")+" X: "+indexX.ToString("X2")+" Y: "+indexY.ToString("X2")+" PC: "+pcHi.ToString("X2")+pcLo.ToString("X2")+" D1: "+DBL1.ToString("X2")+" D2: "+DBL2.ToString("X2"));
            }
            
            // Setup cpu for a reset
            if(!readPin(rstPin) && phi2){
                resetTriggerInt = true;
                resetTrigger = true;
                insideInterrupt = false;
                state = 0;
                wasFetch = false;
                st |= 0x04;
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
            
            // Pause CPU when RDY pin is low
            if(phi1 && !readPin(rdyPin)){
                pause = true;
            } else if(phi1 && readPin(rdyPin)){
                pause = false;
            }
            if(pause){
                return;
            }
            
            // if state is 0, setup pins for a fetch
            if(state == 0 && phi1 && !wasFetch){
                fetchExec();
                setRW(true);
                setSync(true);
                setAddress16(pcLo, pcHi);
                setBus(0);
            }
            
            // Do fetch
            if(state == 0 && phi2){
                loadIr = true;
                wasFetch = true;
                return;
            } else if(state == 0 && phi1 && wasFetch){
                // Force brk into ir if rst, irq or nmi was set
                if(resetTriggerInt || (!readPin(irqPin)&&(st&0x04)==0 || !readPin(nmiPin))&&!insideInterrupt){
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
                setAddress16(pcLo, pcHi);
            }
            
            // Dont do anything unless the clock clocked
            if(!phi1 && !phi2){
                return;
            }
            // Don't execute if the state isn't in execute mode
            if(state == 0){
                return;
            }
            
            if(phi1){
                phi1exec();
            } else {
                if(state != -1){
                    state++;
                    relState++;
                }
            }
            if(phi2 && endInstruction){
                state = 0;
                endInstruction = false;
            }
        }
        
        // Main instruction execution
        protected void phi1exec(){
            switch(irInst){
                case BRK:
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
                    loadPCL = true;
                    break;
                    case 6:
                    if(interruptType == 0){
                        setAddress(0xfffd);
                    } else if(interruptType == 1){
                        setAddress(0xfffb);
                    } else {
                        setAddress(0xffff);
                    }
                    loadPCH = true;
                    if(interruptType != 0){
                        insideInterrupt = true;
                    }
                    endInstruction = true;
                    break;
                }
                if(interruptType == 0){
                    setRW(true);
                }
                break;
                case NOP:
                endInstruction = true;
                break;
                case LDA:
                if(doAddressMode()){
                    break;
                }
                loadA = true;
                endInstruction = true;
                break;
                case LDX:
                if(doAddressMode()){
                    break;
                }
                loadX = true;
                endInstruction = true;
                break;
                case LDY:
                if(doAddressMode()){
                    break;
                }
                loadY = true;
                endInstruction = true;
                break;
                case STA:
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(acc);
                endInstruction = true;
                break;
                case STX:
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(indexX);
                endInstruction = true;
                break;
                case STY:
                if(doAddressMode()){
                    break;
                }
                setRW(false);
                setBus(indexY);
                endInstruction = true;
                break;
                case JMP:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadPCH = true;
                    pcLo = DBL1;
                    endInstruction = true;
                    break;
                } break;
                case JMPIND:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadDBL2 = true;
                    break;
                    case 3:
                    setAddress16(DBL1, DBL2);
                    DBL1++;
                    if(DBL1 == 0){
                        DBL2++;
                    }
                    loadPCL = true;
                    break;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    loadPCH = true;
                    endInstruction = true;
                    break;
                } break;
                case ADC:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case SBC:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case SEC:
                st |= 0b00000001;
                endInstruction = true;
                break;
                case CLC:
                st &= 0b11111110;
                endInstruction = true;
                break;
                case AND:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case ASL:
                if(doAddressMode()){
                    break;
                }
                if(addressModeAcc){
                    clearCarry();
                    if((acc&0x80) != 0){
                        setCarry();
                    }
                    acc <<= 1;
                    statusZN(acc);
                    endInstruction = true;
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    clearCarry();
                    if((DBL1&0x80) != 0){
                        setCarry();
                    }
                    DBL1 <<= 1;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case BCC:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x01) != 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BCS:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x01) == 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BNE:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x02) != 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BEQ:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x02) == 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BPL:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x80) != 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BMI:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x80) == 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BVC:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x40) != 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BVS:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    if((st&0x40) == 0){
                        endInstruction = true;
                        break;
                    }
                    loadDBL1 = true;
                    break;
                    case 1:
                    int tmp = pcLo;
                    if((DBL1&0x80) != 0){
                        DBL1 ^= 0xff;
                        DBL1 += 1;
                        tmp -= (int)DBL1;
                    } else {
                        tmp += (int)DBL1;
                    }
                    pcLo = (byte)tmp;
                    if(tmp > 0xff){
                        pcHi++;
                    } else if(tmp < 0){
                        pcHi--;
                    } else {
                        endInstruction = true;
                    }
                    break;
                    case 2:
                    endInstruction = true;
                    break;
                } break;
                case BIT:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case CLD:
                st &= 0b11110111;
                endInstruction = true;
                break;
                case CLI:
                st &= 0b11111011;
                endInstruction = true;
                break;
                case CLV:
                st &= 0b10111111;
                endInstruction = true;
                break;
                case CMP:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case CPX:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case CPY:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case DEC:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    DBL1--;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case DEX:
                indexX--;
                statusZN(indexX);
                endInstruction = true;
                break;
                case DEY:
                indexY--;
                statusZN(indexY);
                endInstruction = true;
                break;
                case EOR:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case INC:
                if(doAddressMode()){
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    DBL1++;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case INX:
                indexX++;
                statusZN(indexX);
                endInstruction = true;
                break;
                case INY:
                indexY++;
                statusZN(indexY);
                endInstruction = true;
                break;
                case JSR:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    sp--;
                    break;
                    case 3:
                    setRW(false);
                    setBus(pcHi);
                    break;
                    case 4:
                    setAddress16(sp, 0x01);
                    sp--;
                    setBus(pcLo);
                    break;
                    case 5:
                    setBus(0);
                    setRW(true);
                    setAddress16(pcLo, pcHi);
                    pcLo = DBL1;
                    loadPCH = true;
                    endInstruction = true;
                    break;
                } break;
                case LSR:
                if(doAddressMode()){
                    break;
                }
                if(addressModeAcc){
                    clearCarry();
                    if((acc&0x01) != 0){
                        setCarry();
                    }
                    acc >>= 1;
                    statusZN(acc);
                    endInstruction = true;
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    clearCarry();
                    if((DBL1&0x01) != 0){
                        setCarry();
                    }
                    DBL1 >>= 1;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case ORA:
                if(doAddressMode()){
                    break;
                }
                loadDBL1 = true;
                endInstruction = true;
                break;
                case ROL:
                if(doAddressMode()){
                    break;
                }
                if(addressModeAcc){
                    byte carry = (byte)(st&0x1);
                    clearCarry();
                    if((acc&0x80) != 0){
                        setCarry();
                    }
                    acc <<= 1;
                    acc |= carry;
                    statusZN(acc);
                    endInstruction = true;
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    byte carry = (byte)(st&0x1);
                    clearCarry();
                    if((DBL1&0x80) != 0){
                        setCarry();
                    }
                    DBL1 <<= 1;
                    DBL1 |= carry;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case ROR:
                if(doAddressMode()){
                    break;
                }
                if(addressModeAcc){
                    byte carry = (byte)(st<<7);
                    clearCarry();
                    if((acc&0x01) != 0){
                        setCarry();
                    }
                    acc >>= 1;
                    acc |= carry;
                    statusZN(acc);
                    endInstruction = true;
                    break;
                }
                switch(relState){
                    case 0:
                    loadDBL1 = true;
                    break;
                    case 2:
                    byte carry = (byte)(st<<7);
                    clearCarry();
                    if((DBL1&0x01) != 0){
                        setCarry();
                    }
                    DBL1 >>= 1;
                    DBL1 |= carry;
                    statusZN(DBL1);
                    setBus(DBL1);
                    setRW(false);
                    endInstruction = true;
                    break;
                } break;
                case RTS:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    break;
                    case 3:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadPCL = true;
                    break;
                    case 4:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadPCH = true;
                    break;
                    case 5:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    endInstruction = true;
                    break;
                } break;
                case RTI:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    break;
                    case 3:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadSt = true;
                    break;
                    case 4:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadPCL = true;
                    break;
                    case 5:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadPCH = true;
                    insideInterrupt = false;
                    endInstruction = true;
                    break;
                } break;
                case PHA:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    sp--;
                    setRW(false);
                    setBus(acc);
                    endInstruction = true;
                    break;
                } break;
                case PHP:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    sp--;
                    setRW(false);
                    setBus((byte)(st |= 0x30));
                    endInstruction = true;
                    break;
                } break;
                case PLA:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    break;
                    case 3:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadA = true;
                    endInstruction = true;
                    break;
                } break;
                case PLP:
                switch(state){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    break;
                    case 2:
                    setAddress16(sp, 0x01);
                    break;
                    case 3:
                    sp++;
                    setAddress16(sp, 0x01);
                    loadSt = true;
                    endInstruction = true;
                    break;
                } break;
                case SED:
                st |= 0x08;
                endInstruction = true;
                break;
                case SEI:
                st |= 0x04;
                endInstruction = true;
                break;
                case TAX:
                indexX = acc;
                statusZN(indexX);
                endInstruction = true;
                break;
                case TAY:
                indexY = acc;
                statusZN(indexY);
                endInstruction = true;
                break;
                case TSX:
                indexX = sp;
                statusZN(indexX);
                endInstruction = true;
                break;
                case TXA:
                acc = indexX;
                statusZN(acc);
                endInstruction = true;
                break;
                case TXS:
                sp = indexX;
                endInstruction = true;
                break;
                case TYA:
                acc = indexY;
                statusZN(acc);
                endInstruction = true;
                break;
            }
        }
        
        // Some instructions overlap the final step with the fetch cycle
        protected void fetchExec(){
            switch(irInst){
                case ADC:
                    acc = statusCO(acc, DBL1);
                    statusZN(acc);
                    break;
                case SBC:
                    DBL1 = (byte)(~DBL1);
                    acc = statusCO(acc, DBL1);
                    statusZN(acc);
                    break;
                case AND:
                    acc &= DBL1;
                    statusZN(acc);
                    break;
                case BIT:
                    byte tmp = (byte)(acc&DBL1);
                    statusZN(tmp);
                    st &= 0x3f;
                    st |= (byte)(DBL1&0xc0);
                    break;
                case CMP:
                    compare(acc, DBL1);
                    break;
                case CPX:
                    compare(indexX, DBL1);
                    break;
                case CPY:
                    compare(indexY, DBL1);
                    break;
                case EOR:
                    acc ^= DBL1;
                    statusZN(acc);
                    break;
                case ORA:
                    acc |= DBL1;
                    statusZN(acc);
                    break;
            }
        }
        
        // Do the addressing mode operations
        protected bool doAddressMode(){
            if(addressModeDone){
                return false;
            }
            addressModeDone = true;
            addressModeAcc = false;
            relState = 0;
            switch(irMode){
                case addrAcc:
                addressModeAcc = true;
                return false;
                break;
                case imm:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    return false;
                } break;
                case zp:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    return false;
                } break;
                case zpx:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1 += indexX;
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    return false;
                } break;
                case zpy:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1 += indexY;
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    return false;
                } break;
                case abs:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadDBL2 = true;
                    break;
                    case 3:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case absx:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadDBL2 = true;
                    break;
                    case 3:
                    int n = DBL1+indexX;
                    DBL1 = (byte)n;
                    setAddress16(DBL1, DBL2);
                    if(n > 0xff){
                        DBL2++;
                        break;
                    }
                    return false;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case absy:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    loadDBL2 = true;
                    break;
                    case 3:
                    int n = DBL1+indexY;
                    DBL1 = (byte)n;
                    setAddress16(DBL1, DBL2);
                    if(n > 0xff){
                        DBL2++;
                        break;
                    }
                    return false;
                    case 4:
                    setAddress16(DBL1, DBL2);
                    return false;
                } break;
                case indx:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1 += indexX;
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    DBL1++;
                    loadDBL2 = true;
                    break;
                    case 4:
                    setAddress16(DBL1, 0);
                    loadDBL1 = true;
                    break;
                    case 5:
                    setAddress16(DBL2, DBL1);
                    return false;
                } break;
                case indy:
                switch(addrState){
                    case 1:
                    setAddress16(pcLo, pcHi);
                    incrementPC();
                    setBus(0);
                    loadDBL1 = true;
                    break;
                    case 2:
                    setAddress16(DBL1, 0);
                    DBL1++;
                    loadDBL2 = true;
                    break;
                    case 3:
                    setAddress16(DBL1, 0);
                    loadDBL1 = true;
                    break;
                    case 4:
                    int n = DBL2+indexY;
                    DBL2 = (byte)n;
                    setAddress16(DBL2, DBL1);
                    if(n > 0xff){
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
        
        protected void compare(byte v1, byte v2){
            byte tv2 = (byte)((~v2) + 1);
            int tmp = v1+tv2;
            if(tmp > 0xff){
                st |= 0b00000001;
            } else {
                st &= 0b11111110;
            }
            statusZN((byte)tmp);
        }
        protected void statusZN(byte value){
            if(value == 0){
                st |= 0b00000010;
            } else {
                st &= 0b11111101;
            }
            st &= 0b01111111;
            st |= (byte)(value&0x80);
        }
        protected byte statusCO(byte v1, byte v2){
            if((st&0x04) != 0 && irInst == SBC){
                v2 = (byte)(0x99 - (~v2));
            }
            int output = v1+v2+(st&0x1);
            if((st&0x04) != 0){
                int bcdCarry = 0;
                if((v1&0x0f)+(v2&0x0f)+(st&0x1) > 0x09){
                    output += 0x06;
                    bcdCarry = 1;
                }
                if((v1&0xf0)+(v2&0xf0)+bcdCarry > 0x90){
                    output += 0x60;
                }
            }
            if((v1&0x80) == 0 && (v1&0x80) == 0 && (output & 0x80) != 0){
                st |= 0b01000000;
            } else if((v1&0x80) != 0 && (v1&0x80) != 0 && (output & 0x80) == 0){
                st |= 0b01000000;
            } else {
                st &= 0b10111111;
            }
            if(output > 0xff){
                st |= 0b00000001;
            } else {
                st &= 0b11111110;
            }
            return (byte)output;
            
        }
        protected void setCarry(){
            st |= 0x1;
        }
        protected void clearCarry(){
            st &= 0xfe;
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
        
        protected override byte[] SerializeCustomData(){
            // All saved variables:
            /*
            // Some states
            bool lastClkState = false;
            bool pause = false;
            bool lastRdy = false;
            bool lastOFpinState = false;
            bool phi1 = false;
            bool phi2 = false;
            bool isPhi2 = false;
            bool resetTrigger = false;
            bool resetTriggerInt = false;
            bool wasFetch = false;
            bool insideInterrupt = false;
            int interruptType = 0;
            
            // Registers
            byte ir = 0;
            string irInst = -1; // 3 bytes always (NOT SAVED)
            int irMode = 0;
            int irAmt = 0;
            byte pcLo = 0;
            byte pcHi = 0;
            byte sp = 0;
            byte st = 0;
            
            byte acc = 0;
            byte indexX = 0;
            byte indexY = 0;
            
            int state = 0;
            int addrState = 0;
            int relState = 0;
            bool addressModeDone = false;
            bool addressModeAcc = false;
            
            byte DBL1 = 0;
            byte DBL2 = 0;
            bool loadIr = false;
            bool loadPCL = false;
            bool loadPCH = false;
            bool loadA = false;
            bool loadX = false;
            bool loadY = false;
            bool loadSt = false;
            bool loadDBL1 = false;
            bool loadDBL2 = false;
            bool endInstruction = false;
            */
            
            byte[] data = new byte[38];
            data[0] = Convert.ToByte(lastClkState);
            data[1] = Convert.ToByte(pause);
            data[2] = Convert.ToByte(lastRdy);
            data[3] = Convert.ToByte(lastOFpinState);
            data[4] = Convert.ToByte(phi1);
            data[5] = Convert.ToByte(phi2);
            data[6] = Convert.ToByte(isPhi2);
            data[7] = Convert.ToByte(resetTrigger);
            data[8] = Convert.ToByte(resetTriggerInt);
            data[9] = Convert.ToByte(wasFetch);
            data[10] = Convert.ToByte(insideInterrupt);
            data[11] = (byte)interruptType; // Never more than a byte
            
            data[12] = (byte)irMode; // Never more than a byte
            data[13] = (byte)irAmt; // Never more than a byte
            data[14] = pcLo;
            data[15] = pcHi;
            data[16] = sp;
            data[17] = st;
            
            data[18] = acc;
            data[19] = indexX;
            data[20] = indexY;
            
            data[21] = (byte)state; // Never more than a byte
            data[22] = (byte)addrState; // Never more than a byte
            data[23] = (byte)relState; // Never more than a byte
            
            data[24] = Convert.ToByte(addressModeDone);
            data[25] = Convert.ToByte(addressModeAcc);
            
            data[26] = DBL1;
            data[27] = DBL2;
            data[28] = Convert.ToByte(loadIr);
            data[29] = Convert.ToByte(loadPCL);
            data[30] = Convert.ToByte(loadPCH);
            data[31] = Convert.ToByte(loadA);
            data[32] = Convert.ToByte(loadX);
            data[33] = Convert.ToByte(loadY);
            data[34] = Convert.ToByte(loadSt);
            data[35] = Convert.ToByte(loadDBL1);
            data[36] = Convert.ToByte(loadDBL2);
            data[37] = Convert.ToByte(endInstruction);
            
            return data;
        }
        
        protected override void DeserializeData(byte[] data){
            if(data == null){
                // New object
                return;
            } else if(data.Length != 38){
                // Bad data
                return;
            }
            
            lastClkState = Convert.ToBoolean(data[0]);
            pause = Convert.ToBoolean(data[1]);
            lastRdy = Convert.ToBoolean(data[2]);
            lastOFpinState = Convert.ToBoolean(data[3]);
            phi1 = Convert.ToBoolean(data[4]);
            phi2 = Convert.ToBoolean(data[5]);
            isPhi2 = Convert.ToBoolean(data[6]);
            resetTrigger = Convert.ToBoolean(data[7]);
            resetTriggerInt = Convert.ToBoolean(data[8]);
            wasFetch = Convert.ToBoolean(data[9]);
            insideInterrupt = Convert.ToBoolean(data[10]);
            interruptType = (int)data[11];
            
            irMode = (int)data[12];
            irAmt = (int)data[13];
            pcLo = data[14];
            pcHi = data[15];
            sp = data[16];
            st = data[17];
            
            acc = data[18];
            indexX = data[19];
            indexY = data[20];
            
            state = (int)data[21];
            addrState = (int)data[22];
            relState = (int)data[23];
            
            addressModeDone = Convert.ToBoolean(data[24]);
            addressModeAcc = Convert.ToBoolean(data[25]);
            
            DBL1 = data[26];
            DBL2 = data[27];
            loadIr = Convert.ToBoolean(data[28]);
            loadPCL = Convert.ToBoolean(data[29]);
            loadPCH = Convert.ToBoolean(data[30]);
            loadA = Convert.ToBoolean(data[31]);
            loadX = Convert.ToBoolean(data[32]);
            loadY = Convert.ToBoolean(data[33]);
            loadSt = Convert.ToBoolean(data[34]);
            loadDBL1 = Convert.ToBoolean(data[35]);
            loadDBL2 = Convert.ToBoolean(data[36]);
            endInstruction = Convert.ToBoolean(data[37]);
        }
    }
}