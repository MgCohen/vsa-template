## When to use it

- Use a dial to show one value of a whole, for example the part of the tests that pass.
- Use a dial as the main number of a card or a page.
- If the value changes from one step to the next and has an end, use a progress bar.
- If the work has no known end, use a spinner.

## Look

The ring is a deep carved channel. The indicator fills the channel with the accent color and a glow. The center is a raised hole.

```html preview
<vsa-dial value="72" label="Tests that pass"></vsa-dial>
```

## Variants

### Center content

Put a number, a word or an icon in the default slot. If the slot is empty, the medium dial shows the percent, and the small dial shows nothing.

```html preview
<vsa-dial value="131" max="133" label="Tests that pass">
  <div>131<br><small>of 133</small></div>
</vsa-dial>
<vsa-dial value="40" label="Disk use"></vsa-dial>
```

### Colors

Set `variant` for a state color. The default is the accent color.

```html preview
<vsa-dial value="90" variant="success"
  label="Done"></vsa-dial>
<vsa-dial value="45" variant="warning"
  label="Pending"></vsa-dial>
<vsa-dial value="20" variant="danger"
  label="Failed"></vsa-dial>
```

### Sizes

The small dial, `size="s"`, is 36 px, with a shallow carve. Use it in lists and table rows. For another size, set `--size` and `--track-width`, as on `wa-progress-ring`.

```html preview
<vsa-dial size="s" value="60" label="Sync">
  <wa-icon name="rotate" label="Sync"></wa-icon>
</vsa-dial>
<vsa-dial size="s" value="100" variant="success"
  label="Build"></vsa-dial>
<vsa-dial value="72" label="Tests"
  style="--size: 180px; --track-width: 16px"></vsa-dial>
```

## Accessibility

The dial has the role `progressbar`, with its value, its minimum and its maximum. Always set `label`: screen readers read it with the value.

## Change the value

Set `value` from code. The dial shows the new value at once.

```html
<vsa-dial id="tests" label="Tests that pass"></vsa-dial>
<script>
  const dial = document.getElementById('tests');
  dial.value = 72;
</script>
```

## Tokens and looks

The dial is a VSA component: `components/vsa-dial.js`. Web Awesome has no dial, so the project owns it. It follows the Web Awesome rules for an own component: its own prefix, the `--wa-*` tokens, and the custom property names of `wa-progress-ring`.

| Item | Value |
|---|---|
| Channel | `--track-color` (default `--wa-color-surface-lowered`), and the deep carve `--vsa-shadow-dial`. The small dial uses `--vsa-shadow-carve` |
| Indicator | `--indicator-color` (default `--wa-color-brand-fill-loud`). `variant` sets it to `--wa-color-success-fill-loud`, `--wa-color-warning-fill-loud` or `--wa-color-danger-fill-loud`. The glow is the same color |
| Center | `--vsa-gradient-hole` and `--vsa-shadow-hole`. The small dial uses `--vsa-shadow-raise-sm` |
| Text | `--wa-font-family-body`, `--wa-color-text-normal` |

## Open question

The Runner reference also shows a ring with more than one color, for example done, pending and failed. Add it as a `segments` API, or as a separate component?
