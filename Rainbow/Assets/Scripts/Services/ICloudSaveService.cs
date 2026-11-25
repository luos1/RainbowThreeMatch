using System.Threading.Tasks;

namespace Rainbow.Services
{
    public interface ICloudSaveService
    {
        Task<SaveData> Save(string uid, SaveData data);
        Task<SaveData> Load(string uid);
    }
}
