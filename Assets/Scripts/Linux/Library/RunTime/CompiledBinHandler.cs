using Linux.IO;
using Linux.Library;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using UnityEngine;

namespace Linux.Library.RunTime
{
    public class CompiledBinHandler : AbstractRunTimeHandler {
        public CompiledBinHandler(KernelSpace api) : base(api) {}

        public override bool IsFileSupported(File executable){
            return executable is CompiledBin;
        }

        public override int Execute(UserSpace procSpace, File executable) {
            CompiledBin bin = (CompiledBin)executable;
            int returnCode = 255;

            try {
                returnCode = bin.Execute(procSpace);
            }
            
            catch (ExitProcessException exc) {
                returnCode = exc.ExitCode;
            }
            
            catch (System.Exception exception) {
                procSpace.Print($"{bin.Name}: {exception.Message}");
                Debug.Log(exception.ToString());
            }

            return returnCode;
        }
    }
}