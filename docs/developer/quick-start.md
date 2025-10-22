# Quick Start Guide

Get PokeNET running on your machine in under 10 minutes.

## Prerequisites

### Required
- **.NET 9 SDK** or later
- **Git** for version control
- **Code editor** (Visual Studio 2022, JetBrains Rider, or VS Code)

### Platform-Specific

**Windows**:
- Visual Studio 2022 (Community Edition is free)
- Windows 10/11

**macOS**:
- Xcode Command Line Tools
- macOS 10.15+

**Linux**:
- Build essentials
- OpenAL (for audio)

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/youruser/PokeNET.git
cd PokeNET
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Project

**For DesktopGL (Cross-platform)**:
```bash
dotnet build PokeNET/PokeNET.DesktopGL/PokeNET.DesktopGL.csproj
```

**For WindowsDX (Windows only)**:
```bash
dotnet build PokeNET/PokeNET.WindowsDX/PokeNET.WindowsDX.csproj
```

### 4. Run the Game

**DesktopGL**:
```bash
dotnet run --project PokeNET/PokeNET.DesktopGL/PokeNET.DesktopGL.csproj
```

**WindowsDX**:
```bash
dotnet run --project PokeNET/PokeNET.WindowsDX/PokeNET.WindowsDX.csproj
```

## Verify Installation

You should see:
1. Console output showing mod loading
2. Game window with MonoGame orange background
3. No errors in console

## Next Steps

### For Players
- [Installing Mods](../modding/getting-started.md)
- [Configuration Guide](../configuration/system-overview.md)

### For Modders
- [Create Your First Mod](../modding/getting-started.md)
- [ModApi Overview](../api/modapi-overview.md)

### For Developers
- [Development Environment Setup](environment-setup.md)
- [Building from Source](building.md)
- [Contributing Guidelines](contributing.md)

## Troubleshooting

### Build Fails

**Missing SDK**:
```bash
# Check .NET version
dotnet --version  # Should be 9.0 or higher
```

**NuGet Issues**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear
dotnet restore
```

### Runtime Errors

**Missing MonoGame**:
- Ensure MonoGame.Framework.DesktopGL is installed
- Check NuGet package restore

**Audio Issues (Linux)**:
```bash
# Install OpenAL
sudo apt-get install libopenal-dev  # Debian/Ubuntu
sudo dnf install openal-soft-devel  # Fedora
```

### Platform-Specific Issues

**macOS: "Cannot open because developer cannot be verified"**:
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine /path/to/PokeNET.app
```

**Linux: Missing dependencies**:
```bash
# Install required libraries
sudo apt-get install libgdiplus libx11-dev
```

## Getting Help

- **Documentation**: You're reading it!
- **Discord**: [Join our community](https://discord.gg/yourserver)
- **GitHub Issues**: [Report problems](https://github.com/youruser/PokeNET/issues)
- **Discussions**: [Ask questions](https://github.com/youruser/PokeNET/discussions)

---

*Last Updated: 2025-10-22*
