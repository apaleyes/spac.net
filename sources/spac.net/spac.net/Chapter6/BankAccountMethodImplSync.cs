using System.Runtime.CompilerServices;

namespace spac.net.Chapter6
{
    public class BankAccountMethodImplSync
    {
        private int _balance = 0;
        public int Balance
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _balance; }
            [MethodImpl(MethodImplOptions.Synchronized)]
            set { _balance = value; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Withdraw(int amount)
        {
            int b = Balance;
            if (amount > b)
            {
                throw new WithdrawTooLargeException();
            }
            Balance = b - amount;
        }

        // deposit and other operations
        // would also use [MethodImpl(MethodImplOptions.Synchronized)]
    }
}
