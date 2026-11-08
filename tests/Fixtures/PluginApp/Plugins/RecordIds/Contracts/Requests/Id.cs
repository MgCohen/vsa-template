namespace RecordIds.Contracts.Requests;

// A plugin base type that another plugin module (Pipeline) uses. It is in the Contracts/Requests folder of RecordIds,
// so VSA.Sdk compiles it into RecordIds.Contracts.Requests.
public abstract record Id(long Value);
