using System.Collections.Generic;

namespace Metatron.Core
{
    public delegate void FileReadCallback<T>(List<T> result) where T : new();

    public sealed class FileReadStateObject<T> where T : new()
    {
        public string FilePath = string.Empty;
        public FileReadCallback<T> Callback;

        public FileReadStateObject(string filePath, FileReadCallback<T> callback)
        {
            FilePath = filePath;
            Callback = callback;
        }
    }
}
