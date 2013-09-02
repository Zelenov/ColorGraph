using System.Collections.Generic;
using System.Linq;
using ColorGraph.Lists;


namespace ColorGraph
{


    //public delegate void OnFinishEvent(GraphNode sender, Signal result);


    public abstract class GraphNode : Vertex, IUndoable
    {
       
        public FilteredList<IColorable> Input = new FilteredList<IColorable>();
        public ZSortedList<IColorable> InvisiblePoints = new ZSortedList<IColorable>(); 
        public readonly OutPoint Output;
        private bool _alreadyUndone;

        public Color Result { get { return CurrentColor; } set { CurrentColor = value; } }
      
      
       
        public GraphNode()
        {
            Input.OnAllFiltered += OnAllInputsFlowed;
            Output = new OutPoint(this);
        }
        protected override void Clear()
        {
            lock (this)
            {
                base.Clear();
                Input.ClearFilters();
            }
        }
        public IEnumerable<IColorable> GetNotNullInput()
        {
            return
                Input.Where(
                    inPoint =>
                    (inPoint != null) && (inPoint.GetCurrentColor() != null) && (!InvisiblePoints.Contains(inPoint)));
        }
    


        protected InPoint AddInvisiblePoint(InPoint inConn = null)
        {
            var res = AddInConnection(inConn);
            InvisiblePoints.Add(res);
            return res;
        }

        public InPoint AddInConnection(InPoint inConn=null)
        {
            if (inConn==null)
                inConn = new InPoint(this);
            Input.Add(inConn);
            return inConn;
        }

        
        protected void FlowResult(Color output)
        {
            Result = output;
            FlowResult();
        }
        protected void GoBack(Signal stop)
        {
            NotifyBack(this,stop);
        }
        protected void FlowResult()
        {
            
            if (!CanSignal())
                return;
            InvokeOnFlow();
            Output.Flow(this, Result);

        }

        

        

       
       /* public IEnumerable<GraphConnection> GetAllInConnections()
        {
            var res = new List<GraphConnection>();
            foreach (var connectionPoint in Input)
            {
                if (connectionPoint != null) res.AddRange(connectionPoint.GetAllConnections());
            }
            return res;
        }
        public IEnumerable<GraphConnection> GetAllOutConnections()
        {
            var res = new List<GraphConnection>();
            foreach (var connectionPoint in Output)
            {
                if (connectionPoint != null) res.AddRange(connectionPoint.GetAllConnections());
            }
            return res;
        }*/



        public abstract void DoWork();
        public abstract void Stop();
        public abstract void Undo();

        public override ColorableClass ConnectTo(GraphNode outNode, int length = 1)
        {
            return Output.ConnectTo(outNode,length);
        }
        public override ColorableClass ConnectTo(SerialOutPoint outPoint, int length = 1)
        {
            return Output.ConnectTo(outPoint, length);
        }
        public override ColorableClass ConnectTo(InPoint inPoint, int length = 1)
        {
            return Output.ConnectTo(inPoint, length);
        }

        public override Color GetCurrentColor()
        {
            return Result;
        }
        public override Signal GetSignal()
        {
            return Signal;
        }

        public override IEnumerable<IColorable> GetNextPaths()
        {
            return new IColorable[]{Output};
        }
        public override IEnumerable<IColorable> GetPrevPaths()
        {
            return Input;
        }
        public override void Flow(IColorable connection, Color newColor)
        {
            //TODO add "this"
            if (!(connection is InPoint))
                return;
            Input.Filter(connection);
            /*
            if (!Input.IsFiltered(connection));
            if (doFlow)
            {

                base.Flow(connection, newColor);
                CurrentColor = newColor;Undo()
                DoWork(); //Flow
            }
            else
                NotifyConnectionBack(connection);*/
        }
        protected virtual void OnAllInputsFlowed(FilteredList<IColorable> sender)
        {
            if (!GetNotNullInput().Any())
            {
                Clear();
                //InvokeOnFlow();
                FlowResult(null);
            }
            else
            {
                if (!FlowIfCan())
                    return;
                base.Flow(this, Color.Mix(Input.Select(inConn=>inConn.GetCurrentColor())));
                CurrentColor = null;
                DoWork(); //Flow
            }


        }

        //step back
        protected override void NotifyBack(IColorable from, SerialSignal signal)
        {
            if (from != Output && from!=this)
                return;
     
            if (!SignalIfCan())
                return;
            Signal = signal;
               
            base.NotifyBack(this, signal);
            InvokeOnNotifyBack();
            SplitNotify(Input,signal);
        }
        


        public bool GetAlreadyUndone()
        {
            return _alreadyUndone;
        }

        void IUndoable.SetAlreadyUndone(bool value)
        {
            _alreadyUndone = value;
        }

    }
    


}
