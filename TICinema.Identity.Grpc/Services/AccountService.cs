using Grpc.Core;
using TICinema.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using TICinema.Contracts.Protos.Identity;
using TICinema.Identity.Application.Interfaces;
using TICinema.Identity.Application.Interfaces.Services;

public class AccountService(
    UserManager<ApplicationUser> userManager,
    ILogger<AccountService> logger, IAccountService accountService) : TICinema.Contracts.Protos.Identity.AccountService.AccountServiceBase
{
    public override async Task<GetAccountResponse> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        
        if (user == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Пользователь не найден"));
        }

        var roles = await userManager.GetRolesAsync(user);

        return new GetAccountResponse()
        {
            Id = user.Id,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            IsEmailVerified = user.IsEmailVerified,
            IsPhoneVerified = user.IsPhoneVerified,
            Role = ToGrpcRole(roles)
        };
    }
    
    public override async Task<InitEmailChangeResponse> InitEmailChange(InitEmailChangeRequest request, ServerCallContext context)
    {
        try 
        {
            var result = await accountService.InitEmailChangeAsync(request.UserId, request.Email);
            return new InitEmailChangeResponse { Ok = result };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<ConfirmEmailChangeResponse> ConfirmEmailChange(ConfirmEmailChangeRequest request, ServerCallContext context)
    {
        try 
        {
            var result = await accountService.ConfirmEmailChangeAsync(request.UserId, request.Email, request.Code);
            return new ConfirmEmailChangeResponse { Ok = result };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
    
    public override async Task<InitPhoneChangeResponse> InitPhoneChange(InitPhoneChangeRequest request, ServerCallContext context)
    {
        try 
        {
            var result = await accountService.InitPhoneChangeAsync(request.UserId, request.Phone);
            return new InitPhoneChangeResponse { Ok = result };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<ConfirmPhoneChangeResponse> ConfirmPhoneChange(ConfirmPhoneChangeRequest request, ServerCallContext context)
    {
        try 
        {
            var result = await accountService.ConfirmPhoneChangeAsync(request.UserId, request.Phone, request.Code);
            return new ConfirmPhoneChangeResponse { Ok = result };
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
    
    private static Role ToGrpcRole(IEnumerable<string> roles)
    {
        if (roles.Contains("Admin")) return Role.Admin;
        return Role.User;
    }
}