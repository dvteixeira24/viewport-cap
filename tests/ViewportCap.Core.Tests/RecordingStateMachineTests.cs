using ViewportCap.Core;
using Xunit;

namespace ViewportCap.Core.Tests;

public sealed class RecordingStateMachineTests
{
    [Theory]
    [InlineData(RecordingState.Idle, RecordingState.Selecting)] [InlineData(RecordingState.Selecting, RecordingState.Countdown)] [InlineData(RecordingState.Selecting, RecordingState.Idle)]
    [InlineData(RecordingState.Countdown, RecordingState.Recording)] [InlineData(RecordingState.Countdown, RecordingState.Idle)] [InlineData(RecordingState.Recording, RecordingState.Finalizing)] [InlineData(RecordingState.Recording, RecordingState.Idle)]
    [InlineData(RecordingState.Finalizing, RecordingState.Reviewing)] [InlineData(RecordingState.Finalizing, RecordingState.Idle)] [InlineData(RecordingState.Reviewing, RecordingState.Exporting)] [InlineData(RecordingState.Reviewing, RecordingState.Idle)]
    [InlineData(RecordingState.Exporting, RecordingState.Reviewing)] [InlineData(RecordingState.Exporting, RecordingState.Idle)]
    public void AllowsExpectedTransitions(RecordingState from, RecordingState to) { var m = new RecordingStateMachine(); MoveTo(m, from); Assert.True(m.CanTransitionTo(to)); m.TransitionTo(to); Assert.Equal(to, m.State); }
    [Theory]
    [InlineData(RecordingState.Idle, RecordingState.Recording)] [InlineData(RecordingState.Idle, RecordingState.Idle)] [InlineData(RecordingState.Selecting, RecordingState.Reviewing)] [InlineData(RecordingState.Countdown, RecordingState.Finalizing)] [InlineData(RecordingState.Recording, RecordingState.Reviewing)] [InlineData(RecordingState.Reviewing, RecordingState.Recording)]
    public void RejectsInvalidTransitions(RecordingState from, RecordingState to) { var m = new RecordingStateMachine(); MoveTo(m, from); Assert.False(m.CanTransitionTo(to)); Assert.Throws<InvalidOperationException>(() => m.TransitionTo(to)); Assert.Equal(from, m.State); }
    private static void MoveTo(RecordingStateMachine m, RecordingState target) { if (target == RecordingState.Idle) return; m.TransitionTo(RecordingState.Selecting); if (target == RecordingState.Selecting) return; m.TransitionTo(RecordingState.Countdown); if (target == RecordingState.Countdown) return; m.TransitionTo(RecordingState.Recording); if (target == RecordingState.Recording) return; m.TransitionTo(RecordingState.Finalizing); if (target == RecordingState.Finalizing) return; m.TransitionTo(RecordingState.Reviewing); if (target == RecordingState.Reviewing) return; m.TransitionTo(RecordingState.Exporting); }
}
