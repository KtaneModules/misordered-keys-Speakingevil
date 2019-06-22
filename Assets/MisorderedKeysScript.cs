using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using KModkit;

public class MisorderedKeysScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombInfo bomb;
    public KMColorblindMode ColorblindMode;

    public List<KMSelectable> keys;
    public Renderer meter;
    public Renderer[] keyID;
    public Material[] keyColours;

    private static int[][][] table =
        new int[5][][] { new int[6][] { new int[6] { 1, 0, 3, 4, 5, 2},
                                        new int[6] { 4, 1, 2, 5, 0, 3},
                                        new int[6] { 3, 4, 1, 0, 2, 5},
                                        new int[6] { 0, 5, 4, 2, 3, 1},
                                        new int[6] { 2, 3, 5, 1, 4, 0},
                                        new int[6] { 5, 2, 0, 3, 1, 4} },

                        new int[6][]  { new int[6] { 5, 4, 2, 3, 0, 1},
                                        new int[6] { 2, 0, 4, 1, 5, 3},
                                        new int[6] { 1, 2, 5, 0, 3, 4},
                                        new int[6] { 0, 3, 1, 2, 4, 5},
                                        new int[6] { 4, 1, 3, 5, 2, 0},
                                        new int[6] { 3, 5, 0, 4, 1, 2} },

                        new int[6][]  { new int[6] { 1, 5, 6, 2, 3, 4},
                                        new int[6] { 3, 4, 1, 5, 6, 2},
                                        new int[6] { 6, 2, 3, 1, 4, 5},
                                        new int[6] { 2, 3, 5, 4, 1, 6},
                                        new int[6] { 5, 6, 4, 3, 2, 1},
                                        new int[6] { 4, 1, 2, 6, 5, 3} },

                        new int[6][]  { new int[6] { 2, 1, 5, 3, 4, 6},
                                        new int[6] { 6, 3, 2, 1, 5, 4},
                                        new int[6] { 5, 4, 3, 6, 2, 1},
                                        new int[6] { 3, 6, 4, 2, 1, 5},
                                        new int[6] { 1, 5, 6, 4, 3, 2},
                                        new int[6] { 4, 2, 1, 5, 6, 3} },

                        new int[6][]  { new int[6] { 3, 1, 0, 4, 5, 2},
                                        new int[6] { 0, 2, 1, 5, 3, 4},
                                        new int[6] { 4, 5, 3, 0, 2, 1},
                                        new int[6] { 2, 3, 5, 1, 4, 0},
                                        new int[6] { 1, 4, 2, 3, 0, 5},
                                        new int[6] { 5, 0, 4, 2, 1, 3} } };

    private static string[] colourList = new string[6] { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow" };
    private int[][] info = new int[6][] { new int[4], new int[4], new int[4], new int[4], new int[4], new int[4] };
    private int[] lastDigit = new int[6];
    private int blackkey;
    private int pressCount;
    private int resetCount;
    private IEnumerator sequence;
    private bool pressable;
    private bool colorblind;
    private bool[] alreadypressed = new bool[6] { true, true, true, true, true, true};
    private List<string> presses = new List<string> { };
    private List<string> answer = new List<string> { };
    private List<string>[] labelList = new List<string>[6] { new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { }};

    //Logging
    static int moduleCounter = 1;
    int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = moduleCounter++;
        sequence = Shuff();
        meter.material = keyColours[6];
        foreach (KMSelectable key in keys)
        {
            key.transform.localPosition = new Vector3(0, 0, -1f);
            key.OnInteract += delegate () { KeyPress(key); return false; };
            key.OnHighlight += delegate () { KeyHL(key); };
            key.OnHighlightEnded += delegate () { KeyHLEnd(key); };
        }
    }

    void Start () {
        colorblind = ColorblindMode.ColorblindModeActive;
        Reset();
	}

    private void KeyHL(KMSelectable key)
    {
        if (keys.IndexOf(key) == blackkey && moduleSolved == false)
        {
            keyID[keys.IndexOf(key)].material = keyColours[6];
            key.GetComponentInChildren<TextMesh>().text = String.Empty;
        }
    }

    private void KeyHLEnd(KMSelectable key)
    {
        if (alreadypressed[keys.IndexOf(key)] == false && moduleSolved == false && pressable == true)
        {
            if (keys.IndexOf(key) == blackkey && moduleSolved == false)
            {
                setKey(keys.IndexOf(key));

            }
        }
    }

    private void setKey(int keyIndex)
    {
        keyID[keyIndex].material = keyColours[info[keyIndex][0]];
        switch (info[keyIndex][1])
        {
            case 0:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 25, 25, 255);
                break;
            case 1:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 25, 255);
                break;
            case 2:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 25, 255, 255);
                break;
            case 3:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(25, 255, 255, 255);
                break;
            case 4:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 75, 255, 255);
                break;
            default:
                keys[keyIndex].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 75, 255);
                break;
        }
        var label = String.Join("\n", labelList[keyIndex].ToArray());
        if (colorblind == true)
        {
            label += "\n\n" + "RGBCMY"[info[keyIndex][1]] + "\n\n" + "RGBCMY"[info[keyIndex][0]];
            keys[keyIndex].GetComponentInChildren<TextMesh>().fontSize = 120;
        }
        keys[keyIndex].GetComponentInChildren<TextMesh>().text = label;
    }

    private void KeyPress(KMSelectable key)
    {
        if (alreadypressed[keys.IndexOf(key)] == false && moduleSolved == false && pressable == true)
        {
            GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            alreadypressed[keys.IndexOf(key)] = true;
            presses.Add((keys.IndexOf(key) + 1).ToString());
            key.transform.localPosition = new Vector3(0, 0, -1f);
            key.AddInteractionPunch();
            if (pressCount < 5)
            {
                pressCount++;
            }
            else
            {
                pressCount = 0;
                string[] answ = answer.ToArray();
                string[] press = presses.ToArray();
                string ans = string.Join(string.Empty, answ);
                string pr = string.Join(string.Empty, press);
                Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the buttons were pressed in the order: {2}", moduleID, resetCount, pr);
                if (ans == pr)
                {
                    meter.material = keyColours[7];
                    Audio.PlaySoundAtTransform("InputCorrect", transform);
                    moduleSolved = true;                                     
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                }
                answer.Clear();
                presses.Clear();
                resetCount++;
                Reset();
            }
        }
    }

    private void Reset()
    {
        if (moduleSolved == false)
        {
            pressable = false;
            for(int i = 0; i < 6; i++)
            {
                labelList[i].Clear();
            }
            blackkey = UnityEngine.Random.Range(0, 6);
            List<int> availableValues = new List<int> { 1, 2, 3, 4, 5, 6 };
            List<int> iiList = new List<int> { 1, 2, 3, 4, 5, 6 };
            List<int> fiList = new List<int> { };
            int[] ffList = new int[6];
            for (int i = 0; i < 6; i++)
            {
                int temp = UnityEngine.Random.Range(0, iiList.Count());
                fiList.Add(iiList[temp]);
                iiList.RemoveAt(temp);
            }
            int[] rand = new int[6];
            for (int i = 0; i < 6; i++)
            {
                info[i][0] = UnityEngine.Random.Range(0, 6);
                info[i][1] = UnityEngine.Random.Range(0, 6);
                info[i][2] = i + 1;
                rand[i] = UnityEngine.Random.Range(1, 7);
                for (int j = 0; j < rand[i]; j++)
                {
                    int random = UnityEngine.Random.Range(0, 6);
                    labelList[i].Add((random + 1).ToString());
                    if (j == rand[i] - 1)
                    {
                        lastDigit[i] = random;
                    }
                }
                int oh = table[0][info[i][1]][info[i][0]];
                for (int j = 0; j < 6; j++)
                {
                    if (fiList[i] == table[2][j][oh])
                    {
                        info[i][3] = j;
                        break;
                    }
                }
                for (int j = 0; j < 6; j++)
                {
                    if (info[i][3] == table[1][i][j])
                    {
                        info[i][3] = j;
                        break;
                    }
                }
                if(rand[i] == 1)
                {
                    lastDigit[i] = info[i][3];
                }
                labelList[i][0] = (info[i][3] + 1).ToString();
            }
            string[] a = new string[6];
            string[] b = new string[6];
            string[] c = new string[6];
            string[] d = new string[6];
            for(int i = 0; i < 6; i++)
            {
                a[i] = colourList[info[i][0]];
                b[i] = colourList[info[i][1]];
                c[i] = fiList[i].ToString();
                d[i] = String.Join(String.Empty, labelList[i].ToArray());
            }
            string A = String.Join(", ", a);
            string B = String.Join(", ", b);
            string C = String.Join(String.Empty, c);
            string D = String.Join(", ", d);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the black button was {2}", moduleID, resetCount, blackkey + 1);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the buttons had the colours: {2}", moduleID, resetCount, A);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the labels had the colours: {2}", moduleID, resetCount, B);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the labels were: {2}", moduleID, resetCount, D);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the first set of key values was: {2}", moduleID, resetCount, C);

            for (int i = 0; i < 6; i++)
            {
                int[] valueCardinality = new int[6];
                for (int j = 0; j < rand[i]; j++)
                {
                    switch (labelList[i][j])
                    {
                        case "1":
                            valueCardinality[0]++;
                            break;
                        case "2":
                            valueCardinality[1]++;
                            break;
                        case "3":
                            valueCardinality[2]++;
                            break;
                        case "4":
                            valueCardinality[3]++;
                            break;
                        case "5":
                            valueCardinality[4]++;
                            break;
                        case "6":
                            valueCardinality[5]++;
                            break;
                    }
                }
                bool choose = false;
                if(i != blackkey)
                {
                    if(valueCardinality.Sum() == 1)
                    {
                        ffList[i] = fiList.IndexOf(info[i][3] + 1) + 1;
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 1 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    int uniqueDigits = 0;
                    for(int j = 0; j < 6; j++)
                    {
                        if(valueCardinality[j] == 1)
                        {
                            uniqueDigits++;
                        }
                    }
                    if(choose == false && uniqueDigits > 2)
                    {
                        ffList[i] = fiList[blackkey];
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 2 applies to key {1}", moduleID, i + 1, ffList[i]);
                            choose = true;
                        }
                    }
                    int[] digits = new int[6];
                    for (int j = 0; j < 6; j++)
                    {
                        digits[j] = (j + 1) * valueCardinality[j];
                    }
                    if (choose == false && digits.Sum() > 15)
                    {
                        ffList[i] = fiList[lastDigit[i]];
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 3 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    int distinctDigits = 0;
                    for(int j = 0; j < 6; j++)
                    {
                        if(valueCardinality[j] != 0)
                        {
                            distinctDigits++;
                        }
                    }
                    if(choose == false && distinctDigits < 3)
                    {
                        ffList[i] = fiList.IndexOf(lastDigit[i] + 1) + 1;
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 4 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    if(choose == false && valueCardinality[0] == 0 && valueCardinality[2] == 0 && valueCardinality[4] == 0)
                    {
                        ffList[i] = fiList.IndexOf(i + 1) + 1;
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 5 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    if(choose == false && valueCardinality[0] > 0 && valueCardinality[5] > 0)
                    {
                        ffList[i] = fiList.IndexOf(info[blackkey][3] + 1) + 1;
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 6 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    if(choose == false && ((valueCardinality[1] > 0 && valueCardinality[3] > 0) || (valueCardinality[1] > 0 && valueCardinality[5] > 0) || (valueCardinality[3] > 0 && valueCardinality[5] > 0)))
                    {
                        ffList[i] = fiList.IndexOf(lastDigit[blackkey] + 1) + 1;
                        if (availableValues.Contains(ffList[i]))
                        {
                            availableValues.Remove(ffList[i]);
                            Debug.LogFormat("[Misordered Keys #{0}] Rule 7 applies to key {1}", moduleID, i + 1);
                            choose = true;
                        }
                    }
                    if(choose == false)
                    {
                        if (valueCardinality.Sum() != 1)
                        {                       
                            switch (table[4][lastDigit[i]][info[i][1]])
                            {
                                case 0:
                                    ffList[i] = info[i][3] + 1;
                                    break;
                                case 1:
                                    ffList[i] = lastDigit[i] + 1;
                                    break;
                                case 2:
                                    ffList[i] = labelList[i].Count();
                                    break;
                                case 3:
                                    ffList[i] = (digits.Sum() % 6) + 1;
                                    break;
                                case 4:
                                    ffList[i] = i + 1;
                                    break;
                                case 5:
                                    ffList[i] = valueCardinality.ToList().IndexOf((int)Mathf.Max(valueCardinality[0], valueCardinality[1], valueCardinality[2], valueCardinality[3], valueCardinality[4], valueCardinality[5])) + 1;
                                    break;
                            }
                            if (availableValues.Contains(ffList[i]))
                            {
                                availableValues.Remove(ffList[i]);
                                Debug.LogFormat("[Misordered Keys #{0}] Rule 9 applies to key {1}", moduleID, i + 1);
                                choose = true;
                            }
                        }
                        else
                        {
                            ffList[i] = table[3][info[i][3]][info[i][1]];
                            if (availableValues.Contains(ffList[i]))
                            {
                                availableValues.Remove(ffList[i]);
                                Debug.LogFormat("[Misordered Keys #{0}] Rule 8 applies to key {1}", moduleID, i + 1);
                                choose = true;
                            }
                        }
                    }
                    if(choose == false)
                    {
                        ffList[i] = availableValues[0];
                        Debug.LogFormat("[Misordered Keys #{0}] No rules apply to key {1}", moduleID, i + 1);
                        availableValues.RemoveAt(0);
                    }
                }
            }
            ffList[blackkey] = availableValues[0];
            for(int i = 0; i < 6; i++)
            {
                answer.Add(fiList[ffList.ToList().IndexOf(i + 1)].ToString());
            }
            string[] e = new string[6];
            for (int i = 0; i < 6; i++)
            {
                e[i] = ffList[i].ToString();
            }
            string E = String.Join(String.Empty, e);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the second set of key values was: {2}", moduleID, resetCount, E);

            string[] answ = answer.ToArray();
            string ans = string.Join(string.Empty, answ);
            Debug.LogFormat("[Misordered Keys #{0}] After {1} reset(s), the pressing order was: {2}", moduleID, resetCount, ans);
        }
        StartCoroutine(sequence);
    }

    private IEnumerator Shuff()
    {
        for (int i = 0; i < 30; i++)
        {
            if (i % 5 == 4)
            {
                if (moduleSolved == true)
                {
                    alreadypressed[(i - 4) / 5] = false;
                    keyID[(i - 4) / 5].material = keyColours[8];
                    keys[(i - 4) / 5].GetComponentInChildren<TextMesh>().color = new Color32(0, 0, 0, 255);
                    keys[(i - 4) / 5].GetComponentInChildren<TextMesh>().text = "0";
                    if (i == 29)
                    {
                        GetComponent<KMBombModule>().HandlePass();
                        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    }
                }
                else
                {
                    alreadypressed[(i - 4) / 5] = false;
                    keys[(i - 4) / 5].transform.localPosition = new Vector3(0, 0, 0);
                    GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                    keyID[(i - 4) / 5].material = keyColours[info[(i - 4) / 5][0]];
                    keys[(i - 4) / 5].GetComponentInChildren<TextMesh>().text = String.Join("\n", labelList[(i - 4) / 5].ToArray());
                    setKey((i - 4) / 5);
                }
                if (i == 29)
                {
                    i = -1;
                    pressable = true;
                    StopCoroutine(sequence);
                }
            }
            else
            {
                for (int j = 0; j < 6; j++)
                {
                    int[] rand = new int[4];
                    for (int k = 0; k < 3; k++)
                    {
                        rand[k] = UnityEngine.Random.Range(0, 6);
                    }
                    if (alreadypressed[j] == true)
                    {
                        keyID[j].material = keyColours[rand[0]];
                        switch (rand[1])
                        {
                            case 0:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(255, 0, 0, 255);
                                break;
                            case 1:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(0, 255, 0, 255);
                                break;
                            case 2:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(75, 75, 225, 255);
                                break;
                            case 3:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(0, 255, 255, 255);
                                break;
                            case 4:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(255, 0, 255, 255);
                                break;
                            case 5:
                                keys[j].GetComponentInChildren<TextMesh>().color = new Color32(255, 255, 0, 255);
                                break;
                        }
                        List<string>[] labelrand = new List<string>[6] { new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { }, new List<string> { } };
                        rand[3] = UnityEngine.Random.Range(1, 7);
                        for (int k = 0; k < rand[3]; k++)
                        {
                            labelrand[j].Add(UnityEngine.Random.Range(1, 7).ToString());
                        }
                        string[] label = new string[6];
                        label[j] = String.Join("\n", labelrand[j].ToArray());
                        keys[j].GetComponentInChildren<TextMesh>().text = label[j];
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press 123456 [position in reading order] | !{0} k [highlights black key] | !{0} colorblind";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            colorblind = true;
            for (int i = 0; i < keys.Count; i++)
                setKey(i);
            yield return null;
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*k\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;          
            keys[blackkey].OnHighlight();
            yield return new WaitForSeconds(1.2f);
            keys[blackkey].OnHighlightEnded();
            yield return new WaitForSeconds(.1f);
            yield break;
        }

        var m = Regex.Match(command, @"^\s*(?:press\s*)?([123456 ,;]+)\s*$");
        if (!m.Success)
            yield break;

        foreach (var keyToPress in m.Groups[1].Value.Where(ch => ch >= '1' && ch <= '6').Select(ch => keys[ch - '1']))
        {
            yield return null;
            while (!pressable)
                yield return "trycancel";
            yield return new[] { keyToPress };
        }
    }
}
