# Prompt: Create a "Project Dashboard" (Dual-Target: Desktop & Web)

Act as a Senior .NET Solution Architect. Build a local-first development dashboard using **.NET 9**. The solution must target two specific platforms using a shared codebase:

1.  **Desktop App:** A native Windows/macOS application (MAUI Blazor Hybrid).
2.  **Web App:** A browser-based dashboard (ASP.NET Core Blazor Web).

## The Mission

Automate the discovery and tracking of local git repositories. The Desktop app handles deep file-system access, while the Web app provides a remote-accessible view of the discovered data.

## Architecture & Shared Logic

- **Shared Library:** A Razor Class Library (RCL) containing all UI components (Tailwind CSS), Services, and the EF Core Data Layer.
- **Scanner Engine:** A robust background service using `LibGit2Sharp` to extract tech stacks, commit history, and "Project Health."
- **Persistence:** **Entity Framework Core with SQLite**.
  - _Desktop:_ Stores the DB in `AppData/Local`.
  - _Web:_ Connects to the same SQLite file or a shared volume.
- **Flexible Metadata:** Use `System.Text.Json` columns to store "inferred_type" (e.g., "rails-app", "dotnet-api") without needing DB migrations for new fields.

## Core Features

- **Auto-Discovery:** Scans `SCAN_ROOT_PATH` (e.g., `~/Development`) recursively.
- **Rich Extraction:** Detects tech stacks (identifying .csproj, package.json, go.mod, etc.).
- **Dual Dashboard:** - **Quick Resume:** Recent activity cards.
  - **Smart Groups:** "Stalled" (no commits in 30 days) and "Active" (commits this week).
  - **Left Rail:** Navigation with pinned projects and search.
- **Deep Links:** (Desktop Only) Button to "Open in VS Code" or "Open in Terminal."

## Technical Stack

- **Framework:** .NET 9 (MAUI Hybrid for Desktop / Blazor Web for Browser).
- **UI:** Blazor + Tailwind CSS 4.
- **Git:** LibGit2Sharp.
- **Database:** SQLite + EF Core.
- **Task Runner:** Periodic Background Tasks for folder watching.

## Configuration (appsettings.json / Environment)

| Variable           | Default         | Description                    |
| :----------------- | :-------------- | :----------------------------- |
| `SCAN_ROOT_PATH`   | `~/Development` | Path to scan                   |
| `SCAN_CUTOFF_DAYS` | `240`           | Skip aging repos               |
| `OPEN_IN_EDITOR`   | `code`          | Default CLI command for "Open" |

## Expected Output

1. **Solution Structure:** Show how to organize the Shared RCL, the MAUI project, and the Web project.
2. **Data Layer:** The `Project` model with a JSON metadata property.
3. **Scanner Implementation:** The recursive logic to find `.git` folders efficiently.
4. **Platform-Specific Tweaks:** How to handle file-system access differently on Web vs. Desktop.
