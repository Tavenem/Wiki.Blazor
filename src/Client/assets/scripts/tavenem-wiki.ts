import { TavenemChatEditorHTMLElement } from './tavenem-chat-editor';

interface WikiBlazorReference {
    link?: string;
    text?: string;
}

interface WikiBlazor {
    displayPreview?: (e: MouseEvent, text: string, wiki: Element) => void;
    hidePreview?: () => void;
    references?: Record<string, WikiBlazorReference[]>;
    scrollIntoView?: (elementId: string) => void;
    showPreview?: (e: Event, link: string) => void;
    timer?: number;
}

declare global {
    interface Window { wikiblazor?: WikiBlazor; }
}

namespace TavenemWiki {
    export function initialize() {
        window.wikiblazor = window.wikiblazor || {};
        window.wikiblazor.references = window.wikiblazor.references || {};
        window.wikiblazor.timer = window.wikiblazor.timer || -1;

        window.wikiblazor.displayPreview = function (e: MouseEvent, text: string, wiki: Element) {
            let popover = wiki.querySelector('tf-popover.wiki-link-preview') as HTMLElement;
            if (!popover) {
                popover = document.createElement('tf-popover');
                popover.classList.add('wiki-link-preview', 'flip-onopen', 'p-3');
                popover.popover = 'auto';
                popover.style.maxHeight = '50vh';
                popover.style.maxWidth = '90vw';
                popover.style.overflowY = 'auto';
                popover.tabIndex = 0;
                popover.dataset.anchorOrigin = 'top-left';
                popover.dataset.origin = 'top-left';
                wiki.appendChild(popover);
            }
            popover.dataset.positionX = e.clientX.toString();
            popover.dataset.positionY = e.clientY.toString();
            popover.innerHTML = text;
            if (typeof (popover as any).show === 'function') {
                (popover as any).show();
            }
        }

        window.wikiblazor.showPreview = function (e: Event, link: string) {
            if (!window.wikiblazor) {
                return;
            }
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

            window.wikiblazor.timer = setTimeout(async function () {
                if (!window.wikiblazor
                    || !window.wikiblazor.references) {
                    return;
                }
                let ref = window.wikiblazor.references[id];
                if (ref && typeof ref.length == 'number') {
                    for (let i = 0; i < ref.length; i++) {
                        if (ref[i].link === link) {
                            if (ref[i].text) {
                                window.wikiblazor.displayPreview!(e, ref[i].text!, wiki);
                                return;
                            }
                        }
                    }

                    if (ref.length >= 3) {
                        ref[0] = ref[1];
                        ref[1] = ref[2];
                        ref.pop();
                    }
                }

                const response = await fetch(`${link}/preview`);
                const text = await response.text();
                ref = ref || [];
                ref.push({ link, text });
                window.wikiblazor.displayPreview!(e, text, wiki);
            }, 1500);
        }

        window.wikiblazor.hidePreview = function () {
            if (!window.wikiblazor) {
                return;
            }
            if (window.wikiblazor.timer != -1) {
                clearTimeout(window.wikiblazor.timer);
            }

            for (const refId in window.wikiblazor.references) {
                const wiki = document.getElementById(refId);
                if (!wiki) {
                    continue;
                }
                const preview = wiki.querySelector('tf-popover.wiki-link-preview');
                if (preview && typeof (preview as any).hide === 'function') {
                    (preview as any).hide();
                }
            }
        }

        window.wikiblazor.scrollIntoView = function (elementId: string) {
            const element = document.getElementById(elementId);
            if (element) {
                element.scrollIntoView({ behavior: 'smooth' });
            }
        }

        const body = document.querySelector('body');
        if (body) {
            let oldHref = document.location.href;
            const observer = new MutationObserver(_ => {
                if (oldHref !== document.location.href) {
                    oldHref = document.location.href;
                    window.wikiblazor!.hidePreview!();
                }
            });
            observer.observe(body, { childList: true, subtree: true });
        }

        registerWikiComponents();
    }

    function registerWikiComponents() {
        const chat_editor = customElements.get('tf-chat-editor');
        if (chat_editor) {
            return;
        }

        customElements.define('tf-chat-editor', TavenemChatEditorHTMLElement);
    }
}

TavenemWiki.initialize();