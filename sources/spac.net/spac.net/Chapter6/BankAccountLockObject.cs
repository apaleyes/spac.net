namespace spac.net.Chapter6
{
    public class BankAccountLockObject
    {
        private readonly object lk = new object();

        private int _balance = 0;
        public int Balance
        {
            get
            {
                lock (lk)
                {
                    return _balance;
                }
            }
            set
            {
                lock (lk)
                {
                    _balance = value;
                }
            }
        }

        public void Withdraw(int amount)
        {
            lock (lk)
            {
                int b = Balance;
                if (amount > b)
                {
                    throw new WithdrawTooLargeException();
                }
                Balance = b - amount;
            }
        }

        // deposit and other operations
        // would also use lock (lk)
    }
}

namespace spac.net.Chapter6.Clients
{
    using BankAccount = BankAccountLockObject;

    public class BankAccountLockObjectClient
    {
        void doubleBalance(BankAccount acct)
        {
            acct.Balance = acct.Balance * 2;
        }

#if false
        void doubleBalance(BankAccount acct)
        {
            lock (acct.lk)
            {
                acct.Balance = acct.Balance * 2;
            }
        }
#endif
    }
}