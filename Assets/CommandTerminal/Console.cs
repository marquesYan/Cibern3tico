using UnityEngine;
using System.IO;
using System.Collections;

namespace CommandTerminal
{
    public class Console : Terminal {
        [Range(0.001f, 0.00001f)]
        [SerializeField] public float boot_delay = 0.001f; 
        [SerializeField] public string hostname;
        [SerializeField] public string linux_kern_version = "5.4.98-1.fc25.x86_64";
        bool started_boot_msg = false;

        IEnumerator LoadLinuxSystem() {
            yield return StartCoroutine(LogBootMessage());
            yield return StartCoroutine(LoadLoginSystem());

            draw_shell = true;
        }

        protected IEnumerator LogBootMessage() {
            string path = Path.Combine(Application.dataPath, "Resources", "boot.txt");
            string[] lines = File.ReadAllLines(path);

            yield return new WaitForSeconds(1);

            Log("[    0.000000] Linux version " + linux_kern_version + 
                " (user@build-fedora4) (gcc version 6.4.1 20170727 (Red Hat 6.4.1-1) " +
                "(GCC)) #1 SMP Wed Feb 17 01:49:26 UTC 2021");

            yield return new WaitForSeconds(2);
            
            float line_delay;
            for(int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                Log(line);
                ScrollAllDown();

                line_delay = i * boot_delay;

                yield return new WaitForSeconds(Random.Range(0.01f - line_delay, 0.1f - line_delay));
            }

            ScrollAllDown();
        }

        protected IEnumerator LoadLoginSystem() {
            Log("Initializing login");
            yield return new WaitForSeconds(2);

            Buffer.Clear();
            Log("");
            Log("Fedora 32 (Thirty Two)");
            Log("Kernel " + linux_kern_version + " on an x86_64 (hvc0)");
            Log("");
            Log(hostname + " login: ");
        }

        void OnGUI() {
            if (initial_open && ! started_boot_msg) {
                started_boot_msg = true;
                draw_shell = false;
                StartCoroutine(LoadLinuxSystem());
            }

            base.OnGUI();
        }
    }
}