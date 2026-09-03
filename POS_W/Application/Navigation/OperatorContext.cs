namespace POS_W.Application.Navigation;

public sealed record OperatorContext(
    string Username,
    string DisplayName,
    string Role,
    string Establishment,
    string Turn);
