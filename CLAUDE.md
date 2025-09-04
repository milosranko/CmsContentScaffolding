# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CmsContentScaffolding is a .NET library for scaffolding content in CMS platforms, supporting both Optimizely CMS and Piranha CMS. It allows developers to create thousands of pages, blocks, and media assets with any structure using just a few lines of code, primarily for unit testing and development purposes.

## Solution Structure

The solution consists of 5 projects:
- `CmsContentScaffolding.Optimizely` - Main library for Optimizely CMS (.NET 6.0)
- `CmsContentScaffolding.Piranha` - Main library for Piranha CMS (.NET 8.0)
- `CmsContentScaffolding.Shared.Resources` - Shared utilities and resources
- `CmsContentScaffolding.Optimizely.Tests` - Unit tests for Optimizely implementation
- `CmsContentScaffolding.Piranha.Tests` - Unit tests for Piranha implementation

## Build Commands

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build CmsContentScaffolding.Optimizely/CmsContentScaffolding.Optimizely.csproj
dotnet build CmsContentScaffolding.Piranha/CmsContentScaffolding.Piranha.csproj

# Build in Release mode
dotnet build -c Release
```

## Test Commands

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test CmsContentScaffolding.Tests/CmsContentScaffolding.Optimizely.Tests.csproj
dotnet test CmsContentScaffolding.Piranha.Tests/CmsContentScaffolding.Piranha.Tests.csproj

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Package Commands

```bash
# Create NuGet packages (already configured with GeneratePackageOnBuild)
dotnet build -c Release

# Pack specific project
dotnet pack CmsContentScaffolding.Optimizely/CmsContentScaffolding.Optimizely.csproj -c Release
dotnet pack CmsContentScaffolding.Piranha/CmsContentScaffolding.Piranha.csproj -c Release
```

## Architecture

### Core Components

1. **Builder Pattern Implementation**
   - `IContentBuilder` / `ContentBuilder` - Main interface for content construction
   - `IPagesBuilder` / `PagesBuilder` - Handles page hierarchy creation
   - `IAssetsBuilder` / `AssetsBuilder` - Manages blocks, media, and folders
   - `IContentBuilderManager` / `ContentBuilderManager` - Orchestrates the building process

2. **Extension Points**
   - `StartupExtensions` - Provides `AddCmsContentScaffolding()` and `UseCmsContentScaffolding()` methods
   - `PropertyExtensions` - Extensions for property manipulation
   - `PropertyHelpers` - Utility methods for content properties

3. **Configuration Models**
   - `ContentBuilderOptions` - Configuration for build mode, site settings, users, roles
   - `BuildMode` enum - Supports `Append` and `Replace` modes
   - `UserModel` - User creation during scaffolding

### Key Features

- **Fluent API** for content creation using builder pattern
- **Multi-language support** with culture-specific content variants
- **Hierarchical structure** support for pages and assets
- **Bulk operations** with `WithPages<T>()` methods for creating multiple items
- **Reference management** with `out` parameters for content references
- **Media handling** with stream-based file creation
- **User and role management** for access control setup

### Usage Pattern

The library follows a consistent pattern:
1. Configure options (site name, host, language, build mode)
2. Use fluent builders to create content structure
3. Capture references for cross-linking content
4. Support for both page hierarchy and asset organization

## Development Notes

- Projects target different .NET versions: Optimizely uses .NET 6.0, Piranha uses .NET 8.0
- Both libraries are configured to generate NuGet packages on build
- Package validation is enabled to ensure API compatibility
- Warning level is set to 8 for comprehensive code analysis
- Test projects use MSTest framework with appsettings.unittest.json configuration