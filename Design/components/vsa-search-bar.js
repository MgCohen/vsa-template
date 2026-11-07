// <vsa-search-bar>: a molecule. One <vsa-field> and one <vsa-button> that do one job: search.
// The page must load vsa-field.js and vsa-button.js.
// Attributes: placeholder, value, label (the accessible name), button (the button text), disabled.
// Events: input when the text changes, search when the user presses Enter or the button. Reads only --vsa-search-bar-* tokens.
const styles = `
:host { display: flex; min-width: 0; }
:host([hidden]) { display: none; }
.bar { display: flex; flex: 1; min-width: 0; gap: var(--vsa-search-bar-gap); }
vsa-field { flex: 1; }
`;

const ICON = '<svg slot="start" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>';

class VsaSearchBar extends HTMLElement {
  static observedAttributes = ['placeholder', 'value', 'label', 'button', 'disabled'];
  #field;
  #button;

  constructor() {
    super();
    const root = this.attachShadow({ mode: 'open', delegatesFocus: true });
    root.innerHTML = `<style>${styles}</style><div class="bar" role="search" part="base"><vsa-field part="field" type="search" aria-label="Search">${ICON}</vsa-field><vsa-button part="button">Search</vsa-button></div>`;
    this.#field = root.querySelector('vsa-field');
    this.#button = root.querySelector('vsa-button');
    this.#field.addEventListener('keydown', event => { if (event.key === 'Enter') this.#search(); });
    this.#button.addEventListener('click', () => this.#search());
  }

  get value() { return this.#field.value; }
  set value(text) { this.#field.value = text; }
  get disabled() { return this.hasAttribute('disabled'); }
  set disabled(on) { this.toggleAttribute('disabled', Boolean(on)); }

  attributeChangedCallback(name, old, now) {
    if (name === 'placeholder') this.#copy(this.#field, 'placeholder', now);
    if (name === 'label') this.#field.setAttribute('aria-label', now || 'Search');
    if (name === 'value') this.#field.value = now || '';
    if (name === 'button') this.#button.textContent = now || 'Search';
    if (name === 'disabled') { this.#copy(this.#field, 'disabled', now); this.#copy(this.#button, 'disabled', now); }
  }

  #copy(target, name, value) {
    if (value === null) target.removeAttribute(name);
    else target.setAttribute(name, value);
  }

  #search() {
    if (this.disabled) return;
    this.dispatchEvent(new Event('search', { bubbles: true }));
  }
}

customElements.define('vsa-search-bar', VsaSearchBar);
