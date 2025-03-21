namespace HuntaBaddayCPUmod.CustomData {
    public interface IRamData {
        byte[] Data { get; set; }
        byte State { get; set; }
        byte[] ClientIncomingData { get; set; }
    }
    
    public static class InitializeRamData {
        public static void Initialize(this IRamData data) {
            data.Data = new byte[0];
            data.State = 0;
            data.ClientIncomingData = new byte[0];
        }
    }
}