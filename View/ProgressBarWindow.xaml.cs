using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MahApps.Metro.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UpdateLevelByCategory.ViewModel;
using UpdateLevelByCategory.Model;

namespace UpdateLevelByCategory.View
{
    /// <summary>
    /// Логика взаимодействия для ProgressBarWindow.xaml
    /// </summary>
    public partial class ProgressBarWindow : MetroWindow

    {
        public ProgressBarWindow (ProgressBarViewModel _prVM)
        {

            InitializeComponent();

        }

        public void UpdateProgress(string message, int current, int total)
        {
            this.Dispatcher.Invoke(new Action<string, int, int>(

            delegate (string m, int v, int t)
            {
                this.prBar.Maximum = System.Convert.ToDouble(t);
                this.prBar.Value = System.Convert.ToDouble(v);
                this.labelNumberElement.Content = $"Processed {v} elements of {t}";
                this.labelNameElement.Content = $"Element name - \u0022{m}\u0022";
            }),
            System.Windows.Threading.DispatcherPriority.Background,
            message, current, total);
        }

        public void MetroWindow_Closed(object sender, EventArgs e)
        {
            this.Close(); // ?
        }
    }

}
