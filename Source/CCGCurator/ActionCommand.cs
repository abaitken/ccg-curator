using System;
using System.Windows.Input;

namespace CCGCurator
{
    internal class ActionCommand : ICommand
    {
        private readonly Func<bool> canExecute;

        public ActionCommand(string key, Action<object> action, KeyGesture keyGesture)
        : this(key, action, () => true, keyGesture)
        {
        }
        public ActionCommand(string key, Action<object> action, Func<bool> canExecute, KeyGesture keyGesture)
        {
            this.canExecute = canExecute;
            Key = key;
            Action = action;
            KeyGesture = keyGesture;
        }

        public string Key { get; }
        public Action<object> Action { get; }
        public KeyGesture KeyGesture { get; }

        public ICommand Command => this;

        public bool CanExecute(object parameter)
        {
            return canExecute();
        }

        public void Execute(object parameter)
        {
            Action(parameter);
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

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}