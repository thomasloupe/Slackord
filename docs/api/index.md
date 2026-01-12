# Slackord API Reference

This section contains the complete API documentation for Slackord, automatically generated from XML documentation comments in the source code.

## Namespaces

Browse the API by namespace:

- **Slackord** - Core application classes
- **Slackord.Classes** - Business logic and utilities

## Key Classes

### Core Application
- `App` - Main application entry point
- `MainPage` - Primary user interface
- `AppShell` - Application shell and navigation

### Discord Integration
- `DiscordBot` - Manages Discord bot functionality including connection, message posting, and slash commands

### Import System
- `ImportJson` - Handles JSON import from Slack exports
- `ImportSession` - Manages import session state and progress
- `SlackdumpImporter` - Support for Slackdump exports

### Message Processing
- `Deconstruct` - Parses Slack message formats
- `Reconstruct` - Builds Discord-compatible messages
- `ProcessingManager` - Orchestrates the import workflow

### Utilities
- `BandwidthAnalysisUtility` - Monitors and manages API rate limits
- `SmartDownloadUtility` - Handles attachment downloads
- `ImportCleanupUtility` - Cleans up after imports
- `Logger` - Application logging

> **Note**: Run `docfx metadata` to generate the full API documentation from source code.
