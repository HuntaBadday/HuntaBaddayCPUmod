using LogicAPI.Server.Components;
using System.IO;
using System.IO.Compression;
using System;

namespace HuntaBaddayCPUmod {
    public class Buffer8_sw : LogicComponent {
        public override bool HasPersistentValues => true;
        const int inputs = 0;
        const int writeBuffer = 8;
        const int readBuffer = 9;
        const int resetPin = 10;
        const int outputs = 0;
        const int dataAvailable = 8;
        const int bufferFull = 9;
        
        byte[] memory = new byte[0x10000];
        ushort ptr1;
        ushort ptr2;
        bool lastWrite;
        bool lastRead;
        bool full;
        
        protected override void Initialize(){
        }
        protected override void DoLogicUpdate(){
            if(base.Inputs[resetPin].On){
                // Do this junk so spamming reset doesn't lag the simulation
                if (full){
                    for(int i = 0; i < 0x10000; i++){
                        memory[i] = 0;
                    }
                } else {
                    for(int i = ptr2; i < ptr1; i++){
                        memory[i] = 0;
                    }
                }
                
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
                base.Outputs[dataAvailable].On = true; // Force the output to be on in case read and write was done at the same time
                memory[(int)ptr2] = 0;
                ptr2++;
                full = false;
            } else if(!base.Inputs[readBuffer].On) {
                writeOutput(0);
            }
            
            if(!base.Inputs[readBuffer].On) {
                if(ptr1 != ptr2 || full){
                    base.Outputs[dataAvailable].On = true;
                } else {
                    base.Outputs[dataAvailable].On = false;
                }
            }
            
            if(full){
                base.Outputs[bufferFull].On = true;
            } else {
                base.Outputs[bufferFull].On = false;
            }
            
            lastWrite = base.Inputs[writeBuffer].On;
            lastRead = base.Inputs[readBuffer].On;
        }
        
        // Used to save / load state and data
        protected override byte[] SerializeCustomData(){
            // Structure:
            // x0 - xFFFF - Data
            // x10000 - x10001 - ptr1
            // x10002 - x10003 - ptr2
            // x10004 - lastWrite
            // x10005 - lastRead
            // x10006 - full
            
            byte[] data = new byte[0x10000 + 2 + 2 + 3];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x10000);
            
            data[0x10000] = (byte)(ptr1&0xff);
            data[0x10001] = (byte)(ptr1>>8);
            
            data[0x10002] = (byte)(ptr2&0xff);
            data[0x10003] = (byte)(ptr2>>8);
            
            data[0x10004] = Convert.ToByte(lastWrite);
            data[0x10005] = Convert.ToByte(lastRead);
            data[0x10006] = Convert.ToByte(full);
            
            MemoryStream memstream = new MemoryStream();
            memstream.Position = 0;
            DeflateStream compressor = new DeflateStream(memstream, CompressionLevel.Optimal, true);
            
            compressor.Write(data, 0, data.Length);
            compressor.Flush();
            
            int length = (int)memstream.Position;
            memstream.Position = 0;
            byte[] output = new byte[length];
            memstream.Read(output, 0, length);
            
            return output;
        }
        protected override void DeserializeData(byte[] data){
            if(data == null){
                // New object
                ptr1 = 0;
                ptr2 = 0;
                lastWrite = false;
                lastRead = false;
                full = false;
				return;
			}
            
            byte[] customdata = new byte[0x10000 + 2 + 2 + 3];
            
            MemoryStream memstream = new MemoryStream(data);
            memstream.Position = 0;
            DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
            int length = decompressor.Read(customdata, 0, customdata.Length);
            
            if(length == (0x10000 + 2 + 2 + 3)){
                Buffer.BlockCopy(customdata, 0, memory, 0, 0x10000);
            
                ptr1 = (ushort)((customdata[0x10000]) | (customdata[0x10001]<<8));
                ptr2 = (ushort)((customdata[0x10002]) | (customdata[0x10003]<<8));
                
                lastWrite = Convert.ToBoolean(customdata[0x10004]);
                lastRead = Convert.ToBoolean(customdata[0x10005]);
                full = Convert.ToBoolean(customdata[0x10006]);
            } else {
                ptr1 = 0;
                ptr2 = 0;
                lastWrite = false;
                lastRead = false;
                full = false;
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