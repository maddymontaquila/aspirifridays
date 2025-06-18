# AspiriFridays Bingo Board Admin Website Specification

## Project Title
**AspiriFridays Bingo Admin Board**

## Project path
./src/BingoBoard.Admin

## Overview
A web-based admin interface for managing the bingo board content for the AspiriFridays Bingo Board. This tool allows administrators to create, edit, and delete bingo squares, as well as manage the overall bingo set configuration.

## Core Features

To handle communication with clients, the admin interface will use SignalR for real-time updates.

### 1. Admin Dashboard
- A user-friendly dashboard to manage bingo squares
- Display a list of existing bingo squares with options to mark them as checked or unchecked
- Admin should be able to send updates to all clients when a square is checked or unchecked

### 2. Customizable Bingo Sets
- Configurable via a JSON file (Create fake ones for now - a list of 30 so there is randomization)
- Each square has:
  - `label`: Display text
  - `id`: Internal ID
  - `type`: optional categorization (e.g., bug, dev moment, inside joke)

### 3. Handle Client Connections
- Admin should be able to see a list of connected clients
- Every time a new client connects, the admin should store on a redis cache the current bingo set
- The admin should be able to quickly verify the current bingo set for each client

## Tech Stack
- **Frontend**: Blazor
- **Dev Stack**: Uses .NET Aspire for local dev
- **Hosting**: Will be deployed with Aspire to Azure Container Apps