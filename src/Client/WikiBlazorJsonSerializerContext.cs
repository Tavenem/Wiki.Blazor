﻿using System.Text.Json.Serialization;
using Tavenem.DataStorage;
using Tavenem.Wiki.Models;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor;

/// <summary>
/// A source generated serializer context for <c>Tavenem.Wiki.Blazor</c> types.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EditRequest))]
[JsonSerializable(typeof(PagedList<LinkInfo>))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(List<MessageResponse>))]
[JsonSerializable(typeof(PreviewRequest))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(SearchHit))]
[JsonSerializable(typeof(List<SearchHit>))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(SearchResult))]
[JsonSerializable(typeof(UploadRequest))]
[JsonSerializable(typeof(List<WikiLink>))]
[JsonSerializable(typeof(List<string>))]
public partial class WikiBlazorJsonSerializerContext : JsonSerializerContext;
