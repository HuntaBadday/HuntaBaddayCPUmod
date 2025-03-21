using System;
using System.Collections.Generic;
using LogicAPI.Client;
using LICC;
using System.IO;
using LogicLog;

namespace HuntaBaddayCPUmod {
    public class HuntaBaddayCPUmodClient : ClientMod {
        public static List<FileLoadable> fileLoadables = new List<FileLoadable>();
        
        static HuntaBaddayCPUmodClient() {}

        protected override void Initialize() {
            Logger.Info("HuntaBaddayCPUmod - Loaded Client");
        }
        
        [Command("cloadram", Description="Loads a file into ram from HBCM with the load pin active.")]
        public static void cloadram(string filename) {
            LineWriter lineWriter = LConsole.BeginLine();
            if (File.Exists(filename)) {
                lineWriter.WriteLine($"Loading {filename}");
                byte[] data = File.ReadAllBytes(filename);
                foreach (FileLoadable i in fileLoadables) i.Load(data, lineWriter);
            } else {
                lineWriter.WriteLine($"Failed to load file {filename}: File does not exist!");
            }
            lineWriter.End();
        }
    }
}