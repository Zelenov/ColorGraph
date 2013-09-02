using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ColorGraph.Lists;

namespace ColorGraph
{


    //public delegate void OnFinishEvent(GraphNode sender, Signal result);



    public abstract class SerialOutPoint : Vertex
    {
        /*
         *         protected List<GraphConnection> GetNotSignalledConnections()
        {
            if (SignalledNil.Count == 0)
                return Connections;
            var res = new List<GraphConnection>(Connections.Count - SignalledNil.Count);
            res.AddRange(Connections.Where(connection => !SignalledNil.Contains(connection)));
            return res;
        }*/
        //private readonly ZSortedList<IColorable> _signalledNil = new ZSortedList<IColorable>();
        public InPoint Input;
        public readonly FilteredList<IColorable> Output = new FilteredList<IColorable>();

        protected IColorable CurrentPath;


        public SerialOutPoint()
        {
            
            Output.OnAllFiltered += OnAllOutputSignalled;
            Input = new InPoint(this);
        }
        protected override void Clear()
        {
            lock (this)
            {
                base.Clear();
                Output.ClearFilters();
            }
        }
        
        private void OnAllOutputSignalled(FilteredList<IColorable> sender)
        {
            if (!SignalIfCan())
                return;
            var exceptionChain = BuildExceptionChain(Output);
            Signal = new ErrorSignal(new GraphExceptionAllPossibilitiesFailed(this, exceptionChain));
            
            base.NotifyBack(this, Signal);
            InvokeOnNotifyBack();

            Input.NotifyBack(this, Signal);
        }

        

        public OutPoint AddOutConnection()
        {
            var outConn = new OutPoint(this);
            Output.Add(outConn);
            outConn.Name = Name + " Outpoint"+Output.Count.ToString(CultureInfo.InvariantCulture);
            return outConn;
        }

        
      
        protected void FlowResult()
        {
            if (!CanSignal())
                return;
            IColorable nextOut;
            IEnumerable<IColorable> notFilteredOutput = Output.NotFiltered;
            if (!notFilteredOutput.Any())
                nextOut = null;
            else
            {
                //nextOut = notFilteredOutput.MaxBy(outPoint => outPoint.GetLength());
                //without MoreLinq:
                nextOut = null;
                int? minLength = null;
                foreach (IColorable outPoint in notFilteredOutput)
                {
                    int length = (outPoint is IEdge)?((IEdge)outPoint).GetLength():0;
                    if (minLength == null || length < (int) minLength)
                    {
                        minLength = length;
                        nextOut = outPoint;
                    }
                }

            }
            CurrentPath = nextOut;
            InvokeOnFlow();
            if (nextOut == null)
                return;
              //  GoBack(new ErrorSignal(new Exception("All branches failed")));
            new Thread(() => nextOut.Flow(this, CurrentColor)) { IsBackground = true }.Start();

        }

        public override ColorableClass ConnectTo(GraphNode outNode, int length = 1)
        {
            return Connect(this, outNode, length);
        }

        public static OutPoint Connect(SerialOutPoint a, GraphNode b, int length = 1)
        {
            var outPoint = a.AddOutConnection();
            var inPoint = b.AddInConnection();
            GraphConnection.Connect(outPoint, inPoint, length);
            return outPoint;
        }

        public override ColorableClass ConnectTo(InPoint inPoint, int length = 1)
        {
            var outPoint = AddOutConnection();
            return outPoint.ConnectTo(inPoint, length);
        }

        public override Color GetCurrentColor()
        {
            return CurrentColor;
        }
        public override Signal GetSignal()
        {
            return Signal;
        }

        public override IEnumerable<IColorable> GetNextPaths()
        {
            return Output;
        }
        public override IEnumerable<IColorable> GetPrevPaths()
        {
            return new IColorable[]{Input};
        }
        public override void Flow(IColorable connection, Color newColor)
        {
            CurrentColor = Input.GetCurrentColor();
            if (CurrentColor==null)
            {
                Clear();
            }
            else
            {
                if (!FlowIfCan())
                    return;
                base.Flow(this, CurrentColor);
            }
            FlowResult();

        }

        //step back
        protected override void NotifyBack(IColorable from, SerialSignal signal)
        {
            if (!(from is OutPoint) && from!=this)
            //
                return;
            lock (this)
            {
                if( Output.IsFiltered(from))
                    return;
            }
            //Signal == true;
            signal.StopSending();

            Output.Filter(from);
            //if signal==true - we have to signal, otherwise we just need to find other output;
            if (from == CurrentPath)
            {
                FlowResult();
            }
            if (!SignalIfCan())
                    return;
                Signal = signal;
                Output.Filter(from);
                
                base.NotifyBack(this, signal);
                InvokeOnNotifyBack();
                Input.NotifyBack(this, signal);

        }

        
    }


}
