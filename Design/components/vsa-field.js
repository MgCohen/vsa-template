// <vsa-field>: a one-line text input, carved style.
// API of <input>: type, value, placeholder, name, disabled, readonly, required. Events: input, change.
// The aria-label of the element goes to the inner input. Slot "start" holds an icon. Reads only --vsa-field-* tokens.
const styles = `
:host { display: flex; min-width: 0; -webkit-tap-highlight-color: transparent; }
:host([hidden]) { display: none; }
label {
  box-sizing: border-box; flex: 1; min-width: 0; display: flex; align-items: center; gap: 8px; height: 38px; padding: 0 12px; cursor: text;
  border-radius: var(--vsa-field-radius);
  background: var(--vsa-field-background); box-shadow: var(--vsa-field-shadow);
  outline: var(--vsa-field-border); outline-offset: -1px;
  color: var(--vsa-field-placeholder);
}
label:focus-within { outline: var(--vsa-focus-width) solid var(--vsa-field-focus-ring); outline-offset: var(--vsa-focus-offset); }
:host([disabled]) label { cursor: default; opacity: var(--vsa-field-disabled-opacity); }
input {
  all: unset; flex: 1; min-width: 0;
  font-family: var(--vsa-font-body); font-size: var(--vsa-font-size-body); color: var(--vsa-field-text);
}
input::placeholder { color: var(--vsa-field-placeholder); opacity: 1; }
::slotted(svg) { width: var(--vsa-icon-size); height: var(--vsa-icon-size); flex: none; }
`;

const FORWARDED = ['type', 'placeholder', 'disabled', 'readonly', 'required', 'aria-label', 'autocomplete', 'minlength', 'maxlength', 'pattern'];

class VsaField extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = [...FORWARDED, 'value'];
  #internals = this.attachInternals();
  #input;

  constructor() {
    super();
    const root = this.attachShadow({ mode: 'open', delegatesFocus: true });
    root.innerHTML = `<style>${styles}</style><label part="base"><slot name="start"></slot><input part="input"></label>`;
    this.#input = root.querySelector('input');
    this.#input.addEventListener('input', () => this.#sync());
    this.#input.addEventListener('change', () => this.dispatchEvent(new Event('change', { bubbles: true })));
  }

  connectedCallback() { this.#sync(); }

  get value() { return this.#input.value; }
  set value(text) { this.#input.value = text; this.#sync(); }
  get defaultValue() { return this.getAttribute('value') || ''; }
  get name() { return this.getAttribute('name') || ''; }
  set name(value) { this.setAttribute('name', value); }
  get disabled() { return this.hasAttribute('disabled'); }
  set disabled(on) { this.toggleAttribute('disabled', Boolean(on)); }
  get form() { return this.#internals.form; }
  get validity() { return this.#internals.validity; }
  checkValidity() { return this.#internals.checkValidity(); }
  reportValidity() { return this.#internals.reportValidity(); }

  attributeChangedCallback(name, old, now) {
    if (name === 'value') { this.#input.value = now || ''; }
    else if (now === null) { this.#input.removeAttribute(name); }
    else { this.#input.setAttribute(name, now); }
    this.#sync();
  }

  formResetCallback() { this.#input.value = this.defaultValue; this.#sync(); }
  formDisabledCallback(disabled) { this.#input.disabled = disabled || this.disabled; }

  #sync() {
    this.#internals.setFormValue(this.#input.value);
    this.#internals.setValidity(this.#input.validity, this.#input.validationMessage, this.#input);
  }
}

customElements.define('vsa-field', VsaField);
