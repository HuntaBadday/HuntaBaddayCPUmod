using System.Collections.Generic;
using System.IO;
using LogicAPI.Server.Components;

namespace HuntaBaddayCPUmod {
    public class SerialTrans8 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int DATAIN = 0;
        const int WRITE = 8;
        const int SEROUT = 0;
        const int FLAGDONE = 1;
        
        byte currentByte;
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
                currentByte = readData();
                sendCount = 0;
                sending = true;
                Outputs[SEROUT].On = true;
                QueueLogicUpdate();
                return;
            }
            if (sending) {
                Outputs[SEROUT].On = (currentByte & 1) == 1;
                currentByte >>= 1;
                if (++sendCount >= 8) {
                    sending = false;
                    done = true;
                    Outputs[FLAGDONE].On = true;
                }
                QueueLogicUpdate();
            }
        }
        
        byte readData() {
            byte output = 0;
            for (int i = 0; i < 8; i++) {
                output <<= 1;
                output |= (byte)(Inputs[DATAIN+i].On ? 1:0);
            }
            return output;
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream m = new MemoryStream();
            BinaryWriter w = new BinaryWriter(m);
            
            w.Write(currentByte);
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
                currentByte = r.ReadByte();
                sendCount = r.ReadByte();
                sending = r.ReadBoolean();
                done = r.ReadBoolean();
                lastWrite1 = r.ReadBoolean();
                lastWrite2 = r.ReadBoolean();
            } catch (EndOfStreamException ex) {
                Logger.Error("SerialTrans8 - Error loading data");
            }
            
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == WRITE || index == WRITE+1;
        }
    }

    public class SerialRecv8 : LogicComponent {
        public override bool HasPersistentValues => true;
        
        const int SERIN = 0;
        const int DATAOUT = 0;
        const int FLAG = 8;
        
        byte currentByte;
        byte recvCount;
        bool receiving = false;
        
        protected override void DoLogicUpdate() {
            if (!receiving) Outputs[FLAG].On = false;
            if (!receiving && Inputs[SERIN].On) {
                recvCount = 0;
                receiving = true;
                QueueLogicUpdate();
            } else if (receiving) {
                currentByte >>= 1;
                if (Inputs[SERIN].On)
                    currentByte |= 0x80;
                if (++recvCount >= 8) {
                    receiving = false;
                    Outputs[FLAG].On = true;
                    writeData(currentByte);
                }
                QueueLogicUpdate();
            }
        }
        
        void writeData(byte data) {
            for (int i = 0; i < 8; i++) {
                Outputs[DATAOUT+i].On = (data & 0x80) != 0;
                data <<= 1;
            }
        }
        
        protected override byte[] SerializeCustomData(){
            MemoryStream m = new MemoryStream();
            BinaryWriter w = new BinaryWriter(m);
            
            w.Write(currentByte);
            w.Write(recvCount);
            w.Write(receiving);
            
            return m.ToArray();
        }
        
        protected override void DeserializeData(byte[] data){
            if (data == null) return;
            
            MemoryStream m = new MemoryStream(data);
            BinaryReader r = new BinaryReader(m);
            
            try {
                currentByte = r.ReadByte();
                recvCount = r.ReadByte();
                receiving = r.ReadBoolean();
            } catch (EndOfStreamException ex) {
                Logger.Error("SerialRecv8 - Error loading data");
            }
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int index) {
            return index == SERIN;
        }
    }
}

