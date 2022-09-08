namespace Tavenem.Wiki.Blazor;

/// <summary>
/// A service which persists and retrieves files associated with <see cref="WikiFile"/> items.
/// </summary>
public interface IFileManager
{
    /// <summary>
    /// Remove the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// <see langword="true"/> if the file was successfully removed; otherwise <see
    /// langword="false"/>. Also returns <see langword="true"/> if the given file does not exist
    /// (to indicate no issues "removing" it).
    /// </returns>
    public ValueTask<bool> DeleteFileAsync(string? path);

    /// <summary>
    /// Determine the amount of free storage space (in bytes) for the given <paramref
    /// name="user"/>.
    /// </summary>
    /// <param name="user">
    /// <para>
    /// An <see cref="IWikiUser"/>.
    /// </para>
    /// <para>
    /// If <see langword="null" /> zero will be returned.
    /// </para>
    /// </param>
    /// <returns>
    /// The total free storage space (in bytes) for the given <paramref name="user"/>. Or -1 if
    /// the user may upload without limit. Or zero if <paramref name="user"/> is <see
    /// langword="null" />, or an error occurs.
    /// </returns>
    public ValueTask<long> GetFreeSpaceAsync(IWikiUser? user);

    /// <summary>
    /// Gets the total storage size (in bytes) of items owned by the user with the given ID.
    /// </summary>
    /// <param name="userId">
    /// <para>
    /// The ID of a user.
    /// </para>
    /// <para>
    /// If <see langword="null" /> zero will be returned.
    /// </para>
    /// </param>
    /// <returns>
    /// The total storage size (in bytes) of items owned by the user with the given ID. Or zero
    /// if <see langword="null" /> is given, or an error occurs. Or <see cref="ulong.MaxValue"/>
    /// if the total exceeds that amount.
    /// </returns>
    public ValueTask<long> GetUsageAsync(string? userId);

    /// <summary>
    /// Determine if the given <paramref name="user"/> has enough free storage space to store a
    /// file with the given size.
    /// </summary>
    /// <param name="user">
    /// <para>
    /// An <see cref="IWikiUser"/>.
    /// </para>
    /// <para>
    /// If <see langword="null" /> <see langword="false"/> will be returned.
    /// </para>
    /// </param>
    /// <param name="size">
    /// The size (in bytes) of the item to be stored.
    /// </param>
    /// <returns>
    /// The total storage size (in bytes) of items owned by the given <paramref name="user"/>.
    /// Or zero if <paramref name="user"/> is <see langword="null" />, or an error occurs. Or
    /// <see cref="ulong.MaxValue"/> if the total exceeds that amount.
    /// </returns>
    public ValueTask<bool> HasFreeSpaceAsync(IWikiUser? user, long size);

    /// <summary>
    /// Load the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// A <see cref="byte"/> array containing the file; or <see langword="null"/> if no such
    /// file was found.
    /// </returns>
    public ValueTask<byte[]?> LoadFileAsync(string? path);

    /// <summary>
    /// Save the given file to a persistence store.
    /// </summary>
    /// <param name="data">A byte array containing the file.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="userId">
    /// The ID of the owner of the file. May be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The path of the stored file, if it was successfully saved; otherwise <see
    /// langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The returned path is the relative URL to the file.
    /// </remarks>
    public ValueTask<string?> SaveFileAsync(byte[]? data, string? fileName, string? userId = null);

    /// <summary>
    /// Save the given file to a persistence store.
    /// </summary>
    /// <param name="data">A stream containing the file.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="userId">
    /// The ID of the owner of the file. May be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The path of the stored file, if it was successfully saved; otherwise <see
    /// langword="null"/>.
    /// </returns>
    /// <remarks>
    /// The returned path is the relative URL to the file.
    /// </remarks>
    public ValueTask<string?> SaveFileAsync(Stream? data, string? fileName, string? userId = null);

    /// <summary>
    /// Load the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// A <see cref="Stream"/> containing the file; or <see langword="null"/> if no such file
    /// was found.
    /// </returns>
    public ValueTask<Stream?> StreamFileAsync(string? path);
}
