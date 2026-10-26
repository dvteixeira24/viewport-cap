using ViewportCap.Encoding;
using Xunit;

namespace ViewportCap.Encoding.Tests;

public sealed class SystemProcessRunnerTests
{
#pragma warning disable xUnit1031
    [Fact]
    public void RunAsync_DoesNotCaptureSynchronizationContext()
    {
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new DroppingSynchronizationContext());

        try
        {
            var runner = new SystemProcessRunner();
            var task = runner.RunAsync(
                "cmd.exe",
                new[] { "/c", "echo", "hello" },
                null,
                null,
                CancellationToken.None);

            Assert.True(
                task.Wait(TimeSpan.FromSeconds(10)),
                "RunAsync deadlocked on the captured SynchronizationContext.");

            Assert.Equal(0, task.Result.ExitCode);
            Assert.Contains("hello", task.Result.StandardOutput);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
#pragma warning restore xUnit1031

    private sealed class DroppingSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            // Intentionally drop continuations, simulating a blocked UI dispatcher.
        }
    }
}
