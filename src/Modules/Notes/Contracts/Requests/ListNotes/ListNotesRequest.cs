using VSA.Contracts.Requests;

namespace Notes.Contracts.Requests.ListNotes;

[HttpRoute(Http.Get, "/api/notes")]
public sealed record ListNotesRequest : Request<ListNotesResponse>;
