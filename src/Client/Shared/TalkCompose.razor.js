export function getGifAutocomplete(apiKey, searchTerm) {
    const lang = (navigator.languages || ["en"])[0];
    const limit = 5;
    const response = await fetch(`https://tenor.googleapis.com/v2/autocomplete?key=${apiKey}&q=${searchTerm}&locale=${lang}&limit=${limit}`);
    if (response.status != 200) {
        return [];
    } else {
        const result = await response.json();
        return result["results"];
    }
}

export function searchGif(dotNetObjectReference, apiKey, searchTerm, pos) {
    const lang = (navigator.languages || ["en"])[0];
    let url = `https://tenor.googleapis.com/v2/search?q=${searchTerm}&key=${apiKey}&media_filter=nanogif,nanowebp_transparent,nanogif_transparent&locale=${lang}`;
    if (pos) {
        url += `&pos=${pos}`;
    }
    httpGetAsync(
        url,
        tenorCallback_search,
        dotNetObjectReference,
        (!!pos) ? 'AppendGifSearch' : 'PopulateGifSearch');
    if (!pos) {
        const limit = 4;
        httpGetAsync(
            `https://tenor.googleapis.com/v2/search_suggestions?key=${apiKey}&q=${searchTerm}&locale=${lang}&limit=${limit}`,
            tenorCallback_searchSuggestion,
            dotNetObjectReference,
            'GetGifSuggestions');
    }
}

export function shareGif(apiKey, id) {
    httpGetAsync(
        `https://tenor.googleapis.com/v2/posts?key=${apiKey}&ids=${id}&media_filter=tinygif,tinywebp_transparent,tinygif_transparent&limit=1`,
        tenorCallback_post,
        dotNetObjectReference,
        'PostGif');
    const lang = (navigator.languages || ["en"])[0];
    httpGetAsync(`https://tenor.googleapis.com/v2/registershare?id=${id}&key=${apiKey}&q=${searchTerm}&locale=${lang}`);
}

export function showGifSearch(dotNetObjectReference, apiKey) {
    const lang = (navigator.languages || ["en"])[0];
    httpGetAsync(
        `https://tenor.googleapis.com/v2/categories?key=${apiKey}&locale=${lang}`,
        tenorCallback_cateogries,
        dotNetObjectReference,
        'GetGifCategories');
    const suggestion_limit = 4;
    httpGetAsync(
        `https://tenor.googleapis.com/v2/trending_terms?key=${apiKey}&locale=${lang}&limit=${suggestion_limit}`,
        tenorCallback_trending,
        dotNetObjectReference,
        'GetGifSuggestions');
}

function httpGetAsync(theUrl, callback, dotNetObjectReference, identifier) {
    fetch(theUrl)
        .then(response => {
            if (callback && response.status == 200) {
                response.json()
                    .then(result => {
                        callback(dotNetObjectReference, identifier, result);
                    });
            }
        });
}

function tenorCallback_cateogries(dotNetObjectReference, identifier, response) {
    categories = response["tags"];
    const data = categories.map((v) => {
        return {
            image: v["image"],
            name: v["name"],
            searchTerm: v["searchterm"],
        };
    });
    dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', identifier, data);
}

function tenorCallback_post(dotNetObjectReference, identifier, response) {
    gifs = response["results"];
    let title;
    let url;
    for (const gif of gifs) {
        title = gif["title"];
        const mediaFormats = gif["media_formats"];
        for (const format in mediaFormats) {
            url = mediaFormats[format]["url"];
            if (url && url.length) {
                break;
            }
        }
    }
    dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', identifier, { title, url });
}

function tenorCallback_search(dotNetObjectReference, identifier, response) {
    gifs = response["results"];
    const data = {
        next: response["next"],
        gifs: [],
    };
    for (const gif of gifs) {
        const id = gif["id"];
        let url;
        const mediaFormats = gif["media_formats"];
        for (const format in mediaFormats) {
            url = mediaFormats[format]["url"];
            if (url && url.length) {
                break;
            }
        }
        data.gifs.push({ id, url });
    }
    dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', identifier, data);
}

function tenorCallback_searchSuggestion(dotNetObjectReference, identifier, response) {
    predictions = response["results"];
    dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', identifier, predictions);
}

function tenorCallback_trending(dotNetObjectReference, identifier, response) {
    terms = response["results"];
    dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', identifier, terms);
}