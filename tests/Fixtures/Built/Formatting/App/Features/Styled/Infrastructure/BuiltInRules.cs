namespace Styled.Infrastructure
{
    public static class BuiltInRules
    {
        // IDE0040 (relaxed until 0.1.0, no warning): a member with no access modifier.
        static int Value = 1;


        // IDE2000: two blank lines in a row are above this line.
        // IDE0055 (relaxed until 0.1.0): the braces of the class are on their own lines, not on the line of the class.
        public static int Next() {
            // IDE0008 (relaxed until 0.1.0, no warning): var for a built-in type, not int.
            var next = Value + 1;
            // IDE0011: an if with no braces.
            if (next > 10) next = 0;
            Value = next;
            return next;
        }
    }
}
