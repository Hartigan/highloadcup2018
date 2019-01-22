using AspNetCoreWebApi.Domain.Dto;
using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public struct LoaderItem
    {
        public LoaderItem(ObjectPool<AccountDto> pool, AccountDto item)
        {
            Pool = pool;
            Dto = item;
        }

        public ObjectPool<AccountDto> Pool;

        public AccountDto Dto;
    }
}