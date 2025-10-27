# Elite Data Relay v0.55.4 - Cargo Monitoring Performance Update

**Release Date:** October 27, 2025

## What's New

This release focuses on making cargo monitoring more responsive and snappier when you're actively playing Elite Dangerous.

### ⚡ Performance Improvements

**Faster Cargo Response Times**
- Cargo overlay now updates ~42% faster when picking up or jettisoning cargo
- Reduced debounce delay from 50ms to 25ms for instant feedback
- Optimized file retry timing from 50ms to 30ms
- Enhanced file reading with 4KB buffers for better I/O performance

**Performance Metrics:**

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Debounce delay | 50ms | 25ms | 50% faster |
| File retry delay | 50ms | 30ms | 40% faster |
| Max retry time | 250ms | 150ms | 40% faster |
| Total response | ~300ms | ~175ms | ~42% faster |

**Activity Tracking**
- Added internal activity monitoring for future adaptive features
- System now tracks when cargo is actively changing vs idle
- Foundation for future smart monitoring enhancements

### 🔍 UI Improvements

**Enhanced Exploration Log**
- Exploration Log now displays FSS completion status for visited systems
- Shows "FSS: Complete (X bodies detected)" for fully scanned systems
- Shows "FSS: X% (Y detected)" for partially scanned systems
- Provides clearer visibility into system exploration progress

## Technical Changes

### Modified Files:
- `Services/FileMonitoringService.cs` - Reduced debounce, added activity tracking
- `Services/CargoProcessorService.cs` - Enhanced file reading with buffering
- `Configuration/AppConfiguration.Misc.cs` - Optimized retry parameters
- `UI/ExplorationLogControl.cs` - Added FSS status display in system details

## Installation

### New Users
1. Download `EliteDataRelay-v0.55.4.zip`
2. Extract to your preferred location
3. Run `EliteDataRelay.exe`
4. Configure your Elite Dangerous journal path in settings

### Upgrading from Previous Versions
1. Download `EliteDataRelay-v0.55.4.zip`
2. Extract and replace your existing `EliteDataRelay.exe`
3. Your settings and data are preserved automatically

## System Requirements

- **OS:** Windows 10/11
- **.NET:** .NET 8.0 Runtime (included in executable)
- **Elite Dangerous:** Any version with journal files

## What's Coming Next

- Additional adaptive monitoring features
- Further performance optimizations
- Enhanced in-game state detection

## Known Issues

- None reported for this release

## Feedback & Support

- **Issues:** https://github.com/insert3coins/EliteDataRelay/issues
- **Discussions:** https://github.com/insert3coins/EliteDataRelay/discussions
