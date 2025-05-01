namespace HuntaBaddayCPUmod.CustomData {
    public interface ITermControllerData {
        byte Width {get; set;}
        byte Height {get; set;}
        byte[] Data {get; set;}
    }
    
    public static class InitializeTermControllerData {
        public static void Initialize(this ITermControllerData data) {
            data.Width = 1;
            data.Height = 1;
            data.Data = new byte[0];
        }
    }
}