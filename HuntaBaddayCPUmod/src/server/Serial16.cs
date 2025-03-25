using System.Collections.Generic;
using System.IO;
using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod {
    public class SerialTrans16 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int DATAIN = 0;
        const int WRITE = 16;
        const int SEROUT = 0;
        const int FLAGDONE = 1;
        
        ushort currentWord;
        byte sendCount;
        bool sending = false;
        bool done = false;
        
        bool lastWrite1;
        bool lastWrite2;
        
        protected override void DoLogicUpdate() {
            bool writeChange = Inputs[WRITE].On && !lastWrite1 || Inputs[WRITE+1].On && !lastWrite2;
            lastWrite1 = Inputs[WRITE].On;
            lastWrite2 = Inputs[WRITE+1].On;
            
            if (done) {
                Outputs[FLAGDONE].On = false;
                Outputs[SEROUT].On = false;
                done = false;
            }
            
            if (Inputs[WRITE].On && Inputs[WRITE+1].On && !sending && writeChange) {
                currentWord = readData();
                sendCount = 0;
                sending = true;
                Outputs[SEROUT].On = true;
                QueueLogicUpdate();
                return;
            }
            if (sending) {
                Outputs[SEROUT].On = (currentWord & 1) == 1;
                currentWord >>= 1;
                if (++sendCount >= 16) {
                    sending = false;
                    done = true;
                    Outputs[FLAGDONE].On = true;
                }
                QueueLogicUpdate();
            }
        }
        
        ushort readData() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output <<= 1;
                output |= (ushort)(Inputs[DATAIN+i].On ? 1:0);
            }
            return output;
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream m = new MemoryStream();
            BinaryWriter w = new BinaryWriter(m);
            
            w.Write(currentWord);
            w.Write(sendCount);
            w.Write(sending);
            w.Write(done);
            w.Write(lastWrite1);
            w.Write(lastWrite2);
            
            return m.ToArray();
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) return;
            
            MemoryStream m = new MemoryStream(data);
            BinaryReader r = new BinaryReader(m);
            
            try {
                currentWord = r.ReadUInt16();
                sendCount = r.ReadByte();
                sending = r.ReadBoolean();
                done = r.ReadBoolean();
                lastWrite1 = r.ReadBoolean();
                lastWrite2 = r.ReadBoolean();
            } catch (EndOfStreamException ex) {
                Logger.Error("SerialTrans16 - Error loading data");
            }
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == WRITE || index == WRITE+1;
        }
    }

    public class SerialRecv16 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int SERIN = 0;
        const int DATAOUT = 0;
        const int FLAG = 16;
        
        ushort currentWord;
        byte recvCount;
        bool receiving = false;
        
        protected override void DoLogicUpdate() {
            if (!receiving) Outputs[FLAG].On = false;
            if (!receiving && Inputs[SERIN].On) {
                recvCount = 0;
                receiving = true;
                QueueLogicUpdate();
            } else if (receiving) {
                currentWord >>= 1;
                if (Inputs[SERIN].On)
                    currentWord |= 0x8000;
                if (++recvCount >= 16) {
                    receiving = false;
                    Outputs[FLAG].On = true;
                    writeData(currentWord);
                }
                QueueLogicUpdate();
            }
        }
        
        void writeData(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[DATAOUT+i].On = (data & 0x8000) != 0;
                data <<= 1;
            }
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream m = new MemoryStream();
            BinaryWriter w = new BinaryWriter(m);
            
            w.Write(currentWord);
            w.Write(recvCount);
            w.Write(receiving);
            
            return m.ToArray();
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) return;
            
            MemoryStream m = new MemoryStream(data);
            BinaryReader r = new BinaryReader(m);
            
            try {
                currentWord = r.ReadUInt16();
                recvCount = r.ReadByte();
                receiving = r.ReadBoolean();
            } catch (EndOfStreamException ex) {
                Logger.Error("SerialRecv16 - Error loading data");
            }
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == SERIN;
        }
    }
    
    public class SerialRecv16_sw : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int SERIN = 0;
        const int READ = 1;
        const int DATAOUT = 0;
        const int FLAG = 16;
        
        ushort currentWord;
        ushort output;
        byte recvCount;
        bool receiving = false;
        
        protected override void DoLogicUpdate() {
            if (!receiving) Outputs[FLAG].On = false;
            if (!receiving && Inputs[SERIN].On) {
                recvCount = 0;
                receiving = true;
                QueueLogicUpdate();
            } else if (receiving) {
                currentWord >>= 1;
                if (Inputs[SERIN].On)
                    currentWord |= 0x8000;
                if (++recvCount >= 16) {
                    receiving = false;
                    Outputs[FLAG].On = true;
                    output = currentWord;
                }
                QueueLogicUpdate();
            }
            
            if (Inputs[READ].On && Inputs[READ+1].On) {
                writeData(output);
            } else {
                writeData(0);
            }
        }
        
        void writeData(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[DATAOUT+i].On = (data & 0x8000) != 0;
                data <<= 1;
            }
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream m = new MemoryStream();
            BinaryWriter w = new BinaryWriter(m);
            
            w.Write(currentWord);
            w.Write(output);
            w.Write(recvCount);
            w.Write(receiving);
            
            return m.ToArray();
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) return;
            
            MemoryStream m = new MemoryStream(data);
            BinaryReader r = new BinaryReader(m);
            
            try {
                currentWord = r.ReadUInt16();
                output = r.ReadUInt16();
                recvCount = r.ReadByte();
                receiving = r.ReadBoolean();
            } catch (EndOfStreamException ex) {
                Logger.Error("SerialRecv16 - Error loading data");
            }
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == SERIN || index == READ || index == READ+1;
        }
    }
}

