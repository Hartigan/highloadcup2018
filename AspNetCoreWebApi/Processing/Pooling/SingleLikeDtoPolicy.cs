using AspNetCoreWebApi.Domain.Dto;
using Microsoft.Extensions.ObjectPool;

namespace AspNetCoreWebApi.Processing.Pooling
{
    public class SingleLikeDtoPolicy : IPooledObjectPolicy<SingleLikeDto>
    {
        public SingleLikeDto Create()
        {
            return new SingleLikeDto();
        }

        public bool Return(SingleLikeDto obj)
        {
            return true;
        }
    }
}
