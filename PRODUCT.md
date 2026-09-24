# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Live-stream viewers use the bingo board to play along with AspiriFridays broadcasts. A small, trusted host team uses the admin portal to prepare boards, run live sessions, approve player activity, and monitor connected players.

## Product Purpose

AspiriFridays Bingo turns recurring moments from an Aspire live stream into a shared, real-time game. Success means viewers can understand and play immediately while hosts can run the experience confidently without distracting from the stream.

## Positioning

The product connects each viewer's randomized board to the live broadcast through real-time host controls and optional approval, making the stream itself the game clock and source of truth.

## Operating Context

Players typically use the frontend while watching the AspiriFridays stream, often on a second screen or mobile device. Hosts use the admin portal during preparation and live production, where current state, pending actions, and reliable controls must be quickly scannable.

## Capabilities and Constraints

- The player experience provides a randomized 5x5 board, keyboard navigation, free play, live mode, catch-up synchronization, downloadable board images, connection status, and bingo celebrations.
- Live mode sends square changes to the host team for approval; free play applies them immediately.
- SignalR synchronizes board and session state in real time.
- The admin portal manages bingo sets and squares, controls live sessions, handles approvals, and monitors connected clients.
- Existing product terminology and behavior must remain intact during the visual refresh.

## Brand Commitments

- Keep the name **AspiriFridays Bingo**.
- Loosely align both surfaces with the official .NET Aspire brand guidance at https://microsoft.github.io/aspire-brand/.
- Preserve the player frontend's whimsical, delightful personality rather than turning it into a formal corporate interface.
- Give the admin portal a calmer, operational expression of the same shared visual system.

## Evidence on Hand

- Product overview and workflow: `README.md`
- Existing Vue player implementation: `src/bingo-board`
- Existing Blazor admin implementation: `src/BingoBoard.Admin`
- Official Aspire brand colors and assets: https://microsoft.github.io/aspire-brand/
- No testimonials, usage metrics, or commercial claims are available and none should be fabricated.

## Product Principles

- Make joining the game feel immediate and joyful.
- Keep the board legible and tappable while attention is split with a live stream.
- Make live state and pending actions unmistakable.
- Let the player surface celebrate; let the admin surface reassure.
- Use Aspire brand cues as connective tissue, not as a constraint on personality.

## Accessibility & Inclusion

Preserve keyboard navigation, visible focus, semantic status communication, responsive layouts, and sufficient color contrast across both surfaces.
