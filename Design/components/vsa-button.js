// <vsa-button>: a raised button. API of the HTML <button>: type, disabled, name, value. Event: click.
// variant="primary" uses the accent. size="small" gives the small size. Slot "start" holds an icon. Reads only --vsa-button-* tokens.
const styles = `
:host {
  --height: 38px; --padding: 14px; --gap: 6px; --font-size: var(--vsa-font-size-body);
  display: inline-flex; vertical-align: middle; -webkit-tap-highlight-color: transparent;
}
:host([size="small"]) { --height: 30px; --padding: 10px; --gap: 4px; --font-size: var(--vsa-font-size-label); }
:host([hidden]) { display: none; }
button {
  all: unset; box-sizing: border-box; cursor: pointer;
  display: inline-flex; align-items: center; gap: var(--gap); height: var(--height); padding: 0 var(--padding);
  border-radius: var(--vsa-button-radius);
  background: var(--vsa-button-background); color: var(--vsa-button-text); box-shadow: var(--vsa-button-shadow);
  font-family: var(--vsa-font-body); font-size: var(--font-size); font-weight: var(--vsa-button-font-weight); line-height: 1;
}
:host([variant="primary"]) button {
  background: var(--vsa-button-primary-background); color: var(--vsa-button-primary-text); box-shadow: var(--vsa-button-primary-shadow);
}
:host(:not([disabled])) button:active { box-shadow: var(--vsa-button-pressed-shadow); transform: translateY(1px); }
:host([variant="primary"]:not([disabled])) button:active { box-shadow: var(--vsa-button-primary-pressed-shadow); }
@media (prefers-reduced-motion: no-preference) { button { transition: box-shadow var(--vsa-button-motion), transform var(--vsa-button-motion); } }
button:focus-visible { outline: var(--vsa-focus-width) solid var(--vsa-button-focus-ring); outline-offset: var(--vsa-focus-offset); }
:host([disabled]) button { cursor: default; opacity: var(--vsa-button-disabled-opacity); }
::slotted(svg) { width: var(--vsa-icon-size); height: var(--vsa-icon-size); }
`;

class VsaButton extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ['disabled'];
  #internals = this.attachInternals();
  #button;

  constructor() {
    super();
    const root = this.attachShadow({ mode: 'open', delegatesFocus: true });
    root.innerHTML = `<style>${styles}</style><button part="base" type="button"><slot name="start"></slot><slot></slot></button>`;
    this.#button = root.querySelector('button');
    this.addEventListener('click', event => this.#activate(event), { capture: true });
    this.addEventListener('touchstart', () => {}, { passive: true });
  }

  get disabled() { return this.hasAttribute('disabled'); }
  set disabled(on) { this.toggleAttribute('disabled', Boolean(on)); }
  get type() { const t = this.getAttribute('type'); return t === 'submit' || t === 'reset' ? t : 'button'; }
  set type(value) { this.setAttribute('type', value); }
  get variant() { return this.getAttribute('variant') || 'default'; }
  set variant(value) { this.setAttribute('variant', value); }
  get size() { return this.getAttribute('size') || 'default'; }
  set size(value) { this.setAttribute('size', value); }
  get form() { return this.#internals.form; }

  attributeChangedCallback() {
    this.#button.disabled = this.disabled;
  }

  formDisabledCallback(disabled) {
    this.#button.disabled = disabled || this.disabled;
  }

  #activate(event) {
    if (this.disabled) { event.preventDefault(); event.stopImmediatePropagation(); return; }
    const form = this.#internals.form;
    if (!form) return;
    if (this.type === 'submit') form.requestSubmit();
    if (this.type === 'reset') form.reset();
  }
}

customElements.define('vsa-button', VsaButton);
