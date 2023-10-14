// TO DO

// Finish IRQ and add extra IRQs
// Add file loading

using LogicAPI.Server.Components;
using System;

namespace HuntaBaddayCPUmod
{
    public class LWC31_Micro : LogicComponent
    {   
        // Server Options
        const int turboSpeed = 20; // How many instructions can run per tick. (Do not set this too high)
        
        // Component stuff
        public override bool HasPersistentValues => true;
        
        // ================================
        // I/O Port
        const int IOInput = 0; // - 127
        const int IOOutput = 0; // - 127
        
        const int inputOut = 128; // - 135
        const int outputOut = 136; // -143
        
        const int rstPin = 128;
        const int runPin = 129;
        const int turboPin = 130;
        const int irqPin = 131;
        
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
        
        ushort[] tempProgram = {
            0b0011001000000000,
            0b1101000000000000,
            
            0b0100001000000000,
            0b1101000000000001,
            
            0b0001000000000000,
            0b0000000000000000
        };
        protected void loadTempProgram(){
            writeMemory(0xffff, 0x0000);
            for(int i = 0; i < tempProgram.Length; i++){
                memory[i] = tempProgram[i];
            }
        }
        
        protected override void DoLogicUpdate(){
            QueueLogicUpdate();
            for(int i = 0; i <= 15; i++){
                base.Outputs[inputOut+i].On = false;
            }
            if(base.Inputs[rstPin].On){
                pc = memory[0xffff];
                insideInterrupt = false;
                registers[7] |= 0b1000;
                return;
            }
            if(base.Inputs[loadPin].On){
                Logger.Info("Loaded!");
                loadTempProgram();
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
                    genStatus(registers[op1]);
                    break;
                case LOD:
                    if(op3 >= 0x8){
                        registers[op1] = readMemory((ushort)(registers[op2]+registers[op3&0x7]));
                    } else {
                        registers[op1] = readMemory(registers[op2]);
                    }
                    genStatus(registers[op1]);
                    break;
                case STO:
                    if(op3 >= 0x8){
                        writeMemory((ushort)(registers[op2]+registers[op3&0x7]), registers[op1]);
                    } else {
                        writeMemory(registers[op2], registers[op1]);
                    }
                    break;
                case ALU:
                    executeALU();
                    break;
                case CMP:
                    int tmp = registers[op1] + (registers[op2]^0xffff)+1;
                    ushort tmp2 = (ushort)tmp;
                    genCarry(tmp);
                    genStatus(tmp2);
                    break;
                case JIF:
                    int condition = op3 & 0b111;
                    int invert = 0;
                    if((op3&0b1000) == 0b1000){
                        invert = 0b111;
                    }
                    if(((registers[7]^invert) & condition) != 0){
                        pc = registers[op1];
                    }
                    break;
                case NOP:
                    break;
                case INT:
                    if(op3 == 0x0){
                        interruptAddr = registers[op1];
                    } else if(op3 == 0x1){
                        registers[7] |= 0b1000;
                    } else if(op3 == 0x2){
                        registers[7] &= 0b1111111111110111;
                    }
                    break;
                case PLP:
                    registers[7] = memory[registers[6]];
                    registers[6]++;
                    break;
                case PUSH:
                    registers[6]--;
                    memory[registers[6]] = registers[op1];
                    break;
                case POP:
                    registers[op1] = memory[registers[6]];
                    registers[6]++;
                    genStatus(registers[op1]);
                    break;
                case JSR:
                    registers[6]--;
                    memory[registers[6]] = pc;
                    pc = registers[op1];
                    break;
                case RTS:
                    pc = memory[registers[6]];
                    registers[6]++;
                    break;
                case RTI:
                    pc = memory[registers[6]];
                    registers[6]++;
                    insideInterrupt = false;
                    break;
            }
        }
        
        protected void executeALU(){
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
            if(op3 >= 4 && op3 <= 7){
                if(op2 == 0){
                    pc--;
                }
            }
        }
        
        protected void genCarry(int data){
            if(data >= 0x10000){
                registers[7] |= 0b1;
            } else {
                registers[7] &= 0b1111111111111110;
            }
        }
        protected void genStatus(ushort data){
            if(data == 0){
                registers[7] |= 0b010;
            } else {
                registers[7] &= 0b1111111111111101;
            }
            if((data & 0x8000) == 0x8000){
                registers[7] |= 0b100;
            } else {
                registers[7] &= 0b1111111111111011;
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
                base.Outputs[outputOut+outPin].On = true;
                IOAccess = true;
            } else {
                memory[address] = data;
            }
        }
        
        protected void divideInstruction(){
            inst = (ir >> 12) & 0b1111;
            op1 = (ir >> 9) & 0b111;
            op2 = (ir >> 6) & 0b111;
            op3 = (ir >> 2) & 0b1111;
            return;
        }
        
        // Used to save / load cpu state
        protected override byte[] SerializeCustomData(){
            byte[] data = new byte[0x20000 + 4 + 16 + 1];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x20000);
            
            data[0x20000] = (byte)(pc>>8);
            data[0x20001] = (byte)(pc&0xff);
            
            data[0x20002] = (byte)(interruptAddr>>8);
            data[0x20003] = (byte)(interruptAddr&0xff);
            
            Buffer.BlockCopy(registers, 0, data, 0x20004, 16);
            
            if(insideInterrupt){
                data[0x20014] = 1;
            } else {
                data[0x20014] = 0;
            }
            return data;
        }
        protected override void DeserializeData(byte[] customdata){
            if(customdata == null){
                // New object
				return;
			}
            if(customdata.Length == (0x20000 + 4 + 16 + 1)){
                Buffer.BlockCopy(customdata, 0, memory, 0, 0x20000);
            
                pc = (ushort)((customdata[0x20000]<<8) | (customdata[0x20001]));
                interruptAddr = (ushort)((customdata[0x20002]<<8) | (customdata[0x20003]));
                
                Buffer.BlockCopy(customdata, 0x20004, registers, 0, 16);

                insideInterrupt = customdata[0x20014] != 0;
            }
            return;
        }
    }
}