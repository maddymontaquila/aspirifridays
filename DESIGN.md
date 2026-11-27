# Design System

## Direction

AspiriFridays Bingo is an **Aspire stage**: dark-first, confident, and playful through motion and color moments rather than decoration. It follows the official Aspire brand (https://aka.ms/aspire/brand): core purple leads, gradients are a controlled highlight, layouts stay calm, and copy is clear and direct. The player experience is fun and fresh; the admin portal is the production-side expression of the same brand, calmer and denser for trustworthy live operation.

One visual language per surface. No ticket notches, stickers, tilted tiles, category dots, or mixed metaphors on the player board.

## Brand Foundation

Use the official Aspire palette as the source of truth:

- Primary purple: `#7455dd`
- Deep purple: `#512bd4`
- Secondary lavender: `#b9aaee`
- Muted lavender: `#dcd5f6`
- Ink: `#1f1e33`
- Paper: `#ffffff`
- Magenta: `#b30f87`
- Flamingo: `#f65163`
- Blue: `#0078d7`
- Sky: `#1f8fff`
- Cyan: `#0b7e84`
- Mint: `#1ea98c`
- Yellow: `#9b8308`

Player dark theme (default): page `#0f0d1d`, surface `#1a1830`, tile `#221f3a`, muted text `#a8a2c6`, hairlines `rgba(185, 170, 238, .14–.18)`. The light theme mirrors it on `#f6f4fb` with white tiles. Tokens live in `src/bingo-board/styles/variables.css`.

Gradients are reserved for winning: `--gradient-win` (purple to magenta) fills squares in a completed line and the "You have bingo!" banner. Never use gradient text.

## Typography

- Player: **Poppins** (self-hosted via `@fontsource/poppins`, weights 400–700) for everything. Headings 700 with slight negative tracking; square labels 500 and never below 12px on phones.
- Admin: Segoe UI variable family (`"Segoe UI Variable Display"` / `"Segoe UI Variable Text"`, `"Segoe UI"` fallback).
- Long square labels get soft hyphens (`utils/softHyphenate.js`); the board enables them only on tiles where a whole word would overflow, so desktop never hyphenates needlessly.

## Shape and Depth

- Player board: one rounded surface panel holding solid-accent B I N G O letters and a 5x5 grid of rounded tiles with hairline borders.
- Free space: muted lavender tile with the Aspire logo and a small "FREE" label.
- Admin panels: flat white surfaces, 1px hairline borders, 6-8px radius, no shadows. Buttons never lift; badges are small tinted rectangles.
- Pills are reserved for status and compact metadata.
- No decorative glass; blur is only used on the celebration scrim.

## Interaction and State

- Purple is the primary action color.
- Marked squares become solid `#7455dd` with white text and a check badge; they pop in with an ease-out keyframe (no bounce easing).
- Pending squares (live mode, awaiting host approval) show a dashed lavender border, a slow breathe, and an hourglass badge.
- Completed lines animate in sequence with the win gradient; the celebration overlay shows a large "Bingo!" with brand-colored confetti.
- Live mode uses flamingo (magenta in light theme); free play uses mint (cyan in light theme). Always paired with a text label.
- Hover lifts tiles slightly; active scales down. Keyboard focus rings appear only when the grid has visible focus.
- Respect `prefers-reduced-motion` (confetti hidden, animations removed).

## Surface Expression

### Player frontend

The board dominates the first viewport and is sized to fit the viewport height on laptops. The header holds only the logo, the title, a "Watch the stream" link and the theme toggle. A single status card states the mode and marked count with one sentence of help text. Keep copy minimal: no taglines, eyebrow labels, or sentence-fragment subtitles.

Theme: dark by default, with a light/dark toggle persisted in `localStorage['aspirifridays-theme']` and applied before paint in `index.html`. The "Save image" export (`utils/imageGenerator.js`) renders the dark theme.

### Admin portal

Crisp and minimal. A slim 216px ink rail holds the brand, navigation and footer (aspire.dev, sign out, version); there is no top toolbar. The work area is a light canvas with flat hairline cards, one `.page-header` (title plus actions) per page, and fluid `auto-fit` grids instead of fixed Bootstrap columns so layouts hold in narrow previews. Only primary, success and danger buttons are filled; everything else is a neutral bordered or ghost button. State is shown with a dot plus a label (`.status--live`, `--ok`, `--warn`, `--idle`). Copy stays terse: titles and one sentence at most.

## Responsive Behavior

- The player layout moves from board-plus-sidebar to a single column below 960px.
- The board is a container (`container-type: inline-size`); tile text and gaps scale with `cqi`. Below a 34rem container, rows grow to fit labels instead of staying square.
- The board remains a true 5x5 grid with no horizontal scrolling down to 320px.
- The admin navigation collapses to a menu at mobile width.
- Dense tables remain horizontally scrollable inside their content region rather than widening the page.

## Accessibility

Maintain semantic controls, keyboard board navigation, visible focus, reduced-motion behavior, pinch zoom, and WCAG AA text contrast in both themes. Do not use color alone to communicate live, pending, checked, error, or disabled states.
