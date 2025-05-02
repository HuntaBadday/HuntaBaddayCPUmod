using System.Collections.Generic;
using System.IO;
using EccsLogicWorldAPI.Client.Hooks;
using LICC;
using LogicAPI.Client;

namespace HuntaBaddayCPUmod {
    public class HuntaBaddayCPUmodClient : ClientMod {
        public static List<FileLoadable> fileLoadables = new List<FileLoadable>();
        
        static HuntaBaddayCPUmodClient() {}

        protected override void Initialize() {
            WorldHook.worldLoading += () => {
                TerminalControllerMenu.init();
                SplitFlapControllerMenu.init();
            };
            Logger.Info("HuntaBaddayCPUmod - Loaded Client");
        }
        
        [Command("loadraml", Description="Loads a file into ram from HBCM with the load pin active in low byte order.")]
        public static void loadraml(string filename) {
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
        
        [Command("loadramh", Description="Loads a file into ram from HBCM with the load pin active in high byte order.")]
        public static void loadramh(string filename) {
            LineWriter lineWriter = LConsole.BeginLine();
            if (File.Exists(filename)) {
                lineWriter.WriteLine($"Loading {filename}");
                byte[] data = File.ReadAllBytes(filename);
                flipOrder(data);
                foreach (FileLoadable i in fileLoadables) i.Load(data, lineWriter);
            } else {
                lineWriter.WriteLine($"Failed to load file {filename}: File does not exist!");
            }
            lineWriter.End();
        }
        
        static void flipOrder(byte[] data) {
            for (int i = 0; i < data.Length/2; i++) {
                (data[i*2], data[i*2+1]) = (data[i*2+1], data[i*2]);
            }
        }
    }
}