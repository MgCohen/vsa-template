# Theme layer

`theme/vsa.css` is the only place that sets `--wa-*` tokens. It has three blocks, with the selectors that Web Awesome recommends:

| Block | Selector | Content |
|---|---|---|
| Both schemes | `:where(:root), .wa-light, .wa-dark, .wa-invert` | Fonts, sizes, radius, focus, motion, the brand color |
| Light | `:where(:root), .wa-light, .wa-dark .wa-invert` | Surfaces, text and status colors for light |
| Dark | `.wa-dark, .wa-invert` | The same tokens for dark |

To change the look of all components, change a value here. For example, a new `--wa-color-brand-fill-loud` changes each button, switch, slider and badge that uses the brand color.

```tokens ^--wa- @vsa.css
```

## Web Awesome defaults that the theme does not set

These tokens keep their Web Awesome value.

```tokens ^--wa-(space-(3xs|2xs|xs|s|m|l|xl|2xl|3xl)|border-width-[sml]|border-style|font-size-(3xs|4xl)|line-height-expanded|transition-(normal|slow))$ @default.css
```
