# Code

A code block shows code as an IDE does: a dark surface and colors for each kind of code part. The block is dark in the light theme and in the dark theme.

```html
<!-- A switch with a hint -->
<wa-switch checked hint="Runs on each push">Sync</wa-switch>
```

## Fit to the width

The text of a code block scales down until the longest line fits the width. The smallest size is `--vsa-font-size-code-min`. If a line is longer than that, the block scrolls horizontally.

Keep sample lines short, about 60 characters, so that the scaled text stays easy to read.

## Colors

The colors come from highlight.js. The file `theme/code.css` sets a project token for each kind of code part.

| Code part | Token | Example |
|---|---|---|
| Tag name, type, property | `--vsa-color-code-tag` | `wa-switch`, `display` |
| Attribute, selector | `--vsa-color-code-attribute` | `checked`, `.card` |
| String | `--vsa-color-code-string` | `"Runs on each push"` |
| Keyword | `--vsa-color-code-keyword` | `const`, `public` |
| Number | `--vsa-color-code-number` | `16`, `0.5` |
| Comment, tag brackets | `--vsa-color-code-muted` | `<!-- -->`, `< >` |

```tokens ^--vsa-(color-code|font-size-code)
```

## Languages

Write the language after the three backticks, for example `css`, `html`, `csharp`, `js` or `json`. A block without a language has no colors.

```css
wa-switch::part(thumb) {
  background: var(--vsa-gradient-knob);
}
```

```csharp
public sealed class SyncSettings
{
    public bool RunOnPush { get; init; } = true;
}
```
