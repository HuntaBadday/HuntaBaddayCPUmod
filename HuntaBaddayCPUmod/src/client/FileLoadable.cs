using LICC;

namespace HuntaBaddayCPUmod {
    public interface FileLoadable {
        void Load(byte[] data, LineWriter lineWriter);
    }
}