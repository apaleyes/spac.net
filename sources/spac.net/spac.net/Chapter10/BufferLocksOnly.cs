namespace spac.net.Chapter10
{
    class BufferLocksOnly<E>
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
                    if (!IsFull())
                    {
                        // do enqueue
                        return;
                    }
                }
            }
        }

        E Dequeue()
        {
            while (true)
            {
                lock (this)
                {
                    if (!IsEmpty())
                    {
                        E result = default(E);
                        // do dequeue
                        return result;
                    }
                }
            }
        }
    }
}
