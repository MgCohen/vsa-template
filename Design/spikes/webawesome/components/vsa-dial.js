// <vsa-dial>: a ring that shows progress, in a deep carved channel with a raised center.
// API like <wa-progress-ring> and <progress>: value, max, label. variant gives the color, size="small" the small look.
// Behavior of the WAI-ARIA progressbar role: aria-valuenow, aria-valuemin, aria-valuemax and an accessible name.
// Reads only --vsa-dial-* tokens, and the alias tokens for font.
const styles = `
:host {
  --size: 120px; --track-width: 12px; --fill-inset: 3px; --hole-gap: 6px;
  --indicator: var(--vsa-dial-indicator);
  --track-shadow: var(--vsa-dial-track-shadow);
  --hole-shadow: var(--vsa-dial-hole-shadow);
  display: inline-grid; place-items: center; vertical-align: middle; position: relative; flex: none;
  width: var(--size); height: var(--size); border-radius: 50%;
  background: var(--vsa-dial-track); outline: var(--vsa-dial-track-border); outline-offset: -1px;
  font-family: var(--vsa-font-body); color: var(--vsa-dial-text);
}
:host([hidden]) { display: none; }
:host([size="small"]) {
  --size: 36px; --track-width: 5px; --fill-inset: 0px; --hole-gap: 1px;
  --track-shadow: var(--vsa-dial-track-shadow-small);
  --hole-shadow: var(--vsa-dial-hole-shadow-small);
}
:host([variant="success"]) { --indicator: var(--vsa-dial-indicator-success); }
:host([variant="warning"]) { --indicator: var(--vsa-dial-indicator-warning); }
:host([variant="danger"]) { --indicator: var(--vsa-dial-indicator-danger); }
:host::after {
  content: ""; position: absolute; inset: 0; border-radius: 50%; z-index: 1; pointer-events: none;
  box-shadow: var(--track-shadow);
}
.indicator {
  position: absolute; inset: var(--fill-inset); border-radius: 50%;
  background: conic-gradient(var(--indicator) calc(var(--percent, 0) * 1%), transparent 0);
  -webkit-mask: radial-gradient(circle, transparent calc(50% - var(--track-width) - 3px), #000 calc(50% - var(--track-width) - 2.5px));
  mask: radial-gradient(circle, transparent calc(50% - var(--track-width) - 3px), #000 calc(50% - var(--track-width) - 2.5px));
  filter: drop-shadow(0 0 var(--vsa-dial-glow-blur) var(--vsa-dial-glow));
}
.hole {
  position: absolute; inset: calc(var(--track-width) + var(--hole-gap)); border-radius: 50%; z-index: 2;
  background: var(--vsa-dial-hole); box-shadow: var(--hole-shadow);
}
.label {
  position: relative; z-index: 3; display: grid; place-items: center; text-align: center;
  font-size: calc(var(--size) * 0.17); font-weight: var(--vsa-font-weight-strong); line-height: 1.1;
}
:host([size="small"]) .label { font-size: calc(var(--size) * 0.3); }
:host([size="small"]) .value { display: none; }
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
  get size() { return this.getAttribute('size') || 'medium'; }
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
