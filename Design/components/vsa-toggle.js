// <vsa-toggle>: an on/off switch, carved channel style.
// API of <input type="checkbox">: checked, disabled, name, value, required. Events: input, then change.
// Behavior of the WAI-ARIA APG switch pattern: role "switch", aria-checked, Space changes the state.
// size="small" gives the small size. Reads only --vsa-toggle-* tokens.
const styles = `
:host {
  --width: 40px; --height: 24px; --knob-size: 18px; --gap: 3px;
  display: inline-block; vertical-align: middle; position: relative; flex: none; -webkit-tap-highlight-color: transparent;
  width: var(--width); height: var(--height); border-radius: calc(var(--height) / 2); cursor: pointer;
  background: var(--vsa-toggle-track); box-shadow: var(--vsa-toggle-track-shadow);
  outline: var(--vsa-toggle-track-border); outline-offset: -1px;
}
:host([size="small"]) { --width: 32px; --height: 19px; --knob-size: 13px; }
:host([hidden]) { display: none; }
:host([checked]) { background: var(--vsa-toggle-track-on); box-shadow: var(--vsa-toggle-track-on-shadow); }
:host(:focus-visible) { outline: var(--vsa-focus-width) solid var(--vsa-toggle-focus-ring); outline-offset: var(--vsa-focus-offset); }
:host([disabled]) { cursor: default; opacity: var(--vsa-toggle-disabled-opacity); }
.knob {
  position: absolute; top: var(--gap); left: var(--gap); width: var(--knob-size); height: var(--knob-size); border-radius: 50%;
  background: var(--vsa-toggle-knob); box-shadow: var(--vsa-toggle-knob-shadow);
}
:host([checked]) .knob { left: calc(var(--width) - var(--knob-size) - var(--gap)); }
@media (prefers-reduced-motion: no-preference) { .knob { transition: left var(--vsa-toggle-motion); } }
`;

class VsaToggle extends HTMLElement {
  static formAssociated = true;
  static observedAttributes = ['checked', 'disabled', 'value', 'required'];
  #internals = this.attachInternals();
  #defaultChecked = false;

  constructor() {
    super();
    this.attachShadow({ mode: 'open' }).innerHTML = `<style>${styles}</style><span class="knob" part="knob"></span>`;
    this.#internals.role = 'switch';
    this.addEventListener('click', () => this.#flip());
    this.addEventListener('keydown', event => {
      if (event.key !== ' ') return;
      event.preventDefault();
      this.#flip();
    });
  }

  connectedCallback() {
    this.#defaultChecked = this.checked;
    this.#sync();
  }

  get checked() { return this.hasAttribute('checked'); }
  set checked(on) { this.toggleAttribute('checked', Boolean(on)); }
  get disabled() { return this.hasAttribute('disabled'); }
  set disabled(on) { this.toggleAttribute('disabled', Boolean(on)); }
  get required() { return this.hasAttribute('required'); }
  set required(on) { this.toggleAttribute('required', Boolean(on)); }
  get name() { return this.getAttribute('name') || ''; }
  set name(value) { this.setAttribute('name', value); }
  get value() { return this.getAttribute('value') ?? 'on'; }
  set value(value) { this.setAttribute('value', value); }
  get form() { return this.#internals.form; }
  get validity() { return this.#internals.validity; }
  checkValidity() { return this.#internals.checkValidity(); }
  reportValidity() { return this.#internals.reportValidity(); }

  attributeChangedCallback() { this.#sync(); }
  formResetCallback() { this.checked = this.#defaultChecked; }
  formDisabledCallback(disabled) { this.#internals.ariaDisabled = String(disabled || this.disabled); }

  #flip() {
    if (this.disabled) return;
    this.checked = !this.checked;
    this.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
    this.dispatchEvent(new Event('change', { bubbles: true }));
  }

  #sync() {
    this.#internals.ariaChecked = String(this.checked);
    this.#internals.ariaDisabled = String(this.disabled);
    this.#internals.ariaRequired = String(this.required);
    this.#internals.setFormValue(this.checked ? this.value : null);
    if (this.required && !this.checked) this.#internals.setValidity({ valueMissing: true }, 'Switch this on to continue.');
    else this.#internals.setValidity({});
    if (this.disabled) this.removeAttribute('tabindex');
    else if (!this.hasAttribute('tabindex')) this.setAttribute('tabindex', '0');
  }
}

customElements.define('vsa-toggle', VsaToggle);
