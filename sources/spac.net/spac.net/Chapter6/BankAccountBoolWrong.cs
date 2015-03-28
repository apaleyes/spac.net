namespace spac.net.Chapter6.BankAccountBoolWrong
{
    public class BankAccount
    {
        public int Balance { get; set; }
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
