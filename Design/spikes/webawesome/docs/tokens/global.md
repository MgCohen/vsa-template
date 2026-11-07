# Global tokens

A global token has a raw value and no meaning, for example a color or a size. Only alias tokens read global tokens.

| Level | File | Reads | Example |
|---|---|---|---|
| **Global** | `tokens/global.css` | Nothing: raw values | `--vsa-orange-370: #ff6a1a` |
| Alias | `tokens/alias.css` | Global tokens | `--vsa-color-accent: var(--vsa-orange-370)` |
| Component | `theme/vsa.css` | Alias tokens | `--wa-color-brand-fill-loud: var(--vsa-color-accent)` |

The name of a global color is `--vsa-<family>-<n>`, with n = 1000 − 10 × L\*. A higher number is a darker color.

```tokens @global.css
```
