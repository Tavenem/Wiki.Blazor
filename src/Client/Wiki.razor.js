export function initialize(id, dotNetObjectReference) {
    window.wikiblazor = window.wikiblazor || {};

    window.wikiblazor.tmr = -1;

    window.wikiblazor.references = window.wikiblazor.references || {};
    window.wikiblazor.references[id] = dotNetObjectReference;

    window.wikiblazor.showPreview = function (e, link) {
        if (window.wikiblazor.tmr != -1) {
            clearTimeout(window.wikiblazor.tmr);
        }

        if (!window.wikiblazor.references) {
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

        if (!window.wikiblazor.references.hasOwnProperty(id)) {
            return;
        }
        const ref = window.wikiblazor.references[id];
        if (!ref) {
            return;
        }

        window.wikiblazor.tmr = setTimeout(function () {
            ref.invokeMethodAsync('ShowPreview', link, e.clientX, e.clientY);
        }, 1500);
    }

    window.wikiblazor.hidePreview = function () {
        if (window.wikiblazor.tmr != -1) {
            clearTimeout(window.wikiblazor.tmr);
        }

        for (const id in window.wikiblazor.references) {
            var ref = window.wikiblazor.references[id];
            if (ref) {
                ref.invokeMethodAsync('HidePreview');
            }
        }
    }
}