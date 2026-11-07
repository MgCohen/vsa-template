# Spike: the VSA style on Web Awesome

This spike applies the VSA tokens and looks to the Web Awesome components. It answers one question: can one theme file give the Web Awesome components the VSA style, with no change to the components?

Result: yes, for the 15 components on the page. The spike does not change our own atoms in `components/`.

## Files

| File | Content |
|---|---|
| `vsa-theme.css` | Part 1: each `--wa-*` token gets a VSA alias value. Part 2: the raised and carved looks, with `::part()` |
| `index.html` | The spike page: buttons, switches, fields, choices, feedback, a card and tabs, in light and dark |

## How it loads

1. `tokens/fonts.css`, `tokens/global.css`, `tokens/alias.css`: our values, unchanged.
2. Web Awesome 3.14.0 `styles/themes/default.css` from jsDelivr: their default tokens, in cascade layers.
3. `vsa-theme.css`: not in a layer, so it wins over all Web Awesome layers.
4. Web Awesome `webawesome.loader.js` from jsDelivr: it loads each component when the page uses it.

The spike takes only the prebuilt files. It does not use the Web Awesome source, build, docs or tests.

## Token levels

| Level | Source |
|---|---|
| Global | `tokens/global.css` |
| Alias | `tokens/alias.css` |
| Component | Web Awesome tokens (`--wa-color-brand-fill-loud`, `--wa-form-control-*`) with values from `vsa-theme.css` |

## Theme

The page sets `data-theme` for our tokens and the class `wa-dark` or `wa-light` for Web Awesome. Each token that `vsa-theme.css` sets follows `data-theme`.

## Open questions

- Keep the CDN, or keep a copy of the prebuilt files in `Design/vendor/`. A Blazor app with no internet access needs a copy.
- Two-way binding in Blazor needs a thin Razor wrapper for each form control. The spike does not test Blazor.
- The badge default color is the brand color. Decide if the VSA default is neutral.

## Run it

Serve the `Design/` folder with any static web server. Then open `/spikes/webawesome/index.html`.
