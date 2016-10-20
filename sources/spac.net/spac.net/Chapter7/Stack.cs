using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spac.net.Chapter7
{
    class Stack<E>
    {
        private E[] array;
        private int index = 0;

        public Stack(int size)
        {
            array = new E[size];
        }

        public bool IsEmpty()
        {
            lock (this)
            {
                return index == 0;
            }
        }

        public void Push(E val)
        {
            lock (this)
            {
                if (index == array.Length)
                {
                    throw new StackFullException();
                }
                array[index++] = val;
            }
        }

        public E Pop()
        {
            lock (this)
            {
                if (index == 0)
                {
                    throw new StackEmptyException();
                }
                return array[--index];
            }
        }

        public E Peek1()
        {
            lock (this)
            {
                if (index == 0)
                {
                    throw new StackEmptyException();
                }
                return array[index - 1];
            }
        }

        public E Peek2()
        {
            lock (this)
            {
                E ans = Pop();
                Push(ans);
                return ans;
            }
        }
    }

    class StackFullException : Exception
    { }

    class StackEmptyException :  Exception
    { }
}
