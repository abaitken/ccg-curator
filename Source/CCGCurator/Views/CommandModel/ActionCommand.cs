using System;
using System.Windows.Input;

namespace CCGCurator.Views.CommandModel
{
    internal class ActionCommand<T> : CommandModel<T>
    {
        private readonly Func<T, bool> canExecute;
        private readonly Action<T> action;

        public ActionCommand(Action<T> action)
        : this(action, (obj) => true)
        {
        }

        public ActionCommand(Action<T> action, Func<T, bool> canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        protected override bool CanExecute(T parameter)
        {
            return canExecute(parameter);
        }

        protected override void Execute(T parameter)
        {
            action(parameter);
        }
    }

    internal class ActionCommand : CommandModel
    {
        private readonly Func<bool> canExecute;
        private readonly Action action;

        public ActionCommand(Action action)
            : this(action, () => true)
        {
        }

        public ActionCommand(Action action, Func<bool> canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        protected override bool CanExecute()
        {
            return canExecute();
        }

        protected override void Execute()
        {
            action();
        }
    }
}