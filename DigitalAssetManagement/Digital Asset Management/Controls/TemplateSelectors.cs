using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DigitalAssetManagementTemplate.Controls
{
    public class StringTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Template1 { get; set; }
        public string String1 { get; set; }
        public DataTemplate Template2 { get; set; }
        public string String2 { get; set; }
        public DataTemplate Template3 { get; set; }
        public string String3 { get; set; }
        public DataTemplate Template4 { get; set; }
        public string String4 { get; set; }
        public DataTemplate Template5 { get; set; }
        public string String5 { get; set; }
        public DataTemplate Template6 { get; set; }
        public string String6 { get; set; }
        public DataTemplate Template7 { get; set; }
        public string String7 { get; set; }
        public DataTemplate Template8 { get; set; }
        public string String8 { get; set; }
        public DataTemplate Template9 { get; set; }
        public string String9 { get; set; }
        public DataTemplate Template10 { get; set; }
        public string String10 { get; set; }
        public DataTemplate DefaultTemplate { get; set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //validate
            if (item == null)
            {
                return DefaultTemplate;
            }

            //get string
            var str = item as string;
            if (item.GetType().IsPrimitive || item.GetType().IsEnum)
            {
                str = item.ToString();
            }

            //select template
            if (str == null)
            {
                return DefaultTemplate;
            }
            if (String1 != null && String1.Split(',').Contains(str))
            {
                return Template1;
            }
            if (String2 != null && String2.Split(',').Contains(str))
            {
                return Template2;
            }
            if (String3 != null && String3.Split(',').Contains(str))
            {
                return Template3;
            }
            if (String4 != null && String4.Split(',').Contains(str))
            {
                return Template4;
            }
            if (String5 != null && String5.Split(',').Contains(str))
            {
                return Template5;
            }
            if (String6 != null && String6.Split(',').Contains(str))
            {
                return Template6;
            }
            if (String7 != null && String7.Split(',').Contains(str))
            {
                return Template7;
            }
            if (String8 != null && String8.Split(',').Contains(str))
            {
                return Template8;
            }
            if (String9 != null && String9.Split(',').Contains(str))
            {
                return Template9;
            }
            if (String10 != null && String10.Split(',').Contains(str))
            {
                return Template10;
            }
            return DefaultTemplate;
        }
    }

    public class TypeTemplateSelector : StringTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            //convert type to string
            if (item != null)
            {
                item = item.GetType().Name;
            }

            return base.SelectTemplateCore(item, container);
        }
    }

}
