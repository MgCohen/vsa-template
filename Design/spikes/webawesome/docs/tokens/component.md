# Component tokens

The components read the Web Awesome tokens (`--wa-*`). These are our component tokens. `theme/vsa.css` gives each one an alias value.

| Level | File | Reads | Example |
|---|---|---|---|
| Global | `tokens/global.css` | Nothing: raw values | `--vsa-orange-370: #ff6a1a` |
| Alias | `tokens/alias.css` | Global tokens | `--vsa-color-accent: var(--vsa-orange-370)` |
| **Component** | `theme/vsa.css` | Alias tokens | `--wa-color-brand-fill-loud: var(--vsa-color-accent)` |

Two more places hold component values:

- **Size of one component:** a custom property on the element, for example `--width` on `wa-switch`. Each component page lists them under "CSS custom properties".
- **Looks:** the raised and carved shadows go on `::part()`. See [Elevation](#/foundations/elevation).

## Set by the VSA theme

```tokens ^--wa- @vsa.css
```

## Tokens of the VSA components

A VSA component, for example [Dial](#/components/dial), reads its own tokens, `--vsa-<component>-*`. `theme/vsa.css` sets them, also with alias values.

```tokens ^--vsa-dial- @vsa.css
```

## Web Awesome defaults that the VSA theme does not set yet

These tokens keep their Web Awesome value. Each one is a decision: keep it, or give it a VSA alias value.

```tokens ^--wa-(space-(3xs|2xs|xs|s|m|l|xl|2xl|3xl)|border-width-[sml]|border-style|font-size-(3xs|4xl)|line-height-expanded)$ @default.css
```
