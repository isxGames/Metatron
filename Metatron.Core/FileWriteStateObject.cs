namespace Metatron.Core
{
    public delegate void FileWriteCallback();

    public sealed class FileWriteStateObject<T> where T : new()
    {
        public string FilePath = string.Empty;
        public FileWriteCallback Callback;
        public T ObjectToWrite;

        public FileWriteStateObject(string filePath, FileWriteCallback callback, T objectToWrite)
        {
            FilePath = filePath;
            Callback = callback;
            ObjectToWrite = objectToWrite;
        }
    }
}
