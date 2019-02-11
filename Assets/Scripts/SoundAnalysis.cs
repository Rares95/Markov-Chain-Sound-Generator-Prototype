using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using B83.MathHelpers;
using System.Linq;

public class SoundAnalysis : MonoBehaviour
{
    public class SynthSound
    {
        public readonly float SampleSize;
        public readonly float SampleFrequency; //e.g. 44100Hz
        public List<List<Tone>> ToneList = new List<List<Tone>>();

        public float ToneLength => SampleSize / SampleFrequency;

        public void AddTones(List<Tone> tones)
        {
            ToneList.Add(tones);
        }
    }
    public class Tone
    {
        public float frequency;
        public float amplitude;
        public override string ToString()
        {
            return frequency + "Hz at amp " + amplitude;
        }
    }

    public AudioClip Clip;
    public AudioListener listen;
    [Range(0.0001f,0.1f)]
    public float scaling = 0.01f;

    float[] spec = new float[1024];
    float[] tmp = new float[2048];
    Complex[] spec2 = new Complex[2048];
    float[] spec3 =new float[2048];
    void Update()
    {
        //draw the sound playing
        AudioListener.GetOutputData(tmp, 0);
        DrawCurve(tmp, new Vector3(-8, -4, 0), new Vector2(scaling, 1), Color.yellow);

        //Draw the file  
        Clip.GetData(tmp, 0);
        DrawCurve(tmp, new Vector3(-8, -2, 0), new Vector2(scaling, 1), Color.white);

        //draw the frequencyband
        spec3 = GetFrequencySpectrum(Clip, 4096);
        DrawCurve(spec3, new Vector3(-8, 0, 0), new Vector2(scaling, 1), Color.red, true);
        
        //name the most dominant frequencies
        Debug.Log("List of most dominant frequencies: ");
        var list = GetMostPlayedFrequencies(Clip, 2048);
        foreach(var entry in list)
        {
            Debug.Log(entry);
        }

    }
    public void DrawCurve(float[] data, Vector3 position, Vector2 scaling, Color color, bool hideSecondHalf = false)
    {
        for (int i = 1; i < data.Length/(hideSecondHalf?2:1); i++)
        {
            Debug.DrawLine(
                new Vector3(
                    (i - 1) * scaling.x + position.x,
                    data[i - 1] * scaling.y + position.y,
                    position.z),
                new Vector3(
                    i * scaling.x + position.x,
                    data[i] * scaling.y + position.y,
                    position.z),
                color);
        }
    }

    public static SynthSound SplitSoundIntoFrequencies(AudioClip Clip, int GrainSize, int dominantFrequenciesCount = 1)
    {
        int segments = Clip.samples / GrainSize;
        SynthSound Sound = new SynthSound();
        for (int i = 0; i < segments; i++)
        {
            Sound.AddTones(GetMostPlayedFrequencies(Clip, GrainSize, i * GrainSize, dominantFrequenciesCount));
        }
        return Sound;
    }

    public static float SampleSizeToTime(int sampleSize, int sampleRate)
    {
        return (float) sampleSize / sampleRate;
    }
    public static float IndexToFrequency(int index, int sampleSize, int sampleRate)
    {
        return ((float) index / sampleSize) * sampleRate;
    }

    public static List<Tone> GetMostPlayedFrequencies(AudioClip Clip, int GrainSize, int offset = 0, int dominantFrequenciesCount = 1)
    {
        if (!Mathf.IsPowerOfTwo(GrainSize))
            throw new System.ArgumentException("GrainSize needs to be a power of 2");
        float[] data = new float[GrainSize];
        Clip.GetData(data, offset);
        float[] frequencies = GetFrequencySpectrum(data);
        var Tones = new List<Tone>();
        for (int i = 0; i <= frequencies.Length / 2; i++) //ignore second half (IMPORTANT)
        {
            Tone t = new Tone();
            t.frequency = IndexToFrequency(i, GrainSize, Clip.frequency);
            t.amplitude = frequencies[i] * 2;
            Tones.Add(t);
        }
        
        Tones = Tones.OrderByDescending(a => a.amplitude).ToList();
        var Tones2 = new List<Tone>();
        for (int i = 0; i < dominantFrequenciesCount; i++)
        {
            Tones2.Add(Tones[i]);
        }
        return Tones2;
    }
    public static float[] GetFrequencySpectrum(float[] soundData)
    {
        int grainSize = soundData.Length;
        if (!Mathf.IsPowerOfTwo(grainSize))
            throw new System.ArgumentException("Grain Size needs to be a power of 2");

        Complex[] complexData = new Complex[grainSize];
        for (int i = 0; i < grainSize; i++)
        {
            complexData[i] = new Complex(soundData[i], 0);
        }

        FFT.CalculateFFT(complexData, false);
        for (int i = 0; i < grainSize; i++)
        {
            soundData[i] = (float)complexData[i].magnitude;
        }

        return soundData;
    }
    public static float[] GetFrequencySpectrum(AudioClip clip, int grainSize)
    {
        float[] soundData = new float[grainSize];
        clip.GetData(soundData, 0);

        return GetFrequencySpectrum(soundData);        
    }
}
    