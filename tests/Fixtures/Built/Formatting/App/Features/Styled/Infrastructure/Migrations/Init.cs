// EF Core writes these files. They break the rules, but the build gives no warning for them.
namespace Styled.Infrastructure.Migrations
{
    public sealed class Init
    {
        public const string Text = "This text makes the line longer than the limit of one hundred and twenty characters for each line.";
    }

    public sealed class InitSnapshot
    {
    }
}
