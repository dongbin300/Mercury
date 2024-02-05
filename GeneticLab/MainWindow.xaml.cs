using GeneticLab.Samples;

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

using Tensorflow;
using Tensorflow.NumPy;
using Tensorflow.Operations.Initializers;

using static Tensorflow.Binding;

namespace GeneticLab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker();
            worker.DoWork += Run;
            worker.RunWorkerAsync();
        }

        private void Run(object? sender, DoWorkEventArgs e)
        {
            //new SumSample().Run(Dispatcher, LogListBox);
            //new TspSample().Run(Dispatcher, TspCanvas);
            //new FunctionOptimizationSample().Run(Dispatcher, LogListBox);
            new CandleCrossRsiSample().Run(Dispatcher, LogListBox);
		}
    }
}