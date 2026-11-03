
namespace Iguina.Entities
{
    /// <summary>
    /// Provides text entries (usually lines) for a <see cref="VirtualTextView"/> entity.
    /// The view only asks for the entries it currently shows, so the source can be huge (for example a big log file) without loading all of it into the UI.
    /// </summary>
    public interface IVirtualTextProvider
    {
        /// <summary>
        /// Get how many entries are available.
        /// Called every frame the view is drawn, so it should be fast.
        /// The value may change between calls (for example when a log file grows).
        /// </summary>
        /// <returns>Total number of entries.</returns>
        int GetEntriesCount();

        /// <summary>
        /// Get a single entry by index.
        /// Called every frame for every visible entry, so it should be fast (cache data if reading it is expensive).
        /// </summary>
        /// <param name="index">Entry index, from 0 to GetEntriesCount() - 1.</param>
        /// <returns>Entry text. Should be a single line.</returns>
        string GetEntry(int index);
    }

    /// <summary>
    /// A virtual text provider that wraps a list of strings.
    /// The list is not copied, so changes to it (like adding lines) are reflected in the view.
    /// </summary>
    public class ListVirtualTextProvider : IVirtualTextProvider
    {
        /// <summary>
        /// The list of entries to provide.
        /// </summary>
        public IReadOnlyList<string> Entries;

        /// <summary>
        /// Create the provider.
        /// </summary>
        /// <param name="entries">List of entries to provide.</param>
        public ListVirtualTextProvider(IReadOnlyList<string> entries)
        {
            Entries = entries;
        }

        /// <inheritdoc/>
        public int GetEntriesCount() => Entries.Count;

        /// <inheritdoc/>
        public string GetEntry(int index) => Entries[index];
    }

    /// <summary>
    /// A virtual text provider that uses callbacks, so you don't need to implement a class for simple cases.
    /// </summary>
    public class CallbackVirtualTextProvider : IVirtualTextProvider
    {
        readonly Func<int> _getEntriesCount;
        readonly Func<int, string> _getEntry;

        /// <summary>
        /// Create the provider.
        /// </summary>
        /// <param name="getEntriesCount">Callback to return entries count.</param>
        /// <param name="getEntry">Callback to return entry by index.</param>
        public CallbackVirtualTextProvider(Func<int> getEntriesCount, Func<int, string> getEntry)
        {
            _getEntriesCount = getEntriesCount;
            _getEntry = getEntry;
        }

        /// <inheritdoc/>
        public int GetEntriesCount() => _getEntriesCount();

        /// <inheritdoc/>
        public string GetEntry(int index) => _getEntry(index);
    }
}
