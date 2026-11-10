using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class AudioManagerr : DinoBehaviourScript
{
    protected static AudioManagerr instance;
    public static AudioManagerr Instance => instance;
    [SerializeField] protected Sound[] musicSound, sfxSound;
    [SerializeField] protected AudioSource musicSource, sfxSource, footStepSource;
    public AudioSource FootStepSource => footStepSource;
    public AudioSource SfxSource => sfxSource;
    protected override void Start()
    {
        base.Start();
        this.ClockMusic();
        if (LostTreasureSceneHelper.IsAncientCave(SceneManager.GetActiveScene().name))
        {
            OpenMusic();
            PlayMusic("MusicInsideCave");
        }
    }
    protected override void Awake()
    {
        base.Awake();
        if (AudioManagerr.instance != null) Debug.LogWarning("Only 1 AudioManagerr allow to exist");
        AudioManagerr.instance = this;
        
    }
    public void ClockMusic()
    {
        if (this.musicSource != null) this.musicSource.gameObject.SetActive(false);
    }
    public void OpenMusic()
    {
        if (this.musicSource != null) this.musicSource.gameObject.SetActive(true);
    }
    public void PlayMusic(string name)
    {
        if (this.musicSource == null) return;
        Sound s = Array.Find(musicSound, sound => sound.soundName.ToString() == name);
        if (s == null) return;
        this.musicSource.clip = s.clip;
        this.musicSource.Play();
    }
    public void PlayMusic(string name, float volume)
    {
        if (this.musicSource == null) return;
        Sound s = Array.Find(musicSound, sound => sound.soundName.ToString() == name);
        if (s == null) return;
        this.musicSource.clip = s.clip;
        this.musicSource.volume = volume;
        this.musicSource.Play();
    }
    public void StopMusic()
    {
        if (this.musicSource != null) this.musicSource.Stop();
    }
    public void PlaySFX(string name)
    {
        if (this.sfxSource == null) return;
        Sound s = Array.Find(sfxSound, sound => sound.soundName.ToString() == name);
        if (s == null) return;
        this.sfxSource.PlayOneShot(s.clip);
    }
    public void PlaySFX(string name, float volume)
    {
        if (this.sfxSource == null) return;
        Sound s = Array.Find(sfxSound, sound => sound.soundName.ToString() == name);
        if (s == null) return;
        this.sfxSource.PlayOneShot(s.clip, volume);
    }
    private void OnDestroy()
    {
        if (AudioManagerr.instance == this) AudioManagerr.instance = null;
    }
}
