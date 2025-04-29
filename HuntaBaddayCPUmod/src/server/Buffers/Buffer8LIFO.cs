using LogicAPI.Server.Components;
using System.IO;
using System.IO.Compression;
using System;

namespace HuntaBaddayCPUmod {
    public class Buffer8LIFO : LogicComponent {
        public override bool HasPersistentValues => true;
        const int inputs = 0;
        const int writeBuffer = 8;
        const int readBuffer = 9;
        const int resetPin = 10;
        const int outputs = 0;
        const int dataAvailable = 8;
        const int bufferFull = 9;
        
        byte[] memory = new byte[0x10000];
        ushort ptr;
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
                    for(int i = 0; i < ptr; i++){
                        memory[i] = 0;
                    }
                }
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
                memory[(int)ptr] = 0;
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
        
        // Used to save / load state and data
        protected override byte[] SerializeCustomData(){
            // Structure:
            // x0 - xFFFF - Data
            // x10000 - x10001 - ptr
            // x10002 - lastWrite
            // x10003 - lastRead
            // x10004 - full
            
            byte[] data = new byte[0x10000 + 2 + 3];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x10000);
            
            data[0x10000] = (byte)(ptr&0xff);
            data[0x10001] = (byte)(ptr>>8);
            
            data[0x10002] = Convert.ToByte(lastWrite);
            data[0x10003] = Convert.ToByte(lastRead);
            data[0x10004] = Convert.ToByte(full);
            
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
                ptr = 0;
                lastWrite = false;
                lastRead = false;
                full = false;
				return;
			}
            
            byte[] customdata = new byte[0x10000 + 2 + 3];
            
            MemoryStream memstream = new MemoryStream(data);
            memstream.Position = 0;
            DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
            int length = decompressor.Read(customdata, 0, customdata.Length);
            
            if(length == (0x10000 + 2 + 3)){
                Buffer.BlockCopy(customdata, 0, memory, 0, 0x10000);
                
                ptr = (ushort)((customdata[0x10000]) | (customdata[0x10001]<<8));
                
                lastWrite = Convert.ToBoolean(customdata[0x10002]);
                lastRead = Convert.ToBoolean(customdata[0x10003]);
                full = Convert.ToBoolean(customdata[0x10004]);
            } else {
                ptr = 0;
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