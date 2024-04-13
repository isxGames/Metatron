namespace Metatron.Core.Interfaces
{
    public interface IFileManager
    {
        void QueueDeserialize<T>(string filePath, FileReadCallback<T> callback) where T : new();
        void QueueOverwriteSerialize<T>(string filePath, T objectToWrite, FileWriteCallback callback) where T : new();
    }
}
