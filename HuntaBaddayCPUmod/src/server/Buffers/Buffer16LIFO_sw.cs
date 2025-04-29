using LogicAPI.Server.Components;
using System.IO;
using System.IO.Compression;
using System;

namespace HuntaBaddayCPUmod {
    public class Buffer16LIFO_sw : LogicComponent {
        public override bool HasPersistentValues => true;
        const int inputs = 0;
        const int writeBuffer = 16;
        const int readBuffer = 17;
        const int resetPin = 18;
        const int outputs = 0;
        const int dataAvailable = 16;
        const int bufferFull = 17;
        
        ushort[] memory = new ushort[0x10000];
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
            } else if(!base.Inputs[readBuffer].On) {
                writeOutput(0);
            }
            
            if(!base.Inputs[readBuffer].On) {
                if(ptr != 0 || full){
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
        
        // Used to save / load cpu state
        protected override byte[] SerializeCustomData(){
            // Structure:
            // x0 - x1FFFF - Data
            // x20000 - x10001 - ptr
            // x20002 - lastWrite
            // x20003 - lastRead
            // x20004 - full
            
            byte[] data = new byte[0x20000 + 2 + 3];
            
            Buffer.BlockCopy(memory, 0, data, 0, 0x20000);
            
            data[0x20000] = (byte)(ptr&0xff);
            data[0x20001] = (byte)(ptr>>8);
            
            data[0x20002] = Convert.ToByte(lastWrite);
            data[0x20003] = Convert.ToByte(lastRead);
            data[0x20004] = Convert.ToByte(full);
            
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
            
            byte[] customdata = new byte[0x20000 + 2 + 3];
            
            MemoryStream memstream = new MemoryStream(data);
            memstream.Position = 0;
            DeflateStream decompressor = new DeflateStream(memstream, CompressionMode.Decompress);
            int length = decompressor.Read(customdata, 0, customdata.Length);
            
            if(length == (0x20000 + 2 + 3)){
                Buffer.BlockCopy(customdata, 0, memory, 0, 0x20000);
            
                ptr = (ushort)((customdata[0x20000]) | (customdata[0x20001]<<8));
                
                lastWrite = Convert.ToBoolean(customdata[0x20002]);
                lastRead = Convert.ToBoolean(customdata[0x20003]);
                full = Convert.ToBoolean(customdata[0x20004]);
            } else {
                ptr = 0;
                lastWrite = false;
                lastRead = false;
                full = false;
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