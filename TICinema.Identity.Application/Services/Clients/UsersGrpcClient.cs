using TICinema.Contracts.Protos.Users;
using TICinema.Identity.Application.Interfaces.Clients;

namespace TICinema.Identity.Application.Services.Clients;

public class UsersGrpcClient(UsersService.UsersServiceClient client) : IUsersGrpcClient
{
    public async Task<bool> CreateUserAsync(string userId)
    {
        var request = new CreateUserRequest { Id = userId };
        var response = await client.CreateUserAsync(request);
        return response.Ok;
    }
}