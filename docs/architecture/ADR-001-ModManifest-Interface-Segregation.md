# ADR-001: Interface Segregation for IModManifest

## Status
✅ Accepted and Implemented (2025-10-23)

## Context

The `IModManifest` interface had grown to 25 properties covering diverse responsibilities:
- Core identity (Id, Name, Version)
- Descriptive metadata (Author, Description, Tags)
- Dependency management (Dependencies, LoadAfter, LoadBefore)
- Code execution (EntryPoint, Assemblies, HarmonyId)
- Asset loading (AssetPaths, Preload)
- Security (TrustLevel, Checksum, ContentRating)

This violated the Interface Segregation Principle (ISP), forcing all consumers to depend on properties they didn't use, leading to:
- **Tight coupling**: UI components depending on security properties
- **Testing complexity**: Mocking 25 properties for simple tests
- **Unclear intent**: Method signatures don't reveal which properties are used
- **Maintenance burden**: Changes affect unrelated components

## Decision

Split `IModManifest` into 6 focused interfaces based on responsibility:

### 1. IModManifestCore (4 properties)
**Responsibility:** Essential mod identification
- Required by: All components
- Properties: Id, Name, Version, ApiVersion

### 2. IModMetadata (7 properties)
**Responsibility:** Descriptive information for UI/documentation
- Required by: Mod browsers, UI components, documentation generators
- Properties: Author, Description, Homepage, License, Tags, Localization, Metadata

### 3. IModDependencies (4 properties)
**Responsibility:** Load order and dependency management
- Required by: ModLoader, dependency resolver
- Properties: Dependencies, LoadAfter, LoadBefore, IncompatibleWith

### 4. ICodeMod (4 properties)
**Responsibility:** Code execution and assembly loading
- Required by: Assembly loader, Harmony patcher
- Properties: ModType, EntryPoint, Assemblies, HarmonyId

### 5. IContentMod (2 properties)
**Responsibility:** Asset and content management
- Required by: Asset loader, content preloader
- Properties: AssetPaths, Preload

### 6. IModSecurity (3 properties)
**Responsibility:** Security and trust verification
- Required by: Security system, integrity checker
- Properties: TrustLevel, Checksum, ContentRating

### Combined Interface (Backwards Compatibility)
```csharp
public interface IModManifest : IModManifestCore, IModMetadata,
    IModDependencies, ICodeMod, IContentMod, IModSecurity
{
    string Directory { get; } // Runtime property
}
```

## Rationale

### Why This Approach?

1. **ISP Compliance**: Each interface has a single, cohesive responsibility
2. **Loose Coupling**: Components depend only on what they need
3. **Clear Intent**: Method signatures reveal dependencies
4. **Testability**: Mock only relevant properties
5. **Backwards Compatible**: Existing code continues to work

### Why Not Alternatives?

#### Alternative 1: Keep Monolithic Interface
- ❌ Continues to violate ISP
- ❌ Maintains tight coupling
- ❌ Doesn't improve testability

#### Alternative 2: Create Separate Classes
- ❌ Would require breaking changes
- ❌ Mod authors would need to update manifests
- ❌ More complex serialization

#### Alternative 3: Use Abstract Base Class
- ❌ Forces single inheritance
- ❌ Less flexible than interfaces
- ❌ Harder to compose

## Consequences

### Positive

1. **✅ ISP Compliance**: No forced dependencies on unused properties
2. **✅ Better Testability**: Focused mocking (56-92% reduction in mock complexity)
3. **✅ Clear Dependencies**: Method signatures reveal intent
4. **✅ Flexible Implementations**: Can implement subsets of capabilities
5. **✅ Easier Maintenance**: Changes isolated to relevant interfaces
6. **✅ Zero Breaking Changes**: 100% backwards compatible

### Negative

1. **⚠️ More Interfaces**: 1 → 7 interfaces (manageable complexity)
2. **⚠️ Migration Effort**: Optional refactoring for internal code (low priority)

### Neutral

1. **→ Performance**: No runtime impact (compile-time only)
2. **→ Serialization**: No changes to JSON format
3. **→ Mod Authors**: No changes required

## Implementation

### Files Created
```
PokeNET.Domain/Modding/
├── IModManifestCore.cs      (1.2 KB, 4 properties)
├── IModMetadata.cs          (1.4 KB, 7 properties)
├── IModDependencies.cs      (1.2 KB, 4 properties)
├── ICodeMod.cs              (1.1 KB, 4 properties)
├── IContentMod.cs           (0.8 KB, 2 properties)
├── IModSecurity.cs          (0.8 KB, 3 properties)
└── IModManifest.cs          (updated, now inherits from all)

PokeNET.Core/Modding/
└── ModManifest.cs           (updated, organized by interface)

tests/Modding/
└── ModManifestInterfaceSegregationTests.cs (11 tests)

docs/architecture/
├── ModManifest-ISP-Migration-Guide.md
├── ISP-Fix-Summary.md
└── ADR-001-ModManifest-Interface-Segregation.md
```

### Test Coverage
- ✅ 11 comprehensive tests
- ✅ Interface implementation validation
- ✅ Property count verification
- ✅ Usage pattern examples
- ✅ Backwards compatibility verification

## Examples

### Before (ISP Violation)
```csharp
// ❌ UI component forced to depend on 25 properties
void DisplayModCard(IModManifest manifest)
{
    UI.Title = manifest.Name;
    UI.Author = manifest.Author;
    // Only uses 2-3 properties but depends on all 25
}

// ❌ Must mock 25 properties for simple test
var mock = new Mock<IModManifest>();
mock.Setup(m => m.Id).Returns("test");
mock.Setup(m => m.Name).Returns("Test");
mock.Setup(m => m.Version).Returns(version);
mock.Setup(m => m.ApiVersion).Returns("1.0.0");
mock.Setup(m => m.Author).Returns("Author");
// ... 20 more setups
```

### After (ISP Compliant)
```csharp
// ✅ UI component depends only on what it needs
void DisplayModCard(IModManifestCore core, IModMetadata metadata)
{
    UI.Title = core.Name;
    UI.Author = metadata.Author;
    // Clear dependencies: 4 + 7 = 11 properties max
}

// ✅ Mock only what's needed
var mockCore = new Mock<IModManifestCore>();
mockCore.Setup(m => m.Name).Returns("Test");

var mockMeta = new Mock<IModMetadata>();
mockMeta.Setup(m => m.Author).Returns("Author");

DisplayModCard(mockCore.Object, mockMeta.Object);
```

## Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Coupling (UI) | 25 properties | 11 properties | **56% reduction** |
| Coupling (Loader) | 25 properties | 8 properties | **68% reduction** |
| Coupling (Security) | 25 properties | 7 properties | **72% reduction** |
| Test Mock Complexity | 25 setups | 2-11 setups | **56-92% reduction** |
| Interface Size | 25 members | 2-7 members | **72-92% reduction** |

## Migration Guide

See [ModManifest-ISP-Migration-Guide.md](./ModManifest-ISP-Migration-Guide.md) for:
- Detailed interface descriptions
- Usage examples
- Best practices
- Common patterns
- Testing strategies

## Related Principles

This ADR implements:
- ✅ **Interface Segregation Principle (ISP)** - Primary goal
- ✅ **Single Responsibility Principle (SRP)** - Each interface has one responsibility
- ✅ **Dependency Inversion Principle (DIP)** - Depend on abstractions
- ✅ **Open/Closed Principle (OCP)** - Open for extension via composition

## Future Considerations

1. **Internal Refactoring**: Gradually update internal framework code to use focused interfaces
2. **Similar Refactoring**: Consider ISP for other large interfaces (e.g., `IModContext`)
3. **Documentation**: Update mod development tutorials with ISP examples
4. **Tooling**: Create analyzers to detect ISP violations

## References

- [Interface Segregation Principle](https://en.wikipedia.org/wiki/Interface_segregation_principle)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- Martin, Robert C. "Agile Software Development: Principles, Patterns, and Practices"

## Approval

- **Architect**: System Architecture Designer ✅
- **Date**: 2025-10-23
- **Status**: Implemented and Tested
- **Breaking Changes**: None
- **Backwards Compatible**: Yes ✅
