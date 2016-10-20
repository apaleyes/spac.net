using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace spac.net.Chapter7
{
    class CorrectStackHelper
    {
        static E PeekHelper<E>(Stack<E> s)
        {
            lock (s)
            {
                E ans = s.Pop();
                s.Push(ans);
                return ans;
            }
        }
    }

    class WrongStackHelper
    {
        static E PeekHelper<E>(Stack<E> s)
        {
            E ans = s.Pop();
            s.Push(ans);
            return ans;
        }
    }
}