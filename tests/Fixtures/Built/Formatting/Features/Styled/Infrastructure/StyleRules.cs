namespace Styled.Infrastructure;

// IDE0065: a using directive inside the namespace.
using System;

// IDE1006: an interface name without the I prefix.
internal interface Clock
{
    DateTime Now { get; }
}

internal sealed class StyleRules
{
    // IDE1006: a constant in camelCase.
    private const int maxItems = 10;

    // IDE0036: static is before private.
    static private int total;

    // IDE1006: a private field in PascalCase.
    private int Count;

    // IDE0021: a constructor with an expression body.
    public StyleRules() => Count = 0;

    public event EventHandler? Changed;

    // IDE0025: a property with a block body that fits on one line.
    public int Size { get { return Count; } }

    // IDE0027: accessors with block bodies that fit on one line.
    public int Value { get { return Count; } set { Count = value; } }

    // IDE0026: an indexer with a block body that fits on one line.
    public int this[int index] { get { return index + Count; } }

    // IDE0024: an operator with a block body that fits on one line.
    public static StyleRules operator +(StyleRules left, StyleRules right) { return left; }

    // IDE0023: a conversion operator with a block body that fits on one line.
    public static implicit operator int(StyleRules rules) { return rules.Count; }

    // IDE0022: a method with an expression body.
    public int Next() => ++Count;

    // IDE0049 (editor only, the build does not report it): Int32, not int.
    public Int32 Add(object value)
    {
        // IDE0054: count = count + 1, not count += 1.
        Count = Count + 1;
        total = total + maxItems;
        // IDE0083: !(x is T), not x is not T.
        if (!(value is string))
        {
            return Count;
        }
        // IDE1005: a null check and a call, not Changed?.Invoke.
        if (Changed != null)
        {
            Changed(this, EventArgs.Empty);
        }
        // IDE0053 (editor only, the build does not report it): a lambda with a block body that fits on one line.
        Func<int, int> twice = x => { return x * 2; };
        // IDE0061: a local function with an expression body.
        int Half(int x) => x / 2;
        return twice(Half(Count));
    }

    // IDE2002: a blank line between two closing braces.
    public void Reset()
    {
        if (Count > maxItems)
        {
            Count = 0;
        }

    }
}
