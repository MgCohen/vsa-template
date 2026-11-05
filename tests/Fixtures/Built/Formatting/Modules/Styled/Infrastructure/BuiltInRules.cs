// IDE0161: a block-scoped namespace.
namespace Styled.Infrastructure
{
    // IDE0055: the brace of the class is on its own line.
    public static class BuiltInRules
    {
        // IDE0040: private is the default access modifier.
        private static int Value = 1;


        // IDE2000: two blank lines in a row are above this line.
        public static int Next() {
            // IDE0007: an explicit type, not var.
            int next = Value + 1;
            // IDE0011: an if with no braces.
            if (next > 10) next = 0;
            Value = next;
            return next;
        }
    }
}
