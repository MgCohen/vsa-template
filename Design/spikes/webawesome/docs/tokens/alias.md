# Alias tokens

An alias token gives a meaning to a global token, for example "accent" or "sunken surface". Light and dark are two values of the same alias token.

| Level | File | Reads | Example |
|---|---|---|---|
| Global | `tokens/global.css` | Nothing: raw values | `--vsa-orange-370: #ff6a1a` |
| **Alias** | `tokens/alias.css` | Global tokens | `--vsa-color-accent: var(--vsa-orange-370)` |
| Component | `theme/vsa.css` | Alias tokens | `--wa-color-brand-fill-loud: var(--vsa-color-accent)` |

To change the look of all components, change an alias token. For example, a new accent color changes each button, switch, slider and badge that uses the brand color.

```tokens @alias.css
```
