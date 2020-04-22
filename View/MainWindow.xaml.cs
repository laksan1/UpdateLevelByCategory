using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using UpdateLevelByCategory.ViewModel;
using UpdateLevelByCategory.View;
using UpdateLevelByCategory.Model;
using UpdateLevelByCategory.MVVM;
using System.ComponentModel;
using MahApps.Metro.Controls;
namespace UpdateLevelByCategory.View
   
 
{
    
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow :  MetroWindow, IDisposable
    {
        MainWindowViewModel MVVM;
        public MainWindow(MainWindowViewModel _vm)
        {
            InitializeComponent();
            DataContext = _vm;
            MVVM =_vm;
        }

        private void Perfom_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        public void Dispose()
        {
            this.Close();
        }


        private void GetFamilyandGetParameters(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(var en in MVVM.dictionaryCategory)
            {
                RadioButton rb = new RadioButton();
                rb.Content = en.Key;
                rb.Checked += (Checked);
                rb.CommandParameter = en.Value;
                rb.Command = MVVM.CommandRadioChecked;
                RadioPanel.Children.Add(rb);
            }
        }
        
        void Checked(object sender, EventArgs e)
        {
            RadioButton RB = (RadioButton)sender;
            if(RB.IsChecked==true)
            {
                TextBlockSelectedElements.Text = "You have selected the \u0022" + RB.Content + "\u0022 category";
            }

        }

        private void Label_KeyUp(object sender, KeyEventArgs e)
        {
            this.UpdateButton.IsEnabled = (this.textBoxOffset.Text.Length > 0) ? true : false;//Проверка на пустоту TextBox
        }
    }
}
