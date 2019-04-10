using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.PlatformUI;
using ModelFiltersGenerator.Analyzers;
using ModelFiltersGenerator.Models;

namespace ModelFiltersGenerator.Dialogs
{
    internal class SelectPropertiesDialog : DialogWindow
    {
        public SelectPropertiesDialog(IEnumerable<PropertyInfo> properties)
        {
            Width = 500;
            Title = "Select properties for filter";
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Pixel) });

            content.Children.Add(CreatePropertiesList(properties));

            var buttonsPanel = CreateButtonsPanel();
            Grid.SetRow(buttonsPanel, 1);

            content.Children.Add(buttonsPanel);

            Content = content;
        }

        private UIElement CreateButtonsPanel()
        {
            var okBtn = new Button
            {
                Content = "Ok",
                Height = 30,
                Width = 90,
                Margin = new Thickness(0, 0, 10, 0)
            };

            okBtn.Click += OkBtn_Click;

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Height = 30,
                Width = 90,
                Margin = new Thickness(0, 0, 10, 0)
            };

            cancelBtn.Click += CancelBtn_Click;

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            panel.Children.Add(okBtn);
            panel.Children.Add(cancelBtn);

            return panel;
        }

        private static UIElement CreatePropertiesList(IEnumerable<PropertyInfo> properties)
        {
            var mainPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var property in properties)
            {
                var propertyRow = CreatePropertyRow(property);
                mainPanel.Children.Add(propertyRow);
            }

            return mainPanel;
        }

        private static UIElement CreatePropertyRow(PropertyInfo property)
        {
            var propertyRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 10, 10, 0)
            };

            var propertyCheckbox = new CheckBox
            {
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = property.TypeSyntax.ToString(),
                            Margin = new Thickness(0, 0, 5, 0),
                            Foreground = new SolidColorBrush(Colors.MediumBlue)
                        },
                        new TextBlock { Text = property.Name }
                    },
                    Orientation = Orientation.Horizontal
                },
                Width = 300,
            };

            var binding = new Binding(nameof(PropertyInfo.Included)) { Source = property };
            propertyCheckbox.SetBinding(CheckBox.IsCheckedProperty, binding);

            propertyRow.Children.Add(propertyCheckbox);

            if (property.TypeInfo.IsBool() || property.TypeInfo.TypeKind == TypeKind.Enum)
            {
                return propertyRow;
            }

            var filterType = CreateFilterTypeCombobox(property);

            propertyRow.Children.Add(filterType);
            return propertyRow;
        }

        private static ComboBox CreateFilterTypeCombobox(PropertyInfo property)
        {
            var filterType = new ComboBox
            {
                Width = 150,
                SelectedIndex = 0
            };

            var binding = new Binding(nameof(PropertyInfo.FilterType)) { Source = property };
            filterType.SetBinding(ComboBox.SelectedValueProperty, binding);

            if (property.TypeInfo.IsString())
            {
                property.FilterType = FilterType.Contains;
                filterType.Items.Add(new ComboBoxItem { Content = FilterType.Contains });
                filterType.Items.Add(new ComboBoxItem { Content = FilterType.Equals });

                return filterType;
            }

            property.FilterType = FilterType.Range;
            filterType.Items.Add(new ComboBoxItem { Content = FilterType.Range });
            filterType.Items.Add(new ComboBoxItem { Content = FilterType.Equals });

            return filterType;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
