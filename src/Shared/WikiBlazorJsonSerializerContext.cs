using System.Text.Json.Serialization;
using Tavenem.Wiki.Blazor.Models;
using Tavenem.Wiki.Blazor.Services.Search;
using Tavenem.Wiki.Blazor.SignalR;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor;

/// <summary>
/// A source gererated serializer context for <c>Tavenem.Wiki.Blazor</c> types.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EditRequest))]
[JsonSerializable(typeof(ListResponse))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(PreviewRequest))]
[JsonSerializable(typeof(ReplyRequest))]
[JsonSerializable(typeof(SearchHit))]
[JsonSerializable(typeof(List<SearchHit>))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(TalkResponse))]
[JsonSerializable(typeof(UploadRequest))]
[JsonSerializable(typeof(Archive))]
[JsonSerializable(typeof(MarkdownItem))]
[JsonSerializable(typeof(List<MarkdownItem>))]
[JsonSerializable(typeof(List<Article>))]
[JsonSerializable(typeof(List<Category>))]
[JsonSerializable(typeof(List<WikiFile>))]
[JsonSerializable(typeof(List<Message>))]
[JsonSerializable(typeof(Revision))]
[JsonSerializable(typeof(List<Revision>))]
[JsonSerializable(typeof(IWikiOwner))]
[JsonSerializable(typeof(List<IWikiOwner>))]
[JsonSerializable(typeof(IWikiUser))]
[JsonSerializable(typeof(List<IWikiUser>))]
[JsonSerializable(typeof(WikiUser))]
[JsonSerializable(typeof(List<WikiUser>))]
[JsonSerializable(typeof(IWikiGroup))]
[JsonSerializable(typeof(List<IWikiGroup>))]
[JsonSerializable(typeof(WikiGroup))]
[JsonSerializable(typeof(List<WikiGroup>))]
[JsonSerializable(typeof(MissingPage))]
[JsonSerializable(typeof(NormalizedPageReference))]
[JsonSerializable(typeof(PageLinks))]
[JsonSerializable(typeof(PageRedirects))]
[JsonSerializable(typeof(PageReference))]
[JsonSerializable(typeof(PageTransclusions))]
[JsonSerializable(typeof(CategoryInfo))]
[JsonSerializable(typeof(GroupPageInfo))]
[JsonSerializable(typeof(HistoryRequest))]
[JsonSerializable(typeof(LinkInfo))]
[JsonSerializable(typeof(List<LinkInfo>))]
[JsonSerializable(typeof(PagedRevisionInfo))]
[JsonSerializable(typeof(SpecialListRequest))]
[JsonSerializable(typeof(UserPageInfo))]
[JsonSerializable(typeof(WhatLinksHereRequest))]
[JsonSerializable(typeof(WikiEditInfo))]
[JsonSerializable(typeof(WikiItemInfo))]
public partial class WikiBlazorJsonSerializerContext : JsonSerializerContext
{
}
