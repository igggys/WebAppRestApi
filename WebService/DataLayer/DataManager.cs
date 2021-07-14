using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using WebService.BusinessLogic.Settings;
using WebService.BusinessLogic.Models;
using Microsoft.Extensions.Logging;

namespace WebService.DataLayer
{
    public class DataManager : IDataManager
    {
        private readonly IApplicationSettings applicationSettings;

        private readonly ILogger<DataManager> logger;

        private List<object> statisticalCitizens = null;

        private List<MetaDataInfo> metaDataInfoCollection = null;

        static object locker = new object();

        public DataManager(IApplicationSettings applicationSettings, ILogger<DataManager> logger)
        {
            this.applicationSettings = applicationSettings;
            this.logger = logger;

            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName)))
            {
                logger.LogInformation("Creation CSV file");
                CreationCsvFile();
            }

            ReadData();
        }

        private void CreationCsvFile()
        {
            try
            {
                lock (locker)
                {
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName)))
                    {
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName));
                    }

                    metaDataInfoCollection = new List<MetaDataInfo>();

                    List<string> fields = new List<string>();

                    SqliteConnection connection = new SqliteConnection($"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.applicationSettings.SqliteFileName)}");
                    connection.Open();

                    SqliteCommand command = new SqliteCommand("SELECT name FROM PRAGMA_TABLE_INFO('records')", connection);

                    SqliteDataReader sqliteDataReader = command.ExecuteReader();

                    while (sqliteDataReader.Read())
                    {
                        fields.Add(sqliteDataReader.GetString(0));
                    }

                    sqliteDataReader.Close();

                    StringBuilder sql_select = new StringBuilder();

                    StringBuilder sql_join = new StringBuilder();

                    foreach (var field in fields)
                    {
                        if (field.IndexOf("_id") > -1)
                        {
                            string tableName = field.Substring(0, field.IndexOf("_id"));

                            if (tableName[tableName.Length - 1] == 's' || tableName[tableName.Length - 1] == 'x')
                            {
                                tableName += "es";
                            }
                            else if (tableName[tableName.Length - 1] == 'y')
                            {
                                tableName = tableName.Substring(0, tableName.Length - 1) + "ies";
                            }
                            else
                            {
                                tableName += "s";
                            }

                            sql_join.Append($"\r\n inner join {tableName} on {tableName}.id = records.{field}");

                            string fieldName = field.Replace("_id", "_name");

                            sql_select.Append($" {tableName}.name as {fieldName},");

                            metaDataInfoCollection.Add(new MetaDataInfo { PropertyName = field.Replace("_id", string.Empty), PropertyType = "System.String" });
                        }
                        else
                        {
                            sql_select.Append($" records.{field},");

                            metaDataInfoCollection.Add(new MetaDataInfo { PropertyName = field, PropertyType = "System.Int32" });
                        }
                    }

                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.DataInfoFileName)))
                    {
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.DataInfoFileName));
                    }

                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.DataInfoFileName), JsonConvert.SerializeObject(metaDataInfoCollection));

                    string select = sql_select.ToString();

                    select = select.Remove(select.Length - 1);

                    sql_select.Append(" from records ");

                    string sql = "select " + select + " from records " + sql_join.ToString();

                    StringBuilder stringBuilderCsv = new StringBuilder();

                    foreach (var field in fields)
                    {
                        stringBuilderCsv.Append($"{field},");
                    }

                    stringBuilderCsv.Append("\r\n");

                    command.CommandText = sql;

                    sqliteDataReader = command.ExecuteReader();

                    while (sqliteDataReader.Read())
                    {
                        for (int i = 0; i < fields.Count; ++i)
                        {
                            stringBuilderCsv.Append(sqliteDataReader.GetString(i) + ",");
                        }
                        stringBuilderCsv.Append("\r\n");

                    }

                    sqliteDataReader.Close();

                    connection.Close();

                    string csvContent = stringBuilderCsv.ToString().Replace(",\r\n", "\r\n").Replace("_id", string.Empty);

                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName), csvContent);
                }
            }
            catch
            {
                logger.LogError("Error DataManager.CreationCsvFile()");
                throw;
            }
        }

        private void ReadData()
        {
            StringBuilder result = new StringBuilder();
            try
            {
                lock (locker)
                {
                    metaDataInfoCollection = JsonConvert.DeserializeObject<List<MetaDataInfo>>(
                        File.ReadAllText(
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.DataInfoFileName)));

                    string[] csvContent = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName));

                    string[] propertyNames = csvContent[0].Split(',');

                    for (int i = 1; i < csvContent.Length; ++i)
                    {
                        string[] itemsContent = csvContent[i].Split(',');

                        StringBuilder dataItem = new StringBuilder();
                        dataItem.Append("{\r\n");

                        for (int j = 0; j < itemsContent.Length; ++j)
                        {
                            MetaDataInfo property = metaDataInfoCollection.FirstOrDefault(item => item.PropertyName == propertyNames[j]);

                            if (property.PropertyType == "System.String")
                            {
                                dataItem.Append("\t'" + propertyNames[j] + "': " + "'" + itemsContent[j] + "',\r\n");
                            }
                            else if (property.PropertyType == "System.Int32")
                            {
                                dataItem.Append("\t'" + propertyNames[j] + "': " + itemsContent[j] + ",\r\n");
                            }
                        }

                        string itemJson = dataItem.ToString();

                        itemJson = itemJson.Substring(0, itemJson.Length - 3) + "\t\r\n},\r\n";

                        result.Append(itemJson);
                    }

                    string itemsJson = result.ToString();

                    itemsJson = "[" + itemsJson.Substring(0, itemsJson.Length - 3) + "]";

                    statisticalCitizens = JsonConvert.DeserializeObject<List<dynamic>>(itemsJson);
                }
            }
            catch
            {
                logger.LogError("Error DataManager.ReadData()");
                throw;
            }
        }

        private void SaveChangesInCsvFile()
        {
            try
            {
                lock (locker)
                {
                    StringBuilder stringBuilderCsv = new StringBuilder();

                    foreach (var title in metaDataInfoCollection)
                    {
                        stringBuilderCsv.Append($"{title.PropertyName},");
                    }
                    stringBuilderCsv.Append("\r\n");

                    foreach (dynamic citizen in statisticalCitizens)
                    {
                        foreach (var property in metaDataInfoCollection)
                        {
                            var value = (string)citizen[property.PropertyName];
                            stringBuilderCsv.Append($"{(string)citizen[property.PropertyName]},");
                        }

                        stringBuilderCsv.Append("\r\n");
                    }

                    string result = stringBuilderCsv.ToString().Replace(",\r\n", "\r\n");

                    File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName));

                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationSettings.CsvFileName), result);
                }
            }
            catch
            {
                logger.LogError("Error saving CsvFile");
                throw;
            }
        }

        public List<object> ReadDataByFilter(string filter)
        {
            string[] filterPairs = filter.Split('&');

            List<object> result = null;

            lock (locker)
            {
                foreach (var filterPair in filterPairs)
                {
                    if (filterPair.Contains(">"))
                    {
                        string[] filterItem = filterPair.Split('>');

                        var currentProperty = metaDataInfoCollection.FirstOrDefault(property => property.PropertyName == filterItem[0]);

                        if (currentProperty == null)
                        {
                            logger.LogError($"Error ReadDataByFilter - property {filterItem[0]} not found");
                            return null;
                        }

                        if (currentProperty.PropertyType == "System.Int32")
                        {
                            int value;
                            if (!int.TryParse(filterItem[1], out value))
                            {
                                logger.LogError($"Error ReadDataByFilter - value of property {filterItem[0]} is not correctly");
                                return null;
                            }

                            if (result == null)
                            {
                                result = statisticalCitizens.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) > value);
                            }
                            else
                            {
                                result = result.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) > value);
                            }

                            if (result == null)
                                return null;
                        }
                    }

                    if (filterPair.Contains("<"))
                    {
                        string[] filterItem = filterPair.Split('<');

                        var currentProperty = metaDataInfoCollection.FirstOrDefault(property => property.PropertyName == filterItem[0]);

                        if (currentProperty == null)
                        {
                            logger.LogError($"Error ReadDataByFilter - property {filterItem[0]} not found");
                            return null;
                        }

                        if (currentProperty.PropertyType == "System.Int32")
                        {
                            int value;
                            if (!int.TryParse(filterItem[1], out value))
                            {
                                logger.LogError($"Error ReadDataByFilter - value of property {filterItem[0]} is not correctly");
                                return null;
                            }

                            if (result == null)
                            {
                                result = statisticalCitizens.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) < value);
                            }
                            else
                            {
                                result = result.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) < value);
                            }

                            if (result == null)
                                return null;
                        }
                    }

                    if (filterPair.Contains("="))
                    {
                        string[] filterItem = filterPair.Split('=');

                        var currentProperty = metaDataInfoCollection.FirstOrDefault(property => property.PropertyName == filterItem[0]);

                        if (currentProperty == null)
                        {
                            logger.LogError($"Error ReadDataByFilter - property {filterItem[0]} not found");
                            return null;
                        }

                        if (currentProperty.PropertyType == "System.Int32")
                        {
                            int value;
                            if (!int.TryParse(filterItem[1], out value))
                            {
                                logger.LogError($"Error ReadDataByFilter - value of property {filterItem[0]} is not correctly");
                                return null;
                            }

                            if (result == null)
                            {
                                result = statisticalCitizens.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) == value);
                            }
                            else
                            {
                                result = result.FindAll(item => Convert.ToInt32(((dynamic)(item))[currentProperty.PropertyName]) == value);
                            } 
                            if (result == null)
                                return null;
                        }
                        else
                        {
                            if (result == null)
                            {
                                result = statisticalCitizens.FindAll(item => Convert.ToString(((dynamic)(item))[currentProperty.PropertyName]) == filterItem[1]).ToList<object>();
                            }
                            else
                            {
                                result = result.FindAll(item => Convert.ToString(((dynamic)(item))[currentProperty.PropertyName]) == filterItem[1]).ToList<object>();
                            }
                                
                            if (result == null)
                                return null;
                        }
                    }
                }
            }

            return result.ToList<object>();
        }

        public void DeleteById(int id)
        {
            lock (locker)
            {
                statisticalCitizens.RemoveAll(citizen => Convert.ToInt32(((dynamic)(citizen))["id"]) == id);
            }
            try
            {
                SaveChangesInCsvFile();
            }
            catch
            {
                throw;
            }
        }

        public bool Update(int id, string propertyName, string propertyValue)
        {
            if (propertyName == "id")
                return false;
            lock (locker)
            {
                var currentstatisticalCitizen = statisticalCitizens.FirstOrDefault(citizen => Convert.ToInt32(((dynamic)(citizen))["id"]) == id);

                if (currentstatisticalCitizen == null)
                {
                    logger.LogError($"Update error - Element with id={id} not found");
                    return false;
                }

                var currentProperty = metaDataInfoCollection.FirstOrDefault(property => property.PropertyName == propertyName);
                if (currentProperty == null)
                {
                    logger.LogError($"Update error - property {propertyName} not found");
                    return false;
                }

                if (currentProperty.PropertyType == "System.Int32")
                {
                    int value;
                    if (!int.TryParse(propertyValue, out value))
                    {
                        logger.LogError($"Update error - type of the property {propertyName} is not correctly");
                        return false;
                    }
                    else
                    {
                        ((dynamic)(currentstatisticalCitizen))[propertyName] = value;
                    }
                }
                else
                {
                    ((dynamic)(currentstatisticalCitizen))[propertyName] = propertyValue;
                }

                try
                {
                    SaveChangesInCsvFile();
                }
                catch
                {
                    logger.LogError($"Update error");
                    throw;
                }

            }
            return true;
        }

    }
}
