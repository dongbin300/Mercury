using System;
using System.Collections.Generic;
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

            Dictionary<List<int>, int> test = new Dictionary<List<int>, int>()
            {
                { new List<int>(){1, 2, 3, 4}, 6 },
                { new List<int>(){1, 2, 3, 4}, 7 },
            };
        }
    }
}
