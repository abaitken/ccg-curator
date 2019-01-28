using System;
using System.Windows.Input;

namespace CCGCurator
{
    internal class ActionCommand : ICommand
    {
        public ActionCommand(string key, Action action, KeyGesture keyGesture)
        {
            Key = key;
            Action = action;
            KeyGesture = keyGesture;
        }

        public string Key { get; }
        public Action Action { get; }
        public KeyGesture KeyGesture { get; }

        public ICommand Command => this;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Action();
        }

        public event EventHandler CanExecuteChanged;

        public InputBinding CreateInputBinding()
        {
            return new KeyBinding(this, KeyGesture);
        }

        public CommandBinding CreateCommandBinding()
        {
            return new CommandBinding(this);
        }
    }
}