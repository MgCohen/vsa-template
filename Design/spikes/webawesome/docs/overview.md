# VSA Design System

The VSA Design System gives VSA apps one look. It uses the Web Awesome 3.14.0 components and the VSA tokens and looks.

## How the parts connect

Each level reads only the level to its left. A change on the left changes everything to its right.

<div class="topo">
  <a class="node lvl-global" href="#/tokens/global"><b>Global tokens</b><span>Raw values</span><code>tokens/global.css</code></a>
  <span class="arrow">→</span>
  <a class="node lvl-alias" href="#/tokens/alias"><b>Alias tokens</b><span>Meaning, light and dark</span><code>tokens/alias.css</code></a>
  <span class="arrow">→</span>
  <div class="node-stack">
    <a class="node lvl-component" href="#/tokens/component"><b>Component tokens</b><span>--wa-* values</span><code>theme/vsa.css</code></a>
    <a class="node lvl-component" href="#/foundations/elevation"><b>Looks</b><span>Raised, carved, knob</span><code>::part()</code></a>
  </div>
  <span class="arrow">→</span>
  <div class="node-stack">
    <a class="node lvl-web" href="#/gallery"><b>Web Awesome components</b><span>73, styled by the theme</span><code>&lt;wa-*&gt;</code></a>
    <a class="node lvl-component" href="#/components/dial"><b>VSA components</b><span>Only where Web Awesome has none</span><code>&lt;vsa-*&gt;</code></a>
  </div>
  <span class="arrow">→</span>
  <div class="node lvl-planned"><b>Molecules, pages</b><span>Planned</span><code>slots</code></div>
</div>

| Link | What it adds |
|---|---|
| Global → Alias | A meaning. `--vsa-orange-370` becomes `--vsa-color-accent` |
| Alias → Component tokens | A place in a component. `--vsa-color-accent` becomes `--wa-color-brand-fill-loud` |
| Alias → Looks | A shape that tokens cannot give, with `::part()`. `--vsa-shadow-carve` goes on the switch track |
| Components → Molecules | Composition. A molecule puts components into the slots of other components |

## Map

| Section | Pages | Use it to |
|---|---|---|
| [Gallery](#/gallery) | One example of each component | See the look of the whole system |
| Foundations | [Color](#/foundations/color), [Typography](#/foundations/typography), [Spacing](#/foundations/spacing), [Radius](#/foundations/radius), [Elevation](#/foundations/elevation), [Motion](#/foundations/motion) | Select a token for a purpose |
| Tokens | [Global](#/tokens/global), [Alias](#/tokens/alias), [Component](#/tokens/component) | See all tokens of one level and the path to each value |
| Components | One page for each component, for example [Switch](#/components/switch) and [Dial](#/components/dial) | Use a component: variants, slots, events, API |

## Important pages

- [Switch](#/components/switch): the sample usage page of a Web Awesome component. Each new usage page uses the same sections.
- [Dial](#/components/dial): the sample VSA component. A VSA component reads its own `--vsa-<name>-*` tokens.
- [Elevation](#/foundations/elevation): the raised and carved looks, and where `::part()` applies them.
- [Component tokens](#/tokens/component): the Web Awesome defaults that the VSA theme does not set yet. Each one is an open decision.
