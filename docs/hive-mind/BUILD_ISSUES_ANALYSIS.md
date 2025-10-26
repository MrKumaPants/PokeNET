# Build Issues Analysis - MonoGame in Domain Layer

**Date**: 2025-10-26
**Status**: ⚠️ Architectural violations exposed after removing MonoGame from Domain

---

## ✅ **Issues Fixed**

### 1. Removed MonoGame Reference from Domain (C-2 Completed)
**File**: `PokeNET.Domain/PokeNET.Domain.csproj`
- ❌ **Before**: Had `MonoGame.Framework.DesktopGL` reference
- ✅ **After**: Removed - Domain is now platform-independent

### 2. Added Arch.System Source Generator to Core
**File**: `PokeNET.Core/PokeNET.Core.csproj`
- ✅ Added `Arch.System` v1.1.0
- ✅ Added `Arch.System.SourceGenerator` v2.1.0
- ✅ Added `NoWarn` for CS8602 (nullable warnings) and CS0436 (type conflicts)

### 3. Pinned Arch Version in Core
- ❌ **Before**: `Arch` Version="2.*" (wildcard)
- ✅ **After**: `Arch` Version="2.1.0" (pinned)

---

## ❌ **39 Build Errors Remaining**

Removing MonoGame from Domain **correctly exposed** architectural violations:

### **Files Using MonoGame Types in Domain Layer**

#### 1️⃣ **Persistence Formatters** (16 errors)

**ColorFormatter.cs** (4 errors):
```csharp
// ❌ Uses Microsoft.Xna.Framework.Color
public override void Serialize(ref MessagePackWriter writer, Color value, MessagePackSerializerOptions options)
```

**RectangleFormatter.cs** (6 errors):
```csharp
// ❌ Uses Microsoft.Xna.Framework.Rectangle
public override void Serialize(ref MessagePackWriter writer, Rectangle value, MessagePackSerializerOptions options)
```

**Vector2Formatter.cs** (7 errors):
```csharp
// ❌ Uses Microsoft.Xna.Framework.Vector2
public override void Serialize(ref MessagePackWriter writer, Vector2 value, MessagePackSerializerOptions options)
```

#### 2️⃣ **InputSystem** (23 errors)

**InputSystem.cs** (23 errors):
```csharp
// ❌ Uses MonoGame input types
private KeyboardState _currentKeyboardState;
private KeyboardState _previousKeyboardState;
private GamePadState _currentGamePadState;
private GamePadState _previousGamePadState;
private MouseState _currentMouseState;
private MouseState _previousMouseState;

private bool IsKeyPressed(Keys key)
private bool IsKeyJustPressed(Keys key)
private bool IsButtonPressed(Buttons button)
```

---

## 🔧 **Required Fixes**

### **Option 1: Move Files to Core (RECOMMENDED)**

Move platform-specific files from Domain → Core:

```
Domain/ECS/Persistence/Formatters/
├── ColorFormatter.cs         → Core/Persistence/Formatters/
├── RectangleFormatter.cs     → Core/Persistence/Formatters/
└── Vector2Formatter.cs       → Core/Persistence/Formatters/

Domain/ECS/Systems/
└── InputSystem.cs            → Core/ECS/Systems/
```

**Rationale**: These files are **infrastructure**, not domain logic.

### **Option 2: Create Domain Abstractions (COMPLEX)**

Create pure C# types in Domain and map in Core:

```csharp
// Domain/ValueObjects/Color.cs
public readonly struct Color
{
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }
    public byte A { get; init; }
}

// Core/Mapping/ColorMapper.cs
public static class ColorMapper
{
    public static Microsoft.Xna.Framework.Color ToMonoGame(this Domain.Color color)
        => new(color.R, color.G, color.B, color.A);
}
```

**Rationale**: Maintains pure domain but adds complexity.

---

## 📋 **Recommended Action Plan**

### **Phase 1: Move InputSystem** (15 minutes)

1. **Move file**: `Domain/ECS/Systems/InputSystem.cs` → `Core/ECS/Systems/InputSystem.cs`
2. **Update namespace**: `PokeNET.Domain.ECS.Systems` → `PokeNET.Core.ECS.Systems`
3. **Keep interface**: Leave `IInputSystem` in Domain if needed
4. **Update DI**: Fix registration in `Program.cs`

**Impact**: Fixes 23 errors immediately

### **Phase 2: Move Persistence Formatters** (10 minutes)

1. **Create directory**: `Core/Persistence/Formatters/`
2. **Move files**:
   - `ColorFormatter.cs`
   - `RectangleFormatter.cs`
   - `Vector2Formatter.cs`
3. **Update namespaces**: `PokeNET.Domain.ECS.Persistence.Formatters` → `PokeNET.Core.Persistence.Formatters`

**Impact**: Fixes 16 errors

### **Phase 3: Verify Build** (5 minutes)

```bash
dotnet clean
dotnet build
```

**Expected**: ✅ 0 errors

---

## 🎯 **Why This Is Correct**

### **Domain Layer Should Contain:**
✅ Entities, Value Objects
✅ Domain Events
✅ Business Rules
✅ Pure interfaces

### **Core/Infrastructure Layer Should Contain:**
✅ ECS Systems (platform-specific)
✅ Persistence (serialization)
✅ Input adapters (MonoGame → Commands)
✅ Rendering (graphics)

### **What We Had (WRONG):**
❌ InputSystem in Domain (uses MonoGame types)
❌ Formatters in Domain (uses MonoGame types)
❌ MonoGame reference in Domain

### **What We Should Have (CORRECT):**
✅ Domain: Pure C# (no platform dependencies)
✅ Core: MonoGame integration (systems, formatters, input)
✅ Clean separation of concerns

---

## 📊 **Before vs After**

### **Before (Hidden Violations)**
```
Domain.csproj:
  <PackageReference Include="MonoGame..." />  ❌ WRONG

Domain/ECS/Systems/InputSystem.cs:
  using Microsoft.Xna.Framework.Input;       ❌ WRONG

Build: ✅ Compiles (violations hidden)
```

### **After (Violations Exposed)**
```
Domain.csproj:
  <!-- MonoGame removed -->                   ✅ CORRECT

Domain/ECS/Systems/InputSystem.cs:
  using Microsoft.Xna.Framework.Input;       ❌ ERROR CS0246

Build: ❌ 39 errors (violations exposed)
```

### **Target (Fixed Architecture)**
```
Domain.csproj:
  <!-- Pure C#, no platform deps -->          ✅ CORRECT

Core/ECS/Systems/InputSystem.cs:
  using Microsoft.Xna.Framework.Input;       ✅ CORRECT

Build: ✅ 0 errors
```

---

## 🚀 **Next Steps**

1. **Execute Phase 1**: Move InputSystem to Core
2. **Execute Phase 2**: Move Formatters to Core
3. **Execute Phase 3**: Verify build succeeds
4. **Update Documentation**: Record architecture decision

**Estimated Time**: 30 minutes
**Complexity**: Low (simple file moves)
**Risk**: Low (clear violations, clear fix)

---

## 📝 **Architecture Decision Record**

### **ADR-006: Platform-Specific Code Belongs in Infrastructure**

**Context**: Domain layer contained MonoGame-dependent code (InputSystem, formatters)

**Decision**: Move all platform-specific code from Domain → Core

**Consequences**:
- ✅ Domain is now pure C# (testable, portable)
- ✅ Clean architecture boundaries enforced
- ✅ Violations are caught at compile-time
- ⚠️ Requires file moves and namespace updates
- ⚠️ May break existing code referencing Domain.InputSystem

**Status**: Accepted

---

**Last Updated**: 2025-10-26
**Created by**: Hive Mind Queen Coordinator
**Next Action**: Execute file moves to fix build
