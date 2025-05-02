namespace HuntaBaddayCPUmod.CustomData {
    public interface ISplitFlapControllerData {
        int FlipAmount {get; set;}
        byte[] Data {get; set;}
    }
    
    public static class InitializeSplitFlapControllerData {
        public static void Initialize(this ISplitFlapControllerData data) {
            data.FlipAmount = 1;
            data.Data = new byte[0];
        }
    }
}