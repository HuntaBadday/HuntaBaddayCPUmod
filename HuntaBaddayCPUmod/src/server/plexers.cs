using LogicAPI.Server.Components;

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
            return inputIndex==plexPin || inputIndex==plexPin+1 || inputIndex==plexPin+2 || inputIndex==plexPin+3 ||inputIndex==enablePin;
        }
    }
}