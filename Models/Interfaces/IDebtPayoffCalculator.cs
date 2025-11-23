namespace DebtSnowballApp.Services.Interfaces
{
    using System.Collections.Generic;
    using DebtSnowballApp.Models.Interfaces; // for IDebtLike or model references
    using DebtSnowballApp.Models.Payoff;

    /// <summary>
    /// Defines a generic payoff calculator contract that can handle any debt-like type.
    /// </summary>
    public interface IDebtPayoffCalculator<TDebt>
        where TDebt : IDebtLike
    {
        PayoffResult Calculate(IEnumerable<TDebt> debts, decimal extraPayment, string strategy);
    }
}
