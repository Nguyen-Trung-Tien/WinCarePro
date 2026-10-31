# 📝 Minimal-Anchor Diff Patching Examples

## ❌ Bad: Wasteful 40-Line Anchor (Dilutes Context & Wastes Tokens)
```csharp
// Overly large TargetContent block that dumps 30 lines of unmodified code
public class OldImplementation
{
    private int _a;
    private int _b;
    // ... 25 lines of unmodified getters and setters ...
    public void TargetMethod()
    {
        OldCode();
    }
}
```

## ✅ Good: 2 to 4 Line Unique Anchor (Saves 90% Tokens)
```csharp
// TargetContent:
    public void TargetMethod()
    {
        OldCode();

// ReplacementContent:
    public void TargetMethod()
    {
        NewOptimizedCode();
```

