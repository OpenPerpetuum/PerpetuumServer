using System;
using System.Threading;

namespace Perpetuum.Threading
{
    public static class LockFree
    {
        public static void Update(ref uint destination, Func<uint, uint> updater)
        {
            var spinWait = new SpinWait();

            unsafe
            {
                fixed (uint* p = &destination)
                {
                    while (true)
                    {
                        var original = *p;
                        var value = updater(original);

                        if (Interlocked.CompareExchange(ref *(int*)p, (int)value, (int)original) == original)
                            return;

                        spinWait.SpinOnce();
                    }
                }
            }
        }


        public static void Swap<T>(ref T a, ref T b) where T : class
        {
            Interlocked.Exchange(ref a, Interlocked.Exchange(ref b, a));
        }
    }
}