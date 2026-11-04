namespace RecordIds.Ids;

// A Shared base type that another Shared module (Pipeline) uses. No contract of RecordIds uses it,
// so the router puts it in Contracts/Requests, because RecordIds is a Shared module.
public abstract record Id(long Value);
