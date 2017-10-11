using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Configuration;
using System.Text;

public interface IValueCell : ICell<int>
{
}

[Serializable]
public class ValueCell : Cell<int>, IValueCell
{
    public ValueCell(int val) : base(val){}
    public ValueCell() : base() { }
}

[Serializable]
public class FloatCell : Cell<float>
{
    [NonSerialized]
    Stream<float> diff_;
    public IStream<float> diff { get { return diff_ ?? (diff_ = new Stream<float>()); } }

    public FloatCell(float val) : base(val) { }
    public FloatCell() : base() { }

    public override float value
    {
        get
        {
            return base.value;
        }
        set
        {
            float oldVal = base.value;
            base.value = value;
            if (diff_ != null && oldVal != value)
                diff_.Send(value - oldVal);
        }
    }
}

[Serializable]
public class FloatTimeCell : Cell<float>
{
    [NonSerialized] private Stream<float> diff_;

    public IStream<float> diff { get { return diff_ ?? (diff_ = new Stream<float>()); } }

    public FloatTimeCell(float val) : base(val) { }
    public FloatTimeCell() : base() { }

    public override float value
    {
        get
        {
            return base.value;
        }
        set
        {
            if (diff_ == null) diff_ = new Stream<float>();
            base.value = value;
            diff_.Send(value);
        }
    }
}
[Serializable]
public class DoubleCell : Cell<double>
{
    [NonSerialized]
    Stream<double> diff_;
    public IStream<double> diff { get { return diff_ ?? (diff_ = new Stream<double>()); } }

    public DoubleCell() : base() { }
    public DoubleCell(double value) : base(value) { }

    public override double value
    {
        get
        {
            return base.value;
        }
        set
        {
            double oldVal = base.value;
            base.value = value;
            if (diff_ != null && oldVal != value)
                diff_.Send(value - oldVal);
        }
    }
}

[Serializable]
public class IntCell : Cell<int>
{
    [NonSerialized]
    Stream<int> diff_;
    public IStream<int> diff { get { return diff_ ?? (diff_ = new Stream<int>()); } }

    public IntCell(int val) : base(val) { }
    public IntCell() : base() { }

    public override int value
    {
        get
        {
            return base.value;
        }
        set
        {
            int oldVal = base.value;
            base.value = value;
            if (diff_ != null && oldVal != value)
                diff_.Send(value - oldVal);
        }
    }
}
[Serializable]
public class LongCell : Cell<long>
{
    [NonSerialized]
    Stream<long> diff_;
    public IStream<long> diff { get { return diff_ ?? (diff_ = new Stream<long>()); } }

    public LongCell(long val) : base(val) { }
    public LongCell() : base() { }

    public override long value
    {
        get
        {
            return base.value;
        }
        set
        {
            long oldVal = base.value;
            base.value = value;
            if (diff_ != null && oldVal != value)
                diff_.Send(value - oldVal);
        }
    }
}

[Serializable]
public class BoolCell : Cell<bool>
{
    public BoolCell()
    {

    }

    public BoolCell(bool initial) : base(initial)
    {
    }
}

[Serializable]
public class StringCell : Cell<string> { }


