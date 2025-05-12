# DirectPackageInstaller

A tool to preview and send PKG files to your PS4 or PS5 using direct download links.  
This tool supports three different methods for sending PKGs:

-   Remote Package Installer Homebrew
-   GoldHEN Payload Server    
-   etaHEN DPIv2 API
    

### Features

-   Preview PKG files    
-   Standalone PS4 package downloads    
-   PS5 etaHEN support
-   Automatic proxying to speed up PS4 downloads    
-   Support for RAR/7z compressed files    
-   Resumable downloads (only for uncompressed files)    
-   Segmented downloads    
-   Support for several file hosting services    
-   Support for PSN update manifests (split PKGs)    
-   Works with or without Remote Package Installer    
-   Command Line Interface (CLI)    
-   JDownloader Click'n Load support   
    

### Supported File Hosting Services

-   Any direct link (without authentication)    
-   AllDebrid (API key required)    
-   RealDebrid ([API key required](https://real-debrid.com/apitoken))    
-   Google Drive (with or without authentication)    
-   Mediafire (no authentication required)    
-   PixelDrain    
-   1Fichier (must wait 60 minutes after each download)
    

### How to Install

1.  Download and install the [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)    
2.  Download a build compatible with your platform:
    

<details> <summary>Windows</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-X64.zip)    
-   [x86](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Windows-X86.zip)    

</details> <details> <summary>Android</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-X64.zip)    
-   [x86](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Android-X86.zip)    

</details> <details> <summary>Linux</summary>

-   [ARM](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-ARM.zip)    
-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/Linux-X64.zip)    

</details> <details> <summary>macOS</summary>

**CLI builds:**

-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-ARM64.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-X64.zip)    

**App builds:**

-   [ARM64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-ARM64-APP.zip)    
-   [x64](https://raw.githubusercontent.com/marcussacana/DirectPackageInstaller/Updater/OSX-X64-APP.zip)   

</details>

### How to Use

#### Direct Download Mode

1.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
2.  Open DirectPackageInstaller.    
3.  Paste a direct PKG URL into the "PKG URL" field.    
4.  Click "Load" and wait.    
5.  Click "Install" and wait again.    
6.  Done! You can close the application or shut down your PC.    

#### Proxied Download Mode (for PS4s without internet access)

1.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
2.  Open DirectPackageInstaller.    
3.  Go to `Options -> Proxy Downloads` and enable it.    
4.  Paste a direct PKG URL.    
5.  Click "Load", then "Install".    
6.  Wait for the installation to finish.
    

#### Segmented Download Mode (fastest)

1.  Make sure you have enough free disk space.
2.  Launch Remote Package Installer or enable GoldHEN Payload Server.
3.  Open DirectPackageInstaller.
4.  Go to `Options -> Segmented Downloads` and enable it.
5.  Paste a direct PKG URL.
6.  Click "Load", then "Install".
7.  The PKG will be downloaded and preloaded before installation.

> Note: In segmented mode, the PS4 cannot track download progress accurately.

#### Compressed File Mode (always proxied)
1.  Make sure you have enough free disk space.    
2.  Launch Remote Package Installer or enable GoldHEN Payload Server.    
3.  Paste the direct URL to your compressed file.    
4.  Click "Load" and wait.    
5.  [Optional] Select the desired PKG from the file list.    
6.  Click "Install" and wait.
    

----------

### macOS CLI Usage

Use the terminal to run the app:
```
./DirectPackageInstaller.Desktop
```
In some cases, you may need to run:
```
dotnet DirectPackageInstaller.Desktop.dll
```

### How It Works

DirectPackageInstaller is a simple tool that uses the Remote Package Installer's API to add PKG downloads directly to your PS4 or PS5. It helps you preview and send packages easily.

Additionally, the tool features an automatic proxy server to accelerate downloads. Since the PS4 has much better speeds over LAN than WAN, proxying through your PC can significantly improve performance.

It also works without the Remote Package Installer if you enable the binloader — the tool will automatically decide which method to use.

When using compressed files (RAR/7z), everything happens in parallel: download, extraction, and upload to your console. This saves time and disk space compared to traditional methods.

Segmented mode downloads PKGs in parallel threads to your PC, while sending them to the console. It's the fastest option, though it won't show progress on the PS4 screen.

----------

### Command Line Interface (CLI)

An experimental CLI is available.

Note: RAR/7z and segmented downloads are not supported in CLI mode (yet).  
To view available options:

-   On Windows: `DirectPackageInstaller.Desktop.exe -help`    
-   On other systems: `DirectPackageInstaller.Desktop -help`
    

----------

### Dependencies

Requires [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime)

----------

### How to Build

1.  Install the .NET 8.x SDK.    
2.  Run the `Build.cmd` script (works on both Windows and Unix-like systems).    
3.  A `Release` directory with the compiled binaries will be created.    

For custom payloads, run `make` in the payload directory and replace the generated `payload.bin` inside `DirectPackageInstaller/DirectPackageInstaller/payload`.

### Android builds:
-   Targets API level 26    
-   Install Android SDK and NDK    
-   Accept SDK licenses    
-   Edit `Build.cmd` to set SDK/NDK paths    
-   Run `Build.cmd` as admin (first time only)

Alternatively, fork the project and use GitHub Actions for builds.

----------

### Q&A

**My download is taking too long to start**  
You're probably downloading a multipart compressed file. The more volumes, the longer it takes to scan and validate. This isn’t a bug — just how compressed file access works. Please be patient.

**Download speed is slow, even in segmented mode**  
Segmented mode doesn't report accurate progress to the PS4. Use your PC's Task Manager to monitor real network usage.

**Segmented mode and only 2 Mbps?!**  
Some hosting sites (e.g., Zippyshare) are just slow. Try testing with Google Drive for better performance.

**Recommended file hostings?**

-   AllDebrid / RealDebrid    
-   Google Drive    
-   Mediafire
-   Everything else
-   1Fichier (slowest)
    

**I got an error during installation**  
Open an issue! Seriously, don’t worry — mistakes happen. If there's a crash, check the `DPI-Crash.log` for details.

----------

### Disclaimer

This tool is for legitimate purposes only. It’s intended to help users install their personal backups. Piracy is not supported.  
You are responsible for how you use this software.

----------

### Warnings

-   Avoid multiple parallel decompressions — process one file at a time.
-   Compressed file downloads create a temporary PKG cache, requiring free disk space equal to the final PKG size.

----------

## Credits

-   **LibOrbisPkg** by _maxton_
-   **HttpServerLite** by _jchristn_
-   Payload template by _sleirsgoevy_
-   PS4 OS internals help by _LM_
-   PS4 export definitions by _OpenOrbis SDK_
-   **DirectPackageInstaller** by _marcussacana_
