#region

using System.Windows;
using System.Windows.Input;
using ProcessTools;

#endregion

namespace ProcessTools.Core.Commands
{
    public class ActivateWindowCommand : CommandBase<ActivateWindowCommand>
    {
        public override void Execute(object parameter)
        {
            MainWindow.Window.WindowState = WindowState.Normal;
            CommandManager.InvalidateRequerySuggested();
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }
    }
}