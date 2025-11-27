// Services/IQuickAnalysisCalculator.cs
using DebtSnowballApp.Models;
//using DebtSnowballApp.ViewModels.QuickAnalysis;
//using System.Threading.Tasks;

namespace DebtSnowballApp.Services
{
    public interface IQuickAnalysisCalculator
    {
        Task<QuickAnalysisResultViewModel> CalculateAsync(string ownerId);
    }
}
