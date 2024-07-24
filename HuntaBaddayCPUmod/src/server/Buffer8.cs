using LogicAPI.Server.Components;
using System.IO;
using System.IO.Compression;
using System;

namespace HuntaBaddayCPUmod {
    public class Buffer8 : LogicComponent {
        public override bool HasPersistentValues => true;
        const int inputs = 0;
        const int writeBuffer = 8;
        const int readBuffer = 9;
        const int resetPin = 10;
        const int outputs = 0;
        const int dataAvailable = 8;
        const int bufferFull = 9;
        
        byte[] memory = new byte[0x10000];
        ushort ptr1 = 0;
        ushort ptr2 = 0;
        bool lastWrite = false;
        bool lastRead = false;
        bool full = false;
        
        protected override void Initialize(){
        }
        protected override void DoLogicUpdate(){
            if(base.Inputs[resetPin].On){
                ptr1 = 0;
                ptr2 = 0;
                full = false;
            }
            if(base.Inputs[writeBuffer].On && !lastWrite && !full){
                memory[(int)ptr1] = readInput();
                ptr1++;
                if(ptr1 == ptr2){
                    full = true;
                }
            }
            if(base.Inputs[readBuffer].On && !lastRead && (ptr1 != ptr2 || full)){
                writeOutput(memory[(int)ptr2]);
                ptr2++;
                full = false;
            }
            
            if(ptr1 != ptr2 || full){
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
            byte[] data = new byte[0x10000 + 2 + 2];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x10000);
            
            data[0x10000] = (byte)(ptr1>>8);
            data[0x10001] = (byte)(ptr1&0xff);
            
            data[0x10002] = (byte)(ptr2>>8);
            data[0x10003] = (byte)(ptr2&0xff);
            
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
            
            if(length == (0x10000 + 2 + 2)){
                Buffer.BlockCopy(data, 0, memory, 0, 0x10000);
            
                ptr1 = (ushort)((customdata[0x10000]<<8) | (customdata[0x10001]));
                ptr2 = (ushort)((customdata[0x10002]<<8) | (customdata[0x10003]));
            }
            return;
        }
        
        // Output data to data bus
        protected void writeOutput(byte data){
            for(int i = 0; i < 8; i++){
                int state = (data>>i) & 1;
                if(state == 1){
                    base.Outputs[outputs+7-i].On = true;
                } else {
                    base.Outputs[outputs+7-i].On = false;
                }
            }
        }
        
        // Read the data bus
        protected byte readInput(){
            byte data = 0;
            for(int i = 0; i < 8; i++){
                data >>= 1;
                if(base.Inputs[inputs+7-i].On == true){
                    data |= 0x80;
                }
            }
            return data;
        }
    }
}