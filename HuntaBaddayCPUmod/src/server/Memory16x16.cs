using LogicAPI.Server.Components;
using System;
using System.IO;
using System.IO.Compression;
using HuntaBaddayCPUmod.CustomData;
using LogicWorld.Server.Circuitry;

namespace HuntaBaddayCPUmod {
    public class Memory16x16 : LogicComponent<IRamData> {
        public override bool HasPersistentValues => true;
        
        const int DATAIN = 0;
        const int ADDRIN = 16;
        const int READPIN = 32;
        const int WRITEPIN = 33;
        const int LOADPIN = 34;
        const int ENABLEPIN = 35;
        
        const int DATAOUT = 0;
        
        ushort[] memory = new ushort[0x10000];
        bool loadFromSave;
        bool dirty = false;
        
        protected override void Initialize() {
            loadFromSave = true;
        }
        
        protected override void DoLogicUpdate() {
            if (Inputs[READPIN].On && Inputs[ENABLEPIN].On) {
                writeData(memory[readAddr()]);
            } else {
                writeData(0);
            }
            if (Inputs[WRITEPIN].On && Inputs[ENABLEPIN].On) {
                memory[readAddr()] = readData();
                dirty = true;
            }
        }
        
        ushort readData() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output <<= 1;
                output |= (ushort)(Inputs[DATAIN+i].On ? 1 : 0);
            }
            return output;
        }
        
        ushort readAddr() {
            ushort output = 0;
            for (int i = 0; i < 16; i++) {
                output <<= 1;
                output |= (ushort)(Inputs[ADDRIN+i].On ? 1 : 0);
            }
            return output;
        }
        
        void writeData(ushort data) {
            for (int i = 0; i < 16; i++) {
                Outputs[DATAOUT+i].On = (data&0x8000) != 0;
                data <<= 1;
            }
        }
        
        protected override void SetDataDefaultValues() {
            Data.Initialize();
        }
        
        protected override void OnCustomDataUpdated() {
            if (Data.State == 1 && Data.ClientIncomingData != null || loadFromSave && Data.Data != null) {
                byte[] toLoad = Data.Data;
                if (Data.State == 1) {
                    Logger.Info("Loading data from client");
                    toLoad = Data.ClientIncomingData;
                }
                
                MemoryStream memstream = new MemoryStream(toLoad);
                byte[] mem = new byte[memory.Length*2];
                try {
                    DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
                    int bytesRead;
                    int nextStartIndex = 0;
                    while((bytesRead = decompressor.Read(mem, nextStartIndex, mem.Length-nextStartIndex)) > 0){
                        nextStartIndex += bytesRead;
                    }
                    Buffer.BlockCopy(mem, 0, memory, 0, mem.Length);
                } catch(Exception ex) {
                    Logger.Error("HuntaBaddayCPUmod - Loading data from client failed with exception: " + ex);
                }
                loadFromSave = false;
                if (Data.State == 1) {
                    Data.State = 0;
                    Data.ClientIncomingData = new byte[0];
                    dirty = true;
                }
                QueueLogicUpdate();
            }
        }
        
        protected override void SavePersistentValuesToCustomData() {
            if (!dirty) return;
            dirty = false;
            
            byte[] data = new byte[0x20000];
            Buffer.BlockCopy(memory, 0, data, 0, 0x20000);
            
            MemoryStream memstream = new MemoryStream();
            DeflateStream compressor = new DeflateStream(memstream, CompressionLevel.Optimal, true);
            
            compressor.Write(data);
            compressor.Flush();
            
            Data.Data = memstream.ToArray();
        }
    }
}