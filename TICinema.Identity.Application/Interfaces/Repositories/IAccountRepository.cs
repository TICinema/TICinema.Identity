using TICinema.Identity.Domain.Entities;

namespace TICinema.Identity.Application.Interfaces.Repositories;

public interface IAccountRepository
{
    Task<PendingContactChange?> FindPendingContactChange(string accountId, string type);
    Task UpsertPendingContactChange(PendingContactChange data);
    Task DeletePendingContactChange(string accountId, string type);
}