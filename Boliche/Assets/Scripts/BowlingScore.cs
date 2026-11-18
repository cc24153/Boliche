using System.Collections.Generic;

public sealed class BowlingScore
{
    public const int FrameCount = 10;
    public readonly List<int>[] Frames = new List<int>[FrameCount];
    public int Frame { get; private set; }
    public bool Finished => Frame == FrameCount;

    public BowlingScore()
    {
        for (int i = 0; i < FrameCount; i++)
            Frames[i] = new List<int>(3);
    }

    // Returns whether the next delivery starts with a full rack.
    public bool Record(int pins)
    {
        if (Finished)
            return false;

        List<int> rolls = Frames[Frame];
        rolls.Add(pins);
        if (Frame < FrameCount - 1)
        {
            if (pins == 10 || rolls.Count == 2)
            {
                Frame++;
                return true;
            }
            return false;
        }

        if (rolls.Count == 1)
            return pins == 10;
        if (rolls.Count == 2 && rolls[0] + pins >= 10)
            return rolls[0] != 10 || pins == 10;

        Frame++;
        return false;
    }

    public int? Cumulative(int through)
    {
        int total = 0;
        for (int i = 0; i <= through; i++)
        {
            List<int> rolls = Frames[i];
            if (rolls.Count == 0)
                return null;
            if (i == FrameCount - 1)
            {
                if (!Finished)
                    return null;
                foreach (int pins in rolls)
                    total += pins;
                continue;
            }

            bool strike = rolls[0] == 10;
            if (!strike && rolls.Count < 2)
                return null;
            int subtotal = strike ? 10 : rolls[0] + rolls[1];
            int needed = subtotal == 10 ? (strike ? 2 : 1) : 0;
            for (int next = i + 1; next < FrameCount && needed > 0; next++)
            {
                foreach (int pins in Frames[next])
                {
                    subtotal += pins;
                    if (--needed == 0)
                        break;
                }
            }
            if (needed > 0)
                return null;
            total += subtotal;
        }
        return total;
    }

    public string Mark(int frame, int roll)
    {
        List<int> rolls = Frames[frame];
        if (roll >= rolls.Count)
            return "";
        int pins = rolls[roll];
        if (roll > 0 && rolls[roll - 1] != 10 && pins + rolls[roll - 1] == 10 &&
            (roll == 1 || rolls[0] == 10))
            return "/";
        return pins == 10 ? "X" : pins == 0 ? "–" : pins.ToString();
    }
}
