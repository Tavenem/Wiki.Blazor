# Changelog

## 0.11.3-preview
### Added
- Wiki service configuration options

## 0.11.2-preview
### Removed
- Obsolete `DataStore` property on `WikiBlazorOptions`

## 0.11.1-preview
### Fixed
- Editing permissions

## 0.11.0-preview
### Added
- `IArticleRenderManager` and `ArticleRenderManager` to handle custom layout and rendering of wiki articles
- `IOfflineManager` and `OfflineManager` to control offline editing capabilities
### Removed
- `WikiBlazorOptions.ArticleEndMatter` (replaced by `IArticleRenderManager`)
- `WikiBlazorOptions.ArticleEndMatterRenderMode` (replaced by `IArticleRenderManager`)
- `WikiBlazorOptions.ArticleFrontMatter` (replaced by `IArticleRenderManager`)
- `WikiBlazorOptions.ArticleFrontMatterRenderMode` (replaced by `IArticleRenderManager`)
- `WikiBlazorOptions.CanEditOffline` (replaced by `IOfflineManager`)
- `WikiBlazorOptions.IsOfflineDomain` (replaced by `IOfflineManager`)

## 0.10.2-preview
### Updated
- Update dependencies

## 0.10.1-preview
### Fixed
- Preview popups
- Special page text

## 0.10.0-preview
### Updated
- Update to .NET 9
- Update dependencies
### Fixed
- Talk page

## 0.9.12-13-preview
### Updated
- Update dependencies

## 0.9.11-preview
### Changed
- Improve configuration
- Special list page style
### Fixed
- Special page links

## 0.9.10-preview
### Fixed
- Edit page style
### Updated
- Update dependencies

## 0.9.9-preview
### Fixed
- Edit code behind

## 0.9.8-preview
### Changed
- Improve configuration

## 0.9.7-preview
### Updated
- Update dependencies

## 0.9.6-preview
### Changed
- Improve image style

## 0.9.4-5-preview
### Updated
- Update dependencies

## 0.9.3-preview
### Added
- Additional logging

## 0.9.1-2-preview
### Updated
- Update dependencies

## 0.9.0-preview
### Changed
- Implement new Tavenem.Wiki version
- Reduce dependence on interactive rendering

## 0.8.0-preview
### Changed
- Implement built-in search
- Replaced 3rd party emoji popup

## 0.7.8-preview
### Fixed
- Decode wiki route

## 0.7.5-7-preview
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