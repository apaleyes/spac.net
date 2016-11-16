using System.Threading;

namespace spac.net.Chapter10
{
    class BufferRight<E>
    {
        bool IsEmpty() { return true; }
        bool IsFull() { return true; }

        // not shown: an array of fixed size for the queue with two indices
        // for the front and back, along with methods IsEmpty() and IsFull()
        void Enqueue(E elt)
        {
            while (true)
            {
                lock (this)
                {
                    while (IsFull())
                    {
                        Monitor.Wait(this);
                    }
                    // do enqueue as normal
                    //Uncomment this: if (...buffer was empty (i.e., now has 1 element) ...)
                    Monitor.PulseAll(this);
                }
            }
        }

        E Dequeue()
        {
            while (true)
            {
                lock (this)
                {
                    while (IsEmpty())
                    {
                        Monitor.Wait(this);
                    }
                    E ans = default(E);
                    // E ans = do dequeue as normal
                    //Uncomment this: if(... buffer was full (i.e., now has room for 1 element) ...)
                    Monitor.PulseAll(this);
                    return ans;
                }
            }
        }
    }
}
