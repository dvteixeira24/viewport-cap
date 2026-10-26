namespace ViewportCap.Core;

public sealed class RecordingStateMachine
{
    private static readonly IReadOnlyDictionary<RecordingState, RecordingState[]> Allowed = new Dictionary<RecordingState, RecordingState[]>
    {
        [RecordingState.Idle] = new[] { RecordingState.Selecting },
        [RecordingState.Selecting] = new[] { RecordingState.Countdown, RecordingState.Idle },
        [RecordingState.Countdown] = new[] { RecordingState.Recording, RecordingState.Idle },
        [RecordingState.Recording] = new[] { RecordingState.Finalizing, RecordingState.Idle },
        [RecordingState.Finalizing] = new[] { RecordingState.Reviewing, RecordingState.Idle },
        [RecordingState.Reviewing] = new[] { RecordingState.Idle }
    };
    public RecordingState State { get; private set; } = RecordingState.Idle;
    public bool CanTransitionTo(RecordingState next) => Allowed.TryGetValue(State, out var targets) && targets.Contains(next);
    public void TransitionTo(RecordingState next)
    {
        if (!CanTransitionTo(next)) throw new InvalidOperationException($"Cannot transition from {State} to {next}.");
        State = next;
    }
}
