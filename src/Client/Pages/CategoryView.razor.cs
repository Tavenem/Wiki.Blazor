using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text;
using Tavenem.Blazor.Framework;
using Tavenem.Wiki.Queries;

namespace Tavenem.Wiki.Blazor.Client.Pages;

/// <summary>
/// The category page.
/// </summary>
public partial class CategoryView
{
    private CategoryInfo? CategoryInfo { get; set; }

    private MarkupString? Content { get; set; }

    [Inject] private HttpClient HttpClient { get; set; } = default!;

    [Inject] private SnackbarService SnackbarService { get; set; } = default!;

    [Inject] private IWikiBlazorClientOptions WikiBlazorClientOptions { get; set; } = default!;

    [Inject] private WikiOptions WikiOptions { get; set; } = default!;

    [Inject] private WikiState WikiState { get; set; } = default!;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (!string.Equals(
            WikiState.WikiNamespace,
            WikiOptions.CategoryNamespace,
            StringComparison.OrdinalIgnoreCase))
        {
            CategoryInfo = null;
            Content = null;
            return;
        }

        var serverApi = WikiBlazorClientOptions.WikiServerApiRoute
            ?? Client.WikiBlazorClientOptions.DefaultWikiServerApiRoute;
        try
        {
            var url = new StringBuilder(serverApi)
                .Append("/category?title=")
                .Append(WikiState.WikiTitle);
            var response = await HttpClient.GetAsync(url.ToString());
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                WikiState.NotAuthorized = true;
            }
            else if (response.IsSuccessStatusCode)
            {
                CategoryInfo = await response
                    .Content
                    .ReadFromJsonAsync(WikiBlazorJsonSerializerContext.Default.CategoryInfo);
                Content = string.IsNullOrEmpty(CategoryInfo?.Item?.Html)
                    ? null
                    : new MarkupString(CategoryInfo.Item.Html);
            }
            else
            {
                WikiState.LoadError = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            SnackbarService.Add("An error occurred", ThemeColor.Danger);
        }
    }
}