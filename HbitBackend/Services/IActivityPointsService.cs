using System.Threading.Tasks;

namespace HbitBackend.Services;

public interface IActivityPointsService
{
    Task<int> GetAtivityPoints(int userId, int activityId, int activityGoalId);
}
