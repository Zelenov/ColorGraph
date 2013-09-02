using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ColorGraph.Lists;

namespace ColorGraph
{
    public delegate void OnFlowEvent(IColorable connection, Color newColor);
    public delegate void OnNotifyBackEvent(IColorable sender, Signal stop);
    public interface IColorable
    {
        Color GetCurrentColor();
        Signal GetSignal();
        bool CanFlow();
        bool CanSignal();
        IEnumerable<IColorable> GetNextPaths();
        IEnumerable<IColorable> GetPrevPaths();
        void Flow(IColorable connection, Color newColor);
        void NotifyBack(IColorable from, Signal signal);
        IColorable ConnectTo(IColorable to, int length = 1);
        string GetName();

        
    }

    public interface IEdge
    {
        int GetLength();
    }
    public interface IVertex
    {
        int GetLengthBeetween(IColorable to);
    }
    public abstract class ColorableClass : IColorable
    {
        public string Name;
        public abstract Color GetCurrentColor();
        public abstract Signal GetSignal();
        public abstract bool CanFlow();
        public abstract bool CanSignal();


        public abstract IEnumerable<IColorable> GetNextPaths();
        public abstract IEnumerable<IColorable> GetPrevPaths();
        public abstract void Flow(IColorable connection, Color color);
        public void NotifyBack(IColorable from, Signal signal)
        {
            var serialSignal = signal as SerialSignal;
            var broadcastSignal = signal as BroadcastSignal;
            if (serialSignal != null)
                NotifyBack(from, serialSignal);
            else if (broadcastSignal != null)
                NotifyBack(from, broadcastSignal);
        }

        protected abstract void NotifyBack(IColorable from, SerialSignal signal);
        protected abstract void NotifyBack(IColorable from, BroadcastSignal signal);

        public IColorable ConnectTo(IColorable to, int length = 1)
        {
            if (to is InPoint)
                return ConnectTo((InPoint)to, length);
            if (to is OutPoint)
                return ConnectTo((OutPoint)to, length);
            if (to is GraphNode)
                return ConnectTo((GraphNode)to, length);
            if (to is GraphConnection)
                return ConnectTo((GraphConnection)to, length);
            return this;
        }


        public string GetName()
        {
            return Name;
        }

        public virtual ColorableClass ConnectTo(InPoint to, int length = 1) { return this; }
        public virtual ColorableClass ConnectTo(OutPoint to, int length = 1) { return this; }
        public virtual ColorableClass ConnectTo(GraphNode to, int length = 1) { return this; }
        public virtual ColorableClass ConnectTo(SerialOutPoint outPoint, int length = 1) { return this; }
        public virtual ColorableClass ConnectTo(GraphConnection to, int length = 1) { return this; }

        public event OnNotifyBackEvent OnNotifyBack;
        public event OnFlowEvent OnFlow;
        public void InvokeOnFlow()
        {
            OnFlowEvent handler = OnFlow;
            if (handler != null) handler(this, GetCurrentColor());
        }

        public void InvokeOnNotifyBack()
        {
            OnNotifyBackEvent handler = OnNotifyBack;
            if (handler != null) handler(this, GetSignal());
        }
        public override string ToString()
        {
            return string.Format("{0} {1}", GetType(),Name);
        }
    }

    public abstract class ColorableClassWithSignalAndColor : ColorableClass
    {
        protected HashSet<BroadcastSignal> BroadcastSignals = new HashSet<BroadcastSignal>();
        protected Color CurrentColor;
        protected SerialSignal Signal;
        protected bool Flowed = false;
        private bool _signalled;

        protected bool Signalled{
            get { return _signalled; }
            set { _signalled = value; 
                    if(value)
                    {
                        Flowed = true;
                    }
            }
        }

       

        public override Color GetCurrentColor()
        {
            Color res;
            lock (this)
            {
                res = CurrentColor;
            }
            return res;
        }
        protected virtual void Clear()
        {
            Signal = null;
            CurrentColor = null;
            Flowed = false;
            Signalled = false;
            
        }
        public override Signal GetSignal()
        {
            Signal res;
            lock (this)
                res = Signal;
            return res;
        }
        protected bool FlowIfCan()
        {
            bool res;
            lock(this)
            {
                res = CanFlow();
                if (res)
                    Flowed = true;
            }
            return res;
        }

        protected void NotifyConnectionBack(IColorable connection)
        {
            if (connection != null && connection != this)
                new Thread(() => connection.NotifyBack(this, Signal)) { IsBackground = true }.Start(); //NotifyBack this connection
        }
        protected bool SignalIfCan()
        {
            bool res;
            lock (this)
            {
                res = CanSignal();
                if (res)
                    Signalled = true;
            }
            return res;
        }
        public override bool CanFlow()
        {
            return !Flowed;
        }

        public override bool CanSignal()
        {
            return !Signalled;
        }

        public override void Flow(IColorable connection, Color color)
        {
            if (color!=null)
                Flowed = true;
            System.Diagnostics.Debug.WriteLine(String.Format("\tFlowing {0} ({1})", color == null ? "null" : color.ToStringDemuxed(), ToString()));
        }

        protected override void NotifyBack(IColorable from, SerialSignal signal)
        {
            Signalled = true;
            System.Diagnostics.Debug.WriteLine(String.Format("\tSerial Signalling {0} ({1}, color={2})", signal, ToString(), CurrentColor == null ? "null" : CurrentColor.ToStringDemuxed()));
        }
        protected override void NotifyBack(IColorable from, BroadcastSignal signal)
        {
            bool contains;
            lock (BroadcastSignals)
            {
                contains = BroadcastSignals.Contains(signal);
                if (!contains)
                    BroadcastSignals.Add(signal);
            }
            if (contains)
                System.Diagnostics.Debug.WriteLine(String.Format("\tBroadcast repeated {0} ({1}, color={2})", signal, ToString(), CurrentColor == null ? "null" : CurrentColor.ToStringDemuxed()));
            else
                System.Diagnostics.Debug.WriteLine(String.Format("\tBroadcast {0} ({1}, color={2})", signal, ToString(), CurrentColor == null ? "null" : CurrentColor.ToStringDemuxed()));
            signal.Process(this, contains);
            InvokeOnNotifyBack();
            if (!contains)
            {
                SplitNotify(GetPrevPaths(), signal);
            }
        }

   

        protected Exception[] BuildExceptionChain(FilteredList<IColorable> connections)
        {
            FilteredList<IColorable>.ElementList output = connections.Filtered;
            Exception[] previousErrors = output.Select(conn => conn.GetSignal())
                  .Where(signal => signal is ErrorSignal)
                  .Select(signal => ((ErrorSignal) signal).Error).ToArray();
            return previousErrors;
        }
        protected void SplitNotify(IEnumerable<IColorable> connections, Signal signal)
        {
            var serialSignal = signal as SerialSignal;
            var broadcastSignal = signal as BroadcastSignal;
            if (serialSignal != null)
                SplitNotify(connections, serialSignal);
            else if (broadcastSignal != null)
                SplitNotify(connections, broadcastSignal);
        }
        private void SplitNotify(IEnumerable<IColorable> connections, SerialSignal signal)
        {
            var colorables = connections as IColorable[] ?? connections.ToArray();
            var splitSignal = signal.Split(colorables.Count());
            int i = 0;
            foreach (var connection in colorables)
            {

                var signali = splitSignal[i];
                if (connection == null)
                {
                    signal.StopSending();
                    continue;
                }
                IColorable connection1 = connection;
                new Thread(
                    () =>
                        connection1.NotifyBack(this, signali)
                    ) { IsBackground = true }.Start();
                i++;
            }

            splitSignal.WaitForAll();
            Signal.StopSending();
        }
        private void SplitNotify(IEnumerable<IColorable> connections, BroadcastSignal signal)
        {
            var colorables = connections as IColorable[] ?? connections.ToArray();
            if (colorables.Count() == 1)
            {
                colorables.First().NotifyBack(this, signal);
            }else
            foreach (var connection in colorables)
            {
                IColorable connection1 = connection;
                new Thread(
                    () =>
                    connection1.NotifyBack(this, signal)
                    ) {IsBackground = true}.Start();
            }
        }
    }
    public abstract class Vertex:ColorableClassWithSignalAndColor,IVertex
    {
        public int GetLengthBeetween(IColorable to)
        {
            var edge =
                GetNextPaths().FirstOrDefault(path => path == to || (path is IEdge && path.GetNextPaths().Contains(to)));
            if (edge == null)
                return -1;
            var edge1 = edge as IEdge;
            if (edge1 != null)
                return edge1.GetLength();
            return 1;

        }
        public static int GetLengthBeetween(Vertex from, Vertex to)
        {
            if (from == null)
                return -1;
            return from.GetLengthBeetween(to);


        }
    }
    public abstract class Edge : ColorableClassWithSignalAndColor,IEdge
    {
        public abstract int GetLength();
    }
}