using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource BriefingAudio;

    public GameObject AudioSources;

    protected AudioSource CurrentSong;

    protected List<AudioSource> Songs;

    protected int SongIndex;

    // Start is called before the first frame update
    void Start()
    {
        Songs = new List<AudioSource>();

        SongIndex = 0;

        PlaySong(BriefingAudio);

        foreach (Transform transform in AudioSources.transform) {
            AudioSource audio = transform.gameObject.GetComponent<AudioSource>();
            if (audio != null) {
                Songs.Add(audio);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!CurrentSong.isPlaying) {
            if (SongIndex >= Songs.Count) {
                SongIndex = 0;
            }

            PlaySong(Songs[SongIndex]);

            SongIndex++;
        }
    }

    void PlaySong(AudioSource audio) {
        CurrentSong = audio;
        CurrentSong.Play();
    }
}
