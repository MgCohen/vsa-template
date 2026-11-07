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
- Add each new atom to the Atoms section of the gallery, in all its states.
- If no standard covers a topic, ask the user. Record the answer as a decision in `docs/decisions/`.

## Check

1. Serve `Design/` with a static web server.
2. Open `/gallery/index.html`.
3. Make sure that the check shows each mode as "62 of 62 pass". If the system theme is dark, the "dark (system)" mode must also pass.

If you add an alias or component token that has a value in `reference/default.css` or in the Runner Studio CSS, add it to `MAP` in `gallery/index.html`. The second value of an entry is a reference variable, or a literal CSS value from `reference/runner-studio.html`.
