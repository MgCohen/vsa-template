# 0003: Component tokens

**Decision:** All component tokens are in one file, `tokens/component.css`, on `:root, [data-theme]`. A component reads its component tokens and no other tokens.

```css
/* tokens/component.css */
:root, [data-theme] {
  --vsa-toggle-track-on: var(--vsa-color-accent);
  --vsa-toggle-knob: var(--vsa-gradient-knob);
}
```

## Rules

- Name a component token `--vsa-<component>-<part>-<state>`, for example `--vsa-toggle-track-on`.
- The value of a component token refers to alias tokens only.
- The selector includes `[data-theme]`. The browser calculates a `var()` value on the element that declares it. Without `[data-theme]`, a dark area inside a light page gets the light values.
- A size that only the component uses, for example the knob size, is on `:host` in the component file. It does not change with the theme.
- To change all toggles, change one line in `tokens/component.css`.

## Files and levels

| Level | File |
|---|---|
| Global | `tokens/global.css` |
| Alias | `tokens/alias.css` |
| Component | `tokens/component.css` |

## Reason

The references for the token levels keep component tokens in central files:

| Reference | Component tokens | Where |
|---|---|---|
| [Carbon](https://carbondesignsystem.com/elements/themes/overview/) | `$button-primary` | Central theme files, in `@carbon/themes` |
| [Primer](https://github.com/primer/primitives/tree/main/src/tokens/component) | `button.primary.bgColor.rest` | Central token files, in `src/tokens/component/` |
| [Web Awesome](https://webawesome.com/docs/components/switch) | Only sizes, for example `--thumb-size` | On `:host` in the component |

One file gives all values of all components in one place, and a theme changes them with one `:root` rule.
