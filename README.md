# Elite Dangerous Data Relay

![Elite Dangerous Data Relay](https://img.shields.io/badge/Elite%20Dangerous-Data%20Relay-orange?style=flat-square)
[![GitHub License](https://img.shields.io/github/license/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/blob/main/LICENSE.txt)
[![Last Commit](https://img.shields.io/github/last-commit/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/commits/main)
[![Top Language](https://img.shields.io/github/languages/top/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay)

A lightweight Windows utility that monitors your **Elite Dangerous** status and cargo in real-time. It displays the info via a clean UI, a customizable in-game overlay, and a text file output perfect for streaming.

![Screenshot](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/Screenshot.png)
![GameOverLay](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/GameOverlay.png)

---

## ✨ Core Features

* **Live Data Tracking**: View your CMDR name, ship, credit balance, and cargo count as they change.
* **Material Tracking**: Keep an eye on your Raw, Manufactured, and Encoded materials with a dedicated UI tab and a new, optional overlay.
* **Multiple Views**:
    * **Desktop UI**: A simple, clean window for at-a-glance information.
    * **In-Game Overlay**: Customizable overlays to see your status, cargo, and materials without leaving the game.
    * **Text File Output**: Export live data to a text file for use in streaming software like OBS.
* **Global Hotkeys**: Control the app without leaving the game. Set keys to start/stop monitoring and show/hide the overlay.
* **Session Tracking**: Automatically track credits earned and cargo collected during your play session.
* **Highly Configurable**:
    * Independently toggle overlay panels (status, cargo, and materials).
    * Pin specific materials to a watchlist from the main UI or the settings window.
    * Customize the text file format and output location.
* **Smart & Unobtrusive**:
    * Automatically detects the game's data folder.
    * Stops monitoring when the game closes.
    * Minimizes to the system tray to stay out of the way.

---

## 🚀 Getting Started

### Prerequisites

* Windows OS
* [.NET Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* Elite Dangerous

### Installation & Usage

1.  Download the **latest release** and run `EliteDataRelay.exe`.
2.  Launch Elite Dangerous.
3.  Click **Start** in the app to begin monitoring. Your data will appear in the UI, the in-game overlay (if enabled), and the output text file.
4.  To configure options like the overlay, hotkeys, or file output, click **Settings**.
5.  The app minimizes to the system tray. To exit completely, right-click the tray icon and select **Exit**.

---

## 🛠️ Building from Source

1.  Clone the repository:
    `git clone https://github.com/insert3coins/EliteDataRelay.git`
2.  Navigate to the project directory and restore dependencies:
    `cd EliteDataRelay && dotnet restore`
3.  Run the application:
    `dotnet run`

---

## 📌 Project Roadmap

-   [x] Settings UI for easy configuration.
-   [x] Customizable `cargo.txt` output format.
-   [x] System tray icon.
-   [x] Display CMDR name, ship, and credit balance.
-   [x] Configurable in-game overlay.
-   [x] Configurable global hotkeys.
-   [ ] Package the application with an installer.

---

## 🔰 Contributing

Contributions are welcome! Fork the repository, create a feature branch, and submit a pull request.

---

## 🎗 License & Disclaimer

This project is licensed under the **GNU General Public License v3.0**. See `LICENSE.txt` for details.

This tool was created using assets and imagery from Elite: Dangerous, with the permission of Frontier Developments plc for non-commercial purposes. It is not endorsed by nor reflects the views or opinions of Frontier Developments.