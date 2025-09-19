# Elite Dangerous Data Relay

![Elite Dangerous Data Relay](https://img.shields.io/badge/Elite%20Dangerous-Data%20Relay-orange?style=flat-square)
[![GitHub License](https://img.shields.io/github/license/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/blob/main/LICENSE.txt)
[![Last Commit](https://img.shields.io/github/last-commit/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/commits/main)
[![Top Language](https://img.shields.io/github/languages/top/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay)

A lightweight Windows utility for players of Elite Dangerous. It monitors your in-game cargo and status in real-time, displaying the information in a clean UI, exporting it to a text file, and showing it via a customizable in-game overlay.

---

## 📍 Overview

This tool provides a simple way to keep an eye on your ship's cargo and status without having to tab into the game's right-hand panel. It reads the `Cargo.json`, `Status.json`, and player journal files to display your cargo, credit balance, CMDR name, and current ship. This information is available in three ways: a clean desktop UI, a highly configurable text file output for streaming software, and a new in-game overlay.

## 📸 Screenshot
![Screenshot](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/Screenshot.png)
![GameOverLay](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/GameOverlay.png)

## ✨ Features

-   **Real-time Cargo Monitoring**: Watches `Cargo.json` for any changes and updates instantly.
-   **Live Player Status**: Displays your **CMDR name**, **current ship ID**, and real-time **credit balance**.
-   **Cargo Capacity**: Reads player journal files to display your total cargo capacity (e.g., `128/256`).
-   **In-Game Overlay**: A redesigned, modern overlay displays your live status and cargo information.
    -   **Two-Panel Design**: A left panel for CMDR/ship info and a right panel for cargo.
    -   **Visual Cargo Meter**: The cargo overlay includes a visual bar to quickly see how full your hold is.
-   **Simple UI**: A clean, no-fuss window displays all your essential information at a glance.
-   **Visual Cargo Meter**: A bar in the bottom-right corner visually represents how full your cargo hold is.
-   **Global Hotkeys**: Configure system-wide hotkeys to start/stop monitoring and show/hide the overlay without leaving the game.
-   **Active Status Indicator**: A subtle animation appears when monitoring is active, providing clear visual feedback.
-   **Text File Output**: Exports cargo data to a configurable text file (default: `out/cargo.txt`) for easy integration with other tools (like OBS for streaming).
-   **Smart Start**: Silently checks if Elite Dangerous is running before showing the overlay, but allows other monitoring to start regardless.
-   **Automatic Stop on Game Exit**: Automatically stops monitoring and hides the overlay when the Elite Dangerous game process is closed.
-   **Audio Cues**: Plays sounds when monitoring starts and stops.
-   **Automatic Path Detection**: Automatically finds the default Elite Dangerous player data folder for `Cargo.json`, `Status.json`, and journal files.
-   **System Tray**: Minimizes to the system tray to run unobtrusively in the background.

## 🚀 Getting Started

### Prerequisites

-   Windows Operating System
-   .NET Desktop Runtime
-   Elite Dangerous installed.

### Installation

The easiest way to use the Data Relay is to download the latest release.

1.  Launch Elite Dangerous.
2.  Run `EliteDataRelay.exe`.
3.  Click **Start** to begin monitoring.
4.  The application window and in-game overlay (if enabled) will update whenever your cargo or status changes.
5.  A file named `cargo.txt` (by default) will be created and updated in the configured output directory (by default, an `out` sub-folder). You can add this text file as a source in OBS.
6.  Click **Stop** to pause monitoring.
7.  Minimize or close the window to send the application to the system tray. You can restore it by double-clicking the tray icon.

### In-Game Overlay

You can choose to enable the in-game overlay panels independently. By default, both are disabled.

-   **Enable left overlay**: Toggles the display of your CMDR name, ship, and credit balance.
-   **Enable right overlay**: Toggles the display of your cargo count and item list.

### Hotkeys

You can enable and configure global hotkeys to control the application while in-game. By default, hotkeys are disabled.

-   **Start Monitoring**: Starts all monitoring services.
-   **Stop Monitoring**: Stops all monitoring services.
-   **Show Overlay**: Makes the overlay windows visible.
-   **Hide Overlay**: Hides the overlay windows.

### Building from Source

If you want to build the project yourself:

1.  Clone the repository:
    ```sh
    git clone https://github.com/insert3coins/EliteDataRelay.git
    ```
2.  Navigate to the project directory:
    ```sh
    cd EliteDataRelay
    ```
3.  Restore the .NET dependencies:
    ```sh
    dotnet restore
    ```
4.  Run the application:
    ```sh
    dotnet run
    ```

## 🤖 Usage

1.  Launch Elite Dangerous.
2.  Run `EliteDataRelay.exe`.
3.  Click **Start** to begin monitoring.
4.  The application window and in-game overlay (if enabled) will update whenever your cargo or status changes.
5.  A file named `cargo.txt` (by default) will be created and updated in the configured output directory (by default, an `out` sub-folder). You can add this text file as a source in OBS.
6.  Click **Stop** to pause monitoring.
7.  Minimize or close the window to send the application to the system tray. You can restore it by double-clicking the tray icon.
8.  To fully close the application, use the **Exit** button or right-click the tray icon and select **Exit**.

## ⚙️ Settings

You can customize the application's behavior by clicking the **Settings** button.

### In-Game Overlay

You can choose to enable the in-game overlay panels independently. By default, both are disabled.

-   **Enable left overlay**: Toggles the display of your CMDR name, ship, and credit balance.
-   **Enable right overlay**: Toggles the display of your cargo count and item list.

---

### Enable File Output

You can enable or disable the creation of the output text file. This is useful if you only want to use the application's UI and don't need the file for streaming overlays. By default, this is disabled.

### Output File Format

You can change the format of the text written to the output file using a custom format string. The following placeholders are available:

-   `{count}`: Total number of items in cargo.
-   `{capacity}`: Total cargo capacity (blank if not yet known).
-   `{count_slash_capacity}`: A combined view, e.g., "128/256" or just "128".
-   `{items}`: A single-line list of all cargo items, e.g., `Gold (10) Silver (5)`.
-   `{items_multiline}`: A multi-line list of all cargo items.
-   `\n`: Inserts a newline character.

**Default format:** `{count_slash_capacity} | {items}`

### Output File Name

You can change the name of the output file (default is `cargo.txt`).

### Output Directory

You can specify the folder where the output file will be saved. By default, it is saved in an `out` sub-folder. You can type a path directly or use the **Browse...** button to open a folder selection dialog.

## 📌 Project Roadmap

-   [x] Create a settings UI for easier configuration.
-   [x] Allow customization of the `cargo.txt` output format.
-   [x] Add a system tray icon for running in the background.
-   [x] Display CMDR name, ship, and credit balance.
-   [x] Add a configurable in-game overlay.
-   [x] Add configurable global hotkeys for in-game control.
-   [ ] Package the application with an installer.

## 🔰 Contributing

Contributions are welcome! Whether it's reporting a bug, suggesting a feature, or submitting a pull request, your help is appreciated.

1.  **Fork the Repository**: Start by forking the project repository to your GitHub account.
2.  **Create a New Branch**: Work on a new branch with a descriptive name.
    ```sh
    git checkout -b feature/new-thing
    ```
3.  **Make Your Changes**: Code away!
4.  **Commit Your Changes**: Use a clear and descriptive commit message.
    ```sh
    git commit -m 'feat: Implement new thing'
    ```
5.  **Push to GitHub**: Push your changes to your forked repository.
    ```sh
    git push origin feature/new-thing
    ```
6.  **Submit a Pull Request**: Open a PR against the `main` branch of the original repository.

## 🎗 License

This project is licensed under the **GNU General Public License v3.0**. See the LICENSE.txt file for more details.

## 🙌 Disclaimer

This tool was created using assets and imagery from Elite: Dangerous, with the permission of Frontier Developments plc for non-commercial purposes. It is not endorsed by nor reflects the views or opinions of Frontier Developments and no employee of Frontier Developments was involved in the making of it.
