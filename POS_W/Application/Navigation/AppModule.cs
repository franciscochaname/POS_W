namespace POS_W.Application.Navigation;

public sealed record AppModule(
    string Route,
    string Title,
    string Icon,
    string Area,
    string Description,
    string[] AllowedRoles,
    string[] RelatedTables);
