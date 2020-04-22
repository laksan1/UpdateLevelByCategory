using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using UpdateLevelByCategory.MVVM;
using UpdateLevelByCategory.View;
using UpdateLevelByCategory.Model;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace UpdateLevelByCategory.ViewModel
{
    public class MainWindowViewModel :ModelBase
    {
      

        private ObservableCollection<CollectionClass> _listParametersCollection;
        MainWindow MV;
        private ICommand _command1;
        private ICommand _command2;
        private ICommand _command3;
        private ICommand _command4;
        public RelayCommand CheckCommand { get; private set; }
        
        private Action _closeAction;
        internal RevitModelClass RevitModel { get; set; }
        private bool? isAllSelected;
        public bool isCheckedUngroup;
        public Dictionary<string, BuiltInCategory> dictionaryCategory;

        public BuiltInCategory checkedCat; //Выбранная категория

        public string offsetNumberVM;

        public bool IsCheckedUngroup
        {
            get { return isCheckedUngroup; }
            set { isCheckedUngroup = value; OnPropertyChanged(); }
        }
        public bool? IsAllSelected
        {
            get { return isAllSelected; }
            set
            {
                isAllSelected = value;
                OnPropertyChanged();
            }
        }

        public string OffsetNumberVM
        {
            get { return offsetNumberVM; }
            set
            {
                offsetNumberVM = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {

           CheckCommand = new RelayCommand(OnCheckAll);
           IsAllSelected = false;
            dictionaryCategory = new Dictionary<string, BuiltInCategory>();
            {
                dictionaryCategory.Add("Арматура воздуховодов", BuiltInCategory.OST_DuctAccessory);
                dictionaryCategory.Add("Арматура трубопроводов", BuiltInCategory.OST_PipeAccessory);
                dictionaryCategory.Add("Воздуховоды", BuiltInCategory.OST_DuctCurves);
                dictionaryCategory.Add("Воздухораспределители", BuiltInCategory.OST_DuctTerminal);
                dictionaryCategory.Add("Выключатели", BuiltInCategory.OST_LightingDevices);
                dictionaryCategory.Add("Гибкие воздуховоды", BuiltInCategory.OST_FlexDuctCurves);
                dictionaryCategory.Add("Гибкие трубы", BuiltInCategory.OST_FlexPipeCurves);
                dictionaryCategory.Add("Кабельные лотки", BuiltInCategory.OST_CableTray);
                dictionaryCategory.Add("Короба", BuiltInCategory.OST_Conduit);
                dictionaryCategory.Add("Обобщенные модели", BuiltInCategory.OST_GenericModel);
                dictionaryCategory.Add("Оборудование", BuiltInCategory.OST_MechanicalEquipment);
                dictionaryCategory.Add("Трубы", BuiltInCategory.OST_PipeCurves);
                dictionaryCategory.Add("Осветительные приборы", BuiltInCategory.OST_LightingFixtures);
                dictionaryCategory.Add("Сантехнические приборы", BuiltInCategory.OST_PlumbingFixtures);
                dictionaryCategory.Add("Соединительные детали воздуховодов", BuiltInCategory.OST_DuctFitting);
                dictionaryCategory.Add("Соединительные детали кабельных лотков", BuiltInCategory.OST_CableTrayFitting);
                dictionaryCategory.Add("Соединительные детали коробов", BuiltInCategory.OST_ConduitFitting);
                dictionaryCategory.Add("Соединительные детали трубопроводов", BuiltInCategory.OST_PipeFitting);
                dictionaryCategory.Add("Спринклеры", BuiltInCategory.OST_Sprinklers);
                dictionaryCategory.Add("Электрические приборы", BuiltInCategory.OST_ElectricalFixtures);
                dictionaryCategory.Add("Электрооборудование", BuiltInCategory.OST_ElectricalEquipment);
            };

        }

    public ICommand CommandRadioChecked
        {
            get
            {
                if (_command3 == null)
                    _command3 = new RelayCommand(o =>
                    {
                        checkedCat = (BuiltInCategory)o;


                    });
                return _command3;
            }
            set
            {
                OnPropertyChanged();
            }
        }

        public ObservableCollection<CollectionClass> ListcСategoriesCollection
        {
            get => _listParametersCollection;
            set { _listParametersCollection = value; OnPropertyChanged(); }

        }
        private void OnCheckAll(object o)
        {
            if (IsAllSelected == true)
                foreach (var ch in ListcСategoriesCollection)
                {
                    ch.IsChecked = true;
                }
            else
            {
                foreach (var ch in ListcСategoriesCollection)
                {
                    ch.IsChecked = false;
                }
            }
        }
      
        public ICommand CommandUpdateLevel
        {
            get
            {
                if (_command1 == null)
                    _command1 = new RelayCommand(o =>
                    {
                        if(RevitModel.collectionElementsId ==null)
                        {
                            RevitModel.offsetNumberR = OffsetNumberVM;
                            RevitModel.GetRadioButton(checkedCat);
                        }

                        else
                        {

                            RevitModel.offsetNumberR = OffsetNumberVM;
                            RevitModel.UpdateLevel(RevitModel.collectionElementsId);
                          
                        }
                         
                     
                    });
                return _command1;
            }
            set
            {
                OnPropertyChanged();
            }
        }
    
        public ICommand CommandSelectElements
        {
            get
            {
                if (_command2 == null)
                    _command2 = new RelayCommand(o =>
                    {
                       RevitModel.collectionElementsId = RevitModelClass.GetSelectedElements(RevitModel._uiDocument);
                        MV = new MainWindow(this);
                        MV.TextBlockSelectedElements.Text = GetLabelCount(RevitModel.collectionElementsId.Count());
                     
                        MV.ShowDialog();

                    });
                return _command2;
            }
            set
            {
                OnPropertyChanged();
            }
        }


        public ICommand CommandUngroups
        {
            get
            {
                if (_command4 == null)
                    _command4 = new RelayCommand(o =>
                    {
                        if (IsCheckedUngroup)
                            RevitModel.UnGroupAll();

                    });
                return _command4;
            }
            set
            {
                OnPropertyChanged();
            }
        }
        
        public string GetLabelCount(int ct)
        {
            return "Selected "  + ct + "\n elements";
        }

        public Action CloseAction
        {
            get => _closeAction;
            set
            {
                _closeAction = value;
                OnPropertyChanged();
            }
        }
    }

}
