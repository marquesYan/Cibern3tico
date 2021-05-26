using UnityEngine;
using System.IO;
using System.Text;
using System.Collections;

namespace CommandTerminal
{
    public class Console : Terminal {
        [Range(0.001f, 0.00001f)]
        [SerializeField] public float boot_delay = 0.001f; 
        bool started_boot_msg = false;

        IEnumerator LoadBootMessage() {
            string path = Path.Combine(Application.dataPath, "Resources", "boot.txt");
            string[] lines = File.ReadAllLines(path);

            float seconds;

            yield return new WaitForSeconds(1);
            
            for(int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                Log(line);

                if (i < 2) {
                    seconds = 2f;
                } else {
                    seconds = Random.Range(0.01f - (i * boot_delay), 0.1f - (i * boot_delay));
                }

                scroll_position.y = int.MaxValue;

                // scroll_position.y = int.MaxValue;
                yield return new WaitForSeconds(seconds);
            }

            scroll_position.y = int.MaxValue;
            draw_shell = true;
        }

        void OnGUI() {
            if (initial_open && ! started_boot_msg) {
                started_boot_msg = true;
                draw_shell = false;
                StartCoroutine(LoadBootMessage());
            }

            base.OnGUI();
        }
    }
}