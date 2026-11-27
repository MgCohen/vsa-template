namespace CodeCheck.Slices.CheckCode;

using Facts.Domain.Shared.Compile;

public sealed class CommentRule : KernelRule {
    static readonly string[] Kinds = [
        "SingleLineCommentTrivia", "MultiLineCommentTrivia", "SingleLineDocumentationCommentTrivia", "MultiLineDocumentationCommentTrivia",
    ];

    public override int Number => 6;

    public override string Title => "The code has no comments";

    public override string Message =>
        "The code has a comment. Remove it. Put the information in a name: a type, a member, a variable or a test.";

    public override IEnumerable<CodeFinding> Check(CodeFile file, RuleOptions options) =>
        file.Comments.Where(comment => Kinds.Contains(comment.Kind)).Select(comment => Found(comment.Place));
}
