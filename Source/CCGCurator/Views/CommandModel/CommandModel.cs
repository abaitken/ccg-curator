namespace CCGCurator.Views.CommandModel
{
    abstract class CommandModel<T> : CommandModelBase
    {
        public sealed override bool CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }

        protected virtual bool CanExecute(T parameter)
        {
            return true;
        }

        public sealed override void Execute(object parameter)
        {
            Execute((T)parameter);
        }

        protected abstract void Execute(T parameter);
    }

    abstract class CommandModel : CommandModelBase
    {
        public sealed override bool CanExecute(object parameter)
        {
            return CanExecute();
        }

        protected virtual bool CanExecute()
        {
            return true;
        }

        public sealed override void Execute(object parameter)
        {
            Execute();
        }

        protected abstract void Execute();
    }
}