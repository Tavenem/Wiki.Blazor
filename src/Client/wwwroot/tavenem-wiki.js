window.wikiblazor = window.wikiblazor || {};
window.wikiblazor.references = window.wikiblazor.references || {};
window.wikiblazor.timer = window.wikiblazor.timer || -1;

window.wikiblazor.showPreview = function (e, link) {
    if (window.wikiblazor.timer != -1) {
        clearTimeout(window.wikiblazor.timer);
    }

    if (!window.wikiblazor.references) {
        return;
    }

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

    const ref = window.wikiblazor.references[id];
    if (!ref) {
        return;
    }

    window.wikiblazor.timer = setTimeout(function () {
        ref.invokeMethodAsync('ShowPreview', link, e.clientX, e.clientY);
    }, 1500);
}

window.wikiblazor.hidePreview = function () {
    if (window.wikiblazor.timer != -1) {
        clearTimeout(window.wikiblazor.timer);
    }

    for (const refId in window.wikiblazor.references) {
        window.wikiblazor.references[refId].invokeMethodAsync('HidePreview');
    }
}