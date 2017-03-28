namespace spac.net.Chapter6
{
    public class BankAccountLockThis
    {
        private int _balance = 0;
        public int Balance
        {
            get
            {
                lock (this)
                {
                    return _balance;
                }
            }
            set
            {
                lock (this)
                {
                    _balance = value;
                }
            }
        }

        public void Withdraw(int amount)
        {
            lock (this)
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
        // would also use lock (this)
    }
}

namespace spac.net.Chapter6.Clients
{
    using BankAccount = BankAccountLockThis;

    public class BankAccountLockThisClient
    {
        void DoubleBalance(BankAccount acct)
        {
            lock (acct)
            {
                acct.Balance = acct.Balance * 2;
            }
        }
    }
}