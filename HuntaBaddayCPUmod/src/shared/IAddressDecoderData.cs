namespace HuntaBaddayCPUmod.CustomData {
    public interface IAddressDecoderData {
        ushort StartAddress { get; set; }
        ushort EndAddress { get; set; }
        
        string StartAddressText { get; set; }
        string EndAddressText { get; set; }
    }
    
    public static class InitializeAddressDecoderData {
        public static void Initialize(this IAddressDecoderData data) {
            data.StartAddress = 0;
            data.EndAddress = 0;
            
            data.StartAddressText = "";
            data.EndAddressText = "";
        }
    }
}