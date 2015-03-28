namespace spac.net.Chapter6.BankAccountBoolWrong
{
    public class BankAccount
    {
        private int _balance = 0;
        public int Balance
        {
            get { return _balance; }
            set { _balance = value; }
        }
        private bool busy = false;

        public void Withdraw(int amount)
        {
            while (busy)
            { /* spin-wait */ }
            busy = true;

            if (amount > Balance)
            {
                throw new WithdrawTooLargeException();
            }

            Balance -= amount;

            busy = false;
        }
    }
}
