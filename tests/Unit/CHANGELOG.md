# CHANGELOG

## [1.20.0] - 2026-08-07
### Added
- **Waifu2x Integration:** Full integration of the Waifu2x upscaler into the core library.
- **Core Library Migration:** Completed migration to `RoSpriteEnhancer.Core` for better platform independence.
- **Multi-threaded Batching:** Enhanced `BatchProcessor` with `Parallel.ForEachAsync`.

### Changed
- Updated all project versions to 1.20.0.
- Refactored SPR reading logic to support newer binary formats.
- Improved memory management for large bitmap processing.

### Fixed
- Fixed path resolution issues in the test project.
- Resolved magenta colorkey transparency bugs in scaling operations.
- Corrected ACT anchor point scaling formulas.

### Security
- Implemented semaphore-based concurrency limits for external CLI tool execution.
