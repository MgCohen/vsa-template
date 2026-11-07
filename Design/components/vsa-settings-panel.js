// <vsa-settings-panel>: an organism. A card with a heading, a Reset button, a <vsa-search-bar> and one row for each <vsa-toggle> child.
// The page must load vsa-search-bar.js and the atoms that it uses.
// Attributes: heading. The aria-label of each toggle is the text of its row, and data-description is the second line.
// The search bar filters the rows. Reset sets each toggle back to its first state and sends change from that toggle.
// Reads only --vsa-settings-panel-* tokens.
const styles = `
:host { --padding: 18px; display: block; min-width: 0; }
:host([hidden]) { display: none; }
section {
  display: grid; gap: var(--vsa-settings-panel-gap); padding: var(--padding);
  background: var(--vsa-settings-panel-background); color: var(--vsa-settings-panel-text);
  box-shadow: var(--vsa-settings-panel-shadow); border-radius: var(--vsa-settings-panel-radius);
  font-family: var(--vsa-font-body);
}
header { display: flex; align-items: center; justify-content: space-between; gap: var(--vsa-settings-panel-gap); }
h2 { margin: 0; font-size: var(--vsa-font-size-title); font-weight: var(--vsa-font-weight-strong); }
.rows { display: grid; }
.row { display: flex; align-items: center; justify-content: space-between; gap: var(--vsa-settings-panel-gap); padding: 10px 0; border-top: 1px solid var(--vsa-settings-panel-divider); cursor: pointer; }
.row:first-child { border-top: 0; }
.row[hidden], .empty[hidden] { display: none; }
.label { font-size: var(--vsa-font-size-body); font-weight: var(--vsa-font-weight-medium); }
.description { display: block; font-size: var(--vsa-font-size-label); color: var(--vsa-settings-panel-muted); }
.empty { margin: 0; font-size: var(--vsa-font-size-label); color: var(--vsa-settings-panel-muted); }
`;

class VsaSettingsPanel extends HTMLElement {
  static observedAttributes = ['heading'];
  #root;
  #rows;
  #search;
  #empty;
  #firstState = new WeakMap();

  constructor() {
    super();
    this.#root = this.attachShadow({ mode: 'open', slotAssignment: 'manual' });
    this.#root.innerHTML = `<style>${styles}</style><section part="base"><header><h2 part="heading"></h2><vsa-button size="small" part="reset">Reset</vsa-button></header><vsa-search-bar part="search" placeholder="Filter settings" label="Filter settings" button="Find"></vsa-search-bar><div class="rows" part="rows"></div><p class="empty" hidden>No setting matches the filter.</p></section>`;
    this.#rows = this.#root.querySelector('.rows');
    this.#search = this.#root.querySelector('vsa-search-bar');
    this.#empty = this.#root.querySelector('.empty');
    this.#root.querySelector('vsa-button').addEventListener('click', () => this.reset());
    this.#search.addEventListener('input', () => this.#filter());
    this.#search.addEventListener('search', () => this.#filter());
    new MutationObserver(() => this.#render()).observe(this, { childList: true });
  }

  connectedCallback() { this.#render(); }

  attributeChangedCallback() {
    this.#root.querySelector('h2').textContent = this.getAttribute('heading') || '';
  }

  get toggles() { return [...this.children].filter(child => child.localName === 'vsa-toggle'); }

  reset() {
    for (const toggle of this.toggles) {
      const first = this.#firstState.get(toggle);
      if (toggle.checked === first) continue;
      toggle.checked = first;
      toggle.dispatchEvent(new Event('change', { bubbles: true }));
    }
  }

  #render() {
    this.#rows.replaceChildren(...this.toggles.map(toggle => this.#row(toggle)));
    this.#filter();
  }

  #row(toggle) {
    if (!this.#firstState.has(toggle)) this.#firstState.set(toggle, toggle.checked);
    const row = document.createElement('div');
    row.className = 'row';
    row.part = 'row';
    const text = document.createElement('div');
    const label = document.createElement('span');
    label.className = 'label';
    label.textContent = toggle.getAttribute('aria-label') || '';
    text.append(label);
    const description = toggle.dataset.description;
    if (description) {
      const line = document.createElement('span');
      line.className = 'description';
      line.textContent = description;
      text.append(line);
    }
    const slot = document.createElement('slot');
    row.append(text, slot);
    slot.assign(toggle);
    row.addEventListener('click', event => { if (event.target === row || text.contains(event.target)) toggle.click(); });
    return row;
  }

  #filter() {
    const query = (this.#search.value || '').trim().toLowerCase();
    let shown = 0;
    for (const row of this.#rows.children) {
      const match = row.textContent.toLowerCase().includes(query);
      row.hidden = !match;
      if (match) shown++;
    }
    this.#empty.hidden = shown > 0;
  }
}

customElements.define('vsa-settings-panel', VsaSettingsPanel);
