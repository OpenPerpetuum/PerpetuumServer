namespace Perpetuum.Builders
{
    public static class BuilderExtensions
    {
        private class ProxyBuilder<T> : IBuilder<T> where T : class
        {
            private readonly IBuilder<T> builder;
            private T objectToBuild;

            public ProxyBuilder(IBuilder<T> builder)
            {
                this.builder = builder;
            }

            public T Build()
            {
                return objectToBuild ?? (objectToBuild = builder.Build());
            }
        }

        public static IBuilder<T> ToProxy<T>(this IBuilder<T> builder) where T : class
        {
            return new ProxyBuilder<T>(builder);
        }
    }

}