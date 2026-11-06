// IDE0161: a block-scoped namespace.
namespace Styled.Infrastructure
{
    public static class BuiltInRules
    {
        // IDE0040: a member with no access modifier.
        static int Value = 1;


        // IDE2000: two blank lines in a row are above this line.
        // IDE0055: the brace of the method is on the line of the method, not on its own line.
        public static int Next() {
            // IDE0008: var for a built-in type, not int.
            var next = Value + 1;
            // IDE0011: an if with no braces.
            if (next > 10) next = 0;
            Value = next;
            return next;
        }
    }
}
