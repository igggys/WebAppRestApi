using System.Collections.Generic;

namespace WebService.DataLayer
{
    public interface IDataManager
    {
        void DeleteById(int id);
        List<object> ReadDataByFilter(string filter);
        bool Update(int id, string propertyName, string propertyValue);
    }
}