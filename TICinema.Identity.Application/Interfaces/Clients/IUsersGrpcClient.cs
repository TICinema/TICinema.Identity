namespace TICinema.Identity.Application.Interfaces.Clients;

public interface IUsersGrpcClient
{
    Task<bool> CreateUserAsync(string userId);
}