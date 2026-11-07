# Color

Components read the Web Awesome color tokens. The theme layer, `theme/vsa.css`, sets each value for light and for dark.

## Surfaces and text

```tokens ^--wa-color-(surface|text|focus)
```

## Brand

The brand color is the VSA accent. Use it for one main action on each screen.

```tokens ^--wa-color-brand-
```

## Neutral

```tokens ^--wa-color-neutral-(fill|border|on)
```

## Status

| Variant | Use |
|---|---|
| Success | A finished task, a passed check |
| Warning | A task that waits, a value that is out of date |
| Danger | A failed task, an action that deletes data |

```tokens ^--wa-color-(success|warning|danger)-(fill|on)-loud
```

## Project colors

Web Awesome has no token for the page canvas, the accent glow and the surface gradients. These are project tokens: new ideas, not new names for Web Awesome tokens.

```tokens ^--vsa-(color|gradient)-
```
