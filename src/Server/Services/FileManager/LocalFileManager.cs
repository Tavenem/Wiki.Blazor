using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tavenem.Wiki.Blazor.Services.FileManager;

/// <summary>
/// A service which persists and retrieves files associated with <see cref="WikiFile"/> items.
/// This implementation stores files in a subfolder of wwwroot named "files" (created on
/// demand). Files with owners are placed in subfolders named for the owner's ID (sanitized by
/// replacing any disallowed characters with an underscore). Unowned items are placed directly
/// in the "files" folder. Files are given random filenames (i.e. the filename specified when
/// saving is not used to determine the actual name of the file), for security purposes.
/// </summary>
public class LocalFileManager : IFileManager
{
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<IFileManager> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalFileManager"/>.
    /// </summary>
    public LocalFileManager(
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor,
        ILogger<IFileManager> logger)
    {
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Remove the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// <see langword="true"/> if the file was successfully removed; otherwise <see
    /// langword="false"/>. Also returns <see langword="true"/> if the given file does not exist
    /// (to indicate no issues "removing" it).
    /// </returns>
#pragma warning disable RCS1229 // Use async/await when necessary: Not async.
    public ValueTask<bool> DeleteFileAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new ValueTask<bool>(false);
        }
        try
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                throw new Exception("Files cannot be deleted outside of an HTTP request context.");
            }
            var filePath = Path.Combine(_environment.WebRootPath, "files", path);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            return new ValueTask<bool>(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception deleting file at path {Path}", path);
            return new ValueTask<bool>(false);
        }
    }
#pragma warning restore RCS1229 // Use async/await when necessary.

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
    public async ValueTask<long> GetFreeSpaceAsync(IWikiUser? user)
    {
        if (user is null
            || user.UploadLimit == 0)
        {
            return 0;
        }
        if (user.UploadLimit < 0)
        {
            return -1;
        }
        try
        {
            var used = await GetUsageAsync(user.Id).ConfigureAwait(false);
            var limit = (long)(user.UploadLimit * 1000);
            if (used >= limit)
            {
                return 0;
            }
            return limit - used;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting free storage space for user {User}", user.Id);
            return 0;
        }
    }

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
#pragma warning disable RCS1229 // Use async/await when necessary: Not async.
    public ValueTask<long> GetUsageAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new ValueTask<long>(0);
        }
        try
        {
            var filesPath = Path.Combine(_environment.WebRootPath, "files");
            if (!Directory.Exists(filesPath))
            {
                return new ValueTask<long>(0);
            }
            var ownerPathName = string.Join('_', userId.Split(Path.GetInvalidFileNameChars()));
            var ownerPath = Path.Combine(filesPath, ownerPathName);
            if (!Directory.Exists(ownerPath))
            {
                return new ValueTask<long>(0);
            }

            var total = 0L;
            foreach (var file in Directory.EnumerateFiles(ownerPath))
            {
                var size = new FileInfo(file).Length;
                if (size >= long.MaxValue - total)
                {
                    return new ValueTask<long>(long.MaxValue);
                }
                total += size;
            }
            return new ValueTask<long>(total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting storage usage for user {User}", userId);
            return new ValueTask<long>(0);
        }
    }
#pragma warning restore RCS1229 // Use async/await when necessary.

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
    public async ValueTask<bool> HasFreeSpaceAsync(IWikiUser? user, long size)
    {
        if (user is null)
        {
            return false;
        }
        try
        {
            var freeSpace = await GetFreeSpaceAsync(user).ConfigureAwait(false);
            return freeSpace >= size;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting free storage space availability for user {User} for item with size {Size}", user.Id, size);
            return false;
        }
    }

    /// <summary>
    /// Load the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// A <see cref="byte" /> array containing the file; or <see langword="null" /> if no such
    /// file was found.
    /// </returns>
    public async ValueTask<byte[]?> LoadFileAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        try
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                throw new Exception("Files cannot be loaded outside of an HTTP request context.");
            }
            var baseUrl = new Uri(request.Scheme + "://" + request.Host + request.PathBase);
            using var client = new HttpClient { BaseAddress = baseUrl };
            return await client
                .GetByteArrayAsync(path)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading file at path {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Save the given file to a persistence store.
    /// </summary>
    /// <param name="data">A byte array containing the file.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="userId">
    /// The ID of the owner of the file. May be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The path of the stored file, if it was successfully saved; otherwise <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// The returned path is the relative URL to the file.
    /// </remarks>
    public async ValueTask<string?> SaveFileAsync(byte[]? data, string? fileName, string? userId = null)
    {
        if (data is null)
        {
            return null;
        }
        var filesPath = Path.Combine(_environment.WebRootPath, "files");
        try
        {
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 'files' directory creation.");
            return null;
        }

        var filePathName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(fileName);
        string filePath;

        var hasOwner = !string.IsNullOrWhiteSpace(userId);
        string? ownerPathName = null;
        if (hasOwner)
        {
            ownerPathName = string.Join('_', userId!.Split(Path.GetInvalidFileNameChars()));
            var ownerPath = Path.Combine(filesPath, ownerPathName);
            try
            {
                if (!Directory.Exists(ownerPath))
                {
                    Directory.CreateDirectory(ownerPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 'files' subfolder creation for owner {Owner}.", userId);
                return null;
            }
            filePath = Path.Combine(ownerPath, filePathName);
        }
        else
        {
            filePath = Path.Combine(filesPath, filePathName);
        }

        try
        {
            var file = File.Create(filePath);
            await file.WriteAsync(data.AsMemory(0, data.Length)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception saving file at path {Path}.", filePath);
            return null;
        }

        return string.IsNullOrEmpty(userId)
            ? $"/{filePathName}"
            : $"/{ownerPathName}/{filePathName}";
    }

    /// <summary>
    /// Save the given file to a persistence store.
    /// </summary>
    /// <param name="data">A stream containing the file.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="userId">
    /// The ID of the owner of the file. May be <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The path of the stored file, if it was successfully saved; otherwise <see langword="null" />.
    /// </returns>
    /// <remarks>
    /// The returned path is the relative URL to the file.
    /// </remarks>
    public async ValueTask<string?> SaveFileAsync(Stream? data, string? fileName, string? userId = null)
    {
        if (data is null)
        {
            return null;
        }
        var filesPath = Path.Combine(_environment.WebRootPath, "files");
        try
        {
            if (!Directory.Exists(filesPath))
            {
                Directory.CreateDirectory(filesPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during 'files' directory creation.");
            return null;
        }

        var filePathName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.GetExtension(fileName);
        string filePath;

        var hasOwner = !string.IsNullOrWhiteSpace(userId);
        string? ownerPathName = null;
        if (hasOwner)
        {
            ownerPathName = string.Join('_', userId!.Split(Path.GetInvalidFileNameChars()));
            var ownerPath = Path.Combine(filesPath, ownerPathName);
            try
            {
                if (!Directory.Exists(ownerPath))
                {
                    Directory.CreateDirectory(ownerPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 'files' subfolder creation for owner {Owner}.", userId);
                return null;
            }
            filePath = Path.Combine(ownerPath, filePathName);
        }
        else
        {
            filePath = Path.Combine(filesPath, filePathName);
        }

        try
        {
            var file = File.Create(filePath);
            await data.CopyToAsync(file).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception saving file at path {Path}.", filePath);
            return null;
        }

        return string.IsNullOrEmpty(userId)
            ? $"/{filePathName}"
            : $"/{ownerPathName}/{filePathName}";
    }

    /// <summary>
    /// Load the given file from a persistence store.
    /// </summary>
    /// <param name="path">The path to the file. A relative URL is expected.</param>
    /// <returns>
    /// A <see cref="Stream" /> containing the file; or <see langword="null" /> if no such file
    /// was found.
    /// </returns>
    public async ValueTask<Stream?> StreamFileAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }
        try
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is null)
            {
                throw new Exception("Files cannot be loaded outside of an HTTP request context.");
            }
            var baseUrl = new Uri(request.Scheme + "://" + request.Host + request.PathBase);
            using var client = new HttpClient { BaseAddress = baseUrl };
            return await client
                .GetStreamAsync(path)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception loading file at path {Path}", path);
            return null;
        }
    }
}
