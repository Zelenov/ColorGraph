using System.Collections.Generic;

namespace ColorGraph
{
    public class GraphConnection : Edge
    {

        protected readonly int Length;
        public OutPoint From;
        public InPoint To;

        public GraphConnection(int length, OutPoint from, InPoint to)
        {
            Length = length;
            From = from;
            To = to;
        }

        public override Color GetCurrentColor()
        {
            return CurrentColor;
        }

        public override Signal GetSignal()
        {
            return Signal;
        }

        public override int GetLength()
        {
            return Length;
        }

        public override IEnumerable<IColorable> GetNextPaths()
        {
            return To == null ? new IColorable[] {} : new IColorable[] {To};
        }

        public override IEnumerable<IColorable> GetPrevPaths()
        {
            return From == null ? new IColorable[] { } : new IColorable[] { From };
        }

        public override void Flow(IColorable connection, Color newColor)
        {
            if (!FlowIfCan() && newColor!=null)
            {
                NotifyBack(this, new ErrorSignal(new GraphExceptionAlreadyColored(this)));
                return;
            }
            if (newColor == null)
            {
                Clear();
            }
            CurrentColor = newColor;
            base.Flow(From,GetCurrentColor());
            if (To != null)
            {
                InvokeOnFlow();
                To.Flow(this, CurrentColor);
            }
        }
   
        public override bool CanFlow()
        {
            return base.CanFlow() && To!=null ;
        }
        public override bool CanSignal()
        {
            return base.CanSignal() && From != null;
        }
        protected override void NotifyBack(IColorable @from, SerialSignal signal)
        {
            if (SignalIfCan())
            {
                Signal = signal;
                base.NotifyBack(from, signal);
                InvokeOnNotifyBack();
                From.NotifyBack(this, signal);
            }
        }

        public override ColorableClass ConnectTo(OutPoint to, int length = 1)
        {
            to.AddConection(this);
            return this;
        }

        public static GraphConnection Connect(OutPoint outPoint, InPoint inPoint, int length = 1)
        {
            if (outPoint == null || inPoint == null)
                return null;
            var connection = new GraphConnection(length, outPoint, inPoint);
            connection.Name = outPoint.Name + " connection";
            outPoint.AddConection(connection);
            inPoint.AddConection(connection);
            if (string.IsNullOrEmpty(inPoint.Name))
                inPoint.Name = outPoint.Name + " connection inPoint";
            return connection;
        }
    }
}