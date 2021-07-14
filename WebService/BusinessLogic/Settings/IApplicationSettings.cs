﻿namespace WebService.BusinessLogic.Settings
{
    public interface IApplicationSettings
    {
        string CsvFileName { get; set; }
        string SqliteFileName { get; set; }
        string DataInfoFileName { get; set; }
    }
}