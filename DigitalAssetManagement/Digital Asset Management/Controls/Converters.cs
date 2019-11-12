using ServiceHelpers;
using System;
using System.Collections;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace DigitalAssetManagementTemplate.Controls
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? true : false;
        }
    }

    public class ReverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((Visibility)value) == Visibility.Visible ? false : true;
        }
    }

    public class ReverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (Visibility)value;
        }
    }

    public class ReverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
    }

    public class CollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //get min value
            int.TryParse(parameter != null ? (string)parameter : string.Empty, out int minValue);

            //get count
            var count = 0;
            if (value is int)
            {
                count = (int)value;
            }
            else
            {
                var collection = value as ICollection;
                if (collection != null)
                {
                    count = collection.Count;
                }
            }

            return count > minValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseCollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //get min value
            int.TryParse(parameter != null ? (string)parameter : string.Empty, out int minValue);

            //get count
            var count = 0;
            if (value is int)
            {
                count = (int)value;
            }
            else
            {
                var collection = value as ICollection;
                if (collection != null)
                {
                    count = collection.Count;
                }
            }

            return count > minValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class EnumMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
            {
                return Visibility.Collapsed;
            }

            var currentState = value.ToString();
            var stateStrings = parameter.ToString();

            if (string.Equals(currentState, stateStrings, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseEnumMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null || !(value is Enum))
            {
                return Visibility.Collapsed;
            }

            var currentState = value.ToString();
            var stateStrings = parameter.ToString();

            if (!string.Equals(currentState, stateStrings, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseNullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ItemClickedEventArgsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ItemClickEventArgs args = value as ItemClickEventArgs;

            return args != null ? args.ClickedItem : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class StringMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null && 
                string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseStringMatchToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null &&
                string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null && string.Equals(value.ToString(), (string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringContainsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && parameter != null &&
                ((string)value).Contains((string)parameter, StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class StringFormatConverter : IValueConverter
    {
        public string StringFormat { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var format = parameter as string;
            if (format == null)
            {
                format = StringFormat;
            }
            if (!string.IsNullOrWhiteSpace(format) && value != null)
            {
                return String.Format(format, value);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class StringLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null && !string.IsNullOrEmpty((string)value))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class ReverseStringLengthToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || (value is string && string.IsNullOrEmpty((string)value)))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class MaxLengthConverter : IValueConverter
    {
        public int MaxLength { get; set; }
        public bool AddEllipsis { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {

            var maxLength = MaxLength;
            if (parameter != null)
            {
                if (parameter is int)
                {
                    maxLength = (int)parameter;
                }
                if (parameter is string)
                {
                    int.TryParse((string)parameter, out maxLength);
                }
            }

            if (maxLength == 0)
            {
                return value;
            }

            //trim string
            var valueString = value as string;
            if (valueString != null && valueString.Length > maxLength)
            {
                return valueString.Substring(0, maxLength) + (AddEllipsis ? "..." : string.Empty);
            }

            //trim array
            var valueArray = value as Array;
            if (valueArray != null && valueArray.Length > maxLength)
            {
                var newArray = Array.CreateInstance(valueArray.GetType().GetElementType(), maxLength);
                Array.Copy(valueArray, newArray, newArray.Length);
                return newArray;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MaxLengthVisibilityConverter : IValueConverter
    {
        public int MaxLength { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, string language)
        {

            var maxLength = MaxLength;
            if (parameter != null)
            {
                if (parameter is int)
                {
                    maxLength = (int)parameter;
                }
                if (parameter is string)
                {
                    int.TryParse((string)parameter, out maxLength);
                }
            }

            if (maxLength == 0)
            {
                return Visibility.Visible;
            }

            //trim string
            var valueString = value as string;
            if (valueString != null && valueString.Length > maxLength)
            {
                return Visibility.Collapsed;
            }

            //trim array
            var valueArray = value as Array;
            if (valueArray != null && valueArray.Length > maxLength)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class ReverseMaxLengthVisibilityConverter : MaxLengthVisibilityConverter
    {
        public override object Convert(object value, Type targetType, object parameter, string language)
        {
            var result = (Visibility)base.Convert(value, targetType, parameter, language);
            return result == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public class NormalizeSpacesConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, string language)
        {
            //validate
            var strValue = value as string;
            if (strValue == null)
            {
                return value;
            }

            return Normalize(strValue);
        }

        public static string Normalize(string value)
        {
            if (value == null)
            {
                return value;
            }
            return value.Replace("\n", string.Empty).Replace("\t", string.Empty).Replace("\r", string.Empty).Trim();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MathConverter : IValueConverter
    {
        public double Add { get; set; }
        public double Multiply { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var number = System.Convert.ToDouble(value);
                return (number + Add) * Multiply;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                var number = System.Convert.ToDouble(value);
                return (number / Multiply) - Add;
            }
            return value;
        }
    }

    public class StringConverter : IValueConverter
    {
        public object Return1 { get; set; }
        public string If1 { get; set; }
        public object Return2 { get; set; }
        public string If2 { get; set; }
        public object Return3 { get; set; }
        public string If3 { get; set; }
        public object Return4 { get; set; }
        public string If4 { get; set; }
        public object Return5 { get; set; }
        public string If5 { get; set; }
        public object Return6 { get; set; }
        public string If6 { get; set; }
        public object Return7 { get; set; }
        public string If7 { get; set; }
        public object Return8 { get; set; }
        public string If8 { get; set; }
        public object Return9 { get; set; }
        public string If9 { get; set; }
        public object Return10 { get; set; }
        public string If10 { get; set; }
        public object DefaultReturn { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            //validate
            if (value == null)
            {
                return DefaultReturn;
            }

            //get string
            var str = value as string;
            if (value.GetType().IsPrimitive || value.GetType().IsEnum)
            {
                str = value.ToString();
            }

            //select return value
            if (str == null)
            {
                return DefaultReturn;
            }
            if (If1 != null && If1.Split(',').Contains(str))
            {
                return Return1;
            }
            if (If2 != null && If2.Split(',').Contains(value))
            {
                return Return2;
            }
            if (If3 != null && If3.Split(',').Contains(value))
            {
                return Return3;
            }
            if (If4 != null && If4.Split(',').Contains(value))
            {
                return Return4;
            }
            if (If5 != null && If5.Split(',').Contains(value))
            {
                return Return5;
            }
            if (If6 != null && If6.Split(',').Contains(value))
            {
                return Return6;
            }
            if (If7 != null && If7.Split(',').Contains(value))
            {
                return Return7;
            }
            if (If8 != null && If8.Split(',').Contains(value))
            {
                return Return8;
            }
            if (If9 != null && If9.Split(',').Contains(value))
            {
                return Return9;
            }
            if (If10 != null && If10.Split(',').Contains(value))
            {
                return Return10;
            }
            return DefaultReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToIntConverter : IValueConverter
    {
        public int IfTrue { get; set; } = 1;
        public int IfFalse { get; set; } = 0;
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            return (bool)value ? IfTrue : IfFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return ((int)value) == IfTrue ? true : false;
        }
    }

    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                return value.ToString();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
            {
                return 0;
            }
            if (int.TryParse(value as string, out var number))
            {
                return number;
            }
            return 0;
        }
    }

    public class ImageAnalyzerToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var imageAnalyzer = value as ImageAnalyzer;
            if (imageAnalyzer != null)
            {
                return imageAnalyzer.GetImageSource().Result;
            }
            return value; //no convertion
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}