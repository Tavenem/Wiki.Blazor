using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;
using Tavenem.Blazor.Framework;
using Tavenem.DataStorage;

namespace Tavenem.Wiki.Blazor.Sample.Shared;

public partial class TopAppBar
{
    [Inject] private IDataStore DataStore { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private UtilityService UtilityService { get; set; } = default!;

    private async Task GetArchiveAsync()
    {
        var archive = await DataStore.GetWikiArchiveAsync(WikiOptions);

        using var ms = new MemoryStream();
        JsonSerializer.Serialize(
            ms,
            archive,
            new JsonSerializerOptions
            {
                TypeInfoResolver = WikiBlazorJsonSerializerContext.Default,
                WriteIndented = true,
            });
        ms.Position = 0;
        using var streamReference = new DotNetStreamReference(ms);
        await UtilityService.DownloadAsync("archive.json", "application/json", streamReference);
    }
}