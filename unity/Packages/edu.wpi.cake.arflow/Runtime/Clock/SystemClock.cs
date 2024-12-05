using System;

namespace CakeLab.ARFlow.Clock
{
    public class SystemClock : IClock
    {
        public DateTime Now
        {
            get { return DateTime.Now; }
        }
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }
    }
}
