export class TavenemChatEditorHTMLElement extends HTMLElement {
    static formAssociated = true;
    static style = `
:host  {
    display: flex;
    flex-flow: column nowrap;
    gap: .25rem;
    margin-bottom: .25rem;
    margin-top: .25rem;
}

button {
    --button-bg: var(--tavenem-color-default);
    --button-color: var(--tavenem-color-default-text);
    --button-hover-bg: var(--tavenem-color-default-darken);
    --button-hover-color: var(--tavenem-color-default-text);
    align-items: center;
    background-color: var(--button-bg);
    border: none;
    border-radius: var(--tavenem-border-radius);
    box-shadow: 0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12);
    box-sizing: border-box;
    color: var(--button-color);
    cursor: pointer;
    display: inline-flex;
    flex: 0 0 auto;
    font-size: var(--tavenem-font-size-button)
    font-weight: var(--tavenem-font-weight-semibold);
    gap: .25rem;
    justify-content: center;
    line-height: var(--tavenem-lineheight-button);
    margin: 0;
    min-width: 4rem;
    outline: 0;
    overflow: hidden;
    padding: 6px 16px;
    position: relative;
    text-decoration: none;
    text-transform: var(--tavenem-texttransform-button);
    transition: background-color 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms,box-shadow 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms,border 250ms cubic-bezier(0.4, 0, 0.2, 1) 0ms;
    user-select: none;
    vertical-align: middle;
    -moz-appearance: none;
    -webkit-appearance: none;
    -webkit-tap-highlight-color: transparent;

    &:after {
        background-image: radial-gradient(circle,#000 10%,transparent 10.01%);
        background-position: 50%;
        background-repeat: no-repeat;
        content: "";
        display: block;
        height: 100%;
        left: 0;
        opacity: 0;
        pointer-events: none;
        position: absolute;
        top: 0;
        transform: scale(10,10);
        transition: transform .3s,opacity 1s;
        width: 100%;
    }

    &:hover,
    &:focus-visible {
        background-color: var(--button-hover-bg);
        box-shadow: 0px 2px 4px -1px rgba(0, 0, 0, 0.2), 0px 4px 5px 0px rgba(0, 0, 0, 0.14), 0px 1px 10px 0px rgba(0,0,0,.12);
        color: var(--button-hover-color);
    }
}
button::-moz-focus-inner {
    border-style: none;
}

.primary {
    --button-bg: var(--tavenem-theme-color);
    --button-color: var(--tavenem-theme-color-text);
    --button-hover-bg: var(--tavenem-theme-color-darken);
    --button-hover-color: var(--tavenem-theme-color-text);
    color: var(--button-color);
}

.more-button {
    align-self: center;
    justify-self: center;
}

.rounded-pill {
    border-radius: 9999px;
}

.message-editor  {
    display: flex;
    flex-flow: column nowrap;
    gap: .25rem;
}

.controls {
    display: flex;
    flex-wrap: wrap;
    gap: .5rem;
    justify-content: space-between;
}

.gif-picker {
    align-items: stretch;
    background-color: var(--tavenem-color-bg);
    border: 1px solid var(--tavenem-color-border);
    border-radius: var(--tavenem-border-radius);
    display: none;
    flex-flow: column nowrap;
    height: 26rem;
    max-width: 100%;
    padding: .25rem;
    width: 20rem;
}

menu {
    color: var(--tavenem-color-action);
    display: flex;
    flex-direction: column;
    list-style: none;
    margin: 0;
    overflow: auto;
    padding-bottom: .25em;
    padding-left: .75em;
    padding-right: .75em;
    padding-top: .25em;
    position: relative;
    scrollbar-gutter: stable;
    transition: all 300ms cubic-bezier(0.4, 0, 0.2, 1) 0ms;

    > * {
        align-items: center;
        background-color: transparent;
        border: 0;
        border-radius: 0;
        box-sizing: border-box;
        color: inherit;
        column-gap: .5em;
        cursor: pointer;
        display: flex;
        flex: 0 0 auto;
        flex-wrap: wrap;
        justify-content: flex-start;
        list-style: none;
        margin: 0;
        outline: 0;
        overflow: hidden;
        padding-bottom: .25em;
        padding-inline-end: .25em;
        padding-inline-start: .25em;
        padding-top: .25em;
        position: relative;
        text-align: start;
        text-decoration: none;
        transition: background-color 150ms cubic-bezier(.4,0,.2,1) 0ms;
        user-select: none;
        vertical-align: middle;
        -webkit-appearance: none;
        -webkit-tap-highlight-color: transparent;

        &:focus-visible, &:hover {
            color: inherit;
        }

        &:focus-visible {
            background-color: transparent;
        }

        &:hover,
        &:focus:not(.active) {
            background-color: var(--tavenem-color-action-hover-bg);
        }

        &:after {
            background-image: radial-gradient(circle,#000 10%,transparent 10.01%);
            background-position: 50%;
            background-repeat: no-repeat;
            content: "";
            display: block;
            height: 100%;
            left: 0;
            opacity: 0;
            pointer-events: none;
            position: absolute;
            top: 0;
            transform: scale(10,10);
            transition: transform .3s,opacity 1s;
            width: 100%;
        }

        &:active:after {
            transform: scale(0,0);
            opacity: .1;
            transition: 0s;
        }
    }

    > .active {
        background-color: var(--tavenem-color-primary-hover);
        border-inline-end: 1px solid var(--tavenem-color-primary);
        color: var(--tavenem-color-primary);
        padding-inline-end: calc(1em - 1px);

        &:hover {
            background-color: var(--tavenem-color-primary-hover-bright);
        }
    }
}

.suggestions {
    display: flex;
    flex-wrap: wrap;
    gap: .5rem;
    margin-top: .5rem;
    margin-bottom: .5rem;
}

.gif-content {
    display: flex;
    flex-wrap: wrap;
    flex-grow: 1;
    justify-content: space-evenly;
    overflow-x: hidden;
    overflow-y: auto;
}

.gif-item {
    align-items: center;
    color: white;
    cursor: pointer;
    display: flex;
    max-height: 9em;
    max-width: 9em;
    position: relative;
    text-align: center;

    img {
        max-width: inherit;
    }

    span {
        background-color: #000;
        background-color: rgba(0,0,0,0.5);
        border-radius: var(--tavenem-border-radius);
        left: 50%;
        padding-left: .25rem;
        padding-right: .25rem;
        position: absolute;
        top: 50%;
        transform: translate(-50%, -50%);
    }
}
`;

    private _gifsCategories: {
        image: string,
        name: string,
        searchTerm: string,
    }[] | undefined;
    private _gifShown = false;
    private _initialValue: string | null | undefined;
    private _internals: ElementInternals;
    private _next: string | undefined;
    private _searchTerm: string | undefined;
    private _settingValue = false;
    private _suggestionTimer = -1;
    private _value = '';

    static get observedAttributes() { return ['value']; }

    private static newValueChangeEvent(value: string) {
        return new CustomEvent('valuechange', { bubbles: true, composed: true, detail: { value: value } });
    }

    get form() { return this._internals.form; }
    get name() { return this.getAttribute('name'); }
    get type() { return this.localName; }

    get validity() { return this._internals.validity; }
    get validationMessage() { return this._internals.validationMessage; }

    get value() { return this._value; }
    set value(v: string) { this.setValue(v); }

    get willValidate() { return this._internals.willValidate; }

    constructor() {
        super();

        this._internals = this.attachInternals();
    }

    async connectedCallback() {
        const shadow = this.attachShadow({ mode: 'open', delegatesFocus: true });

        const style = document.createElement('style');
        style.textContent = TavenemChatEditorHTMLElement.style;
        shadow.appendChild(style);

        const messageEditor = document.createElement('div');
        messageEditor.classList.add('message-editor');
        shadow.appendChild(messageEditor);

        const disabled = this.hasAttribute('disabled')
            && !!this.getAttribute('disabled');

        const editor = document.createElement('tf-editor');
        editor.dataset.lockSyntax = '';
        editor.dataset.syntax = 'handlebars';
        if (disabled) {
            editor.setAttribute('disabled', '');
        }
        editor.setAttribute('height', '10rem');
        editor.setAttribute('max-height', '10rem');
        editor.setAttribute('placeholder', 'Type a message');
        editor.spellcheck = true;
        editor.setAttribute('wysiwyg', '');
        editor.addEventListener('valuechange', this.onEditorValueChange.bind(this))
        messageEditor.appendChild(editor);

        const controls = document.createElement('div');
        controls.classList.add('controls');
        messageEditor.appendChild(controls);

        if ('apiKey' in this.dataset
            && this.dataset.apiKey
            && this.dataset.apiKey.length) {
            const gifButton = document.createElement('button');
            gifButton.classList.add('gif-toggle-button');
            gifButton.disabled = disabled;
            gifButton.textContent = 'GIF';
            gifButton.addEventListener('click', this.toggleGifControls.bind(this));
            controls.appendChild(gifButton);
        }

        const postButton = document.createElement('button');
        postButton.classList.add('primary', 'post-button');
        postButton.disabled = disabled;
        postButton.textContent = 'Post';
        postButton.addEventListener('click', this.onPost.bind(this));
        controls.appendChild(postButton);

        const gifPicker = document.createElement('div');
        gifPicker.classList.add('gif-picker');
        shadow.appendChild(gifPicker);

        const hiddenInput = document.createElement('input');
        hiddenInput.hidden = true;
        hiddenInput.type = 'hidden';
        gifPicker.appendChild(hiddenInput);

        const inputId = randomUUID();
        const input = document.createElement('tf-select');
        input.id = inputId;
        input.classList.add('clearable', 'dense');
        input.dataset.disableAutosearch = '';
        input.dataset.hasTextInput = '';
        input.dataset.hideExpand = '';
        input.dataset.popoverLimitHeight = '';
        input.dataset.searchFilter = '';
        input.role = 'search';
        if ('label' in this.dataset) {
            input.dataset.label = this.dataset.label;
        }
        if (this.hasAttribute('placeholder')) {
            input.setAttribute('placeholder', this.getAttribute('placeholder') || '');
        }
        input.setAttribute(
            'size',
            Math.max(
                1,
                (this.getAttribute('placeholder') || '').length,
                (this.dataset.label || '').length)
                .toString());
        input.addEventListener('valuechange', this.onGifSearchValueChange.bind(this));
        input.addEventListener('enter', this.onGifSearchValueChange.bind(this));
        input.addEventListener('valueinput', this.onGifSearchValueInput.bind(this));
        gifPicker.appendChild(input);

        const postfixIcon = document.createElement('tf-icon');
        postfixIcon.slot = 'postfix';
        postfixIcon.textContent = 'search';
        input.appendChild(postfixIcon);

        const menu = document.createElement('menu');
        menu.slot = 'popover';
        input.appendChild(menu);

        const suggestions = document.createElement('div');
        suggestions.classList.add('suggestions');
        gifPicker.appendChild(suggestions);

        const gifContent = document.createElement('div');
        gifContent.classList.add('gif-content');
        gifPicker.appendChild(gifContent);

        if (this.hasAttribute('value')) {
            this._settingValue = true;

            this._value = this.getAttribute('value') || '';

            if (this._value.length) {
                this._internals.setFormValue(this._value);
            } else {
                this._internals.setFormValue(null);
            }

            this._initialValue = this._value;

            if (typeof (editor as any).setValue === 'function') {
                (editor as any).setValue(this._value);
            } else {
                editor.setAttribute('value', this._value);
            }

            this._settingValue = false;
        }

        await this.loadAsync();
    }

    disconnectedCallback() {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const input = root.querySelector('tf-input');
        if (input) {
            input.removeEventListener('valuechange', this.onGifSearchValueInput.bind(this));
            input.removeEventListener('valueinput', this.onGifSearchValueInput.bind(this));
        }
    }

    attributeChangedCallback(name: string, oldValue: string | null | undefined, newValue: string | null | undefined) {
        if (newValue == oldValue) {
            return;
        }

        if (name === 'value'
            && newValue) {
            this.setValue(newValue);
            return;
        }
    }

    formDisabledCallback(disabled: boolean) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }

        const input = root.querySelector('tf-input');
        if (input && typeof (input as any).formDisabledCallback === 'function') {
            (input as any).formDisabledCallback(disabled);
        }

        const editor = root.querySelector('tf-editor');
        if (editor && typeof (input as any).formDisabledCallback === 'function') {
            (editor as any).formDisabledCallback(disabled);
        }

        const gifButton = root.querySelector('.gif-toggle-button');
        if (gifButton instanceof HTMLButtonElement) {
            gifButton.disabled = disabled;
        }

        const postButton = root.querySelector('.post-button');
        if (postButton instanceof HTMLButtonElement) {
            postButton.disabled = disabled;
        }

        if (this._gifShown) {
            this.toggleGifControls();
        }
    }

    formResetCallback() { this.reset(); }

    formStateRestoreCallback(state: string | File | FormData | null, mode: 'restore' | 'autocomplete') {
        if (typeof state === 'string') {
            this.value = state;
        } else if (state == null) {
            this.clear();
        }
    }

    checkValidity() { return this._internals.checkValidity(); }

    reportValidity() { return this._internals.reportValidity(); }

    reset() {
        this.setValue(this._initialValue);

        const root = this.shadowRoot;
        if (root) {
            const editor = root.querySelector('tf-editor');
            if (editor && typeof (editor as any).reset === 'function') {
                (editor as any).reset();
            }

            if (this._gifShown) {
                this.toggleGifControls();
            }

            const input = root.querySelector('tf-input');
            if (input && typeof (input as any).reset === 'function') {
                (input as any).reset();
            }

            this.setSearchValue();
        }
    }

    protected clear() {
        clearTimeout(this._suggestionTimer);

        this._settingValue = true;

        this._value = '';
        this._internals.setFormValue(null);

        const root = this.shadowRoot;
        if (root) {
            const input = root.querySelector('tf-input');
            if (input) {
                if (input.shadowRoot) {
                    (input as any).value = '';
                } else {
                    input.removeAttribute('value');
                }
            }

            this.setSearchValue();
        }

        this._settingValue = false;
    }

    protected onGifSearchValueChange(event: Event) {
        if (event.target
            && event instanceof CustomEvent
            && event.detail
            && typeof event.detail.value === 'string') {
            this.setSearchValue(event.detail.value as string);
        }
    }

    protected onGifSearchValueInput(event: Event) {
        if (event.target
            && event instanceof CustomEvent
            && event.detail
            && typeof event.detail.value === 'string') {
            const value = event.detail.value as string;

            clearTimeout(this._suggestionTimer);

            if (!value || value.length == null || value.length < 3) {
                this.addSuggestions([]);
                this.setLoading(false);
                return;
            }

            this._searchTerm = value.trim();

            this._suggestionTimer = setTimeout(this.getSuggestions, 300);
        }
    }

    private addSuggestions(suggestions: string[]) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const menu = root.querySelector('menu');
        if (!menu) {
            return;
        }

        suggestions.sort();

        const newChildren: Node[] = [];

        for (let i = 0; i < suggestions.length; i++) {
            const suggestion = document.createElement('li');
            suggestion.dataset.closePicker = '';
            suggestion.dataset.closePickerValue = suggestions[i];
            suggestion.tabIndex = 0;
            suggestion.textContent = suggestions[i];
            newChildren.push(suggestion);
        }

        menu.replaceChildren(...newChildren);
    }

    private appendGifSearch(gifs: { id: string, url: string }[]) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const gifContent = root.querySelector('.gif-content');
        if (!gifContent) {
            return;
        }

        const button = gifContent.querySelector('.more-button');
        if (button) {
            button.remove();
        }

        const list = this.getGifList(gifs);
        gifContent.append(...list);
    }

    private appendValue(value?: string | null) {
        if (value == null
            || !value.length) {
            return;
        }

        this.setValue(this._value + value);
    }

    private getGifList(gifs: { id: string, url: string }[]) {
        const list: Node[] = [];
        if (gifs && gifs.length) {
            for (let i = 0; i < gifs.length; i++) {
                const gif = document.createElement('div');
                gif.classList.add('gif-item');
                gif.addEventListener('click', this.onSelectGifAsync.bind(this, gifs[i].id));
                list.push(gif);

                const img = document.createElement('img');
                img.src = gifs[i].url;
                gif.appendChild(img);
            }

            const button = document.createElement('button');
            button.classList.add('more-button', 'primary');
            button.addEventListener('click', this.searchGifsAsync.bind(this, true));
            list.push(button);
        } else if (this._gifsCategories && this._gifsCategories.length) {
            for (let i = 0; i < this._gifsCategories.length; i++) {
                const gifCategory = document.createElement('div');
                gifCategory.classList.add('gif-item');
                gifCategory.addEventListener('click', this.onSearchCategoryAsync.bind(this, this._gifsCategories[i].searchTerm));

                const img = document.createElement('img');
                img.src = this._gifsCategories[i].image;
                gifCategory.appendChild(img);

                const span = document.createElement('span');
                span.textContent = this._gifsCategories[i].name;
                gifCategory.appendChild(span);

                list.push(gifCategory);
            }
        } else {
            const span = document.createElement('span');
            span.textContent = 'Loading...';
            list.push(span);
        }
        return list;
    }

    private getSuggestions() {
        if (this._searchTerm && this._searchTerm.length) {
            this.getSuggestionsAsync(this._searchTerm).catch(() => { });
        } else {
            this.addSuggestions([]);
            this.setLoading(false);
        }
    }

    private async getSuggestionsAsync(searchTerm: string) {
        const apiKey = this.dataset.apiKey;
        if (!apiKey || !apiKey.length) {
            return;
        }

        this.setLoading(true);

        this.addSuggestions([]);

        const lang = (navigator.languages || ["en"])[0];
        const limit = 5;
        const response = await fetch(`https://tenor.googleapis.com/v2/autocomplete?key=${apiKey}&q=${searchTerm}&locale=${lang}&limit=${limit}`);
        if (response.status == 200) {
            const result = await response.json();
            const results = result["results"];
            if (results && results.length) {
                this.addSuggestions(results);
            }
        }

        this.setLoading(false);
    }

    private async loadAsync() {
        const apiKey = this.dataset.apiKey;
        if (!apiKey || !apiKey.length) {
            return;
        }

        const lang = (navigator.languages || ["en"])[0];

        const categoryResponse = await httpGetAsync(`https://tenor.googleapis.com/v2/categories?key=${apiKey}&locale=${lang}`);
        const categories = categoryResponse["tags"];
        this._gifsCategories = categories.map((v: { [x: string]: string; }) => {
            return {
                image: v["image"],
                name: v["name"],
                searchTerm: v["searchterm"],
            };
        });
        this.populateGifSearch([]);

        const suggestionResponse = await httpGetAsync(`https://tenor.googleapis.com/v2/trending_terms?key=${apiKey}&locale=${lang}&limit=4`);
        if (suggestionResponse) {
            this.populateGifSuggestions(suggestionResponse["results"]);
        } else {
            this.populateGifSuggestions([]);
        }
    }

    private onEditorValueChange(event: Event) {
        if (this._settingValue) {
            return;
        }
        if (event instanceof CustomEvent
            && event.detail
            && event.detail.value) {
            event.stopPropagation();
            this.setValue(event.detail.value);
            this.dispatchEvent(TavenemChatEditorHTMLElement.newValueChangeEvent(this._value));
        } else {
            this.clear();
        }
    }

    private onPost() {
        if (this._internals.form) {
            if (this._internals.form.requestSubmit) {
                this._internals.form.requestSubmit();
            } else {
                this._internals.form.submit();
            }
        }
        this.clear();
        if (this._gifShown) {
            this.toggleGifControls();
        }
    }

    private async onSearchCategoryAsync(searchTerm: string) {
        this._searchTerm = searchTerm;
        await this.searchGifsAsync();
    }

    private async onSearchSuggestionAsync(suggestion: string) {
        this._searchTerm = suggestion.trim();
        await this.searchGifsAsync();
    }

    private async onSelectGifAsync(id: string) {
        const apiKey = this.dataset.apiKey;
        if (!apiKey || !apiKey.length) {
            return;
        }

        const response = await httpGetAsync(`https://tenor.googleapis.com/v2/posts?key=${apiKey}&ids=${id}&media_filter=tinygif,tinywebp_transparent,tinygif_transparent&limit=1`);
        const gifs = response["results"];
        let title: string | undefined;
        let url: string | undefined;
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

        if (!url || !url.length) {
            return;
        }

        this.appendValue(`[![${title}](${url})](${url})`);
        this.dispatchEvent(TavenemChatEditorHTMLElement.newValueChangeEvent(this._value));
        if (this._gifShown) {
            this.toggleGifControls();
        }

        const lang = (navigator.languages || ["en"])[0];
        await httpGetAsync(`https://tenor.googleapis.com/v2/registershare?id=${id}&key=${apiKey}&q=${this._searchTerm}&locale=${lang}`);
    }

    private populateGifSearch(gifs: { id: string, url: string }[]) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const gifContent = root.querySelector('.gif-content');
        if (!gifContent) {
            return;
        }

        const list = this.getGifList(gifs);
        gifContent.replaceChildren(...list);
    }

    private populateGifSuggestions(suggestions: string[]) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const suggestionDiv = root.querySelector('.suggestions');
        if (!suggestionDiv) {
            return;
        }

        const list: Node[] = [];
        if (suggestions) {
            for (let i = 0; i < suggestions.length; i++) {
                const button = document.createElement('button');
                button.classList.add('rounded-pill');
                button.textContent = suggestions[i];
                button.addEventListener('click', this.onSearchSuggestionAsync.bind(this, suggestions[i]));
                list.push(button);
            }
        }

        suggestionDiv.replaceChildren(...list);
    }

    private async searchGifsAsync(more: boolean = false) {
        const apiKey = this.dataset.apiKey;
        if (!apiKey || !apiKey.length) {
            return;
        }

        const lang = (navigator.languages || ["en"])[0];
        let url = `https://tenor.googleapis.com/v2/search?q=${this._searchTerm}&key=${apiKey}&media_filter=nanogif,nanowebp_transparent,nanogif_transparent&locale=${lang}`;
        if (more && this._next && this._next.length) {
            url += `&pos=${this._next}`;
        }
        const response = await httpGetAsync(url);
        if (!response) {
            this.populateGifSearch([]);
            return;
        }

        const gifResults = response["results"];
        this._next = response["next"];
        const gifs: { id: string, url: string }[] = [];
        for (const gif of gifResults) {
            const id = gif["id"];
            let url;
            const mediaFormats = gif["media_formats"];
            for (const format in mediaFormats) {
                url = mediaFormats[format]["url"];
                if (url && url.length) {
                    break;
                }
            }
            gifs.push({ id, url });
        }

        if (this._next && this._next.length) {
            this.appendGifSearch(gifs)
        } else {
            this.populateGifSearch(gifs)
        }

        if (!this._next || !this._next.length) {
            const suggestionResponse = await httpGetAsync(`https://tenor.googleapis.com/v2/search_suggestions?key=${apiKey}&q=${this._searchTerm}&locale=${lang}&limit=4`);
            if (suggestionResponse) {
                this.populateGifSuggestions(suggestionResponse["results"]);
            } else {
                this.populateGifSuggestions([]);
            }
        }
    }

    private setLoading(value: boolean) {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        if (value) {
            const loadingIndicator = document.createElement('tf-progress-circle');
            loadingIndicator.slot = 'popover';
            root.appendChild(loadingIndicator);
        } else {
            const loadingIndicator = root.querySelector('tf-progress-circle');
            if (loadingIndicator) {
                loadingIndicator.remove();
            }
        }
    }

    private setSearchValue(value?: string) {
        clearTimeout(this._suggestionTimer);

        if (!value
            || value.length == null
            || value.length < 3) {
            this.addSuggestions([]);
            this.setLoading(false);
            return;
        }

        this._searchTerm = value.trim();

        this._suggestionTimer = setTimeout(this.getSuggestions, 300);

        this.searchGifsAsync();
    }

    private setValue(value?: string | null) {
        if (value == null) {
            if (this._value == null) {
                return;
            }
        } else if (this._value === value) {
            return;
        }

        if (!value) {
            this.clear();
            return;
        }

        clearTimeout(this._suggestionTimer);

        this._settingValue = true;

        this._value = value;

        if (this._value.length) {
            this._internals.setFormValue(this._value);
        } else {
            this._internals.setFormValue(null);
        }

        const root = this.shadowRoot;
        if (root) {
            const editor = root.querySelector('tf-editor');
            if (editor) {
                if (typeof (editor as any).setValue === 'function') {
                    (editor as any).setValue(this._value);
                } else {
                    editor.setAttribute('value', this._value);
                }
            }
        }

        this._settingValue = false;
    }

    private toggleGifControls() {
        const root = this.shadowRoot;
        if (!root) {
            return;
        }
        const gifPicker = root.querySelector('.gif-picker');
        if (!gifPicker || !(gifPicker instanceof HTMLElement)) {
            return;
        }
        if (this._gifShown) {
            gifPicker.style.display = 'none';
        } else {
            gifPicker.style.display = 'flex';
        }
    }
}

async function httpGetAsync(theUrl: string): Promise<any> {
    const response = await fetch(theUrl);
    if (response.status !== 200) {
        return;
    }
    return await response.json();
}

function randomUUID() {
    if (window.isSecureContext) {
        return crypto.randomUUID();
    }
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
        (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16)
    );
}