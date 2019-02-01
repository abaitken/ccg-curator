using System;
using System.Windows.Input;

namespace CCGCurator.Views.CommandModel
{
    abstract class CommandModelBase : ICommand
    {
        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);

        public event EventHandler CanExecuteChanged;

        protected void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public InputBinding CreateInputBinding(KeyGesture gesture)
        {
            return new KeyBinding(this, gesture);
        }

        public CommandBinding CreateCommandBinding()
        {
            return new CommandBinding(this);
        }

        public InputBinding CreateKeyBinding(Key key)
        {
            return new KeyBinding
            {
                Command = this,
                Key = key
            };
        }
    }
}