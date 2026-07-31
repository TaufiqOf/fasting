using Fasting.Models;

namespace Fasting.Shared.Services;

public interface IUserProfileStore
{
    Task<UserProfile?> LoadAsync();

    Task SaveAsync(UserProfile profile);

    Task ClearAsync();
}