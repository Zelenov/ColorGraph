using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ColorGraph
{
    public enum SignalType
    {
        Error,
        Stop,
        Undo,
        Done
    };
    public class Signal:ICloneable
    {
        public virtual void Process(ColorableClass obj, bool repetition) { }
        public virtual object Clone()
        {
            Signal clone = new Signal();
            return clone;
        }


         
      /*  public static bool IsError(Signal signal)
        {
            return (signal != null && signal.Error != null);
        }*/

    }
    public class SerialSignal:Signal
    {  
        protected ManualResetEvent SendingEvent = new ManualResetEvent(false);


        public void WaitForSending()
        {
            SendingEvent.WaitOne();
        }
        public void StartSending()
        {
            SendingEvent.Set();
        }
        public void StopSending()
        {
            SendingEvent.Reset();
        }
        public override object Clone()
        {
            SerialSignal clone = new SerialSignal();
            return clone;
        }
        public SignalGroup Split(int count)
        {
            if (count < 1)
                return new SignalGroup();
            SerialSignal[] array = new SerialSignal[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = (SerialSignal)Clone();
            }
            return new SignalGroup(array);
        }

    }
    public class BroadcastSignal:Signal
    {
        private static int _currentId;

        protected int Id { get; private set; }
        public BroadcastSignal()
        {
            Id = Interlocked.Increment(ref _currentId);
        }
        public override int GetHashCode()
        {
            return Id;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            var signal = obj as BroadcastSignal;
            if (signal == null)
            {
                return false;
            }
            return signal.Id == Id;
        }
    }
    
    public class SignalGroup:IEnumerable<SerialSignal>
    {
        protected List<SerialSignal> Signals;
        public SignalGroup()
        {
            Signals = new List<SerialSignal>();
        }
        public SignalGroup(IEnumerable<SerialSignal> signals)
        {
            Signals = signals.ToList();
        }
        public void WaitForAll()
        {
            foreach (var signal in Signals)
            {
                signal.WaitForSending();
            }
        }


        public IEnumerator<SerialSignal> GetEnumerator()
        {
            return Signals.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public SerialSignal this[int i]
        {
            get { return Signals[i]; }
        }
    }
}
