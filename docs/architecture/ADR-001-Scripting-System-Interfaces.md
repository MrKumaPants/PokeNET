# ADR-001: Scripting System Interface Design

**Status:** Accepted
**Date:** 2025-10-22
**Decision Makers:** System Architecture Designer
**Category:** Architecture

---

## Context and Problem Statement

Phase 5 of PokeNET requires a robust scripting system to enable mod support and dynamic gameplay features. The system must be:
- **Secure**: Scripts cannot compromise game integrity or access unauthorized resources
- **Performant**: Minimal overhead for script execution
- **Extensible**: Support for multiple scripting languages and mod frameworks
- **Maintainable**: Clear separation of concerns and testability

The core architectural challenge is designing interfaces that provide sufficient power for mod authors while maintaining security boundaries and following SOLID principles.

---

## Decision Drivers

1. **SOLID Principles Compliance**: All interfaces must demonstrate clear adherence to SOLID
2. **Security First**: Scripts must be sandboxed with no direct access to ECS internals
3. **Dependency Injection**: Full DI support for testability and extensibility
4. **Interface Segregation**: Small, focused interfaces over monolithic ones
5. **Future-Proofing**: Design must accommodate Lua, JavaScript, or other scripting languages
6. **Performance**: Compiled script caching and minimal abstraction overhead
7. **Developer Experience**: Clear, well-documented APIs with comprehensive XML docs

---

## Considered Options

### Option 1: Monolithic Scripting API
**Pros:**
- Simple to understand for mod developers
- Single entry point for all operations
- Easier to implement initially

**Cons:**
- ❌ Violates Interface Segregation Principle
- ❌ Tight coupling between unrelated concerns
- ❌ Difficult to test individual components
- ❌ Forces all implementations to support all features

### Option 2: Segregated Interface Architecture (CHOSEN)
**Pros:**
- ✅ Full SOLID compliance
- ✅ Each interface has single, well-defined responsibility
- ✅ Easy to mock for testing
- ✅ Extensible through composition
- ✅ Clear security boundaries

**Cons:**
- More interfaces to learn (mitigated by excellent documentation)
- Slightly more complex implementation (worth the benefits)

### Option 3: Direct ECS Access
**Pros:**
- Maximum performance (zero abstraction)
- Full power for mod developers

**Cons:**
- ❌ Catastrophic security risk
- ❌ Scripts can break ECS invariants
- ❌ No sandboxing or resource limits
- ❌ Rejected immediately

---

## Decision Outcome

**Chosen Option: Segregated Interface Architecture**

We will implement four primary interfaces following strict SOLID principles:

### 1. IScriptingEngine - Script Lifecycle Management
**Responsibility:** Compilation, loading, and execution of scripts
**SOLID Alignment:**
- **SRP:** Only handles script lifecycle, not content or discovery
- **OCP:** New script languages added via new implementations
- **LSP:** All engine implementations are substitutable
- **ISP:** Focused interface, no unused methods
- **DIP:** Depends on IScriptContext and IScriptLoader abstractions

**Key Features:**
- Async compilation with timeout protection
- Compilation result caching for performance
- Hot-reload support via cache invalidation
- Function-level execution for callbacks
- Comprehensive diagnostics

### 2. IScriptContext - Execution Context & DI
**Responsibility:** Provides service access and execution environment
**SOLID Alignment:**
- **SRP:** Only manages service access and context state
- **DIP:** Scripts depend on abstraction, not concrete services
- **OCP:** New services registered without interface changes

**Key Features:**
- Dependency injection service locator pattern
- Script-scoped data storage (per-execution state)
- Shared data storage (inter-script communication)
- Scoped logging (IScriptLogger)
- Cleanup callback registration
- Metadata access (game version, mod context)

### 3. IScriptApi - Safe ECS World Interaction
**Responsibility:** Controlled access to game world and ECS
**SOLID Alignment:**
- **ISP:** Segregated into focused sub-APIs (IEntityApi, IComponentApi, IEventApi, etc.)
- **SRP:** Each sub-API has single concern
- **OCP:** New APIs added through composition

**Key Features:**
- **IEntityApi:** Entity creation, destruction, naming, cloning
- **IComponentApi:** Type-safe component manipulation
- **IEventApi:** Event-driven scripting with publish/subscribe
- **IQueryApi:** Fluent ECS query builder
- **IResourceApi:** Singleton resource access

**Security Model:**
- Scripts cannot access arbitrary .NET APIs
- All operations validated and sandboxed
- Queries are bounded to prevent performance issues
- Resource limits enforced (memory, CPU time)

### 4. IScriptLoader - Script Discovery & Loading
**Responsibility:** Finding and loading scripts from various sources
**SOLID Alignment:**
- **SRP:** Only handles discovery and retrieval
- **OCP:** New sources added via implementations
- **DIP:** Engine depends on abstraction, not file system

**Key Features:**
- Multi-source support (file system, mods, embedded, network)
- Priority-based loader ordering (mod overrides)
- File watching for hot-reload
- Metadata extraction
- Discovery and enumeration

---

## Architecture Diagram (C4 Model - Component Level)

```
┌─────────────────────────────────────────────────────────────┐
│                    Scripting System                          │
│                                                              │
│  ┌────────────────┐      ┌────────────────┐                │
│  │ IScriptLoader  │─────▶│IScriptingEngine│                │
│  │                │      │                │                │
│  │ - FileSystem   │      │ - Compile      │                │
│  │ - ModLoader    │      │ - Execute      │                │
│  │ - Embedded     │      │ - Cache        │                │
│  └────────────────┘      └───────┬────────┘                │
│                                   │                          │
│                                   │ uses                     │
│                                   ▼                          │
│                          ┌────────────────┐                 │
│                          │ IScriptContext │                 │
│                          │                │                 │
│                          │ - DI Access    │                 │
│                          │ - Data Storage │                 │
│                          │ - Logger       │                 │
│                          └───────┬────────┘                 │
│                                  │                           │
│                                  │ provides                  │
│                                  ▼                           │
│                          ┌────────────────┐                 │
│                          │  IScriptApi    │                 │
│                          │                │                 │
│                          │ ┌────────────┐ │                 │
│                          │ │IEntityApi  │ │                 │
│                          │ │IComponentApi│ │                 │
│                          │ │IEventApi   │ │                 │
│                          │ │IQueryApi   │ │                 │
│                          │ │IResourceApi│ │                 │
│                          │ └────────────┘ │                 │
│                          └───────┬────────┘                 │
│                                  │                           │
│                                  │ safe access               │
│                                  ▼                           │
│                          ┌────────────────┐                 │
│                          │   ECS World    │                 │
│                          │                │                 │
│                          │ - Entities     │                 │
│                          │ - Components   │                 │
│                          │ - Systems      │                 │
│                          └────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

---

## Consequences

### Positive
✅ **Full SOLID Compliance**: All interfaces follow all five principles
✅ **Security**: Scripts are fully sandboxed with no direct ECS access
✅ **Testability**: Each interface easily mocked for unit testing
✅ **Extensibility**: New features added without modifying existing interfaces
✅ **Performance**: Compilation caching and minimal abstraction overhead
✅ **Documentation**: Comprehensive XML docs with examples and architectural notes
✅ **Future-Proof**: Design accommodates multiple scripting languages

### Negative
⚠️ **Learning Curve**: Mod developers must learn four interfaces (mitigated by docs)
⚠️ **Implementation Complexity**: More interfaces require more implementation code
⚠️ **Abstraction Overhead**: Small performance cost vs. direct access (acceptable trade-off)

### Neutral
📊 **Dependency Management**: DI container must be properly configured
📊 **Error Handling**: Comprehensive exception hierarchy required
📊 **Documentation Maintenance**: XML docs must stay synchronized with code

---

## Implementation Notes

### Phase 5 Implementation Order
1. ✅ **Interfaces** (this ADR) - Complete
2. **Exception Types** - ScriptExecutionException, ScriptLoadException, etc.
3. **Roslyn Implementation** - C# scripting engine using Microsoft.CodeAnalysis.CSharp.Scripting
4. **File System Loader** - Basic script discovery and loading
5. **Script Context** - DI integration and API provisioning
6. **Script API** - Safe ECS interaction layer
7. **Testing** - Comprehensive unit and integration tests
8. **Documentation** - Usage guides and examples

### Security Considerations
- Scripts execute in isolated AppDomains (if using .NET Framework) or AssemblyLoadContext (if using .NET Core+)
- Resource limits enforced via CancellationToken timeouts
- API surface is whitelist-based (only explicitly registered services accessible)
- Component access validated before allowing manipulation
- Query result counts bounded to prevent memory exhaustion

### Performance Optimizations
- Compilation results cached with SHA-256 source hashing
- Hot-reload via cache invalidation (no restart required)
- Async execution to prevent blocking game loop
- Reusable ICompiledScript instances (thread-safe)
- Query builder optimized for common patterns

---

## Related Decisions
- ADR-002: Roslyn Scripting Engine Implementation (pending)
- ADR-003: Mod Loading System Architecture (pending)
- ADR-004: Script Security Sandbox Design (pending)

---

## References
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/)
- [Interface Segregation Principle](https://en.wikipedia.org/wiki/Interface_segregation_principle)
- [Dependency Inversion Principle](https://en.wikipedia.org/wiki/Dependency_inversion_principle)

---

**Approval:** Accepted by System Architecture Designer on 2025-10-22
