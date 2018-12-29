using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WCFAssignment
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "BankServices" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select BankServices.svc or BankServices.svc.cs at the Solution Explorer and start debugging.
    public class BankServices : IBankServices
    {
        BankDataContextDataContext dt = new BankDataContextDataContext();

        public bool AddAccount(Account account)
        {
            try
            {
                string salt = Guid.NewGuid().ToString().Substring(0, 7);
                account.Salt = salt;
                string pin = "123456";
                var str = pin + salt;
                var MD5Pass = Encryptor.MD5Hash(str);
                account.Pin = MD5Pass;
                account.Balance = 50000;
                account.CreatedAt = DateTime.Now;
                account.UpdatedAt = DateTime.Now;
                account.Status = 0;
                dt.Accounts.InsertOnSubmit(account);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddCustomer(Customer cus)
        {
            try
            {
                if(dt.Customers.Any(y=>y.Id == cus.Id))
                {
                    return false;
                }

                var id = cus.Id;
                Account acount = (from ac in dt.Accounts
                              where ac.AccountNumber == id
                              && ac.Status == 0
                              select ac).Single();
                if(acount == null)
                {
                    return false;
                }
                cus.CreatedAt = DateTime.Now;
                cus.UpdatedAt = DateTime.Now;
                acount.Status = 1;

                dt.Customers.InsertOnSubmit(cus);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddHistory(HistoryTransaction ht)
        {
            try
            {
                dt.HistoryTransactions.InsertOnSubmit(ht);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddPartner(Partner partner)
        {
            try
            {
                if(dt.Partners.Any(y=>y.AccountNumber == partner.AccountNumber))
                {
                    return false;
                }

                var id = partner.AccountNumber;

                Account acount = (from ac in dt.Accounts
                                  where ac.AccountNumber == id
                                  && ac.Status == 1
                                  select ac).Single();
                if(acount == null)
                {
                    return false;
                }

                string salt = Guid.NewGuid().ToString().Substring(0, 7);
                partner.Salt = salt;
                string pin = "123456";
                var str = pin + salt;
                var MD5Pass = Encryptor.MD5Hash(str);
                partner.Password = MD5Pass;
                partner.Status = 1;

                dt.Partners.InsertOnSubmit(partner);
                dt.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AddTransaction(Transaction transaction)
        {
            try
            {
                var payid = 2;

                Account senderAC = (from ac in dt.Accounts
                                    where ac.AccountNumber == transaction.SenderAccountNumber
                                    && ac.Status == 1
                                    select ac).Single();

                Account receiverAccount = (from ac in dt.Accounts
                                           where ac.AccountNumber == transaction.ReceiverAccountNumber
                                           && ac.Status == 1
                                           select ac).Single();

                Account paypal = (from ac in dt.Accounts
                                  where ac.AccountNumber == payid
                                  && ac.Status == 1
                                  select ac).Single();

                var queryBalanceSender = (from b in dt.Accounts
                                    where b.AccountNumber == transaction.SenderAccountNumber
                                    && b.Status == 1
                                    select b.Balance).FirstOrDefault().ToString();
                if(queryBalanceSender == null)
                {
                    return false;
                }

                var queryBalanceReceiver = (from b in dt.Accounts
                                            where b.AccountNumber == transaction.ReceiverAccountNumber
                                            && b.Status == 1
                                            select b.Balance).FirstOrDefault().ToString();

                if(queryBalanceReceiver == null)
                {
                    return false;
                }

                var queryBalancePaypal = (from b in dt.Accounts
                                          where b.AccountNumber == payid
                                          && b.Status == 1
                                          select b.Balance).FirstOrDefault().ToString();

                if(queryBalancePaypal == null)
                {
                    return false;
                }

                decimal fee = 0;
                var amount = Convert.ToInt64(transaction.Amount);
                if(amount <= 100000)
                {
                    fee = 10000;
                }
                else if(100000 < amount && amount <= 500000)
                {
                    long i = (2 * amount) / 100;
                    fee = i;
                }
                else if(500000 < amount && amount <= 1000000)
                {
                    long i = Convert.ToInt64(1.5 * amount) / 100;
                    fee = i;
                }
                else if(1000000 < amount && amount <= 5000000)
                {
                    long i = (1 * amount) / 100;
                    fee = i;
                }else if(amount > 5000000)
                {
                    long i = Convert.ToInt64(0.5 * amount) / 100;
                }

                if((amount + fee) > Convert.ToInt64(queryBalanceSender))
                {
                    return false;
                }

                senderAC.Balance = Convert.ToInt64(queryBalanceSender) - amount - fee;
                senderAC.UpdatedAt = DateTime.Now;

                receiverAccount.Balance = Convert.ToInt64(queryBalanceReceiver) + amount;
                receiverAccount.UpdatedAt = DateTime.Now;

                paypal.Balance = Convert.ToInt64(queryBalancePaypal) + fee;
                paypal.UpdatedAt = DateTime.Now;

                transaction.FeeTransaction = fee;
                dt.Transactions.InsertOnSubmit(transaction);
                dt.SubmitChanges();
                return true;

            }
            catch
            {
                return false;
            }

        }

        public Partner GetPartnerById(long id)
        {
            var account = dt.Partners.Where(p => p.PartnerAccount == id).FirstOrDefault();
            return account;
        }

        public List<Transaction> GetTransactionListAll(string id)
        {
            try
            {
                return (from ht in dt.Transactions
                        where ht.SenderAccountNumber == Convert.ToInt64(id)
                         || ht.ReceiverAccountNumber == Convert.ToInt64(id)
                        select ht).ToList();
            }
            catch
            {
                return null;
            }
        }

        public bool LoginAccount(Account account)
        {
            try
            {
                string pass = account.Pin.ToString();

                var salt = (from c in dt.Accounts
                                  where c.AccountNumber == account.AccountNumber
                                  select c.Salt).FirstOrDefault().ToString();

                if (salt == null)
                {
                    return false;
                }
                var str = pass + salt;
                var MD5Pass = Encryptor.MD5Hash(str);

                var acount = dt.Accounts.Where(x => x.AccountNumber == account.AccountNumber && x.Pin == MD5Pass).FirstOrDefault();
                if (acount == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool LoginPartnerAccount(Partner partner)
        {
            try
            {
                string pass = partner.Password.ToString();

                var salt = (from c in dt.Partners
                                  where c.PartnerAccount == partner.PartnerAccount
                                  select c.Salt).FirstOrDefault().ToString();

                if (salt == null)
                {
                    return false;
                }
                var str = pass + salt;
                var MD5Pass = Encryptor.MD5Hash(str);

                var acount = dt.Partners.Where(x => x.PartnerAccount == partner.PartnerAccount && x.Password == MD5Pass).FirstOrDefault();
                if (acount == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
