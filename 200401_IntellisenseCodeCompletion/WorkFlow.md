
# Work Flow

Open File:
IVsTextViewCreationListener -> VsTextViewCreated -> 

class CommandFilter : IOleCommandTarget

When typing anything:
Exec -> Check type-in is blank space -> StartSession(); -> _currentSession.Start(); ->

OokCompletionSourceProvider : ICompletionSourceProvider -> OokCompletionSource : ICompletionSource =>

AugmentCompletionSession(Triggered by every type-in) -> Make a List<Completion> completions -> Added completionSets.Add(new CompletionSet)

==> Display the full List of completions







