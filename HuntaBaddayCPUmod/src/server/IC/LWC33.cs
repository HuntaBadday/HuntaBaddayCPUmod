namespace HuntaBaddayCPUmod {
    public class TSC_LWC33 {
        // Constants for each opcode
        const int BRK = 0;
        const int JMP = 1;
        const int MOV = 2;
        const int LOD = 3;
        const int STO = 4;
        const int ALU = 5;
        const int CMP = 6;
        const int SBR = 7;
        const int JPS = 8;
        const int DEV = 9;
        const int PUSH = 10;
        const int POP = 11;
        const int JSR = 12;
        const int RTS = 13;
        const int RTI = 14;
        const int NOP = 15;
        
        // Constants for each ALU operation
        const int ADD = 0;
        const int ADC = 1;
        const int SUB = 2;
        const int SBC = 3;
        const int AND = 4;
        const int OR = 5;
        const int XOR = 6;
        // -- blank space for ALU 7
        const int SHL = 8;
        const int ROL = 9;
        const int SHR = 10;
        const int ROR = 11;
        const int MUL = 12;
        const int SMUL = 13;
        const int DIV = 14;
        const int SDIV = 15;
        
        // Register ids
        const int RR = 13;
        const int SP = 14;
        const int ST = 15;
        
        // Subregister ids
        const int INTV0 = 0;
        const int INTV1 = 1;
        const int INTV2 = 2;
        const int INTV3 = 3;
        const int INTV4 = 4;
        const int BP = 5;
        const int SM = 6;
        const int VMEMV = 7;
        const int VMEMS = 8;
        const int VMEMSEG = 9;
        const int VMEMBP = 10;
        const int VMEMSM = 11;
        const int PROGSEG = 12;
        const int DATASEG = 13;
        const int SPSWAP = 14;
        const int CONTROL = 15;
        
        // Status flags
        const ushort F_CARRY = 0b1;
        const ushort F_ZERO = 0b10;
        const ushort F_NEG = 0b100;
        const ushort F_INT1 = 0b100000000;
        const ushort F_INT2 = 0b1000000000;
        const ushort F_INT3 = 0b10000000000;
        const ushort F_INT4 = 0b100000000000;
        const ushort F_VMEM = 0b1000000000000;
        const ushort F_ABSACCESS = 0b10000000000000;
        const ushort F_UNSAFE = 0b100000000000000;
        const ushort F_BRK = 0b1000000000000000;
        
        // Instruction operand lengths
        static readonly int[] opNums = {
            1, 1, 2, 2, 2, 2, 2, 1, 1, 2, 1, 0, 1, 0, 0, 0
        };
        
        // Which instructions automatically skip the second execution phase - UNUSED
        //static readonly bool[] skipExec2 = {
        //    false, true, true, false, false, true, true, true, true, true, false, false, false, false, false, true
        //};
        
        // Register files
        ushort[] registers = new ushort[16];
        ushort[] subRegisters = new ushort[16];
        
        ushort[] registersBackup = new ushort[16];
        
        // Program counter
        ushort pc;
        // Instruction register
        ushort ir;
        
        // CPU's state
        // 0 - Initialize
        // 1 - Fetch
        // 2 - Execute1
        // 3 - Execute2
        // 4 - Paused
        int cpuState = -1;
        
        // IR Gets split into these registers
        int inst;
        int op1;
        int op2;
        int op3;
        
        // Flag for if the cpu is executing inside an interrupt
        bool insideInt = false;
        
        // Set for if the CPU should go to the fetch phase right away
        bool goToFetch = false;
        
        // How much the pc should increment at the end of the instruction
        // This is either set to 0 or 1
        int pcInc = 0;
        
        // Virtual mode enabled
        bool virtEnabled = false;
        
        // If not 0 then read device bus into specified registers
        int readFromDev = 0;
        bool shouldWriteDevState = false;
        
        // If the CPU is executing in virtual memory
        bool isVirt => virtEnabled && !insideInt;
        
        // CPU IO
        public ushort dataBusInput;
        public ushort dataBusOutput;
        public ushort addressOutput;
        public ushort segmentOutput;
        public ushort deviceBusInput;
        public ushort deviceBusOutput;
        public ushort deviceAddrOutput;
        
        public bool readState;
        public bool writeState;
        public bool devReadState;
        public bool devWriteState;
        
        public bool setCarryState;
        public byte auxState;
        public byte interruptPinStates;
        
        public bool clockState;
        public bool pauseState;
        public bool syncState;
        public bool resetState;
        
        // Pin management stuff
        bool lastClockState = false;
        bool lastSetCarryState = false;
        byte lastAuxState = 0;
        
        // Main logic updating
        public void UpdateLogic() {
            // Gets the clock phase
            bool clockHigh = !lastClockState && clockState;
            bool clockLow = lastClockState && !clockState;
            lastClockState = clockState;
            
            // When reset pin is pulled high
            if (resetState) {
                // Reset segment
                subRegisters[PROGSEG] = 0;
                subRegisters[DATASEG] = 0;
                
                // 0xffff is the boot vector address
                setAddress(0xffff);
                writeDataBus(0);
                
                // Make sure nothing is happening on the device IO bus
                setDeviceID(0);
                writeDeviceBus(0);
                
                // Initial pin states to read boot vector
                syncState = false;
                readState = true;
                writeState = false;
                
                devReadState = false;
                devWriteState = false;
                
                // Reset initial CPU state
                cpuState = 0;
                insideInt = false;
                virtEnabled = false;
                
                readFromDev = 0;
                shouldWriteDevState = false;
                
                // Initialize the status register
                registers[ST] = 0;
                registers[ST] |= F_INT1;
                registers[ST] |= F_INT2;
                registers[ST] |= F_INT3;
                registers[ST] |= F_INT4;
                return;
            }
            
            // If the set carry pin goes high
            if (setCarryState && !lastSetCarryState)
                registers[ST] |= F_CARRY;
            // Bit logic to detect rising edge of aux pins to set the aux flags
            int auxChange = ~lastAuxState & auxState & 0xf;
            registers[ST] |= (ushort)(auxChange << 4);
            
            lastSetCarryState = setCarryState;
            lastAuxState = auxState;
            
            // Read the boot vector and set the pc to it
            if (cpuState == 0 && clockLow) {
                pc = readDataBus();
                setAddress(pc);
                cpuState = 1;
                syncState = true;
                return;
            }
            
            // Complete device bus operation
            if ((cpuState == 1 || cpuState == 4 ) && clockHigh) {
                if (shouldWriteDevState) {
                    shouldWriteDevState = false;
                    devWriteState = true;
                }
                if (readFromDev != 0) {
                    writeReg(readFromDev, readDeviceBus());
                }
            } else if ((cpuState == 1 || cpuState == 4 )&& clockLow) {
                devWriteState = false;
                devReadState = false;
                writeDeviceBus(0);
                setDeviceID(0);
            }
            
            // Unpause the cpu
            if (cpuState == 4 && clockLow && !pauseState) {
                cpuState = 1;
                setAddress(pc);
                readState = true;
                writeState = false;
                writeDataBus(0);
                syncState = true;
                return;
            }
            
            // Fetch instruction
            if (cpuState == 1 && clockHigh) {
                // If interrupt, replace for load IR to BRK with OP1 = INT#
                if ((interruptPinStates & ~registers[ST]>>8 & 0xf) != 0 && !insideInt) {
                    int inum = pinsToNum(interruptPinStates);
                    ir = (ushort)(inum << 8);
                    registers[ST] &= F_BRK^0xffff;
                } else {
                    ir = readDataBus();
                    if ((ir&0xf000) == 0) {
                        // If the interrupt was forced, set the flag
                        registers[ST] |= F_BRK;
                    }
                    setAddress(++pc);
                }
                syncState = false;
            } else if (cpuState == 1 && clockLow) {
                // Read constant value on second phase of fetch
                registers[0] = readDataBus();
                cpuState = 2;
            }
            
            // Instruction stage 1
            if (cpuState == 2 && clockLow) {
                // Extract instruction and operand
                inst = ir>>12&0xf;
                op1 = ir>>8&0xf;
                op2 = ir>>4&0xf;
                op3 = ir&0xf;
                
                pcInc = 0;
                // Check if a constant value is used
                if (opNums[inst] >= 1) {
                    if (op1 == 0) pcInc = 1;
                }
                if (opNums[inst] == 2) {
                    if (op2 == 0) pcInc = 1;
                }
                
                // Default skip second instruction phase
                goToFetch = true;
                
                // Do the first phase
                execStage1();
                
                // Skip second stage if needed
                if (goToFetch) {
                    cpuState = 3;
                }
            }
            
            // Instruction stage 2
            if (cpuState == 2 && clockHigh) {
                execStage2();
                cpuState = 3;
            }
            
            // Clean up end of instruction
            if (cpuState == 3 && clockLow) {
                pc += (ushort)pcInc;
                // If pause, clear outputs, otherwise setup next fetch
                if (pauseState) {
                    cpuState = 4;
                    addressOutput = 0;
                    segmentOutput = 0;
                    readState = false;
                    writeState = false;
                    writeDataBus(0);
                    syncState = true;
                } else {
                    cpuState = 1;
                    setAddress(pc);
                    readState = true;
                    writeState = false;
                    writeDataBus(0);
                    syncState = true;
                }
            }
        }
        
        // First execution phase
        void execStage1() {
            switch (inst) {
                case BRK:
                    // Only do first 5 vectors
                    if (op1 > 5) break;
                    if (isVirt) swapSP(); // Swap stack pointers if leaving vmem
                    insideInt = true;
                    // Push return address to stack
                    decSp();
                    setAddress((ushort)(registers[SP]+subRegisters[BP]));
                    readState = false;
                    writeDataBus(pc);
                    pc = subRegisters[op1]; // Get interrupt vector and jump to it
                    pcInc = 0;
                    goToFetch = false;
                    break;
                case JMP: {
                    // Get the mask for the status
                    int mask = op3 & 0b111 | (op2 & 0b11) << 4;
                    // Check if it should do the inverse operation
                    bool invert = (op3 & 0b1000) != 0;
                    // Break if jump does not occur
                    if ((registers[ST] & mask) != 0 == invert) break;
                    
                    pcInc = 0; // Because of a jump, don't increment the pc at the end
                    // Indirect jump, get address from memory
                    if ((op2 & 0b1000) != 0) {
                        goToFetch = false;
                        setAddress(registers[op1]);
                        break;
                    }
                    // If relative, add to the pc instead of setting it
                    if ((op2 & 0b100) != 0) {
                        pc += registers[op1];
                    } else {
                        pc = registers[op1];
                    }
                    break;
                }
                case MOV:
                    writeReg(op1, registers[op2]);
                    if (op3 == 0 && op1 != ST) {
                        genZN(registers[op2]);
                    }
                    break;
                case LOD:
                    // Check if this is indexed
                    if (op3 != 0) {
                        // Special to load from effective stack location if using sp
                        if (op2 == SP || op3 == SP) {
                            setAddress((ushort)(registers[op2]+registers[op3]+getEffectiveBP()));
                        } else {
                            setAddress((ushort)(registers[op2]+registers[op3]));
                        }
                    } else {
                        // Special to load from effective stack location if using sp
                        if (op2 == SP) {
                            setAddress((ushort)(registers[op2]+getEffectiveBP()));
                        } else {
                            setAddress(registers[op2]);
                        }
                    }
                    goToFetch = false;
                    break;
                case STO:
                    if (op3 != 0) {
                        if (op2 == SP || op3 == SP) {
                            setAddress((ushort)(registers[op2]+registers[op3]+getEffectiveBP()));
                        } else {
                            setAddress((ushort)(registers[op2]+registers[op3]));
                        }
                    } else {
                        if (op2 == SP) {
                            setAddress((ushort)(registers[op2]+getEffectiveBP()));
                        } else {
                            setAddress(registers[op2]);
                        }
                    }
                    goToFetch = false;
                    readState = false;
                    writeDataBus(registers[op1]);
                    break;
                case ALU:
                    doALU();
                    break;
                case CMP:
                    doALU();
                    break;
                case SBR:
                    if (op3 == 0) {
                        // Read subregister
                        writeReg(op1, subRegisters[op2]);
                    // Do not allow access unless in unsafe mode or not in virtual mode, but allow subregister 15
                    } else if (!isVirt || (registers[ST]&F_UNSAFE) != 0 || op2 == CONTROL) {
                        // Special case for CONTROL register
                        if (op2 == CONTROL) {
                            if ((registers[op1]&0b1) != 0) {
                                for(int i = 1; i <= 13; i++) registersBackup[i] = registers[i];
                                registersBackup[15] = registers[15];
                            } else if((registers[op1]&0b10) != 0) {
                                for(int i = 1; i <= 13; i++) registers[i] = registersBackup[i];
                                registers[15] = registersBackup[15];
                            } else if((registers[op1]&0b100) != 0) {
                                for(int i = 1; i <= 13; i++) registers[i] = registersBackup[i];
                            }
                        } else {
                            // Write subregister
                            subRegisters[op2] = registers[op1];
                        }
                    }
                    break;
                case JPS:
                    // Don't allow access to instruction in virtual mode
                    if (isVirt) break;
                    if (op3 == 0) {
                        subRegisters[PROGSEG] = (ushort)op2;
                        pc = registers[op1];
                    } else {
                        insideInt = false;  // This instruction can act as a way to exit an interrupt when entering vmem
                        virtEnabled = true;
                        registers[ST] |= F_VMEM;
                        pc = registers[op1];
                        swapSP();
                    }
                    pcInc = 0;
                    break;
                case DEV:
                    if (op3 == 0) {
                        // Read device
                        setDeviceID(registers[op2]);
                        devReadState = true;
                        readFromDev = op1;
                    // Do not allow access unless in unsafe mode or not in virtual mode
                    } else if (!isVirt || (registers[ST]&F_UNSAFE) != 0 || registers[op2] <= 0xff) {
                        // Write device
                        setDeviceID(registers[op2]);
                        writeDeviceBus(registers[op1]);
                        shouldWriteDevState = true;
                    }
                    break;
                case PUSH:
                    decSp();
                    readState = false;
                    writeDataBus(registers[op1]);
                    setAddress((ushort)(registers[SP]+getEffectiveBP()));
                    goToFetch = false;
                    break;
                case POP:
                    setAddress((ushort)(registers[SP]+getEffectiveBP()));
                    goToFetch = false;
                    break;
                case JSR: {
                    // Get the mask for the status
                    int mask = op3 & 0b111 | (op2 & 0b11) << 4;
                    // Check if it should do the inverse operation
                    bool invert = (op3 & 0b1000) != 0;
                    // Break if jump does not occur
                    if ((registers[ST] & mask) != 0 == invert) break;
                    
                    writeDataBus(pc);
                    
                    decSp();
                    readState = false;
                    setAddress((ushort)(registers[SP]+getEffectiveBP()));
                    
                    pcInc = 0; // Because of a jump, don't increment the pc at the end
                    // If relative, add to the pc instead of setting it
                    if ((op2 & 0b100) != 0) {
                        pc += registers[op1];
                    } else {
                        pc = registers[op1];
                    }
                    
                    goToFetch = false;
                    break;
                }
                case RTS:
                    setAddress((ushort)(registers[SP]+getEffectiveBP()));
                    goToFetch = false;
                    break;
                case RTI:
                    if (isVirt) break;
                    setAddress((ushort)(registers[SP]+getEffectiveBP()));
                    goToFetch = false;
                    break;
                case NOP:
                    break;
            }
        }
        
        void execStage2() {
            switch (inst) {
                case BRK:
                    writeState = true;
                    break;
                case JMP:
                    // If relative, add to the pc instead of setting it
                    if ((op2 & 0b100) != 0) {
                        pc += readDataBus();
                    } else {
                        pc = readDataBus();
                    }
                    break;
                case MOV:
                    break;
                case LOD:
                    writeReg(op1, readDataBus());
                    break;
                case STO:
                    writeState = true;
                    break;
                case ALU:
                    break;
                case CMP:
                    break;
                case SBR:
                    break;
                case JPS:
                    break;
                case DEV:
                    break;
                case PUSH:
                    writeState = true;
                    break;
                case POP:
                    writeReg(op1, readDataBus());
                    incSp();
                    break;
                case JSR:
                    writeState = true;
                    break;
                case RTS:
                    pc = readDataBus();
                    incSp();
                    break;
                case RTI:
                    pc = readDataBus();
                    incSp();
                    virtEnabled = (registers[ST]&F_VMEM) != 0; // Set flag if trying to enter vmem from interrupt
                    if (virtEnabled) swapSP(); // Swap registers if so
                    insideInt = false;
                    break;
            }
        }
        
        void doALU() {
            bool doWriteback = inst == ALU; // If CMP don't write back to register
            int tmp;
            int tmp2;
            ushort q;
            ushort r;
            switch (op3) {
                case ADD:
                    tmp = registers[op1] + registers[op2];
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    genCarry(tmp);
                    break;
                case ADC:
                    tmp = registers[op1] + registers[op2] + (registers[ST]&F_CARRY);
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    genCarry(tmp);
                    break;
                case SUB:
                    tmp = registers[op1] + (registers[op2]^0xffff) + 1;
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    genCarry(tmp);
                    break;
                case SBC:
                    tmp = registers[op1] + (registers[op2]^0xffff) + (registers[15]&1);
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    genCarry(tmp);
                    break;
                case AND:
                    if (doWriteback) writeReg(op1, (ushort)(registers[op1]&registers[op2]));
                    break;
                case OR:
                    if (doWriteback) writeReg(op1, (ushort)(registers[op1]|registers[op2]));
                    break;
                case XOR:
                    if (doWriteback) writeReg(op1, (ushort)(registers[op1]^registers[op2]));
                    break;
                case SHL:
                    registers[RR] = registers[op1];
                    tmp = registers[op1] << registers[op2];
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    break;
                case ROL:
                    tmp2 = registers[RR];
                    registers[RR] = registers[op1];
                    tmp = registers[op1] << registers[op2];
                    tmp |= tmp2 >> (16-registers[op2]);
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    break;
                case SHR:
                    registers[RR] = registers[op1];
                    tmp = registers[op1] >> registers[op2];
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    break;
                case ROR:
                    tmp2 = registers[RR];
                    registers[RR] = registers[op1];
                    tmp = registers[op1] >> registers[op2];
                    tmp |= tmp2 << (16-registers[op2]);
                    if (doWriteback) writeReg(op1, (ushort)tmp);
                    break;
                case MUL:
                    tmp = registers[op1]*registers[op2];
                    if (doWriteback) {
                        writeReg(op1, (ushort)tmp);
                        registers[RR] = (ushort)(tmp>>16);
                    }
                    genCarry(tmp);
                    break;
                case SMUL:
                    tmp = (short)registers[op1] * (short)registers[op2];
                    if (doWriteback) {
                        writeReg(op1, (ushort)tmp);
                        registers[RR] = (ushort)(tmp>>16);
                    }
                    genCarry(tmp);
                    break;
                case DIV:
                    if(registers[op2] == 0){
                        q = 0;
                        r = 0;
                    } else {
                        q = (ushort)(registers[op1] / registers[op2]);
                        r = (ushort)(registers[op1] % registers[op2]);
                    }
                    if (doWriteback) {
                        writeReg(op1, q);
                        registers[RR] = r;
                    }
                    break;
                case SDIV:
                    if(registers[op2] == 0){
                        q = 0;
                        r = 0;
                    } else {
                        q = (ushort)((short)registers[op1] / (short)registers[op2]);
                        r = (ushort)((short)registers[op1] % (short)registers[op2]);
                    }
                    if (doWriteback) {
                        writeReg(op1, q);
                        registers[RR] = r;
                    }
                    break;
            }
            if (op1 != ST) {
                genZN(registers[op1]);
            }
        }
        
        void swapSP() {
            ushort tmp = registers[SP];
            registers[SP] = subRegisters[SPSWAP];
            registers[SPSWAP] = tmp;
        }
        
        void writeReg(int reg, ushort data) {
            // If writing to sp in vmem mode, only write bits 0-7
            if (reg == ST && isVirt && (registers[ST]&F_UNSAFE) == 0) {
                registers[ST] &= 0xff00;
                registers[ST] |= (ushort)(data & 0x00ff);
            } else {
                registers[reg] = data;
            }
        }
        
        // Return which base pointer should be used
        ushort getEffectiveBP() {
            if (isVirt) {
                return subRegisters[VMEMBP];
            }
            return subRegisters[BP];
        }
        
        // Generate carry flag depending on ALU result
        void genCarry(int data) {
            registers[ST] &= F_CARRY^0xffff;
            // Check if there was an overflow from an addition
            if((uint)data >= 0x10000){
                registers[ST] |= F_CARRY;
            }
        }
        
        // Generate zero and negative flag based on data
        void genZN(ushort data) {
            registers[ST] &= (F_ZERO|F_NEG)^0xffff;
            registers[ST] |= (ushort)(data == 0 ? F_ZERO : 0);
            registers[ST] |= (ushort)((data & 0x8000) != 0 ? F_NEG : 0);
        }
        
        // Decrement stack pointer
        void decSp() {
            registers[SP]--;
            registers[SP] &= isVirt ? subRegisters[VMEMSM] : subRegisters[SM];
        }
        
        // Increment stack pointer
        void incSp() {
            registers[SP]++;
            registers[SP] &= isVirt ? subRegisters[VMEMSM] : subRegisters[SM];
        }
        
        // Set the effective address and segment outputs
        void setAddress(ushort addr) {
            // If vmem mode
            if (isVirt) {
                // If not fetching and absolute access is enabled, don't limit to vmem space
                if (cpuState != 1 && (registers[ST]&F_ABSACCESS) != 0) {
                    addressOutput = addr;
                    segmentOutput = subRegisters[DATASEG];
                } else {
                    // Otherwise limit to vmem space
                    if (subRegisters[VMEMS] == 0) return;
                    addressOutput = (ushort)(addr%subRegisters[VMEMS] + subRegisters[VMEMV]);
                    segmentOutput = subRegisters[VMEMSEG];
                }
            } else {
                addressOutput = addr;
                segmentOutput = cpuState == 1 ? subRegisters[PROGSEG] : subRegisters[DATASEG];
            }
        }
        
        // Write to the data bus
        void writeDataBus(ushort data) {
            dataBusOutput = data;
        }
        
        // Read from the data bus
        ushort readDataBus() {
            return dataBusInput;
        }
        
        // Write to the device bus
        void writeDeviceBus(ushort data) {
            deviceBusOutput = data;
        }
        
        // Read from the device bus
        ushort readDeviceBus() {
            return deviceBusInput;
        }
        
        // Set device bus id output
        void setDeviceID(ushort id) {
            deviceAddrOutput = id;
        }
        
        // Convert a set of pin booleans to a number that can be used for the interrupt number
        int pinsToNum(byte pins) {
            if ((pins & 0b1) != 0) {
                return 1;
            } else if ((pins & 0b10) != 0) {
                return 2;
            } else if ((pins & 0b100) != 0) {
                return 3;
            } else if ((pins & 0b1000) != 0) {
                return 4;
            }
            return 0;
        }
        
        // Convert CPU state to byte[]
        public byte[] serializeCPUState() {
            return null;
        }
        
        // Set CPU state from byte[]
        public void deserializeCPUState(byte[] data) {
            
        }
    }
}