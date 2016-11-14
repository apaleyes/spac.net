using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace spac.net.Chapter9
{
    class BankAccount
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        void Withdraw(int amt) { }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void Deposit(int amt) { }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void TransferToWrong(int amt, BankAccount a)
        {
            this.Withdraw(amt);
            a.Deposit(amt);
        }

        void TransferToUnsync(int amt, BankAccount a)
        {
            this.Withdraw(amt);
            a.Deposit(amt);
        }
        
        void TransferToAtomicWrong(int amt, BankAccount a)
        {
            lock (this)
            {
                lock (a)
                {
                    this.Withdraw(amt);
                    a.Deposit(amt);
                }
            }
        }

        private int acctNumber;
        void transfertToAtomic(int amt, BankAccount a)
        {
            if (this.acctNumber < a.acctNumber)
            {
                lock (this)
                {
                    lock (a)
                    {
                        this.Withdraw(amt);
                        a.Deposit(amt);
                    }
                }
            }
            else
            {
                lock (a)
                {
                    lock (this)
                    {
                        this.Withdraw(amt);
                        a.Deposit(amt);
                    }
                }
            }
        }
    }
}
