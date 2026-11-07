# Design system: VSA on Web Awesome

**Foundation.** Web Awesome 3.14.0 from the CDN loader, version pinned. On `<html>`: `wa-theme-default wa-palette-default`, and `wa-light` or `wa-dark` from a System / Light / Dark switch. No Font Awesome Pro kit.

**Theme layer.** All `--wa-*` overrides are in `theme/vsa.css`. Add there. Do not set `--wa-*` tokens in another file.

**Project tokens.** `--vsa-*` tokens are only for ideas that Web Awesome has no token for: the raised and carved shadows (`--vsa-shadow-*`), the surface gradients (`--vsa-gradient-*`) and the code colors (`--vsa-color-code-*`). Do not make a `--vsa-*` name for an idea that a `--wa-*` token has.

**Looks.** Each surface is raised or carved. `theme/vsa.css` gives the looks to Web Awesome components with documented parts and states:

| Look | Where |
|---|---|
| Raised: `--vsa-shadow-raise`, `--vsa-gradient-surface` | `wa-button` (not plain or outlined), `wa-card`, `wa-details` |
| Carved: `--vsa-shadow-carve`, `--wa-color-surface-lowered` | `wa-input`, `wa-textarea`, `wa-select`, switch track, checkbox, radio, slider track, progress bar |
| Knob: `--vsa-gradient-knob`, `--vsa-shadow-knob` | Switch thumb, slider thumb |

**Layout.** Use `wa-stack`, `wa-cluster`, `wa-grid` and `wa-gap-*` for layout, and `--wa-space-*` for each gap. Do not write flex or grid by hand. Exception: the docs site shell, `site/index.html`, still has its own layout CSS.

**Vocabulary.** Use these before you make something new.

| Name | Built from | Use for | Defined in |
|---|---|---|---|
| `<vsa-dial>` | Own element: Web Awesome has no dial | One value of a whole, the main number of a card | `components/vsa-dial.js` |
| `pre.vsa-code` | `<pre>` | A code sample, dark in both schemes | `theme/code.css` |

**Conventions.** Main action: `variant="brand"`, one on each screen. Sizes: `s`, `m`, `l`. Status colors: `variant="success"`, `"warning"`, `"danger"`. An own element uses the `vsa-` prefix, reads `--wa-*` tokens, and uses the custom property names of the nearest Web Awesome component.

**Don't.**
- Do not use `size="small"`, `"medium"` or `"large"`. They are deprecated. Use `s`, `m`, `l`.
- Do not style `::part(base)`. It is deprecated. Use the part names on the component page.
- Do not put a hex color or a px size outside `theme/vsa.css` and `theme/code.css`. Use a token.

**Verify.** Open `site/index.html` in light and in dark. No check runs automatically yet.
