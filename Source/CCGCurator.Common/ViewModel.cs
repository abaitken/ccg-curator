﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CCGCurator.Common
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool viewLoaded;

        public void ViewLoaded(Window window)
        {
            if(viewLoaded)
                return;
            viewLoaded = true;
            View = window;
            OnViewLoaded(window);
        }

        protected Window View { get; private set; }

        protected virtual void OnViewLoaded(Window window)
        {
        }
    }
}
