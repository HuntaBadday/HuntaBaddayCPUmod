using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod
{
    public class LWC31 : LogicComponent
    {
        // Define constants for each instruction and operation
        const int BRK = 0;
        const int JMP = 1;
        const int MOV = 2;
        const int LOD = 3;
        const int STO = 4;
        const int ALU = 5;
        const int CMP = 6;
        const int JIF = 7;
        const int NOP = 8;
        const int INT = 9;
        const int PLP = 10;
        const int PUSH = 11;
        const int POP = 12;
        const int JSR = 13;
        const int RTS = 14;
        const int RTI = 15;
        
        const int ADD = 0;
        const int ADC = 1;
        const int SUB = 2;
        const int SBC = 3;
        const int SHL = 4;
        const int ROL = 5;
        const int SHR = 6;
        const int ROR = 7;
        const int AND = 8;
        const int OR = 9;
        const int XOR = 10;
        
        // Array for how many operands each instruction uses
        int[] opNums = {
            0, 1, 2, 2, 2, 2, 2, 1, 0, 1, 0, 1, 0, 1, 0, 0
        };
        
        // All instructions that can have turbo
        int[] perfBypass = {
            0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0
        };
        
        // All pin numbers
        static int addressPin = 0;
        static int WritePin = 16;
        static int ReadPin = 17;
        static int dataPinW = 18;
        static int dataPinR = 0;
        static int rstPin = 16;
        static int clkPin = 17;
        static int useBusPin = 34;
        static int disableBusPin = 18;
        static int interruptPin = 19;
        static int turboPin = 20;
        
        // Program counter and instruction register
        ushort pc = 0;
        ushort ir = 0;
        
        // CPU state
        ushort CPUstate = 0;
        // 0 - Initialize (load start address from 0xffff)
        // 1 - Load next instruction
        // 2 - Execute
        
        // Execution phase
        ushort execPhase = 1;
        
        // Array of registers
        ushort[] registers = {0, 0, 0, 0, 0, 0, 0, 0};
        
        // Variables that the instruction get separated into
        int inst = 0;
        int op1 = 0;
        int op2 = 0;
        int op3 = 0;
        
        // Interrupt address set by int instruction
        ushort interruptAddr = 0;
        
        // This gets set to true when the code is executing inside an interrupt
        // so it doesn't keep triggering.
        bool insideInt = false;
        
        // Last state of clock
        bool lastClkState = false;
        
        protected override void DoLogicUpdate(){
            // Setup reset configuration
            if(readPin(rstPin)){
                setAddress(0xffff);
                setBus(0);
                setWrite(0);
                setRead(1);
                setUB(1);
                registers[7] |= 0b1000;
                CPUstate = 0;
                lastClkState = readPin(clkPin);
                insideInt = false;
                return;
            }
            // Check if the logic update was the clock
            if(readPin(clkPin) == lastClkState){
                return;
            }
            lastClkState = readPin(clkPin);
            
            // Reset phase
            if(CPUstate == 0 && readPin(clkPin)){
                CPUstate = 1;
                pc = readBus();
                setAddress(pc);
                return;
            }
            
            // Fetech phase
            if(CPUstate == 1 && readPin(clkPin)){
                CPUstate = 2;
                // If the interrupt pin is enabled inject a "brk" instruction to the instruction register
                if(readPin(interruptPin) && !insideInt && (registers[7] & 0b1000) == 0){
                    ir = 0;
                } else {
                    ir = readBus();
                    pc++;
                }
                setAddress(pc);
                return;
            }
            // Load the constant data for instructions and turn off all pins used
            if(CPUstate == 2 && !readPin(clkPin)){
                CPUstate = 3;
                registers[0] = readBus();
                setAddress(0);
                setRead(0);
                setUB(0);
                execPhase = 1;
            }
            
            // Execute first phase of instruction
            if(CPUstate == 3 && execPhase == 1){
                // 0000 000 000 0000 00
                inst = (ir >> 12) & 0b1111;
                op1 = (ir >> 9) & 0b111;
                op2 = (ir >> 6) & 0b111;
                op3 = (ir >> 2) & 0b1111;
                execPhase1();   // Do the phase
                
                // If the instruction supports turbo then skip the second phase
                if(readPin(turboPin) && perfBypass[inst] == 1){
                    setupLoad();
                } else {
                    execPhase = 2;
                }
                return;
            }
            // Execute phase 2
            if(CPUstate == 3 && execPhase == 2 && readPin(clkPin)){
                execPhase2();
                execPhase = 3;
                return;
            }
            // Phase 3 is just to check the falling-edge of the clock to initialize the next fetch.
            if(CPUstate == 3 && execPhase == 3 && !readPin(clkPin)){
                setupLoad();
                return;
            }
        }
        
        protected void execPhase1(){
            // If constant data is used then incrment the pc
            // so the constant data isn't executed.
            ushort inc = 0;
            if(opNums[inst] >= 1){
                if(op1 == 0){
                    inc = 1;
                }
            }
            if(opNums[inst] == 2){
                if(op2 == 0){
                    inc = 1;
                }
            }
            pc += inc;
            
            // Instruction execution
            switch(inst){
                case BRK:
                    registers[6]--;
                    setAddress(registers[6]);
                    setBus(pc);
                    setUB(1);
                    pc = interruptAddr;
                    insideInt = true;
                    break;
                case JMP:
                    pc = registers[op1];
                    break;
                case MOV:
                    registers[op1] = registers[op2];
                    genStatus(registers[op1]);
                    break;
                case LOD:
                    if((op3 & 0b1000) == 0b1000){
                        setAddress((ushort)(registers[op2]+registers[op3&0b111]));
                    } else {
                        setAddress(registers[op2]);
                    }
                    setRead(1);
                    setUB(1);
                    break;
                case STO:
                    if((op3 & 0b1000) == 0b1000){
                        setAddress((ushort)(registers[op2]+registers[op3&0b111]));
                    } else {
                        setAddress(registers[op2]);
                    }
                    setBus(registers[op1]);
                    setUB(1);
                    break;
                case ALU:
                    ushort preVal = registers[op1];
                    int tmp;
                    // Subset instructions of ALU
                    switch(op3){
                        case ADD:
                            tmp = registers[op1] + registers[op2];
                            registers[op1] = (ushort)tmp;
                            genCarry(tmp);
                            break;
                        case ADC:
                            tmp = registers[op1] + registers[op2] + (registers[7]&1);
                            registers[op1] = (ushort)tmp;
                            genCarry(tmp);
                            break;
                        case SUB:
                            tmp = registers[op1] + (registers[op2]^0xffff) + 1;
                            registers[op1] = (ushort)tmp;
                            genCarry(tmp);
                            break;
                        case SBC:
                            tmp = registers[op1] + (registers[op2]^0xffff) + (registers[7]&1);
                            registers[op1] = (ushort)tmp;
                            genCarry(tmp);
                            break;
                        case SHL:
                            registers[op1] = (ushort)(registers[op1] << 1);
                            registers[7] = (ushort)((registers[7]&0xfffe) | (preVal >> 15));
                            break;
                        case ROL:
                            registers[op1] = (ushort)((registers[op1] << 1) | (registers[7] & 0x1));
                            registers[7] = (ushort)((registers[7]&0xfffe) | (preVal >> 15));
                            break;
                        case SHR:
                            registers[op1] = (ushort)(registers[op1] >> 1);
                            registers[7] = (ushort)((registers[7]&0xfffe) | (preVal & 0x1));
                            break;
                        case ROR:
                            registers[op1] = (ushort)((registers[op1] >> 1) | ((registers[7] & 0x1) << 15));
                            registers[7] = (ushort)((registers[7]&0xfffe) | (preVal & 0x1));
                            break;
                        case AND:
                            registers[op1] = (ushort)(registers[op1] & registers[op2]);
                            break;
                        case OR:
                            registers[op1] = (ushort)(registers[op1] | registers[op2]);
                            break;
                        case XOR:
                            registers[op1] = (ushort)(registers[op1] ^ registers[op2]);
                            break;
                    }
                    genStatus(registers[op1]);
                    
                    // If the shift instruction is used and the second operand is set to constant data
                    // which isn't used, then decrement the pc so the next instruction isn't skipped.
                    inc = 0;
                    if(op3 >= 4 && op3 <= 7){
                        if(op2 == 0){
                            pc--;
                        }
                    }
                    
                    break;
                case CMP:
                    tmp = registers[op1] + (registers[op2]^0xffff) + 1;
                    genCarry(tmp);
                    genStatus((ushort)tmp);
                    break;
                case JIF:
                    ushort mask = (ushort)(op3 & 0b111);
                    ushort invert = 0;
                    if((op3 & 0b1000) != 0){
                        invert = 0b111;
                    }
                    if(((registers[7] ^ invert) & mask) != 0){
                        pc = registers[op1];
                    }
                    break;
                case INT:
                    if(op3 == 0){
                        interruptAddr = registers[op1];
                    } else if(op3 == 1){
                        registers[7] |= 0b1000;
                    } else if(op3 == 2){
                        registers[7] &= 0b1111111111110111;
                    }
                    
                    inc = 0;
                    if((op3 & 0b11) != 0){
                        if(op1 == 0){
                            inc = 1;
                        }
                    }
                    pc -= inc;
                    break;
                case PLP:
                    setAddress(registers[6]);
                    setRead(1);
                    setUB(1);
                    break;
                case PUSH:
                    registers[6]--;
                    setAddress(registers[6]);
                    setBus(registers[op1]);
                    setUB(1);
                    break;
                case POP:
                    setAddress(registers[6]);
                    setRead(1);
                    setUB(1);
                    break;
                case JSR:
                    registers[6]--;
                    setAddress(registers[6]);
                    setBus(pc);
                    setUB(1);
                    pc = registers[op1];
                    break;
                case RTS:
                    setAddress(registers[6]);
                    setRead(1);
                    setUB(1);
                    break;
                case RTI:
                    setAddress(registers[6]);
                    setRead(1);
                    setUB(1);
                    insideInt = false;
                    break;
            }
        }
        protected void execPhase2(){
            // This is the second phase for instructions that need the data bus
            // You can see there are only a few here, that's where turbo comes from.
            switch(inst){
                case BRK:
                    setWrite(1);
                    break;
                case LOD:
                    registers[op1] = readBus();
                    genStatus(registers[op1]);
                    break;
                case STO:
                    setWrite(1);
                    break;
                case PLP:
                    registers[7] = readBus();
                    registers[6]++;
                    break;
                case PUSH:
                    setWrite(1);
                    break;
                case POP:
                    registers[op1] = readBus();
                    registers[6]++;
                    genStatus(registers[op1]);
                    break;
                case JSR:
                    setWrite(1);
                    break;
                case RTS:
                    pc = readBus();
                    registers[6]++;
                    break;
                case RTI:
                    pc = readBus();
                    registers[6]++;
                    break;
            }
        }
        
        // Setup I/O port for fetch
        protected void setupLoad(){
            CPUstate = 1;
            setAddress(pc);
            setBus(0);
            setRead(1);
            setWrite(0);
            setUB(1);
        }
        
        // Set the carry flag depending on the input
        protected void genCarry(int data){
            registers[7] &= 0b1111111111111110;
            // Check if there was an overflow from an addition
            if(data >= 0x10000){
                registers[7] |= 0b1;
            }
        }
        
        // Generate zero and negative flags
        protected void genStatus(ushort data){
            registers[7] &= 0b1111111111111001;
            if(data == 0){
                registers[7] |= 0b10;
            }
            if((data & 0x8000) == 0x8000){
                registers[7] |= 0b100;
            }
        }
        
        // Output data to address port
        protected void setAddress(ushort address){
            for(int i = 0; i < 16; i++){
                int state = (address>>i) & 1;
                if(state == 1){
                    base.Outputs[addressPin+i].On = true;
                } else {
                    base.Outputs[addressPin+i].On = false;
                }
            }
        }
        
        // Output data to data bus
        protected void setBus(ushort data){
            for(int i = 0; i < 16; i++){
                int state = (data>>i) & 1;
                if(state == 1){
                    base.Outputs[dataPinW+i].On = true;
                } else {
                    base.Outputs[dataPinW+i].On = false;
                }
            }
        }
        
        // Read the data bus
        protected ushort readBus(){
            ushort data = 0;
            for(int i = 0; i < 16; i++){
                data >>= 1;
                if(base.Inputs[dataPinR+i].On == true){
                    data |= 0x8000;
                }
            }
            return data;
        }
        
        // Set write pin
        protected void setWrite(ushort state){
            base.Outputs[WritePin].On = state != 0;
        }
        // Set readpin
        protected void setRead(ushort state){
            base.Outputs[ReadPin].On = state != 0;
        }
        // Set pin for when CPU is using the bus
        protected void setUB(ushort state){
            base.Outputs[useBusPin].On = state != 0;
        }
        // Read a pin's state
        protected bool readPin(int pin){
            return base.Inputs[pin].On;
        }
    }
}