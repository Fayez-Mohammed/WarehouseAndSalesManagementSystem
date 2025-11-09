namespace Base.Services.Interfaces
{
    public interface IUserProfileService
    {
        Task<bool> DeleteProfileAndUserAsync(string profileId);
    }
}