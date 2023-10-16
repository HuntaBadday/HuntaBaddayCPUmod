using LogicWorld.Rendering.Components;
using System;
using System.IO;
using System.IO.Compression;
using LICC;

namespace HuntaBaddayCPUmod.Client {
    public class LWC31_Micro_Client : ComponentClientCode, FileLoadable {
        /*protected override void Initialize(){
            HBCMClient.fileLoadables.Add(this);
        }
        protected override void OnComponentDestroyed(){
            HBCMClient.fileLoadables.Remove(this);
        }
        public void Load(byte[] data){
            if(GetInputState(132)){
                LConsole.WriteLine("Sending data to server!");
                BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack((BuildRequest) new BuildRequest_UpdateComponentCustomData(this.Address, data));
            }
        }*/
    }
}