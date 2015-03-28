namespace spac.net.Chapter6.BankAccountSingleThread
{
    public class BankAccount
    {
        // No auto property for consistency with
        // further variations of the BankAccoutn class
        private int _balance = 0;
        public int Balance
        {
            get { return _balance; }
            set { _balance = value; }
        }

        public void Withdraw(int amount)
        {
            if (amount > Balance)
            {
                throw new WithdrawTooLargeException();
            }

            Balance -= amount;
        }

        // ... other operations like deposit, etc.
    }
}
