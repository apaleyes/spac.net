namespace spac.net.Chapter6.BankAccountSingleThread
{
    public class BankAccount
    {
        public int Balance { get; set; }

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
