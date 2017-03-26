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
        private bool _busy = false;

        public void Withdraw(int amount)
        {
            while (_busy)
            { /* spin-wait */ }
            _busy = true;

            if (amount > Balance)
            {
                throw new WithdrawTooLargeException();
            }

            Balance -= amount;

            _busy = false;
        }
    }
}
