using Linux.Sys.RunTime;
using Linux.Library.ShellInterpreter;

namespace Linux.Library.ShellInterpreter.Builtins
{    
    public abstract class AbstractShellBuiltin {
        protected UserSpace UserSpace;

        protected BashProcess Bash;

        public AbstractShellBuiltin(BashProcess bash) {
            Bash = bash;
            UserSpace = Bash.UserSpace;
        }

        public abstract int Execute(string[] args);
    }
}