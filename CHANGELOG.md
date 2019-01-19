# Changelog
This project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html). Refer to 
[The Semantic Versioning Lifecycle](https://www.jering.tech/articles/the-semantic-versioning-lifecycle)
for an overview of semantic versioning.

## [Unreleased](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.9.0...HEAD)

## [0.9.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.8.0...0.9.0) - Jan 19, 2019
### Changes
- Bumped dependencies.
### Fixes
- `SnippetCreator` now corrects hash hrefs for use in snippets.

## [0.8.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.7.0...0.8.0) - Jan 7, 2019
### Additions
- Added `Presets` `IDocumentBuildStep`. Convenience plugin that expands options.
### Fixes
- Fixed `FileMetadataExposer` file name.

## [0.7.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.6.0...0.7.0) - Jan 2, 2019
### Changes
- Bumped `Jering.Markdig.Extensions.FlexiBlocks` to 0.14.0.
### Additions
- Added ExplicitParagraphsExtension to MimoMarkdown.
- Added mimo_toc option to TocEmbedder to allow for overriding of a page's TOC.
### Fixes
- Fixed FlexiIncludeBlocksExtensionOptions.RootBaseUri being set to the wrong value.

## [0.6.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.5.0...0.6.0) - Dec 7, 2018 
### Changes
- Bumped `Jering.Markdig.Extensions.FlexiBlocks` to 0.13.0.

## [0.5.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.4.0...0.5.0) - Nov 17, 2018 
### Changes
- TocEmbedder now leaves making URLs relative to AbsolutePathResolver.
### Additions
- AbsolutePathResolver now makes relative URLs in 404.html absolute.

## [0.4.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.3.0...0.4.0) - Nov 13, 2018 
### Changes
- TocEmbedder now removes unused meta tags.
- Improved TocEmbedder.CleanHrefs. Now uses `URI` instead of plain string manipulation.
- SnippetCreator now removes .html suffixes in URLs.
### Additions
- Added ExternalAnchorFixer.

## [0.3.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.2.0...0.3.0) - Nov 7, 2018 
### Changes
- Bumped DocFx dependencies to a version that omits .html suffixes from sitemap loc elements.
### Additions
- Added AbsolutePathResolver.
### Fixes
- Fixed TocEmbedder sometimes not assigning active class to navbar anchor.

## [0.2.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.1.0...0.2.0) - Nov 6, 2018 
### Fixes
- Fixed TocEmbedder throwing null reference exception if category menu has no expandable nodes.
- Fixed TocEmbedder not cleaning hrefs properly.

## [0.1.0](https://github.com/JeremyTCD/DocFx.Plugins/compare/0.1.0...0.1.0) - Oct 30, 2018 
Initial release.