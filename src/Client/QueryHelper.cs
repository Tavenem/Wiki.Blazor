using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Tavenem.Wiki.Blazor.Client;

internal static class QueryHelper
{
    public static T? GetQueryParam<T>(this NavigationManager navigationManager, string key)
    {
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);

        if (QueryHelpers
            .ParseQuery(uri.Query)
            .TryGetValue(key, out var valueFromQueryString))
        {
            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(valueFromQueryString, out var valueAsBool))
                {
                    return (T)(object)valueAsBool;
                }
                else
                {
                    return (T)(object)false;
                }
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)valueFromQueryString.ToString();
            }

            if (typeof(T) == typeof(int?)
                && int.TryParse(valueFromQueryString, out var valueAsInt))
            {
                return (T)(object)valueAsInt;
            }

            if (typeof(T) == typeof(long?)
                && int.TryParse(valueFromQueryString, out var valueAsLong))
            {
                return (T)(object)valueAsLong;
            }
        }

        if (typeof(T) == typeof(bool))
        {
            return (T)(object)false;
        }

        return default;
    }
}
