

namespace ColorGraph
{
    public interface IUndoable
    {
        bool GetAlreadyUndone();
        void SetAlreadyUndone(bool value);
    }

}
