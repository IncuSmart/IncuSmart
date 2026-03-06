namespace IncuSmart.Core.Services
{
    public interface ISMSService
    {
        Task<SMSResponse> SendSMSAsync(SMSDto dto);
    }

}
