namespace IncuSmart.API.Mappers
{
    public class FileUploadMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<UploadFileRequest, UploadFileCommand>();
        }
    }
}
