using System;
using System.Collections.Generic;
using Linux.IO;
using Linux.FileSystem;
using Linux.Sys.RunTime;

namespace Linux.Library.ArgumentParser
{    
    public class GenericArgParser {
        protected UserSpace UserSpace;

        protected string Usage;

        protected string ProgName;

        protected string Description;

        protected OptionSet OptionSet;

        protected bool IsShowingHelp = false;

        public GenericArgParser(
            UserSpace userSpace,
            string usage,
            string description,
            string progName
        ) {
            UserSpace = userSpace;
            OptionSet = new OptionSet();

            Description = description;
            ProgName = progName;

            Usage = string.Format(usage, ProgName);

            AddArgument<string>(
                "h|help",
                "Show help message",
                v => IsShowingHelp = true
            );
        }

        public GenericArgParser(
            UserSpace userSpace,
            string usage,
            string description
        ) : this(
                userSpace,
                usage, 
                description, 
                PathUtils.BaseName(userSpace.Api.GetExecutable())
            ) {}

        public void AddArgument<T>(
            string prototype, 
            string description,
            Action<T> action
        ) {
			OptionSet.Add(prototype, description, action);
		}

        public List<string> Parse(string[] cliArgs) {
            string[] args = cliArgs ?? UserSpace.Api.GetArgs();

            List<string> result;

            try {
                result = OptionSet.Parse(args);
            } catch (OptionException exc) {
                UserSpace.Stderr.WriteLine($"{ProgName}: {exc.Message}");
                ShowHelpInfo();
                UserSpace.Exit(127);

                // Just compiler awareness
                return null;
            }

            result.RemoveAt(0);

            if (IsShowingHelp) {
                ShowHelp();
                UserSpace.Exit(127);

                // Just compiler awareness
                return null;
            }

            return result;
        }

        public List<string> Parse() {
            return Parse(null);
        }

        public void ShowHelp() {
            UserSpace.Stderr.WriteLine(Usage);
            UserSpace.Stderr.WriteLine($"\t{Description}");
            UserSpace.Stderr.WriteLine("");
            OptionSet.WriteOptionDescriptions(UserSpace.Stderr);
        }

        public void ShowHelpInfo() {
            UserSpace.Stderr.WriteLine($"{ProgName}: Try {ProgName} --help for more information");
        }
    }
}