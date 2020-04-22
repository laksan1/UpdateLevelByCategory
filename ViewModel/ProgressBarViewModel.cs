using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateLevelByCategory.View;
using UpdateLevelByCategory.Model;

namespace UpdateLevelByCategory.ViewModel
{
   public class ProgressBarViewModel
    {
        ProgressBarWindow ProgressBarWindow { get; set; }
        internal RevitModelClass RevitModel { get; set; }



        public ProgressBarViewModel (RevitModelClass _rv)
        {

            RevitModel = _rv;

        }
    }
}
