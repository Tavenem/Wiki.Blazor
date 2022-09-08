import { autoTheme, EmojiSelection, NativeRenderer } from 'picmo';
import { createPopup } from '@picmo/popup-picker';

export function initialize(dotNetObjectReference: any, triggerElementId: string, referenceElementId: string) {
    const trigger = document.getElementById(triggerElementId);
    if (!trigger) {
        return;
    }

    let reference;
    if (referenceElementId) {
        reference = document.getElementById(referenceElementId);
    }
    if (!reference) {
        reference = trigger;
    }

    const popup = createPopup({
        className: 'tavenem-emoji-picker',
        renderer: new NativeRenderer(),
        theme: autoTheme,
    }, {
        position: 'auto',
        referenceElement: reference,
        triggerElement: trigger,
    });
    popup.addEventListener('emoji:select', (selection: EmojiSelection) => {
        dotNetObjectReference.invokeMethodAsync('Tavenem.Wiki.Blazor.Client', 'PostEmoji', selection.emoji);
    });
}