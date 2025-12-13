using DebtSnowballApp.Models;

namespace DebtSnowballApp.Services
{
    public class EmailBuilder
    {
        public static string BuildStep3EmailBody(QuickAnalysisResultViewModel m)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"" />
  <title>Quick Analysis Results</title>
</head>
<body style=""font-family: Arial, Helvetica, sans-serif; background-color:#f5f5f5; padding:20px;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
    <tr>
      <td align=""center"">
        <table width=""700"" cellpadding=""0"" cellspacing=""0"" 
               style=""background-color:#ffffff; border-radius:6px; padding:20px;"">
          <tr>
            <td style=""text-align:left; font-size:20px; font-weight:bold;"">
              Quick Analysis
            </td>
            <td style=""text-align:right; font-size:12px; text-transform:uppercase; color:#777;"">
              Prepared for<br />
              <span style=""font-size:18px; font-weight:600; text-transform:none;"">
                {System.Net.WebUtility.HtmlEncode(m.ClientName)}
              </span>
            </td>
          </tr>

          <!-- Comparison table -->
          <tr>
            <td colspan=""2"" style=""padding-top:20px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" 
                     style=""border-collapse:collapse; border:1px solid #ccc;"">
                <tr>
                  <td colspan=""3"" 
                      style=""background-color:#f8f8f8; text-align:center; font-weight:bold; padding:8px;"">
                    Current Debt vs. My Financial Outlook
                  </td>
                </tr>
                <tr style=""background-color:#e9ecef; font-weight:bold;"">
                  <td style=""padding:6px;""></td>
                  <td style=""padding:6px; text-align:right;"">Current</td>
                  <td style=""padding:6px; text-align:right;"">My Plan</td>
                </tr>

                <tr>
                  <td style=""padding:6px;"">Total Debt</td>
                  <td style=""padding:6px; text-align:right;"">{m.CurrentTotalDebt:C0}</td>
                  <td style=""padding:6px; text-align:right; color:#198754; font-weight:600;"">
                    {m.PlanTotalDebt:C0}
                  </td>
                </tr>

                <tr style=""background-color:#f8f9fa;"">
                  <td style=""padding:6px;"">Monthly Debt Payment</td>
                  <td style=""padding:6px; text-align:right;"">{m.CurrentMonthlyDebt:C0}</td>
                  <td style=""padding:6px; text-align:right; color:#198754; font-weight:600;"">
                    {m.PlanMonthlyDebt:C0}
                  </td>
                </tr>

                <tr>
                  <td style=""padding:6px;"">Debt Freedom</td>
                  <td style=""padding:6px; text-align:right;"">{m.CurrentDebtFreedomYears:0.0} years</td>
                  <td style=""padding:6px; text-align:right; color:#198754; font-weight:600;"">
                    {m.PlanDebtFreedomYears:0.0} years
                  </td>
                </tr>

                <tr style=""background-color:#f8f9fa;"">
                  <td style=""padding:6px;"">Principal &amp; Interest Paid</td>
                  <td style=""padding:6px; text-align:right;"">{m.CurrentPrincipalAndInterest:C0}</td>
                  <td style=""padding:6px; text-align:right; color:#198754; font-weight:600;"">
                    {m.PlanPrincipalAndInterest:C0}
                  </td>
                </tr>

                <tr>
                  <td colspan=""2"" 
                      style=""padding:8px; text-align:right; color:#198754; font-weight:bold; font-size:18px;"">
                    Total Savings
                  </td>
                  <td style=""padding:8px; text-align:right; color:#198754; font-weight:bold; font-size:18px;"">
                    {m.TotalSavings:C0}
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Cost of Not Taking Action -->
          <tr>
            <td colspan=""2"" style=""padding-top:20px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" 
                     style=""border:1px solid #ccc; border-radius:4px;"">
                <tr>
                  <td style=""padding:12px; text-align:center;"">
                    <div style=""font-weight:bold; text-transform:uppercase; margin-bottom:4px;"">
                      Cost of Not Taking Action
                    </div>
                    <div style=""font-size:13px; margin-bottom:6px;"">
                      <strong>Every day</strong> you delay starting this plan costs you approximately:
                    </div>
                    <div style=""font-size:32px; font-weight:bold; color:#dc3545; margin-bottom:4px;"">
                      {m.CostPerDay:C0}
                    </div>
                    <div style=""font-size:12px; color:#777;"">
                      in lost future wealth*
                    </div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Wealth Builder -->
          <tr>
            <td colspan=""2"" style=""padding-top:20px;"">
              <table width=""100%"" cellpadding=""0"" cellspacing=""0"" 
                     style=""border:1px solid #ccc; border-radius:4px; padding:12px;"">
                <tr>
                  <td style=""font-size:16px; font-weight:600;"">
                    Wealth Builder
                  </td>
                  <td style=""text-align:right;"">
                    <div style=""font-size:11px; font-weight:bold; text-transform:uppercase;"">
                      Projected Total Wealth
                    </div>
                    <div style=""font-size:22px; font-weight:bold; color:#dc3545;"">
                      {m.WealthBuilderTotalWealth:C0}
                    </div>
                  </td>
                </tr>

                <tr>
                  <td colspan=""2"">
                    <hr style=""border:none; border-top:1px solid #ddd; margin:8px 0;"" />
                  </td>
                </tr>

                <tr>
                  <td colspan=""2"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""text-align:center; font-size:13px;"">
                      <tr>
                        <td width=""33%"">
                          <div style=""font-size:18px; font-weight:bold;"">
                            {m.WealthBuilderMonthlyContribution:C0}
                          </div>
                          <div style=""text-transform:uppercase; color:#777;"">
                            Monthly Investment
                          </div>
                        </td>
                        <td width=""33%"">
                          <div style=""font-size:18px; font-weight:bold;"">
                            {m.WealthBuilderPeriodYears:0.0} years
                          </div>
                          <div style=""text-transform:uppercase; color:#777;"">
                            Investment Period
                          </div>
                        </td>
                        <td width=""33%"">
                          <div style=""font-size:18px; font-weight:bold;"">
                            {m.WealthBuilderRoiPercent:0.#}%
                          </div>
                          <div style=""text-transform:uppercase; color:#777;"">
                            Annual Return
                          </div>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>

                <tr>
                  <td colspan=""2"" style=""padding-top:8px; font-size:12px; color:#777;"">
                    Once you are debt free, redirecting your monthly debt payments into conservative
                    investments over the years you've saved can build significant long-term wealth.
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <tr>
            <td colspan=""2"" style=""padding-top:12px; font-size:11px; color:#999;"">
              * Based on your current debt, payoff time, and an assumed {m.WealthBuilderRoiPercent:0.#}% annual return.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
        }

    }
}
