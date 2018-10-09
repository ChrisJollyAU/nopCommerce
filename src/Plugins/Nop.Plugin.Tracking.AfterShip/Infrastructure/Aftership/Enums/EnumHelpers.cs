using System;
using System.ComponentModel;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums
{
    public static class EnumHelpers
    {
        public static string GetName(this FieldTracking value)
        {
            var fi = typeof(FieldTracking).GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static string GetName(this StatusTag value)
        {
            var fi = typeof(StatusTag).GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static string GetName(this FieldCheckpoint value)
        {
            var fi = typeof(FieldCheckpoint).GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static string GetIso3Code(this Iso3Country value)
        {
            var fi = typeof(Iso3Country).GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static Iso3Country ToIso3Enum(this string value)
        {
            var enumValArray = Enum.GetValues(typeof(Iso3Country));
            foreach (int val in enumValArray)
            {
                if (((Iso3Country)val).GetIso3Code().Equals(value, StringComparison.CurrentCultureIgnoreCase))
                    return (Iso3Country)val;
            }

            return Iso3Country.Null;
        }
    }
}