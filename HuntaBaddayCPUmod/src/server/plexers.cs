using LogicAPI.Server.Components;
using System;

// NOTE TO MYSELF
// THE PLEXERS THAT SAY INV ARE THE NORMAL ONE!!
// This is to avoid conflicts with the plexers
// that already exist because the inverted plexers
// were made first.

namespace HuntaBaddayCPUmod {
    public class Multiplexer16 : LogicComponent {
        const int plexPin = 0;
        const int enablePin = 4;
        const int inputPin = 5;
        const int outputPin = 0;
        protected override void DoLogicUpdate(){
            int plex = readPlex();
            for(int i = 0; i < 16; i++){
                if(i == plex && !base.Inputs[enablePin].On){
                    base.Outputs[outputPin+i].On = base.Inputs[inputPin].On;
                } else {
                    base.Outputs[outputPin+i].On = false;
                }
            }
        }
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
    }
    
    public class Demultiplexer16 : LogicComponent {
        const int plexPin = 0;
        const int enablePin = 4;
        const int inputPin = 5;
        const int outputPin = 0;
        protected override void DoLogicUpdate(){
            int plex = readPlex();
            if(!base.Inputs[enablePin].On){
                base.Outputs[outputPin].On = base.Inputs[inputPin+plex].On;
            } else {
                base.Outputs[outputPin].On = false;
            }
        }
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
    }
    
    public class BiPlexer16 : LogicComponent {
        public override bool HasPersistentValues => true;
        const int plexPin = 0;
        const int enablePin = 4;
        const int frontInputPin = 5;
        const int backInputPin = 21;
        int previousPlex = 0;
        
        protected override void DoLogicUpdate(){
            int plex = readPlex();
            base.Inputs[backInputPin].RemovePhasicLinkWith(base.Inputs[frontInputPin+previousPlex]);
            if(!base.Inputs[enablePin].On){
                base.Inputs[backInputPin].AddPhasicLinkWith(base.Inputs[frontInputPin+plex]);
            }
            previousPlex = plex;
        }
        
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex){
            return inputIndex==plexPin || inputIndex==plexPin+1 || inputIndex==plexPin+2 || inputIndex==plexPin+3 || inputIndex==enablePin;
        }
        
        protected override byte[] SerializeCustomData(){
            byte[] data = new byte[1];
            data[0] = (byte)previousPlex;
            return data;
        }
        
        protected override void DeserializeData(byte[] data){
            if(data == null){
                // New object
                return;
            }
            if(data.Length == 1){
                previousPlex = (int)data[0];
            }
        }
    }
    public class Multiplexer16Inv : LogicComponent {
        const int plexPin = 0;
        const int enablePin = 4;
        const int inputPin = 5;
        const int outputPin = 0;
        protected override void DoLogicUpdate(){
            int plex = readPlex();
            for(int i = 0; i < 16; i++){
                if(i == plex && base.Inputs[enablePin].On){
                    base.Outputs[outputPin+i].On = base.Inputs[inputPin].On;
                } else {
                    base.Outputs[outputPin+i].On = false;
                }
            }
        }
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
    }
    
    public class Demultiplexer16Inv : LogicComponent {
        const int plexPin = 0;
        const int enablePin = 4;
        const int inputPin = 5;
        const int outputPin = 0;
        protected override void DoLogicUpdate(){
            int plex = readPlex();
            if(base.Inputs[enablePin].On){
                base.Outputs[outputPin].On = base.Inputs[inputPin+plex].On;
            } else {
                base.Outputs[outputPin].On = false;
            }
        }
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
    }
    
    public class BiPlexer16Inv : LogicComponent {
        public override bool HasPersistentValues => true;
        const int plexPin = 0;
        const int enablePin = 4;
        const int frontInputPin = 5;
        const int backInputPin = 21;
        int previousPlex = 0;
        
        protected override void DoLogicUpdate(){
            
            int plex = readPlex();
            base.Inputs[backInputPin].RemovePhasicLinkWith(base.Inputs[frontInputPin+previousPlex]);
            if(base.Inputs[enablePin].On){
                base.Inputs[backInputPin].AddPhasicLinkWith(base.Inputs[frontInputPin+plex]);
            }
            previousPlex = plex;
        }
        
        protected int readPlex(){
            int data = 0;
            for(int i = 0; i < 4; i++){
                data >>= 1;
                if(base.Inputs[plexPin+i].On){
                    data |= 0x08;
                }
            }
            return data;
        }
        
        public override bool InputAtIndexShouldTriggerComponentLogicUpdates(int inputIndex){
            return inputIndex==plexPin || inputIndex==plexPin+1 || inputIndex==plexPin+2 || inputIndex==plexPin+3 ||inputIndex==enablePin;
        }
        
        protected override byte[] SerializeCustomData(){
            byte[] data = new byte[1];
            data[0] = (byte)previousPlex;
            return data;
        }
        
        protected override void DeserializeData(byte[] data){
            if(data == null){
                // New object
                return;
            }
            if(data.Length == 1){
                previousPlex = (int)data[0];
            }
        }
    }
}