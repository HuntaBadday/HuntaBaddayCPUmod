using System;
using LogicWorld.Rendering.Components;
using System.IO;
using System.IO.Compression;
using LICC;
using HuntaBaddayCPUmod.CustomData;

namespace HuntaBaddayCPUmod {
    
    public class TSC3301Client : ComponentClientCode<IRamData>, FileLoadable {
        const int LOADPIN = 45;
        
        protected override void Initialize() {
            HuntaBaddayCPUmodClient.fileLoadables.Add(this);
        }
        
        protected override void OnComponentDestroyed() {
            HuntaBaddayCPUmodClient.fileLoadables.Remove(this);
        }
        
        public void Load(byte[] data, LineWriter lineWriter) {
            if (data == null || data.Length == 0 || !GetInputState(LOADPIN)) return;
            
            int loadCount = data.Length <= 0x4000 ? data.Length : 0x4000;
            MemoryStream output = new MemoryStream();
            using (DeflateStream comp = new DeflateStream(output, CompressionLevel.Optimal))
                comp.Write(data, 0, loadCount);
            
            Data.ClientIncomingData = output.ToArray();
            Data.State = 1;
        }
        
        protected override void SetDataDefaultValues() {
            Data.Initialize();
        }
    }
}