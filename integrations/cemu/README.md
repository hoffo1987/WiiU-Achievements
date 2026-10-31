# Cemu 2.6 development integration

See [the integration guide](../../docs/CEMU-INTEGRATION.md) for use, architecture,
build requirements and validation limits.

- `prepare.ps1`: pinned source and safe allowlisted overlay.
- `build.ps1`: managed module/tests and separate native Windows build.
- `cemu-2.6.patch`: lifecycle, submitted-frame and Tools-menu hooks.
- `native/`: Windows CLR host, packet ABI and CMake integration.
- `native-tests/`: clean-CI native host fixture, without game files.
- `workflows/wiiu-achievements.yml`: native smoke then full Windows build.

The Desktop installation and private user state are never staged or replaced.
