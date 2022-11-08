const references = {};

let tmr = -1;

window.wikiblazor = window.wikiblazor || {};

window.wikiblazor.showPreview = function (e, link) {
    if (tmr != -1) {
        clearTimeout(tmr);
    }

    if (!references) {
        return;
    }

    e = e || window.event;
    if (!(e instanceof MouseEvent)) {
        return;
    }

    const target = e.currentTarget;
    if (!(target instanceof HTMLElement)) {
        return;
    }

    const wiki = target.closest(".wiki");
    if (!wiki) {
        return;
    }

    const id = wiki.id;
    if (id.length == 0) {
        return;
    }

    const ref = references[id];
    if (!ref) {
        return;
    }

    tmr = setTimeout(function () {
        ref.invokeMethodAsync('ShowPreview', link, e.clientX, e.clientY);
    }, 1500);
}

window.wikiblazor.hidePreview = function () {
    if (tmr != -1) {
        clearTimeout(tmr);
    }

    for (const id in references) {
        var ref = references[id];
        if (ref) {
            ref.invokeMethodAsync('HidePreview');
        }
    }
}

export function dispose(id) {
    delete references[id];
}

export function initialize(id, dotNetObjectReference) {
    references[id] = dotNetObjectReference;
}

export function scrollIntoView(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}