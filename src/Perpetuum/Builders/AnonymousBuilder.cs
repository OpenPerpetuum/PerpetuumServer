using System;

namespace Perpetuum.Builders
{
    public class AnonymousBuilder<T> : IBuilder<T>
    {
        private readonly Func<T> builder;

        public AnonymousBuilder(Func<T> builder)
        {
            this.builder = builder;
        }

        public T Build()
        {
            return builder();
        }
    }
}