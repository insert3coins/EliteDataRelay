# Elite Dangerous Data Relay

![Elite Dangerous Data Relay](https://img.shields.io/badge/Elite%20Dangerous-Data%20Relay-orange?style=flat-square)
[![GitHub License](https://img.shields.io/github/license/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/blob/main/LICENSE.txt)
[![Last Commit](https://img.shields.io/github/last-commit/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay/commits/main)
[![Top Language](https://img.shields.io/github/languages/top/insert3coins/EliteDataRelay?style=flat-square)](https://github.com/insert3coins/EliteDataRelay)

A lightweight Windows utility for Elite Dangerous that monitors your in-game status, cargo, and materials in real-time. It displays the information via a clean UI, customizable in-game overlays, and a text file output perfect for streaming.

---

## 📸 Screenshots


![Screenshot](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/Screenshot.png)

![ScreenshotMaterials](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/Mats.png)

![ScreenshotShip](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/Ship.png)

![GameOverLay](https://github.com/insert3coins/EliteDataRelay/blob/master/Images/GameOverlay.png)

---

## ✨ Features

-   **Live Data Tracking**: Monitors your **CMDR name**, **ship**, **credit balance**, **cargo count**, and **materials** (Raw, Manufactured, Encoded) in real-time.
-   **Multiple Views**:
    -   **Desktop UI**: A simple, clean window for at-a-glance information.
    -   **In-Game Overlay**: Customizable overlays to see your status, cargo, and materials without leaving the game.
    -   **Text File Output**: Exports live data to a text file for use in streaming software like OBS.
-   **Highly Configurable**: Set **global hotkeys**, toggle overlay panels, pin materials to a watchlist, and customize the text file format.
-   **Session Tracking**: Automatically tracks credits earned and cargo collected during your play session.
-   **Smart & Unobtrusive**: Automatically detects the game's data folder, stops monitoring when the game closes, and minimizes to the system tray.

---

## 🚀 Getting Started

### Prerequisites

-   Windows OS
-   [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
-   Elite Dangerous

### Installation & Usage

1.  Download the **latest release** and run `EliteDataRelay.exe`.
2.  Launch Elite Dangerous.
3.  Click **Start** in the app to begin monitoring.
4.  Click **Settings** to configure the overlay, hotkeys, or file output.
5.  The app minimizes to the system tray. To exit completely, right-click the tray icon and select **Exit**.

---

## 🛠️ Building from Source

If you want to build the project yourself:

1.  Clone the repository:
    ```sh
    git clone [https://github.com/insert3coins/EliteDataRelay.git](https://github.com/insert3coins/EliteDataRelay.git)
    ```
2.  Navigate to the project directory and restore dependencies:
    ```sh
    cd EliteDataRelay && dotnet restore
    ```
3.  Run the application:
    ```sh
    dotnet run
    ```

---

## 🔰 Contributing

Contributions are welcome! Whether it's reporting a bug, suggesting a feature, or submitting a pull request, your help is appreciated. Feel free to fork the repository, create a feature branch, and submit a pull request.

---

## 🎗 License & Disclaimer

This project is licensed under the **GNU General Public License v3.0**. See `LICENSE.txt` for details.

This tool was created using assets and imagery from Elite: Dangerous, with the permission of Frontier Developments plc for non-commercial purposes. It is not endorsed by nor reflects the views or opinions of Frontier Developments.