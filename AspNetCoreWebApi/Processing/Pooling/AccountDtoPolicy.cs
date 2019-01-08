using AspNetCoreWebApi.Domain.Dto;
using Microsoft.Extensions.ObjectPool;


namespace AspNetCoreWebApi.Processing.Pooling
{
    public class AccountDtoPolicy : IPooledObjectPolicy<AccountDto>
    {
        public AccountDto Create()
        {
            return new AccountDto();
        }

        public bool Return(AccountDto obj)
        {
            obj.Clear();
            return true;
        }
    }
}
