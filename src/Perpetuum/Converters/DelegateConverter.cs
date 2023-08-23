using System;

namespace Perpetuum.Converters
{
    public class DelegateConverter<TIn, TOut> : IConverter<TIn, TOut>
    {
        private readonly Converter<TIn, TOut> _converter;

        public DelegateConverter(Converter<TIn, TOut> converter)
        {
            _converter = converter;
        }

        public TOut Convert(TIn item)
        {
            return _converter(item);
        }
    }
}