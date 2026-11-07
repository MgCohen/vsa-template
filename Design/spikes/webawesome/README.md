# Spike: the VSA Design System on Web Awesome

This spike builds the VSA Design System on the Web Awesome 3.14.0 components. One theme file gives the components the VSA tokens and looks, with no change to the components. A docs site shows the foundations and one page for each component.

The spike does not change our own atoms in `components/`.

## Files

| Path | Content |
|---|---|
| `theme/vsa.css` | Part 1: each `--wa-*` token gets a VSA alias value. Part 2: the raised and carved looks, with `::part()` |
| `site/index.html` | The docs site: side menu, foundations, one page for each component, light and dark |
| `docs/index.json` | The menu: the foundations pages and the components that have a usage page |
| `docs/overview.md` | The first page |
| `docs/foundations/*.md` | Color, typography, spacing, radius, elevation, motion |
| `docs/components/<name>.md` | The usage page of one component: when to use it, variants as live examples, VSA tokens |

## How the site works

- The site has no build. It reads the Markdown files in the browser.
- A code block with the language `html preview` shows the live HTML and its code.
- A code block with the language `tokens <regex>` shows a table of the matching tokens. Each row shows the path to the source value, and the light and dark values.
- The API tables (attributes, slots, events, parts, CSS properties, states) come from the Web Awesome `custom-elements.json`. Each of the 73 components gets an API page, also with no usage page.

## How the site loads

1. `tokens/fonts.css`, `tokens/global.css`, `tokens/alias.css`: our values, unchanged.
2. Web Awesome `styles/themes/default.css` from jsDelivr: their default tokens, in cascade layers.
3. `theme/vsa.css`: not in a layer, so it wins over all Web Awesome layers.
4. Web Awesome `webawesome.loader.js` from jsDelivr: it loads each component when the page uses it.
5. `marked` 11.2.0 from jsDelivr: it changes Markdown to HTML.

The spike takes only the prebuilt Web Awesome files. It does not use the Web Awesome source, build, docs or tests.

## Add a usage page

1. Write `docs/components/<name>.md`. Use `<name>` without `wa-`, for example `switch`.
2. Start with "When to use it". Then write "Look", "Variants" and "VSA tokens and looks".
3. Add the component to `components` in `docs/index.json`.

## Open questions

- Keep the CDN, or keep a copy of the prebuilt files in `Design/vendor/`. A Blazor app with no internet access needs a copy.
- Two-way binding in Blazor needs a thin Razor wrapper for each form control. The spike does not test Blazor.
- The VSA theme does not set the Web Awesome space scale. Decide if it uses the VSA spacing.
- The badge default color is the brand color. Decide if the VSA default is neutral.
- The dial and the dots have no Web Awesome component. Write them as VSA components with the same rules.

## Run it

Serve the `Design/` folder with any static web server. Then open `/spikes/webawesome/site/index.html`.
