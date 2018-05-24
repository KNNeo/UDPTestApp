using System;
using System.Windows.Input;

namespace UDPTestApp.Base
{
    public class RelayCommand : ICommand
    {
        //initialisation
        private Action _work;
        public RelayCommand(Action work)
        {
            _work = work;
        }

        //criteria not set in this class
        public bool CanExecute(object parameter)
        {
            return true;
        }

        //just runs action object passed
        public void Execute(object parameter)
        {
            _work();
        }

        public event EventHandler CanExecuteChanged;
    }
}
