using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod
{
    public class LWC31_Micro : LogicComponent
    {   
        // Server Options
        const int turboSpeed = 15; // How many instructions can run per tick. (Do not set this too high)
        
        // ================================
        // I/O Port
        const int IOInput = 0; // - 127
        const int IOOutput = 0; // - 127
        
        const int inputOut = 128; // - 135
        const int outputOut = 136; // -143
        
        const int irqPin = 128;
        const int runPin = 129;
        const int rstPin = 130;
        const int turboPin = 131;
        
        const int loadPin = 132;
        
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
        
        // Program counter
        ushort pc = 0;
        ushort ir = 0;
        
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
        bool insideInterrupt = false;
        
        ushort[] memory = new ushort[0x10000];
        
        const int IOAddress = 0xd000;
        bool IOAccess = false;
        
        protected override void DoLogicUpdate(){
            QueueLogicUpdate();
            if(base.Inputs[rstPin].On){
                pc = memory[0xffff];
                insideInterrupt = false;
                registers[7] |= 0b1000;
                return;
            }
            if(!base.Inputs[runPin].On){
                return;
            }
            for(int i = 0; i < turboSpeed; i++){
                ir = memory[pc];
                pc++;
                divideInstruction();
                if(opNums[inst] == 1 && op1 == 0 || opNums[inst] == 2 && (op1 == 0 || op2 == 0)){
                    registers[0] = memory[pc];
                    pc++;
                }
                executeInstruction();
                if(IOAccess){
                    break;
                }
                if(!base.Inputs[turboPin].On){
                    break;
                }
            }
        }
        
        protected void executeInstruction(){
            switch(inst){
                case BRK:
                    registers[6]--;
                    memory[registers[6]] = pc;
                    pc = interruptAddr;
                    insideInterrupt = true;
                    break;
                case JMP:
                    pc = registers[op1];
                    break;
                case MOV:
                    registers[op1] = registers[op2];
                    break;
                case LOD:
                    if(op3 >= 0x8){
                        registers[op1] = readMemory((ushort)(registers[op2]+registers[op3&0x7]));
                    } else {
                        registers[op1] = readMemory(registers[op2]);
                    }
                    break;
                case STO:
                    if(op3 >= 0x8){
                        writeMemory((ushort)(registers[op2]+registers[op3&0x7]), registers[op1]);
                    } else {
                        writeMemory(registers[op2], registers[op1]);
                    }
                    break;
            }
        }
        
        protected ushort readMemory(ushort address){
            if(address >= IOAddress && address <= IOAddress+7){
                int outPin = address & 0xf;
                int dataPinStart = outPin * 16;
                ushort inputData = 0;
                for(int i = dataPinStart; i < dataPinStart+16; i++){
                    inputData <<= 1;
                    if(base.Inputs[IOInput+i].On){
                        inputData |= 1;
                    }
                }
                base.Outputs[inputOut+outPin].On = true;
                IOAccess = true;
                return inputData;
            } else {
                return memory[address];
            }
        }
        protected void writeMemory(ushort address, ushort data){
            if(address >= IOAddress && address <= IOAddress+7){
                int outPin = address & 0xf;
                int dataPinStart = outPin * 16;
                ushort outputData = data;
                for(int i = dataPinStart; i < dataPinStart+16; i++){
                    base.Outputs[IOOutput+i].On = (outputData&0x8000) != 0;
                    outputData <<= 1;
                }
                base.Outputs[inputOut+outPin].On = true;
                IOAccess = true;
            } else {
                memory[address] = data;
            }
        }
        
        protected void divideInstruction(){
            inst = (ir & 0b1111000000000000) >> 12;
            op1 = (ir & 0b0000111000000000) >> 9;
            op2 = (ir & 0b0000000111000000) >> 6;
            op3 = (ir & 0b0000000000111100) >> 2;
        }
    }
}