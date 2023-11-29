window.wikiblazor = window.wikiblazor || {};
window.wikiblazor.references = window.wikiblazor.references || {};
window.wikiblazor.timer = window.wikiblazor.timer || -1;

export function dispose(id) {
    delete window.wikiblazor.references[id];
}

export function initialize(id, dotNetObjectReference) {
    window.wikiblazor.references[id] = dotNetObjectReference;
}

export function scrollIntoView(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}