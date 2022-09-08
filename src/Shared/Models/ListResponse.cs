using Tavenem.DataStorage;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Models;

/// <summary>
/// A special list response object.
/// </summary>
public record ListResponse(PagedListDTO<LinkInfo> Links);
