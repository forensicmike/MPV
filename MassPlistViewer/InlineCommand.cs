using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MassPlistViewer
{
    public class InlineCommand : ICommand
    {
        public InlineCommand(Action<object> commandAction)
        {
            CommandAction = commandAction;
        }

        public event EventHandler CanExecuteChanged = (o, e) => { };

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CommandExecuted = (o, e) => { };
        public object Result { get; set; }
        public Action<object> CommandAction { get; set; }

        public void Execute(object parameter)
        {
            CommandAction(parameter);

            CommandExecuted(this, new EventArgs());
        }
    }
}
