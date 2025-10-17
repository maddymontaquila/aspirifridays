# Approve All Button - UI Mockup

## Visual Representation

### Pending Approvals Section - WITH Pending Requests

```
┌────────────────────────────────────────────────────────────────────────┐
│ ⚠️ Pending Approval Requests                                    [5]    │
│                           [✓✓ Approve All (5)]  [↻ Refresh]            │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────────────────────────────────┐  ┌──────────────┐   │
│  │ [PENDING]              15:23:45              │  │ [PENDING]    │   │
│  │                                              │  │  15:24:12    │   │
│  │ Bug found in guest's app                     │  │              │   │
│  │                                              │  │ Screen share │   │
│  │ 3 clients want to CHECK this square         │  │ fail         │   │
│  │                                              │  │              │   │
│  │ ℹ️ Approving/denying will affect all 3       │  │ Client wants │   │
│  │    requests                                  │  │ to CHECK     │   │
│  │                                              │  │ this square  │   │
│  │ Multiple clients: abc123..., def456..., ... │  │              │   │
│  │                                              │  │ Client ID:   │   │
│  │ [✓ Approve All (3)]  [✗ Deny All (3)]      │  │ xyz789...    │   │
│  └──────────────────────────────────────────────┘  │              │   │
│                                                     │ [✓ Approve]  │   │
│                                                     │ [✗ Deny]     │   │
│                                                     └──────────────┘   │
└────────────────────────────────────────────────────────────────────────┘
```

### Button States

#### Normal State (Has Pending Approvals)
```
┌───────────────────────────┐
│ ✓✓ Approve All (5)        │  ← Green button, enabled
└───────────────────────────┘
```

#### Processing State
```
┌───────────────────────────┐
│ ✓✓ Approve All (5)        │  ← Green button, disabled, slightly grayed
└───────────────────────────┘
```

#### No Pending State
```
┌───────────────────────────┐
│ ✓✓ Approve All (0)        │  ← Green button, disabled
└───────────────────────────┘
```

### Section When No Pending Approvals

When in Live Mode with NO pending approvals, the entire section is hidden:

```
┌────────────────────────────────────────────────────────────────┐
│ 🟢 Stream Mode: LIVE                              [Toggle ON]  │
├────────────────────────────────────────────────────────────────┤
│ 🎥 Live Stream Mode Active:                                     │
│ Clients must request approval to mark squares. Use the         │
│ approval workflow below to manage requests.                    │
└────────────────────────────────────────────────────────────────┘

(No pending approvals section shown - it only appears when there are pending requests)

┌────────────────────────────────────────────────────────────────┐
│ 📊 All Available Squares                            [100]      │
│                        [↻ Refresh] [✓ Show Checked (45)]      │
│                                    [👁 Show All]               │
├────────────────────────────────────────────────────────────────┤
│ (Grid of all squares...)                                       │
└────────────────────────────────────────────────────────────────┘
```

## User Flow

### 1. Initial State
- Admin opens Board Management page
- Sees pending approval requests section with multiple requests
- "Approve All" button shows total count of pending requests

### 2. Click "Approve All"
- Button becomes disabled immediately
- Local state updates (squares visually change to checked/unchecked)
- Pending approvals section clears immediately for instant feedback
- Loading state indicated by disabled button

### 3. Server Processing
- All pending approvals are processed on server
- Related approvals (same square, same state) are grouped and processed together
- Global square states are updated
- All affected clients are notified

### 4. Completion
- Admin receives `AllApprovalsProcessed` event
- Activity log shows: "Bulk approval completed: Approved X requests across Y squares"
- Button re-enables (or stays disabled if no more pending approvals)
- UI refreshes with latest state from server
- Any new pending requests that came in during processing are shown

## Color Scheme

### Button Colors
- **Approve All**: Green (`btn-success`) - Positive action
- **Refresh**: Dark outline (`btn-outline-dark`) - Neutral action
- **Approve (Individual)**: Green (`btn-success btn-sm`) - Positive action
- **Deny (Individual)**: Red (`btn-danger btn-sm`) - Negative action

### Section Colors
- **Pending Approvals Header**: Warning yellow (`bg-warning text-dark`)
- **Approval Cards**: Light yellow (`bg-warning` variant with subtle border)
- **Badge**: Dark background (`badge bg-dark`) for contrast

## Responsive Behavior

### Desktop View
```
┌──────────────────────────────────────────────────────────────┐
│ [Approval Group 1]        [Approval Group 2]                 │
│ (50% width)               (50% width)                        │
└──────────────────────────────────────────────────────────────┘
```

### Mobile/Tablet View
```
┌──────────────────────────┐
│ [Approval Group 1]       │
│ (100% width)             │
├──────────────────────────┤
│ [Approval Group 2]       │
│ (100% width)             │
└──────────────────────────┘
```

### Button Group Responsive
On mobile, the "Approve All" and "Refresh" buttons stack or wrap as needed to maintain readability.

## Accessibility

### Button Attributes
- **aria-label**: "Approve all 5 pending requests"
- **disabled**: When processing or no pending requests
- **title**: "Approve all pending square marking requests at once"

### Visual Indicators
- ✓✓ Double check icon clearly indicates bulk action
- Count badge shows number of items being processed
- Disabled state provides clear visual feedback
- Color contrast meets WCAG AA standards

## Example Scenarios

### Scenario 1: Heavy Activity
During a busy live stream, 15 clients request to mark various squares:
- Admin sees "Approve All (15)" button
- Clicks once
- All 15 requests approved instantly
- Individual clients see their squares update
- Admin board reflects all changes

### Scenario 2: Mixed Requests
Clients submit both check and uncheck requests:
- 3 requests to CHECK "Bug found"
- 2 requests to UNCHECK "Free Space"
- Admin clicks "Approve All (5)"
- System processes both types correctly
- "Bug found" gets checked globally
- "Free Space" gets unchecked globally

### Scenario 3: No Pending Requests
When no requests are pending:
- Section is hidden entirely (clean UI)
- Or button shows "Approve All (0)" and is disabled
- Admin focuses on other management tasks

## Integration with Existing Features

### Works alongside individual approval buttons
- Admins can still approve/deny individual requests
- "Approve All" is just a convenience feature
- Both approaches use the same backend logic

### Activity Log Integration
- Bulk approvals create single log entry
- Shows count of processed requests
- Shows count of affected squares
- Timestamp for audit trail

### Live Mode Toggle
- Button only appears in Live Mode
- Hidden in Free Play mode (no approvals needed)
- Respects current mode setting
