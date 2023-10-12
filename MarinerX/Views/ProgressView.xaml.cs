using System.Windows;

namespace MarinerX.Views
{
    /// <summary>
    /// ProgressView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ProgressView : Window
    {
        public ProgressView()
        {
            InitializeComponent();
            Top = 0;
            Left = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = 14;
        }

        public ProgressView(int top, int left, int width, int height)
        {
            InitializeComponent();
            Top = top;
            Left = left;
            Width = width;
            Height = height;
        }
    }
}
