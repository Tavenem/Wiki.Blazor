let beforeStartComplete = false;
function tavenemBlazorWikiBeforeStart() {
    if (beforeStartComplete) {
        return;
    }
    beforeStartComplete = true;

    addHeadContent();
}

export function beforeStart() { tavenemBlazorWikiBeforeStart(); }

export function beforeWebStart() { tavenemBlazorWikiBeforeStart(); }

function addHeadContent() {
    const script = document.createElement('script');
    script.type = 'module';
    script.src = './_content/Tavenem.Wiki.Blazor.Client/tavenem-wiki.js';
    script.async = true;
    document.head.appendChild(script);

    const style = document.createElement('link');
    style.rel = 'stylesheet';
    style.type = 'text/css';
    style.href = "_content/Tavenem.Wiki.Blazor.Client/tavenem-wiki.css";
    document.head.appendChild(style);
}

function onEnhancedLoad() {
    addHeadContent();
}

Blazor.addEventListener('enhancedload', onEnhancedLoad);