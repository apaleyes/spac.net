#if false
namespace spac.net.Chapter6
{
    public class BankAccountLockPseudocode
    {
        private int _balance = 0;
        public int Balance
        {
            get { return _balance; }
            set { _balance = value; }
        }

        private Lock lk = new Lock();

        public void Withdraw(int amount)
        {
            lk.Acquire(); /* may block execution */
            int b = Balance;
            if (amount > b)
            {
                throw new WithdrawTooLargeException();
            }
            Balance = b;
            lk.Release();
        }

        // deposit would also acquire/release lk
    }
}
#endif
