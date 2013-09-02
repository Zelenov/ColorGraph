using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ColorGraph
{
    public class Black : Color, IBlack
    {
        public override int CompareTo(Color other)
        {
            if (other == null)
                return 1;
            if (Mixed)
                return -1;
            return other is IBlack ? 0 : -1;
        }

        protected override Color CloneContent()
        {
            return new Black();
        }

        public override string ToString()
        {
            return "Black color";
        }
    }
    public class IntColor : Color
    {
        public int Value;
        public IntColor(int value)
        {
            Value = value;
        }
        public override int CompareTo(Color other)
        {
            if (other is IntColor) 
                return Value.CompareTo((other as IntColor).Value);
           return base.CompareTo(other);
        }

        protected override Color CloneContent()
        {
            return new IntColor(Value);
        }
        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }
    }
    public class StringColor : Color
    {
        public string Value;
        public override int CompareTo(Color other)
        {
            return other is StringColor
                       ? String.Compare(Value, (other as StringColor).Value, StringComparison.Ordinal)
                       : base.CompareTo(other);
        }
        public StringColor(string value)
        {
            Value = value;
        }

        protected override Color CloneContent()
        {
            return new StringColor(Value);
        }
        public override string ToString()
        {
            return "\"" + Value.ToString(CultureInfo.InvariantCulture) + "\"";
        }

    }
    public enum CompareType { Less, LessOrEqual, Equal, MoreOrEqual, More, NotEqual }
    public class CompareColor : Color
    {
       
        public CompareType Value;
        public CompareColor(CompareType value)
        {
            Value = value;
        }
        public override int CompareTo(Color other)
        {
            if (other is CompareColor)
                return Value.CompareTo((other as CompareColor).Value);
            return base.CompareTo(other);
        }

        protected override Color CloneContent()
        {
            return new CompareColor(Value);
        }
        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
