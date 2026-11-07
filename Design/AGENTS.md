# AGENTS.md: VSA Design System

Instructions for agents that work in `Design/`. The root `AGENTS.md` also applies.

## Scope

- Keep all design system files in `Design/`. Do not add files, references or build steps outside this folder.
- The repository is the source of truth. A gallery or an artifact is a view of these files.
- Do not change `reference/default.css` or `themes/default.json`. They record the default theme as the user chose it.

## Tokens

- CSS is the only source of the tokens. Do not add a build step or a token JSON file.
- Use three levels: global (`tokens/global.css`), alias (`tokens/alias.css`), component (`tokens/component.css`). See `docs/decisions/0003-component-tokens.md`.
- A global token has a raw value and no meaning. Name a global color `--vsa-<family>-<n>`, n = 1000 − 10 × L*, rounded to the nearest 5. The family is `warm` (red > blue), `cool` (blue > red), `gray` (red = blue), or the hue name for strong colors.
- An alias token gives meaning. Its value refers to global tokens only. Make a transparent color with `color-mix(in srgb, var(--vsa-<global>) <n>%, transparent)`.
- Components and screens read only alias tokens and component tokens. Do not write a color or a shadow directly in a component. A size that only the component uses is the one exception, on `:host`.
- `tokens/alias.css` has two dark blocks: one for `prefers-color-scheme: dark` and one for `[data-theme="dark"]`. If you change one, change the other.

## Components

- Follow the standards in `docs/decisions/0002-component-standards.md`: the HTML spec and Open UI for the API, the WAI-ARIA APG for behavior, the web component best practices.
- Put each atom in one file, `components/vsa-<name>.js`: a custom element with shadow DOM, plain JavaScript, no build.
- An atom reads only its own component tokens, and the alias tokens for font, focus and icon size. A size that only the atom uses goes on `:host` as a custom property.
- Each atom has one style: the style of the default theme.
- Add each new atom to the Overview page of the gallery, in all its states.
- Add each new component to `COMPONENTS` in the gallery and to its menu. The gallery makes the component page from that entry.
- Follow `docs/decisions/0004-variants-and-levels.md` for variants and levels. An atom never holds another `<vsa-*>` part. A molecule or an organism does not import its parts.
- If no standard covers a topic, ask the user. Record the answer as a decision in `docs/decisions/`.

## Look at a change

1. Serve `Design/` with a static web server.
2. Open `/gallery/index.html`.
3. Make sure that the changed atoms and tokens show correctly in light and in dark. Use the theme switch.

The Tokens pages read the token files, so a new token shows there automatically. A Foundations page lists its tokens by name: if you add an alias token, add it to the correct Foundations page. Show each token with one of two gallery parts. If the look of the token is the information (color, gradient, shadow, radius, font family), use the `spec()` card: a large preview, then the name, the path and the value on one line. If the number is the information (spacing, size, weight, line height, motion, effects), use `specRow()` in a `specRows()` table: Preview, Token, Path, Value. Do not make a different layout for one page. Show each token name as a pill with the color of its level (`tokLink` or `tokName`). Make each section with `card()`, so that it can fold. Make the header of a page with `pageHead()`: it does not fold.
