using Compost.Core.Interfaces;

namespace Compost.Contexts.Services;

public interface ITimeTrackingService
{
    Task<TimeSpan> GetTotalTimeSpentAsync(string projectId);
    Task<TimeSpan> GetCurrentSessionDurationAsync(string projectId);
    Task<Dictionary<string, TimeSpan>> GetTimeBreakdownAsync();
}

public class TimeTrackingService(IProjectManager projectManager) : ITimeTrackingService
{
    public async Task<TimeSpan> GetTotalTimeSpentAsync(string projectId)
    {
        var context = await projectManager.GetProjectByIdAsync(projectId);
        if (context == null) return TimeSpan.Zero;

        var total = TimeSpan.FromSeconds(context.TotalTimeSpentSeconds);

        // Add current session time if active
        if (!context.CurrentSessionStartedAt.HasValue) return total;
        var currentSession = DateTime.UtcNow - context.CurrentSessionStartedAt.Value;
        total += currentSession;

        return total;
    }

    public async Task<TimeSpan> GetCurrentSessionDurationAsync(string projectId)
    {
        var context = await projectManager.GetProjectByIdAsync(projectId);
        if (context?.CurrentSessionStartedAt == null)
            return TimeSpan.Zero;

        return DateTime.UtcNow - context.CurrentSessionStartedAt.Value;
    }

    public async Task<Dictionary<string, TimeSpan>> GetTimeBreakdownAsync()
    {
        return await projectManager.GetTimeSpentByProjectAsync();
    }
}
