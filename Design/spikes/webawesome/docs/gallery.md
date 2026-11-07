# Gallery

One example of each component with the VSA look, grouped by category. To see all variants of a component, open its page.

## Actions

```html preview
<wa-button>Default</wa-button>
<wa-button variant="brand">Primary</wa-button>
<wa-button appearance="outlined">Outlined</wa-button>
<wa-button appearance="plain">Plain</wa-button>
<wa-button disabled>Disabled</wa-button>
<wa-button variant="brand" loading>Loading</wa-button>
<wa-button-group label="Range">
  <wa-button>Day</wa-button>
  <wa-button>Week</wa-button>
  <wa-button>Month</wa-button>
</wa-button-group>
<wa-copy-button value="dotnet test"
  aria-label="Copy the command"></wa-copy-button>
```

## Forms

```html preview
<wa-input label="Name" placeholder="Runner name"
  style="width: 220px"></wa-input>
<wa-select label="Source" value="git"
  style="width: 180px">
  <wa-option value="git">Git</wa-option>
  <wa-option value="nuget">NuGet</wa-option>
</wa-select>
<wa-textarea label="Notes" rows="2"
  placeholder="Write a note"
  style="width: 220px"></wa-textarea>
<wa-switch checked>Sync</wa-switch>
<wa-checkbox checked>Run tests</wa-checkbox>
<wa-radio-group label="Channel" value="stable"
  orientation="horizontal">
  <wa-radio value="stable">Stable</wa-radio>
  <wa-radio value="preview">Preview</wa-radio>
</wa-radio-group>
<wa-slider label="Parallel jobs" min="1" max="16"
  value="6" style="width: 200px"></wa-slider>
<wa-rating label="Rating" value="3"></wa-rating>
```

## Feedback

```html preview
<wa-callout variant="success" style="width: 260px">
  All 412 tests pass.
</wa-callout>
<wa-callout variant="danger" style="width: 260px">
  The build failed.
</wa-callout>
<wa-badge variant="success">Done</wa-badge>
<wa-badge variant="warning">Pending</wa-badge>
<wa-badge variant="danger">Failed</wa-badge>
<wa-tag>net10.0</wa-tag>
<wa-progress-bar value="62" label="Build"
  style="width: 200px"></wa-progress-bar>
<wa-progress-ring value="62" label="Build">
</wa-progress-ring>
<wa-spinner></wa-spinner>
<vsa-dial value="72" label="Tests that pass"></vsa-dial>
<vsa-dial size="s" value="60" label="Sync"></vsa-dial>
<wa-tooltip for="tip-target">Runs on each push</wa-tooltip>
<wa-button id="tip-target" size="s">
  Hover for a tooltip
</wa-button>
```

## Navigation

```html preview
<wa-breadcrumb>
  <wa-breadcrumb-item href="#/gallery">
    Modules
  </wa-breadcrumb-item>
  <wa-breadcrumb-item href="#/gallery">
    Orders
  </wa-breadcrumb-item>
  <wa-breadcrumb-item>Slices</wa-breadcrumb-item>
</wa-breadcrumb>
<wa-tab-group style="width: 100%">
  <wa-tab panel="a">Overview</wa-tab>
  <wa-tab panel="b">Modules</wa-tab>
  <wa-tab-panel name="a">
    Three modules, one host.
  </wa-tab-panel>
  <wa-tab-panel name="b">
    Orders, Billing, Shipping.
  </wa-tab-panel>
</wa-tab-group>
```

## Layout

```html preview
<wa-card style="width: 260px">
  <div slot="header">Runner Home</div>
  A card is a raised panel.
  <div slot="footer">
    <wa-button size="s" variant="brand">Run</wa-button>
  </div>
</wa-card>
<wa-details summary="Build settings"
  style="width: 260px">
  Release, net10.0, no restore.
</wa-details>
<wa-divider style="width: 100%"></wa-divider>
```

## Media

```html preview
<wa-avatar initials="MC" label="User"></wa-avatar>
<wa-qr-code value="https://example.com" size="72"
  label="QR code"></wa-qr-code>
```
