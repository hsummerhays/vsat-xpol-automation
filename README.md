# VSAT XPOL Automation

This repository hosts the top-level project for VSAT cross-polarization (XPOL) monitoring automation.

## Structure
- `MainstreamData.Monitoring.VsatXpolLmp` — LMP-specific components (library consumed by the website)
- `VsatXpolLmp.Web/` — ASP.NET Web Application that consumes the LMP library (scaffolded)
- `VsatXpolRmp/` — Windows Service host (Remote Monitor Point) running near the spectrum analyzer
- `VsatXpolBatchMP/` — Batch service host (optional) for scheduled/queued jobs

## Core Libraries
- **`MainstreamData.Monitoring.VsatXpol/`**: XPOL analysis library (signals, analyzers, settings). Lives in this repo as a library (not a submodule).
- **`MainstreamData.Data/`**: Data access layer and shared models (entities/DTOs, configuration, repository/file helpers). May become a submodule later.
- **`MainstreamData.Logging/`**: Provides `ExtendedLogger`, event args, comparison helpers, and rolling file deletion for consistent, file-based logging.
- **`MainstreamData.Monitoring/`**: Core monitoring components (e.g., `MonitorService`, `EventSource`, controller integrations) that underpin multiple monitoring apps.
- **`MainstreamData.Utility/`**: Utilities such as `ApplicationInfo`, INI readers (`IniFile`, `IniSection`), file readers, text splitters, and common extension methods.
- **`MainstreamData.Web/`**: Networking and web utilities including `TelnetClient`, `WebDavRequest`, HTML `WebpageParser`, and related extensions.

## Getting Started
1. Clone this repository.
2. Initialize submodules after they are added:
   ```powershell
   git submodule update --init --recursive
   ```
3. Open the solution in Visual Studio (targeting .NET Framework 3.5). Build the solution.
4. To run the LMP website:
   - Create a `VsatXpolLmp.Web/Web.config` from `Web.config.example` and provide environment-specific values.
   - Start the site using IIS Express or attach to your IIS site pointing at `VsatXpolLmp.Web/`.

## Terminology
- **LMP (Local Monitor Point)**: ASP.NET website that orchestrates monitoring and polling operations locally.
- **RMP (Remote Monitor Point)**: Windows service running on a remote server connected to the spectrum analyzer.

## Architecture & Data Flow
- **RMP (service)** runs near the spectrum analyzer and exposes endpoints (e.g., WCF) for measurement/control.
- **LMP (website)** calls the RMP endpoints to poll measurements and retrieve analyzer data.
- **Shared libraries** provide common logic and models used by both sides (e.g., `MainstreamData.Monitoring`, `MainstreamData.Monitoring.VsatXpol`, `MainstreamData.Monitoring.VsatXpolLmp`).
- Logging and configuration are environment-specific and configured via `App.config`/`Web.config` (use the provided `*.example` templates).

## Development
- Default branch: `main` (to be created on first push)
- Submodules will be added under the directories listed above.
- This repository was re-initialized on 2025-10-02 to consolidate multiple projects as submodules.

## Security & Configuration
- **Do not commit secrets**: Private keys (`*.pfx`, `*.snk`), passwords, tokens, or connection strings must not be committed.
- **Strong-name key**: If a PFX is required for signing, store it securely outside the repo and load via CI secrets. The historical file `MainstreamData.Monitoring.VsatXpolLmp/MainstreamDataDotNetStrongNameKey2010-06-14.pfx` must not be published.
- **Configs**: Use `App.config.example` templates and supply real values via environment variables or machine-scoped config.
  - Provided templates:
    - `VsatXpolRmp/App.config.example`
    - `VsatXpolBatchMP/App.config.example`
    - `VsatXpolLmp.Web/Web.config.example`
- **Git hygiene**: Build outputs (`bin/`, `obj/`) and user/IDE files are ignored via `.gitignore`.

## LMP Website
- **Project**: `VsatXpolLmp.Web/` (ASP.NET Web Application, .NET Framework 3.5)
- **Dependency**: Project reference to `MainstreamData.Monitoring.VsatXpolLmp`
- **Entry**: `Default.aspx` with basic status label; extend to invoke LMP workflows.
- **Configuration**: Copy `Web.config.example` to `Web.config` and set keys like `LogPath` and `ServiceBaseAddress`.
- **Hosting**: IIS Express for development; IIS for production. Ensure the app pool .NET CLR version supports 3.5.

## RMP Service
- **Projects**: `VsatXpolRmp/` (primary RMP service), optional `VsatXpolBatchMP/` (batch host)
- **Role**: Runs on a remote server connected to the spectrum analyzer; exposes endpoints (WCF) consumed by the LMP website.
- **Entry point**: `VsatXpolRmp/Program.cs` calls `MonitorApplication.Start(new VsatXpolRmp());`
- **Configuration**:
  - Copy `VsatXpolRmp/App.config.example` to `VsatXpolRmp/App.config` and set environment-specific values (logging paths, WCF base address, analyzer settings).
  - Open firewall for the configured port (default example uses `http://localhost:8000/...`).
- **Build**: Build the service project in Release on the target architecture (many projects here use .NET 3.5 and x86).
- **Install** (choose one):
  - InstallUtil:
    ```powershell
    InstallUtil.exe "<deploy-path>\VsatXpolRmp.exe"
    ```
  - sc.exe:
    ```powershell
    sc create VsatXpolRmp binPath= "<deploy-path>\VsatXpolRmp.exe" start= auto
    sc description VsatXpolRmp "VSAT XPOL Remote Monitor Point"
    ```
- **Start/Stop**:
  ```powershell
  sc start VsatXpolRmp
  sc stop VsatXpolRmp
  ```
- **Service identity**: Run under an account with access to the spectrum analyzer and file/log locations.
- **Relationship to LMP**: LMP website calls RMP’s endpoints to poll measurements and retrieve analyzer data.
