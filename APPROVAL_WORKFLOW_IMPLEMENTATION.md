# Bingo Board Approval Workflow Implementation

## Overview

The bingo board application has been updated with an **inverted approval workflow**. Previously, only admin users could mark squares globally. Now, any connected client can request to mark a square, which sends a notification to admin users for approval.

## How It Works

### For Regular Users (Bingo Players):
1. **Request Square Marking**: When a user clicks a square, instead of marking it immediately, it sends an approval request to the admin
2. **Pending State**: The square enters a "pending approval" state
3. **Notification**: User receives confirmation that their request was submitted
4. **Response**: User gets notified when admin approves or denies the request
5. **Auto-Update**: If approved, the square is automatically marked on all boards

### For Admin Users:
1. **Real-time Notifications**: Receive instant notifications when users request square markings
2. **Approval Dashboard**: View all pending approval requests
3. **Quick Actions**: Approve or deny requests with optional reasons
4. **Global Update**: Approved squares are automatically marked on all connected boards

## New Backend Components

### Models
- **`PendingApproval`**: Tracks approval requests with status, timestamps, and metadata
- **`ApprovalStatus`**: Enum for Pending, Approved, Denied, Expired

### Services
- **Enhanced `BingoService`**: 
  - `RequestSquareApprovalAsync()`: Create new approval requests
  - `GetPendingApprovalsAsync()`: Retrieve pending requests
  - `ApproveSquareRequestAsync()`: Process approvals
  - `DenySquareRequestAsync()`: Process denials
  - `CleanupExpiredApprovalsAsync()`: Remove old requests

- **`ApprovalCleanupService`**: Background service that runs every 30 minutes to clean up expired requests

### SignalR Hub Updates
- **`RequestSquareApproval`**: Client method to request approval
- **`ApproveSquareRequest`**: Admin method to approve requests
- **`DenySquareRequest`**: Admin method to deny requests
- **`GetPendingApprovals`**: Admin method to retrieve pending requests

## New Frontend Integration

### Updated SignalR Service
```javascript
// For regular users - request approval instead of direct update
await signalRService.requestSquareApproval(squareId, true)

// For admins - approve requests
await signalRService.approveSquareRequest(approvalId)

// For admins - deny requests
await signalRService.denySquareRequest(approvalId, "Reason for denial")

// For admins - get pending approvals
await signalRService.getPendingApprovals()
```

### New Event Listeners
- **`approvalRequestSubmitted`**: Fired when user's request is submitted
- **`approvalRequestApproved`**: Fired when admin approves user's request
- **`approvalRequestDenied`**: Fired when admin denies user's request
- **`newApprovalRequest`**: Fired for admins when new request comes in
- **`approvalRequestProcessed`**: Fired for admins when request is processed
- **`pendingApprovalsList`**: Fired when admin requests pending approvals

## Frontend Changes Needed

### 1. Update Square Click Handler
Replace the direct square update logic:
```javascript
// OLD: Direct update
await signalRService.updateSquare(squareId, !isChecked)

// NEW: Request approval
await signalRService.requestSquareApproval(squareId, !isChecked)
```

### 2. Add Approval Status UI
- Show "pending approval" status on squares
- Display approval/denial notifications
- Add loading states during approval process

### 3. Admin Dashboard Features
- List of pending approval requests
- Approve/Deny buttons for each request
- Real-time notifications for new requests
- Optional reason field for denials

### 4. Event Handling
```javascript
// Listen for approval responses
signalRService.addEventListener('approvalRequestSubmitted', (response) => {
  // Show "request submitted" message
  showNotification(`Request submitted for "${response.SquareId}"`)
})

signalRService.addEventListener('approvalRequestApproved', (response) => {
  // Show success message and update square
  showNotification(`Request approved: ${response.Message}`)
})

signalRService.addEventListener('approvalRequestDenied', (response) => {
  // Show denial message
  showNotification(`Request denied: ${response.Message}`, 'error')
})

// For admin interface
signalRService.addEventListener('newApprovalRequest', (request) => {
  // Add to pending approvals list
  addToPendingApprovals(request)
})
```

## Data Storage

### Redis Cache Structure
- **`pending_approval_{approvalId}`**: Individual approval requests
- **`pending_approvals_list`**: List of all approval IDs
- **`global_square_{squareId}`**: Global square states (existing)
- **`bingo_set_{clientId}`**: Client bingo sets (existing)

### Expiration Policy
- Approval requests expire after 2 hours
- Background service cleans up expired requests every 30 minutes
- Processed requests are kept for 24 hours for tracking

## Benefits

1. **Democratic Marking**: All users can contribute to marking squares
2. **Quality Control**: Admin oversight prevents incorrect markings
3. **Real-time Feedback**: Users know immediately when requests are processed
4. **Audit Trail**: All approval decisions are logged with timestamps
5. **Automatic Cleanup**: Expired requests are automatically removed

## Backward Compatibility

The old `updateSquare` method is maintained but deprecated, automatically redirecting to the new approval workflow. This ensures existing frontend code continues to work while encouraging migration to the new system.

## Troubleshooting

### Fixed Issue: JsonElement Error
**Problem**: `'System.Text.Json.JsonElement' does not contain a definition for 'SquareId'`

**Root Cause**: The approval workflow was calling `UpdateSquareGloballyAsync()` which only updated the database but didn't send SignalR notifications. The frontend was expecting SignalR events to update the UI.

**Solution**: Modified the `ApproveSquareRequest` method in the SignalR hub to explicitly send `GlobalSquareUpdate` events to all clients after approving a request, ensuring the frontend receives the proper notification format.

**Key Changes**:
- Added explicit `GlobalSquareUpdate` SignalR event in approval flow
- Ensured all clients receive immediate notification when squares are approved
- Maintained backward compatibility with existing global update structure

### Fixed Issue: Pending Approvals Not Showing in Real-Time
**Problem**: Pending approval requests were being received by the server but the Board Management page only showed them after a manual page refresh.

**Root Cause**: The SignalR event handlers were calling `LoadPendingApprovals()` which sent a request to the server, but the UI wasn't updating immediately because:
1. The local `pendingApprovals` list wasn't being updated in real-time
2. The `UpdateDisplaySquares()` wasn't being called to refresh the visual indicators
3. No fallback mechanism existed for missed SignalR events

**Solution**: Enhanced the real-time update mechanism:
- **Immediate local updates**: Remove processed approvals from local list immediately for instant UI feedback
- **Enhanced SignalR handlers**: Added better state management and debug logging
- **Periodic refresh timer**: Added 10-second fallback timer to ensure approvals are always up-to-date
- **Visual indicator updates**: Ensure `UpdateDisplaySquares()` is called after approval list changes
- **Improved processing flow**: Added small delays and multiple refresh points during approval/denial process

**Key Changes**:
- Added immediate removal of processed approvals from local list
- Enhanced `NewApprovalRequest` and `ApprovalRequestProcessed` event handlers
- Added periodic refresh timer as fallback mechanism
- Improved error handling and debug logging
- Better state synchronization between server and client

## Next Steps

1. Update the frontend UI to support the approval workflow
2. Add admin dashboard components for managing approvals
3. Implement user notifications for approval status
4. Add visual indicators for pending squares
5. Test the complete approval workflow end-to-end

## UI Implementation Complete ✅

The Board Management page now includes complete approval workflow UI:

### **Visual Features Added**:

1. **Pending Approval Requests Section**:
   - Yellow warning card that appears when there are pending requests
   - Shows all pending approval requests with client details
   - Real-time updates when new requests come in
   - Refresh button to manually reload pending requests

2. **Square Visual Indicators**:
   - **Yellow pulsing border** on squares with pending approval requests
   - **Clock icon** next to the check/uncheck status for pending squares
   - **Pending indicator badge** showing "Request to CHECK/UNCHECK"
   - Maintains existing color coding for different square types

3. **Approval Actions**:
   - **Green "Approve" button** for quick approval
   - **Red "Deny" button** that opens a modal for reason entry
   - **Loading states** during processing to prevent double-clicks
   - **Optional denial reason** with textarea input

4. **Real-time Updates**:
   - Automatic refresh when new approval requests arrive
   - Activity log entries for all approval actions
   - Status updates when requests are processed
   - Immediate visual feedback for all actions

### **User Experience**:
- **Clear visual hierarchy**: Pending requests shown prominently at the top
- **Intuitive color coding**: Yellow for pending, green for approved, standard for normal
- **Responsive design**: Works on desktop and mobile devices
- **Accessibility**: Proper ARIA labels and keyboard navigation
- **Professional appearance**: Clean Bootstrap-based design matching existing admin interface

The admin now has complete visibility and control over the approval workflow with obvious visual cues and easy-to-use approval/denial interface.
