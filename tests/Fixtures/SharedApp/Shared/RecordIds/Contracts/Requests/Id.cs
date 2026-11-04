namespace RecordIds.Contracts.Requests;

// A Shared base type that another Shared module (Pipeline) uses.
public abstract record Id(long Value);
