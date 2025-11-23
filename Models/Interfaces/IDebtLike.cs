    namespace DebtSnowballApp.Models.Interfaces
    {
        /// <summary>
        /// Common contract for any debt-like entity (e.g. DebtItem, QaDebtItem)
        /// so payoff calculators can work generically across them.
        /// </summary>
        public interface IDebtLike
        {
            int Id { get; }
            string Name { get; }
            decimal Balance { get; }
            decimal InterestRate { get; }
            decimal MinimumPayment { get; }
        }
    }
