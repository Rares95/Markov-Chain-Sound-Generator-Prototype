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
    [Range(1, 256)]
    public int m_AmountOfTones = 128;

    private int numFrames = -1;
    private AudioSource m_AudioSource;
    //public AnimationCurve m_AnimationCurve = new AnimationCurve();
    private int m_Tone = 0;
    private float m_Time = 0;
    private float m_TimeOffset = 0;

    private void Awake()
    {
        soundToBePlayed = SoundAnalysis.SplitSoundIntoFrequencies(Clip, 2048, m_AmountOfTones);
        m_TimeOffset = Time.time;

        m_AudioSource = gameObject.GetComponent<AudioSource>();
        m_AudioSource = m_AudioSource ?? gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
        m_AudioSource.spatialBlend = 0;
        m_AudioSource.Play();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!m_AudioSource.isPlaying)
            {
                m_Tone = 0;  //resets timer before playing sound
                m_AudioSource.Play();
            }
            else
            {
                m_AudioSource.Stop();
            }
        }
        m_Time = Time.time;
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        m_Tone = Mathf.FloorToInt(((m_Time -m_TimeOffset) / soundToBePlayed.ToneLength) % soundToBePlayed.ToneCount);


        var tones = soundToBePlayed.GetToneAtIndex(m_Tone);

        float offset = 0;
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }
        foreach (var tone in tones)
        {
            float value = 0;
            offset += 117.279f; //a random number just to 

            for (int i = 0; i < data.Length; i += channels)
            {
                var calcFunc = Mathf.Sin((i+offset) * (tone.frequency)/(soundToBePlayed.SampleFrequency) * (Mathf.PI)) * tone.amplitude;
                value += calcFunc;

                //smooth the Edges, very necesaary right now as every frequency is a sine and so start a 0 and every singel one goes up :P
                int distance = Mathf.Min(i, data.Length - i);
                if (distance < 50)
                    value *= distance / 50f;
                value *= volume;

                for (int j = 0; j < channels; j++)
                {
                    data[i + j] += value;
                }
            }
            
        }
    }

    public float CreateSine(int m_TimeIndex, float frequency, float sampleRate)
    {
        return Mathf.Sin(2 * Mathf.PI * m_TimeIndex * frequency / sampleRate);
    }
}
