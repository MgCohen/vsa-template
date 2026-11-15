namespace Umpire.Cli;

// An error for the user. The tool shows the message and stops with exit code 1.
sealed class UmpireException(string message) : Exception(message);
