// <vsa-dial>: a ring that shows progress, in a deep carved channel with a raised center.
// API like <wa-progress-ring> and <progress>: value, max, label. variant gives the color, size="s" the small look.
// Custom properties with the names of <wa-progress-ring>: --size, --track-width, --track-color, --indicator-color.
// Behavior of the WAI-ARIA progressbar role: aria-valuenow, aria-valuemin, aria-valuemax and an accessible name.
// Reads the Web Awesome tokens, and the project tokens for the carved and raised shadows.
const styles = `
:host {
  --size: 120px;
  --track-width: 12px;
  --track-color: var(--wa-color-surface-lowered);
  --indicator-color: var(--wa-color-brand-fill-loud);
  --fill-inset: 3px; --hole-gap: 6px;
  --track-shadow: var(--vsa-shadow-dial);
  --hole-shadow: var(--vsa-shadow-hole);
  display: inline-grid; place-items: center; vertical-align: middle; position: relative; flex: none;
  width: var(--size); height: var(--size); border-radius: 50%;
  background: var(--track-color); outline: var(--vsa-border-sunken); outline-offset: -1px;
  font-family: var(--wa-font-family-body); color: var(--wa-color-text-normal);
}
:host([hidden]) { display: none; }
:host([size="s"]) {
  --size: 36px; --track-width: 5px; --fill-inset: 0px; --hole-gap: 1px;
  --track-shadow: var(--vsa-shadow-carve);
  --hole-shadow: var(--vsa-shadow-raise-sm);
}
:host([variant="success"]) { --indicator-color: var(--wa-color-success-fill-loud); }
:host([variant="warning"]) { --indicator-color: var(--wa-color-warning-fill-loud); }
:host([variant="danger"]) { --indicator-color: var(--wa-color-danger-fill-loud); }
:host::after {
  content: ""; position: absolute; inset: 0; border-radius: 50%; z-index: 1; pointer-events: none;
  box-shadow: var(--track-shadow);
}
.indicator {
  position: absolute; inset: var(--fill-inset); border-radius: 50%;
  background: conic-gradient(var(--indicator-color) calc(var(--percent, 0) * 1%), transparent 0);
  -webkit-mask: radial-gradient(circle, transparent calc(50% - var(--track-width) - 3px), #000 calc(50% - var(--track-width) - 2.5px));
  mask: radial-gradient(circle, transparent calc(50% - var(--track-width) - 3px), #000 calc(50% - var(--track-width) - 2.5px));
  filter: drop-shadow(0 0 8px color-mix(in srgb, var(--indicator-color) 45%, transparent));
}
.hole {
  position: absolute; inset: calc(var(--track-width) + var(--hole-gap)); border-radius: 50%; z-index: 2;
  background: var(--vsa-gradient-hole); box-shadow: var(--hole-shadow);
}
.label {
  position: relative; z-index: 3; display: grid; place-items: center; text-align: center;
  font-size: calc(var(--size) * 0.17); font-weight: var(--wa-font-weight-semibold); line-height: 1.1;
}
:host([size="s"]) .label { font-size: calc(var(--size) * 0.3); }
:host([size="s"]) .value { display: none; }
`;

class VsaDial extends HTMLElement {
  static observedAttributes = ['value', 'max', 'label'];
  #internals = this.attachInternals();
  #indicator;
  #fallback;

  constructor() {
    super();
    this.attachShadow({ mode: 'open' }).innerHTML = `<style>${styles}</style>`
      + '<span class="indicator" part="indicator"></span><span class="hole" part="hole"></span>'
      + '<span class="label" part="label"><slot><span class="value"></span></slot></span>';
    this.#indicator = this.shadowRoot.querySelector('.indicator');
    this.#fallback = this.shadowRoot.querySelector('.value');
    this.#internals.role = 'progressbar';
    this.#internals.ariaValueMin = '0';
  }

  connectedCallback() { this.#sync(); }
  attributeChangedCallback() { this.#sync(); }

  get value() { return Number(this.getAttribute('value')) || 0; }
  set value(number) { this.setAttribute('value', String(number)); }
  get max() { return Number(this.getAttribute('max')) || 100; }
  set max(number) { this.setAttribute('max', String(number)); }
  get label() { return this.getAttribute('label') || ''; }
  set label(text) { this.setAttribute('label', text); }
  get variant() { return this.getAttribute('variant') || 'brand'; }
  set variant(name) { this.setAttribute('variant', name); }
  get size() { return this.getAttribute('size') || 'm'; }
  set size(name) { this.setAttribute('size', name); }

  #sync() {
    const percent = Math.min(100, Math.max(0, this.value / this.max * 100));
    this.#indicator.style.setProperty('--percent', String(percent));
    this.#fallback.textContent = `${Math.round(percent)}%`;
    this.#internals.ariaValueMax = String(this.max);
    this.#internals.ariaValueNow = String(Math.min(this.max, Math.max(0, this.value)));
    this.#internals.ariaLabel = this.label || null;
  }
}

customElements.define('vsa-dial', VsaDial);
