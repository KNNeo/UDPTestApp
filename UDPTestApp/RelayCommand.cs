using System;
using System.Threading;
using System.Windows.Input;

namespace UDPTestApp
{
    public class RelayCommand : ICommand
    {
        private Thread newThread;
        private Action _work;
        public RelayCommand(Action work)
        {
            _work = work;
        }

        public bool CanExecute(object parameter)
        {
            return true;
            //conditions for passing thread here instead?
            //or always true then sort at Execute?
        }

        public void Execute(object parameter)
        {
            if (newThread == null)
            {
                newThread = new Thread(() => _work());
                //_work();
                newThread.Start();
            }
            else if (newThread.ThreadState != ThreadState.Stopped)
            {
                //newThread.Abort(); if need to cancel use same thing, have to cancel before starting anew
                Console.WriteLine("Action in progress: Do not interrupt!");
            }
            else
            {
                newThread = new Thread(() => _work());
                //_work();
                newThread.Start();
                Console.WriteLine("Action restart!");
            }
        }

        public event EventHandler CanExecuteChanged;
    }
}
