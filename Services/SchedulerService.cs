namespace NTWallpaper.Services;

using System.Collections.Concurrent;
using NTWallpaper.Domain.Interfaces;

/// <summary>
/// Lightweight in-memory scheduler. Fires <see cref="GroupDue"/> once per due group,
/// then removes it from the schedule so a missed rotation (e.g. after sleep) cannot
/// trigger a burst of duplicate executions. The orchestrator reschedules after each run.
/// </summary>
public class SchedulerService : ISchedulerService
{
    private readonly System.Threading.Timer _timer;
    private readonly ConcurrentDictionary<string, DateTime> _schedule = new();
    private readonly object _timerLock = new();

    public SchedulerService()
    {
        _timer = new System.Threading.Timer(CheckDue, null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler<GroupDueEventArgs>? GroupDue;

    public void Start()
    {
        lock (_timerLock)
            _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
    }

    public void Stop()
    {
        lock (_timerLock)
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void ScheduleGroup(string groupId, DateTime nextRunUtc)
    {
        _schedule[groupId] = nextRunUtc;
    }

    public void TriggerNow(string groupId)
    {
        GroupDue?.Invoke(this, new GroupDueEventArgs { GroupId = groupId });
    }

    public DateTime? GetNextRun(string groupId)
        => _schedule.TryGetValue(groupId, out var v) ? v : null;

    private void CheckDue(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _schedule)
        {
            if (kvp.Value <= now && _schedule.TryRemove(kvp.Key, out _))
            {
                GroupDue?.Invoke(this, new GroupDueEventArgs { GroupId = kvp.Key });
            }
        }
    }
}
