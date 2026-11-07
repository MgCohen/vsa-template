# 0004: Variants and component levels

**Decision:** A variant is an attribute that changes only the look. An atom never holds another `<vsa-*>` part. A molecule and an organism are custom elements that put atoms in their shadow DOM.

```html
<vsa-button variant="primary" size="small">Save</vsa-button>
<vsa-search-bar placeholder="Search sources"></vsa-search-bar>
<vsa-settings-panel heading="Listener">
  <vsa-toggle name="log" aria-label="Log everything"></vsa-toggle>
</vsa-settings-panel>
```

## Three kinds of attribute

| Kind | Source | Example |
|---|---|---|
| Variant | The author of the screen | `variant="primary"`, `size="small"` |
| State | The user or the data | `checked`, `disabled`, `value` |
| Content | The author of the screen | The default slot, `heading`, `placeholder` |

## Rules

- Use `variant` for the look and `size` for the size. Each has the value `default` when the attribute is missing.
- The values of `size` are `default` and `small`. The button and the toggle use the same values.
- A variant changes only the look. If it changes the behavior or the structure, make a different component.
- A size that only the component uses goes on `:host`, as a custom property. A variant that changes a color reads component tokens.

## Levels

| Level | Holds | Example |
|---|---|---|
| Atom | Plain content only, for example text or an icon in a slot | `<vsa-button>` |
| Molecule | Atoms that do one job | `<vsa-search-bar>`: a field and a button |
| Organism | Molecules and atoms that make one area of a screen | `<vsa-settings-panel>` |

- A molecule or an organism does not import its parts. The page loads each file that it uses.
- A molecule or an organism reads its own component tokens for its layout. Its parts read their tokens.
- An organism keeps form controls in the light DOM, so a form can read them.

## Reason

Web Awesome and Shoelace use `variant` and `size` attributes. Atomic Design defines the three levels by what each level holds.

## Open questions

- A toggle with a visible text: the default slot of the toggle, or a molecule with a label.
- The other variant names, for example `ghost` or `danger`.
