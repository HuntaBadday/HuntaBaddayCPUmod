using LogicAPI.Client;
using HuntaBaddayCPUmod.Client;
using System;
using System.IO;
using LICC;

namespace HuntaBaddayCPUmod {
    public class HBCMClient : ClientMod {
        
        public List<FileLoadable> fileLoadables = new List<FileLoadable>();
        
        //[Command("loadcpu", Description = "Load a file into a microcontroller with the load pin active.")]
        public static void loadcpu(string file){
            if(File.Exists(file)){
                byte[] data = ReadAllBytes(file)
                
                MemoryStream memstream = new MemoryStream();
                memstream.Position = 0;
                DeflateStream compressor = new DeflateStream(memstream, CompressionLevel.Optimal, true);
                compressor.Write(data, 0 ,data.Length);
                compressor.Flush();
                int length = (int)memstream.Position;
                memstream.Position = 0;
                byte[] output = new byte[length];
                memstream.Read(bytes, 0, length);
                
                memstream.close();
                compressor.close();
                
                foreach(var item in fileLoadables) item.Load(output);
            } else {
                LConsole.WriteLine("File does not exist!");
            }
        }
    }
}