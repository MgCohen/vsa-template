# VSA Design System

The VSA Design System gives VSA apps one look. It uses the Web Awesome 3.14.0 components and the VSA tokens and looks.

## Levels

| Level | Example | Where |
|---|---|---|
| Global token | `--vsa-orange-370: #ff6a1a` | `tokens/global.css` |
| Alias token | `--vsa-color-accent: var(--vsa-orange-370)` | `tokens/alias.css` |
| Component token | `--wa-color-brand-fill-loud: var(--vsa-color-accent)` | `theme/vsa.css`, part 1 |
| Look | The raised and carved shadows, with `::part()` | `theme/vsa.css`, part 2 |
| Component | `<wa-switch>`, `<wa-input>` | Web Awesome, prebuilt files |

A change to a global or alias token changes each component that uses it.

## Pages

- **Foundations:** the tokens, with their light and dark values and the path to the source value.
- **Components:** the components that have a VSA usage page, with examples of each variant.
- **All components:** each Web Awesome component. The API tables come from the Web Awesome `custom-elements.json`.

## All looks on one page

```html preview
<wa-button>Default</wa-button>
<wa-button variant="brand">Primary</wa-button>
<wa-button disabled>Disabled</wa-button>
<wa-switch checked>Sync</wa-switch>
<wa-checkbox checked>Run tests</wa-checkbox>
<wa-input placeholder="Runner name" style="width: 220px"></wa-input>
<wa-slider min="1" max="16" value="6" style="width: 200px" aria-label="Parallel jobs"></wa-slider>
<wa-progress-bar value="62" style="width: 200px" aria-label="Build"></wa-progress-bar>
<wa-badge variant="success">Done</wa-badge>
<wa-badge variant="danger">Failed</wa-badge>
```
