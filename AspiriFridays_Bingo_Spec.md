
# AspiriFridays Bingo Board Website Specification

## Project Title
**AspiriFridays Bingo Board**

## Overview
A fun, interactive website that displays a bingo board of common or humorous scenarios that tend to happen during an AspiriFridays livestream. Users can watch along and mark squares as they happen. This is meant to be a playful, companion site to the livestreams.

## Core Features

### 1. Bingo Board Display
- A 5x5 grid of bingo squares
- Each square has a short label like "Maddy clicks the wrong button"
- Free space in the center and is just the Aspire logo

### 2. Customizable Bingo Sets
- Configurable via a JSON file (Create fake ones for now - a list of 30 so there is randomization)
- Each square has:
  - `label`: Display text
  - `id`: Internal ID
  - `type`: optional categorization (e.g., bug, dev moment, inside joke)

### 3. User Interaction
- Click to toggle squares as "complete"
- Visual indicator (e.g., checkmark or background change)
- “Bingo!” indicator if a row/col/diagonal is fully checked

### 4. Session State
- Use `localStorage` or `sessionStorage` to persist state between browser refreshes
- Include a reset board button - random shuffle of layout using same square pool

### 5. Responsive Design
- Mobile and desktop friendly
- Font: Outfit for headings, Rubik for body
- Color ideas: Dark purple #2c256b ; Bright purple #9B5DE5 ; accents should be bright and fun and space themed.
- Space themed background

### 6. Social Sharing (Required)
- Button to **download an image** of the board as png for sharing on social media

## Stretch Features (Optional)
- Shareable link or state
- Confetti animation on "Bingo!"
- Easter eggs for specific combinations

## Tech Stack
- **Frontend**: React
- **Dev Stack**: Uses .NET Aspire for local dev
- **Hosting**: Will be deployed with Aspire to Azure Container Apps

## Example Square Pool (JSON)
```json
[
  { "id": "free", "label": "Free Space", "type": "free" },
  { "id": "console-logs", "label": "Maddy clicks Console Logs wrong", "type": "oops" },
  { "id": "david-polyglot", "label": "Fowler says 'polyglot'", "type": "quote" },
  { "id": "docker-compose", "label": "Someone mentions 'compose up'", "type": "infra" },
  { "id": "chat-gpt", "label": "ChatGPT gets name dropped", "type": "meta" }
]
```

