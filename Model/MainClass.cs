using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using UpdateLevelByCategory.ViewModel;
using UpdateLevelByCategory.View;

namespace UpdateLevelByCategory.Model
{
    [Transaction(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
   public class UpdateLevelByCategory :  IExternalCommand
       
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var vm = new MainWindowViewModel();
            vm.RevitModel = new RevitModelClass(commandData.Application);
            var MainWindow = new MainWindow (vm);
            try
            {
                if (MainWindow != null)
                {
                    MainWindow.ShowDialog();
                }
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                TaskDialog.Show("Error", exception.Message);
                return Result.Failed;
            }
        }
   }
}

