using System;

namespace Common
{
    public interface IExceptionConfiguration
    {
        string Configure(Guid? errorId = null);
    }
}