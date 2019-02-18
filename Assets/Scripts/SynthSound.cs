using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SynthSound : ScriptableObject
{
    public float SampleSize;
    public float SampleFrequency; //e.g. 44100Hz
    public List<List<Tone>> ToneList = new List<List<Tone>>();

    public float ToneLength => SampleSize / SampleFrequency;
    public float SoundLength => ToneLength * ToneList.Count;
    public int ToneCount => ToneList.Count;

    public List<Tone> GetToneAtTime(float time)
    {
        int index = Mathf.FloorToInt(time / SoundLength);
        return GetToneAtIndex(index);
    }
    public List<Tone> GetToneAtIndex(int index)
    {
        if (ToneList.Count == 0)
        {
            Debug.Log("Trying to play an empty sound");
            return new List<Tone>();
        }
        if (index < 0 || index >= ToneList.Count)
        {
            Debug.Log("Sound trying to play outside its defined range");
            return new List<Tone>();
        }
        return ToneList[index];
    }
    public void AddTones(List<Tone> tones)
    {
        ToneList.Add(tones);
    }
#if UNITY_EDITOR
    //[UnityEditor.MenuItem("Assets/Process AudioClip")]
    private static void ProcessSound()
    {
        AudioClip selected = UnityEditor.Selection.activeObject as AudioClip;
        if (!selected)
            Debug.LogError("Selected was not an audioclip");
        SynthSound result = SoundAnalysis.SplitSoundIntoFrequencies(selected, 2048, 1);
        //UnityEditor.EditorUtility.SaveFilePanelInProject(Choose save location)
    }
# endif
    
}
[Serializable]
public class Tone
{
    public float frequency;
    public float amplitude;
    public override string ToString()
    {
        return frequency + "Hz at amp " + amplitude;
    }
}