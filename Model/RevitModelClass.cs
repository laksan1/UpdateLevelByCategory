using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using UpdateLevelByCategory.ViewModel;
using UpdateLevelByCategory.Model;
using System.Linq;
using UpdateLevelByCategory.MVVM;
using UpdateLevelByCategory.View;
using Excel = Microsoft.Office.Interop.Excel;
using System.Globalization;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using System.Threading;
using Autodesk.Revit.DB.Plumbing;

namespace UpdateLevelByCategory.Model
{
    public class RevitModelClass
    {
        public ProgressBarViewModel progressBarViewModel;
        public ProgressBarWindow progressBarWindow;
        public static EventWaitHandle _progressWindowWaitHandle;
        public UIApplication _uiApplication;
        public Application _application;
        public Document _document;
        public readonly UIDocument _uiDocument;
        public string offsetNumberR;
        public IList<Element> collectionElementsId;
        public string messageGroup = "";
        public int countCollector { get; set; }

        public int i = 0;
     
        //Метод обновления уровня
        public bool UpdateLevel(IList<Element> _listElementsSelected)
        {
            double offsetOfCustomer;

            if ( !string.IsNullOrEmpty(offsetNumberR))
            {
                offsetOfCustomer = MeterToPounds(ConvertToDouble(offsetNumberR));

            }

            else
            {
                 offsetOfCustomer = 0.0;

            }
           


            if (_listElementsSelected == null)
                {
                    TaskDialog.Show("Error", "Count elements = 0");
                    return false;
                }

            else
            {

                using (var tr = new Transaction(_document, "Update level"))
                {
                    tr.Start();

                    FilteredElementCollector collector = new FilteredElementCollector(_document).OfClass(typeof(PipeInsulation));

                    //Коллекция всех уровней в проекте
                    IEnumerable<Level> levels = new FilteredElementCollector(_document).OfClass(typeof(Level)).Cast<Level>(); 

                    countCollector = _listElementsSelected.Count();
                    string nameTreadCurrentOld = Thread.CurrentThread.Name;
                      
                    using (_progressWindowWaitHandle = new AutoResetEvent(false))
                    {
                        //Создание нового потока для прогрессбара
                        Thread newprogWindowThread = new Thread(new ThreadStart(ShowProgWindow));
                        newprogWindowThread.SetApartmentState(ApartmentState.STA);
                        newprogWindowThread.IsBackground = true;
                        newprogWindowThread.Start();

                        _progressWindowWaitHandle.WaitOne();
                    }
                     
                    Level minLevelinProject = GetMinimalLevel();

                    foreach (Element el in _listElementsSelected)
                    {
                        ElementId levelBaseElement = el.LevelId;

                        Level newLevel = null; ;

                        string name_Element = el.Name;

                        //Обновление прогресс бара
                        this.progressBarWindow.UpdateProgress(name_Element , i, countCollector);


                        //Кроме групп
                        if (el.GroupId.IntegerValue != -1) 
                        {
                            var NameGroup = _document.GetElement(el.GroupId).Name;

                            if (!messageGroup.Contains(NameGroup))
                            {
                                messageGroup += "\u0022" + NameGroup + "\u0022" + "\n";

                            }
                        }

                        //Если элемент семейство
                        if (el.GroupId.IntegerValue == -1) 
                        {
                                if (el is FamilyInstance fi)

                                {
                                    //Параметр уровня
                                    Parameter ParameterElementLevel = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM); 
                                    string nameParameterLevel = ParameterElementLevel.Definition.Name;
                                    //Параметр привязки
                                    Parameter paramOffSet = el.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);
                                    LocationPoint LocalPointElement = el.Location as LocationPoint;

                                //Смещение пользователя
                                BoundingBoxXYZ bb = el.get_BoundingBox(_document.ActiveView);
                                double value_Z_ElementBoundingBox = bb.Min.Z;

                                double value_Z_Element;

                                if (LocalPointElement != null)
                                {

                                    value_Z_Element = LocalPointElement.Point.Z; //Z элемента 

                                }

                                else
                                {
                                    value_Z_Element = value_Z_ElementBoundingBox;

                                }
                                  
                                    Level levelSameMinBB = _document.GetElement(el.LevelId) as Level;

                                    if (paramOffSet.AsDouble() == 0)

                                    {
                                        if (!fi.CanFlipWorkPlane)
                                        {
                                            newLevel = levelSameMinBB;
                                        }
                                        else
                                        {

                                            if (!fi.HasSpatialElementCalculationPoint)
                                            {

                                            //Для минимальной Z семейства, независимо от самого семейства
                                            newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels); 
                                            }
                                            else
                                            {
                                            //Для семейства с точкой расчета площади
                                            newLevel = GetNearestLevelToElements(fi.GetSpatialElementCalculationPoint().Z + offsetOfCustomer, levels);

                                            }

                                        }

                                    }


                                    else
                                    {
                                        if (!fi.HasSpatialElementCalculationPoint)
                                        {

                                            newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels); 

                                        }
                                        else
                                        {
                                            newLevel = GetNearestLevelToElements(fi.GetSpatialElementCalculationPoint().Z + offsetOfCustomer, levels); 

                                        }
                                    }

                                    if (newLevel == null)
                                    {
                                        newLevel = minLevelinProject;
                                    }

                                    Parameter parameterlElement = el.LookupParameter("ADSK_Этаж");
                                    Parameter parameterLevel = newLevel.LookupParameter("ADSK_Этаж");

                                /*
                                if (parameterLevel == null)
                                {

                                    TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                    progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close));
                                    return false;

                                }

                                string nameParLev = parameterLevel.AsString();

                                    if (string.IsNullOrEmpty(nameParLev))
                                    {

                                        TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                        progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close));
                                        return false;

                                    }
                            
                                */
                                //Если уровни не совпали
                                if (newLevel.Id != levelBaseElement && !ParameterElementLevel.IsReadOnly)
                                    {
                                    double LocationZFAmily = PoundsToM(value_Z_Element);
                                    double ZnewLevel = PoundsToM(newLevel.Elevation);
                                    //double ZPointCulculate = PoundsToM(fi.GetSpatialElementCalculationPoint().Z);
                                    double ZBoxMin = PoundsToM(value_Z_ElementBoundingBox);

                                    string NameLevel = newLevel.Name;
                                    ParameterElementLevel.Set(newLevel.Id);//Задает новый уровень
                                    paramOffSet.Set(value_Z_Element - newLevel.Elevation);//Задает новое смещение      

                                    }
                                }

                            //Если элемент системный
                           else if (el as MEPCurve!= null)
                           {

                                BoundingBoxXYZ bb = el.get_BoundingBox(_document.ActiveView);
                                double value_Z_ElementBoundingBox = bb.Min.Z;
                                MEPCurve mEPCurve = el as MEPCurve;

                                //Параметр уровня
                                Parameter ParameterElementLevel = el.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);

                                //Параметр привязки
                                Parameter paramOffSetStart = el.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                                Element elForLevel = el;

                                //double value_Z_ElementBoundingBox = el.get_BoundingBox(_document.ActiveView).Min.Z;

                                //Если элемент является MEPCurve
                                if (mEPCurve != null)
                                {
                                    LocationCurve lc = mEPCurve.Location as LocationCurve;

                                    double value_Z_Element = GetMinPointCurve(lc);

                                    newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels);

                                    if (newLevel == null)
                                    {
                                        newLevel = minLevelinProject;
                                    }

                                    Parameter parameterlElement = elForLevel.LookupParameter("ADSK_Этаж");
                                    Parameter parameterLevel = newLevel.LookupParameter("ADSK_Этаж");

                                    /*
                                    if (parameterLevel == null)
                                    {

                                        TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                        progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close)); // Close ProgressBar
                                        return false;

                                    }

                                    string nameParLev = parameterLevel.AsString();
                                    */
                                    //Если уровни не совпали
                                    if (newLevel.Id != levelBaseElement)
                                    {
                                        string NameLevel = newLevel.Name;
                                        ParameterElementLevel.Set(newLevel.Id);//Задает новый уровень

                                    }

                                }
                           }

                            i++;
                        }
                  

                }//Конец foreach

                    tr.Commit();
                }


                progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close)); // Close ProgressBar

                if (!string.IsNullOrEmpty(messageGroup))
                {
                    TaskDialog.Show("Missed Groups", "Missed groups:\n" + messageGroup + "\n");
                }
                else
                {
                   // progressBarWindow.Close();
                    TaskDialog.Show("Result", i + " element(s) were analyzed");
                }
                return true;
            }
        }

        //Метод отображения прогрессбара
        public void ShowProgWindow()

        {
            progressBarViewModel = new ProgressBarViewModel(this);
            progressBarWindow = new ProgressBarWindow(progressBarViewModel);
            string nameTreadCurrentNew = Thread.CurrentThread.Name;
            progressBarWindow.Show();

            progressBarWindow.Closed += new EventHandler(progressBarWindow.MetroWindow_Closed);

            _progressWindowWaitHandle.Set();

            System.Windows.Threading.Dispatcher.Run();
            
        }
    

           
        public void SetParameterAdskLevelFamily(Parameter _parElemnt, string _parLevel, FamilyInstance _fi)
        {
                //Получение вложенных семейств
                var listNestedFamilies = _fi.GetSubComponentIds().ToList();
                if (listNestedFamilies.Count != 0)
                {
                    foreach (var sbel in listNestedFamilies)
                      {
                   
                        var elSub = _document.GetElement(sbel);

                    FamilyInstance fIsubFamiy = elSub as FamilyInstance; //Для рекурсии

                    string nameSubEl = elSub.Name;
                    Parameter _parlvl = elSub.LookupParameter("ADSK_Этаж");

                        if (_parlvl != null)
                        {
                            _parlvl.Set(_parLevel);
                        SetParameterAdskLevelFamily(_parElemnt, _parLevel, fIsubFamiy);
                        continue;

                        }
                      }
                    _parElemnt.Set(_parLevel);

                }

                else
                {
                    _parElemnt.Set(_parLevel);

                }
        }

        double GetMinPointCurve(LocationCurve _lc)
        {

            double Zpt1 = _lc.Curve.GetEndPoint(0).Z;
            double mpt1 = PoundsToM(Zpt1);
            double Zpt2 = _lc.Curve.GetEndPoint(1).Z;
            double mpt2 = PoundsToM(Zpt2);

            //Применить тернарный оператор
            if (Zpt2 > Zpt1)
            {
                return Zpt1;
            }

            else
            {

                return Zpt2;
            }

        }

        public double PoundsToM(double value)
        {
            return Math.Round(UnitUtils.ConvertFromInternalUnits(value, DisplayUnitType.DUT_MILLIMETERS), 3);
        }

        public double MeterToPounds(double value)
        {
            return UnitUtils.ConvertToInternalUnits(value, DisplayUnitType.DUT_MILLIMETERS);
        }

        //Нахождение минимального уровня в проекте
        public Level GetMinimalLevel()
        {

            //Создание словаря для поиска минмальной высоты уровня Dictionary<уровень, разница между Z элемента и Z уровня>
            Dictionary<Level, double> PointsLevels = new Dictionary<Level, double>(); //Создание словаря для поиска уровня с минимальной высотой

            var colLevel = new FilteredElementCollector(_document).OfClass(typeof(Level)).Cast<Level>(); //Коллекция всех уровней в проекте
            foreach (Level lv in colLevel)
            {
                double minZ = lv.ProjectElevation;
                PointsLevels.Add(lv, minZ);

            }

            //Нахождение ближайшего уровня к элементу
            Level minLEvel = PointsLevels.Where(e => e.Value == PointsLevels.Min(e2 => e2.Value)).FirstOrDefault().Key;

           // Level minLEvel = colLevel.Where(g => g.ProjectElevation == colLevel.Min(f => f.ProjectElevation)).First();

            return minLEvel;
        }

        //Нахождение ближайшего уровня к элементу
        public Level GetNearestLevelToElements(double _valueZElement, IEnumerable<Level> _levels)
        {
            //Создание словаря для поиска минмальной высоты уровня Dictionary<уровень, разница между Z элемента и Z уровня>
            Dictionary<Level, double> PointsLevels = new Dictionary<Level, double>(); //Создание словаря для поиска уровня с минимальной высотой

           
            foreach (Level lv in _levels)
            {
                string aaa = lv.Name;

                double value_Z_Level = lv.ProjectElevation;
                if (value_Z_Level < _valueZElement)
                {
                    double dif = _valueZElement - value_Z_Level;
                    PointsLevels.Add(lv, dif);
                }

            }
            //Нахождение ближайшего уровня к элементу
            Level newLevel = PointsLevels.Where(e => e.Value == PointsLevels.Min(e2 => e2.Value)).FirstOrDefault().Key;

            return newLevel;

        }
        public string GetParameterValue(Parameter parameter)
        {
            string s;
            switch (parameter.StorageType)
            {
                case StorageType.Double:

                    s = RealString(parameter.AsDouble());
                    break;

                case StorageType.Integer:
                    s = parameter.AsInteger().ToString();
                    break;

                case StorageType.String:
                    s = parameter.AsString();
                    break;

                case StorageType.ElementId:
                    s = parameter.AsElementId().IntegerValue.ToString();
                    break;

                case StorageType.None:
                    s = "";
                    break;

                default:
                    s = "0.0";
                    break;
            }
            return s;
        }

        public static string RealString(double a)
        {
            return a.ToString();
        }

        public static double ConvertToDouble(string Value)
        {
            if (Value == null)
            {
                return 0;
            }
            else
            {
                double OutVal;
                double.TryParse(Value, out OutVal);

                if (double.IsNaN(OutVal) || double.IsInfinity(OutVal))
                {
                    return 0;
                }
                return OutVal;
            }
        }

        public RevitModelClass(UIApplication uiapp)
        {
            _uiApplication = uiapp;
            _application = _uiApplication.Application;
            _uiDocument = _uiApplication.ActiveUIDocument;
            _document = _uiDocument.Document;

        }


        public bool GetRadioButton(BuiltInCategory bt)
        {

            return UpdateLevel(new FilteredElementCollector(_document).OfCategory(bt).ToList());
        }

        public bool GetRadioButtonForLevel(BuiltInCategory bt)
        {

            return SetAdskLevel(new FilteredElementCollector(_document).OfCategory(bt).ToList());
        }

        public static IList<Element> GetSelectedElements(UIDocument _uid)
        {
            try
            {
                var collector = _uid.Selection.PickElementsByRectangle("Select by rectangle");

                return collector;

            }


            catch
            {
                TaskDialog.Show("Revit", "You haven't selected any elements.");
                return null;
            }
        }

        //Разгруппировка
        public void UnGroupAll()
        {

            using (Transaction t = new Transaction(_document, "Ungroup All Groups"))
            {
                t.Start();

                var allGroups = new FilteredElementCollector(_document).OfClass(typeof(Group)).Cast<Group>();
                foreach (Group  g in allGroups)
                {

                    g.UngroupMembers();
                }
                   
           
                t.Commit();
            }
        }

        #region Повтор кода
        //Установить только ADSK Этаж при нажатие на флажок
        public bool SetAdskLevel(IList<Element> _listElements)
        {
            double offsetOfCustomer;

            if (!string.IsNullOrEmpty(offsetNumberR))
            {
                offsetOfCustomer = MeterToPounds(ConvertToDouble(offsetNumberR));

            }

            else
            {
                offsetOfCustomer = 0.0;

            }


            if (_listElements == null)
            {
                TaskDialog.Show("Error", "Count elements = 0");
                return false;
            }

            else
            {

                using (var tr = new Transaction(_document, "Update level"))
                {
                    tr.Start();

                    FilteredElementCollector collector = new FilteredElementCollector(_document).OfClass(typeof(PipeInsulation));

                    IEnumerable<Level> levels = new FilteredElementCollector(_document).OfClass(typeof(Level)).Cast<Level>(); //Коллекция всех уровней в проекте

                    countCollector = _listElements.Count();
                    string nameTreadCurrentOld = Thread.CurrentThread.Name;

                    using (_progressWindowWaitHandle = new AutoResetEvent(false))
                    {

                        Thread newprogWindowThread = new Thread(new ThreadStart(ShowProgWindow));
                        newprogWindowThread.SetApartmentState(ApartmentState.STA);
                        newprogWindowThread.IsBackground = true;
                        newprogWindowThread.Start();
                        _progressWindowWaitHandle.WaitOne();
                    }

                    Level minLevelinProject = GetMinimalLevel();

                    foreach (Element el in _listElements)
                    {
                        ElementId levelBaseElement = el.LevelId;

                        Level newLevel = null; ;

                        string name_Element = el.Name;

                        this.progressBarWindow.UpdateProgress(name_Element, i, countCollector);


                
                        if (el.GroupId.IntegerValue != -1) 
                        {
                            var NameGroup = _document.GetElement(el.GroupId).Name;

                            if (!messageGroup.Contains(NameGroup))
                            {
                                messageGroup += "\u0022" + NameGroup + "\u0022" + "\n";

                            }
                        }

                        if (el.GroupId.IntegerValue == -1) 
                        {

                            if (el is FamilyInstance fi)

                            {
                                Parameter ParameterElementLevel = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM); 
                                string nameParameterLevel = ParameterElementLevel.Definition.Name;
                                Parameter paramOffSet = el.get_Parameter(BuiltInParameter.INSTANCE_FREE_HOST_OFFSET_PARAM);

                                LocationPoint LocalPointElement = el.Location as LocationPoint;
                                BoundingBoxXYZ bb = el.get_BoundingBox(_document.ActiveView);
                                double value_Z_ElementBoundingBox = bb.Min.Z;

                                double value_Z_Element;

                                if (LocalPointElement != null)
                                {

                                    value_Z_Element = LocalPointElement.Point.Z; 

                                }

                                else
                                {
                                    value_Z_Element = value_Z_ElementBoundingBox;

                                }

                                Level levelSameMinBB = _document.GetElement(el.LevelId) as Level;

                                if (paramOffSet.AsDouble() == 0)

                                {
                                    if (!fi.CanFlipWorkPlane)
                                    {
                                        newLevel = levelSameMinBB;
                                    }
                                    else
                                    {

                                        if (!fi.HasSpatialElementCalculationPoint)
                                        {
                                            newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels); 
                                        }
                                        else
                                        {
                                            newLevel = GetNearestLevelToElements(fi.GetSpatialElementCalculationPoint().Z + offsetOfCustomer, levels);

                                        }

                                    }

                                }


                                else
                                {
                                    if (!fi.HasSpatialElementCalculationPoint)
                                    {
                                        newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels); 
                                    }
                                    else
                                    {
                                        newLevel = GetNearestLevelToElements(fi.GetSpatialElementCalculationPoint().Z + offsetOfCustomer, levels);

                                    }
                                }

                                if (newLevel == null)
                                {
                                    newLevel = minLevelinProject;
                                }

                                Parameter parameterlElement = el.LookupParameter("ADSK_Этаж");
                                Parameter parameterLevel = newLevel.LookupParameter("ADSK_Этаж");
                                if (parameterLevel == null)
                                {

                                    TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                    progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close));
                                    return false;


                                }
                                string nameParLev = parameterLevel.AsString();
                                if (string.IsNullOrEmpty(nameParLev))
                                {

                                    TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                    progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close));
                                    return false;


                                }

                             
                                if (parameterlElement != null && parameterLevel != null)
                                {

                                    SetParameterAdskLevelFamily(parameterlElement, nameParLev, fi);
                                }

                            
                            }

                            else if (el as MEPCurve != null)
                            {

                                BoundingBoxXYZ bb = el.get_BoundingBox(_document.ActiveView);
                                double value_Z_ElementBoundingBox = bb.Min.Z;
                                MEPCurve mEPCurve = el as MEPCurve;
                                Parameter ParameterElementLevel = el.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
                                Parameter paramOffSetStart = el.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                                Element elForLevel = el;


                                if (mEPCurve != null)
                                {
                                    LocationCurve lc = mEPCurve.Location as LocationCurve;

                                    double value_Z_Element = GetMinPointCurve(lc);

                                    newLevel = GetNearestLevelToElements(value_Z_ElementBoundingBox + offsetOfCustomer, levels); 
                                    if (newLevel == null)
                                    {
                                        newLevel = minLevelinProject;
                                    }

                                    Parameter parameterlElement = elForLevel.LookupParameter("ADSK_Этаж");
                                    Parameter parameterLevel = newLevel.LookupParameter("ADSK_Этаж");

                                    if (parameterLevel == null)
                                    {

                                        TaskDialog.Show("Error", $"Level - \u0022{newLevel.Name}\u0022 does not have the value \u0022ADSK_Этаж\u0022");
                                        progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close));
                                        return false;


                                    }

                                    string nameParLev = parameterLevel.AsString();


                                    if (parameterlElement != null && parameterLevel != null)
                                    {

                                        parameterlElement.Set(nameParLev);
                                    }

                                }
                            }

                            i++;
                        }


                    }

                    tr.Commit();
                }


                progressBarWindow.Dispatcher.Invoke(new Action(progressBarWindow.Close)); 

                if (!string.IsNullOrEmpty(messageGroup))
                {
                    TaskDialog.Show("Missed Groups", "Missed groups:\n" + messageGroup + "\n");
                }
                else
                {

                    TaskDialog.Show("Result", i + " element(s) were analyzed");
                }
                return true;
            }
        }
        #endregion

    }
}
