using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Task.Run(() =>
            //{
            //    for (int i = 0; i < 1000000; i++)
            //    {
            //        //terminalControl.WriteLine(i + " aaa");
            //        //terminalControl.WriteInLine(i + " aaa");
            //    }
            //});

            //Task.Factory.StartNew(() => {
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        terminalControl.WriteLine(i + " aaa");

            //    }
            //}, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        int i=1;
        private void BtnTest_Click(object sender, RoutedEventArgs e)
        {

                //for (int i = 0; i < 100; i++)
                //{
                //    terminalControl.WriteLine(i + " aaa");
                //    //terminalControl.WriteInLine(i + " aaa");
                //}
            terminalControl.WriteLine(i.ToString());
            i++;
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            i = 1;
            terminalControl.Clear();
        }
    }
}
