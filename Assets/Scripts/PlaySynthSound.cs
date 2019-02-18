using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaySynthSound : MonoBehaviour
{
    public AudioClip Clip;
    public SynthSound soundToBePlayed;

    [Range(0f, 1f)]
    public float volume = 0.5f;

    private int numFrames = -1;
    private AudioSource m_AudioSource;
    //public AnimationCurve m_AnimationCurve = new AnimationCurve();
    private int m_Time;

    private void Awake()
    {
        soundToBePlayed = SoundAnalysis.SplitSoundIntoFrequencies(Clip, 2048, 128);

        m_AudioSource = gameObject.GetComponent<AudioSource>();
        m_AudioSource = m_AudioSource ?? gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
        m_AudioSource.spatialBlend = 0;
        m_AudioSource.Stop();
    }

    void Update()
    {

    }

    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!m_AudioSource.isPlaying)
            {
                m_Time = 0;  //resets timer before playing sound
                m_AudioSource.Play();
            }
            else
            {
                m_AudioSource.Stop();
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        var tones = soundToBePlayed.GetToneAtIndex(m_Time);
        
        for (int i = 0; i < data.Length; i+=channels)
        {

            float value = 0;
            float offset = 0;

            foreach (var tone in tones)
            {
                var calcFunc = Mathf.Sin((i+offset) * (tone.frequency)/(soundToBePlayed.SampleFrequency) * (2 * Mathf.PI)) * tone.amplitude;
                value += calcFunc;
            }

            //smooth the Edges, very necesaary right now as every frequency is a sine and so start a 0 and every singel one goes up :P
            int distance = Mathf.Min(i, data.Length - i);
            if (distance < 200)
                value *= distance / 200;
            value *= volume;


            for (int j = 0; j < channels; j++)
            {
                data[i + j] = value;
            }
        }
        
        m_Time++;
        if (m_Time >= (soundToBePlayed.ToneCount))
        {
            m_Time = 0;
        }
    }

    public float CreateSine(int m_TimeIndex, float frequency, float sampleRate)
    {
        return Mathf.Sin(2 * Mathf.PI * m_TimeIndex * frequency / sampleRate);
    }
}
