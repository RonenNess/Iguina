
namespace Iguina.Drivers
{
    /// <summary>
    /// An interface to provide file reading access.
    /// By default it uses the simple built-in C# text file reader, but you can override this to get files content from other sources.
    /// </summary>
    public interface IFilesProvider
    {
        /// <summary>
        /// Read entire text file from a given path.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>File content as string.</returns>
        public string ReadAllText(string path);
    }

    /// <summary>
    /// Built in files provider that simply read text files.
    /// </summary>
    public class DefaultFilesProvider : IFilesProvider
    {
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }
}
