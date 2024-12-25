using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace DirectPackageInstaller.ViewModels
{
    public class SelectViewModel : DialogModel
    {
        private string[] _Options = null;
        public string[] Options
        {
            get => _Options; set => this.RaiseAndSetIfChanged(ref _Options, value);
        }

        private string _Caption = "PKG:";
        public string Caption
        {
            get => _Caption;
            set => this.RaiseAndSetIfChanged(ref _Caption, value);
        }
    }
}