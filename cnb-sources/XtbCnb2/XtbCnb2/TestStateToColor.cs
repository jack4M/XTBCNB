using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace XtbCnb2
{
    class TestStateToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = (string)parameter;
            object valNull;
            object valTrue;
            object valFalse;

            if (param == "color")
            {
                valNull = null;
                valTrue = "#a0f0a0";
                valFalse = "#f0a0a0";
            }
            else
            {
                valNull = "Please load an XML";
                valTrue = "Test data";
                valFalse = "LIVE DATA!";
            }

            switch ((bool?)value)
            {
                default:
                    return valNull;

                case true:
                    return valTrue;

                case false:
                    return valFalse;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
