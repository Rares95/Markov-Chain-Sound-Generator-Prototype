using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class MarkovSampleLoudness : MonoBehaviour
{
    [Header("Input")]

    [SerializeField]
    AudioClip m_Source;

    [Space(10)]
    [Header("Analysis")]
    [SerializeField]
    [Tooltip("The maximum number of samples to not be pruned during analysis.")]
    int m_SpareMaxSamples = 8;
    
    [SerializeField]
    [Tooltip("Number of samples to analyze ahead, result will be averaged between them and recorded.")]
    int m_Ahead = 1;

    [Space(10)]
    [Header("Output")]

    [Range(0, 1)]
    [SerializeField]
    public float m_Volume = 0.5f;

    [Header("Oscilloscope")]

    [SerializeField]
    float m_ScaleXY = 1;

    [SerializeField]
    float m_ScaleY = 1;


    Dictionary<short,Node> m_IntensityLevelNode;
    List<KeyValuePair<short,Node>> m_IntensityLevelList;
    short m_LastIntensityLevelIndex = 0;
    bool m_Ready = false;

    class Node
    {
        Dictionary<short,ulong> m_IntensityLevels = new Dictionary<short, ulong>();
        List<KeyValuePair<short, float>> m_ComputedNodes;

        public void IncrementLoudness(short loudness)
        {
            if (m_IntensityLevels.ContainsKey(loudness))
            {
                ++m_IntensityLevels[loudness];
            }
            else
            {
                m_IntensityLevels.Add(loudness, 1);
            }
        }

        public void PruneByLoudness(int number)
        {
            var ordered = m_IntensityLevels.OrderBy(x => x.Value).ToList();
            m_IntensityLevels.Clear();

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

            m_ComputedNodes = new List<KeyValuePair<short, float>>(pruned.Count);
            for (int i = 0; i < pruned.Count; ++i)
            {
                m_ComputedNodes.Add(new KeyValuePair<short, float>(pruned[i].Key, pruned[i].Value / (float)total));
            }
        }

        public short FindLoudnessIndex(float range)
        {
            float sum = 0;
            for (int i = 0; i < m_ComputedNodes.Count; ++i)
            {
                if (m_ComputedNodes[i].Value != 0)
                {
                    sum += m_ComputedNodes[i].Value;
                    if (sum > range)
                    {
                        return m_ComputedNodes[i].Key;
                    }
                }
            }
            return -1;
        }
    }

    public void Awake()
    {
        // Init dictionary
        m_IntensityLevelNode = new Dictionary<short, Node>();
        for (int i = short.MinValue + 1; i <= short.MaxValue; ++i)
        {
            m_IntensityLevelNode.Add((short)i, new Node());
        }

        // Load clip data
        float[] data = new float[m_Source.samples];
        m_Source.GetData(data, 0);

        // Analysis
        for (int i = 0; i < data.Length; ++i)
        {
            short currentIntensityLevel = (short)(data[i] * 32767.0f);
            short nextIntensityLevel = 0;
            int intensityNumber = 0;
            float intensitySum = 0;
            for (int j = 1; j <= m_Ahead; ++j)
            {
                if (i + j < data.Length)
                {
                    ++intensityNumber;
                    intensitySum += (data[i + j] * 32767.0f);
                }
            }
            var intensityAvg = intensitySum / intensityNumber;
            nextIntensityLevel = (short)(intensityAvg);

            m_IntensityLevelNode[currentIntensityLevel].IncrementLoudness(nextIntensityLevel);
        }

        // Prune data
        for (int i = short.MinValue + 1; i < short.MaxValue; ++i)
        {
            m_IntensityLevelNode[(short)i].PruneByLoudness(m_SpareMaxSamples);
        }

        m_IntensityLevelList = m_IntensityLevelNode.ToList();

        m_Ready = true;
    }

    private void Update()
    {
        var data = AudioListener.GetOutputData(2048, 0);
        for (int i = 0; i < data.Length - 1; ++i)
        {
            Debug.DrawLine(new Vector3(i / (float)data.Length, data[i] * m_ScaleY * m_Volume, 0) * m_ScaleXY, new Vector3((i + 1) / (float)data.Length, data[i + 1] * m_ScaleY * m_Volume, 0) * m_ScaleXY, Color.green);
        }

        Debug.DrawLine(new Vector3(0, m_Volume * m_ScaleY, 0) * m_ScaleXY, new Vector3(1, m_Volume * m_ScaleY, 0) * m_ScaleXY, Color.yellow);
        Debug.DrawLine(new Vector3(0, 0, 0) * m_ScaleXY, new Vector3(1, 0, 0) * m_ScaleXY, Color.white);
        Debug.DrawLine(new Vector3(0, -m_Volume * m_ScaleY, 0) * m_ScaleXY, new Vector3(1, -m_Volume * m_ScaleY, 0) * m_ScaleXY, Color.yellow);
    }


    void OnAudioFilterRead(float[] data, int channels)
    {
        if (m_Ready)
        {
            for (int i = 0; i < data.Length - 1; i += channels)
            {
                var currentIntensityLevel = m_IntensityLevelNode[m_LastIntensityLevelIndex].FindLoudnessIndex(StaticRandom.Rand());
                data[i] = data[i + 1] = currentIntensityLevel / 32767.0f * m_Volume;
                m_LastIntensityLevelIndex = currentIntensityLevel;
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
