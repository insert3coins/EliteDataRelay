# Elite Dangerous Cargo Monitor

![Elite Dangerous Cargo Monitor](https://img.shields.io/badge/Elite%20Dangerous-Cargo%20Monitor-orange?style=flat-square)
[![GitHub License](https://img.shields.io/github/license/insert3coins/EliteCargoMonitor?style=flat-square)](https://github.com/insert3coins/EliteCargoMonitor/blob/main/LICENSE.txt)
[![Last Commit](https://img.shields.io/github/last-commit/insert3coins/EliteCargoMonitor?style=flat-square)](https://github.com/insert3coins/EliteCargoMonitor/commits/main)
[![Top Language](https://img.shields.io/github/languages/top/insert3coins/EliteCargoMonitor?style=flat-square)](https://github.com/insert3coins/EliteCargoMonitor)

A lightweight Windows utility for players of Elite Dangerous. It monitors your in-game cargo in real-time, displaying the contents and total count in a simple interface and exporting the data to a text file for use with streaming overlays or other tools.

---

## 📍 Overview

This tool provides a simple way to keep an eye on your ship's cargo without having to tab into the game's right-hand panel. It reads the `Cargo.json` file that Elite Dangerous generates, along with player journals to determine total capacity. The output is displayed in the app window and also written to a `cargo.txt` file, which is perfect for adding a cargo display to your stream via OBS or other broadcasting software.

<!-- Optional: Add a screenshot of the application here -->
<!-- !Elite Cargo Monitor Screenshot -->

## ✨ Features

-   **Real-time Monitoring**: Watches `Cargo.json` for any changes and updates instantly.
-   **Cargo Capacity**: Reads player journal files to display your total cargo capacity (e.g., `128/256`).
-   **Simple UI**: A clean, no-fuss window displays your current cargo list.
-   **Text File Output**: Exports cargo data to `out/cargo.txt` for easy integration with other tools (like OBS for streaming).
-   **Audio Cues**: Plays sounds when monitoring starts and stops.
-   **Automatic Path Detection**: Automatically finds the default Elite Dangerous player data folder.

## 🚀 Getting Started

### Prerequisites

-   Windows Operating System
-   .NET Desktop Runtime
-   Elite Dangerous installed.

### Installation

The easiest way to use the Cargo Monitor is to download the latest release.

1.  Go to the **Releases** page.
2.  Download the `.zip` file from the latest release.
3.  Extract the contents to a folder of your choice.
4.  Run `EliteCargoMonitor.exe`.

### Building from Source

If you want to build the project yourself:

1.  Clone the repository:
    ```sh
    git clone https://github.com/insert3coins/EliteCargoMonitor.git
    ```
2.  Navigate to the project directory:
    ```sh
    cd EliteCargoMonitor
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
2.  Run `EliteCargoMonitor.exe`.
3.  Click **Start** to begin monitoring.
4.  The application window will update whenever your cargo changes.
5.  A file named `cargo.txt` will be created and updated in an `out` sub-folder inside the application's directory. You can add this text file as a source in OBS.
6.  Click **Stop** to pause monitoring.

## 📌 Project Roadmap

-   [ ] Create a settings UI for easier configuration (e.g., custom file paths).
-   [ ] Allow customization of the `cargo.txt` output format.
-   [ ] Add a system tray icon for running in the background.
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
