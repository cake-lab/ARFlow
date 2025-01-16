using System;

namespace CakeLab.ARFlow.Clock
{
    public interface IClock
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
}
