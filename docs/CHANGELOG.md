# Changelog

## 0.7.5-preview
### Updated
- Update dependencies

## 0.7.4-preview
### Changed
- Allow setting unauthorized to true from external code

## 0.7.3-preview
### Changed
- Simplified project structure

## 0.7.2-preview
### Changed
- Simplified NuGet package structure

## 0.7.1-preview
### Changed
- Enable dialogs and snackbars in injected components

## 0.7.0-preview
### Added
- Render mode control for injected components
- Web app example project

## 0.6.6-preview
### Fixed
- Link preview

## 0.6.5-preview
### Changed
- Improve security

## 0.6.4-preview
### Updated
- Update to .NET 8

## 0.6.3-preview
### Changed
- Add automatic `TypeInfoResolverChain` configuration to server

## 0.6.2-preview
### Updated
- Update to .NET 8 RC1
### Changed
- Rename `AddTavenemWikiClient` to `AddWikiClient`
- Rename `AddWiki` to `AddWikiServer`

## 0.6.1-preview
### Changed
- Make `WikiState` public

## 0.6.0-preview
### Updated
- Update dependencies

## 0.5.8-preview
### Changed
- Add named, undefined authorization policy to wiki controller "WikiPolicy" for user customization

## 0.5.7-preview
### Changed
- Ensure wiki controller captures logged in users while allowing anonymous users

## 0.5.6-preview
### Changed
- Made `wiki-main-heading` an `id` instead of a `class`

## 0.5.5-preview
### Added
- Title requests

## 0.5.4-preview
### Updated
- Update dependencies

## 0.5.3-preview
### Added
- `GetWikiLinksAsync` to `WikiEditComponent`

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
### Updated
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