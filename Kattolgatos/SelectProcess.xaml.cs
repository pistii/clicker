using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Kattolgatos
{
    /// <summary>
    /// Interaction logic for SelectProcess.xaml
    /// </summary>
    public partial class SelectProcess : Window
    {
        public SelectProcess()
        {
            this.DataContext = this;
            AvailableProcesses = new List<System.Diagnostics.Process>();

            searchProcess();
            InitializeComponent();
        }

        private int ID;

        List<System.Diagnostics.Process> _processes = new List<System.Diagnostics.Process>();
        public List<System.Diagnostics.Process> AvailableProcesses
        {
            get { return _processes; }
            set {  }
        }

        private void searchProcess()
        {
            System.Diagnostics.Process[] localAll = System.Diagnostics.Process.GetProcesses();
            
            foreach (System.Diagnostics.Process AvailableProcess in localAll)
            {
                if (!string.IsNullOrEmpty(AvailableProcess.MainWindowTitle))
                {
                    //MessageBox.Show(AvailableProcess.MainWindowTitle);
                    AvailableProcesses.Add(AvailableProcess);

                }
            }


        }
    }
}
