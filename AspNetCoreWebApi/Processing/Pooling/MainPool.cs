using AspNetCoreWebApi.Domain.Dto;
using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class MainPool
    {
        public DefaultObjectPool<AccountDto> AccountDto { get; } = new DefaultObjectPool<AccountDto>(new AccountDtoPolicy());

        public DefaultObjectPool<SingleLikeDto> SingleLikeDto { get; } = new DefaultObjectPool<SingleLikeDto>(new SingleLikeDtoPolicy());
    }
}
