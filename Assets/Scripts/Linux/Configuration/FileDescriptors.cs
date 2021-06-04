using System.Collections.Generic;
using Linux.IO;
using UnityEngine;

namespace Linux.Configuration
{
    public class FileDescriptorsTable {
        protected Dictionary<Process, Dictionary<int, ITextIO>> Descriptors;

        protected Dictionary<int, List<int>> Counters;

        public int HighestPid {
            get {
                int highestPid = 0;

                foreach (int pid in Counters.Keys) {
                    if (pid > highestPid) {
                        highestPid = pid;
                    }
                }

                return highestPid;
            }
        }

        public FileDescriptorsTable() {
            Descriptors = new Dictionary<Process, Dictionary<int, ITextIO>>();
            Counters = new Dictionary<int, List<int>>();
        }

        public void Remove(Process process) {
            if (!HasProcess(process)) {
                throw new System.ArgumentException(
                    $"Process with PID '{process.Pid}' does not exist"
                );
            }

            Descriptors.Remove(process);
            Counters.Remove(process.Pid);
        }

        public ITextIO LookupFd(Process process, int fd) {
            if (!HasProcess(process)) {
                throw new System.ArgumentException(
                    $"Process with PID '{process.Pid}' does not exist"
                );
            }

            if (!Descriptors[process].ContainsKey(fd)) {
                return null;
            }

            return Descriptors[process][fd];
        }

        public Process LookupPid(int pid) {
            if (Counters.ContainsKey(pid)) {
                return GetProcesses().Find(p => p.Pid == pid);
            }

            return null;
        }

        public bool HasProcess(Process process) {
            return Descriptors.ContainsKey(process);
        }

        public List<Process> GetProcesses() {
            return new List<Process>(Descriptors.Keys);
        }

        public void Add(Process process) {
            if (HasProcess(process)) {
                throw new System.ArgumentException(
                    $"Process with PID '{process.Pid}' already exists"
                );
            }

            Descriptors[process] = new Dictionary<int, ITextIO>();
            Counters[process.Pid] = new List<int>();
        }

        public void Add(Process process, ITextIO stream, int fd) {
            if (!HasProcess(process)) {
                Add(process);
            }

            if (LookupFd(process, fd) != null) {
                throw new System.ArgumentException(
                    $"Fd already exists: {fd}"
                );
            }

            Debug.Log("adding stream to PID: " + process.Pid);

            foreach(KeyValuePair<int, ITextIO> kvp in Descriptors[process]) {
                Debug.Log("existing fd: " + kvp.Key);           
                Debug.Log("existing stream: " + kvp.Value);           
            }

            Descriptors[process].Add(fd, stream);
            Counters[process.Pid].Add(fd);
        }

        public int GetAvailableFd(Process process) {
            if (!HasProcess(process)) {                
                return 0;
            }

            int candidate = 0;

            while (LookupFd(process, candidate) != null) {
                candidate++;
            }

            return candidate;
        }
    }
}