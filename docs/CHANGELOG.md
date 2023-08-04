# Changelog

## 0.5.2-preview
### Added
- `GetWikiLinksAsync` to `WikiDataManager`

## 0.5.1-preview
### Fixed
- `WikiEditComponent` typos

## 0.5.0-preview
### Added
- `WikiEditComponent` for easy implementing of alternative edit controls.
### Changed
- Made `OfflineSupportContent` members protected for inheritance

## 0.4.1-preview
### Changed
- Remove `[Authorize]` attribute from server methods, check `IsAuthorized` directly

## 0.4.0-preview
### Changed
- Update to .NET 8 preview

## 0.3.0-preview
### Added
- Dynamic contents
- `ShowPageTools` parameter for `WikiLeftDrawer`

## 0.2.8-preview
### Fixed
- Non-existing pages

## 0.2.7-preview
### Fixed
- Category links

## 0.2.6-preview
### Updated
- Update dependencies

## 0.2.5-preview
### Changed
- Removed version footer
### Fixed
- Handle 204 and allow fallback to offline

## 0.2.4-preview
### Changed
- Return 204 instead of 404

## 0.2.3-preview
### Changed
- Add token redirect to API calls

## 0.2.2-preview
### Changed
- Hide last update if it's a default value

## 0.2.1-preview
### Updated
- Update dependencies

## 0.2.0-preview
### Added
- Archive support
- Built-in support for user domains to make drafts
- Offline support
### Changed
- Update to .NET 7
- Extract default left drawer to its own component, for easy re-use
- Sample project simplified, take advantage of offline support

## 0.1.2-preview
### Changed
- Style fixes

## 0.1.1-preview
### Changed
- Update to .NET 7 RC1
### Fixed
- Table of contents styles

## 0.1.0-preview
### Added
- Initial preview release