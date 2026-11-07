# 0002: Component standards

**Decision:** Each component of the VSA Design System follows four public standards. The design system does not make its own rule for a topic that a standard covers.

## The standards

| Topic | Standard | Rule for a VSA component |
|---|---|---|
| Names, attributes, properties, events | [HTML spec](https://html.spec.whatwg.org/), [Open UI](https://open-ui.org/) | Use the API of the native element with the same role. Use the Open UI names for parts and states |
| Behavior, keyboard, accessibility | [WAI-ARIA Authoring Practices (APG)](https://www.w3.org/WAI/ARIA/apg/patterns/) | Use the role, the states, the keys and the focus of the APG pattern |
| Web component rules | [Custom Element Best Practices](https://web.dev/articles/custom-elements-best-practices), [Gold Standard checklist](https://github.com/webcomponents/gold-standard/wiki) | Attributes and properties stay equal. Form controls work in a `<form>` |
| Tokens | [Design Tokens Community Group (DTCG)](https://www.designtokens.org/) | Three levels: global, alias, component. See `0003-component-tokens.md` |

## Example: the toggle

| Topic | Value | From |
|---|---|---|
| Attributes | `checked`, `disabled`, `name`, `value`, `required` | HTML `<input type="checkbox">` |
| Events | `input`, then `change` | HTML `<input type="checkbox">` |
| Role and state | `role="switch"`, `aria-checked` | APG switch pattern |
| Keys | Space changes the state | APG switch pattern |
| Form | The value goes in the form data when `checked` is set | Gold Standard checklist |

## Event names

- An event that a native element also sends has the native name: `click`, `input`, `change`, `focus`, `blur`.
- An event with no native name has the prefix `vsa-`, for example `vsa-select`.

## Open questions

A topic that no standard covers is a question for the user. Record each answer as a decision in this folder.
