using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace IncuSmart.Infra.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinaryOptions> options)
        {
            var opt = options.Value;
            var account = new Account(opt.CloudName, opt.ApiKey, opt.ApiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<string> UploadImageAsync(Stream fileStream, string fileName)
        {
            var uploadParams = new ImageUploadParams
            {
                File   = new FileDescription(fileName, fileStream),
                Folder = "incusmart/models",
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new Exception($"Cloudinary upload failed: {result.Error.Message}");

            return result.SecureUrl.ToString();
        }
    }
}
