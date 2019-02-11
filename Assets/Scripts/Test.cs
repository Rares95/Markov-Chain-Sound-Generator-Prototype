using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class Test : MonoBehaviour
{
    public AudioClip clip;

    private Dictionary<short,Node> loudnesses;
    private List<KeyValuePair<short,Node>> loudnessList;

    short lastLoudnessIndex = 0;

    [Range(0,1)]
    public float volume = 0.5f;

    public int spareMaxSamples = 8;
    public int ahead = 1;

    public float scale = 1;
    public float scaleY = 1;
    bool ready = false;

    class Node
    {
        Dictionary<short,ulong> loudnessLevels = new Dictionary<short, ulong>();
        List<KeyValuePair<short, float>> computedNodes;

        public void IncrementLoudness(short loudness)
        {
            if (loudnessLevels.ContainsKey(loudness))
            {
                ++loudnessLevels[loudness];
            }
            else
            {
                loudnessLevels.Add(loudness, 1);
            }
        }

        public void PruneByLoudness(int number)
        {
            var ordered = loudnessLevels.OrderBy(x => x.Value).ToList();
            loudnessLevels.Clear();

            List<KeyValuePair<short, ulong>> pruned = new List<KeyValuePair<short, ulong>>();

            ulong total = 0;
            for (int i = 0; i < number; ++i)
            {
                if (i == ordered.Count)
                {
                    break;
                }
                pruned.Add(ordered[i]);
                total += pruned[i].Value;
            }

            computedNodes = new List<KeyValuePair<short, float>>(pruned.Count);
            for (int i = 0; i < pruned.Count; ++i)
            {
                computedNodes.Add(new KeyValuePair<short, float>(pruned[i].Key, pruned[i].Value / (float)total));
            }
        }

        public short FindLoudnessIndex(float range)
        {
            float sum = 0;
            for (int i = 0; i < computedNodes.Count; ++i)
            {
                if (computedNodes[i].Value != 0)
                {
                    sum += computedNodes[i].Value;
                    if (sum > range)
                    {
                        return computedNodes[i].Key;
                    }
                }
            }
            return -1;
        }
    }

    public void Awake()
    {
        // Init dictionary
        loudnesses = new Dictionary<short, Node>();
        for (int i = short.MinValue + 1; i <= short.MaxValue; ++i)
        {
            loudnesses.Add((short)i, new Node());
        }

        // Load clip data
        float[] data = new float[clip.samples];
        clip.GetData(data, 0);

        // Analysis
        for (int i = 0; i < data.Length; ++i)
        {
            short loudnessLevel = (short)(data[i] * 32767.0f);
            short nextLoudnessLevel = 0;
            int count = 0;
            float sumLoudness = 0;
            for (int j = 1; j <= ahead; ++j)
            {
                if (i + j < data.Length)
                {
                    ++count;
                    sumLoudness += (data[i + j] * 32767.0f);
                }
            }
            var avgLoudness = sumLoudness / count;
            nextLoudnessLevel = (short)(avgLoudness);

            loudnesses[loudnessLevel].IncrementLoudness(nextLoudnessLevel);
        }

        // Prune data
        for (int i = short.MinValue + 1; i < short.MaxValue; ++i)
        {
            loudnesses[(short)i].PruneByLoudness(spareMaxSamples);
        }

        loudnessList = loudnesses.ToList();

        ready = true;
    }

    private void Update()
    {
        var data = AudioListener.GetOutputData(2048, 0);
        for (int i = 0; i < data.Length - 1; ++i)
        {
            Debug.DrawLine(new Vector3(i / (float)data.Length, data[i] * scaleY * volume, 0) * scale, new Vector3((i + 1) / (float)data.Length, data[i + 1] * scaleY * volume, 0) * scale, Color.green);
        }

        Debug.DrawLine(new Vector3(0, volume * scaleY, 0) * scale, new Vector3(1, volume * scaleY, 0) * scale, Color.yellow);
        Debug.DrawLine(new Vector3(0, 0, 0) * scale, new Vector3(1, 0, 0) * scale, Color.white);
        Debug.DrawLine(new Vector3(0, -volume * scaleY, 0) * scale, new Vector3(1, -volume * scaleY, 0) * scale, Color.yellow);
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        if (ready)
        {
            for (int i = 0; i < data.Length - 1; i += channels)
            {
                var currentLoudnessLevel = loudnesses[lastLoudnessIndex].FindLoudnessIndex(StaticRandom.Rand());
                data[i] = data[i + 1] = currentLoudnessLevel / 32767.0f * volume;
                lastLoudnessIndex = currentLoudnessLevel;
            }
        }
    }

    public static class StaticRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<System.Random> random = new ThreadLocal<System.Random>(() => new System.Random(Interlocked.Increment(ref seed)));

        public static float Rand()
        {
            float value = random.Value.Next()  / (float) int.MaxValue;
            return value;
        }
    }
}
