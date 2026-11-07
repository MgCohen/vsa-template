## When to use it

- Use a switch for one setting that applies at once, for example "Sync".
- If the change applies only when the user saves a form, use a checkbox.
- If the user selects one value from a list, use a radio group or a select.

## Look

The track is carved. The knob is raised. When the switch is on, the track fills with the accent color and a glow.

```html preview
<wa-switch aria-label="Pin"></wa-switch>
<wa-switch checked aria-label="Sync"></wa-switch>
```

## Variants

### With text

Put the text in the default slot. A click on the text also changes the switch.

```html preview
<wa-switch>Pin to home</wa-switch>
<wa-switch checked>Sync</wa-switch>
```

### Without text

A switch with no text must have an `aria-label`.

```html preview
<wa-switch aria-label="Sync"></wa-switch>
```

### With a hint

```html preview
<wa-switch hint="Runs the tests after each build">
  Auto test
</wa-switch>
```

### Sizes

```html preview
<wa-switch size="s">Small</wa-switch>
<wa-switch size="m">Medium</wa-switch>
<wa-switch size="l">Large</wa-switch>
```

### Disabled

```html preview
<wa-switch disabled>Off and disabled</wa-switch>
<wa-switch checked disabled>On and disabled</wa-switch>
```

## Events

The switch sends `input` and then `change` when the user changes it. Read the state from `checked`.

```html
<wa-switch id="sync">Sync</wa-switch>
<script>
  const sync = document.getElementById('sync');
  sync.addEventListener('change', e => {
    console.log(e.target.checked);
  });
</script>
```

## VSA tokens and looks

| Item | Value |
|---|---|
| Size | `--width`, `--height`, `--thumb-size` on `wa-switch`: 40, 24 and 18 px at size `m`, in `em` |
| Track off | `--wa-color-surface-lowered`, `--vsa-shadow-carve` on `::part(control)` |
| Track on | `--wa-color-brand-fill-loud`, `--vsa-shadow-press`, `--vsa-shadow-glow` on `:state(checked)::part(control)` |
| Knob | `--vsa-gradient-knob`, `--vsa-shadow-knob` on `::part(thumb)` |
