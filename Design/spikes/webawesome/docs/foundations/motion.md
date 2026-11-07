# Motion

All components read the Web Awesome transition tokens. The theme layer sets `--wa-transition-fast` to 150 ms, the VSA fast motion. The normal and slow durations keep the Web Awesome values.

```tokens ^--wa-transition-
```

## Rules

- Each component stops its transitions when the user asks for reduced motion.
- Show the pressed state at once. A long transition on press makes a tap on a phone feel slow.

## Open question

The pressed state on a phone shows late. Find if `--wa-transition-fast: 0ms` for the press fixes it.
