using System;
using System.Windows;

namespace Lab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var test = "20231012";

            var result = DateTime.Parse($"{test[0..4]}-{test[4..6]}-{test[6..8]}");
        }
    }
}
