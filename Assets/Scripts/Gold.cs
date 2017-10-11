using UniRx;
using UnityEngine;

public interface IReward
{
    void Apply(long count);
}

public interface IConsume
{
    bool IsCanConsume(long count);
    bool TryConsume(long count);
}

[System.Serializable]
public class Resources : IReward, IConsume
{
    [SerializeField] public LongCell count;

    public Resources()
    {
        count = new LongCell(0);
    }

    public void Apply(long count)
    {
        this.count.value += count;
    }

    public bool IsCanConsume(long count)
    {
        return this.count.value >= count;
    }

    public bool TryConsume(long count)
    {
        if (this.count.value < count) return false;
        this.count.value -= count;
        return true;
    }
}
[System.Serializable]
public class Gold : Resources
{
    public Gold(long count)
    {
        this.count = new LongCell(count);
    }
}

[System.Serializable]
public class PasiiveGold : Resources
{
    public PasiiveGold(long count)
    {
        this.count = new LongCell(count);
    }
}
[System.Serializable]
public class GoldForTap : Resources
{
    public GoldForTap(long count)
    {
        this.count = new LongCell(count);
    }
}