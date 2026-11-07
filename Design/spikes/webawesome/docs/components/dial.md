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

The small dial is 36 px, with a shallow carve. Use it in lists and table rows. For another size, set `--size` and `--track-width`.

```html preview
<vsa-dial size="small" value="60" label="Sync">
  <wa-icon name="rotate" label="Sync"></wa-icon>
</vsa-dial>
<vsa-dial size="small" value="100" variant="success"
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

## VSA tokens and looks

The dial is a VSA component: `components/vsa-dial.js`. It reads only its own tokens, which `theme/vsa.css` sets. See [Component tokens](#/tokens/component).

| Item | Value |
|---|---|
| Channel | `--vsa-dial-track`, `--vsa-dial-track-shadow` (deep carve), `--vsa-dial-track-shadow-small` |
| Indicator | `--vsa-dial-indicator`, the three state colors, `--vsa-dial-glow`, `--vsa-dial-glow-blur` |
| Center | `--vsa-dial-hole`, `--vsa-dial-hole-shadow`, `--vsa-dial-hole-shadow-small` |
| Text | `--vsa-dial-text`, `--vsa-font-body` |

## Open question

The Runner reference also shows a ring with more than one color, for example done, pending and failed. Add it as a `segments` API, or as a separate component?
