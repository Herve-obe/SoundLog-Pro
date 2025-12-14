using System.Windows;
using System.Windows.Controls;

namespace Veriflow.Desktop.Helpers
{
    public static class TreeViewHelper
    {
        public static readonly DependencyProperty BringIntoViewOnSelectedProperty =
            DependencyProperty.RegisterAttached(
                "BringIntoViewOnSelected",
                typeof(bool),
                typeof(TreeViewHelper),
                new UIPropertyMetadata(false, OnBringIntoViewOnSelectedChanged));

        public static bool GetBringIntoViewOnSelected(DependencyObject obj)
        {
            return (bool)obj.GetValue(BringIntoViewOnSelectedProperty);
        }

        public static void SetBringIntoViewOnSelected(DependencyObject obj, bool value)
        {
            obj.SetValue(BringIntoViewOnSelectedProperty, value);
        }

        private static void OnBringIntoViewOnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TreeViewItem item)) return;

            if ((bool)e.NewValue)
            {
                // Subscribe
                item.Selected += OnItemSelected;

                // Handle immediate case if already selected when property attached
                if (item.IsSelected)
                {
                    item.BringIntoView();
                }
            }
            else
            {
                // Unsubscribe
                item.Selected -= OnItemSelected;
            }
        }

        private static void OnItemSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item)
            {
                item.BringIntoView();
                e.Handled = true; // Prevent bubbling to parent items
            }
        }
    }
}
