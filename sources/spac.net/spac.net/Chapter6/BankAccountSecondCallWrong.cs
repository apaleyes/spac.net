namespace spac.net.Chapter6
{
    class BankAccountSecondCallWrong
    {
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
            // maybe balance changed, so get the new balance
            Balance = Balance - amount;
        }
    }
}
