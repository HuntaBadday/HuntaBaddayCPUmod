using LogicAPI.Server.Components;
using System.IO;
using System.IO.Compression;
using System;

namespace HuntaBaddayCPUmod {
    public class Buffer16LIFO : LogicComponent {
        public override bool HasPersistentValues => true;
        const int inputs = 0;
        const int writeBuffer = 16;
        const int readBuffer = 17;
        const int resetPin = 18;
        const int outputs = 0;
        const int dataAvailable = 16;
        const int bufferFull = 17;
        
        ushort[] memory = new ushort[0x10000];
        ushort ptr = 0;
        bool lastWrite = false;
        bool lastRead = false;
        bool full = false;
        
        protected override void Initialize(){
        }
        protected override void DoLogicUpdate(){
            if(base.Inputs[resetPin].On){
                ptr = 0;
                full = false;
            }
            if(base.Inputs[writeBuffer].On && !lastWrite && !full){
                memory[(int)ptr] = readInput();
                ptr++;
                if(ptr == 0){
                    full = true;
                }
            }
            if(base.Inputs[readBuffer].On && !lastRead && (ptr != 0 || full)){
                ptr--;
                writeOutput(memory[(int)ptr]);
                full = false;
            }
            
            if(ptr != 0 || full){
                base.Outputs[dataAvailable].On = true;
            } else {
                base.Outputs[dataAvailable].On = false;
            }
            if(full){
                base.Outputs[bufferFull].On = true;
            } else {
                base.Outputs[bufferFull].On = false;
            }
            
            lastWrite = base.Inputs[writeBuffer].On;
            lastRead = base.Inputs[readBuffer].On;
        }
        
        // Used to save / load cpu state
        protected override byte[] SerializeCustomData(){
            return null;
            byte[] data = new byte[0x20000 + 2 + 2];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x20000);
            
            data[0x20000] = (byte)(ptr>>8);
            data[0x20001] = (byte)(ptr&0xff);
            
            data[0x20002] = 0;
            data[0x20003] = 0;
            
            MemoryStream memstream = new MemoryStream();
            memstream.Position = 0;
            DeflateStream compressor = new DeflateStream(memstream, CompressionLevel.Optimal, true);
            compressor.Write(data, 0, data.Length);
            compressor.Flush();
            int length = (int)memstream.Position;
            memstream.Position = 0;
            byte[] output = new byte[length];
            memstream.Read(output, 0, length);
            
            memstream.Dispose();
            compressor.Dispose();
            
            return output;
        }
        protected override void DeserializeData(byte[] data){
            return;
            if(data == null){
                // New object
				//return;
			}
            
            byte[] customdata = new byte[0x10000 + 2 + 2];
            MemoryStream memstream = new MemoryStream(customdata);
            memstream.Position = 0;
            DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
            int length = decompressor.Read(customdata, 0, customdata.Length);
            
            memstream.Dispose();
            decompressor.Dispose();
            
            if(length == (0x20000 + 2 + 2)){
                Buffer.BlockCopy(customdata, 0, memory, 0, 0x20000);
            
                ptr = (ushort)((customdata[0x20000]<<8) | (customdata[0x20001]));
            }
            return;
        }
        
        // Output data to data bus
        protected void writeOutput(ushort data){
            for(int i = 0; i < 16; i++){
                int state = (data>>i) & 1;
                if(state == 1){
                    base.Outputs[outputs+15-i].On = true;
                } else {
                    base.Outputs[outputs+15-i].On = false;
                }
            }
        }
        
        // Read the data bus
        protected ushort readInput(){
            ushort data = 0;
            for(int i = 0; i < 16; i++){
                data >>= 1;
                if(base.Inputs[inputs+15-i].On == true){
                    data |= 0x8000;
                }
            }
            return data;
        }
    }
}