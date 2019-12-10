using Microsoft.Extensions.Configuration;
using NumberSearch.DataAccess;

using System.Threading.Tasks;

namespace NumberSearch.Ingest
{
    public interface IPhoneNumberProvider
    {
        Task<IngestStatistics> IngestPhoneNumbersAsync(IConfiguration configuration);
        Task<int[]> GetValidNPAsAsync(IConfiguration configuration);
        Task<int[]> GetVaildNXXsAsync(int npa, IConfiguration configuration);
        Task<PhoneNumber[]> GetVaildXXXXsAsync(int npa, int nxx, IConfiguration configuration);
        Task<IngestStatistics> SubmitPhoneNumbersAsync(PhoneNumber[] numbers, IConfiguration configuration);
    }
}
