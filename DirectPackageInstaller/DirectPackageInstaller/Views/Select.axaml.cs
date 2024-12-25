using System;
using Avalonia.Controls;
using DirectPackageInstaller.UIBase;
using DirectPackageInstaller.ViewModels;

namespace DirectPackageInstaller.Views
{
    public partial class Select : DialogWindow
    {
        private string[] Choices;
        public string Choice { get; private set; }


        public SelectViewModel ViewModel { 
            get {
                if (View == null)
                    View = this.Find<SelectView>("View");

                if (View!.DataContext == null)
                    View.DataContext = new SelectViewModel();

                return (SelectViewModel)View.DataContext;
            } 
        }
        public string Caption { get => ViewModel.Caption; set => ViewModel.Caption = value; } 

        public Select(string[] Choices) : this()
        {
            this.Choices = Choices;
        }

        public Select()
        {
            InitializeComponent();
            
            if (DataContext == null)
                DataContext = new SelectViewModel();

            View = this.Find<SelectView>("View");
            View.DataContext = DataContext;
            
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            View.Initialize(this, Choices, (Item) => Choice = Item);
        }
    }
}