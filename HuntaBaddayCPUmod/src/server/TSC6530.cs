using LogicAPI.Server.Components;
using System;

namespace HuntaBaddayCPUmod {
    
    public class TSC6530_Comp : LogicComponent {
        public override bool HasPersistentValues => true;
        const int databus = 0;
        const int CSpin = 8;
        const int Rpin = 9;
        const int Wpin = 10;
        const int Addrpin = 11;
        const int Extpin = 14;
        const int CNTpin = 17;
        const int RSTpin = 18;
        const int Clockpin = 19;
        
        const int TApin = 8;
        const int TBpin = 9;
        const int IRQpin = 10;
        
        bool lastClock = false;
        bool lastCNT = false;
        bool lastWrite = false;
        byte lastExt = 0;
        bool hasICRread = false;
        
        byte ICRData;
        byte ICRMask;
        byte CRA;
        byte CRB;
        
        byte TAL;
        byte TAH;
        byte TBL;
        byte TBH;
        byte TALL;
        byte TAHL;
        byte TBLL;
        byte TBHL;
        bool TAOut;
        bool TBOut;
        protected override void Initialize(){
            
        }
        protected override void DoLogicUpdate(){
            // Reset everything
            if(getInput(RSTpin)){
                ICRMask = 0;
                ICRData = 0;
                CRA = 0;
                CRB = 0;
                TAL = 0;
                TAH = 0;
                TBL = 0;
                TBH = 0;
                TALL = 0;
                TAHL = 0;
                TBLL = 0;
                TBHL = 0;
                TAOut = false;
                TBOut = false;
                setOutput(TApin, false);
                setOutput(TBpin, false);
                setOutput(IRQpin, false);
            }
            if(getInput(Clockpin) && !lastClock){
                clockCycle();
            }
            if(!getInput(Clockpin) && lastClock){
                if((CRA & 0b100) == 0){
                    TAOut = false;
                }
                if((CRB & 0b100) == 0){
                    TBOut = false;
                }
            }
            if(getInput(CNTpin) && !lastCNT){
                CNTCycle();
            }
            if(getInput(Rpin) && getInput(CSpin)){
                read();
            } else {
                writeBus(0);
                if(hasICRread){
                    ICRData = 0;
                    hasICRread = false;
                }
            }
            if(getInput(Wpin) && getInput(CSpin) && !lastWrite){
                write();
            }
            
            byte extin = readExt();
            int extChange = (~lastExt & readExt())&0b111;
            if(extChange != 0){
                ICRData |= (byte)(extChange<<2);
            }
            
            // Outputting
            if((CRA & 0b10) != 0){
                setOutput(TApin, TAOut);
            } else {
                setOutput(TApin, false);
            }
            if((CRB & 0b10) != 0){
                setOutput(TBpin, TBOut);
            } else {
                setOutput(TBpin, false);
            }
            
            if((ICRData & 0x7f & ICRMask) != 0){
                setOutput(IRQpin, true);
                ICRData |= 0x80;
            } else {
                setOutput(IRQpin, false);
            }
            
            // Saving last state
            lastClock = getInput(Clockpin);
            lastCNT = getInput(CNTpin);
            lastWrite = getInput(Wpin);
            lastExt = readExt();
        }
        
        protected void clockCycle(){
            int BMode = (CRB >> 5) & 0b11;
            if((CRA & 0b100000) == 0){
                TACycle();
            }
            if(BMode == 0){
                TBCycle();
            }
        }
        protected void CNTCycle(){
            int BMode = (CRB >> 5) & 0b11;
            if((CRA & 0b100000) != 0){
                TACycle();
            }
            if(BMode == 1){
                TBCycle();
            }
        }
        protected void read(){
            byte addr = readAddr();
            switch(addr){
                case 0:
                    writeBus(TAL);
                    break;
                case 1:
                    writeBus(TAH);
                    break;
                case 2:
                    writeBus(TBL);
                    break;
                case 3:
                    writeBus(TBH);
                    break;
                case 4:
                    writeBus(ICRData);
                    hasICRread = true;
                    break;
                case 5:
                    writeBus(CRA);
                    break;
                case 6:
                    writeBus(CRB);
                    break;
                default:
                    writeBus(0);
                    break;
            }
        }
        protected void write(){
            byte addr = readAddr();
            byte data = readBus();
            switch(addr){
                case 0:
                    TALL = data;
                    if((CRA & 0b10000) != 0 || (CRA & 0b000001) == 0){
                        TAL = data;
                    }
                    break;
                case 1:
                    TAHL = data;
                    if((CRA & 0b10000) != 0 || (CRA & 0b000001) == 0){
                        TAH = data;
                    }
                    break;
                case 2:
                    TBLL = data;
                    if((CRB & 0b10000) != 0 || (CRB & 0b000001) == 0){
                        TBL = data;
                    }
                    break;
                case 3:
                    TBHL = data;
                    if((CRB & 0b10000) != 0 || (CRB & 0b000001) == 0){
                        TBH = data;
                    }
                    break;
                case 4:
                    if((data & 0x80) != 0){
                        data &= 0x7f;
                        ICRMask |= data;
                    } else {
                        data = (byte)((data ^ 0xff) & 0x7f);
                        ICRMask &= data;
                    }
                    break;
                case 5:
                    byte oldA = CRA;
                    CRA = data;
                    if((data & 0b1) != 0 && (data & 0b100) != 0 && (oldA & 0b1) == 0){
                        TAOut = true;
                    }
                    break;
                case 6:
                    byte oldB = CRB;
                    CRB = data;
                    if((data & 0b1) != 0 && (data & 0b100) != 0 && (oldB & 0b1) == 0){
                        TBOut = true;
                    }
                    break;
            }
        }
        
        protected void TACycle(){
            if((CRA & 0b000001) == 0){
                return;
            }
            TAL--;
            if(TAL == 0xff){
                TAH--;
                if(TAH == 0xff){
                    TAL = TALL;
                    TAH = TAHL;
                    if((CRA & 0b1000) != 0){
                        CRA &= 0xfe;
                    }
                    int BMode = (CRB >> 5) & 0b11;
                    if(BMode == 2){
                        TBCycle();
                    } else if(BMode == 3 && getInput(CNTpin)){
                        TBCycle();
                    }
                    ICRData |= 0b00000001;
                    if((CRA & 0b100) != 0){
                        TAOut = !TAOut;
                    } else {
                        TAOut = true;
                    }
                }
            }
        }
        protected void TBCycle(){
            if((CRB & 0b000001) == 0){
                return;
            }
            TBL--;
            if(TBL == 0xff){
                TBH--;
                if(TBH == 0xff){
                    TBL = TBLL;
                    TBH = TBHL;
                    if((CRB & 0b1000) != 0){
                        CRB &= 0xfe;
                    }
                    ICRData |= 0b00000010;
                    if((CRB & 0b100) != 0){
                        TBOut = !TBOut;
                    } else {
                        TBOut = true;
                    }
                }
            }
        }
        
        // Output data to data bus
        protected void writeBus(byte data){
            for(int i = 0; i < 8; i++){
                int state = (data>>i) & 1;
                if(state == 1){
                    base.Outputs[databus+7-i].On = true;
                } else {
                    base.Outputs[databus+7-i].On = false;
                }
            }
        }
        
        // Read the data bus
        protected byte readBus(){
            byte data = 0;
            for(int i = 0; i < 8; i++){
                data >>= 1;
                if(base.Inputs[databus+7-i].On == true){
                    data |= 0x80;
                }
            }
            return data;
        }
        
        // Read the address
        protected byte readAddr(){
            byte data = 0;
            for(int i = 0; i < 3; i++){
                data >>= 1;
                if(base.Inputs[Addrpin+2-i].On == true){
                    data |= 0x04;
                }
            }
            return data;
        }
        
        // Read ext pins
        protected byte readExt(){
            byte data = 0;
            for(int i = 0; i < 3; i++){
                data >>= 1;
                if(base.Inputs[Extpin+i].On == true){
                    data |= 0x04;
                }
            }
            return data;
        }
        
        // Read a pin
        protected bool getInput(int num){
            return base.Inputs[num].On;
        }
        
        // Write a pin
        protected void setOutput(int num, bool state){
            base.Outputs[num].On = state;
        }
        
        protected override byte[] SerializeCustomData(){
            // Variables to save
            /*
            bool lastClock;
            bool lastCNT;
            bool lastWrite;
            byte lastExt;
            bool hasICRread;
            
            byte ICRData;
            byte ICRMask;
            byte CRA;
            byte CRB;
            
            byte TAL;
            byte TAH;
            byte TBL;
            byte TBH;
            byte TALL;
            byte TAHL;
            byte TBLL;
            byte TBHL;
            bool TAOut;
            bool TBOut;
            */
            
            byte[] data = new byte[19];
            
            data[0] = Convert.ToByte(lastClock);
            data[1] = Convert.ToByte(lastCNT);
            data[2] = Convert.ToByte(lastWrite);
            data[3] = lastExt;
            data[4] = Convert.ToByte(hasICRread);
            
            data[5] = ICRData;
            data[6] = ICRMask;
            data[7] = CRA;
            data[8] = CRB;
            
            data[9] = TAL;
            data[10] = TAH;
            data[11] = TBL;
            data[12] = TBH;
            data[13] = TALL;
            data[14] = TAHL;
            data[15] = TBLL;
            data[16] = TBHL;
            data[17] = Convert.ToByte(TAOut);
            data[18] = Convert.ToByte(TBOut);
            
            return data;
        }
        
        protected override void DeserializeData(byte[] data){
            if(data == null){
                // New object
                return;
            } if(data.Length != 19){
                // Bad data
                return;
            }
            
            lastClock = Convert.ToBoolean(data[0]);
            lastCNT = Convert.ToBoolean(data[1]);
            lastWrite = Convert.ToBoolean(data[2]);
            lastExt = data[3];
            hasICRread = Convert.ToBoolean(data[4]);
            
            ICRData = data[5];
            ICRMask = data[6];
            CRA = data[7];
            CRB = data[8];
            
            TAL = data[9];
            TAH = data[10];
            TBL = data[11];
            TBH = data[12];
            TALL = data[13];
            TAHL = data[14];
            TBLL = data[15];
            TBHL = data[16];
            TAOut = Convert.ToBoolean(data[17]);
            TBOut = Convert.ToBoolean(data[18]);
        }
    }
}