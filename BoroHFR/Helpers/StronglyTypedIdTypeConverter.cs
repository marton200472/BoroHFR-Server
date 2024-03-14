using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel;
using System.Globalization;

#nullable disable
namespace BoroHFR.Helpers
{
    public interface IStrongId
    {
        public Guid value { get; init; }
    }

    public class StronglyTypedIdTypeConverter<T> : TypeConverter where T : IStrongId, new()
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string)
                || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var casted = value as string;
            return new T() { value = Guid.Parse(casted) };
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var stronglyTypedId = (T)value;
            Guid idValue = stronglyTypedId.value;
            if (destinationType == typeof(string))
                return idValue.ToString()!;
            if (destinationType == typeof(Guid))
                return idValue;
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
