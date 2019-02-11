using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayFrequency : MonoBehaviour
{
    [Range(0f,1f)]
    public float volume = 0.5f;

    [Range(1,20000)]
    public int Frequency1=404;

    [Range(1,20000)]
    public int Frequency2=416;


    public const int k_SampleRate = 44100;
    public float WaveLengthInSeconds;

    public int squareSines = 2;

    public AnimationCurve AnimationCurveS = new AnimationCurve();
    public const int k_Samples = 2048;
    public int k_AnimationCurveCompressionFactor = 8;
    public int oscilloscopeFPS = 10;
    private int numFrames = -1;

    private AudioSource m_AudioSource;
    private int m_TimeIndex;

    private void Awake()
    {
        m_AudioSource = gameObject.AddComponent<AudioSource>();
        m_AudioSource.playOnAwake = false;
        m_AudioSource.spatialBlend = 0;
        m_AudioSource.Stop();

        for (int i = 0; i < k_Samples / k_AnimationCurveCompressionFactor; ++i)
        {
            AnimationCurveS.AddKey(WaveLengthInSeconds / k_Samples * i * k_AnimationCurveCompressionFactor, 0);
        }
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
                m_TimeIndex = 0;  //resets timer before playing sound
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
        numFrames--;
        if (numFrames < 0)
        {
            numFrames = 60 / oscilloscopeFPS;
        }
        for (int i = 0; i < data.Length; i += channels)
        {
            //data[i] = CreateSine(m_TimeIndex, Frequency1, k_SampleRate);

            //if (channels == 2)
            //    data[i + 1] = CreateSine(m_TimeIndex, Frequency2, k_SampleRate);

            m_TimeIndex++;

            float value = 0;

            for (var j = 1; j <= squareSines; j++)
            {
                var calcFunc = Mathf.Sin(((2 * Mathf.PI) * (2 * j - 1) * i * (Frequency1/2000f)) * Mathf.Deg2Rad) / (2 * j - 1);
                value += calcFunc;
            }
            value *= Mathf.PI / 4;

            value *= volume;

            data[i] = value;
            data[i + 1] = value;

            if (numFrames == 60 / oscilloscopeFPS)
                if (i % k_AnimationCurveCompressionFactor == 0)
                {
                    AnimationCurveS.MoveKey(i / k_AnimationCurveCompressionFactor, new Keyframe(AnimationCurveS.keys[i / k_AnimationCurveCompressionFactor].time, data[i]));
                }

            if (m_TimeIndex >= (k_SampleRate * WaveLengthInSeconds))
            {
                m_TimeIndex = 0;
            }
        }
    }

    public float CreateSine(int m_TimeIndex, float frequency, float sampleRate)
    {
        return Mathf.Sin(2 * Mathf.PI * m_TimeIndex * frequency / sampleRate);
    }
}
