public class AmortizationRow
{
    public int Month { get; set; }
    public decimal StartingBalance { get; set; }
    public decimal Payment { get; set; }
    public decimal Interest { get; set; }
    public decimal EndingBalance { get; set; }
    public bool IsTarget { get; set; } // ✅ True if this debt was the snowball target that month


    public decimal Principal => Payment - Interest;
}
